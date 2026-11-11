using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Checks the appearance styles of the default theme. A style is CSS scoped to
///     <c>html[data-style]</c> plus the picker script, so these look for the rules that implement it.
/// </summary>
public sealed class EmbeddedThemeStyleTests
{
	private const string _sharedScope =
		"html:is([data-style=\"moka\"], [data-style=\"minimal\"], [data-style=\"classic\"], [data-style=\"soft\"])";

	private static readonly string _css = EmbeddedThemeProvider.GetCss().Replace("\r\n", "\n");

	public static TheoryData<string> Styles()
	{
		var data = new TheoryData<string>();
		foreach (string style in ThemeStyles.Names.Where(s => s != ThemeStyles.Default))
		{
			data.Add(style);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Styles))]
	public void EveryStyle_HasALightAndADarkBlock(string style)
	{
		string light = Rule($"\nhtml[data-style=\"{style}\"] {{");
		string dark = Rule($"\nhtml[data-style=\"{style}\"][data-theme=\"dark\"] {{");

		light.Should().Contain("--color-bg:").And.Contain("--color-text:").And.Contain("--color-text-muted:");
		dark.Should().Contain("--color-bg:").And.Contain("--color-text:").And.Contain("--color-text-muted:");
	}

	[Fact]
	public void StyleRules_AreScopedToTheirStyle()
	{
		// An unscoped selector would change the default look, which must stay as it was.
		int start = _css.IndexOf("/* Styles: shared tokens", StringComparison.Ordinal);
		start.Should().BeGreaterThan(0);
		int end = _css.IndexOf("/* Layouts:", start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start, "the layout variants follow the styles and have their own scoping test");
		string[] allowed =
		[
			_sharedScope,
			$":where({_sharedScope})",
			.. ThemeStyles.Names.Where(s => s != ThemeStyles.Default).Select(s => $"html[data-style=\"{s}\"]")
		];

		var unscoped = new List<string>();
		bool inComment = false;
		foreach (string line in _css[start..end].Split('\n'))
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

			if (!allowed.Any(scope => selector.StartsWith(scope, StringComparison.Ordinal)))
			{
				unscoped.Add(selector);
			}
		}

		unscoped.Should().BeEmpty();
	}

	[Fact]
	public void StyleRules_KeepPseudoElementsOutOfIs()
	{
		// A pseudo-element inside :is() is invalid, and the browser drops the whole rule.
		Regex.Matches(_css, @":is\([^)]*::").Should().BeEmpty();
	}

	[Fact]
	public void MokaStyle_HasNoDropShadows()
	{
		string moka = Rule("\nhtml[data-style=\"moka\"] {");

		moka.Should().Contain("--shadow-sm: none;").And.Contain("--shadow-md: none;").And.Contain("--shadow-lg: none;");
	}

	[Fact]
	public void MokaStyle_TintsBordersWithTheActiveAccent()
	{
		string dark = Rule("\nhtml[data-style=\"moka\"][data-theme=\"dark\"] {");

		dark.Should().Contain("--mk-border: color-mix(in srgb, var(--color-primary) 6%, transparent);");
		dark.Should().Contain("--mk-border-visible: color-mix(in srgb, var(--color-primary) 12%, transparent);");
		dark.Should().Contain("--mk-border-strong: color-mix(in srgb, var(--color-primary) 24%, transparent);");
	}

	[Fact]
	public void Picker_IsKeyboardAccessibleAndRemembersTheChoice()
	{
		string js = EmbeddedThemeProvider.GetJs();

		js.Should().Contain("localStorage.setItem('mokadocs-style', style);");
		js.Should().Contain("opt.setAttribute('aria-checked', active ? 'true' : 'false');");
		js.Should().Contain("e.key === 'ArrowDown'").And.Contain("e.key === 'Escape'");
	}

	[Fact]
	public void Picker_SwitchesToDarkForMokaOnlyWhenTheReaderNeverChose()
	{
		// The dark mode toggle stores mokadocs-theme. Without it, picking Moka switches to dark.
		string js = EmbeddedThemeProvider.GetJs();

		js.Should().Contain("if (!localStorage.getItem('mokadocs-theme')) {");
		js.Should().Contain("var dark = style === 'moka' || window.matchMedia('(prefers-color-scheme: dark)').matches;");
	}

	[Fact]
	public void Picker_IsHiddenOnNarrowScreensLikeTheOtherAppearancePickers()
	{
		_css.Should().Contain("@media (max-width: 768px) {\n    .style-selector { display: none; }\n}");
	}

	private static string Rule(string selector)
	{
		int start = _css.IndexOf(selector, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, $"the theme should have a rule for {selector.Trim()}");
		return _css[start..(_css.IndexOf('}', start) + 1)];
	}
}
