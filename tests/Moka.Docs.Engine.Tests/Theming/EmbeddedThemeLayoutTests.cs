using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the CSS of the default theme's layout variants (<c>theme.options.layout</c>). Each
///     variant is a block of rules scoped to the attribute the layouts write on <c>&lt;html&gt;</c>.
/// </summary>
public sealed partial class EmbeddedThemeLayoutTests
{
	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");

	private static string LayoutCss()
	{
		int start = _css.IndexOf("/* Layouts:", StringComparison.Ordinal);
		start.Should().BeGreaterThan(0);
		return _css[start..];
	}

	private static string AttributeOf(ThemeLayoutPart part) => part.Key == "pageNav" ? "page-nav" : part.Key;

	[GeneratedRegex("""^(?::where\()?html(?:\[data-style="[a-z]+"\])?\[data-(?<part>[a-z-]+)="(?<variant>[a-z]+)"\]""")]
	private static partial Regex ScopeRegex();

	[Fact]
	public void LayoutRules_AreScopedToANonDefaultVariant()
	{
		// An unscoped rule would change the default layout, which must stay as it was.
		var unscoped = new List<string>();
		bool inComment = false;
		foreach (string line in LayoutCss().Split('\n'))
		{
			if (inComment || line.TrimStart().StartsWith("/*", StringComparison.Ordinal))
			{
				inComment = !line.Contains("*/", StringComparison.Ordinal);
				continue;
			}

			int brace = line.IndexOf('{', StringComparison.Ordinal);
			string? selector = brace >= 0 ? line[..brace].Trim()
				: line.Length > 0 && !char.IsWhiteSpace(line[0]) && line.EndsWith(',') ? line.Trim()
				: null;
			if (string.IsNullOrEmpty(selector) || selector.StartsWith('@'))
			{
				continue;
			}

			Match scope = ScopeRegex().Match(selector);
			string variant = scope.Groups["variant"].Value;
			ThemeLayoutPart? part = ThemeLayouts.Parts.SingleOrDefault(p => AttributeOf(p) == scope.Groups["part"].Value);
			if (!scope.Success || part is null || variant == ThemeLayouts.Default || !part.Names.Contains(variant))
			{
				unscoped.Add(selector);
			}
		}

		unscoped.Should().BeEmpty();
	}

	[Fact]
	public void EveryVariant_HasRules()
	{
		string css = LayoutCss();
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			string attribute = AttributeOf(part);
			foreach (string variant in part.Names.Where(n => n != ThemeLayouts.Default))
			{
				// footer: none leaves the footer out of the page, so it has nothing to style.
				if (part.Key == "footer" && variant == "none")
				{
					continue;
				}

				css.Should().Contain($"[data-{attribute}=\"{variant}\"]", $"{part.Key}: {variant} needs its rules");
			}
		}
	}

	[Fact]
	public void StickyParts_FollowTheHeaderOffset()
	{
		// The tabs, floating and autohide navbars move where the sticky sidebars start through
		// --header-offset. A rule using --header-height would sit under the tabs row.
		_css.Should().NotContain("top: var(--header-height)").And.Contain("top: var(--header-offset)");
		_css.Should().NotContain("calc(100vh - var(--header-height))");
	}
}
