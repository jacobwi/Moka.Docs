using System.Globalization;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Content;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Parsing.FrontMatter;

/// <summary>
///     Extracts and parses YAML front matter from the top of Markdown files.
///     Returns the parsed <see cref="Core.Content.FrontMatter" /> and the remaining Markdown body.
/// </summary>
/// <remarks>
///     The block opens with a <c>---</c> line at column 0 (blank lines and a byte order mark may
///     come first) and closes at the next <c>---</c> line at column 0. Safe to use from several
///     threads at once.
/// </remarks>
public sealed class FrontMatterExtractor
{
	private const string _delimiter = "---";

	// YamlDotNet's Deserializer keeps its caches in ConcurrentDictionary instances, so one shared
	// instance serves parallel parses. Building it per page cost a third of the parse time.
	private static readonly IDeserializer _deserializer = new DeserializerBuilder()
		.WithNamingConvention(CamelCaseNamingConvention.Instance)
		.IgnoreUnmatchedProperties()
		.Build();

	private static readonly HashSet<string> _knownKeys = KeysOf<FrontMatterDto>();
	private static readonly HashSet<string> _knownFeatureKeys = KeysOf<LandingFeatureDto>();
	private static readonly HashSet<string> _knownStatKeys = KeysOf<LandingStatDto>();
	private static readonly HashSet<string> _knownCtaKeys = KeysOf<LandingCallToActionDto>();

	private static readonly Regex _yamlLocationPrefix = new(
		@"^\(Line: \d+, Col: \d+, Idx: \d+\) - \(Line: \d+, Col: \d+, Idx: \d+\): ",
		RegexOptions.Compiled);

