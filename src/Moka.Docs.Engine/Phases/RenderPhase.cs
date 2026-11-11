using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Applies theme templates to every page, producing final HTML output.
/// </summary>
public sealed class RenderPhase(
	ScribanTemplateEngine templateEngine,
	ThemeResolver themeResolver,
	ILogger<RenderPhase> logger) : IBuildPhase
{
	/// <summary>Where a shared sidebar is written, relative to the output directory.</summary>
	public const string SidebarScriptPath = "_theme/js/sidebar.js";

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
		if (theme.IsEmbedded)
		{
			ReportUnknownStyle(context);
		}

		// Update the theme context with the current config, navigation, and version data
		var renderContext = new ThemeRenderContext
		{
			Config = context.Config,
			Navigation = context.Navigation,
			Templates = themeContext.Templates,
			Partials = themeContext.Partials,
			CssFiles = VersionedAssetUrls(themeContext.CssFiles, theme, context),
			JsFiles = VersionedAssetUrls(themeContext.JsFiles, theme, context),
			AllPages = context.Pages,
			Versions = context.Versions,
			CurrentVersion = context.CurrentVersion,
			PackageInfo = context.PackageInfo,
			BrandAssetFiles = context.BrandAssetFiles,
			Blog = context.Blog,
			SidebarScriptUrl = ShareSidebar(context, theme, ct)
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
				string html = templateEngine.RenderPage(page, renderContext, ct);

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
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
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
	///     Decides between an inline and a shared sidebar (<c>theme.options.sidebar</c>). A shared
	///     sidebar is rendered once and staged as <see cref="SidebarScriptPath" />; the result is its
	///     URL with a content hash, or <c>null</c> for an inline sidebar.
	/// </summary>
	/// <remarks>
	///     Every page used to hold the whole sidebar: 5012 pages with an entry each wrote 8.4 GB.
	///     The file is written by <c>OutputPhase</c> with the other staged files, after the clean and
	///     not at all in a dry run.
	/// </remarks>
	private string? ShareSidebar(BuildContext context, ResolvedTheme theme, CancellationToken ct)
	{
		ThemeOptions options = context.Config.Theme.Options;
		if (!SidebarModes.IsKnown(options.Sidebar))
		{
			context.Diagnostics.Warning(
				$"Sidebar mode '{options.Sidebar}' is not one of {string.Join(", ", SidebarModes.Names)}; using '{SidebarModes.Auto}'",
				Name);
		}

		string mode = SidebarModes.Resolve(options.Sidebar);
		int entries = CountEntries(context.Navigation?.Items ?? []);
		if (mode == SidebarModes.Inline || (mode == SidebarModes.Auto && entries <= options.SidebarInlineLimit))
		{
			return null;
		}

		if (!theme.IsEmbedded)
		{
			if (mode == SidebarModes.Shared)
			{
				context.Diagnostics.Warning(
					$"Theme '{context.Config.Theme.Name}' has no shared sidebar, so every page holds the whole sidebar",
					Name);
			}

			return null;
		}

		var sidebarContext = new ThemeRenderContext
		{
			Config = context.Config,
			Navigation = context.Navigation,
			Templates = [],
			Partials = [],
			AllPages = context.Pages
		};

		string html;
		try
		{
			html = templateEngine.RenderSidebar(EmbeddedThemeProvider.GetSidebarTemplate(), sidebarContext, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			context.Diagnostics.Warning(
				$"The shared sidebar could not be rendered, so every page holds the whole sidebar: {ex.Message}", Name);
			return null;
		}

		byte[] script = new UTF8Encoding(false).GetBytes(EmbeddedThemeProvider.CreateSidebarScript(html));
		context.DeferredOutputFiles[SidebarScriptPath] = script;
		logger.LogInformation("The sidebar has {Entries} entries: written once to {Path} ({Bytes} bytes) for every page",
			entries, SidebarScriptPath, script.Length);

		return "/" + SidebarScriptPath + "?v=" + ThemeAssetPhase.ContentHash(script);
	}

	private static int CountEntries(List<NavigationNode> nodes) => nodes.Sum(n => 1 + CountEntries(n.Children));

	private static List<string> VersionedAssetUrls(List<string> urls, ResolvedTheme theme, BuildContext context) =>
		urls.Select(url => ThemeAssetPhase.VersionedUrl(url, theme, context)).ToList();

	/// <summary>
	///     Warns when <c>theme.options.style</c> names no style of the default theme. Pages
	///     render with the default style then. A custom theme may define its own style names, so
	///     it is only checked for the embedded theme.
	/// </summary>
	private void ReportUnknownStyle(BuildContext context)
	{
		string style = context.Config.Theme.Options.Style;
		if (!ThemeStyles.IsKnown(style))
		{
			context.Diagnostics.Warning(
				$"Style '{style}' is not one of {string.Join(", ", ThemeStyles.Names)}; using '{ThemeStyles.Default}'",
				Name);
		}
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
