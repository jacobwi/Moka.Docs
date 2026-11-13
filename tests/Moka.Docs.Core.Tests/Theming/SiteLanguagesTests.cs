using FluentAssertions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Core.Tests.Theming;

public sealed class SiteLanguagesTests
{
	[Theory]
	[InlineData("en", "en")]
	[InlineData("PT-br", "pt-BR")]
	[InlineData("pt_BR", "pt-BR")]
	[InlineData("zh-hans", "zh-Hans")]
	[InlineData("ZH-HANT-tw", "zh-Hant-TW")]
	[InlineData(" de ", "de")]
	[InlineData("", "en")]
	[InlineData(null, "en")]
	[InlineData("english please", "en")]
	[InlineData("e", "en")]
	[InlineData("de-", "en")]
	public void Normalize_WritesTheTagInItsUsualCase(string? tag, string expected)
	{
		SiteLanguages.Normalize(tag).Should().Be(expected);
	}

	[Theory]
	[InlineData("ar", null, "rtl")]
	[InlineData("he-IL", "", "rtl")]
	[InlineData("fa", null, "rtl")]
	[InlineData("ur", null, "rtl")]
	[InlineData("en", null, "ltr")]
	[InlineData("ja", null, "ltr")]
	[InlineData("ar", "ltr", "ltr")]
	[InlineData("en", "RTL", "rtl")]
	[InlineData("ar", "sideways", "rtl")]
	public void Direction_FollowsTheLanguageUnlessSet(string language, string? configured, string expected)
	{
		SiteLanguages.Direction(language, configured).Should().Be(expected);
	}

	[Theory]
	[InlineData("", true)]
	[InlineData("rtl", true)]
	[InlineData(" LTR ", true)]
	[InlineData("auto", false)]
	public void IsKnownDirection_AcceptsEmptyLtrAndRtl(string configured, bool known)
	{
		SiteLanguages.IsKnownDirection(configured).Should().Be(known);
	}

	[Theory]
	[InlineData("en", "")]
	[InlineData("de", "")]
	[InlineData("pt-BR", "pt_BR")]
	[InlineData("de-AT", "de_AT")]
	[InlineData("ja", "ja_JP")]
	[InlineData("ko", "ko_KR")]
	[InlineData("zh-Hans", "zh_CN")]
	[InlineData("zh-Hant", "zh_TW")]
	[InlineData("zh-Hant-HK", "zh_HK")]
	[InlineData("zh", "")]
	public void OpenGraphLocale_IsWrittenOnlyWhenItMapsCleanly(string language, string expected)
	{
		SiteLanguages.OpenGraphLocale(language).Should().Be(expected);
	}

	[Theory]
	[InlineData("en", "November 12, 2026")]
	[InlineData("en-GB", "November 12, 2026")]
	[InlineData("es", "12 de noviembre de 2026")]
	[InlineData("fr", "12 novembre 2026")]
	[InlineData("de", "12. November 2026")]
	[InlineData("pt-BR", "12 de novembro de 2026")]
	[InlineData("ja", "2026年11月12日")]
	[InlineData("zh-CN", "2026年11月12日")]
	public void FormatDate_UsesTheBuiltInPatternOnEveryMachine(string language, string expected)
	{
		SiteLanguages.FormatDate(language, new DateOnly(2026, 11, 12)).Should().Be(expected);
	}

	[Fact]
	public void FormatDate_WithoutDataForTheLanguage_IsTheIsoDate()
	{
		SiteLanguages.FormatDate("qaa", new DateOnly(2026, 3, 5)).Should().Be("2026-03-05");
	}

	[Fact]
	public void FormatDate_OfAnotherLanguage_LeavesOutTheDayOfTheWeek()
	{
		string date = SiteLanguages.FormatDate("nl", new DateOnly(2026, 11, 12));

		date.Should().Contain("2026").And.NotContain("donderdag");
	}
}
