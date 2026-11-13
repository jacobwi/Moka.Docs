using System.Text;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Engine.Phases;

public static partial class ApiPageRenderer
{
	/// <summary>
	///     Renders an API type as Markdown, for the page's Markdown copy and <c>llms-full.txt</c>: the
	///     declaration, the summary and remarks, type parameters, base types, and each member with its
	///     signature, summary, parameters and return value. Documentation text is plain text.
	/// </summary>
	/// <param name="type">The type to render.</param>
	/// <param name="allTypes">All types in the API model, for the type's display name.</param>
	/// <returns>The Markdown.</returns>
	public static string RenderMarkdown(ApiType type, IReadOnlyList<ApiType>? allTypes = null)
	{
		var md = new StringBuilder();
		bool python = IsPython(type);
		string language = python ? "python" : "csharp";

		md.Append("# ").AppendLine(ApiTypeRoutes.For(allTypes ?? [type]).DisplayName(type)).AppendLine();
		if (!string.IsNullOrEmpty(type.Namespace))
		{
			md.Append(type.Kind).Append(" in `").Append(type.Namespace).AppendLine("`").AppendLine();
		}

		Fence(md, language, WithAttributes(type.Attributes, python, type.IsObsolete, type.Signature ?? BuildTypeSignature(type)));

		if (type.IsObsolete)
		{
			md.Append("**Obsolete:** ").AppendLine(type.ObsoleteMessage ?? "This type is deprecated.").AppendLine();
		}

		Text(md, type.Documentation?.Summary);
		if (ApiDocText.ToPlainText(type.Documentation?.Remarks) is { Length: > 0 } remarks)
		{
			md.AppendLine("## Remarks").AppendLine().AppendLine(remarks).AppendLine();
		}

		if (type.TypeParameters.Count > 0)
		{
			md.AppendLine("## Type Parameters").AppendLine();
			foreach (ApiTypeParameter parameter in type.TypeParameters)
			{
				Item(md, $"`{parameter.Name}`", type.Documentation?.TypeParameters.GetValueOrDefault(parameter.Name));
			}

			md.AppendLine();
		}

		if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
		{
			md.AppendLine("## Inheritance").AppendLine();
			if (type.BaseType is not null)
			{
				md.Append("Inherits from `").Append(type.BaseType).AppendLine("`.").AppendLine();
			}

			if (type.ImplementedInterfaces.Count > 0)
			{
				md.Append("Implements ").AppendJoin(", ", type.ImplementedInterfaces.Select(i => $"`{i}`")).AppendLine(".")
					.AppendLine();
			}
		}

		foreach ((string title, ApiMemberKind[] kinds) in _memberSections)
		{
			List<ApiMember> members = SectionMembers(type, kinds);
			if (members.Count == 0)
			{
				continue;
			}

			md.Append("## ").AppendLine(title).AppendLine();
			foreach (ApiMember member in members)
			{
				Member(md, member, type, python, language);
			}
		}

		return md.ToString().ReplaceLineEndings("\n").TrimEnd() + "\n";
	}

	private static void Member(StringBuilder md, ApiMember member, ApiType parent, bool python, string language)
	{
		md.Append("### ").AppendLine(MemberTitle(member, parent, false)).AppendLine();
		Fence(md, language, WithAttributes(member.Attributes, python, member.IsObsolete, member.Signature));

		if (member.IsObsolete)
		{
			md.Append("**Obsolete:** ")
				.AppendLine(string.IsNullOrWhiteSpace(member.ObsoleteMessage) ? "This member is deprecated." : member.ObsoleteMessage)
				.AppendLine();
		}

		if (member.Documentation is not { } doc)
		{
			return;
		}

		Text(md, doc.Summary);
		if (member.Parameters.Count > 0 && doc.Parameters.Count > 0)
		{
			md.AppendLine("Parameters:").AppendLine();
			foreach (ApiParameter parameter in member.Parameters)
			{
				Item(md, $"`{parameter.Name}` (`{ParameterModifiers(parameter)}{parameter.Type}`)",
					doc.Parameters.GetValueOrDefault(parameter.Name));
			}

			md.AppendLine();
		}

		if (ApiDocText.ToPlainText(doc.Returns) is { Length: > 0 } returns)
		{
			md.Append("Returns: ").AppendLine(returns).AppendLine();
		}

		if (ApiDocText.ToPlainText(doc.Value) is { Length: > 0 } value)
		{
			md.Append("Value: ").AppendLine(value).AppendLine();
		}
	}

	private static void Fence(StringBuilder md, string language, string code)
	{
		// A fence longer than any backtick run inside the code, so the code can't close it early.
		int longest = 0;
		int run = 0;
		foreach (char c in code)
		{
			run = c == '`' ? run + 1 : 0;
			longest = Math.Max(longest, run);
		}

		string fence = new('`', Math.Max(3, longest + 1));
		md.Append(fence).AppendLine(language).AppendLine(code).AppendLine(fence).AppendLine();
	}

	private static void Text(StringBuilder md, string? html)
	{
		if (ApiDocText.ToPlainText(html) is { Length: > 0 } text)
		{
			md.AppendLine(text).AppendLine();
		}
	}

	private static void Item(StringBuilder md, string code, string? html)
	{
		md.Append("- ").Append(code);
		if (ApiDocText.ToPlainText(html) is { Length: > 0 } text)
		{
			md.Append(": ").Append(text);
		}

		md.AppendLine();
	}
}
