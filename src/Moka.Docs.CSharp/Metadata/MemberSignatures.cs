using System.Text;
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
		// Default values are written by AppendDefaults, as the source spells them.
		parameterOptions: SymbolDisplayParameterOptions.IncludeExtensionThis
		                  | SymbolDisplayParameterOptions.IncludeModifiers
		                  | SymbolDisplayParameterOptions.IncludeType
		                  | SymbolDisplayParameterOptions.IncludeName,
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

	// The name and type parameters of a type declaration: Transform<in T, out TResult>.
	private static readonly SymbolDisplayFormat _typeNameFormat = new(
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

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
		string signature = Prefix(member) + Display(member);
		return member is IPropertySymbol property ? signature + Accessors(property) : signature;
	}

	/// <summary>
	///     The declaration of a delegate type, for example
	///     <c>public delegate TResult Transform&lt;in T, out TResult&gt;(T input)</c>.
	/// </summary>
	public static string ForDelegate(INamedTypeSymbol delegateType) =>
		Prefix(delegateType) + "delegate " + Display(delegateType);

	/// <summary>
	///     The declaration line of a class, struct, record, interface or enum as the source writes it,
	///     without attributes and type parameter constraints: <c>public readonly record struct
	///     Money(decimal Amount, string Currency)</c>, <c>public sealed class Parser : Ns.ParserBase</c>,
	///     <c>public enum Flags : byte</c>. A delegate gets <see cref="ForDelegate" />.
	/// </summary>
	/// <remarks>
	///     The page used to build this line from the model, so a record struct read
	///     <c>public record Money : System.ValueType, System.IEquatable&lt;Money&gt;</c>: no <c>struct</c>,
	///     no <c>readonly</c>, and the base type and interface the compiler adds.
	/// </remarks>
	public static string ForTypeDeclaration(INamedTypeSymbol type, IReadOnlyList<MemberDeclarationSyntax> declarations)
	{
		if (type.TypeKind == TypeKind.Delegate)
		{
			return ForDelegate(type);
		}

		var words = new List<string>();
		if (AccessibilityKeyword(type.DeclaredAccessibility) is { } accessibility)
		{
			words.Add(accessibility);
		}

		if (declarations.Any(d => d.Modifiers.Any(SyntaxKind.NewKeyword)))
		{
			words.Add("new");
		}

		if (type.TypeKind == TypeKind.Class)
		{
			if (type.IsStatic)
			{
				words.Add("static");
			}
			else if (type.IsAbstract)
			{
				words.Add("abstract");
			}
			else if (type.IsSealed)
			{
				words.Add("sealed");
			}
		}

		if (type.TypeKind == TypeKind.Struct && type.IsReadOnly)
		{
			words.Add("readonly");
		}

		if (type.IsRefLikeType)
		{
			words.Add("ref");
		}

		words.Add(type.TypeKind switch
		{
			TypeKind.Struct when type.IsRecord => "record struct",
			TypeKind.Struct => "struct",
			TypeKind.Interface => "interface",
			TypeKind.Enum => "enum",
			_ when type.IsRecord => "record",
			_ => "class"
		});

		var sb = new StringBuilder(string.Join(' ', words));
		sb.Append(' ').Append(type.ToDisplayString(_typeNameFormat));

		if (PrimaryConstructor(type) is { } constructor)
		{
			sb.Append('(').Append(string.Join(", ", constructor.Parameters.Select(Display))).Append(')');
		}

		List<string> bases = BaseList(type, declarations);
		if (bases.Count > 0)
		{
			sb.Append(" : ").Append(string.Join(", ", bases));
		}

		return sb.ToString();
	}

	/// <summary>
	///     A parameter's default value as the source writes it, such as <c>DayOfWeek.Friday</c>,
	///     <c>double.NaN</c> or <c>default</c>. Roslyn's own display wrote <c>Friday</c> and <c>NaN</c>.
	/// </summary>
	public static string DefaultValue(IParameterSymbol parameter)
	{
		foreach (SyntaxReference reference in parameter.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax() is ParameterSyntax { Default.Value: { } value })
			{
				return value.ToString();
			}
		}

		return FormatConstant(parameter.Type, parameter.ExplicitDefaultValue);
	}

	private static string FormatConstant(ITypeSymbol type, object? value)
	{
		if (value is null)
		{
			return type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T ? "default" : "null";
		}

		if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
		{
			string enumName = ForType(enumType);
			return enumType.GetMembers().OfType<IFieldSymbol>()
				.FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, value)) is { } field
				? $"{enumName}.{field.Name}"
				: $"({enumName}){value}";
		}

		return value switch
		{
			double d when double.IsNaN(d) => "double.NaN",
			double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
			double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
			float f when float.IsNaN(f) => "float.NaN",
			float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
			float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
			_ => SymbolDisplay.FormatPrimitive(value, true, false) ?? value.ToString() ?? ""
		};
	}

	/// <summary>
	///     The symbol's display with each parameter's default value appended, as the source writes it.
	/// </summary>
	private static string Display(ISymbol symbol)
	{
		var sb = new StringBuilder();
		foreach (SymbolDisplayPart part in symbol.ToDisplayParts(_declarationFormat))
		{
			sb.Append(part.ToString());
			if (part.Kind == SymbolDisplayPartKind.ParameterName
			    && part.Symbol is IParameterSymbol { HasExplicitDefaultValue: true } parameter)
			{
				sb.Append(" = ").Append(DefaultValue(parameter));
			}
		}

		return sb.ToString();
	}

	// A record's or class's primary constructor is the one declared by the type declaration itself.
	private static IMethodSymbol? PrimaryConstructor(INamedTypeSymbol type) =>
		type.InstanceConstructors.FirstOrDefault(constructor =>
			constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax));

	/// <summary>
	///     The base type and interfaces the declaration names, written in full as the Inheritance section
	///     writes them. The ones every struct, enum or record gets from the compiler are left out.
	/// </summary>
	private static List<string> BaseList(INamedTypeSymbol type, IReadOnlyList<MemberDeclarationSyntax> declarations)
	{
		var bases = new List<string>();
		if (type.TypeKind == TypeKind.Enum)
		{
			if (type.EnumUnderlyingType is { SpecialType: not SpecialType.System_Int32 } underlying)
			{
				bases.Add(underlying.ToDisplayString());
			}

			return bases;
		}

		if (type.TypeKind == TypeKind.Class && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
		{
			bases.Add(baseType.ToDisplayString());
		}

		bool declaresEquatable = declarations.OfType<TypeDeclarationSyntax>()
			.Any(d => d.BaseList?.Types.Any(t => t.Type.ToString().Contains("IEquatable", StringComparison.Ordinal)) == true);
		foreach (INamedTypeSymbol iface in type.Interfaces)
		{
			bool synthesized = type.IsRecord && !declaresEquatable
			                                 && iface.OriginalDefinition.ToDisplayString() == "System.IEquatable<T>"
			                                 && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], type);
			if (!synthesized)
			{
				bases.Add(iface.ToDisplayString());
			}
		}

		return bases;
	}

	// Roslyn displays a block as a member of its static class with only the receiver's type, as in
	// ShapeExtensions.extension(Shape), so the declaration is put together here.
	public static string ForExtensionBlock(INamedTypeSymbol extension)
	{
		var sb = new StringBuilder("extension");
		if (extension.TypeParameters.Length > 0)
		{
			sb.Append('<').Append(string.Join(", ", extension.TypeParameters.Select(tp => tp.Name))).Append('>');
		}

		sb.Append('(').Append(extension.ExtensionParameter?.ToDisplayString(_declarationFormat)).Append(')');

		foreach (ITypeParameterSymbol typeParameter in extension.TypeParameters)
		{
			List<string> constraints = Constraints(typeParameter);
			if (constraints.Count > 0)
			{
				sb.Append($" where {typeParameter.Name} : {string.Join(", ", constraints)}");
			}
		}

		return sb.ToString();
	}

	private static List<string> Constraints(ITypeParameterSymbol typeParameter)
	{
		var constraints = new List<string>();

		// unmanaged implies the struct constraint, and C# writes only one of the two.
		if (typeParameter.HasUnmanagedTypeConstraint)
		{
			constraints.Add("unmanaged");
		}
		else if (typeParameter.HasValueTypeConstraint)
		{
			constraints.Add("struct");
		}
		else if (typeParameter.HasReferenceTypeConstraint)
		{
			constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
				? "class?"
				: "class");
		}
		else if (typeParameter.HasNotNullConstraint)
		{
			constraints.Add("notnull");
		}

		constraints.AddRange(typeParameter.ConstraintTypes.Select(ForType));

		if (typeParameter.HasConstructorConstraint)
		{
			constraints.Add("new()");
		}

		if (typeParameter.AllowsRefLikeType)
		{
			constraints.Add("allows ref struct");
		}

		return constraints;
	}

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
