using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Builds member signatures that read like the C# declaration, for example
///     <c>public static string Shout(this string value)</c> or
///     <c>public string Name { get; private set; }</c>.
/// </summary>
internal static class MemberSignatures
{
	/// <summary>
	///     Everything after the modifiers: type, name, type parameters, parameters, constraints
	///     and constant values. Accessibility and modifiers are written by <see cref="Prefix" />:
	///     <c>new</c> only shows in the declaration syntax, and interface members must leave out
	///     the modifiers their declarations imply.
	/// </summary>
	private static readonly SymbolDisplayFormat _declarationFormat = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
		                 | SymbolDisplayGenericsOptions.IncludeTypeConstraints
		                 | SymbolDisplayGenericsOptions.IncludeVariance,
		memberOptions: SymbolDisplayMemberOptions.IncludeType
		               | SymbolDisplayMemberOptions.IncludeParameters
		               | SymbolDisplayMemberOptions.IncludeRef
		               | SymbolDisplayMemberOptions.IncludeConstantValue,
		delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
		extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
		parameterOptions: SymbolDisplayParameterOptions.IncludeExtensionThis
		                  | SymbolDisplayParameterOptions.IncludeModifiers
		                  | SymbolDisplayParameterOptions.IncludeType
		                  | SymbolDisplayParameterOptions.IncludeName
		                  | SymbolDisplayParameterOptions.IncludeDefaultValue,
		propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
		kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
		                      | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
		                      | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
		                      | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName
		                      | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

	private static readonly SymbolDisplayFormat _typeFormat = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
		                      | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
		                      | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
		                      | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

	/// <summary>
	///     A type the way the signatures write it: no namespaces, keywords for built-in types and
	///     nullable annotations kept, for example <c>IReadOnlyList&lt;ApiType&gt;?</c>.
	/// </summary>
	public static string ForType(ITypeSymbol type) => type.ToDisplayString(_typeFormat);

	/// <summary>
	///     The signature of a method, constructor, operator, property, indexer, field or event.
	/// </summary>
	public static string ForMember(ISymbol member)
	{
		string signature = Prefix(member) + member.ToDisplayString(_declarationFormat);
		return member is IPropertySymbol property ? signature + Accessors(property) : signature;
	}

	/// <summary>
	///     The declaration of a delegate type, for example
	///     <c>public delegate TResult Transform&lt;in T, out TResult&gt;(T input)</c>.
	/// </summary>
	public static string ForDelegate(INamedTypeSymbol delegateType) =>
		Prefix(delegateType) + "delegate " + delegateType.ToDisplayString(_declarationFormat);

	private static string Prefix(ISymbol symbol)
	{
		var words = new List<string>();
		bool inInterface = symbol.ContainingType?.TypeKind == TypeKind.Interface;
		SyntaxTokenList modifiers = DeclaredModifiers(symbol);

		// Interface members are public unless they say otherwise, and their declarations leave it out.
		if (!(inInterface && symbol.DeclaredAccessibility == Accessibility.Public)
		    && AccessibilityKeyword(symbol.DeclaredAccessibility) is { } accessibility)
		{
			words.Add(accessibility);
		}

		if (symbol is IFieldSymbol { IsConst: true })
		{
			words.Add("const");
		}
		else if (symbol.IsStatic && symbol is not INamedTypeSymbol)
		{
			words.Add("static");
		}

		if (symbol.IsExtern)
		{
			words.Add("extern");
		}

		if (modifiers.Any(SyntaxKind.NewKeyword))
		{
			words.Add("new");
		}

		// An instance interface member is implicitly abstract without a body and virtual with
		// one, and its declaration says neither. Static abstract and static virtual are written.
		bool implicitInterfaceModifier = inInterface && !symbol.IsStatic;
		if (symbol.IsVirtual && !implicitInterfaceModifier)
		{
			words.Add("virtual");
		}

		if (symbol.IsAbstract && !implicitInterfaceModifier && symbol is not INamedTypeSymbol)
		{
			words.Add("abstract");
		}

		if (symbol.IsSealed && symbol is not INamedTypeSymbol)
		{
			words.Add("sealed");
		}

		if (symbol.IsOverride)
		{
			words.Add("override");
		}

		// Roslyn's display already writes readonly for struct methods and properties, but not for fields.
		if (symbol is IFieldSymbol { IsReadOnly: true })
		{
			words.Add("readonly");
		}

		if (symbol is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
		{
			words.Add("required");
		}

		if (symbol is IFieldSymbol { IsVolatile: true })
		{
			words.Add("volatile");
		}

		return words.Count == 0 ? "" : string.Join(' ', words) + " ";
	}

	private static string Accessors(IPropertySymbol property)
	{
		var accessors = new List<string>(2);
		if (property.GetMethod is { } getter)
		{
			accessors.Add(Accessor(property, getter, "get"));
		}

		if (property.SetMethod is { } setter)
		{
			accessors.Add(Accessor(property, setter, setter.IsInitOnly ? "init" : "set"));
		}

		return accessors.Count == 0 ? "" : $" {{ {string.Join(' ', accessors)} }}";
	}

	private static string Accessor(IPropertySymbol property, IMethodSymbol accessor, string keyword)
	{
		// Only an accessor narrower than its property declares its own accessibility.
		return accessor.DeclaredAccessibility != property.DeclaredAccessibility
		       && AccessibilityKeyword(accessor.DeclaredAccessibility) is { } accessibility
			? $"{accessibility} {keyword};"
			: $"{keyword};";
	}

	private static SyntaxTokenList DeclaredModifiers(ISymbol symbol)
	{
		foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
		{
			switch (reference.GetSyntax())
			{
				case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax field }:
					return field.Modifiers;
				case MemberDeclarationSyntax member:
					return member.Modifiers;
			}
		}

		return default;
	}

	private static string? AccessibilityKeyword(Accessibility accessibility)
	{
		return accessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Protected => "protected",
			Accessibility.Internal => "internal",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.Private => "private",
			_ => null
		};
	}
}
