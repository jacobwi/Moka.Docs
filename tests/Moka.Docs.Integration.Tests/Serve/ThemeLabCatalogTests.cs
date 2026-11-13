using System.Text.Json;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     The options the theme lab offers come from the Core lists, so a style, preset or layout part
///     added there shows up in the lab, and a save is checked against them.
/// </summary>
public sealed class ThemeLabCatalogTests
{
	private static readonly IReadOnlyList<ThemeLabOption> _options = ThemeLabCatalog.CreateOptions();

	private static ThemeLabOption Option(string key) => _options.Single(o => o.Key == key);

	private static IEnumerable<string> Values(string key) => Option(key).Choices.Select(c => c.Value);

	private static Dictionary<string, JsonElement> Json(string json) =>
		JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

	[Fact]
	public void Choices_TrackTheCoreLists()
	{
		Values("preset").Should().Equal(new[] { "" }.Concat(ThemePresets.Names));
		Values("style").Should().Equal(ThemeStyles.Names);
		Values("defaultColorTheme").Should().Equal(ColorPresets.Names);
		Values("codeTheme").Should().Equal(CodeThemes.Names);
		Values("codeStyle").Should().Equal(CodeStyles.Names);
		Values("banner.variant").Should().Equal(BannerVariants.Names);
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			Values("layout." + part.Key).Should().Equal(part.Names);
		}

