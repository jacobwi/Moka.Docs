using System.Text;
using Moka.Docs.Core.Api;

namespace Moka.Docs.CSharp.XmlDoc;

/// <summary>
///     Resolves <c>&lt;inheritdoc/&gt;</c> references by finding documentation
///     from base types and implemented interfaces.
/// </summary>
/// <remarks>
///     A type or member inherits when it has no summary, or when its comment has an
///     <c>&lt;inheritdoc/&gt;</c> tag. Tags it documents itself are kept.
///     <list type="bullet">
///         <item>
///             <description>
///                 With <c>&lt;inheritdoc cref="..."/&gt;</c>, the documentation comes from the type or
///                 member the cref names.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Otherwise, or when the cref names nothing in the model, the base type chain is
///                 searched nearest first, then the interfaces: the type's own, those of each base
///                 type in turn, then the interfaces those extend. The first match with a summary
///                 wins, and a match that inherits its own documentation is resolved first.
///             </description>
///         </item>
///     </list>
///     Members match by name, kind and parameter count, preferring identical parameter types
///     when overloads share a count. Only types in the API model are searched.
/// </remarks>
public sealed class InheritDocResolver
{
	/// <summary>
	///     Resolves any missing documentation on types and members by walking
	///     base types and interfaces in the API reference.
	/// </summary>
	/// <param name="reference">The API reference to resolve.</param>
	/// <returns>A new API reference with inheritdoc resolved.</returns>
	public ApiReference Resolve(ApiReference reference)
	{
		var resolution = new Resolution(reference.Namespaces.SelectMany(ns => ns.Types));

		var resolvedNamespaces = reference.Namespaces.Select(ns => ns with
		{
			Types = ns.Types.Select(type => type with
			{
				Documentation = resolution.TypeDoc(type),
				Members = type.Members
					.Select(member => member with { Documentation = resolution.MemberDoc(type, member) })
					.ToList()
			}).ToList()
		}).ToList();

		return reference with { Namespaces = resolvedNamespaces };
	}

	/// <summary>
	///     Resolved documentation, memoized per type and member so a shared ancestor is resolved once.
	/// </summary>
	private sealed class Resolution
	{
		private readonly Dictionary<string, List<ApiType>> _bySimpleName = new(StringComparer.Ordinal);
		private readonly Dictionary<string, ApiType> _byTypeKey = new(StringComparer.Ordinal);
		private readonly HashSet<object> _inProgress = new(ReferenceEqualityComparer.Instance);
		private readonly Dictionary<ApiMember, XmlDocBlock?> _memberDocs = new(ReferenceEqualityComparer.Instance);
		private readonly Dictionary<ApiType, XmlDocBlock?> _typeDocs = new(ReferenceEqualityComparer.Instance);

		public Resolution(IEnumerable<ApiType> types)
		{
			foreach (ApiType type in types)
			{
				_byTypeKey.TryAdd(TypeKey(type.FullName), type);

				string simpleName = type.TypeParameters.Count > 0 ? $"{type.Name}`{type.TypeParameters.Count}" : type.Name;
				if (!_bySimpleName.TryGetValue(simpleName, out List<ApiType>? sameName))
				{
					sameName = [];
					_bySimpleName[simpleName] = sameName;
				}

				sameName.Add(type);
			}
		}

		public XmlDocBlock? TypeDoc(ApiType type)
		{
			if (_typeDocs.TryGetValue(type, out XmlDocBlock? resolved))
			{
				return resolved;
			}

			XmlDocBlock? own = type.Documentation;
			if (!NeedsInheritance(own) || !_inProgress.Add(type))
			{
				return own;
			}

			XmlDocBlock? inherited = FromCref(own?.InheritDocCref)
			                         ?? Ancestors(type).Select(TypeDoc).FirstOrDefault(HasSummary);

			_inProgress.Remove(type);
			resolved = Merge(own, inherited);
			_typeDocs[type] = resolved;
			return resolved;
		}

		public XmlDocBlock? MemberDoc(ApiType owner, ApiMember member)
		{
			if (_memberDocs.TryGetValue(member, out XmlDocBlock? resolved))
			{
				return resolved;
			}

			XmlDocBlock? own = member.Documentation;
			if (!NeedsInheritance(own) || !_inProgress.Add(member))
			{
				return own;
			}

			XmlDocBlock? inherited = FromCref(own?.InheritDocCref) ?? FromAncestors(owner, member);

			_inProgress.Remove(member);
			resolved = Merge(own, inherited);
			_memberDocs[member] = resolved;
			return resolved;
		}

		private XmlDocBlock? FromAncestors(ApiType owner, ApiMember member)
		{
			foreach (ApiType ancestor in Ancestors(owner))
			{
				if (FindMatchingMember(ancestor, member) is { } match
				    && MemberDoc(ancestor, match) is { } doc
				    && HasSummary(doc))
				{
					return doc;
				}
			}

			return null;
		}

