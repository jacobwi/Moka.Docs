namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     Decides which namespaces are documented, from <see cref="MokaDocsOptions.IncludeNamespaces" />
///     and <see cref="MokaDocsOptions.ExcludeNamespaces" />.
/// </summary>
/// <remarks>
///     An entry matches its namespace and the ones below it, segment by segment, so
///     <c>Contoso.Api</c> matches <c>Contoso.Api.Models</c> but not <c>Contoso.Apis</c>. The global
///     namespace is <c>(global)</c>, the name its types are listed under, or an empty string; it
///     contains no other namespace. Matching is case-sensitive, like C# namespaces.
/// </remarks>
internal sealed class NamespaceFilter
{
	/// <summary>The name the API pages give the global namespace.</summary>
	public const string GlobalNamespace = "(global)";

	private readonly List<string> _exclude;
	private readonly List<string> _include;

	/// <summary>Creates a filter from the include and exclude lists.</summary>
	public NamespaceFilter(IEnumerable<string>? include, IEnumerable<string>? exclude)
	{
		_include = Normalize(include);
		_exclude = Normalize(exclude);
	}

	/// <summary>
	///     Whether types in the namespace are documented. Null, empty and <c>(global)</c> all mean
	///     the global namespace.
	/// </summary>
	public bool IsIncluded(string? ns)
	{
		string name = ns is null or GlobalNamespace ? "" : ns;
		if (_exclude.Any(entry => Matches(name, entry)))
		{
			return false;
		}

		return _include.Count == 0 || _include.Any(entry => Matches(name, entry));
	}

	private static bool Matches(string ns, string entry) =>
		entry.Length == 0
			? ns.Length == 0
			: ns.StartsWith(entry, StringComparison.Ordinal) && (ns.Length == entry.Length || ns[entry.Length] == '.');

	private static List<string> Normalize(IEnumerable<string>? entries) =>
		(entries ?? [])
		.Where(entry => entry is not null)
		.Select(entry => entry.Trim().TrimEnd('.'))
		.Select(entry => entry == GlobalNamespace ? "" : entry)
		.ToList();
}
