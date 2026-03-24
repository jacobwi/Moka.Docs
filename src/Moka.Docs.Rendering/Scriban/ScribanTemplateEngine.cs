using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Navigation;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
using Scriban;
using Scriban.Runtime;

namespace Moka.Docs.Rendering.Scriban;

/// <summary>
///     Renders pages using Scriban templates from the active theme.
/// </summary>
public sealed class ScribanTemplateEngine(ILogger<ScribanTemplateEngine> logger)
{
    private readonly Dictionary<string, Template> _templateCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Renders a page using the specified layout template.
    /// </summary>
    /// <param name="page">The page to render.</param>
    /// <param name="themeContext">The theme rendering context with templates, config, nav, etc.</param>
    /// <returns>The fully rendered HTML string.</returns>
    public string RenderPage(DocPage page, ThemeRenderContext themeContext)
    {
        var layoutName = page.FrontMatter.Layout;
        var templateContent = themeContext.GetTemplate(layoutName)
                              ?? themeContext.GetTemplate("default")
                              ?? throw new InvalidOperationException(
                                  $"Layout template '{layoutName}' not found in theme.");

        var template = GetOrParseTemplate(layoutName, templateContent);

        var scriptObject = BuildScriptObject(page, themeContext);
        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        context.MemberRenamer = member => member.Name;

        try
        {
            var html = template.Render(context);
            return FixPreBlockIndentation(html);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to render template '{Layout}' for page '{Route}'",
                layoutName, page.Route);
            return $"<!-- Template rendering error: {ex.Message} -->\n{page.Content.Html}";
        }
    }

    private Template GetOrParseTemplate(string name, string content)
    {
        if (_templateCache.TryGetValue(name, out var cached))
            return cached;

        var template = Template.Parse(content);
        if (template.HasErrors)
        {
            var errors = string.Join("; ", template.Messages.Select(m => m.Message));
            logger.LogWarning("Template '{Name}' has errors: {Errors}", name, errors);
        }

        _templateCache[name] = template;
        return template;
    }

    private static string PrefixRoute(string route, string basePath)
    {
        if (basePath == "/") return route;
        if (string.IsNullOrEmpty(route) || route == "/") return basePath + "/";
        return basePath + (route.StartsWith('/') ? route : "/" + route);
    }

    private static ScriptObject BuildScriptObject(DocPage page, ThemeRenderContext ctx)
    {
        var so = new ScriptObject();
        var bp = ctx.Config.Build.BasePath;

        // Base path for templates and JS
        so.SetValue("base_path", bp == "/" ? "" : bp, false);

        #region Page Data

        so.SetValue("page", new ScriptObject
        {
            { "title", page.FrontMatter.Title },
            { "description", page.FrontMatter.Description },
            { "content", page.Content.Html },
            { "route", PrefixRoute(page.Route, bp) },
            { "toc", BuildTocObject(page.TableOfContents) },
            {
                "show_toc",
                page.FrontMatter.Toc && page.TableOfContents.Entries.Count > 0 &&
                ctx.Config.Theme.Options.ShowTableOfContents
            },
            { "tags", page.FrontMatter.Tags },
            { "layout", page.FrontMatter.Layout },
            { "source_path", page.SourcePath ?? "" },
            { "last_modified", page.LastModified?.ToString("yyyy-MM-dd") ?? "" },
            { "is_api", page.Origin == PageOrigin.ApiGenerated }
        }, false);

        #endregion

        #region Site Config

        so.SetValue("site", new ScriptObject
        {
            { "title", ctx.Config.Site.Title },
            { "description", ctx.Config.Site.Description },
            { "url", ctx.Config.Site.Url },
            { "copyright", ctx.Config.Site.Copyright ?? "" },
            { "logo", ctx.Config.Site.Logo ?? "" }
        }, false);

        #endregion

        #region Theme Options

        so.SetValue("theme", new ScriptObject
        {
            { "primary_color", ctx.Config.Theme.Options.PrimaryColor },
            { "accent_color", ctx.Config.Theme.Options.AccentColor },
            { "code_theme", ctx.Config.Theme.Options.CodeTheme },
            { "show_edit_link", ctx.Config.Theme.Options.ShowEditLink },
            { "show_last_updated", ctx.Config.Theme.Options.ShowLastUpdated },
            { "color_themes", ctx.Config.Theme.Options.ColorThemes },
            { "code_theme_selector", ctx.Config.Theme.Options.CodeThemeSelector },
            { "code_style", ctx.Config.Theme.Options.CodeStyle },
            { "code_style_selector", ctx.Config.Theme.Options.CodeStyleSelector },
            { "show_feedback", ctx.Config.Theme.Options.ShowFeedback },
            { "show_dark_mode_toggle", ctx.Config.Theme.Options.ShowDarkModeToggle },
            { "show_animations", ctx.Config.Theme.Options.ShowAnimations },
            { "show_search", ctx.Config.Theme.Options.ShowSearch },
            { "show_table_of_contents", ctx.Config.Theme.Options.ShowTableOfContents },
            { "show_prev_next", ctx.Config.Theme.Options.ShowPrevNext },
            { "show_breadcrumbs", ctx.Config.Theme.Options.ShowBreadcrumbs },
            { "show_back_to_top", ctx.Config.Theme.Options.ShowBackToTop },
            { "show_copy_button", ctx.Config.Theme.Options.ShowCopyButton },
            { "show_line_numbers", ctx.Config.Theme.Options.ShowLineNumbers },
            { "toc_depth", ctx.Config.Theme.Options.TocDepth },
            { "show_version_selector", ctx.Config.Theme.Options.ShowVersionSelector },
            { "social_links", BuildSocialLinks(ctx.Config.Theme.Options.SocialLinks) }
        }, false);

        #endregion

        // Edit link
        if (ctx.Config.Site.EditLink is { } el && ctx.Config.Theme.Options.ShowEditLink)
        {
            var editUrl = $"{el.Repo.TrimEnd('/')}/edit/{el.Branch}/{el.Path.TrimEnd('/')}/{page.SourcePath}";
            so.SetValue("edit_url", editUrl, false);
        }

        #region Navigation and Breadcrumbs

        var pageRoutes = new HashSet<string>(ctx.AllPages.Select(p => p.Route), StringComparer.OrdinalIgnoreCase);
        so.SetValue("nav", BuildNavObject(ctx.Navigation, page.Route, pageRoutes, bp), false);

        so.SetValue("breadcrumbs",
            BuildBreadcrumbs(page.Route, page.FrontMatter.Title, ctx.Navigation, pageRoutes, bp), false);

        #endregion

        // Partials (injected as strings so templates can use {{ partials.head }})
        so.SetValue("partials", BuildPartialsObject(ctx), false);

        // CSS/JS paths (prefixed with base path)
        so.SetValue("css_files", ctx.CssFiles.Select(f => PrefixRoute(f, bp)).ToList(), false);
        so.SetValue("js_files", ctx.JsFiles.Select(f => PrefixRoute(f, bp)).ToList(), false);

        #region Version Data

        if (ctx.Versions.Count > 0)
        {
            var versionsArray = new ScriptArray();
            foreach (var v in ctx.Versions)
                versionsArray.Add(new ScriptObject
                {
                    { "label", v.Label },
                    { "slug", v.Slug },
                    { "is_default", v.IsDefault },
                    { "is_prerelease", v.IsPrerelease }
                });
            so.SetValue("versions", versionsArray, false);
            so.SetValue("current_version", ctx.CurrentVersion?.Label ?? "", false);
        }
        else
        {
            so.SetValue("versions", new ScriptArray(), false);
            so.SetValue("current_version", "", false);
        }

        #endregion

        // Package metadata for NuGet install widget
        if (ctx.PackageInfo is { } pkg)
            so.SetValue("package", new ScriptObject
            {
                { "name", pkg.Name },
                { "version", pkg.Version }
            }, false);

        #region Prev/Next Page Navigation

        var orderedPages = ctx.AllPages
            .Where(p => p.FrontMatter.Visibility == PageVisibility.Public && p.FrontMatter.Layout == "default")
            .OrderBy(p => p.FrontMatter.Order)
            .ThenBy(p => p.Route, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentIndex = orderedPages.FindIndex(p => p.Route == page.Route);
        if (currentIndex >= 0)
        {
            if (currentIndex > 0)
            {
                var prev = orderedPages[currentIndex - 1];
                so.SetValue("prev_page", new ScriptObject
                {
                    { "title", prev.FrontMatter.Title },
                    { "route", PrefixRoute(prev.Route, bp) }
                }, false);
            }

            if (currentIndex < orderedPages.Count - 1)
            {
                var next = orderedPages[currentIndex + 1];
                so.SetValue("next_page", new ScriptObject
                {
                    { "title", next.FrontMatter.Title },
                    { "route", PrefixRoute(next.Route, bp) }
                }, false);
            }
        }

        #endregion

        return so;
    }

    private static ScriptArray BuildTocObject(TableOfContents toc)
    {
        var arr = new ScriptArray();
        foreach (var entry in toc.Entries) arr.Add(BuildTocEntryObject(entry));
        return arr;
    }

    private static ScriptObject BuildTocEntryObject(TocEntry entry)
    {
        var obj = new ScriptObject
        {
            { "level", entry.Level },
            { "text", entry.Text },
            { "id", entry.Id }
        };

        var children = new ScriptArray();
        foreach (var child in entry.Children) children.Add(BuildTocEntryObject(child));
        obj.SetValue("children", children, false);

        return obj;
    }

    private static ScriptArray BuildNavObject(NavigationTree? nav, string activeRoute, HashSet<string> pageRoutes,
        string basePath)
    {
        if (nav is null) return [];

        var arr = new ScriptArray();
        foreach (var node in nav.Items) arr.Add(BuildNavNodeObject(node, activeRoute, pageRoutes, basePath));
        return arr;
    }

    private static ScriptObject BuildNavNodeObject(NavigationNode node, string activeRoute, HashSet<string> pageRoutes,
        string basePath)
    {
        var hasActiveChild = HasActiveDescendant(node, activeRoute);
        var hasChildren = node.Children.Count > 0;
        var hasPage = !string.IsNullOrEmpty(node.Route) && pageRoutes.Contains(node.Route);

        // A node is "active" only if its route matches AND it doesn't have an active child.
        // This prevents parent sections from showing as "current" when their route was
        // resolved to a child's route (e.g. /guide → /guide/getting-started).
        var isActive = node.Route == activeRoute && !hasActiveChild;

        var obj = new ScriptObject
        {
            { "label", node.Label },
            { "route", !string.IsNullOrEmpty(node.Route) ? PrefixRoute(node.Route, basePath) : "" },
            { "icon", ResolveIcon(node.Icon) },
            { "expanded", node.Expanded || hasActiveChild },
            { "is_active", isActive },
            { "has_active_child", hasActiveChild },
            { "has_children", hasChildren },
            { "has_page", hasPage }
        };

        var children = new ScriptArray();
        foreach (var child in node.Children)
            children.Add(BuildNavNodeObject(child, activeRoute, pageRoutes, basePath));
        obj.SetValue("children", children, false);

        return obj;
    }

    private static bool HasActiveDescendant(NavigationNode node, string activeRoute)
    {
        foreach (var child in node.Children)
        {
            if (child.Route == activeRoute) return true;
            if (HasActiveDescendant(child, activeRoute)) return true;
        }

        return false;
    }

    private static ScriptArray BuildSocialLinks(List<SocialLink> links)
    {
        var arr = new ScriptArray();
        foreach (var link in links)
        {
            var iconSvg = LucideIcons.Get(link.Icon) ?? link.Icon;
            arr.Add(new ScriptObject { { "icon", link.Icon }, { "url", link.Url }, { "icon_svg", iconSvg } });
        }
        return arr;
    }

    private static ScriptArray BuildBreadcrumbs(string route, string pageTitle, NavigationTree? nav,
        HashSet<string> pageRoutes, string basePath)
    {
        var crumbs = new ScriptArray();

        // Always start with Home
        crumbs.Add(new ScriptObject
            { { "label", "Home" }, { "url", PrefixRoute("/", basePath) }, { "is_current", false } });

        var segments = route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return crumbs;

        // Build intermediate crumbs from route segments
        var pathSoFar = "";
        for (var i = 0; i < segments.Length - 1; i++)
        {
            pathSoFar += "/" + segments[i];

            // Try to find a label from the nav tree
            var label = FindNavLabel(nav, pathSoFar) ?? FormatSegment(segments[i]);

            // Only make it a link if a real page exists at this route (avoid linking to redirects)
            var hasPage = pageRoutes.Contains(pathSoFar);
            crumbs.Add(new ScriptObject
            {
                { "label", label },
                { "url", hasPage ? PrefixRoute(pathSoFar + "/", basePath) : "" },
                { "is_current", false },
                { "has_page", hasPage }
            });
        }

        // Current page
        crumbs.Add(new ScriptObject
        {
            { "label", pageTitle }, { "url", PrefixRoute(route, basePath) }, { "is_current", true },
            { "has_page", true }
        });

        return crumbs;
    }

    private static string? FindNavLabel(NavigationTree? nav, string route)
    {
        if (nav is null) return null;
        return FindNavLabelInNodes(nav.Items, route);
    }

    private static string? FindNavLabelInNodes(List<NavigationNode> nodes, string route)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Route, route, StringComparison.OrdinalIgnoreCase))
                return node.Label;
            var childResult = FindNavLabelInNodes(node.Children, route);
            if (childResult is not null) return childResult;
        }

        return null;
    }

    private static string ResolveIcon(string? iconName)
    {
        if (string.IsNullOrEmpty(iconName)) return "";
        return LucideIcons.Get(iconName) ?? "";
    }

    private static string FormatSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment)) return "";
        var formatted = segment.Replace('-', ' ');
        return char.ToUpper(formatted[0]) + formatted[1..];
    }

    private static ScriptObject BuildPartialsObject(ThemeRenderContext ctx)
    {
        var obj = new ScriptObject();
        foreach (var (name, content) in ctx.Partials)
            // Parse and render partials as-is (they'll be included raw)
            obj.SetValue(name, content, false);
        return obj;
    }

    /// <summary>
    ///     Fixes whitespace inside <c>&lt;pre&gt;</c> blocks that gets added by Scriban template indentation.
    ///     Finds the common leading whitespace on lines within each pre block and strips it.
    /// </summary>
    private static string FixPreBlockIndentation(string html)
    {
        const string preOpen = "<pre>";
        const string preClose = "</pre>";

        var result = new StringBuilder(html.Length);
        var pos = 0;

        while (pos < html.Length)
        {
            var preStart = html.IndexOf(preOpen, pos, StringComparison.OrdinalIgnoreCase);
            if (preStart < 0)
            {
                result.Append(html, pos, html.Length - pos);
                break;
            }

            // Copy everything before <pre>
            result.Append(html, pos, preStart - pos);

            var contentStart = preStart + preOpen.Length;
            // Find matching </pre> — handle <pre ...> with attributes too
            var actualPreEnd = html.IndexOf('>', preStart);
            if (actualPreEnd < 0)
            {
                result.Append(html, preStart, html.Length - preStart);
                break;
            }

            contentStart = actualPreEnd + 1;

            var preEnd = html.IndexOf(preClose, contentStart, StringComparison.OrdinalIgnoreCase);
            if (preEnd < 0)
            {
                result.Append(html, preStart, html.Length - preStart);
                break;
            }

            // Extract pre content and strip common leading whitespace
            var preTag = html[preStart..contentStart];
            var content = html[contentStart..preEnd];
            var stripped = StripCommonIndent(content);

            result.Append(preTag);
            result.Append(stripped);
            result.Append(preClose);

            pos = preEnd + preClose.Length;
        }

        return result.ToString();
    }

    private static string StripCommonIndent(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length <= 1) return text;

        // Find minimum indentation across non-empty lines (skip first line which is on the <pre> line)
        var minIndent = int.MaxValue;
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var indent = 0;
            while (indent < line.Length && line[indent] == ' ') indent++;
            if (indent < minIndent) minIndent = indent;
        }

        if (minIndent == 0 || minIndent == int.MaxValue) return text;

        // Strip the common indent from all lines except the first
        var sb = new StringBuilder();
        sb.Append(lines[0]);
        for (var i = 1; i < lines.Length; i++)
        {
            sb.Append('\n');
            if (lines[i].Length > minIndent)
                sb.Append(lines[i][minIndent..]);
            else
                sb.Append(lines[i].TrimStart());
        }

        return sb.ToString();
    }
}

