using Moka.Docs.Core.Content;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Parsing.FrontMatter;

/// <summary>
///     Extracts and parses YAML front matter from the top of Markdown files.
///     Returns the parsed <see cref="Core.Content.FrontMatter" /> and the remaining Markdown body.
/// </summary>
public sealed class FrontMatterExtractor
{
	private const string _delimiter = "---";

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

		ReadOnlySpan<char> span = markdown.AsSpan().TrimStart();

		// Must start with ---
		if (!span.StartsWith(_delimiter))
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown);
		}

		// Find the closing ---
		ReadOnlySpan<char> afterFirstDelimiter = span[3..];
		int closingIndex = FindClosingDelimiter(afterFirstDelimiter);

		if (closingIndex < 0)
			// No closing delimiter - treat entire content as body
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown);
		}

		string yamlContent = afterFirstDelimiter[..closingIndex].ToString().Trim();
		int bodyStart = 3 + closingIndex + 3; // skip both --- delimiters
		string body = bodyStart < span.Length
			? span[bodyStart..].ToString().TrimStart('\r', '\n')
			: "";

		if (string.IsNullOrWhiteSpace(yamlContent))
		{
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), body);
		}

		try
		{
			IDeserializer deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.IgnoreUnmatchedProperties()
				.Build();

			FrontMatterDto dto = deserializer.Deserialize<FrontMatterDto>(yamlContent);
			Core.Content.FrontMatter frontMatter = MapFromDto(dto);
			return new FrontMatterResult(frontMatter, body);
		}
		catch (Exception ex)
		{
			// Malformed YAML, or a value of the wrong type (order: first). The page keeps
			// building without front matter; the error is handed back so the build can say so.
			return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown, ex.Message);
		}
	}

	#region Private Helpers

	private static int FindClosingDelimiter(ReadOnlySpan<char> content)
	{
		int index = 0;
		while (index < content.Length)
		{
			// Skip to next newline
			int newlineIndex = content[index..].IndexOf('\n');
			if (newlineIndex < 0)
			{
				break;
			}

			index += newlineIndex + 1;

			// Check if next line starts with ---
			ReadOnlySpan<char> remaining = content[index..];
			ReadOnlySpan<char> trimmed = remaining.TrimStart([' ', '\t']);
			if (trimmed.StartsWith(_delimiter))
			{
				// Verify it's just --- (possibly with trailing whitespace)
				int lineEnd = trimmed.IndexOfAny('\r', '\n');
				ReadOnlySpan<char> line = lineEnd >= 0 ? trimmed[..lineEnd] : trimmed;
				if (line.TrimEnd().Length == 3)
				{
					return index;
				}
			}
		}

		return -1;
	}

	private static Core.Content.FrontMatter DefaultFrontMatter(string title) => new() { Title = title };

	private static Core.Content.FrontMatter MapFromDto(FrontMatterDto? dto)
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
			Visibility = ParseVisibility(dto.Visibility),
			Toc = dto.Toc ?? true,
			Expanded = dto.Expanded ?? true,
			Route = dto.Route,
			Version = dto.Version,
			Requires = dto.Requires,
			Features = MapFeatures(dto.Features),
			FeaturesTitle = dto.FeaturesTitle?.Trim() ?? "",
			FeaturesSubtitle = dto.FeaturesSubtitle?.Trim() ?? ""
		};
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
/// <param name="FrontMatter">The parsed front matter metadata.</param>
/// <param name="Body">The Markdown body content after the front matter block.</param>
/// <param name="FrontMatter">The parsed front matter, or defaults when there was none or it failed to parse.</param>
/// <param name="Body">The Markdown after the front matter block.</param>
/// <param name="Error">Why the front matter block could not be read, or <c>null</c>.</param>
public sealed record FrontMatterResult(Core.Content.FrontMatter FrontMatter, string Body, string? Error = null);

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
	public string? Version { get; set; }
	public string? Requires { get; set; }
	public List<LandingFeatureDto?>? Features { get; set; }
	public string? FeaturesTitle { get; set; }
	public string? FeaturesSubtitle { get; set; }
}

/// <summary>DTO for one item of the <c>features</c> front matter list.</summary>
internal sealed class LandingFeatureDto
{
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? Icon { get; set; }
	public string? Link { get; set; }
}

#endregion
