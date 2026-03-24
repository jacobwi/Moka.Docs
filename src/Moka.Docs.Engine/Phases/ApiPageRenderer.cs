using System.Text;
using System.Web;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Generates HTML content for API type documentation pages.
/// </summary>
public static class ApiPageRenderer
{
    /// <summary>
    ///     Renders an API type as HTML documentation.
    /// </summary>
    /// <param name="type">The type to render.</param>
    /// <param name="allTypes">All types in the API model, used to discover derived types for the dependency graph.</param>
    public static string RenderType(ApiType type, IReadOnlyList<ApiType>? allTypes = null)
    {
        var sb = new StringBuilder();

        #region Type Header with Badge

        var kindBadge = type.Kind.ToString().ToLowerInvariant();
        sb.AppendLine("<div class=\"api-type-header\">");
        sb.AppendLine($"<span class=\"api-badge api-badge-{kindBadge}\">{type.Kind}</span>");
        if (type.IsObsolete)
            sb.AppendLine("<span class=\"api-badge api-badge-obsolete\">Obsolete</span>");
        if (type.IsStatic)
            sb.AppendLine("<span class=\"api-badge api-badge-static\">Static</span>");
        if (type.IsAbstract)
            sb.AppendLine("<span class=\"api-badge api-badge-abstract\">Abstract</span>");
        if (type.IsSealed && type.Kind != ApiTypeKind.Record)
            sb.AppendLine("<span class=\"api-badge api-badge-sealed\">Sealed</span>");
        sb.AppendLine("</div>");

        #endregion

        // Signature
        sb.AppendLine(
            $"<pre class=\"api-signature\"><code class=\"language-csharp\">{Esc(BuildTypeSignature(type))}</code></pre>");

        // Namespace
        if (!string.IsNullOrEmpty(type.Namespace))
            sb.AppendLine($"<p class=\"api-namespace\">Namespace: <code>{Esc(type.Namespace)}</code></p>");

        // Obsolete warning
        if (type.IsObsolete)
            sb.AppendLine(
                $"<div class=\"warning\"><p><strong>Obsolete:</strong> {Esc(type.ObsoleteMessage ?? "This type is deprecated.")}</p></div>");

        // Summary
        if (type.Documentation is { } doc && !string.IsNullOrEmpty(doc.Summary))
            sb.AppendLine($"<div class=\"api-summary\">{doc.Summary}</div>");

        // Remarks
        if (type.Documentation?.Remarks is { } remarks && !string.IsNullOrEmpty(remarks))
        {
            sb.AppendLine("<h2 id=\"remarks\">Remarks</h2>");
            sb.AppendLine($"<div class=\"api-remarks\">{remarks}</div>");
        }

        #region Type Parameters

        if (type.TypeParameters.Count > 0)
        {
            sb.AppendLine("<h2 id=\"type-parameters\">Type Parameters</h2>");
            sb.AppendLine(
                "<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Constraints</th><th>Description</th></tr></thead><tbody>");
            foreach (var tp in type.TypeParameters)
            {
                var constraints = tp.Constraints.Count > 0 ? string.Join(", ", tp.Constraints) : "—";
                var desc = type.Documentation?.TypeParameters.GetValueOrDefault(tp.Name) ?? "";
                sb.AppendLine(
                    $"<tr><td><code>{Esc(tp.Name)}</code></td><td>{Esc(constraints)}</td><td>{desc}</td></tr>");
            }

            sb.AppendLine("</tbody></table></div>");
        }

        #endregion

        #region Inheritance

        if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
        {
            sb.AppendLine("<h2 id=\"inheritance\">Inheritance</h2>");
            if (type.BaseType is not null)
                sb.AppendLine($"<p>Inherits from: <code>{Esc(type.BaseType)}</code></p>");
            if (type.ImplementedInterfaces.Count > 0)
                sb.AppendLine(
                    $"<p>Implements: {string.Join(", ", type.ImplementedInterfaces.Select(i => $"<code>{Esc(i)}</code>"))}</p>");
        }

        #endregion

        #region Members by Kind

        RenderMemberSection(sb, "Constructors", type.Members.Where(m => m.Kind == ApiMemberKind.Constructor).ToList(),
            type);
        RenderMemberSection(sb, "Properties",
            type.Members.Where(m => m.Kind is ApiMemberKind.Property or ApiMemberKind.Indexer).ToList(), type);
        RenderMemberSection(sb, "Methods", type.Members.Where(m => m.Kind == ApiMemberKind.Method).ToList(), type);
        RenderMemberSection(sb, "Events", type.Members.Where(m => m.Kind == ApiMemberKind.Event).ToList(), type);
        RenderMemberSection(sb, "Fields", type.Members.Where(m => m.Kind == ApiMemberKind.Field).ToList(), type);
        RenderMemberSection(sb, "Operators", type.Members.Where(m => m.Kind == ApiMemberKind.Operator).ToList(), type);

        #endregion

        #region Examples

        if (type.Documentation?.Examples is { Count: > 0 } examples)
        {
            sb.AppendLine("<h2 id=\"examples\">Examples</h2>");
            foreach (var example in examples) sb.AppendLine($"<div class=\"api-example\">{example}</div>");
        }

        #endregion

        #region See Also

        if (type.Documentation?.SeeAlso is { Count: > 0 } seeAlso)
        {
            sb.AppendLine("<h2 id=\"see-also\">See Also</h2>");
            sb.AppendLine("<ul>");
            foreach (var sa in seeAlso)
                sb.AppendLine($"<li><code>{Esc(sa)}</code></li>");
            sb.AppendLine("</ul>");
        }

        #endregion

        #region Type Dependency Graph

        var mermaidDiagram = BuildTypeDependencyGraph(type, allTypes ?? []);
        if (mermaidDiagram is not null)
        {
            sb.AppendLine("<details class=\"type-graph\">");
            sb.AppendLine("<summary>");
            sb.AppendLine(
                "<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"5\" r=\"3\"/><circle cx=\"5\" cy=\"19\" r=\"3\"/><circle cx=\"19\" cy=\"19\" r=\"3\"/><line x1=\"12\" y1=\"8\" x2=\"5\" y2=\"16\"/><line x1=\"12\" y1=\"8\" x2=\"19\" y2=\"16\"/></svg>");
            sb.AppendLine("Type Relationships");
            sb.AppendLine("</summary>");
            sb.AppendLine($"<pre class=\"mermaid\">{mermaidDiagram}</pre>");
            sb.AppendLine("</details>");
        }

        #endregion

        #region View Source

        if (!string.IsNullOrEmpty(type.SourceCode))
        {
            sb.AppendLine("<details class=\"source-viewer\">");
            sb.AppendLine("<summary>");
            sb.AppendLine(
                "<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/></svg>");
            sb.AppendLine("View Source");
            sb.AppendLine("</summary>");
            sb.AppendLine($"<pre><code class=\"language-csharp\">{Esc(type.SourceCode)}</code></pre>");
            sb.AppendLine("</details>");
        }

        #endregion

        return sb.ToString();
    }

