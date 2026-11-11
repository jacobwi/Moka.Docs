using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Produces the source shown in a type page's View Source panel.
/// </summary>
internal static class DocumentedSource
{
	private const string _newLine = "\n";

	/// <summary>
	///     Renders every declaration of a type as the author wrote it, leaving out the members the page
	///     does not document.
	/// </summary>
	/// <param name="compilation">The compilation the declarations belong to.</param>
	/// <param name="declarations">
	///     The type's declarations: one, or one per part of a partial type, in syntax tree order.
	/// </param>
	/// <param name="isDocumented">Whether a member or nested type is documented.</param>
	/// <returns>The source, with the indentation its lines share removed and LF line breaks.</returns>
	/// <remarks>
	///     The text used to go through <c>NormalizeWhitespace</c>, which reformatted the author's code and
	///     took about half of the analysis time.
	/// </remarks>
	public static string Render(
		Compilation compilation,
		IReadOnlyList<MemberDeclarationSyntax> declarations,
		Func<ISymbol, bool> isDocumented)
	{
		var parts = new List<string>(declarations.Count);
		foreach (MemberDeclarationSyntax declaration in declarations)
		{
			SemanticModel model = compilation.GetSemanticModel(declaration.SyntaxTree);
			var hidden = new List<SyntaxNode>();
			var hiddenEnumMembers = new Dictionary<EnumDeclarationSyntax, HashSet<int>>();
			CollectHiddenMembers(declaration, model, isDocumented, hidden, hiddenEnumMembers);

			string text = Tidy(WithoutBlankLineAfterOpenBrace(WithoutHidden(declaration, hidden, hiddenEnumMembers)).ToFullString());

			// Two identical-looking "partial class" blocks are confusing without knowing where
			// each one lives.
			string fileName = Path.GetFileName(declaration.SyntaxTree.FilePath);
			if (declarations.Count > 1 && fileName.Length > 0)
			{
				text = $"// {fileName}{_newLine}{text}";
			}

			parts.Add(text);
		}

		return string.Join(_newLine + _newLine, parts);
	}

	private static SyntaxNode WithoutHidden(
		SyntaxNode declaration,
		List<SyntaxNode> hidden,
		Dictionary<EnumDeclarationSyntax, HashSet<int>> hiddenEnumMembers)
	{
		if (hidden.Count == 0 && hiddenEnumMembers.Count == 0)
		{
			return declaration;
		}

		SyntaxNode visible = declaration.TrackNodes(hidden.Concat(hiddenEnumMembers.Keys));
		foreach ((EnumDeclarationSyntax enumDeclaration, HashSet<int> hiddenIndexes) in hiddenEnumMembers)
		{
			if (visible.GetCurrentNode(enumDeclaration) is { } current)
			{
				visible = visible.ReplaceNode(current, WithoutEnumMembers(current, hiddenIndexes));
			}
		}

		// A removed member can carry the #region or #if whose closing directive sits on a
		// member that stays. Dropping it with the member would orphan the closing one.
		return hidden.Count == 0
			? visible
			: visible.RemoveNodes(visible.GetCurrentNodes(hidden), SyntaxRemoveOptions.KeepUnbalancedDirectives) ?? visible;
	}

	/// <summary>
	///     The enum without the members at <paramref name="hidden" />. <c>RemoveNodes</c> takes the comma
	///     before a removed last member along with its line break, which put the closing brace on the
	///     line of the member before it.
	/// </summary>
	private static EnumDeclarationSyntax WithoutEnumMembers(EnumDeclarationSyntax declaration, HashSet<int> hidden)
	{
		SeparatedSyntaxList<EnumMemberDeclarationSyntax> members = declaration.Members;
		bool trailingSeparator = members.SeparatorCount == members.Count;
		List<int> kept = Enumerable.Range(0, members.Count).Where(i => !hidden.Contains(i)).ToList();

		var list = new List<SyntaxNodeOrToken>();
		for (int position = 0; position < kept.Count; position++)
		{
			int index = kept[position];
			EnumMemberDeclarationSyntax member = members[index];
			bool hasSeparator = index < members.SeparatorCount;
			if (position < kept.Count - 1 || trailingSeparator)
			{
				list.Add(member);
				list.Add(hasSeparator ? members.GetSeparator(index) : SyntaxFactory.Token(SyntaxKind.CommaToken));
			}
			else
			{
				list.Add(hasSeparator
					? member.WithTrailingTrivia(member.GetTrailingTrivia().AddRange(members.GetSeparator(index).TrailingTrivia))
					: member);
			}
		}

		return declaration.WithMembers(SyntaxFactory.SeparatedList<EnumMemberDeclarationSyntax>(list));
	}

