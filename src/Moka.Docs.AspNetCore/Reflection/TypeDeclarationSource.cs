using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     The type declarations in an assembly's C# source files, parsed with Roslyn, for the View
///     Source section the CLI's pages have.
/// </summary>
/// <remarks>
///     The compilation has no references. Declaring a symbol needs none, and accessibility and
///     modifiers come from the syntax, which is all the member filter reads.
/// </remarks>
internal sealed class TypeDeclarationSource
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	private readonly CSharpCompilation _compilation;
	private readonly Dictionary<string, List<MemberDeclarationSyntax>> _declarations;

	private TypeDeclarationSource(CSharpCompilation compilation, Dictionary<string, List<MemberDeclarationSyntax>> declarations)
	{
		_compilation = compilation;
		_declarations = declarations;
	}

	/// <summary>
	///     Parses the C# documents whose text is known. Returns null when there are none.
	/// </summary>
	public static TypeDeclarationSource? Create(string assemblyName, IEnumerable<PdbDocument> documents)
	{
		List<SyntaxTree> trees = documents
			.Where(d => d.IsCSharp && d.Text is not null)
			.Select(d => CSharpSyntaxTree.ParseText(d.Text!, _parseOptions, d.Path))
			.ToList();
		if (trees.Count == 0)
		{
			return null;
		}

		CSharpCompilation compilation = CSharpCompilation.Create(assemblyName + ".Source", trees);
		var declarations = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.Ordinal);

		// Trees in PDB order, declarations in file order: the parts of a partial type come out
		// in the order the compiler saw them.
		foreach (SyntaxTree tree in trees)
		{
			SemanticModel model = compilation.GetSemanticModel(tree);
			foreach (MemberDeclarationSyntax declaration in tree.GetRoot().DescendantNodes()
				         .OfType<MemberDeclarationSyntax>()
				         .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
			{
				if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
				{
					continue;
				}

				string key = MetadataFullName(symbol);
				if (!declarations.TryGetValue(key, out List<MemberDeclarationSyntax>? parts))
				{
					parts = [];
					declarations[key] = parts;
				}

				parts.Add(declaration);
			}
		}

		return new TypeDeclarationSource(compilation, declarations);
	}

	/// <summary>
	///     The type's declarations without the members its page leaves out, or null when the
	///     source has none.
	/// </summary>
	public string? Render(Type type)
	{
		if (type.FullName is not { } fullName
		    || !_declarations.TryGetValue(fullName, out List<MemberDeclarationSyntax>? declarations))
		{
			return null;
		}

		return DocumentedSource.Render(_compilation, declarations, IsDocumented);
	}

	/// <summary>
	///     The rules <see cref="ReflectionApiModelBuilder" /> applies to members and nested types:
	///     public ones, protected ones when the containing type can be derived from, and none
	///     marked <c>[EditorBrowsable(EditorBrowsableState.Never)]</c>.
	/// </summary>
	private static bool IsDocumented(ISymbol symbol)
	{
		if (IsHiddenFromEditors(symbol))
		{
			return false;
		}

		bool canDerive = symbol.ContainingType is { IsSealed: false, IsStatic: false, TypeKind: TypeKind.Class or TypeKind.Interface };
		return symbol.DeclaredAccessibility switch
		{
			Accessibility.Public => true,
			Accessibility.Protected or Accessibility.ProtectedOrInternal => canDerive,
			_ => false
		};
	}

	/// <summary>
	///     Read from the syntax: without references the attribute's type and argument don't bind.
	/// </summary>
	private static bool IsHiddenFromEditors(ISymbol symbol) =>
		symbol.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax())
			.Select(node => node is VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax field } ? field : node)
			.OfType<MemberDeclarationSyntax>()
			.SelectMany(member => member.AttributeLists)
			.SelectMany(list => list.Attributes)
			.Any(IsEditorBrowsableNever);

	private static bool IsEditorBrowsableNever(AttributeSyntax attribute)
	{
		string name = attribute.Name switch
		{
			QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.ValueText,
			SimpleNameSyntax simple => simple.Identifier.ValueText,
			_ => ""
		};

		return name is "EditorBrowsable" or "EditorBrowsableAttribute"
		       && attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is MemberAccessExpressionSyntax
		       {
			       Name.Identifier.ValueText: "Never"
		       };
	}

	/// <summary>
	///     The name reflection gives the type: <c>Namespace.Outer`1+Inner</c>.
	/// </summary>
	private static string MetadataFullName(INamedTypeSymbol symbol)
	{
		string name = symbol.MetadataName;
		INamedTypeSymbol outermost = symbol;
		while (outermost.ContainingType is { } container)
		{
			name = container.MetadataName + "+" + name;
			outermost = container;
		}

		return outermost.ContainingNamespace is { IsGlobalNamespace: false } ns
			? ns.ToDisplayString() + "." + name
			: name;
	}
}
