using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     The content of <c>search-index.json</c>. <c>RenderPhase</c> hashes it into the URL pages fetch the
///     index from and <c>OutputPhase</c> writes it, so the serialization has to give the same bytes both
///     times.
/// </summary>
internal static class SearchIndexFile
{
	public const string FileName = "search-index.json";

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	/// <summary>The file's bytes, or <c>null</c> when the build has no search entries and writes no file.</summary>
	public static byte[]? Bytes(BuildContext context)
	{
		SearchIndex? searchIndex = context.SearchIndex;
		if (searchIndex is null || searchIndex.Count == 0)
		{
			return null;
		}

		// Compact field names keep the payload small.
		string basePath = context.Config.Build.BasePath;
		var entries = searchIndex.Entries.Select(e => new
		{
			t = e.Title,
			s = e.Section ?? "",
			r = basePath == "/" ? e.Route : basePath + e.Route,
			c = e.Content.Length > 300 ? e.Content[..300] : e.Content,
			x = e.Code,
			g = e.Category,
			// Front matter tags. They were collected but never written, so tags did not help
			// anyone find a page. Null is omitted from the JSON.
			k = e.Tags.Count > 0 ? string.Join(' ', e.Tags) : null
		});

		return new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(entries, _jsonOptions));
	}
}
