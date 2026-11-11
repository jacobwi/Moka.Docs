using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Versioning;

namespace Moka.Docs.Integration.Tests.Versioning;

/// <summary>
///     The version label was typed by hand in mokadocs.yaml and drifted from the package version for
///     six releases. A version without a label now takes <c>v{major}.{minor}</c> of the documented
///     package's version.
/// </summary>
public sealed class VersionLabelTests
{
	private const string _project = "/repo/src/Lib/Lib.csproj";

	private static string Props(string properties) => $"<Project><PropertyGroup>{properties}</PropertyGroup></Project>";

	private static string Csproj(string properties = "", string sdk = "Microsoft.NET.Sdk") =>
		$"<Project Sdk=\"{sdk}\"><PropertyGroup><TargetFramework>net9.0</TargetFramework>{properties}</PropertyGroup></Project>";

	private static SiteConfig Config(List<VersionDefinition>? versions = null, params string[] projects) => new()
	{
		Site = new SiteMetadata { Title = "Lib" },
		Content = new ContentConfig
		{
			Docs = "./docs",
			Projects = projects.Select(p => new ProjectSource { Path = p }).ToList()
		},
		Features = new FeaturesConfig
		{
			Versioning = new VersioningFeatureConfig { Enabled = true, Versions = versions ?? [] }
		}
	};

	private static BuildContext Apply(SiteConfig config, Dictionary<string, MockFileData> files)
	{
		var context = new BuildContext
		{
			Config = config,
			FileSystem = new MockFileSystem(files),
			RootDirectory = "/repo",
			OutputDirectory = "/repo/_site"
		};
		new VersionManager(config, NullLogger<VersionManager>.Instance).ApplyTo(context);
		return context;
	}

	[Fact]
	public void NoVersionsListed_LabelComesFromTheDirectoryBuildPropsChain()
	{
		BuildContext context = Apply(Config(null, "./src/Lib/Lib.csproj"), new()
		{
			["/repo/Directory.Build.props"] = new(Props("<Version>1.8.3</Version><Authors>x</Authors>")),
			["/repo/src/Directory.Build.props"] = new(Props("<Nullable>enable</Nullable>")),
			[_project] = new(Csproj())
		});

		context.Versions.Should().ContainSingle();
		DocVersion version = context.Versions[0];
		version.Label.Should().Be("v1.8");
		version.Slug.Should().Be("v1.8");
		version.Number.Should().Be("1.8.3");
		version.IsDefault.Should().BeTrue();
		version.IsPrerelease.Should().BeFalse();
		context.CurrentVersion.Should().Be(version);
	}

	[Fact]
	public void ProjectVersion_WinsOverDirectoryBuildProps_AndASuffixMarksAPrerelease()
	{
		BuildContext context = Apply(Config(null, "./src/Lib/Lib.csproj"), new()
		{
			["/repo/Directory.Build.props"] = new(Props("<Version>1.0.0</Version>")),
			[_project] = new(Csproj("<VersionPrefix>2.1.0</VersionPrefix><VersionSuffix>preview.1</VersionSuffix>"))
		});

		context.CurrentVersion!.Label.Should().Be("v2.1");
		context.CurrentVersion.Number.Should().Be("2.1.0");
		context.CurrentVersion.IsPrerelease.Should().BeTrue();
	}

	[Fact]
	public void FirstPackableProject_IsTheOneThatCounts()
	{
		BuildContext context = Apply(Config(null, "./tests/Lib.Tests/Lib.Tests.csproj", "./samples/App/App.csproj",
			"./src/Lib/Lib.csproj"), new()
		{
			["/repo/tests/Lib.Tests/Lib.Tests.csproj"] = new(Csproj("<IsTestProject>true</IsTestProject><Version>9.9.9</Version>")),
			["/repo/samples/App/App.csproj"] = new(Csproj("<Version>7.0.0</Version>", "Microsoft.NET.Sdk.Web")),
			[_project] = new(Csproj("<Version>3.4.0</Version>"))
		});

		context.CurrentVersion!.Label.Should().Be("v3.4");
	}

