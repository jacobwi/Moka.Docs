using System.Globalization;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Core.Theming;

/// <summary>
///     The theme's text for one site: for each key of <see cref="ThemeStrings.All" />, the site's override
///     from <c>theme.options.strings</c>, else the built-in translation for <c>site.language</c> (the most
///     specific table first, so <c>pt-BR</c> reads <c>pt</c>), else English.
/// </summary>
public sealed class ThemeText
{
	private readonly Dictionary<string, string> _strings;

	private ThemeText(string language, Dictionary<string, string> strings, Dictionary<string, string> sources,
		IReadOnlyList<string> unknownKeys)
	{
		Language = language;
		_strings = strings;
		Sources = sources;
		UnknownKeys = unknownKeys;
	}

	/// <summary>The English text without overrides.</summary>
	public static ThemeText English { get; } = For(SiteLanguages.Default);

	/// <summary>The site's language tag in its usual case, such as <c>pt-BR</c>.</summary>
	public string Language { get; }

	/// <summary>
	///     Every key of <see cref="ThemeStrings.All" /> with its text, plus the extra plural forms an override
	///     added (<c>search.resultCount.few</c>).
	/// </summary>
	public IReadOnlyDictionary<string, string> Strings => _strings;

	/// <summary>
	///     Where each text came from: <c>site</c> for an override, a name from
	///     <see cref="ThemeStrings.BuiltInLanguages" /> for a translation, else <c>en</c>.
	/// </summary>
	public IReadOnlyDictionary<string, string> Sources { get; }

	/// <summary>The override keys the theme doesn't have, in the order the site gave them. They are ignored.</summary>
	public IReadOnlyList<string> UnknownKeys { get; }

	/// <summary>The text of <paramref name="key" />.</summary>
	/// <param name="key">A key of <see cref="ThemeStrings.All" />.</param>
	/// <exception cref="ArgumentException">The theme has no such key.</exception>
	public string this[string key] =>
		_strings.TryGetValue(key, out string? text) ? text : throw new ArgumentException($"The theme has no text '{key}'.", nameof(key));

	/// <summary>The theme's text for a site: its language and its <c>theme.options.strings</c>.</summary>
	/// <param name="config">The site's configuration.</param>
	/// <returns>The resolved text.</returns>
	public static ThemeText For(SiteConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);
		return For(config.Site.Language, config.Theme.Options.Strings);
	}

	/// <summary>The theme's text in a language, with a site's own wording for some keys.</summary>
	/// <param name="language">A BCP 47 tag such as <c>de</c> or <c>pt-BR</c>; empty or malformed means English.</param>
	/// <param name="overrides">Text by key that replaces the theme's, or <c>null</c>.</param>
	/// <returns>The resolved text.</returns>
	public static ThemeText For(string? language, IReadOnlyDictionary<string, string>? overrides = null)
	{
		string tag = SiteLanguages.Normalize(language);
		var strings = new Dictionary<string, string>(StringComparer.Ordinal);
		var sources = new Dictionary<string, string>(StringComparer.Ordinal);
		IReadOnlyList<string> chain = SiteLanguages.TranslationChain(tag);
		foreach (ThemeStringInfo entry in ThemeStrings.All)
		{
			strings[entry.Key] = entry.English;
			sources[entry.Key] = SiteLanguages.Default;
			foreach (string table in chain)
			{
				if (ThemeStrings.Translation(table).TryGetValue(entry.Key, out string? translated))
				{
					strings[entry.Key] = translated;
					sources[entry.Key] = table;
					break;
				}
			}
		}

		var unknown = new List<string>();
		foreach ((string key, string value) in overrides ?? new Dictionary<string, string>())
		{
			if (!ThemeStrings.IsKnown(key))
			{
				unknown.Add(key);
				continue;
			}

			strings[key] = value ?? "";
			sources[key] = "site";
		}

		return new ThemeText(tag, strings, sources, unknown);
	}

	/// <summary>The text of <paramref name="key" /> with each <c>{name}</c> replaced by its value.</summary>
	/// <param name="key">A key of <see cref="ThemeStrings.All" />.</param>
	/// <param name="values">Names and values, such as <c>("label", "Guide")</c>.</param>
	/// <returns>The filled-in text. Values are inserted as they are, not escaped.</returns>
	public string Format(string key, params (string Name, string Value)[] values) => Fill(this[key], values);

	/// <summary>
	///     The plural form of a counted text for <paramref name="count" /> (<c>key.one</c> or <c>key.other</c>,
	///     as <see cref="SiteLanguages.PluralCategory" /> picks), with <c>{count}</c> and the other names filled in.
	/// </summary>
	/// <param name="key">The key without its form, such as <c>blog.readingTime</c>.</param>
	/// <param name="count">The count.</param>
	/// <param name="values">Other names and values in the text.</param>
	/// <returns>The filled-in text.</returns>
	public string Plural(string key, long count, params (string Name, string Value)[] values)
	{
		string form = SiteLanguages.PluralCategory(Language, count);
		string text = _strings.TryGetValue(key + "." + form, out string? chosen) ? chosen : this[key + ".other"];
		return Fill(text, [("count", count.ToString(CultureInfo.InvariantCulture)), .. values]);
	}

	/// <summary>A date written out in the site's language (see <see cref="SiteLanguages.FormatDate" />).</summary>
	/// <param name="date">The date.</param>
	/// <returns>The date as text.</returns>
	public string FormatDate(DateOnly date) => SiteLanguages.FormatDate(Language, date);

	// One pass, so a value that itself contains "{name}" is never filled in again.
	private static string Fill(string text, (string Name, string Value)[] values)
	{
		if (values.Length == 0 || !text.Contains('{', StringComparison.Ordinal))
		{
			return text;
		}

		var result = new System.Text.StringBuilder(text.Length + 32);
		int i = 0;
		while (i < text.Length)
		{
			int open = text.IndexOf('{', i);
			int close = open < 0 ? -1 : text.IndexOf('}', open + 1);
			if (open < 0 || close < 0)
			{
				result.Append(text, i, text.Length - i);
				break;
			}

			string name = text[(open + 1)..close];
			int match = Array.FindIndex(values, v => v.Name == name);
			result.Append(text, i, open - i);
			if (match >= 0)
			{
				result.Append(values[match].Value);
				i = close + 1;
			}
			else
			{
				result.Append('{');
				i = open + 1;
			}
		}

		return result.ToString();
	}
}
