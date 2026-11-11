using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Leaves out the pages whose <c>version</c> front matter range doesn't include the version being
///     built, so they are missing from that version's site, navigation and search.
/// </summary>
/// <remarks>
///     Runs only when the build has a <see cref="BuildContext.CurrentVersion" />, which hosts set when
///     versioning is enabled; otherwise the key is ignored. A page whose range can't be parsed is kept
///     with a warning. A current version without a number, such as <c>next</c>, keeps every page.
///     Runs before the plugin hook, so plugins don't spend work on pages that are left out.
/// </remarks>
public sealed class VersionGatePhase(ILogger<VersionGatePhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "VersionGate";

	/// <inheritdoc />
	public int Order => 450;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (context.CurrentVersion is not { } current)
		{
			return Task.CompletedTask;
		}

		VersionNumber? number = VersionNumber.TryParse(current.Number ?? current.Label, out VersionNumber parsed)
			? parsed
			: null;
		int removed = 0;

		for (int i = context.Pages.Count - 1; i >= 0; i--)
		{
			ct.ThrowIfCancellationRequested();

			DocPage page = context.Pages[i];
			string? text = page.FrontMatter.Version;
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}

			if (!VersionRange.TryParse(text, out VersionRange? range, out string? error))
			{
				context.Diagnostics.Warning(
					$"{page.SourcePath ?? page.Route}: front matter version '{text}' is not a version range ({error}); the page is kept in every version",
					Name);
				continue;
			}

			if (number is { } version && !range.Contains(version))
			{
				logger.LogDebug("Leaving out {Route}: version {Version} is not in '{Range}'", page.Route, version, range);
				context.Pages.RemoveAt(i);
				removed++;
			}
		}

		if (removed > 0)
		{
			logger.LogInformation("Left out {Count} page(s) whose version range excludes {Version}", removed,
				current.Label);
		}

		return Task.CompletedTask;
	}
}
