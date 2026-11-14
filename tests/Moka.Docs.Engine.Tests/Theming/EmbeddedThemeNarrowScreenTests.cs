using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     Keeps pages from scrolling sideways on phones. The landing page was 683px wide on a 565px screen
///     (and 560px on a 320px one) from 1.9.0 on: an 800px glow centered with a transform, grids whose
///     columns couldn't get narrower than a long word, 200px buttons, a nowrap banner link and wide
///     tables. These read the stylesheets the way a browser applies them to a phone, with no browser.
/// </summary>
public sealed partial class EmbeddedThemeNarrowScreenTests
{
	private const double _phoneWidth = 320;
	private static readonly string _css = ThemeCss.All;
	private static readonly string _rtl = EmbeddedThemeProvider.GetRtlCss().Replace("\r\n", "\n");

	private sealed record CssRule(IReadOnlyList<string> Groups, string Selector, IReadOnlyList<(string Property, string Value)> Declarations)
	{
		public string? Value(string property) =>
			Declarations.Where(d => d.Property == property).Select(d => d.Value).LastOrDefault();

		public bool AppliesToPhones =>
			!Groups.Any(g => g.Contains("min-width", StringComparison.Ordinal) || g.Contains("print", StringComparison.Ordinal));
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex CommentRegex();

	[GeneratedRegex(@"^(?<n>-?\d+(?:\.\d+)?)(?<unit>px|rem|em)$")]
	private static partial Regex LengthRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(@"minmax\(")]
	private static partial Regex MinmaxRegex();

	// A flexible track outside minmax(): 1fr, 2fr, repeat(3, 1fr).
	[GeneratedRegex(@"(^|[\s(,])[\d.]*fr\b")]
	private static partial Regex FrTrackRegex();

	private static List<CssRule> Parse(string css)
	{
		css = CommentRegex().Replace(css, "");
		var rules = new List<CssRule>();
		var groups = new List<string>();
		var prelude = new StringBuilder();
		for (int i = 0; i < css.Length; i++)
		{
			char c = css[i];
			if (c == '{')
			{
				string head = prelude.ToString().Trim();
				prelude.Clear();
				if (head.StartsWith('@'))
				{
					groups.Add(head);
					continue;
				}

				int end = css.IndexOf('}', i);
				rules.Add(new CssRule([.. groups], WhitespaceRegex().Replace(head, " "), Declarations(css[(i + 1)..end])));
				i = end;
			}
			else if (c == '}')
			{
				if (groups.Count > 0)
				{
					groups.RemoveAt(groups.Count - 1);
				}

				prelude.Clear();
			}
			else
			{
				prelude.Append(c);
			}
		}

		return rules;
	}

	// Splits on semicolons outside parentheses and quotes, so data URIs and functions stay whole.
	private static List<(string, string)> Declarations(string body)
	{
		var list = new List<(string, string)>();
		int depth = 0;
		char quote = '\0';
		int start = 0;
		for (int i = 0; i <= body.Length; i++)
		{
			char c = i < body.Length ? body[i] : ';';
			if (quote != '\0')
			{
				quote = c == quote ? '\0' : quote;
				continue;
			}

			switch (c)
			{
				case '"' or '\'':
					quote = c;
					break;
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				case ';' when depth == 0:
					string declaration = body[start..i].Trim();
					int colon = declaration.IndexOf(':', StringComparison.Ordinal);
					if (colon > 0)
					{
						list.Add((declaration[..colon].Trim(), declaration[(colon + 1)..].Replace("!important", "").Trim()));
					}

					start = i + 1;
					break;
			}
		}

		return list;
	}

	private static double? Pixels(string value)
	{
		Match length = LengthRegex().Match(value.Trim());
		if (!length.Success)
		{
			return null;
		}

		double n = double.Parse(length.Groups["n"].Value, CultureInfo.InvariantCulture);
		return length.Groups["unit"].Value == "px" ? n : n * 16;
	}

	private static readonly List<CssRule> _rules = [.. Parse(_css), .. Parse(_rtl)];

