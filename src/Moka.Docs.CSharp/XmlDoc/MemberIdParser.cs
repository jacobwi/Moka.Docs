namespace Moka.Docs.CSharp.XmlDoc;

/// <summary>
///     Utilities for working with .NET XML documentation member ID strings.
///     Member IDs follow the format: prefix:fully.qualified.name(param1,param2)
/// </summary>
/// <remarks>
///     Prefixes:
///     <list type="bullet">
///         <item>
///             <description><c>T:</c> — Type</description>
///         </item>
///         <item>
///             <description><c>M:</c> — Method or constructor</description>
///         </item>
///         <item>
///             <description><c>P:</c> — Property</description>
///         </item>
///         <item>
///             <description><c>F:</c> — Field</description>
///         </item>
///         <item>
///             <description><c>E:</c> — Event</description>
///         </item>
///         <item>
///             <description><c>N:</c> — Namespace</description>
///         </item>
///         <item>
///             <description><c>!:</c> — Error reference</description>
///         </item>
///     </list>
/// </remarks>
public static class MemberIdParser
{
    /// <summary>
    ///     Parses a member ID string into its components.
    /// </summary>
    /// <param name="memberId">The full member ID (e.g., "M:Namespace.Type.Method(System.String)").</param>
    /// <returns>The parsed components, or null if the ID is malformed.</returns>
    public static MemberIdInfo? Parse(string memberId)
    {
        if (string.IsNullOrEmpty(memberId) || memberId.Length < 2 || memberId[1] != ':')
            return null;

        var prefix = memberId[0];
        var kind = prefix switch
        {
            'T' => MemberIdKind.Type,
            'M' => MemberIdKind.Method,
            'P' => MemberIdKind.Property,
            'F' => MemberIdKind.Field,
            'E' => MemberIdKind.Event,
            'N' => MemberIdKind.Namespace,
            '!' => MemberIdKind.Error,
            _ => (MemberIdKind?)null
        };

        if (kind is null) return null;

        var body = memberId[2..];

        // Extract parameter list if present
        string fullName;
        string? parameterList = null;
        var parenIndex = body.IndexOf('(');

        if (parenIndex >= 0)
        {
            fullName = body[..parenIndex];
            parameterList = body[(parenIndex + 1)..].TrimEnd(')');
        }
        else
        {
            fullName = body;
        }

        // Split into namespace/type and member name
        var lastDot = fullName.LastIndexOf('.');
        string? containingType = null;
        string name;

        if (kind == MemberIdKind.Type || kind == MemberIdKind.Namespace)
        {
            name = fullName;
        }
        else if (lastDot >= 0)
        {
            containingType = fullName[..lastDot];
            name = fullName[(lastDot + 1)..];
        }
        else
        {
            name = fullName;
        }

        return new MemberIdInfo
        {
            Kind = kind.Value,
            FullId = memberId,
            FullName = fullName,
            Name = name,
            ContainingType = containingType,
            ParameterList = parameterList
        };
    }

    /// <summary>
    ///     Gets a human-friendly display name from a member ID or cref string.
    /// </summary>
    /// <param name="cref">A cref like "T:System.String" or "M:MyApp.Foo.Bar(System.Int32)".</param>
    /// <returns>A simplified display name like "String" or "Foo.Bar".</returns>
    public static string GetDisplayName(string cref)
    {
        if (string.IsNullOrEmpty(cref)) return "";

        // Strip prefix
        var body = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;

        // Strip parameter list for methods
        var parenIndex = body.IndexOf('(');
        if (parenIndex >= 0)
            body = body[..parenIndex];

        // Get the last meaningful segment
        var lastDot = body.LastIndexOf('.');
        if (lastDot >= 0)
        {
            // For types, return just the type name
            // For members, return Type.Member
            var prefix = cref.Length > 1 ? cref[0] : ' ';
            if (prefix == 'T' || prefix == 'N') return body[(lastDot + 1)..];

            // For members, include parent type
            var secondLastDot = body.LastIndexOf('.', lastDot - 1);
            if (secondLastDot >= 0) return body[(secondLastDot + 1)..];
        }

        return body;
    }

    /// <summary>
    ///     Generates a member ID for a type.
    /// </summary>
    public static string ForType(string fullTypeName)
    {
        return $"T:{fullTypeName}";
    }

    /// <summary>
    ///     Generates a member ID for a method.
    /// </summary>
    public static string ForMethod(string fullTypeName, string methodName, IEnumerable<string>? parameterTypes = null)
    {
        var id = $"M:{fullTypeName}.{methodName}";
        if (parameterTypes is not null)
        {
            var paramList = string.Join(",", parameterTypes);
            if (!string.IsNullOrEmpty(paramList))
                id += $"({paramList})";
        }

        return id;
    }

    /// <summary>
    ///     Generates a member ID for a property.
    /// </summary>
    public static string ForProperty(string fullTypeName, string propertyName)
    {
        return $"P:{fullTypeName}.{propertyName}";
    }

    /// <summary>
    ///     Generates a member ID for a field.
    /// </summary>
    public static string ForField(string fullTypeName, string fieldName)
    {
        return $"F:{fullTypeName}.{fieldName}";
    }

    /// <summary>
    ///     Generates a member ID for an event.
    /// </summary>
    public static string ForEvent(string fullTypeName, string eventName)
    {
        return $"E:{fullTypeName}.{eventName}";
    }

    /// <summary>
    ///     Generates a member ID for a constructor.
    /// </summary>
    public static string ForConstructor(string fullTypeName, IEnumerable<string>? parameterTypes = null)
    {
        return ForMethod(fullTypeName, "#ctor", parameterTypes);
    }
}

/// <summary>
///     Parsed components of a member ID string.
/// </summary>
public sealed record MemberIdInfo
{
    /// <summary>The kind of member.</summary>
    public required MemberIdKind Kind { get; init; }

    /// <summary>The full original member ID.</summary>
    public required string FullId { get; init; }

    /// <summary>The fully qualified name (without prefix).</summary>
    public required string FullName { get; init; }

    /// <summary>The simple name (last segment).</summary>
    public required string Name { get; init; }

    /// <summary>The containing type, for non-type members.</summary>
    public string? ContainingType { get; init; }

    /// <summary>The parameter list string, for methods.</summary>
    public string? ParameterList { get; init; }
}

/// <summary>
///     The kind of member referenced by a member ID.
/// </summary>
public enum MemberIdKind
{
    /// <summary>A namespace.</summary>
    Namespace,

    /// <summary>A type (class, struct, etc.).</summary>
    Type,

    /// <summary>A method or constructor.</summary>
    Method,

    /// <summary>A property.</summary>
    Property,

    /// <summary>A field.</summary>
    Field,

    /// <summary>An event.</summary>
    Event,

    /// <summary>An error reference.</summary>
    Error
}