	private static readonly Regex _isoDate = new(
		@"^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:\d{2})?)?$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	///     Extracts front matter from a Markdown string.
	/// </summary>
	/// <param name="markdown">The raw Markdown content (may include front matter).</param>
	/// <returns>The extracted front matter and the Markdown body after the front matter block.</returns>
	public FrontMatterResult Extract(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), "");
		}

		string text = markdown[0] == '\uFEFF' ? markdown[1..] : markdown;

		int openingLine = SkipBlankLines(text);
		if (!IsDelimiterLine(text, openingLine, out int yamlStart))
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), text);
		}

		int closingLine = FindClosingDelimiter(text, yamlStart, out int bodyStart);
		if (closingLine < 0)
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), text)
			{
				Warnings = ["front matter has no closing '---' line, so it was read as Markdown"]
			};
		}

		string yaml = text[yamlStart..closingLine];
		string body = text[bodyStart..].TrimStart('\r', '\n');

		if (string.IsNullOrWhiteSpace(yaml))
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), body);
		}

		int firstYamlLine = LineNumberAt(text, yamlStart);
		try
		{
			FrontMatterDto? dto = _deserializer.Deserialize<FrontMatterDto>(yaml);
			var valueProblems = new List<(string Key, string Message)>();
			Core.Content.FrontMatter frontMatter = MapFromDto(dto, valueProblems);
			List<string> warnings = FindUnknownKeys(yaml, firstYamlLine);
			warnings.AddRange(valueProblems.Select(p => $"{p.Message} (line {KeyLine(yaml, p.Key, firstYamlLine)})"));
			if (dto?.Version is not null && VersionRangeText(dto.Version) is null)
			{
				warnings.Add("front matter 'version' must be a version range or a list of them; it was ignored");
			}

			return new FrontMatterResult(frontMatter, body)
			{
				Warnings = warnings
			};
		}
		catch (YamlException ex)
		{
			// Malformed YAML, or a value of the wrong type (order: first). The page keeps
			// building without front matter; the error is handed back so the build can say so.
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), body, Describe(ex, firstYamlLine));
		}
	}

	#region Private Helpers

	private static int SkipBlankLines(string text)
	{
		int lineStart = 0;
		while (lineStart < text.Length)
		{
			int lineEnd = LineEnd(text, lineStart);
			if (!text.AsSpan(lineStart, lineEnd - lineStart).IsWhiteSpace())
			{
				return lineStart;
			}

			lineStart = lineEnd + 1;
		}

		return text.Length;
	}

	/// <summary>
	///     Whether the line starting at <paramref name="lineStart" /> is <c>---</c> at column 0,
	///     with nothing after it but spaces or tabs.
	/// </summary>
	private static bool IsDelimiterLine(string text, int lineStart, out int nextLineStart)
	{
		int lineEnd = LineEnd(text, lineStart);
		nextLineStart = Math.Min(lineEnd + 1, text.Length);
		ReadOnlySpan<char> line = text.AsSpan(lineStart, lineEnd - lineStart).TrimEnd(" \t\r");
		return line.SequenceEqual(_delimiter);
	}

	private static int FindClosingDelimiter(string text, int from, out int bodyStart)
	{
		int lineStart = from;
		while (lineStart < text.Length)
		{
			if (IsDelimiterLine(text, lineStart, out bodyStart))
			{
				return lineStart;
			}

			lineStart = LineEnd(text, lineStart) + 1;
		}

		bodyStart = text.Length;
		return -1;
	}

	private static int LineEnd(string text, int lineStart)
	{
		int newline = text.IndexOf('\n', lineStart);
		return newline < 0 ? text.Length : newline;
	}

	private static int LineNumberAt(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

	private static string Describe(YamlException ex, int firstYamlLine)
	{
		long line = firstYamlLine + ex.Start.Line - 1;
		string message = _yamlLocationPrefix.Replace(ex.Message, "");
		if (ex.InnerException is { } inner)
		{
			message = $"{message.TrimEnd('.')}: {inner.Message}";
		}

		return $"line {line}, column {ex.Start.Column}: {message}";
	}

	private static List<string> FindUnknownKeys(string yaml, int firstYamlLine)
	{
		var stream = new YamlStream();
		try
		{
			stream.Load(new StringReader(yaml));
		}
		catch (YamlException)
		{
			return [];
		}

		if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode root)
		{
			return [];
		}

		var warnings = new List<string>();
		foreach ((YamlNode key, YamlNode value) in root.Children)
		{
			if (key is not YamlScalarNode { Value: { } name })
			{
				continue;
			}

			if (!_knownKeys.Contains(name))
			{
				warnings.Add(UnknownKey(name, name, key, firstYamlLine, _knownKeys));
				continue;
			}

			switch (name)
			{
				case "features" when value is YamlSequenceNode features:
					AddUnknownItemKeys(features, "features[]", _knownFeatureKeys);
					break;
				case "stats" when value is YamlSequenceNode stats:
					AddUnknownItemKeys(stats, "stats[]", _knownStatKeys);
					break;
				case "cta" when value is YamlMappingNode cta:
					AddUnknownKeys(cta, "cta", _knownCtaKeys);
					break;
			}
		}

		return warnings;

		void AddUnknownItemKeys(YamlSequenceNode items, string path, HashSet<string> knownKeys)
		{
			foreach (YamlNode item in items)
			{
				if (item is YamlMappingNode mapping)
				{
					AddUnknownKeys(mapping, path, knownKeys);
				}
			}
		}

		void AddUnknownKeys(YamlMappingNode mapping, string path, HashSet<string> knownKeys)
		{
			foreach (YamlNode itemKey in mapping.Children.Keys)
			{
				if (itemKey is YamlScalarNode { Value: { } itemName } && !knownKeys.Contains(itemName))
				{
					warnings.Add(UnknownKey(itemName, $"{path}.{itemName}", itemKey, firstYamlLine, knownKeys));
				}
			}
		}
	}

	private static string UnknownKey(string name, string shownName, YamlNode key, int firstYamlLine,
		HashSet<string> knownKeys)
	{
		long line = firstYamlLine + key.Start.Line - 1;
		string message = $"unknown front matter key '{shownName}' (line {line})";
		string? sameButCase = knownKeys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
		return sameButCase is null ? message : $"{message}; keys are case sensitive, did you mean '{sameButCase}'?";
	}

	private static HashSet<string> KeysOf<T>() =>
		typeof(T).GetProperties()
			.Select(p => CamelCaseNamingConvention.Instance.Apply(p.Name))
			.ToHashSet(StringComparer.Ordinal);

	private static Core.Content.FrontMatter DefaultFrontMatter(string title) => new() { Title = title };

	private static Core.Content.FrontMatter MapFromDto(FrontMatterDto? dto, List<(string Key, string Message)> problems)
	{
		if (dto is null)
		{
			return DefaultFrontMatter("Untitled");
		}

		return new Core.Content.FrontMatter
		{
			Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled" : dto.Title,
			Description = dto.Description ?? "",
			Order = dto.Order,
			Icon = dto.Icon,
			Layout = dto.Layout ?? "default",
			Tags = dto.Tags ?? [],
			Visibility = dto.Draft == true ? PageVisibility.Draft : ParseVisibility(dto.Visibility),
			Toc = dto.Toc ?? true,
			Expanded = dto.Expanded ?? true,
			Route = dto.Route,
			Version = VersionRangeText(dto.Version),
			Requires = dto.Requires,
			Features = MapFeatures(dto.Features),
			FeaturesTitle = dto.FeaturesTitle?.Trim() ?? "",
			FeaturesSubtitle = dto.FeaturesSubtitle?.Trim() ?? "",
			HeroCode = string.IsNullOrWhiteSpace(dto.HeroCode) ? null : dto.HeroCode.TrimEnd(),
			HeroCodeLanguage = string.IsNullOrWhiteSpace(dto.HeroCodeLanguage) ? null : dto.HeroCodeLanguage.Trim(),
			HeroImage = string.IsNullOrWhiteSpace(dto.HeroImage) ? null : dto.HeroImage.Trim(),
			ShowcaseCode = string.IsNullOrWhiteSpace(dto.ShowcaseCode) ? null : dto.ShowcaseCode.TrimEnd(),
			ShowcaseCodeLanguage = string.IsNullOrWhiteSpace(dto.ShowcaseCodeLanguage) ? null : dto.ShowcaseCodeLanguage.Trim(),
			ShowcaseTitle = dto.ShowcaseTitle?.Trim() ?? "",
			ShowcaseText = dto.ShowcaseText?.Trim() ?? "",
			Stats = MapStats(dto.Stats),
			Cta = MapCallToAction(dto.Cta),
			Date = MapDate(dto.Date, problems),
			Authors = [..MapNames("author", dto.Author, problems), ..MapNames("authors", dto.Authors, problems)],
			Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim(),
			Image = string.IsNullOrWhiteSpace(dto.Image) ? null : dto.Image.Trim()
		};
	}

	private static DateTimeOffset? MapDate(string? value, List<(string Key, string Message)> problems)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (TryParseDate(value, out DateTimeOffset date))
		{
			return date;
		}

		problems.Add(("date",
			$"date '{value}' is not an ISO 8601 date such as 2026-03-15 or 2026-03-15T09:30:00Z, so it was ignored"));
		return null;
	}

	/// <summary>
	///     Reads an ISO 8601 date (<c>2026-03-15</c>) or date and time (<c>2026-03-15T09:30</c>,
	///     <c>2026-03-15 09:30:00+02:00</c>, <c>2026-03-15T09:30:00Z</c>). A date or time without an
	///     offset is UTC. Other forms such as <c>03/15/2026</c> are refused, since their meaning
	///     depends on the culture.
	/// </summary>
	/// <param name="value">The text to read.</param>
	/// <param name="date">The date, when the text is one.</param>
	/// <returns><c>true</c> when the text is an ISO 8601 date or date and time.</returns>
	public static bool TryParseDate(string value, out DateTimeOffset date)
	{
		date = default;
		string text = value.Trim();
		return _isoDate.IsMatch(text)
		       && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
			       DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out date);
	}

	// A YAML scalar comes back as a string and a sequence as a list. A mapping (an inline author
	// object) is not supported: authors with a URL or avatar are defined in features.blog.authors.
	private static IEnumerable<string> MapNames(string key, object? value, List<(string Key, string Message)> problems)
	{
		switch (value)
		{
			case null:
				return [];
			case string name:
				return string.IsNullOrWhiteSpace(name) ? [] : [name.Trim()];
			case IEnumerable<object?> items when items.All(item => item is null or string):
				return items
					.OfType<string>()
					.Where(item => !string.IsNullOrWhiteSpace(item))
					.Select(item => item.Trim())
					.ToList();
			default:
				problems.Add((key,
					$"{key} must be a name or a list of names; define an author's URL and avatar under features.blog.authors"));
				return [];
		}
	}

	private static long KeyLine(string yaml, string key, int firstYamlLine)
	{
		var stream = new YamlStream();
		stream.Load(new StringReader(yaml));
		YamlNode? keyNode = stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode root
			? root.Children.Keys.FirstOrDefault(k => k is YamlScalarNode { Value: { } name } && name == key)
			: null;
		return keyNode is null ? firstYamlLine : firstYamlLine + keyNode.Start.Line - 1;
	}

	private static List<LandingFeature> MapFeatures(List<LandingFeatureDto?>? features)
	{
		var cards = new List<LandingFeature>();
		foreach (LandingFeatureDto? feature in features ?? [])
		{
			// A bare "-" list item deserializes to null and has nothing to show.
			if (feature is null)
			{
				continue;
			}

			cards.Add(new LandingFeature
			{
				Title = feature.Title?.Trim() ?? "",
				Description = feature.Description?.Trim() ?? "",
				Icon = string.IsNullOrWhiteSpace(feature.Icon) ? null : feature.Icon.Trim(),
				Link = string.IsNullOrWhiteSpace(feature.Link) ? null : feature.Link.Trim()
			});
		}

		return cards;
	}

	private static List<LandingStat> MapStats(List<LandingStatDto?>? stats)
	{
		var figures = new List<LandingStat>();
		foreach (LandingStatDto? stat in stats ?? [])
		{
			string value = stat?.Value?.Trim() ?? "";
			string label = stat?.Label?.Trim() ?? "";
			if (value.Length > 0 || label.Length > 0)
			{
				figures.Add(new LandingStat { Value = value, Label = label });
			}
		}

		return figures;
	}

	private static LandingCallToAction? MapCallToAction(LandingCallToActionDto? cta)
	{
		if (cta is null)
		{
			return null;
		}

		var action = new LandingCallToAction
		{
			Title = cta.Title?.Trim() ?? "",
			Text = cta.Text?.Trim() ?? "",
			Link = string.IsNullOrWhiteSpace(cta.Link) ? null : cta.Link.Trim(),
			LinkText = string.IsNullOrWhiteSpace(cta.LinkText) ? null : cta.LinkText.Trim()
		};
		return action.Title.Length > 0 || action.Text.Length > 0 || action.Link is not null ? action : null;
	}

	/// <summary>
	///     The <c>version</c> value as one range: a list becomes its items joined with <c>||</c>, which
	///     means the same thing. <c>null</c> for a mapping or a list holding anything but scalars.
	/// </summary>
	private static string? VersionRangeText(object? value) => value switch
	{
		string range => range,
		List<object?> items when items.TrueForAll(i => i is string) =>
			string.Join(" || ", items.Cast<string>().Select(i => i.Trim()).Where(i => i.Length > 0)),
		_ => null
	};

	private static PageVisibility ParseVisibility(string? value)
	{
		return value?.ToLowerInvariant() switch
		{
			"hidden" => PageVisibility.Hidden,
			"draft" => PageVisibility.Draft,
			_ => PageVisibility.Public
		};
	}

	#endregion
}

