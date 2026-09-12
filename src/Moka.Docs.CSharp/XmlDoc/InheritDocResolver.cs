using System.Diagnostics.CodeAnalysis;
using Moka.Docs.Core.Api;

namespace Moka.Docs.CSharp.XmlDoc;

/// <summary>
///     Resolves <c>&lt;inheritdoc/&gt;</c> references by finding documentation
///     from base types and implemented interfaces.
/// </summary>
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
		var index = new TypeIndex(reference.Namespaces.SelectMany(ns => ns.Types));

		var resolvedNamespaces = reference.Namespaces.Select(ns =>
		{
			var resolvedTypes = ns.Types.Select(type => ResolveType(type, index)).ToList();
			return ns with { Types = resolvedTypes };
		}).ToList();

		return reference with { Namespaces = resolvedNamespaces };
	}

	private static ApiType ResolveType(ApiType type, TypeIndex index)
	{
		// Try to inherit type-level documentation
		XmlDocBlock? doc = type.Documentation;
		if (doc is null || string.IsNullOrEmpty(doc.Summary))
		{
			doc = FindInheritedTypeDoc(type, index) is { } inherited
				? inherited with { HasInheritDocTag = type.Documentation?.HasInheritDocTag ?? false }
				: doc;
		}

		// Resolve member documentation
		var resolvedMembers = type.Members.Select(member =>
		{
			if (member.Documentation is not null && !string.IsNullOrEmpty(member.Documentation.Summary))
			{
				return member;
			}

			XmlDocBlock? inheritedDoc = FindInheritedMemberDoc(type, member, index);
			if (inheritedDoc is null)
			{
				return member;
			}

			return member with
			{
				Documentation = inheritedDoc with
				{
					IsInherited = true,
					HasInheritDocTag = member.Documentation?.HasInheritDocTag ?? false
				}
			};
		}).ToList();

		return type with
		{
			Documentation = doc,
			Members = resolvedMembers
		};
	}

	private static XmlDocBlock? FindInheritedTypeDoc(ApiType type, TypeIndex index)
	{
		foreach (string reference in BaseAndInterfaces(type))
		{
			if (index.TryFind(reference, out ApiType? candidate)
			    && candidate.Documentation is not null
			    && !string.IsNullOrEmpty(candidate.Documentation.Summary))
			{
				return candidate.Documentation with { IsInherited = true };
			}
		}

		return null;
	}

	private static XmlDocBlock? FindInheritedMemberDoc(ApiType type, ApiMember member, TypeIndex index)
	{
		foreach (string reference in BaseAndInterfaces(type))
		{
			if (!index.TryFind(reference, out ApiType? candidate))
			{
				continue;
			}

			ApiMember? match = FindMatchingMember(candidate, member);
			if (match?.Documentation is not null && !string.IsNullOrEmpty(match.Documentation.Summary))
			{
				return match.Documentation;
			}
		}

		return null;
	}

	private static IEnumerable<string> BaseAndInterfaces(ApiType type)
	{
		if (type.BaseType is not null)
		{
			yield return type.BaseType;
		}

		foreach (string iface in type.ImplementedInterfaces)
		{
			yield return iface;
		}
	}

	private static ApiMember? FindMatchingMember(ApiType type, ApiMember target)
	{
		return type.Members.FirstOrDefault(m =>
			m.Name == target.Name &&
			m.Kind == target.Kind &&
			m.Parameters.Count == target.Parameters.Count);
	}

	/// <summary>
	///     Finds types by the name a base-type or interface reference was written with.
	/// </summary>
	private sealed class TypeIndex
	{
		private readonly Dictionary<string, ApiType> _byFullName = new(StringComparer.Ordinal);
		private readonly Dictionary<string, List<ApiType>> _bySimpleName = new(StringComparer.Ordinal);

		public TypeIndex(IEnumerable<ApiType> types)
		{
			foreach (ApiType type in types)
			{
				_byFullName.TryAdd(type.FullName, type);

				if (!_bySimpleName.TryGetValue(type.Name, out List<ApiType>? sameName))
				{
					sameName = [];
					_bySimpleName[type.Name] = sameName;
				}

				sameName.Add(type);
			}
		}

		/// <summary>
		///     Looks a reference up by full name, then by simple name when the reference is
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
		public bool TryFind(string reference, [NotNullWhen(true)] out ApiType? type)
		{
			if (_byFullName.TryGetValue(reference, out type))
			{
				return true;
			}

			int genericStart = reference.IndexOf('<');
			string name = genericStart >= 0 ? reference[..genericStart] : reference;

			if (!name.Contains('.')
			    && _bySimpleName.TryGetValue(name, out List<ApiType>? candidates)
			    && candidates.Count == 1)
			{
				type = candidates[0];
				return true;
			}

			type = null;
			return false;
		}
	}
}
