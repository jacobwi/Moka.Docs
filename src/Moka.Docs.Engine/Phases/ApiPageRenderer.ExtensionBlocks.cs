using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Engine.Phases;

public static partial class ApiPageRenderer
{
	private static readonly ConditionalWeakTable<ApiType, IReadOnlyList<ApiExtensionGroup>> _extensionGroups = new();

	// Blocks with the same declaration share a section.
	internal static IReadOnlyList<ApiExtensionGroup> ExtensionGroups(ApiType type) =>
		type.Members.Any(m => m.Extension is not null) ? _extensionGroups.GetValue(type, BuildExtensionGroups) : [];

	internal static string MemberSectionId(ApiType parent, ApiMember member) =>
		member.Extension is { } block
		&& ExtensionGroups(parent).FirstOrDefault(g => g.Block.Signature == block.Signature) is { } group
			? group.Id
			: SectionId(member.Kind);

	private static IReadOnlyList<ApiExtensionGroup> BuildExtensionGroups(ApiType type)
	{
		var blocks = new List<(ApiExtensionBlock Block, List<ApiMember> Members)>();
		foreach (ApiMember member in type.Members)
		{
			if (member.Extension is not { } block)
			{
				continue;
			}

			int index = blocks.FindIndex(b => b.Block.Signature == block.Signature);
			if (index < 0)
			{
				blocks.Add((block, [member]));
				continue;
			}

			blocks[index].Members.Add(member);
			if (blocks[index].Block.Documentation is null && block.Documentation is not null)
			{
				blocks[index] = (block, blocks[index].Members);
			}
		}

		// Member anchors are identifiers with an optional numeric suffix, so an id with a hyphen between
		// two words never collides with one. The fixed section ids are the only others to avoid.
		var used = new HashSet<string>(StringComparer.Ordinal) { _inheritedMembersId, _extensionMethodsId };
		var groups = new List<ApiExtensionGroup>(blocks.Count);
		foreach ((ApiExtensionBlock block, List<ApiMember> members) in blocks)
		{
			string receiver = NonAlphanumericRegex().Replace(ShortTypeName(block.Receiver.Type).ToLowerInvariant(), "-").Trim('-');
			string baseId = receiver.Length > 0 ? $"extension-{receiver}" : "extension-block";
			string id = baseId;
			for (int n = 2; !used.Add(id); n++)
			{
				id = $"{baseId}-{n}";
			}

			List<ApiMember> ordered = _memberSections
				.SelectMany(section => members.Where(m => section.Kinds.Contains(m.Kind)).OrderBy(m => m.Name))
				.ToList();
			groups.Add(new ApiExtensionGroup(id, WithoutConstraints(block.Signature), block, ordered));
		}

		return groups;
	}

	// "extension<T>(List<T> list) where T : IComparable<T>" is too long for a heading.
	private static string WithoutConstraints(string signature)
	{
		int depth = 0;
		for (int i = 0; i < signature.Length; i++)
		{
			if (signature[i] == '(')
			{
				depth++;
			}
			else if (signature[i] == ')' && --depth == 0)
			{
				return signature[..(i + 1)];
			}
		}

		return signature;
	}

	private static void RenderExtensionGroup(StringBuilder sb, ApiExtensionGroup group, ApiType parentType,
		ApiCrefResolver links)
	{
		ApiExtensionBlock block = group.Block;
		XmlDocBlock? doc = block.Documentation;

		sb.AppendLine($"<h2 id=\"{Esc(group.Id)}\">{Esc(group.Title)}</h2>");
		sb.AppendLine($"<pre class=\"api-signature\"><code class=\"language-csharp\">{Esc(block.Signature)}</code></pre>");

		if (!string.IsNullOrEmpty(doc?.Summary))
		{
			sb.AppendLine($"<p>{links.ResolveHtml(doc.Summary, parentType)}</p>");
		}

		if (block.TypeParameters.Count > 0)
		{
			sb.AppendLine("<h4>Type Parameters</h4>");
			RenderTypeParameterTable(sb, block.TypeParameters, doc, parentType, links);
		}

		ApiParameter receiver = block.Receiver;
		if (receiver.Name.Length > 0 && doc?.Parameters.GetValueOrDefault(receiver.Name) is { Length: > 0 } description)
		{
			sb.AppendLine("<h4>Receiver</h4>");
			sb.AppendLine(
				"<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Type</th><th>Description</th></tr></thead><tbody>");
			sb.AppendLine(
				$"<tr><td><code>{Esc(receiver.Name)}</code></td><td><code>{Esc(ParameterModifiers(receiver) + receiver.Type)}</code></td><td>{links.ResolveHtml(description, parentType)}</td></tr>");
			sb.AppendLine("</tbody></table></div>");
		}

		if (!string.IsNullOrEmpty(doc?.Remarks))
		{
			sb.AppendLine("<h4>Remarks</h4>");
			sb.AppendLine($"<div class=\"api-remarks\">{links.ResolveHtml(doc.Remarks, parentType)}</div>");
		}

		RenderMembers(sb, group.Members, parentType, links);
	}

	[GeneratedRegex("[^a-z0-9]+")]
	private static partial Regex NonAlphanumericRegex();
}

internal sealed record ApiExtensionGroup(string Id, string Title, ApiExtensionBlock Block, List<ApiMember> Members);
