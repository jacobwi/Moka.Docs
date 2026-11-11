using System.Runtime.CompilerServices;
using System.Text;

namespace Moka.Docs.Core.Api;

/// <summary>
///     The routes and display names of API type pages. The CLI, the ASP.NET Core host and every link to a
///     type page use it, so a link always reaches the page it names.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 A type's page is at <c>/api/</c>, its namespace with dots turned into slashes, then its name,
///                 all lowercase: <c>/api/mylib/text/parser</c>. Types outside any namespace are under
///                 <c>(global)</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 A nested type's name follows its containing types, joined with dots:
///                 <c>Outer.Options</c> is at <c>/api/mylib/outer.options</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 When types with the same namespace and containing type (or none) share a name, as
///                 <c>Result</c> and <c>Result&lt;T&gt;</c> do, the one with the fewest type parameters keeps
///                 the plain name and each other one gets a hyphen and its number of type parameters:
///                 <c>/api/mylib/result-1</c>. A generic type without such a twin keeps the plain name.
///             </description>
///         </item>
///     </list>
///     The display name is the name with its containing types and type parameters, as C# writes it:
///     <c>Outer.Options</c>, <c>Result&lt;T&gt;</c>.
/// </remarks>
public sealed class ApiTypeRoutes
{
	private const string _globalNamespace = "(global)";

	private static readonly ConditionalWeakTable<IReadOnlyList<ApiType>, ApiTypeRoutes> _cache = new();

	private readonly Dictionary<ApiType, Entry> _entries = new(ReferenceEqualityComparer.Instance);

	private ApiTypeRoutes(IEnumerable<ApiType> types)
	{
		foreach (IGrouping<string, ApiType> group in types
			         .Distinct<ApiType>(ReferenceEqualityComparer.Instance)
			         .GroupBy(t => t.Namespace ?? "", StringComparer.Ordinal))
		{
			new NamespaceRoutes(group.ToList(), _entries).Build();
		}
	}

	/// <summary>
	///     Gets the routes for a list of types: all the documented types, or the types of one namespace.
	///     Routes depend only on the other types of the same namespace, so both give the same answer.
	/// </summary>
	/// <param name="types">The types. The result is cached for the list instance.</param>
	/// <returns>The routes of those types.</returns>
	public static ApiTypeRoutes For(IReadOnlyList<ApiType> types) =>
		_cache.GetValue(types, list => new ApiTypeRoutes(list));

	/// <summary>
	///     The route of a type's page, such as <c>/api/mylib/outer.options</c>.
	/// </summary>
	/// <param name="ns">The namespace the type belongs to.</param>
	/// <param name="type">The type.</param>
	/// <returns>The route, without any base path.</returns>
	public static string Route(ApiNamespace ns, ApiType type) => For(ns.Types).Route(type);

	/// <summary>
	///     The route of a type's page, such as <c>/api/mylib/outer.options</c>. A type that isn't in the
	///     list gets the route it would have on its own.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The route, without any base path.</returns>
	public string Route(ApiType type)
	{
		string ns = type.Namespace is { Length: > 0 } name ? name : _globalNamespace;
		return $"/api/{ns.Replace('.', '/')}/{Find(type).Segment}".ToLowerInvariant();
	}

	/// <summary>
	///     The type's name as C# writes it, with its containing types and type parameters but no namespace:
	///     <c>Outer.Options</c>, <c>Result&lt;T&gt;</c>, <c>Outer&lt;T&gt;.Inner</c>.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The display name.</returns>
	public string DisplayName(ApiType type) => Find(type).DisplayName;

	/// <summary>
	///     The type's documentation ID without its <c>T:</c> prefix, such as <c>MyLib.Outer`1.Inner</c>.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The documentation ID.</returns>
	public string DocumentationId(ApiType type)
	{
		string id = Find(type).IdPath;
		return type.Namespace is { Length: > 0 } ns ? $"{ns}.{id}" : id;
	}

	private Entry Find(ApiType type) =>
		_entries.TryGetValue(type, out Entry? entry) ? entry : new ApiTypeRoutes([type])._entries[type];

	/// <summary>
	///     The full name without the namespace: <c>Outer&lt;T&gt;.Inner</c> from the CLI, which writes type
	///     parameters, or <c>Outer.Inner</c> from the ASP.NET Core host, which doesn't.
	/// </summary>
	private static string PathOf(ApiType type)
	{
		string fullName = type.FullName;
		if (type.Namespace is { Length: > 0 } ns && fullName.StartsWith(ns + ".", StringComparison.Ordinal))
		{
			return fullName[(ns.Length + 1)..];
		}

		return fullName.Length > 0 ? fullName : type.Name;
	}

	/// <summary>
	///     Splits a path at the dots between type names, leaving dots inside type argument lists alone.
	/// </summary>
	private static List<string> SplitPath(string path)
	{
		var parts = new List<string>();
		int depth = 0;
		int start = 0;
		for (int i = 0; i < path.Length; i++)
		{
			switch (path[i])
			{
				case '<':
					depth++;
					break;
				case '>':
					depth--;
					break;
				case '.' when depth == 0:
					parts.Add(path[start..i]);
					start = i + 1;
					break;
			}
		}

		parts.Add(path[start..]);
		return parts;
	}

	private static bool IsPrefix(List<ApiTypeParameter> prefix, List<ApiTypeParameter> list) =>
		prefix.Count <= list.Count
		&& prefix.Select(p => p.Name).SequenceEqual(list.Take(prefix.Count).Select(p => p.Name), StringComparer.Ordinal);

	private static string StripTypeArguments(string name)
	{
		int open = name.IndexOf('<');
		return open >= 0 ? name[..open] : name;
	}