/// <summary>
///     The result of front matter extraction: the parsed metadata and remaining body.
/// </summary>
/// <param name="FrontMatter">The parsed front matter, or defaults when there was none or it failed to parse.</param>
/// <param name="Body">The Markdown after the front matter block.</param>
/// <param name="Error">
///     Why the front matter block could not be read, with its line and column in the file, or <c>null</c>.
/// </param>
public sealed record FrontMatterResult(Core.Content.FrontMatter FrontMatter, string Body, string? Error = null)
{
	/// <summary>
	///     Problems that did not stop the front matter from being read: unknown keys, or a block
	///     with no closing <c>---</c>. Messages give the line but not the file.
	/// </summary>
	public IReadOnlyList<string> Warnings { get; init; } = [];
}

#region Front Matter DTO

/// <summary>DTO for YAML front matter deserialization.</summary>
internal sealed class FrontMatterDto
{
	public string? Title { get; set; }
	public string? Description { get; set; }
	public int Order { get; set; }
	public string? Icon { get; set; }
	public string? Layout { get; set; }
	public List<string>? Tags { get; set; }
	public string? Visibility { get; set; }
	public bool? Toc { get; set; }
	public bool? Expanded { get; set; }
	public string? Route { get; set; }

	// A range, or a list of ranges of which any may match.
	public object? Version { get; set; }

