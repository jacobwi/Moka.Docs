using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Applies theme templates to every page, producing final HTML output.
/// </summary>
public sealed class RenderPhase(
	ScribanTemplateEngine templateEngine,
	ThemeResolver themeResolver,
	ILogger<RenderPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "Render";

	/// <inheritdoc />
	public int Order => 900;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		ThemeRenderContext themeContext =
			themeResolver.Resolve(context.Config, context.RootDirectory, context.FileSystem).Context;

		// Update the theme context with the current config, navigation, and version data
		var renderContext = new ThemeRenderContext
		{
			Config = context.Config,
			Navigation = context.Navigation,
			Templates = themeContext.Templates,
			Partials = themeContext.Partials,
			CssFiles = themeContext.CssFiles,
			JsFiles = themeContext.JsFiles,
			AllPages = context.Pages,
			Versions = context.Versions,
			CurrentVersion = context.CurrentVersion,
			PackageInfo = context.PackageInfo
		};

		int rendered = 0;
		for (int i = 0; i < context.Pages.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			DocPage page = context.Pages[i];
			if (!context.IncludeDrafts && page.FrontMatter.Visibility == PageVisibility.Draft)
			{
				continue;
			}

			try
			{
				string html = templateEngine.RenderPage(page, renderContext);

				// Replace the page content with the fully rendered HTML
				context.Pages[i] = page with
				{
					Content = new PageContent
					{
						Html = html,
						PlainText = page.Content.PlainText
					}
				};

				rendered++;
			}
			catch (Exception ex)
			{
				context.Diagnostics.Warning(
					$"Failed to render page '{page.Route}': {ex.Message}", Name);
				logger.LogWarning(ex, "Failed to render page: {Route}", page.Route);
			}
		}

		logger.LogInformation("Rendered {Count} pages with theme templates", rendered);
		return Task.CompletedTask;
	}
}