		List<string> keys = ["preset", "style", "defaultColorTheme", "codeTheme", "codeStyle"];
		keys.AddRange(ThemeLayouts.Parts.Select(p => "layout." + p.Key));
		keys.AddRange(["sidebarToggle", "banner.text", "banner.link", "banner.variant"]);
		_options.Select(o => o.Key).Should().Equal(keys);
	}

	[Fact]
	public void EveryOptionAPresetSets_IsAnOptionTheLabShows()
	{
		ThemePresets.All.SelectMany(p => p.Options.Keys).Distinct()
			.Should().BeSubsetOf(_options.Select(o => o.Key));
		Option("preset").Choices.Skip(1).Select(c => c.Sets).Should().Equal(ThemePresets.All.Select(p => p.Options));
	}

	[Fact]
	public void StylesKnowWhichStartDark()
	{
		Option("style").Choices.Where(c => c.DarkFirst).Select(c => c.Value)
			.Should().Equal(ThemeStyles.All.Where(s => s.DarkFirst).Select(s => s.Name));
	}

	[Theory]
	[InlineData("pageNav", "data-page-nav", "MOKADOCS_LAYOUT_PAGE_NAV", "Page nav")]
	[InlineData("heroBackground", "data-hero-background", "MOKADOCS_LAYOUT_HERO_BACKGROUND", "Hero background")]
	[InlineData("sidebarScope", "data-sidebar-scope", "MOKADOCS_LAYOUT_SIDEBAR_SCOPE", "Sidebar scope")]
	[InlineData("toc", "data-toc", "MOKADOCS_LAYOUT_TOC", "TOC")]
	[InlineData("home", "data-home", "MOKADOCS_LAYOUT_HOME", "Home")]
	[InlineData("features", "data-features", "MOKADOCS_LAYOUT_FEATURES", "Features")]
	public void LayoutParts_NameTheirAttributeAndVariable(string part, string attribute, string variable, string label)
	{
		ThemeLabOption option = Option("layout." + part);

		option.Attribute.Should().Be(attribute);
		option.EnvironmentVariable.Should().Be(variable);
		option.Label.Should().Be(label);
		option.RemoveAttributeAtDefault.Should().BeTrue();
		option.YamlPath.Should().Be("theme.options.layout." + part);
	}

	[Fact]
	public void OptionsThatChangeMarkup_HaveNoAttributeToSwitch()
	{
		_options.Where(o => o.Attribute is null).Select(o => o.Key)
			.Should().Equal("preset", "sidebarToggle", "banner.text", "banner.link", "banner.variant");
	}

	[Fact]
	public void Validate_TurnsValuesIntoWhatGoesInTheFile()
	{
		Dictionary<string, string?> valid = ThemeLabCatalog.Validate(_options, Json("""
			{
				"style": " Moka ",
				"preset": "",
				"layout.navbar": "TABS",
				"layout.footer": null,
				"sidebarToggle": true,
				"banner.text": "  Version 2  ",
				"banner.link": "   "
			}
			"""), out string? error);

		error.Should().BeNull();
		valid.Should().BeEquivalentTo(new Dictionary<string, string?>
		{
			["style"] = "moka",
			["preset"] = null,
			["layout.navbar"] = "tabs",
			["layout.footer"] = null,
			["sidebarToggle"] = "true",
			["banner.text"] = "Version 2",
			["banner.link"] = null
		});
	}

	[Theory]
	[InlineData("""{ "theme.options.style": "moka" }""", "isn't an option the theme lab can save")]
	[InlineData("""{ "name": "custom" }""", "isn't an option the theme lab can save")]
	[InlineData("""{ "style": "fancy" }""", "'fancy' isn't a style name. Use one of: default, moka")]
	[InlineData("""{ "layout.navbar": "wide" }""", "'wide' isn't a navbar name")]
	[InlineData("""{ "preset": "glossy" }""", "isn't a preset name")]
	[InlineData("""{ "style": 3 }""", "isn't a style name")]
	[InlineData("""{ "sidebarToggle": "yes" }""", "takes true, false or null")]
	[InlineData("""{ "banner.text": 1 }""", "takes a string or null")]
	[InlineData("""{ "banner.text": "two\nlines" }""", "can't hold line breaks")]
	public void Validate_RefusesWhatTheCatalogDoesntHave(string json, string error)
	{
		ThemeLabCatalog.Validate(_options, Json(json), out string? message).Should().BeEmpty();

		message.Should().Contain(error);
	}

	[Fact]
	public void Validate_RefusesTextOverTheLimit()
	{
		ThemeLabCatalog.Validate(_options, Json($$"""{ "banner.text": "{{new string('a', 301)}}" }"""), out string? error);

		error.Should().Contain("longer than 300 characters");
	}

	[Fact]
	public void Mismatch_ComparesTheLoadedOptionsWithTheFileAndThePreset()
	{
		var written = new Dictionary<string, string?>
		{
			["theme.options.preset"] = "book",
			["theme.options.layout.navbar"] = "tabs"
		};
		ThemeOptions loaded = SiteConfigReader.Parse(
			"site:\n  title: Docs\ntheme:\n  options:\n    preset: book\n    layout:\n      navbar: tabs\n").Theme.Options;

		ThemeLabCatalog.Mismatch(_options, loaded, written, _ => null).Should().BeNull();
		ThemeLabCatalog.Mismatch(_options, loaded with { Style = "moka" }, written, _ => null)
			.Should().Be("theme.options.style read back as 'moka' instead of 'paper'");
		ThemeLabCatalog.Mismatch(_options, loaded with { Style = "moka" }, written,
			name => name == "MOKADOCS_STYLE" ? "moka" : null).Should().BeNull("the variable wins over the file");
	}

	[Fact]
	public void LayoutPartsReadersSwitch_ClearTheReadersPickOnSaveAndPreviewLiveOnTheirPages()
	{
		// A reader's pick in the Appearance panel's Layout section (mokadocs-layout-<part>) would hide the
		// saved variant. Pages built with theme.options.layoutSelector carry every variant's markup, so the
		// lab previews even the markup variants of those parts without a rebuild there.
		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			ThemeLabOption option = Option("layout." + part.Key);
			bool reader = ThemeLayouts.ReaderParts.Contains(part);
			string[] keys = reader ? ["mokadocs-layout-" + option.Attribute!["data-".Length..]] : [];

			option.ReaderStorageKeys.Should().Equal(keys);
			option.LiveWithLayoutSelector.Should().Be(reader);
		}

		ThemeLabAssets.Script().Should().Contain(
			"const everyVariant = option.liveWithLayoutSelector && !!document.querySelector('.appearance-layout');");
	}

	[Theory]
	[InlineData("navbar", "tabs", true)]
	[InlineData("navbar", "Autohide", false)]
	[InlineData("navbar", "compact", false)]
	[InlineData("width", "boxed", false)]
	[InlineData("toc", "floating", false)]
	[InlineData("home", "docs", true)]
	[InlineData("home", "Portal", true)]
	[InlineData("features", "bento", false)]
	public void NeedsRebuild_ComesFromOneList(string part, string variant, bool rebuild)
	{
		ThemeLayoutPart layoutPart = ThemeLayouts.Parts.Single(p => p.Key == part);

		ThemeLabCatalog.NeedsRebuild(layoutPart, variant).Should().Be(rebuild);
		Option("layout." + part).FindChoice(variant)!.Rebuild.Should().Be(rebuild);
	}
}
