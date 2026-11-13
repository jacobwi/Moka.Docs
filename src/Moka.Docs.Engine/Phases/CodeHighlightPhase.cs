using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Parsing.Highlighting;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Highlights and wraps the code blocks that didn't come from a Markdown fence: API signatures
///     and source, plugin pages (OpenAPI examples, Python examples), Blazor preview sources and raw
///     HTML. Markdown fences are highlighted while they are parsed.
/// </summary>
/// <remarks>
///     It runs after the plugins, so a plugin that reads a code block's text out of the page
///     (the Blazor preview plugin compiles it) still finds plain text.
/// </remarks>
public sealed class CodeHighlightPhase(ILogger<CodeHighlightPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "CodeHighlight";

	/// <inheritdoc />
	public int Order => 680;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		int changed = 0;
		ThemeText text = ThemeText.For(context.Config);
		Parallel.For(0, context.Pages.Count, new ParallelOptions { CancellationToken = ct }, i =>
		{
			DocPage page = context.Pages[i];
			string html = CodeBlockHtml.EnhanceBlocks(page.Content.Html, text);
			if (!string.Equals(html, page.Content.Html, StringComparison.Ordinal))
			{
				page.Content = page.Content with { Html = html };
				Interlocked.Increment(ref changed);
			}
		});

		logger.LogDebug("Highlighted code blocks outside Markdown fences on {Count} page(s)", changed);
		return Task.CompletedTask;
	}
}