		/// <summary>
		///     Base types nearest first, then interfaces: the type's own, those of each base type,
		///     then the interfaces those extend.
		/// </summary>
		/// <remarks>
		///     Only the direct base type and the type's own interfaces used to be searched, so a
		///     member documented on a grandparent, on an interface a base type implements, or on an
		///     interface the implemented one extends rendered blank.
		/// </remarks>
		private IEnumerable<ApiType> Ancestors(ApiType type)
		{
			var visited = new HashSet<ApiType>(ReferenceEqualityComparer.Instance) { type };
			var baseTypes = new List<ApiType>();
			ApiType current = type;
			while (current.BaseType is not null && Find(current.BaseType) is { } baseType && visited.Add(baseType))
			{
				baseTypes.Add(baseType);
				current = baseType;
			}

			foreach (ApiType baseType in baseTypes)
			{
				yield return baseType;
			}

			var pending = new Queue<string>(type.ImplementedInterfaces.Concat(
				baseTypes.SelectMany(baseType => baseType.ImplementedInterfaces)));
			while (pending.Count > 0)
			{
				if (Find(pending.Dequeue()) is not { } iface || !visited.Add(iface))
				{
					continue;
				}

				yield return iface;
				foreach (string extended in iface.ImplementedInterfaces)
				{
					pending.Enqueue(extended);
				}
			}
		}

		/// <summary>
		///     Takes the documentation the cref names. The cref used to be ignored, so the docs came
		///     from a same-named base member or from nowhere.
		/// </summary>
		private XmlDocBlock? FromCref(string? cref)
		{
			// "!:" marks a cref Roslyn could not bind, which names nothing to look up.
			if (string.IsNullOrEmpty(cref) || MemberIdParser.Parse(cref) is not { } id || id.Kind == MemberIdKind.Error)
			{
				return null;
			}

			if (id.Kind == MemberIdKind.Type)
			{
				return Find(id.FullName) is { } type ? TypeDoc(type) : null;
			}

			if (id.ContainingType is null || Find(id.ContainingType) is not { } owner)
			{
				return null;
			}

			return FindMember(owner, id) is { } member ? MemberDoc(owner, member) : null;
		}

		/// <summary>
		///     Looks a type reference up by full name, then by simple name when the reference is
		///     unqualified and exactly one type carries that name.
		/// </summary>
		/// <remarks>
		///     Each project is compiled on its own, so a type defined in another analyzed
		///     project is unresolved in that compilation. Roslyn renders it without its
		///     namespace and cannot tell an interface from a class, so
		///     <c>DiscoveryPhase : IBuildPhase</c> arrives as base type <c>IBuildPhase</c>
		///     rather than interface <c>Moka.Docs.Core.Pipeline.IBuildPhase</c>. Before this
		///     fallback, every member documented with <c>&lt;inheritdoc/&gt;</c> across a
		///     project boundary rendered with no description.
		///     <para>
		///         Qualified references are never matched by simple name: an unfound
		///         <c>System.Exception</c> must not attach to a user type that happens to be
		///         called <c>Exception</c>.
		///     </para>
		/// </remarks>
		private ApiType? Find(string reference)
		{
			string key = TypeKey(reference);
			if (_byTypeKey.TryGetValue(key, out ApiType? type))
			{
				return type;
			}

			return !key.Contains('.')
			       && _bySimpleName.TryGetValue(key, out List<ApiType>? candidates)
			       && candidates.Count == 1
				? candidates[0]
				: null;
		}

		/// <summary>
		///     The key a type is looked up by: its name with each type argument list replaced by its
		///     arity, the way documentation IDs write generics. <c>MyLib.Repository&lt;T&gt;</c>,
		///     <c>MyLib.Repository&lt;MyLib.User&gt;</c> and the ID <c>T:MyLib.Repository`1</c> all
		///     become <c>MyLib.Repository`1</c>.
		/// </summary>
		/// <remarks>
		///     Implemented interfaces are recorded with their type arguments filled in, so a class
		///     implementing <c>IRepository&lt;User&gt;</c> used to miss the interface's documentation.
		/// </remarks>
		private static string TypeKey(string name)
		{
			var key = new StringBuilder(name.Length);
			int angleDepth = 0;
			int otherDepth = 0;
			int arity = 0;

			foreach (char c in name)
			{
				switch (c)
				{
					case '<':
						if (++angleDepth == 1 && otherDepth == 0)
						{
							arity = 1;
						}

						break;
					case '>':
						if (--angleDepth == 0 && otherDepth == 0)
						{
							key.Append('`').Append(arity);
						}

						break;
					// Tuple and array type arguments carry commas that do not separate arguments.
					case '(' or '[':
						otherDepth++;
						break;
					case ')' or ']':
						otherDepth--;
						break;
					case ',' when angleDepth == 1 && otherDepth == 0:
						arity++;
						break;
					default:
						if (angleDepth == 0 && otherDepth == 0)
						{
							key.Append(c);
						}

						break;
				}
			}

			return key.ToString();
		}

