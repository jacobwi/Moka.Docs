using System.Text;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Engine.Phases;

public static partial class ApiPageRenderer
{
	// Member anchors are lowercased C# identifiers with an optional numeric suffix, so ids with a hyphen
	// between two words can't collide with them.
	private const string _inheritedMembersId = "inherited-members";
	private const string _extensionMethodsId = "extension-methods";

	private static readonly IReadOnlyDictionary<string, string> _noTypeArguments = new Dictionary<string, string>();

	/// <summary>
	///     Lists the members a type inherits from documented types, one row per declaring type, each member
	///     linked to its entry on that type's page.
	/// </summary>
	private static void RenderInheritedMembers(StringBuilder sb, ApiType type, ApiTypeRelations relations,
		ApiCrefResolver links)
	{
		IReadOnlyList<ApiInheritedGroup> groups = relations.InheritedMembers(type);
		if (groups.Count == 0)
		{
			return;
		}

		sb.AppendLine($"<h2 id=\"{_inheritedMembersId}\">Inherited Members</h2>");
		sb.AppendLine(
			"<div class=\"table-responsive\"><table class=\"api-member-table api-inherited-members\"><thead><tr><th>Declared in</th><th>Members</th></tr></thead><tbody>");

		foreach (ApiInheritedGroup group in groups)
		{
			ApiType declaringType = group.From.Type;
			IEnumerable<string> members = group.Members.Select(member =>
				$"<a href=\"{Esc(MemberHref(declaringType, member, links))}\" class=\"api-member-link\"><code>{Esc(ApiTypeRelations.MemberText(member, group.From.TypeArguments))}</code></a>");

			sb.AppendLine("<tr>");
			sb.AppendLine(
				$"<td><a href=\"{Esc(ApiCrefResolver.TypeRoute(declaringType))}\">{Esc(ShortTypeName(group.From.Reference))}</a></td>");
			sb.AppendLine($"<td>{string.Join(", ", members)}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	/// <summary>
	///     Lists the documented extension methods that apply to a type, linked to their entries on the
	///     pages of the static classes that declare them.
	/// </summary>
	private static void RenderExtensionMethods(StringBuilder sb, ApiType type, ApiTypeRelations relations,
		ApiCrefResolver links)
	{
		IReadOnlyList<ApiExtensionMethod> extensions = relations.ExtensionMethods(type);
		if (extensions.Count == 0)
		{
			return;
		}

		sb.AppendLine($"<h2 id=\"{_extensionMethodsId}\">Extension Methods</h2>");
		sb.AppendLine(
			"<div class=\"table-responsive\"><table class=\"api-member-table api-extension-methods\"><thead><tr><th>Name</th><th>Declared in</th><th>Description</th></tr></thead><tbody>");

		foreach ((ApiType declaringType, ApiMember method) in extensions)
		{
			string obsolete = method.IsObsolete ? " <span class=\"api-badge-sm api-badge-obsolete\">obsolete</span>" : "";
			string summary = links.ResolveHtml(method.Documentation?.Summary ?? "", type);

			sb.AppendLine("<tr>");
			sb.AppendLine(
				$"<td><a href=\"{Esc(MemberHref(declaringType, method, links))}\" class=\"api-member-link\"><code>{Esc(ApiTypeRelations.MemberText(method, _noTypeArguments))}</code></a>{obsolete}</td>");
			sb.AppendLine($"<td><a href=\"{Esc(ApiCrefResolver.TypeRoute(declaringType))}\">{Esc(declaringType.Name)}</a></td>");
			sb.AppendLine($"<td>{summary}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	/// <summary>
	///     The link to a member on its type's page: its detail block, or its section when it has none.
	/// </summary>
	private static string MemberHref(ApiType parent, ApiMember member, ApiCrefResolver links)
	{
		string anchor = links.GetAnchors(parent).TryGetValue(member, out string? id) ? id : SectionId(member.Kind);
		return $"{ApiCrefResolver.TypeRoute(parent)}#{anchor}";
	}
}