	// Outer<T, U> becomes Outer`2, as documentation IDs write it.
	private static string WithArity(string name)
	{
		int open = name.IndexOf('<');
		if (open < 0)
		{
			return name;
		}

		int depth = 0;
		int arity = 1;
		foreach (char c in name[open..])
		{
			switch (c)
			{
				case '<':
					depth++;
					break;
				case '>':
					depth--;
					break;
				case ',' when depth == 1:
					arity++;
					break;
			}
		}

		return $"{name[..open]}`{arity}";
	}

	private static string SanitizeSegment(string name)
	{
		var sb = new StringBuilder(name.Length);
		foreach (char c in name)
		{
			sb.Append(c is '<' or '>' or '`' ? '-' : c);
		}

		return sb.ToString().TrimEnd('-');
	}

	private sealed record Entry(string Segment, string DisplayName, string IdPath);

	/// <summary>
	///     Works out the entries of one namespace's types, which only depend on each other.
	/// </summary>
	private sealed class NamespaceRoutes(List<ApiType> types, Dictionary<ApiType, Entry> entries)
	{
		private readonly int[] _containers = new int[types.Count];
		private readonly Dictionary<(int Container, string Name), int> _fewestTypeParameters = [];
		private readonly List<string>[] _ownTypeParameters = new List<string>[types.Count];
		private readonly List<string>[] _paths = new List<string>[types.Count];

		public void Build()
		{
			var byPath = new Dictionary<string, List<int>>(StringComparer.Ordinal);
			for (int i = 0; i < types.Count; i++)
			{
				_paths[i] = SplitPath(PathOf(types[i]));
				string key = string.Join('.', _paths[i]);
				if (!byPath.TryGetValue(key, out List<int>? samePath))
				{
					samePath = [];
					byPath[key] = samePath;
				}

				samePath.Add(i);
			}

			for (int i = 0; i < types.Count; i++)
			{
				_containers[i] = FindContainer(i, byPath);
			}

			for (int i = 0; i < types.Count; i++)
			{
				_ownTypeParameters[i] = OwnTypeParameters(i);

				// Routes are lowercase, so names that differ only in case count as the same name.
				(int, string) key = (_containers[i], types[i].Name.ToLowerInvariant());
				int count = _ownTypeParameters[i].Count;
				_fewestTypeParameters[key] = _fewestTypeParameters.TryGetValue(key, out int fewest)
					? Math.Min(fewest, count)
					: count;
			}

			var states = new byte[types.Count];
			for (int i = 0; i < types.Count; i++)
			{
				Resolve(i, states);
			}
		}

		private int FindContainer(int index, Dictionary<string, List<int>> byPath)
		{
			List<string> path = _paths[index];
			if (path.Count < 2 || !byPath.TryGetValue(string.Join('.', path.SkipLast(1)), out List<int>? candidates))
			{
				return -1;
			}

			if (candidates.Count == 1)
			{
				return candidates[0];
			}

			// The host's full names leave type parameters out, so Outer and Outer<T> share one. Its nested
			// types repeat the type parameters of their containing types first, which tells the two apart.
			ApiType type = types[index];
			return candidates
				.Where(c => IsPrefix(types[c].TypeParameters, type.TypeParameters))
				.OrderByDescending(c => types[c].TypeParameters.Count)
				.DefaultIfEmpty(candidates[0])
				.First();
		}

		/// <summary>
		///     The type parameters a type declares itself. The CLI lists only those; the host also lists the
		///     ones a nested type takes from its containing types, first.
		/// </summary>
		private List<string> OwnTypeParameters(int index)
		{
			ApiType type = types[index];
			IEnumerable<ApiTypeParameter> own = type.TypeParameters;
			bool listsInherited = !type.FullName.Contains('<');
			if (listsInherited && _containers[index] >= 0
			                   && types[_containers[index]].TypeParameters is var outer
			                   && IsPrefix(outer, type.TypeParameters))
			{
				own = own.Skip(outer.Count);
			}

			return own.Select(tp => tp.Name).ToList();
		}

		private Entry Resolve(int index, byte[] states)
		{
			ApiType type = types[index];
			if (entries.TryGetValue(type, out Entry? built))
			{
				return built;
			}

			// A containing type can't contain itself, but a cycle from odd input must not recurse forever.
			states[index] = 1;
			int container = _containers[index];
			Entry? outer = container >= 0 && states[container] != 1 ? Resolve(container, states) : null;

			List<string> own = _ownTypeParameters[index];
			int fewest = _fewestTypeParameters[(container, type.Name.ToLowerInvariant())];
			string name = SanitizeSegment(type.Name);
			string segment = own.Count > fewest ? $"{name}-{own.Count}" : name;
			string display = own.Count > 0 ? $"{type.Name}<{string.Join(", ", own)}>" : type.Name;
			string idPath = own.Count > 0 ? $"{type.Name}`{own.Count}" : type.Name;

			Entry entry;
			if (outer is not null)
			{
				entry = new Entry($"{outer.Segment}.{segment}", $"{outer.DisplayName}.{display}", $"{outer.IdPath}.{idPath}");
			}
			else if (_paths[index].Count > 1)
			{
				// A nested type whose containing type isn't in the list keeps the names its full name gives.
				List<string> outerPath = _paths[index][..^1];
				entry = new Entry(
					$"{SanitizeSegment(string.Join('.', outerPath.Select(StripTypeArguments)))}.{segment}",
					$"{string.Join('.', outerPath)}.{display}",
					$"{string.Join('.', outerPath.Select(WithArity))}.{idPath}");
			}
			else
			{
				entry = new Entry(segment, display, idPath);
			}

			entries[type] = entry;
			states[index] = 2;
			return entry;
		}
	}
}
