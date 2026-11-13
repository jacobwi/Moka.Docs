using System.Reflection;
using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class ThemePresetsTests
{
	public static TheoryData<string> Presets()
	{
		var data = new TheoryData<string>();
		foreach (string name in ThemePresets.Names)
		{
			data.Add(name);
		}

		return data;
	}

	private static IEnumerable<(string Preset, string Key, string Value)> Entries() =>
		ThemePresets.All.SelectMany(p => p.Options.Select(o => (p.Name, o.Key, o.Value)));

	private static void ShouldAllNameOptions(IEnumerable<string> unresolved) =>
		unresolved.Distinct().Order(StringComparer.Ordinal).Should().BeEmpty("every preset key must name a theme option");

	// The option a preset key names on the record the reader produces, by the same mokadocs.yaml names.
	private static object? ValueAt(ThemeOptions options, string key)
	{
		object? current = options;
		foreach (string segment in key.Split('.'))
		{
			PropertyInfo? property = current?.GetType().GetProperties()
				.FirstOrDefault(p => CamelCaseNamingConvention.Instance.Apply(p.Name) == segment);
			if (property is null)
			{
				return null;
			}

			current = property.GetValue(current);
		}

		return current;
	}

	[Fact]
	public void Names_AreListedInTheDocumentedOrder()
	{
		ThemePresets.Names.Should().Equal("reference", "terminal", "book", "product", "minimal", "accessible", "moka");
		ThemePresets.Names.Should().Equal(ThemePresets.Reference, ThemePresets.Terminal, ThemePresets.Book, ThemePresets.Product,
			ThemePresets.Minimal, ThemePresets.Accessible, ThemePresets.Moka);
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void EveryPreset_HasALabelADescriptionAndAStyle(string name)
	{
		ThemePresetInfo preset = ThemePresets.Find(name)!;

		preset.Label.Should().NotBeNullOrWhiteSpace();
		preset.Description.Should().NotBeNullOrWhiteSpace().And.NotContain("\u2014").And.NotContain("\u2013");
		preset.Options.Should().ContainKey("style");
	}

	[Theory]
	[InlineData("Reference", "reference")]
	[InlineData("  TERMINAL ", "terminal")]
	[InlineData("book", "book")]
	public void Find_IgnoresCaseAndSurroundingWhitespace(string configured, string expected)
	{
		ThemePresets.Find(configured)!.Name.Should().Be(expected);
		ThemePresets.IsKnown(configured).Should().BeTrue();
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("   ", true)]
	[InlineData("fancy", false)]
	[InlineData("classic", false)]
	public void IsKnown_TreatsEmptyAsNoPreset(string? name, bool known)
	{
		ThemePresets.IsKnown(name).Should().Be(known);
		if (string.IsNullOrWhiteSpace(name))
		{
			ThemePresets.Find(name).Should().BeNull();
		}
	}

	[Fact]
	public void EveryPresetKey_IsAThemeOption()
	{
		// A typo in a preset key would set nothing and say nothing in a user's build, so it fails here.
		ShouldAllNameOptions(Entries().Where(e => !ThemePresetMerge.IsOption(e.Key)).Select(e => e.Key));
	}

	[Fact]
	public void EveryPresetValue_IsANameTheThemeKnows()
	{
		var unknown = new List<string>();
		var unresolved = new List<string>();
		foreach ((string preset, string key, string value) in Entries())
		{
			bool? known = key switch
			{
				"style" => ThemeStyles.IsKnown(value),
				"defaultColorTheme" => ColorPresets.IsKnown(value),
				"codeTheme" => CodeThemes.IsKnown(value),
				"codeStyle" => CodeStyles.IsKnown(value),
				_ when key.StartsWith("layout.", StringComparison.Ordinal) =>
					ThemeLayouts.Parts.FirstOrDefault(p => p.Key == key["layout.".Length..])?.IsKnown(value),
				_ => false
			};

			if (known is null)
			{
				unresolved.Add(key);
			}
			else if (known == false)
			{
				unknown.Add($"{preset}: {key} = {value}");
			}
		}

		unknown.Should().BeEmpty();
		ShouldAllNameOptions(unresolved);
	}

	[Fact]
	public void EveryPresetValue_ReachesTheConfig()
	{
		var unresolved = new List<string>();
		foreach (ThemePresetInfo preset in ThemePresets.All)
		{
			ThemeOptions options = SiteConfigReader.Parse($"site:\n  title: Docs\ntheme:\n  options:\n    preset: {preset.Name}\n").Theme.Options;

			options.Preset.Should().Be(preset.Name);
			foreach ((string key, string value) in preset.Options)
			{
				object? actual = ValueAt(options, key);
				if (actual is null)
				{
					unresolved.Add(key);
					continue;
				}

				actual.ToString().Should().Be(value, "{0} sets {1}", preset.Name, key);
			}
		}

		ShouldAllNameOptions(unresolved);
	}
}