	// Every rule for the selector at top level (or in the group), in cascade order, as one.
	private static CssRule Rule(string selector, string? group = null)
	{
		List<CssRule> matches = _rules.Where(r => r.Selector == selector && (group is null ? r.Groups.Count == 0 : r.Groups.Contains(group))).ToList();
		matches.Should().NotBeEmpty("the stylesheet should have a rule for {0}{1}", selector, group is null ? "" : " in " + group);
		return new CssRule(matches[0].Groups, selector, [.. matches.SelectMany(r => r.Declarations)]);
	}

	[Fact]
	public void Boxes_AreNeverWiderThanAPhoneUnlessCapped()
	{
		// A width, min-width or flex basis over 320px makes a phone scroll sideways unless a max-width
		// caps it or the flex item can shrink. The landing glow was width: 800px.
		var wide = new List<string>();
		foreach (CssRule rule in _rules.Where(r => r.AppliesToPhones))
		{
			foreach ((string property, string value) in rule.Declarations)
			{
				string[] flex = property == "flex" ? value.Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
				double? size = property switch
				{
					"width" or "min-width" or "flex-basis" => Pixels(value),
					"flex" when flex.Length == 3 && flex[1] == "0" => Pixels(flex[2]),
					_ => null
				};
				bool capped = property != "min-width" && rule.Value("max-width") is { } max && Pixels(max) is null or <= _phoneWidth;
				if (size > _phoneWidth && !capped)
				{
					wide.Add($"{rule.Selector} {{ {property}: {value} }}");
				}
			}
		}

		wide.Should().BeEmpty();
	}

	[Fact]
	public void GridColumns_CanGetNarrowerThanTheirContent()
	{
		// A bare 1fr column is minmax(auto, 1fr): never narrower than its longest word, so a feature
		// card titled with a package name widened the grid past the screen. A minimum over 10rem has
		// to give way on a narrower box: minmax(min(100%, 250px), 1fr).
		var rigid = new List<string>();
		foreach (CssRule rule in _rules)
		{
			if (rule.Value("grid-template-columns") is not { } columns)
			{
				continue;
			}

			string outside = columns;
			foreach (Match minmax in MinmaxRegex().Matches(columns))
			{
				int open = minmax.Index + minmax.Length;
				int depth = 1;
				int close = open;
				for (; close < columns.Length && depth > 0; close++)
				{
					depth += columns[close] switch { '(' => 1, ')' => -1, _ => 0 };
				}

				string least = columns[open..(close - 1)].Split(',')[0].Trim();
				if (Pixels(least) > 160)
				{
					rigid.Add($"{rule.Selector} {{ grid-template-columns: {columns} }}");
				}

				outside = outside.Replace(columns[minmax.Index..close], "minmax()", StringComparison.Ordinal);
			}

			if (FrTrackRegex().IsMatch(outside))
			{
				rigid.Add($"{rule.Selector} {{ grid-template-columns: {columns} }}");
			}
		}

		rigid.Should().BeEmpty();
	}

	[Fact]
	public void LandingGlow_SpansThePageInsteadOfStickingOutOfIt()
	{
		CssRule glow = Rule(".landing::before");
		glow.Value("left").Should().Be("0");
		glow.Value("right").Should().Be("0");
		glow.Value("width").Should().BeNull();
		glow.Value("transform").Should().BeNull();
		glow.Value("background").Should().StartWith("radial-gradient(565.69px 353.55px at 50% 50%,", "the 800x500 box's farthest-corner ellipse, so it paints as before");
	}

