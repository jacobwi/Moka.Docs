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
        // Build a lookup of all types by full name
        var typeLookup = new Dictionary<string, ApiType>(StringComparer.Ordinal);
        foreach (var type in reference.Namespaces.SelectMany(ns => ns.Types)) typeLookup.TryAdd(type.FullName, type);

        var resolvedNamespaces = reference.Namespaces.Select(ns =>
        {
            var resolvedTypes = ns.Types.Select(type =>
                ResolveType(type, typeLookup)).ToList();

            return ns with { Types = resolvedTypes };
        }).ToList();

        return reference with { Namespaces = resolvedNamespaces };
    }

    private static ApiType ResolveType(ApiType type, Dictionary<string, ApiType> lookup)
    {
        // Try to inherit type-level documentation
        var doc = type.Documentation;
        if (doc is null || string.IsNullOrEmpty(doc.Summary)) doc = FindInheritedTypeDoc(type, lookup);

        // Resolve member documentation
        var resolvedMembers = type.Members.Select(member =>
        {
            if (member.Documentation is not null && !string.IsNullOrEmpty(member.Documentation.Summary))
                return member;

            var inheritedDoc = FindInheritedMemberDoc(type, member, lookup);
            if (inheritedDoc is null) return member;

            return member with
            {
                Documentation = inheritedDoc with { IsInherited = true }
            };
        }).ToList();

        return type with
        {
            Documentation = doc,
            Members = resolvedMembers
        };
    }

    private static XmlDocBlock? FindInheritedTypeDoc(ApiType type, Dictionary<string, ApiType> lookup)
    {
        // Check base type
        if (type.BaseType is not null && lookup.TryGetValue(type.BaseType, out var baseType))
            if (baseType.Documentation is not null && !string.IsNullOrEmpty(baseType.Documentation.Summary))
                return baseType.Documentation with { IsInherited = true };

        // Check interfaces
        foreach (var iface in type.ImplementedInterfaces)
            if (lookup.TryGetValue(iface, out var ifaceType) &&
                ifaceType.Documentation is not null &&
                !string.IsNullOrEmpty(ifaceType.Documentation.Summary))
                return ifaceType.Documentation with { IsInherited = true };

        return null;
    }

    private static XmlDocBlock? FindInheritedMemberDoc(
        ApiType type, ApiMember member, Dictionary<string, ApiType> lookup)
    {
        // Search base type
        if (type.BaseType is not null && lookup.TryGetValue(type.BaseType, out var baseType))
        {
            var baseMember = FindMatchingMember(baseType, member);
            if (baseMember?.Documentation is not null &&
                !string.IsNullOrEmpty(baseMember.Documentation.Summary))
                return baseMember.Documentation;
        }

        // Search interfaces
        foreach (var iface in type.ImplementedInterfaces)
        {
            if (!lookup.TryGetValue(iface, out var ifaceType)) continue;

            var ifaceMember = FindMatchingMember(ifaceType, member);
            if (ifaceMember?.Documentation is not null &&
                !string.IsNullOrEmpty(ifaceMember.Documentation.Summary))
                return ifaceMember.Documentation;
        }

        return null;
    }

    private static ApiMember? FindMatchingMember(ApiType type, ApiMember target)
    {
        return type.Members.FirstOrDefault(m =>
            m.Name == target.Name &&
            m.Kind == target.Kind &&
            m.Parameters.Count == target.Parameters.Count);
    }
}