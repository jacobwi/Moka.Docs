using FluentAssertions;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Tests.Configuration;

/// <summary>
///     The text edits the theme lab makes to mokadocs.yaml. Everything outside the changed keys has to
///     stay byte for byte as the author wrote it, comments included.
/// </summary>
public sealed class ConfigYamlEditorTests
{
	private const string _style = "theme.options.style";
	private const string _navbar = "theme.options.layout.navbar";

	private static string Edit(string yaml, params ConfigYamlChange[] changes)
	{
		ConfigYamlEdit edit = ConfigYamlEditor.Apply(yaml, changes);
		edit.Refusal.Should().BeNull();
		return edit.Yaml!;
	}

	private static string Refusal(string yaml, params ConfigYamlChange[] changes)
	{
		ConfigYamlEdit edit = ConfigYamlEditor.Apply(yaml, changes);
		edit.Yaml.Should().BeNull();
		return edit.Refusal!;
	}

	private static ConfigYamlChange Set(string path, string value) => new(path, value);

	private static ConfigYamlChange Remove(string path) => new(path, null);

	// Raw literals take this file's line endings; the tests that care about them say so.
	private static string Lf(string text) => text.ReplaceLineEndings("\n");

	[Fact]
	public void ExistingValue_IsReplacedInPlace_KeepingCommentsAndEverythingElse()
	{
		string yaml = Lf("""
		                 # Site settings
		                 site:
		                   title: Docs # the name

		                 theme:
		                   options:
		                     style: moka   # dense
		                     codeTheme: nord
		                 # build settings follow
		                 build:
		                   output: _site

		                 """);

		Edit(yaml, Set(_style, "paper")).Should().Be(yaml.Replace("style: moka   # dense", "style: paper   # dense"));
	}

	[Theory]
	[InlineData("style: \"moka\"", "style: \"paper\"")]
	[InlineData("style: 'moka'", "style: 'paper'")]
	[InlineData("style: moka", "style: paper")]
	[InlineData("style:", "style: paper")]
	[InlineData("style: ~", "style: paper")]
	[InlineData("style:    # pick one", "style: paper    # pick one")]
	public void ExistingValue_KeepsItsQuotingStyle(string before, string after)
	{
		string yaml = $"site:\n  title: Docs\ntheme:\n  options:\n    {before}\n";

		Edit(yaml, Set(_style, "paper")).Should().Be($"site:\n  title: Docs\ntheme:\n  options:\n    {after}\n");
	}

	[Fact]
	public void SameValue_LeavesTheFileAsItIs()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    style: \"paper\"\n    sidebarToggle: True\n";

