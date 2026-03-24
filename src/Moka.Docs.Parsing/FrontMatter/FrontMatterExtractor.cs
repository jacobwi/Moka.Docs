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
    private const string Delimiter = "---";

    /// <summary>
    ///     Extracts front matter from a Markdown string.
    /// </summary>
    /// <param name="markdown">The raw Markdown content (may include front matter).</param>
    /// <returns>The extracted front matter and the Markdown body after the front matter block.</returns>
    public FrontMatterResult Extract(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new FrontMatterResult(DefaultFrontMatter("Untitled"), "");

        var span = markdown.AsSpan().TrimStart();

        // Must start with ---
        if (!span.StartsWith(Delimiter)) return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown);

        // Find the closing ---
        var afterFirstDelimiter = span[3..];
        var closingIndex = FindClosingDelimiter(afterFirstDelimiter);

        if (closingIndex < 0)
            // No closing delimiter — treat entire content as body
            return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown);

        var yamlContent = afterFirstDelimiter[..closingIndex].ToString().Trim();
        var bodyStart = 3 + closingIndex + 3; // skip both --- delimiters
        var body = bodyStart < span.Length
            ? span[bodyStart..].ToString().TrimStart('\r', '\n')
            : "";

        if (string.IsNullOrWhiteSpace(yamlContent)) return new FrontMatterResult(DefaultFrontMatter("Untitled"), body);

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var dto = deserializer.Deserialize<FrontMatterDto>(yamlContent);
            var frontMatter = MapFromDto(dto);
            return new FrontMatterResult(frontMatter, body);
        }
        catch
        {
            // Malformed YAML — treat as no front matter
            return new FrontMatterResult(DefaultFrontMatter("Untitled"), markdown);
        }
    }

    #region Private Helpers

    private static int FindClosingDelimiter(ReadOnlySpan<char> content)
    {
        var index = 0;
        while (index < content.Length)
        {
            // Skip to next newline
            var newlineIndex = content[index..].IndexOf('\n');
            if (newlineIndex < 0) break;

            index += newlineIndex + 1;

            // Check if next line starts with ---
            var remaining = content[index..];
            var trimmed = remaining.TrimStart([' ', '\t']);
            if (trimmed.StartsWith(Delimiter))
            {
                // Verify it's just --- (possibly with trailing whitespace)
                var lineEnd = trimmed.IndexOfAny('\r', '\n');
                var line = lineEnd >= 0 ? trimmed[..lineEnd] : trimmed;
                if (line.TrimEnd().Length == 3) return index;
            }
        }

        return -1;
    }

    private static Core.Content.FrontMatter DefaultFrontMatter(string title)
    {
        return new Core.Content.FrontMatter { Title = title };
    }

    private static Core.Content.FrontMatter MapFromDto(FrontMatterDto? dto)
    {
        if (dto is null) return DefaultFrontMatter("Untitled");

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
            Requires = dto.Requires
        };
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
public sealed record FrontMatterResult(Core.Content.FrontMatter FrontMatter, string Body);

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
}

#endregion