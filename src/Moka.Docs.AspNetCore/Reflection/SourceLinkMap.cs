using System.Globalization;
using System.Text.Json;

namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     The document map of a SourceLink JSON file: local source paths, or path prefixes ending in
///     <c>*</c>, mapped to the URLs the files can be downloaded from.
/// </summary>
/// <remarks>
///     Matching follows the debuggers: paths compare without regard to case, the longest matching
///     prefix wins, and the rest of the path replaces the <c>*</c> in the URL with each segment
///     escaped and joined by <c>/</c>.
/// </remarks>
internal sealed class SourceLinkMap
{
	private readonly List<Entry> _entries;

	private SourceLinkMap(List<Entry> entries) => _entries = entries;

	/// <summary>
	///     Reads a SourceLink JSON document. Returns null when it isn't valid JSON or maps nothing.
	/// </summary>
	public static SourceLinkMap? Parse(string json)
	{
		var entries = new List<Entry>();
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			if (document.RootElement.ValueKind != JsonValueKind.Object
			    || !document.RootElement.TryGetProperty("documents", out JsonElement documents)
			    || documents.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			foreach (JsonProperty property in documents.EnumerateObject())
			{
				if (property.Value.GetString() is { Length: > 0 } url && TryCreateEntry(property.Name, url) is { } entry)
				{
					entries.Add(entry);
				}
			}
		}
		catch (JsonException)
		{
			return null;
		}

		if (entries.Count == 0)
		{
			return null;
		}

		entries.Sort((a, b) => b.Path.Length.CompareTo(a.Path.Length));
		return new SourceLinkMap(entries);
	}

	/// <summary>
	///     The download URL of a document, or null when no entry maps its path.
	/// </summary>
	public string? GetUrl(string documentPath)
	{
		foreach (Entry entry in _entries)
		{
			if (!entry.IsPrefix)
			{
				if (string.Equals(documentPath, entry.Path, StringComparison.OrdinalIgnoreCase))
				{
					return entry.UrlPrefix + entry.UrlSuffix;
				}

				continue;
			}

			if (documentPath.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase))
			{
				string relative = string.Join('/', documentPath[entry.Path.Length..]
					.Split('/', '\\')
					.Select(Uri.EscapeDataString));
				return entry.UrlPrefix + relative + entry.UrlSuffix;
			}
		}

		return null;
	}

	/// <summary>
	///     The page a reader can browse: the file view at <paramref name="line" /> for GitHub and
	///     GitLab, whose SourceLink URLs point at raw file content. Any other URL is returned as is,
	///     because a raw file has no line anchors.
	/// </summary>
	public static string ToBrowseUrl(string rawUrl, int? line)
	{
		string anchor = line is { } l ? "#L" + l.ToString(CultureInfo.InvariantCulture) : "";

		// https://raw.githubusercontent.com/{owner}/{repo}/{commit}/{path}
		if (Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri)
		    && uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
		    && string.IsNullOrEmpty(uri.Query))
		{
			string[] parts = uri.AbsolutePath.TrimStart('/').Split('/', 4);
			if (parts.Length == 4 && parts.All(p => p.Length > 0))
			{
				return $"https://github.com/{parts[0]}/{parts[1]}/blob/{parts[2]}/{parts[3]}{anchor}";
			}
		}

		// GitLab, including self-hosted instances: https://{host}/{group}/{project}/-/raw/{commit}/{path}
		const string gitLabRaw = "/-/raw/";
		int raw = rawUrl.IndexOf(gitLabRaw, StringComparison.Ordinal);
		if (raw > 0 && !rawUrl.Contains('?') && !rawUrl.Contains('#'))
		{
			return rawUrl[..raw] + "/-/blob/" + rawUrl[(raw + gitLabRaw.Length)..] + anchor;
		}

		return rawUrl;
	}

	private static Entry? TryCreateEntry(string path, string url)
	{
		bool isPrefix = path.EndsWith('*');
		int star = url.IndexOf('*');

		// A prefix needs somewhere to put the rest of the path, and an exact path has no rest.
		if (isPrefix != star >= 0)
		{
			return null;
		}

		return isPrefix
			? new Entry(path[..^1], true, url[..star], url[(star + 1)..])
			: new Entry(path, false, url, "");
	}

	private sealed record Entry(string Path, bool IsPrefix, string UrlPrefix, string UrlSuffix);
}
