using Moka.Docs.Core.Theming;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     What a page's Markdown can point at outside itself: API types and members for <c>xref:</c> links, the
///     site's glossary for <c>:term[...]</c> and <c>:::glossary</c>, and known package versions for
///     <c>:::package</c>. The build makes one for all pages; <see cref="None" /> knows nothing.
/// </summary>
/// <remarks>Shared by pages parsed at the same time, so whatever it holds must be safe to read from several threads.</remarks>
public sealed class ContentReferences
{
	private static readonly IReadOnlyDictionary<string, string> _empty = new Dictionary<string, string>();

	private readonly Lazy<Dictionary<string, string>> _glossaryLookup;
	private readonly Lazy<List<KeyValuePair<string, string>>> _sortedGlossary;

	/// <summary>Creates an empty set of references; fill it with the init properties.</summary>
	public ContentReferences()
	{
		_glossaryLookup = new Lazy<Dictionary<string, string>>(() =>
		{
			var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach ((string term, string definition) in Glossary)
			{
				lookup.TryAdd(term.Trim(), definition);
			}

			return lookup;
		});
		_sortedGlossary = new Lazy<List<KeyValuePair<string, string>>>(() => _glossaryLookup.Value
			.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
			.ThenBy(entry => entry.Key, StringComparer.Ordinal)
			.ToList());
	}

	/// <summary>References that resolve nothing: every <c>xref:</c> and <c>:term[...]</c> is reported as unknown.</summary>
	public static ContentReferences None { get; } = new();

	/// <summary>
	///     Resolves the target of an <c>xref:</c> link (the text after <c>xref:</c>, such as
	///     <c>MyLib.Parser.Parse</c>) to a URL and a display text, or returns <c>null</c> when nothing matches.
	///     A URL starting with <c>/</c> is a site route without the base path.
	/// </summary>
	public Func<string, XrefTarget?>? ResolveXref { get; init; }

	/// <summary>Glossary terms and their definitions. Terms match case-insensitively.</summary>
	public IReadOnlyDictionary<string, string> Glossary { get; init; } = _empty;

	/// <summary>
	///     Versions for <c>:::package</c> blocks that don't give one, by package ID (case-insensitive), such as
	///     the version of the documented project's package.
	/// </summary>
	public IReadOnlyDictionary<string, string> PackageVersions { get; init; } = _empty;

	/// <summary>
	///     The site's text for what the parser writes itself: admonition and <c>:::details</c> titles, the install
	///     widget of <c>:::package</c>, video and file tree names for screen readers, and code block labels.
	///     Defaults to English.
	/// </summary>
	public ThemeText Text { get; init; } = ThemeText.English;

	internal string? Define(string term) =>
		_glossaryLookup.Value.TryGetValue(term.Trim(), out string? definition) ? definition : null;

	internal IReadOnlyList<KeyValuePair<string, string>> SortedGlossary => _sortedGlossary.Value;

	internal string? VersionOf(string packageId)
	{
		foreach ((string id, string version) in PackageVersions)
		{
			if (string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase))
			{
				return version;
			}
		}

		return null;
	}
}

/// <summary>Where an <c>xref:</c> link goes and the text it shows when the link has none of its own.</summary>
/// <param name="Url">A site route (starting with <c>/</c>, without the base path) or an absolute URL.</param>
/// <param name="Text">The display text, such as <c>SiteConfig</c> or <c>SiteConfig.Build</c>.</param>
public sealed record XrefTarget(string Url, string Text);
