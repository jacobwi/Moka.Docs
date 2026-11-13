using FluentAssertions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     The theme lab warns when a <c>MOKADOCS_*</c> variable wins over an option it saves. The names it
///     warns about must be the ones the configuration reader reads. Environment variables are
///     process-wide, so these run alone.
/// </summary>
[Collection(EndToEndCollection.Name)]
public sealed class ThemeLabEnvironmentTests
{
	public static TheoryData<string> OptionsWithAVariable() =>
		new(ThemeLabCatalog.CreateOptions().Where(o => o.EnvironmentVariable is not null).Select(o => o.Key));

	private static ThemeOptions ParseWith(params (string Name, string Value)[] variables)
	{
		string?[] previous = [.. variables.Select(v => Environment.GetEnvironmentVariable(v.Name))];
		foreach ((string name, string value) in variables)
		{
			Environment.SetEnvironmentVariable(name, value);
		}

		try
		{
			return SiteConfigReader.Parse("site:\n  title: Docs\n").Theme.Options;
		}
		finally
		{
			for (int i = 0; i < variables.Length; i++)
			{
				Environment.SetEnvironmentVariable(variables[i].Name, previous[i]);
			}
		}
	}

	[Theory]
	[MemberData(nameof(OptionsWithAVariable))]
	public void Variable_IsTheOneTheReaderUses(string key)
	{
		ThemeLabOption option = ThemeLabCatalog.CreateOptions().Single(o => o.Key == key);
		string value = option.Kind switch
		{
			ThemeLabOptionKind.Choice => option.Choices.Last(c => c.Value.Length > 0 && c.Value != option.Default).Value,
			ThemeLabOptionKind.Toggle => option.Default == "true" ? "false" : "true",
			_ => key == "banner.link" ? "/from-the-environment" : "From the environment"
		};
		List<(string, string)> variables = [(option.EnvironmentVariable!, value)];
		if (key == "banner.link")
		{
			variables.Add(("MOKADOCS_BANNER_TEXT", "A banner"));
		}

		option.Read(ParseWith([.. variables])).Should().Be(value);
	}

	[Fact]
	public void EveryLayoutPart_HasAVariable()
	{
		ThemeLabCatalog.CreateOptions().Where(o => o.Key.StartsWith("layout.", StringComparison.Ordinal))
			.Should().AllSatisfy(o => o.EnvironmentVariable.Should().StartWith("MOKADOCS_LAYOUT_"));
	}
}
