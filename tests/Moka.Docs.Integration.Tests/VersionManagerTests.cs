using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Versioning;

namespace Moka.Docs.Integration.Tests;

public sealed class VersionManagerTests
{
	private static SiteConfig CreateConfig(
		bool enabled = true,
		string strategy = "directory",
		params VersionDefinition[] versions)
	{
		return new SiteConfig
		{
			Site = new SiteMetadata { Title = "Test Site" },
			Features = new FeaturesConfig
			{
				Versioning = new VersioningFeatureConfig
				{
					Enabled = enabled,
					Strategy = strategy,
					Versions = versions.ToList()
				}
			}
		};
	}

	#region Basic Properties

	[Fact]
	public void IsEnabled_WhenEnabled_ReturnsTrue()
	{
		SiteConfig config = CreateConfig(versions: new VersionDefinition { Label = "v1.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.IsEnabled.Should().BeTrue();
	}

	[Fact]
	public void IsEnabled_WhenDisabled_ReturnsFalse()
	{
		SiteConfig config = CreateConfig(false);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void Strategy_ReturnsConfiguredStrategy()
	{
		SiteConfig config = CreateConfig(strategy: "dropdown-only",
			versions: new VersionDefinition { Label = "v1.0" });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Strategy.Should().Be("dropdown-only");
	}

	#endregion

	#region Version List

	[Fact]
	public void Versions_WhenDisabled_ReturnsEmptyList()
	{
		SiteConfig config = CreateConfig(false);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions.Should().BeEmpty();
	}

	[Fact]
	public void Versions_WhenEnabled_ReturnsAllVersions()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0", Default = true },
				new VersionDefinition { Label = "v2.0" },
				new VersionDefinition { Label = "v3.0-beta", Prerelease = true }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions.Should().HaveCount(3);
		manager.Versions[0].Label.Should().Be("v1.0");
		manager.Versions[1].Label.Should().Be("v2.0");
		manager.Versions[2].Label.Should().Be("v3.0-beta");
	}

	[Fact]
	public void Versions_GeneratesSlugs()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v2.0 Beta", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions[0].Slug.Should().Be("v2.0-beta");
	}

	[Fact]
	public void Versions_PreservesIsDefault()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0" },
				new VersionDefinition { Label = "v2.0", Default = true }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions[0].IsDefault.Should().BeFalse();
		manager.Versions[1].IsDefault.Should().BeTrue();
	}

	[Fact]
	public void Versions_PreservesIsPrerelease()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0" },
				new VersionDefinition { Label = "v2.0-rc", Prerelease = true }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions[0].IsPrerelease.Should().BeFalse();
		manager.Versions[1].IsPrerelease.Should().BeTrue();
	}

	#endregion

	#region DefaultVersion

	[Fact]
	public void DefaultVersion_ReturnsExplicitDefault()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0" },
				new VersionDefinition { Label = "v2.0", Default = true }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.DefaultVersion.Should().NotBeNull();
		manager.DefaultVersion!.Label.Should().Be("v2.0");
	}

	[Fact]
	public void DefaultVersion_NoExplicitDefault_ReturnsFirstNonPrerelease()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v3.0-beta", Prerelease = true },
				new VersionDefinition { Label = "v2.0" },
				new VersionDefinition { Label = "v1.0" }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.DefaultVersion.Should().NotBeNull();
		manager.DefaultVersion!.Label.Should().Be("v2.0");
	}

	[Fact]
	public void DefaultVersion_AllPrerelease_ReturnsFirst()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v2.0-rc", Prerelease = true },
				new VersionDefinition { Label = "v1.0-beta", Prerelease = true }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.DefaultVersion.Should().NotBeNull();
		manager.DefaultVersion!.Label.Should().Be("v2.0-rc");
	}

	[Fact]
	public void DefaultVersion_NoVersions_ReturnsNull()
	{
		SiteConfig config = CreateConfig(false);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.DefaultVersion.Should().BeNull();
	}

	#endregion

	#region FindByLabel

	[Fact]
	public void FindByLabel_ExistingVersion_ReturnsVersion()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0", Default = true },
				new VersionDefinition { Label = "v2.0" }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		DocVersion? found = manager.FindByLabel("v2.0");

		found.Should().NotBeNull();
		found!.Label.Should().Be("v2.0");
	}

	[Fact]
	public void FindByLabel_CaseInsensitive()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "V2.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		DocVersion? found = manager.FindByLabel("v2.0");

		found.Should().NotBeNull();
	}

	[Fact]
	public void FindByLabel_NonExistingVersion_ReturnsNull()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v1.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		DocVersion? found = manager.FindByLabel("v99.0");

		found.Should().BeNull();
	}

	#endregion

	#region GetOutputPath

	[Fact]
	public void GetOutputPath_DefaultVersion_ReturnsBaseOutputPath()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v1.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);
		DocVersion version = manager.Versions[0];

		string path = manager.GetOutputPath(version, "./_site");

		path.Should().Be("./_site");
	}

	[Fact]
	public void GetOutputPath_NonDefaultVersion_ReturnsSubdirectory()
	{
		SiteConfig config = CreateConfig(
			versions:
			[
				new VersionDefinition { Label = "v1.0", Default = true },
				new VersionDefinition { Label = "v2.0" }
			]);
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);
		DocVersion v2 = manager.Versions[1];

		string path = manager.GetOutputPath(v2, "./_site");

		path.Should().Contain("v2.0");
	}

	#endregion

	#region GetBranch

	[Fact]
	public void GetBranch_WithConfiguredBranch_ReturnsBranch()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v1.0", Branch = "release/1.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		string? branch = manager.GetBranch(manager.Versions[0]);

		branch.Should().Be("release/1.0");
	}

	[Fact]
	public void GetBranch_WithoutConfiguredBranch_ReturnsNull()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v1.0", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		string? branch = manager.GetBranch(manager.Versions[0]);

		branch.Should().BeNull();
	}

	#endregion

	#region Slug Generation

	[Fact]
	public void Versions_SlugStripsSpecialChars()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "v2.0 (Latest!)", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		string slug = manager.Versions[0].Slug;

		slug.Should().Be("v2.0-latest");
		slug.Should().NotContain("(");
		slug.Should().NotContain(")");
		slug.Should().NotContain("!");
	}

	[Fact]
	public void Versions_SlugIsLowercase()
	{
		SiteConfig config = CreateConfig(
			versions: new VersionDefinition { Label = "V2.0-RC1", Default = true });
		var manager = new VersionManager(config, NullLogger<VersionManager>.Instance);

		manager.Versions[0].Slug.Should().Be("v2.0-rc1");
	}

	#endregion
}
