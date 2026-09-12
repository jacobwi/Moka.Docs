using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Themes;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

public sealed class ThemeResolverTests
{
	private const string RootDir = "/mokadocs-test";

	private static ThemeResolver CreateResolver(MockFileSystem fs) =>
		new(new ThemeLoader(fs, NullLogger<ThemeLoader>.Instance), NullLogger<ThemeResolver>.Instance);

	private static SiteConfig ConfigWithTheme(string themeName) =>
		new()
		{
			Site = new SiteMetadata { Title = "Test" },
			Theme = new ThemeConfig { Name = themeName }
		};

	[Theory]
	[InlineData("default")]
	[InlineData("DEFAULT")]
	[InlineData("")]
	[InlineData("   ")]
	public void Resolve_DefaultName_UsesEmbeddedTheme(string themeName)
	{
		var fs = new MockFileSystem();

		ResolvedTheme theme = CreateResolver(fs).Resolve(ConfigWithTheme(themeName), RootDir, fs);

		theme.IsEmbedded.Should().BeTrue();
		theme.ThemeDirectory.Should().BeNull();
		theme.Context.Templates.Should().ContainKey("default");
	}

	[Fact]
	public void Resolve_ThemeDirectoryWithLayouts_UsesThatTheme()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/mokadocs-test/mytheme/layouts/default.html", new MockFileData("<html>{{ page.content }}</html>") },
			{ "/mokadocs-test/mytheme/css/main.css", new MockFileData("body{}") }
		});

		ResolvedTheme theme = CreateResolver(fs).Resolve(ConfigWithTheme("./mytheme"), RootDir, fs);

		theme.IsEmbedded.Should().BeFalse();
		theme.ThemeDirectory.Should().NotBeNull();
		theme.Context.Templates.Should().ContainKey("default");
		theme.Context.Templates["default"].Should().Contain("{{ page.content }}");
		theme.Context.CssFiles.Should().ContainSingle().Which.Should().Be("/_theme/css/main.css");
	}

	[Fact]
	public void Resolve_MissingThemeDirectory_FallsBackToEmbedded()
	{
		var fs = new MockFileSystem();

		ResolvedTheme theme = CreateResolver(fs).Resolve(ConfigWithTheme("./nope"), RootDir, fs);

		theme.IsEmbedded.Should().BeTrue();
		theme.Context.Templates.Should().ContainKey("default");
	}

	[Fact]
	public void Resolve_ThemeDirectoryWithoutLayouts_FallsBackToEmbedded()
	{
		// A theme with css but no layouts would otherwise throw "layout not found" on
		// every single page rather than degrading to something renderable.
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/mokadocs-test/emptytheme/css/main.css", new MockFileData("body{}") }
		});

		ResolvedTheme theme = CreateResolver(fs).Resolve(ConfigWithTheme("./emptytheme"), RootDir, fs);

		theme.IsEmbedded.Should().BeTrue();
	}

	[Fact]
	public void Resolve_SameConfigTwice_ReturnsCachedInstance()
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "/mokadocs-test/mytheme/layouts/default.html", new MockFileData("<html></html>") }
		});
		ThemeResolver resolver = CreateResolver(fs);
		SiteConfig config = ConfigWithTheme("./mytheme");

		ResolvedTheme first = resolver.Resolve(config, RootDir, fs);
		ResolvedTheme second = resolver.Resolve(config, RootDir, fs);

		// RenderPhase and ThemeAssetPhase both resolve; a custom theme should only be
		// read off disk once per build.
		second.Should().BeSameAs(first);
	}
}