	[Fact]
	public void VersionBuiltFromOtherProperties_IsSkippedForTheNextOneUp()
	{
		BuildContext context = Apply(Config(null, "./src/Lib/Lib.csproj"), new()
		{
			["/repo/Directory.Build.props"] = new(Props("<Version>4.2.0</Version>")),
			[_project] = new(Csproj("<Version>$(Major).1</Version>"))
		});

		context.CurrentVersion!.Label.Should().Be("v4.2");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NoVersionFound_FallsBackToLatest(bool withProject)
	{
		var files = new Dictionary<string, MockFileData> { ["/repo/docs/index.md"] = new("# Home") };
		if (withProject)
		{
			files[_project] = new MockFileData(Csproj());
		}

		BuildContext context = withProject
			? Apply(Config(null, "./src/Lib/Lib.csproj"), files)
			: Apply(Config(), files);

		context.CurrentVersion!.Label.Should().Be("latest");
		context.CurrentVersion.Number.Should().BeNull();
		context.Diagnostics.All.Should().BeEmpty();
	}

	[Fact]
	public void ListedLabels_Win_AndAnEntryWithoutOneIsDerived()
	{
		BuildContext context = Apply(Config(
		[
			new VersionDefinition { Label = "Stable", Default = true },
			new VersionDefinition { Label = "" },
			new VersionDefinition { Label = "v1.0" }
		], "./src/Lib/Lib.csproj"), new()
		{
			[_project] = new(Csproj("<Version>2.3.1</Version>"))
		});

		context.Versions.Select(v => v.Label).Should().Equal("Stable", "v2.3", "v1.0");
		context.Versions.Select(v => v.Number).Should().Equal("2.3.1", "2.3.1", "1.0.0");
		context.CurrentVersion!.Label.Should().Be("Stable");
	}

	[Fact]
	public void DefaultFallsBackToTheFirstStableVersion()
	{
		BuildContext context = Apply(Config(
		[
			new VersionDefinition { Label = "v3.0-beta", Prerelease = true },
			new VersionDefinition { Label = "v2.0" }
		]), []);

		context.Versions.Select(v => v.IsDefault).Should().Equal(false, true);
		context.CurrentVersion!.Label.Should().Be("v2.0");
	}

	[Fact]
	public void UnknownStrategy_IsAWarning()
	{
		SiteConfig config = Config([new VersionDefinition { Label = "v1.0" }]);
		config = config with
		{
			Features = config.Features with
			{
				Versioning = config.Features.Versioning with { Strategy = "folders" }
			}
		};

		BuildContext context = Apply(config, []);

		context.Diagnostics.All.Should().ContainSingle(d => d.Severity == DiagnosticSeverity.Warning)
			.Which.Message.Should().Contain("'folders'");
	}

	[Fact]
	public void VersioningOff_AddsNoVersions()
	{
		SiteConfig config = Config(null, "./src/Lib/Lib.csproj") with { Features = new FeaturesConfig() };

		BuildContext context = Apply(config, new() { [_project] = new(Csproj("<Version>1.0.0</Version>")) });

		context.Versions.Should().BeEmpty();
		context.CurrentVersion.Should().BeNull();
	}

	[Fact]
	public void Tag_IsReadFromTheConfig()
	{
		SiteConfig config = SiteConfigReader.Parse("""
		                                           site:
		                                             title: T
		                                           features:
		                                             versioning:
		                                               enabled: true
		                                               versions:
		                                                 - tag: v1.0.0
		                                                 - branch: main
		                                                   default: true
		                                           """);

		config.Warnings.Should().BeEmpty();
		config.Features.Versioning.Versions[0].Tag.Should().Be("v1.0.0");
		config.Features.Versioning.Versions[0].Label.Should().BeEmpty();
		config.Features.Versioning.Versions[1].Branch.Should().Be("main");
	}

	[Theory]
	[InlineData("https://u.example.com/Lib", "/Lib", "https://u.example.com/Lib/v1.0")]
	[InlineData("https://u.example.com/Lib/", "/Lib", "https://u.example.com/Lib/v1.0")]
	[InlineData("https://u.example.com", "/Lib", "https://u.example.com")]
	[InlineData("https://docs.example.com", "/", "https://docs.example.com")]
	[InlineData("", "/Lib", "")]
	public void VersionSiteUrl_AddsTheFolderOnlyWhenTheUrlCarriesTheBasePath(string url, string basePath, string expected)
	{
		string versionUrl = VersionedBuildOrchestrator.VersionSiteUrl(url, basePath, "v1.0");

		versionUrl.Should().Be(expected);
		if (expected.Length > 0)
		{
			SiteUrls.Absolute(versionUrl, basePath == "/" ? "/v1.0" : basePath + "/v1.0", "/guide")
				.Should().EndWith("/v1.0/guide/").And.NotContain("/Lib/Lib");
		}
	}
}