		private static ApiMember? FindMatchingMember(ApiType type, ApiMember target)
		{
			ApiMember? sameCount = null;
			foreach (ApiMember candidate in type.Members)
			{
				// Constructors are named after their own type, so a base constructor never shares the name.
				if (candidate.Kind != target.Kind
				    || candidate.Parameters.Count != target.Parameters.Count
				    || (target.Kind != ApiMemberKind.Constructor && candidate.Name != target.Name))
				{
					continue;
				}

				if (candidate.Parameters.Select(p => p.Type).SequenceEqual(target.Parameters.Select(p => p.Type)))
				{
					return candidate;
				}

				sameCount ??= candidate;
			}

			return sameCount;
		}

		private static ApiMember? FindMember(ApiType type, MemberIdInfo id)
		{
			// Documentation IDs append a method's generic arity ("Map``1") and call an indexer "Item".
			string name = id.Name.Split('`')[0];
			int parameterCount = CountParameters(id.ParameterList);
			(ApiMemberKind Kind, string? Name) wanted = id.Kind switch
			{
				MemberIdKind.Method when name == "#ctor" => (ApiMemberKind.Constructor, null),
				MemberIdKind.Method when name.StartsWith("op_", StringComparison.Ordinal) => (ApiMemberKind.Operator, name),
				MemberIdKind.Method => (ApiMemberKind.Method, name),
				MemberIdKind.Property when id.ParameterList is not null => (ApiMemberKind.Indexer, null),
				MemberIdKind.Property => (ApiMemberKind.Property, name),
				MemberIdKind.Event => (ApiMemberKind.Event, name),
				_ => (ApiMemberKind.Field, name)
			};

			return type.Members.FirstOrDefault(member =>
				member.Kind == wanted.Kind
				&& (wanted.Name is null || member.Name == wanted.Name)
				&& (member.Kind is ApiMemberKind.Field or ApiMemberKind.Event or ApiMemberKind.Property
				    || member.Parameters.Count == parameterCount));
		}

		private static int CountParameters(string? parameterList)
		{
			if (string.IsNullOrWhiteSpace(parameterList))
			{
				return 0;
			}

			int count = 1;
			int depth = 0;
			foreach (char c in parameterList)
			{
				switch (c)
				{
					case '{' or '[' or '(':
						depth++;
						break;
					case '}' or ']':
						depth--;
						break;
					// A conversion operator's ID continues past the parameter list with ")~ReturnType".
					case ')' when depth == 0:
						return count;
					case ')':
						depth--;
						break;
					case ',' when depth == 0:
						count++;
						break;
				}
			}

			return count;
		}

		private static bool NeedsInheritance(XmlDocBlock? doc) =>
			doc is null || string.IsNullOrEmpty(doc.Summary) || doc.HasInheritDocTag;

		private static bool HasSummary(XmlDocBlock? doc) => !string.IsNullOrEmpty(doc?.Summary);

		private static XmlDocBlock? Merge(XmlDocBlock? own, XmlDocBlock? inherited)
		{
			if (inherited is null)
			{
				return own;
			}

			// The inheritdoc flags describe the comment on this symbol, not the one inherited from.
			if (own is null)
			{
				return inherited with { IsInherited = true, HasInheritDocTag = false, InheritDocCref = null };
			}

			return own with
			{
				Summary = Pick(own.Summary, inherited.Summary),
				Remarks = Pick(own.Remarks, inherited.Remarks),
				Returns = Pick(own.Returns, inherited.Returns),
				Value = Pick(own.Value, inherited.Value),
				Parameters = MergeTags(own.Parameters, inherited.Parameters),
				TypeParameters = MergeTags(own.TypeParameters, inherited.TypeParameters),
				Exceptions = own.Exceptions.Count > 0 ? own.Exceptions : inherited.Exceptions,
				Examples = own.Examples.Count > 0 ? own.Examples : inherited.Examples,
				SeeAlso = own.SeeAlso.Count > 0 ? own.SeeAlso : inherited.SeeAlso,
				IsInherited = true
			};

			static string Pick(string mine, string theirs) => string.IsNullOrEmpty(mine) ? theirs : mine;
		}

		private static Dictionary<string, string> MergeTags(
			Dictionary<string, string> own,
			Dictionary<string, string> inherited)
		{
			var merged = new Dictionary<string, string>(inherited, StringComparer.Ordinal);
			foreach ((string name, string text) in own)
			{
				merged[name] = text;
			}

			return merged;
		}
	}
}
