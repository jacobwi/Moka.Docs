using System.Globalization;
using System.Text.RegularExpressions;

namespace Moka.Docs.Core.Theming;

/// <summary>
///     The page language of a site (<c>site.language</c>, a BCP 47 tag such as <c>en</c>, <c>pt-BR</c> or
///     <c>zh-Hans</c>) and what follows from it: the text direction, the <c>og:locale</c> value, the built-in
///     translation of the theme's text, plural forms and how dates are written.
/// </summary>
public static partial class SiteLanguages
{
	/// <summary>The language a site has when <c>site.language</c> isn't set.</summary>
	public const string Default = "en";

	/// <summary>Left to right, the direction of every language that isn't written right to left.</summary>
	public const string LeftToRight = "ltr";

	/// <summary>Right to left, the direction of Arabic, Hebrew, Persian, Urdu and a few more.</summary>
	public const string RightToLeft = "rtl";

	// Primary language subtags written right to left. A site in one of them gets dir="rtl" unless
	// site.direction says otherwise.
	private static readonly HashSet<string> _rightToLeft = new(StringComparer.OrdinalIgnoreCase)
	{
		"ar", "he", "fa", "ur", "ps", "sd", "ug", "yi", "dv", "ckb"
	};

	// Languages whose og:locale is clear without a region: the language is spoken as a first language in
	// one country only. Others need a region in site.language (pt-BR gives pt_BR).
	private static readonly Dictionary<string, string> _ogLocales = new(StringComparer.OrdinalIgnoreCase)
	{
		["ja"] = "ja_JP",
		["ko"] = "ko_KR",
		["zh-hans"] = "zh_CN",
		["zh-hant"] = "zh_TW"
	};

	/// <summary>The text directions <c>site.direction</c> accepts.</summary>
	public static IReadOnlyList<string> Directions { get; } = [LeftToRight, RightToLeft];

	/// <summary>
	///     Whether <paramref name="tag" /> is a well-formed language tag: a 2 or 3 letter language, then
	///     subtags of 1 to 8 letters or digits, separated by hyphens (or underscores, which are read as hyphens).
	/// </summary>
	/// <param name="tag">A configured language tag.</param>
	/// <returns><c>true</c> when the tag can be used as <c>&lt;html lang&gt;</c>.</returns>
	public static bool IsWellFormed(string? tag) => !string.IsNullOrWhiteSpace(tag) && TagPattern().IsMatch(tag.Trim());

	/// <summary>
	///     The tag in its usual case: the language lower case, a 4-letter script title case, a 2-letter region
	///     upper case (<c>PT-br</c> gives <c>pt-BR</c>, <c>zh-hans</c> gives <c>zh-Hans</c>). An empty or
	///     malformed tag gives <see cref="Default" />.
	/// </summary>
	/// <param name="tag">A configured language tag.</param>
	/// <returns>A well-formed tag.</returns>
	public static string Normalize(string? tag)
	{
		if (!IsWellFormed(tag))
		{
			return Default;
		}

		string[] parts = tag!.Trim().Replace('_', '-').Split('-');
		for (int i = 0; i < parts.Length; i++)
		{
			string part = parts[i].ToLowerInvariant();
			parts[i] = i == 0
				? part
				: part.Length == 4 && part.All(char.IsLetter)
					? char.ToUpperInvariant(part[0]) + part[1..]
					: part.Length == 2 && part.All(char.IsLetter)
						? part.ToUpperInvariant()
						: part;
		}

		return string.Join('-', parts);
	}

	/// <summary>
	///     The text direction of a page: <paramref name="configured" /> when it is <c>ltr</c> or <c>rtl</c>
	///     (any case), else right to left for a language written that way (Arabic, Hebrew, Persian, Urdu,
	///     Pashto, Sindhi, Uyghur, Yiddish, Dhivehi, Central Kurdish) and left to right for every other.
	/// </summary>
	/// <param name="language">The site's language tag.</param>
	/// <param name="configured">The configured <c>site.direction</c>, or empty for the language's own.</param>
	/// <returns><see cref="LeftToRight" /> or <see cref="RightToLeft" />.</returns>
	public static string Direction(string? language, string? configured)
	{
		string direction = configured?.Trim().ToLowerInvariant() ?? "";
		if (direction is LeftToRight or RightToLeft)
		{
			return direction;
		}

		return _rightToLeft.Contains(Normalize(language).Split('-')[0]) ? RightToLeft : LeftToRight;
	}

	/// <summary>Whether <paramref name="configured" /> is empty or one of <see cref="Directions" />, in any case.</summary>
	/// <param name="configured">The configured <c>site.direction</c>.</param>
	/// <returns><c>true</c> when the value is usable.</returns>
	public static bool IsKnownDirection(string? configured) =>
		string.IsNullOrWhiteSpace(configured) || Directions.Contains(configured.Trim().ToLowerInvariant());

	/// <summary>
	///     The Open Graph locale of a language (<c>og:locale</c>, such as <c>pt_BR</c>), or an empty string when it
	///     doesn't map cleanly: a tag with a 2-letter region gives <c>language_REGION</c>; without one only
	///     Japanese, Korean and Chinese with a script map (<c>ja_JP</c>, <c>ko_KR</c>, <c>zh_CN</c> for
	///     <c>zh-Hans</c>, <c>zh_TW</c> for <c>zh-Hant</c>).
	/// </summary>
	/// <param name="language">The site's language tag.</param>
	/// <returns>The locale, or an empty string.</returns>
	public static string OpenGraphLocale(string? language)
	{
		string[] parts = Normalize(language).Split('-');
		string? region = parts.Skip(1).FirstOrDefault(p => p.Length == 2 && p.All(char.IsLetter));
		if (region is not null)
		{
			return parts[0] + "_" + region;
		}

		string key = parts.Length > 1 && parts[1].Length == 4 ? parts[0] + "-" + parts[1] : parts[0];
		return _ogLocales.GetValueOrDefault(key) ?? "";
	}

