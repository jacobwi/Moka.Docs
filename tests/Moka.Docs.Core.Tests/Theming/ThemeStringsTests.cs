using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed partial class ThemeStringsTests
{
	public static TheoryData<string> Languages => [.. ThemeStrings.BuiltInLanguages];

	[Fact]
	public void Catalog_HasUniqueWellFormedKeysWithText()
	{
		ThemeStrings.Keys.Should().OnlyHaveUniqueItems();
		ThemeStrings.All.Should().AllSatisfy(s =>
		{
			KeyPattern().IsMatch(s.Key).Should().BeTrue($"'{s.Key}' is a dotted camelCase key");
			s.English.Should().NotBeNullOrWhiteSpace();
			s.Description.Should().NotBeNullOrWhiteSpace();
		});
	}

	[Fact]
	public void Catalog_CountedTextsHaveBothForms()
	{
		foreach (string key in ThemeStrings.Keys.Where(k => k.EndsWith(".one", StringComparison.Ordinal)))
		{
			ThemeStrings.Find(key[..^4] + ".other").Should().NotBeNull($"{key} needs an .other form");
		}
	}

	// Every built-in translation is complete, so no text falls back to English. A key a language leaves out on
	// purpose would show here; there are none.
	[Theory]
	[MemberData(nameof(Languages))]
	public void BuiltInLanguage_TranslatesEveryKey(string language)
	{
		IReadOnlyDictionary<string, string> translation = ThemeStrings.Translation(language);

		translation.Keys.Should().BeSubsetOf(ThemeStrings.Keys, "a translation has only the theme's keys");
		ThemeStrings.Keys.Except(translation.Keys).Should().BeEmpty($"{language} falls back to English for no key");
		ThemeText.For(language).Sources.Values.Should().AllBe(language);
	}

	[Theory]
	[MemberData(nameof(Languages))]
	public void BuiltInLanguage_KeepsEveryPlaceholderAndUsesNoDashes(string language)
	{
		foreach ((string key, string text) in ThemeStrings.Translation(language))
		{
			Placeholders(text).Should().BeEquivalentTo(Placeholders(ThemeStrings.Find(key)!.English), $"{language} {key}: '{text}'");
			text.Should().NotContainAny(["\u2014", "\u2013"], $"{language} {key}");
			text.Should().NotBeNullOrWhiteSpace($"{language} {key}");
		}
	}

	[Fact]
	public void English_HasNoDashesEither()
	{
		ThemeStrings.All.Should().AllSatisfy(s => s.English.Should().NotContainAny("\u2014", "\u2013"));
	}

	[Theory]
	[InlineData("de", "Auf dieser Seite")]
	[InlineData("de-AT", "Auf dieser Seite")]
	[InlineData("pt-BR", "Nesta página")]
	[InlineData("PT-br", "Nesta página")]
	[InlineData("zh", "本页目录")]
	[InlineData("zh-CN", "本页目录")]
	[InlineData("zh-Hans-SG", "本页目录")]
	[InlineData("zh-TW", "On this page")]
	[InlineData("zh-Hant", "On this page")]
	[InlineData("nl", "On this page")]
	[InlineData("not a tag", "On this page")]
	[InlineData("", "On this page")]
	public void For_FallsBackFromTheMostSpecificTableToEnglish(string language, string tocTitle)
	{
		ThemeText.For(language)["toc.title"].Should().Be(tocTitle);
	}

	[Fact]
	public void For_FallsBackKeyByKey()
	{
		SiteLanguages.TranslationChain("pt-BR").Should().Equal("pt");
		SiteLanguages.TranslationChain("zh-Hans-CN").Should().Equal("zh-Hans");
		SiteLanguages.TranslationChain("zh-HK").Should().BeEmpty();
		SiteLanguages.TranslationChain("en-GB").Should().BeEmpty();
	}

	[Fact]
	public void Overrides_WinOverTheTranslationForTheirKeyOnly()
	{
		ThemeText text = ThemeText.For("de", new Dictionary<string, string> { ["toc.title"] = "Inhalt" });

		text["toc.title"].Should().Be("Inhalt");
		text.Sources["toc.title"].Should().Be("site");
		text["search.button"].Should().Be("Suche");
		text.UnknownKeys.Should().BeEmpty();
	}

	[Fact]
	public void Overrides_UnknownKeysAreReportedAndIgnored()
	{
		ThemeText text = ThemeText.For("en", new Dictionary<string, string>
		{
			["toc.Title"] = "x",
			["nothing.here"] = "y",
			["toc.title.few"] = "z",
			["search.resultCount.few"] = "{count} wyniki"
		});

		text.UnknownKeys.Should().Equal("toc.Title", "nothing.here", "toc.title.few");
		text["toc.title"].Should().Be("On this page");
		text.Strings.Should().ContainKey("search.resultCount.few");
		ThemeStrings.IsKnown("search.resultCount.many").Should().BeTrue();
		ThemeStrings.IsKnown("toc.title.other").Should().BeFalse();
	}

	[Theory]
	[InlineData("en", 1, "one")]
	[InlineData("en", 0, "other")]
	[InlineData("en", 2, "other")]
	[InlineData("de", 1, "one")]
	[InlineData("fr", 0, "one")]
	[InlineData("fr", 1, "one")]
	[InlineData("fr", 2, "other")]
	[InlineData("pt-BR", 0, "one")]
	[InlineData("ja", 1, "other")]
	[InlineData("zh-Hans", 1, "other")]
	public void PluralCategory_FollowsTheLanguage(string language, long count, string form)
	{
		SiteLanguages.PluralCategory(language, count).Should().Be(form);
	}

	[Fact]
	public void Plural_PicksTheFormAndFillsTheCount()
	{
		ThemeText.English.Plural("blog.postCount", 1).Should().Be("1 post");
		ThemeText.English.Plural("blog.postCount", 3).Should().Be("3 posts");
		ThemeText.For("fr").Plural("search.resultCount", 0).Should().Be("0 résultat");
	}

	[Fact]
	public void Format_FillsEachValueOnce()
	{
		ThemeText.English.Format("nav.toggleSection", ("label", "{label} and {count}")).Should().Be("Toggle {label} and {count}");
		ThemeText.English.Format("toc.position.other", ("index", "2")).Should().Be("Section 2 of {count}");
	}

	[Fact]
	public void Indexer_RejectsAKeyTheThemeDoesNotHave()
	{
		Action read = () => _ = ThemeText.English["no.such.key"];

		read.Should().Throw<ArgumentException>();
	}

	private static List<string> Placeholders(string text) =>
		[.. PlaceholderPattern().Matches(text).Select(m => m.Value).Order(StringComparer.Ordinal)];

	[GeneratedRegex(@"^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9-]*)+$")]
	private static partial Regex KeyPattern();

	[GeneratedRegex(@"\{[a-z]+\}")]
	private static partial Regex PlaceholderPattern();
}
