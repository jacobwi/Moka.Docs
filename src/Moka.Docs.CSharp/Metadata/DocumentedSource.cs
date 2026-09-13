using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Produces the source shown in a type page's View Source panel.
/// </summary>
internal static class DocumentedSource
{
	// NormalizeWhitespace writes CRLF by default; parts are joined the same way.
	private const string _newLine = "\r\n";

	/// <summary>
	///     Renders every declaration of a type, leaving out the members the page does not document.
	/// </summary>
	/// <param name="compilation">The compilation the declarations belong to.</param>
	/// <param name="declarations">
	///     The type's declarations: one, or one per part of a partial type, in syntax tree order.
	/// </param>
	/// <param name="isDocumented">Whether a member or nested type is documented.</param>
	/// <returns>The formatted source.</returns>
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
			CollectHiddenMembers(declaration, model, isDocumented, hidden);

			// A removed member can carry the #region or #if whose closing directive sits on a
			// member that stays. Dropping it with the member would orphan the closing one.
			SyntaxNode visible = hidden.Count == 0
				? declaration
				: declaration.RemoveNodes(hidden, SyntaxRemoveOptions.KeepUnbalancedDirectives) ?? declaration;

			string text = visible.NormalizeWhitespace().ToFullString();

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

	private static void CollectHiddenMembers(
		MemberDeclarationSyntax declaration,
		SemanticModel model,
		Func<ISymbol, bool> isDocumented,
		List<SyntaxNode> hidden)
	{
		// Enum members are all documented, and delegates have no members.
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
			else if (member is TypeDeclarationSyntax)
			{
				CollectHiddenMembers(member, model, isDocumented, hidden);
			}
		}
	}
}