	[Fact]
	public void LongWords_BreakInsteadOfWideningThePage()
	{
		Rule("#mokadocs-main").Value("overflow-wrap").Should().Be("break-word");
		Rule(":is(.site-footer, .landing-footer)").Value("overflow-wrap").Should().Be("anywhere");

		// Flex items are never narrower than their longest word under break-word, so the flex rows of
		// author text take anywhere.
		foreach (string selector in new[]
		         {
			         ".page-content .admonition-title", ".component-card-header", ".component-details > summary", ".install-header",
			         ".landing-btn", ".feedback-prompt", "html[data-home=\"portal\"] .home-section"
		         })
		{
			Rule(selector).Value("overflow-wrap").Should().Be("anywhere", "{0} holds text in a flex or grid row", selector);
		}

		Rule(".breadcrumbs li").Value("min-width").Should().Be("0");
		Rule(".component-badge", "@media (max-width: 768px)").Value("white-space").Should().Be("normal");
		Rule(".component-kbd", "@media (max-width: 768px)").Value("white-space").Should().Be("normal", "a key combination wraps between its keys");
		Rule(".site-banner-link, .site-banner-link:hover").Value("white-space").Should().BeNull("a long link wraps onto the next line");
	}

	[Fact]
	public void LandingHero_FitsAPhone()
	{
		Rule(".landing-hero-title").Value("font-size").Should().Be("clamp(2.25rem, 6vw, 4.5rem)");
		Rule(".landing .landing-content > h1:first-child, .landing .landing-content > h1:first-of-type").Value("font-size").Should().Be("clamp(2.25rem, 6vw, 4.5rem)");

		const string phone = "@media (max-width: 480px)";
		CssRule actions = Rule(".landing-hero-actions", phone);
		actions.Value("flex-direction").Should().Be("column");
		actions.Value("align-items").Should().Be("stretch");
		CssRule button = Rule(".landing-btn", phone);
		button.Value("width").Should().Be("100%");
		button.Value("min-width").Should().Be("0");
		Rule(".landing-feature-card", "@media (max-width: 600px)").Value("padding").Should().Be("1.5rem 1.25rem");
		Rule(".landing-features-grid", "@media (max-width: 600px)").Value("grid-template-columns").Should().Be("minmax(0, 1fr)");
	}

	[Fact]
	public void WideContent_ScrollsInsideItsOwnBox()
	{
		CssRule table = Rule(".landing-md-content table", "@media (max-width: 768px)");
		table.Value("display").Should().Be("block");
		table.Value("overflow-x").Should().Be("auto");
		Rule(".page-content table").Value("overflow-x").Should().Be("auto");
		Rule(".install-tabs", "@media (max-width: 480px)").Value("overflow-x").Should().Be("auto");
	}

	[Fact]
	public void HeaderButtons_AreBiggerOnPhones()
	{
		Rule(":root", "@media (max-width: 768px)").Value("--header-button").Should().Be("40px");
		Rule(":root", "@media (max-width: 359px)").Value("--header-button").Should().Be("36px", "the site name keeps its room beside four buttons");
		Rule(".version-trigger", "@media (max-width: 768px)").Value("min-height").Should().Be("var(--header-button)");
		Rule(".theme-toggle, .mobile-nav-toggle").Value("width").Should().Be("var(--header-button)");
		Rule(".appearance-trigger").Value("height").Should().Be("var(--header-button)");
		CssRule search = Rule(".site-header .header-actions .search-trigger", "@media (max-width: 768px)");
		search.Value("width").Should().Be("var(--header-button)");
		search.Value("padding").Should().Be("0");
		Rule("html[data-style=\"terminal\"]").Value("--header-button").Should().Be("32px");
		Rule("html[data-navbar=\"compact\"]").Value("--header-button").Should().Be("32px");
	}

	[Fact]
	public void HiddenGlossaryTips_TakeNoRoom()
	{
		// A hidden tip near the right edge kept its box and made the page scroll sideways. It is scaled to
		// nothing once it has faded out, in both directions.
		Rule(".glossary-tip").Value("transform").Should().EndWith("scale(var(--tip-scale, 0))");
		Rule(".glossary-tip").Value("transition").Should().Contain("transform 0s var(--transition)");
		Rule(".glossary-term:hover > .glossary-tip, .glossary-term:focus-within > .glossary-tip, .glossary-term.is-open > .glossary-tip")
			.Value("--tip-scale").Should().Be("1");
		Rule(":where(html[dir=\"rtl\"]) .glossary-tip").Value("transform").Should().EndWith("scale(var(--tip-scale, 0))");
	}
}