	public string? Requires { get; set; }
	public List<LandingFeatureDto?>? Features { get; set; }
	public string? FeaturesTitle { get; set; }
	public string? FeaturesSubtitle { get; set; }
	public string? HeroCode { get; set; }
	public string? HeroCodeLanguage { get; set; }
	public string? HeroImage { get; set; }
	public string? ShowcaseCode { get; set; }
	public string? ShowcaseCodeLanguage { get; set; }
	public string? ShowcaseTitle { get; set; }
	public string? ShowcaseText { get; set; }
	public List<LandingStatDto?>? Stats { get; set; }
	public LandingCallToActionDto? Cta { get; set; }
	public string? Date { get; set; }
	public object? Author { get; set; }
	public object? Authors { get; set; }
	public string? Summary { get; set; }
	public string? Image { get; set; }
	public bool? Draft { get; set; }
}

/// <summary>DTO for one item of the <c>features</c> front matter list.</summary>
internal sealed class LandingFeatureDto
{
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? Icon { get; set; }
	public string? Link { get; set; }
}

/// <summary>DTO for one item of the <c>stats</c> front matter list.</summary>
internal sealed class LandingStatDto
{
	public string? Value { get; set; }
	public string? Label { get; set; }
}

/// <summary>DTO for the <c>cta</c> front matter mapping.</summary>
internal sealed class LandingCallToActionDto
{
	public string? Title { get; set; }
	public string? Text { get; set; }
	public string? Link { get; set; }
	public string? LinkText { get; set; }
}

#endregion