	/// <summary>
	///     Removes the blank lines before the first member of each type. They used to separate that member
	///     from one the panel leaves out.
	/// </summary>
	private static SyntaxNode WithoutBlankLineAfterOpenBrace(SyntaxNode node)
	{
		List<MemberDeclarationSyntax> firstMembers = node.DescendantNodesAndSelf()
			.OfType<TypeDeclarationSyntax>()
			.Where(type => type.Members.Count > 0 && StartsWithBlankLine(type.Members[0].GetLeadingTrivia()))
			.Select(type => type.Members[0])
			.ToList();

		return firstMembers.Count == 0
			? node
			: node.ReplaceNodes(firstMembers, (original, _) =>
				original.WithLeadingTrivia(SkipBlankLines(original.GetLeadingTrivia())));
	}

	private static bool StartsWithBlankLine(SyntaxTriviaList trivia) =>
		trivia.SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).FirstOrDefault().IsKind(SyntaxKind.EndOfLineTrivia);

	private static SyntaxTriviaList SkipBlankLines(SyntaxTriviaList trivia)
	{
		int start = 0;
		for (int i = 0; i < trivia.Count; i++)
		{
			if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
			{
				start = i + 1;
			}
			else if (!trivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
			{
				break;
			}
		}

		return SyntaxFactory.TriviaList(trivia.Skip(start));
	}

	/// <summary>
	///     Trims the blank lines around the text and the indentation its lines share, such as the one a
	///     nested type or a block-scoped namespace adds.
	/// </summary>
	internal static string Tidy(string text)
	{
		string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
			.Select(line => string.IsNullOrWhiteSpace(line) ? "" : line)
			.ToArray();

		int start = Array.FindIndex(lines, line => line.Length > 0);
		if (start < 0)
		{
			return "";
		}

		int end = Array.FindLastIndex(lines, line => line.Length > 0);
		string[] kept = lines[start..(end + 1)];

		string? indent = null;
		foreach (string line in kept.Where(line => line.Length > 0))
		{
			string lineIndent = line[..(line.Length - line.TrimStart(' ', '\t').Length)];
			indent = indent is null ? lineIndent : CommonPrefix(indent, lineIndent);
			if (indent.Length == 0)
			{
				break;
			}
		}

		int width = indent?.Length ?? 0;
		return string.Join(_newLine, kept.Select(line => line.Length >= width ? line[width..] : line));
	}

	private static string CommonPrefix(string a, string b)
	{
		int length = 0;
		while (length < a.Length && length < b.Length && a[length] == b[length])
		{
			length++;
		}

		return a[..length];
	}

	private static void CollectHiddenMembers(
		MemberDeclarationSyntax declaration,
		SemanticModel model,
		Func<ISymbol, bool> isDocumented,
		List<SyntaxNode> hidden,
		Dictionary<EnumDeclarationSyntax, HashSet<int>> hiddenEnumMembers)
	{
		if (declaration is EnumDeclarationSyntax enumDeclaration)
		{
			HashSet<int> indexes = Enumerable.Range(0, enumDeclaration.Members.Count)
				.Where(i => model.GetDeclaredSymbol(enumDeclaration.Members[i]) is not { } symbol || !isDocumented(symbol))
				.ToHashSet();
			if (indexes.Count > 0)
			{
				hiddenEnumMembers[enumDeclaration] = indexes;
			}

			return;
		}

		// Delegates have no members.
		if (declaration is not TypeDeclarationSyntax type)
		{
			return;
		}

		foreach (MemberDeclarationSyntax member in type.Members)
		{
			// All variables of one field declaration share its modifiers, so the first decides.
			ISymbol? symbol = member is BaseFieldDeclarationSyntax field
				? field.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v)).FirstOrDefault()
				: model.GetDeclaredSymbol(member);

			if (symbol is null || !isDocumented(symbol))
			{
				hidden.Add(member);
			}
			else if (member is BaseTypeDeclarationSyntax)
			{
				CollectHiddenMembers(member, model, isDocumented, hidden, hiddenEnumMembers);
			}
		}
	}
}
