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

	private readonly List<Entry> _exclude;
	private readonly List<Entry> _include;

	/// <summary>Creates a filter from the include and exclude lists.</summary>
	public NamespaceFilter(IEnumerable<string>? include, IEnumerable<string>? exclude)
	{
		_include = Normalize(include, nameof(MokaDocsOptions.IncludeNamespaces));
		_exclude = Normalize(exclude, nameof(MokaDocsOptions.ExcludeNamespaces));
	}

	/// <summary>
	///     Whether types in the namespace are documented. Null, empty and <c>(global)</c> all mean
	///     the global namespace.
	/// </summary>
	public bool IsIncluded(string? ns)
	{
		string name = ToName(ns);
		if (_exclude.Any(entry => Matches(name, entry.Namespace)))
		{
			return false;
		}

		return _include.Count == 0 || _include.Any(entry => Matches(name, entry.Namespace));
	}

	// An entry that matches no namespace is usually a typo, and one with '*' expects a wildcard.
	public IEnumerable<string> CheckEntries(IEnumerable<string?> namespaces)
	{
		var names = new HashSet<string>(namespaces.Select(ToName), StringComparer.Ordinal);

		foreach (Entry entry in _include.Concat(_exclude))
		{
			if (entry.Text.Contains('*'))
			{
				yield return $"{entry.List} entry '{entry.Text}' contains '*', which is not a wildcard. " +
				             "An entry already matches the namespaces below it, so write the namespace without '.*'.";
			}
			else if (!names.Any(name => Matches(name, entry.Namespace)))
			{
				yield return $"{entry.List} entry '{entry.Text}' matches no namespace in Assemblies. " +
				             "Entries match namespaces case-sensitively, segment by segment.";
			}
		}
	}

	private static string ToName(string? ns) => ns is null or GlobalNamespace ? "" : ns;

	private static bool Matches(string ns, string entry) =>
		entry.Length == 0
			? ns.Length == 0
			: ns.StartsWith(entry, StringComparison.Ordinal) && (ns.Length == entry.Length || ns[entry.Length] == '.');

	private static List<Entry> Normalize(IEnumerable<string>? entries, string list) =>
		(entries ?? [])
		.Where(entry => entry is not null)
		.Select(entry => new Entry(entry, ToName(entry.Trim().TrimEnd('.')), list))
		.ToList();

	private sealed record Entry(string Text, string Namespace, string List);
}