/// <summary>
///     Context provided to the template engine for rendering a page.
/// </summary>
public sealed class ThemeRenderContext
{
    /// <summary>Site configuration.</summary>
    public required SiteConfig Config { get; init; }

    /// <summary>Navigation tree.</summary>
    public NavigationTree? Navigation { get; init; }

    /// <summary>Layout templates keyed by name.</summary>
    public required Dictionary<string, string> Templates { get; init; }

    /// <summary>Partial templates keyed by name.</summary>
    public required Dictionary<string, string> Partials { get; init; }

    /// <summary>CSS file paths relative to output root.</summary>
    public List<string> CssFiles { get; init; } = [];

    /// <summary>JS file paths relative to output root.</summary>
    public List<string> JsFiles { get; init; } = [];

    /// <summary>All pages in the site, used for prev/next navigation.</summary>
    public IReadOnlyList<DocPage> AllPages { get; init; } = [];

    /// <summary>All configured documentation versions.</summary>
    public IReadOnlyList<DocVersion> Versions { get; init; } = [];

    /// <summary>The current version being built, if versioning is enabled.</summary>
    public DocVersion? CurrentVersion { get; init; }

    /// <summary>Package metadata for NuGet install widget.</summary>
    public PackageMetadata? PackageInfo { get; init; }

    /// <summary>Gets a template by layout name.</summary>
    public string? GetTemplate(string layoutName)
    {
        return Templates.GetValueOrDefault(layoutName);
    }
}