    private static void RenderMemberSection(StringBuilder sb, string title, List<ApiMember> members, ApiType parentType)
    {
        if (members.Count == 0) return;

        var sectionId = title.ToLowerInvariant().Replace(' ', '-');
        sb.AppendLine($"<h2 id=\"{sectionId}\">{Esc(title)}</h2>");
        sb.AppendLine(
            "<div class=\"table-responsive\"><table class=\"api-member-table\"><thead><tr><th>Name</th><th>Description</th></tr></thead><tbody>");

        foreach (var member in members.OrderBy(m => m.Name))
        {
            var name = member.Kind == ApiMemberKind.Constructor ? parentType.Name : member.Name;
            var badges = new List<string>();
            if (member.IsStatic) badges.Add("<span class=\"api-badge-sm api-badge-static\">static</span>");
            if (member.IsVirtual) badges.Add("<span class=\"api-badge-sm api-badge-virtual\">virtual</span>");
            if (member.IsAbstract) badges.Add("<span class=\"api-badge-sm api-badge-abstract\">abstract</span>");
            if (member.IsOverride) badges.Add("<span class=\"api-badge-sm\">override</span>");
            if (member.IsObsolete) badges.Add("<span class=\"api-badge-sm api-badge-obsolete\">obsolete</span>");

            var badgeHtml = badges.Count > 0 ? " " + string.Join(" ", badges) : "";
            var summary = member.Documentation?.Summary ?? "";

            var nameId = name.ToLowerInvariant();
            var compactParams = FormatParams(member, true);
            var fullParams = FormatParams(member);
            var titleAttr = compactParams != fullParams
                ? $" title=\"{Esc(name)}{Esc(fullParams)}\""
                : "";

            sb.AppendLine("<tr>");
            sb.AppendLine(
                $"<td><a href=\"#{Esc(nameId)}\" class=\"api-member-link\"><code>{Esc(name)}{Esc(compactParams)}</code></a>{badgeHtml}</td>");
            sb.AppendLine($"<td>{summary}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></div>");

        #region Detailed Member Docs

        foreach (var member in members.OrderBy(m => m.Name))
        {
            if (member.Documentation is null) continue;
            if (string.IsNullOrEmpty(member.Documentation.Summary) &&
                member.Documentation.Parameters.Count == 0 &&
                string.IsNullOrEmpty(member.Documentation.Returns)) continue;

            var name = member.Kind == ApiMemberKind.Constructor ? parentType.Name : member.Name;

            sb.AppendLine($"<h3 id=\"{Esc(name.ToLowerInvariant())}\">{Esc(name)}{FormatParams(member)}</h3>");
            sb.AppendLine(
                $"<pre class=\"api-signature\"><code class=\"language-csharp\">{Esc(member.Signature)}</code></pre>");

            if (!string.IsNullOrEmpty(member.Documentation.Summary))
                sb.AppendLine($"<p>{member.Documentation.Summary}</p>");

            // Parameters
            if (member.Documentation.Parameters.Count > 0)
            {
                sb.AppendLine("<h4>Parameters</h4>");
                sb.AppendLine(
                    "<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Type</th><th>Description</th></tr></thead><tbody>");
                foreach (var param in member.Parameters)
                {
                    var desc = member.Documentation.Parameters.GetValueOrDefault(param.Name) ?? "";
                    var modifiers = new List<string>();
                    if (param.IsRef) modifiers.Add("ref");
                    if (param.IsOut) modifiers.Add("out");
                    if (param.IsIn) modifiers.Add("in");
                    if (param.IsParams) modifiers.Add("params");
                    var mod = modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
                    sb.AppendLine(
                        $"<tr><td><code>{Esc(param.Name)}</code></td><td><code>{Esc(mod + param.Type)}</code></td><td>{desc}</td></tr>");
                }

                sb.AppendLine("</tbody></table></div>");
            }

            // Returns
            if (!string.IsNullOrEmpty(member.Documentation.Returns))
                sb.AppendLine($"<p><strong>Returns:</strong> {member.Documentation.Returns}</p>");

            // Exceptions
            if (member.Documentation.Exceptions.Count > 0)
            {
                sb.AppendLine("<h4>Exceptions</h4>");
                sb.AppendLine(
                    "<div class=\"table-responsive\"><table><thead><tr><th>Exception</th><th>Condition</th></tr></thead><tbody>");
                foreach (var ex in member.Documentation.Exceptions)
                    sb.AppendLine($"<tr><td><code>{Esc(ex.Type)}</code></td><td>{ex.Description}</td></tr>");
                sb.AppendLine("</tbody></table></div>");
            }
        }

        #endregion
    }

    private static string FormatParams(ApiMember member, bool compact = false)
    {
        if (member.Parameters.Count == 0 && member.Kind is not ApiMemberKind.Method and not ApiMemberKind.Constructor)
            return "";
        if (member.Parameters.Count == 0)
            return "()";

        // In compact mode (summary tables), abbreviate long parameter lists
        if (compact && member.Parameters.Count > 2)
            return "(\u2026)";

        var parms = string.Join(", ", member.Parameters.Select(p => p.Type.Split('.').Last() + " " + p.Name));
        return $"({parms})";
    }

    private static string BuildTypeSignature(ApiType type)
    {
        var sb = new StringBuilder();

        sb.Append(type.Accessibility.ToString().ToLowerInvariant().Replace("protectedinternal", "protected internal"));
        sb.Append(' ');

        if (type.IsStatic) sb.Append("static ");
        if (type.IsAbstract && type.Kind != ApiTypeKind.Interface) sb.Append("abstract ");
        if (type.IsSealed && type.Kind == ApiTypeKind.Class) sb.Append("sealed ");

        sb.Append(type.Kind switch
        {
            ApiTypeKind.Class => type.IsRecord ? "record " : "class ",
            ApiTypeKind.Struct => type.IsRecord ? "record struct " : "struct ",
            ApiTypeKind.Interface => "interface ",
            ApiTypeKind.Enum => "enum ",
            ApiTypeKind.Delegate => "delegate ",
            ApiTypeKind.Record => "record ",
            _ => ""
        });

        sb.Append(type.Name);

        if (type.TypeParameters.Count > 0)
            sb.Append($"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>");

        if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
        {
            var bases = new List<string>();
            if (type.BaseType is not null) bases.Add(type.BaseType);
            bases.AddRange(type.ImplementedInterfaces);
            sb.Append($" : {string.Join(", ", bases)}");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Builds a Mermaid class diagram showing the type's inheritance and interface relationships.
    ///     Returns null if the diagram would only contain the type itself (no meaningful relationships).
    /// </summary>
    private static string? BuildTypeDependencyGraph(ApiType type, IReadOnlyList<ApiType> allTypes)
    {
        const int MaxNodes = 20;

        // Collect all nodes and edges for the graph
        var nodes = new HashSet<string> { type.Name };
        var edges = new List<(string From, string To, string Label, string Arrow)>();

        // Base type (skip Object — not meaningful)
        if (type.BaseType is not null && type.BaseType != "Object" && type.BaseType != "object")
        {
            var baseShort = GetShortTypeName(type.BaseType);
            nodes.Add(baseShort);
            edges.Add((type.Name, baseShort, "inherits", "--|>"));
        }

        // Implemented interfaces
        foreach (var iface in type.ImplementedInterfaces)
        {
            if (nodes.Count >= MaxNodes) break;
            var ifaceShort = GetShortTypeName(iface);
            nodes.Add(ifaceShort);
            edges.Add((type.Name, ifaceShort, "implements", "..|>"));
        }

        // Derived types — types that list this type as their base
        foreach (var other in allTypes)
        {
            if (nodes.Count >= MaxNodes) break;
            if (other.BaseType is not null && GetShortTypeName(other.BaseType) == type.Name)
            {
                nodes.Add(other.Name);
                edges.Add((other.Name, type.Name, "inherits", "--|>"));
            }
        }

        // Types that implement this type (when this type is an interface)
        if (type.Kind == ApiTypeKind.Interface)
            foreach (var other in allTypes)
            {
                if (nodes.Count >= MaxNodes) break;
                if (other.ImplementedInterfaces.Any(i => GetShortTypeName(i) == type.Name))
                {
                    nodes.Add(other.Name);
                    edges.Add((other.Name, type.Name, "implements", "..|>"));
                }
            }

        // Skip diagram if the type is alone (no relationships worth showing)
        if (nodes.Count <= 1)
            return null;

        // Build the Mermaid class diagram
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");

        // Highlight the current type with a style directive
        sb.AppendLine($"    style {EscapeMermaidId(type.Name)} fill:#f9f,stroke:#333,stroke-width:2px");

        // Render edges
        foreach (var (from, to, label, arrow) in edges)
            sb.AppendLine($"    {EscapeMermaidId(from)} {arrow} {EscapeMermaidId(to)} : {label}");

        return sb.ToString();
    }

    /// <summary>
    ///     Extracts the short type name, stripping namespace prefixes and generic arity suffixes.
    ///     E.g., "System.Collections.Generic.List&lt;T&gt;" becomes "List&lt;T&gt;".
    /// </summary>
    private static string GetShortTypeName(string fullName)
    {
        // Strip namespace
        var dotIndex = fullName.LastIndexOf('.');
        var name = dotIndex >= 0 ? fullName[(dotIndex + 1)..] : fullName;
        return name;
    }

    /// <summary>
    ///     Escapes a type name for use as a Mermaid node identifier.
    ///     Mermaid doesn't allow angle brackets or special characters in bare identifiers.
    /// </summary>
    private static string EscapeMermaidId(string name)
    {
        // Replace characters that break Mermaid syntax with safe alternatives
        return name
            .Replace("<", "~")
            .Replace(">", "~")
            .Replace(" ", "_");
    }

    private static string Esc(string text)
    {
        return HttpUtility.HtmlEncode(text);
    }

    /// <summary>
    ///     Builds a table of contents for an API type page based on the sections it will render.
    /// </summary>
    public static TableOfContents BuildTocForType(ApiType type)
    {
        var entries = new List<TocEntry>();

        if (type.Documentation?.Remarks is { Length: > 0 })
            entries.Add(new TocEntry { Level = 2, Text = "Remarks", Id = "remarks" });

        if (type.TypeParameters.Count > 0)
            entries.Add(new TocEntry { Level = 2, Text = "Type Parameters", Id = "type-parameters" });

        if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
            entries.Add(new TocEntry { Level = 2, Text = "Inheritance", Id = "inheritance" });

        if (type.Members.Any(m => m.Kind == ApiMemberKind.Constructor))
            entries.Add(new TocEntry { Level = 2, Text = "Constructors", Id = "constructors" });

        if (type.Members.Any(m => m.Kind is ApiMemberKind.Property or ApiMemberKind.Indexer))
            entries.Add(new TocEntry { Level = 2, Text = "Properties", Id = "properties" });

        if (type.Members.Any(m => m.Kind == ApiMemberKind.Method))
            entries.Add(new TocEntry { Level = 2, Text = "Methods", Id = "methods" });

        if (type.Members.Any(m => m.Kind == ApiMemberKind.Event))
            entries.Add(new TocEntry { Level = 2, Text = "Events", Id = "events" });

        if (type.Members.Any(m => m.Kind == ApiMemberKind.Field))
            entries.Add(new TocEntry { Level = 2, Text = "Fields", Id = "fields" });

        if (type.Members.Any(m => m.Kind == ApiMemberKind.Operator))
            entries.Add(new TocEntry { Level = 2, Text = "Operators", Id = "operators" });

        if (type.Documentation?.Examples is { Count: > 0 })
            entries.Add(new TocEntry { Level = 2, Text = "Examples", Id = "examples" });

        if (type.Documentation?.SeeAlso is { Count: > 0 })
            entries.Add(new TocEntry { Level = 2, Text = "See Also", Id = "see-also" });

        return new TableOfContents { Entries = entries };
    }
}