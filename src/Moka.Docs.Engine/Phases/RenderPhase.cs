using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
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
		ResolvedTheme theme = themeResolver.Resolve(context.Config, context.RootDirectory, context.FileSystem);
		ThemeRenderContext themeContext = theme.Context;

		// The resolver falls back to the embedded theme and only logs why, which a quiet build
		// never shows: a typo in theme.name looked like the custom theme had been ignored.
		string themeName = context.Config.Theme.Name;
		if (theme.IsEmbedded && !string.IsNullOrWhiteSpace(themeName)
		                     && !themeName.Equals(MokaDefaults.ThemeName, StringComparison.OrdinalIgnoreCase))
		{
			context.Diagnostics.Warning(
				$"Theme '{themeName}' was not found, or its folder has no layouts/*.html; using the default theme",
				Name);
		}

		ReportUnknownLayouts(context, themeContext);
		ReportUnknownSocialIcons(context);

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
		var failures = new Dictionary<string, (int Count, string Example)>();
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
				// A broken layout fails every page that uses it, so each distinct error is
				// reported once with a count.
				failures[ex.Message] = failures.TryGetValue(ex.Message, out (int Count, string Example) seen)
					? (seen.Count + 1, seen.Example)
					: (1, page.Route);
				logger.LogDebug(ex, "Failed to render page: {Route}", page.Route);
			}
		}

		// An error, not a warning: the page is written without its layout, and a build that
		// exits 0 gets published.
		foreach ((string message, (int count, string example)) in failures)
		{
			context.Diagnostics.Error(
				$"Failed to render {count} page(s) (for example '{example}'): {message}", Name);
		}

		logger.LogInformation("Rendered {Count} pages with theme templates", rendered);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Warns about social links whose icon is not in the icon set. The footer prints the
	///     name as text instead, which looked like a theme bug.
	/// </summary>
	private void ReportUnknownSocialIcons(BuildContext context)
	{
		foreach (SocialLink link in context.Config.Theme.Options.SocialLinks)
		{
			if (!string.IsNullOrWhiteSpace(link.Icon) && LucideIcons.Get(link.Icon) is null)
			{
				context.Diagnostics.Warning(
					$"Social link icon '{link.Icon}' is not in the icon set; the footer shows the name as text ({link.Url})",
					Name);
			}
		}
	}

	/// <summary>
	///     Warns once per layout name that the theme does not have. Such pages render with the
	///     default layout, which used to happen without a word (layout: wide, for example).
	/// </summary>
	private void ReportUnknownLayouts(BuildContext context, ThemeRenderContext theme)
	{
		IEnumerable<IGrouping<string, DocPage>> missing = context.Pages
			.Where(p => context.IncludeDrafts || p.FrontMatter.Visibility != PageVisibility.Draft)
			.Where(p => theme.GetTemplate(p.FrontMatter.Layout) is null)
			.GroupBy(p => p.FrontMatter.Layout, StringComparer.OrdinalIgnoreCase);

		foreach (IGrouping<string, DocPage> group in missing)
		{
			string example = group.First().SourcePath ?? group.First().Route;
			context.Diagnostics.Warning(
				$"Layout '{group.Key}' does not exist in the theme; {group.Count()} page(s) use the default layout instead (for example {example})",
				Name);
		}
	}
}
