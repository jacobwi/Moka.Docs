using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     The theme lab's catalog and save against a real configuration file. A save checks every value,
///     edits the file as text and writes it only when it loads to the intended values.
/// </summary>
public sealed class ThemeLabExtensionTests : IDisposable
{
	private const string _commented = "# Docs for the lab\r\nsite:\r\n  title: Docs   # shown in the header\r\n\r\n" +
	                                  "theme:\r\n  options:\r\n    style: moka # dense\r\n    socialLinks:\r\n      - icon: github\r\n" +
	                                  "        url: https://github.com/x\r\n# build settings\r\nbuild:\r\n  output: _site\r\n";

	private static readonly JsonSerializerOptions _web = new(JsonSerializerDefaults.Web);

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-lab-" + Guid.NewGuid().ToString("N"));

	public ThemeLabExtensionTests() => Directory.CreateDirectory(_root);

	private string ConfigPath => Path.Combine(_root, "mokadocs.yaml");

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	private ThemeLabExtension Lab(Func<string, string?>? environment = null, string? path = null) =>
		new(path ?? ConfigPath, null, null, environment ?? (_ => null));

	private static ThemeLabSaveRequest Request(string values, string? version = null) => new()
	{
		Version = version,
		Values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values)
	};

	private static JsonElement ToJson(object body) => JsonSerializer.SerializeToElement(body, _web);

	private static JsonElement OptionIn(JsonElement catalog, string key) =>
		catalog.GetProperty("options").EnumerateArray().Single(o => o.GetProperty("key").GetString() == key);

	private (int Status, JsonElement Body) Save(string values, string? version = null, ThemeLabExtension? lab = null)
	{
		(int status, object body) = (lab ?? Lab()).Save(Request(values, version));
		return (status, ToJson(body));
	}

	private JsonElement Catalog(ThemeLabExtension? lab = null)
	{
		(int status, object body) = (lab ?? Lab()).Catalog();
		status.Should().Be(StatusCodes.Status200OK);
		return ToJson(body);
	}

	[Fact]
	public void Catalog_HasTheValuesAsWrittenInTheFile()
	{
		File.WriteAllText(ConfigPath,
			"site:\n  title: Docs\ntheme:\n  options:\n    style: Moka\n    sidebarToggle: true\n    layout:\n      navbar: tabs\n    banner:\n      text: Hi\n");

		JsonElement catalog = Catalog();

		catalog.GetProperty("configFile").GetString().Should().Be("mokadocs.yaml");
		catalog.GetProperty("version").GetString().Should().MatchRegex("^[0-9a-f]{16}$");
		catalog.GetProperty("readOnly").ValueKind.Should().Be(JsonValueKind.Null);
		catalog.GetProperty("customTheme").GetBoolean().Should().BeFalse();
		OptionIn(catalog, "style").GetProperty("value").GetString().Should().Be("Moka");
		OptionIn(catalog, "sidebarToggle").GetProperty("value").GetString().Should().Be("true");
		OptionIn(catalog, "layout.navbar").GetProperty("value").GetString().Should().Be("tabs");
		OptionIn(catalog, "banner.text").GetProperty("value").GetString().Should().Be("Hi");
		OptionIn(catalog, "codeTheme").GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
		OptionIn(catalog, "style").GetProperty("attribute").GetString().Should().Be("data-style");
		OptionIn(catalog, "layout.navbar").GetProperty("choices").EnumerateArray()
			.Where(c => c.GetProperty("rebuild").GetBoolean()).Select(c => c.GetProperty("value").GetString())
			.Should().Equal("tabs");
		OptionIn(catalog, "layout.navbar").GetProperty("readerStorageKeys").EnumerateArray().Select(k => k.GetString())
			.Should().Equal("mokadocs-layout-navbar");
		OptionIn(catalog, "layout.navbar").GetProperty("liveWithLayoutSelector").GetBoolean().Should().BeTrue();
		OptionIn(catalog, "layout.hero").GetProperty("liveWithLayoutSelector").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void Catalog_SaysWhichEnvironmentVariablesWin()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\n");

		JsonElement catalog = Catalog(Lab(name => name == "MOKADOCS_LAYOUT_PAGE_NAV" ? "stacked" : name == "MOKADOCS_STYLE" ? "" : null));

		JsonElement pageNav = OptionIn(catalog, "layout.pageNav").GetProperty("environment");
		pageNav.GetProperty("name").GetString().Should().Be("MOKADOCS_LAYOUT_PAGE_NAV");
		pageNav.GetProperty("value").GetString().Should().Be("stacked");
		OptionIn(catalog, "style").GetProperty("environment").ValueKind.Should().Be(JsonValueKind.Null, "an empty variable doesn't count");
	}

	[Theory]
	[InlineData("site:\n  title: Docs\ntheme: { options: { style: moka } }\n", "can't be edited safely: theme is written as a flow mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options: &o\n    style: moka\n", "can't be edited safely: theme.options uses an anchor or an alias")]
	[InlineData("site:\n  description: no title\n", "doesn't load")]
	public void Catalog_TurnsSaveOff_ForAFileItCantSave(string yaml, string reason)
	{
		File.WriteAllText(ConfigPath, yaml);

		Catalog().GetProperty("readOnly").GetString().Should().Contain(reason);
	}

	[Fact]
	public void Catalog_NotesACustomThemeAndPrimaryColor()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\ntheme:\n  name: my-theme\n  options:\n    primaryColor: \"#123456\"\n");

		JsonElement catalog = Catalog();

		catalog.GetProperty("customTheme").GetBoolean().Should().BeTrue();
		catalog.GetProperty("themeName").GetString().Should().Be("my-theme");
		catalog.GetProperty("customPrimaryColor").GetBoolean().Should().BeTrue();
	}

	[Fact]
	public void Save_EditsOnlyTheChangedKeys_KeepingCommentsAndLineEndings()
	{
		File.WriteAllText(ConfigPath, _commented);

		(int status, JsonElement body) = Save("""
			{
				"style": "paper",
				"layout.navbar": "tabs",
				"sidebarToggle": true,
				"banner.text": "Version 2: out now"
			}
			""");

		status.Should().Be(StatusCodes.Status200OK);
		body.GetProperty("saved").GetBoolean().Should().BeTrue();
		body.GetProperty("changed").EnumerateArray().Select(e => e.GetString())
			.Should().Equal("style", "layout.navbar", "sidebarToggle", "banner.text");
		string written = File.ReadAllText(ConfigPath);
		written.Should().Be(_commented
			.Replace("style: moka # dense", "style: paper # dense")
			.Replace("        url: https://github.com/x\r\n",
				"        url: https://github.com/x\r\n    layout:\r\n      navbar: tabs\r\n    sidebarToggle: true\r\n" +
				"    banner:\r\n      text: \"Version 2: out now\"\r\n"));
		body.GetProperty("version").GetString().Should().Be(Catalog().GetProperty("version").GetString());
		ThemeOptions options = SiteConfigReader.Parse(written).Theme.Options;
		options.Style.Should().Be("paper");
		options.Layout.Navbar.Should().Be("tabs");
		options.SidebarToggle.Should().BeTrue();
		options.Banner!.Text.Should().Be("Version 2: out now");
		Directory.GetFiles(_root).Should().Equal(ConfigPath);
	}

	[Fact]
	public void Save_BackToTheDefaults_RestoresTheFileByteForByte()
	{
		File.WriteAllText(ConfigPath, _commented);
		Save("""{ "preset": "book", "layout.toc": "floating", "codeTheme": "nord", "banner.variant": "accent" }""");

		(int status, JsonElement body) = Save("""{ "preset": "", "layout.toc": null, "codeTheme": null, "banner.variant": null }""");

		status.Should().Be(StatusCodes.Status200OK);
		body.GetProperty("saved").GetBoolean().Should().BeTrue();
		File.ReadAllText(ConfigPath).Should().Be(_commented);
	}

	[Fact]
	public void Save_ValuesTheFileAlreadyHas_WritesNothing()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\ntheme:\n  options:\n    style: Moka\n    sidebarToggle: True\n");
		DateTime before = File.GetLastWriteTimeUtc(ConfigPath);

		(int status, JsonElement body) = Save("""{ "style": "moka", "sidebarToggle": true, "layout.toc": null }""");

		status.Should().Be(StatusCodes.Status200OK);
		body.GetProperty("saved").GetBoolean().Should().BeFalse();
		body.GetProperty("message").GetString().Should().Be("mokadocs.yaml already has these values.");
		File.GetLastWriteTimeUtc(ConfigPath).Should().Be(before);
	}

	[Fact]
	public void Save_AfterTheFileChanged_IsAConflict()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\n");
		string version = Catalog().GetProperty("version").GetString()!;
		File.WriteAllText(ConfigPath, "site:\n  title: Docs edited by hand\n");

		(int status, JsonElement body) = Save("""{ "style": "moka" }""", version);

		status.Should().Be(StatusCodes.Status409Conflict);
		body.GetProperty("error").GetString().Should().Contain("changed since this page loaded it");
		File.ReadAllText(ConfigPath).Should().Be("site:\n  title: Docs edited by hand\n");
	}

	[Theory]
	[InlineData("""{ "style": "fancy" }""", "'fancy' isn't a style name")]
	[InlineData("""{ "theme.name": "custom" }""", "isn't an option the theme lab can save")]
	[InlineData("""{ "banner.text": "a\u0007b" }""", "control characters")]
	public void Save_ValuesOutsideTheCatalog_AreRefused(string values, string error)
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\n");

		(int status, JsonElement body) = Save(values);

		status.Should().Be(StatusCodes.Status400BadRequest);
		body.GetProperty("error").GetString().Should().Contain(error);
		File.ReadAllText(ConfigPath).Should().Be("site:\n  title: Docs\n");
	}

	[Fact]
	public void Save_WithoutValues_IsABadRequest()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\n");

		(int status, object _) = Lab().Save(new ThemeLabSaveRequest());

		status.Should().Be(StatusCodes.Status400BadRequest);
	}

	[Theory]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    layout: { navbar: tabs }\n", "theme.options.layout is written as a flow mapping")]
	[InlineData("site:\n  title: Docs\ntheme:\n  options:\n    style: &s moka\n", "theme.options.style uses an anchor")]
	[InlineData("site:\n  title: Docs\n---\nsite:\n  title: Two\n", "2 YAML documents")]
	public void Save_FileItCantEditSafely_IsRefusedAndLeftAlone(string yaml, string reason)
	{
		File.WriteAllText(ConfigPath, yaml);

		(int status, JsonElement body) = Save("""{ "style": "paper", "layout.navbar": "compact" }""");

		status.Should().Be(StatusCodes.Status422UnprocessableEntity);
		body.GetProperty("error").GetString().Should().Contain(reason).And.Contain("Copy the YAML");
		body.GetProperty("refused").GetBoolean().Should().BeTrue();
		File.ReadAllText(ConfigPath).Should().Be(yaml);
	}

	[Fact]
	public void Save_FileThatDoesntLoad_IsRefused()
	{
		File.WriteAllText(ConfigPath, "site:\n  description: no title\n");

		(int status, JsonElement body) = Save("""{ "style": "paper" }""");

		status.Should().Be(StatusCodes.Status422UnprocessableEntity);
		body.GetProperty("error").GetString().Should().Contain("doesn't load");
	}

	[Fact]
	public void Save_KeepsAByteOrderMark()
	{
		File.WriteAllBytes(ConfigPath, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("site:\n  title: Docs\n")]);

		Save("""{ "style": "paper" }""").Status.Should().Be(StatusCodes.Status200OK);

		File.ReadAllBytes(ConfigPath).Should().Equal(
			[0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("site:\n  title: Docs\ntheme:\n  options:\n    style: paper\n")]);
	}

	[Fact]
	public void Save_Utf16File_IsLeftAlone()
	{
		File.WriteAllText(ConfigPath, "site:\n  title: Docs\n", Encoding.Unicode);

		(int status, JsonElement body) = Save("""{ "style": "paper" }""");

		status.Should().Be(StatusCodes.Status500InternalServerError);
		body.GetProperty("error").GetString().Should().Contain("UTF-16");
	}

	[Fact]
	public void Save_EditsTheFileThatConfigNamed()
	{
		string other = Path.Combine(_root, "docs.dev.yml");
		File.WriteAllText(other, "site:\n  title: Docs\n");
		ThemeLabExtension lab = Lab(path: other);

		Catalog(lab).GetProperty("configFile").GetString().Should().Be("docs.dev.yml");
		(int status, JsonElement _) = Save("""{ "layout.width": "wide" }""", lab: lab);

		status.Should().Be(StatusCodes.Status200OK);
		File.ReadAllText(other).Should().EndWith("    layout:\n      width: wide\n");
		File.Exists(ConfigPath).Should().BeFalse();
	}

	[Fact]
	public void PageHtml_LoadsTheScriptFromTheApi()
	{
		Lab().PageHtml.Should().Be("<script src=\"/api/theme-lab/lab.js\" defer></script>");
	}
}