		Edit(yaml, Set(_style, "paper"), new ConfigYamlChange("theme.options.sidebarToggle", "true", true)).Should().Be(yaml);
	}

	[Fact]
	public void MissingKey_GoesAtTheEndOfItsMapping_AfterANestedBlock()
	{
		string yaml = Lf("""
		                 site:
		                   title: Docs
		                 theme:
		                   options:
		                     style: moka
		                     layout:
		                       navbar: tabs # sections
		                   # name: custom
		                 build:
		                   output: _site

		                 """);

		Edit(yaml, Set("theme.options.codeTheme", "nord"), Set("theme.options.layout.footer", "columns")).Should().Be(Lf("""
			site:
			  title: Docs
			theme:
			  options:
			    style: moka
			    layout:
			      navbar: tabs # sections
			      footer: columns
			    codeTheme: nord
			  # name: custom
			build:
			  output: _site

			"""));
	}

	[Fact]
	public void MissingTheme_IsAddedAfterTheLastKey_WithEveryParent()
	{
		const string yaml = "site:\n  title: Docs\nbuild:\n  output: _site\n# plugins:\n#   - name: mokadocs-repl\n";

		Edit(yaml, Set(_style, "moka"), Set(_navbar, "tabs"), Set("theme.options.banner.text", "Hello")).Should().Be(
			"site:\n  title: Docs\nbuild:\n  output: _site\n" +
			"theme:\n  options:\n    style: moka\n    layout:\n      navbar: tabs\n    banner:\n      text: Hello\n" +
			"# plugins:\n#   - name: mokadocs-repl\n");
	}

	[Fact]
	public void NewBlocks_UseTheFilesIndentationStep()
	{
		const string yaml = "site:\n    title: Docs\ntheme:\n    name: default\n";

		Edit(yaml, Set(_navbar, "tabs")).Should().Be(
			"site:\n    title: Docs\ntheme:\n    name: default\n    options:\n        layout:\n            navbar: tabs\n");
	}

	[Theory]
	[InlineData("    layout:\n", "    layout:\n")]
	[InlineData("    layout: ~\n", "    layout:\n")]
	[InlineData("    layout: null # nothing yet\n", "    layout: # nothing yet\n")]
	public void EmptyParent_GetsTheNewKeyBelowIt(string before, string after)
	{
		string yaml = "site:\n  title: Docs\ntheme:\n  options:\n" + before + "    style: moka\n";

		string edited = Edit(yaml, Set(_navbar, "tabs"));

		edited.Should().Be("site:\n  title: Docs\ntheme:\n  options:\n" + after + "      navbar: tabs\n    style: moka\n");
		SiteConfigReader.Parse(edited).Theme.Options.Layout.Navbar.Should().Be("tabs");
	}

	[Fact]
	public void RemovedKey_TakesItsLineAndComment()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    style: moka # dense\n    codeTheme: nord\n";

		Edit(yaml, Remove(_style)).Should().Be("site:\n  title: Docs\ntheme:\n  options:\n    codeTheme: nord\n");
	}

	[Fact]
	public void RemovingTheLastKey_RemovesTheParentsLeftEmpty()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      navbar: tabs\nbuild:\n  output: _site\n";

		Edit(yaml, Remove(_navbar)).Should().Be("site:\n  title: Docs\nbuild:\n  output: _site\n");
	}

	[Fact]
	public void RemovingTheLastKey_KeepsAParentThatHoldsComments()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      # tabs suit this site\n      navbar: tabs\n";

		string edited = Edit(yaml, Remove(_navbar));

		edited.Should().Be("site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      # tabs suit this site\n");
		SiteConfigReader.Parse(edited).Theme.Options.Layout.Navbar.Should().Be("default");
	}

	[Fact]
	public void RemovingAMissingKey_ChangesNothing()
	{
		const string yaml = "site:\n  title: Docs\n";

		Edit(yaml, Remove(_style), Remove(_navbar)).Should().Be(yaml);
	}

	[Fact]
	public void SetAndRemove_InOneEdit()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n    layout:\n      navbar: tabs\n";

		Edit(yaml, Remove(_style), Set(_navbar, "compact"), Set("theme.options.layout.toc", "inline")).Should().Be(
			"site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      navbar: compact\n      toc: inline\n");
	}

	[Fact]
	public void RemovingTheLastEntry_AndAddingToTheMappingBeforeIt()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      navbar: tabs\n    style: moka\n";

		Edit(yaml, Remove(_style), Set("theme.options.layout.footer", "none")).Should().Be(
			"site:\n  title: Docs\ntheme:\n  options:\n    layout:\n      navbar: tabs\n      footer: none\n");
	}

	[Fact]
	public void CrlfFile_StaysCrlf()
	{
		const string yaml = "site:\r\n  title: Docs\r\ntheme:\r\n  options:\r\n    style: moka\r\n";

		string edited = Edit(yaml, Set(_navbar, "tabs"), Set("theme.options.codeTheme", "nord"));

		edited.Should().Be("site:\r\n  title: Docs\r\ntheme:\r\n  options:\r\n    style: moka\r\n    layout:\r\n      navbar: tabs\r\n    codeTheme: nord\r\n");
		edited.Replace("\r\n", "").Should().NotContain("\n").And.NotContain("\r");
	}

	[Fact]
	public void LfFile_StaysLf()
	{
		string edited = Edit("site:\n  title: Docs\n", Set(_navbar, "tabs"));

		edited.Should().NotContain("\r");
	}

	[Fact]
	public void FileWithoutAFinalLineBreak_KeepsItThatWay()
	{
		Edit("site:\n  title: Docs", Set(_style, "moka")).Should().Be("site:\n  title: Docs\ntheme:\n  options:\n    style: moka");
		Edit("site:\r\n  title: Docs", Set(_style, "moka")).Should().Be("site:\r\n  title: Docs\r\ntheme:\r\n  options:\r\n    style: moka");
	}

	[Fact]
	public void DocumentMarkers_StayAroundTheContent()
	{
		Edit("---\nsite:\n  title: Docs\n...\n", Set(_style, "moka"))
			.Should().Be("---\nsite:\n  title: Docs\ntheme:\n  options:\n    style: moka\n...\n");
	}

	[Theory]
	[InlineData("Version 2.0 is out", "Version 2.0 is out")]
	[InlineData("/blog/2-0", "/blog/2-0")]
	[InlineData("https://example.com/a?b=c#d", "https://example.com/a?b=c#d")]
	[InlineData("say \"hi\" \\ bye", "say \"hi\" \\ bye")]
	[InlineData("Read: the notes", "\"Read: the notes\"")]
	[InlineData("#1 release", "\"#1 release\"")]
	[InlineData("Big news #1", "\"Big news #1\"")]
	[InlineData("yes", "\"yes\"")]
	[InlineData("No", "\"No\"")]
	[InlineData("null", "\"null\"")]
	[InlineData("2.0", "\"2.0\"")]
	[InlineData("0x1F", "\"0x1F\"")]
	[InlineData("- item", "\"- item\"")]
	[InlineData("[draft]", "\"[draft]\"")]
	[InlineData("ends with:", "\"ends with:\"")]
	[InlineData(" padded ", "\" padded \"")]
	[InlineData("\"hi\": \\ bye", "\"\\\"hi\\\": \\\\ bye\"")]
	[InlineData("tab\there", "\"tab\\there\"")]
	[InlineData("Grüße, 日本語 ✓", "Grüße, 日本語 ✓")]
	public void NewText_IsQuotedOnlyWhenPlainWouldReadDifferently(string value, string written)
	{
		const string path = "theme.options.banner.text";

		string edited = Edit("site:\n  title: Docs\n", Set(path, value));

		edited.Should().EndWith($"    banner:\n      text: {written}\n");
		ConfigYamlEditor.ReadValues(edited, [path])[path].Should().Be(value);
	}

	[Fact]
	public void SingleQuotedValue_StaysSingleQuoted_WithQuotesDoubled()
	{
		const string path = "theme.options.banner.text";
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    banner:\n      text: 'Old'\n";

		string edited = Edit(yaml, Set(path, "It's here"));

		edited.Should().Contain("text: 'It''s here'\n");
		ConfigYamlEditor.ReadValues(edited, [path])[path].Should().Be("It's here");
	}

	[Fact]
	public void Booleans_AreWrittenPlain()
	{
		Edit("site:\n  title: Docs\ntheme:\n  options:\n    sidebarToggle: \"false\"\n",
				new ConfigYamlChange("theme.options.sidebarToggle", "true", true))
			.Should().Be("site:\n  title: Docs\ntheme:\n  options:\n    sidebarToggle: true\n");
	}

	[Fact]
	public void FlowCollectionsAndAnchorsElsewhere_AreLeftAlone()
	{
		string yaml = Lf("""
		                 site:
		                   title: Docs
		                 nav: [{ label: Home, path: / }]
		                 plugins:
		                   - name: mokadocs-repl
		                     options: &repl { packages: [Humanizer] }
		                 theme:
		                   options:
		                     socialLinks: [{ icon: github, url: "https://github.com/x" }]
		                     primaryColor: &brand "#123456"
		                     accentColor: *brand
		                 build: { output: _site }

		                 """);

		string edited = Edit(yaml, Set(_style, "paper"));

		edited.Should().Be(yaml.Replace("    accentColor: *brand\n", "    accentColor: *brand\n    style: paper\n"));
	}

	[Fact]
	public void EditedFile_ParsesToTheIntendedConfiguration()
	{
		const string yaml = "# My docs\nsite:\n  title: Docs\ntheme:\n  options:\n    preset: book\n    layout:\n      toc: inline\n";

		string edited = Edit(yaml,
			Set(_style, "moka"),
			Remove("theme.options.layout.toc"),
			Set(_navbar, "tabs"),
			new ConfigYamlChange("theme.options.sidebarToggle", "true", true),
			Set("theme.options.banner.text", "Version 2: out now"),
			Set("theme.options.banner.variant", "accent"));

		edited.Should().Be("# My docs\nsite:\n  title: Docs\ntheme:\n  options:\n    preset: book\n    layout:\n      navbar: tabs\n" +
		                   "    style: moka\n    sidebarToggle: true\n    banner:\n      text: \"Version 2: out now\"\n      variant: accent\n");
		ThemeOptions options = SiteConfigReader.Parse(edited).Theme.Options;
		options.Preset.Should().Be("book");
		options.Style.Should().Be("moka");
		options.Layout.Navbar.Should().Be("tabs");
		options.Layout.Toc.Should().Be("inline", "the book preset fills it in again once the yaml leaves it out");
		options.SidebarToggle.Should().BeTrue();
		options.Banner!.Text.Should().Be("Version 2: out now");
		options.Banner.Variant.Should().Be("accent");
	}

	[Theory]
	[InlineData("site:\n  title: Docs\ntheme: { options: { style: moka } }\n", "theme is written as a flow mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options: { style: moka }\n", "theme.options is written as a flow mapping")]
	[InlineData("{ site: { title: Docs } }\n", "flow mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options: &opts\n    style: moka\n", "theme.options uses an anchor or an alias")]
	[InlineData("base: &base\n  options:\n    style: moka\nsite:\n  title: Docs\ntheme: *base\n", "theme uses an anchor or an alias")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style: &s moka\n", "theme.options.style uses an anchor")]
	[InlineData("defaults: &d\n  title: Docs\n<<: *d\nsite:\n  title: Docs\n", "merge key")]
	[InlineData("site:\n  title: Docs\n---\nsite:\n  title: Other\n", "2 YAML documents")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    codeTheme: \"a\tb\"\n", "tab character")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style: |\n      moka\n", "block scalar")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n    style: paper\n", "theme.options.style appears more than once")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style:\n      - moka\n", "holds a list or a mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options: plain\n", "theme.options isn't a mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style: [moka\n", "doesn't parse")]
	[InlineData("", "empty")]
	[InlineData("- just\n- a list\n", "mapping at the top level")]
	[InlineData("site:\r  title: Docs\r", "carriage returns")]
	public void UnsafeFiles_AreRefused(string yaml, string reason)
	{
		Refusal(yaml, Set(_style, "paper")).Should().Contain(reason);
		ConfigYamlEditor.FindRefusal(yaml, [_style]).Should().Contain(reason);
	}

	[Fact]
	public void TabsOutsideTheEditedSection_AreFine()
	{
		const string yaml = "site:\n  title: Docs\n  description: \"a\tb\"\ntheme:\n  options:\n    style: moka\n";

		Edit(yaml, Set(_style, "paper")).Should().Be(yaml.Replace("moka", "paper"));
	}

	[Fact]
	public void FindRefusal_IsNullForAnEditableFile()
	{
		ConfigYamlEditor.FindRefusal("site:\n  title: Docs\ntheme:\n  options:\n    style: moka\n", [_style, _navbar]).Should().BeNull();
	}

	[Fact]
	public void ReadValues_ReturnsWhatIsWritten()
	{
		const string yaml = "site:\n  title: Docs\ntheme:\n  options:\n    style: 'Moka'\n    codeTheme:\n    codeStyle: ~\n    preset: \"\"\n    layout:\n      navbar: tabs\n";

		IReadOnlyDictionary<string, string?> values = ConfigYamlEditor.ReadValues(yaml,
			[_style, "theme.options.codeTheme", "theme.options.codeStyle", "theme.options.preset", _navbar, "theme.options.layout.toc", "theme.options.layout"]);

		values.Should().BeEquivalentTo(new Dictionary<string, string?>
		{
			[_style] = "Moka",
			["theme.options.codeTheme"] = null,
			["theme.options.codeStyle"] = null,
			["theme.options.preset"] = "",
			[_navbar] = "tabs",
			["theme.options.layout.toc"] = null,
			["theme.options.layout"] = null
		});
	}

	[Fact]
	public void ReadValues_OfAFileThatDoesntParse_AreNull()
	{
		ConfigYamlEditor.ReadValues("site: [", [_style])[_style].Should().BeNull();
	}

	[Fact]
	public void OverlappingChanges_AreAProgrammingError()
	{
		Action overlap = () => ConfigYamlEditor.Apply("site:\n  title: Docs\n",
			[Set("theme.options.layout", "x"), Set(_navbar, "tabs")]);

		overlap.Should().Throw<ArgumentException>();
	}
}