	/// <summary>
	///     The built-in translations a language uses, most specific first: <c>pt-BR</c> looks in a
	///     <c>pt-BR</c> table, then <c>pt</c>. Chinese in the simplified script (<c>zh</c>, <c>zh-CN</c>,
	///     <c>zh-SG</c>, <c>zh-Hans</c>) uses <c>zh-Hans</c>; traditional Chinese (<c>zh-Hant</c>, <c>zh-TW</c>,
	///     <c>zh-HK</c>, <c>zh-MO</c>) uses none. Only tables that exist are returned.
	/// </summary>
	/// <param name="language">The site's language tag.</param>
	/// <returns>Names from <see cref="ThemeStrings.BuiltInLanguages" />, most specific first.</returns>
	public static IReadOnlyList<string> TranslationChain(string? language)
	{
		string[] parts = Normalize(language).Split('-');
		var chain = new List<string>();
		bool chinese = parts[0] == "zh";
		bool traditional = chinese && parts.Skip(1).Any(p => p is "Hant" or "TW" or "HK" or "MO");
		bool simplified = chinese && !traditional;
		for (int length = parts.Length; length >= 1; length--)
		{
			string candidate = string.Join('-', parts.Take(length));
			if (simplified && length <= 2 && !candidate.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase))
			{
				candidate = "zh-Hans";
			}

			if (ThemeStrings.BuiltInLanguages.FirstOrDefault(l => l.Equals(candidate, StringComparison.OrdinalIgnoreCase)) is { } table
			    && !chain.Contains(table))
			{
				chain.Add(table);
			}
		}

		return chain;
	}

	/// <summary>
	///     The plural form a count takes in a language, as the theme's <c>.one</c> and <c>.other</c> keys name
	///     them: <c>one</c> for 1 (and for 0 in French and Portuguese), <c>other</c> for the rest, and always
	///     <c>other</c> in Japanese, Chinese and Korean, which don't inflect for number.
	/// </summary>
	/// <param name="language">The site's language tag.</param>
	/// <param name="count">The count.</param>
	/// <returns><c>one</c> or <c>other</c>.</returns>
	public static string PluralCategory(string? language, long count)
	{
		string primary = Normalize(language).Split('-')[0];
		return primary switch
		{
			"ja" or "zh" or "ko" or "th" or "vi" or "id" or "ms" => "other",
			"fr" or "pt" => count is 0 or 1 ? "one" : "other",
			_ => count == 1 ? "one" : "other"
		};
	}

	/// <summary>
	///     A date written out in a language, such as <c>November 12, 2026</c>, <c>12. November 2026</c> or
	///     <c>2026年11月12日</c>. The built-in languages use their own fixed patterns, so the output is the same on
	///     every machine. Another language uses the build machine's long date pattern for it without the day of
	///     the week, and a language the machine has no data for gets the ISO date (<c>2026-11-12</c>).
	/// </summary>
	/// <param name="language">The site's language tag.</param>
	/// <param name="date">The date.</param>
	/// <returns>The date as text.</returns>
	public static string FormatDate(string? language, DateOnly date)
	{
		string tag = Normalize(language);
		if (DatePatternFor(tag) is { } pattern)
		{
			return pattern.Format(date);
		}

		try
		{
			var culture = CultureInfo.GetCultureInfo(tag, true);
			string longDate = WeekdayPattern().Replace(culture.DateTimeFormat.LongDatePattern, "");
			return date.ToString(longDate.Length > 0 ? longDate : "D", culture);
		}
		catch (CultureNotFoundException)
		{
			return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}

	private static DatePattern? DatePatternFor(string tag)
	{
		string primary = tag.Split('-')[0];
		return primary switch
		{
			"en" => new DatePattern("{month} {day}, {year}",
				["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]),
			"es" => new DatePattern("{day} de {month} de {year}",
				["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"]),
			"fr" => new DatePattern("{day} {month} {year}",
				["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre"]),
			"de" => new DatePattern("{day}. {month} {year}",
				["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"]),
			"pt" => new DatePattern("{day} de {month} de {year}",
				["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"]),
			"ja" => new DatePattern("{year}年{monthNumber}月{day}日", []),
			"zh" when TranslationChain(tag).Count > 0 => new DatePattern("{year}年{monthNumber}月{day}日", []),
			_ => null
		};
	}

	[GeneratedRegex(@"^[A-Za-z]{2,3}([-_][A-Za-z0-9]{1,8})*$")]
	private static partial Regex TagPattern();

	// The day of the week at the start or end of a long date pattern, with the punctuation that joins it.
	[GeneratedRegex(@"^\s*dddd[\s,،、]*|[\s,،、]*dddd\s*$")]
	private static partial Regex WeekdayPattern();

	private sealed record DatePattern(string Pattern, string[] Months)
	{
		public string Format(DateOnly date) => Pattern
			.Replace("{year}", date.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
			.Replace("{monthNumber}", date.Month.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
			.Replace("{month}", Months.Length == 12 ? Months[date.Month - 1] : "", StringComparison.Ordinal)
			.Replace("{day}", date.Day.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
	}
}
