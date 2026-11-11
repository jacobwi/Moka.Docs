namespace Moka.Docs.Core.Api;

/// <summary>
///     The complete API reference model for all analyzed projects/assemblies.
/// </summary>
public sealed record ApiReference
{
	/// <summary>All namespaces discovered across all analyzed assemblies.</summary>
	public List<ApiNamespace> Namespaces { get; init; } = [];

	/// <summary>Source assembly names that were analyzed.</summary>
	public List<string> Assemblies { get; init; } = [];

	/// <summary>
	///     Problems the analysis found that left documentation out, such as a doc comment that isn't
	///     well-formed XML. The build reports each one as a warning.
	/// </summary>
	public List<string> Warnings { get; init; } = [];
}

/// <summary>
///     A namespace containing types.
/// </summary>
public sealed record ApiNamespace
{
	/// <summary>Fully qualified namespace name.</summary>
	public required string Name { get; init; }

	/// <summary>Types within this namespace.</summary>
	public List<ApiType> Types { get; init; } = [];

	/// <inheritdoc />
	public override string ToString() => $"ApiNamespace({Name}, {Types.Count} types)";
}

/// <summary>
///     A type (class, struct, record, interface, enum, delegate).
/// </summary>
public sealed record ApiType
{
	/// <summary>Simple type name (e.g., "MyClass").</summary>
	public required string Name { get; init; }

	/// <summary>Fully qualified name including namespace.</summary>
	public required string FullName { get; init; }

	/// <summary>The kind of type.</summary>
	public required ApiTypeKind Kind { get; init; }

	/// <summary>Access modifier.</summary>
	public ApiAccessibility Accessibility { get; init; } = ApiAccessibility.Public;

	/// <summary>Whether the type is static.</summary>
	public bool IsStatic { get; init; }

	/// <summary>Whether the type is abstract.</summary>
	public bool IsAbstract { get; init; }

	/// <summary>Whether the type is sealed.</summary>
	public bool IsSealed { get; init; }

	/// <summary>Whether the type is a record.</summary>
	public bool IsRecord { get; init; }

	/// <summary>
	///     The declaration as C# writes it, without attributes, such as
	///     <c>public readonly record struct Money(decimal Amount, string Currency)</c> or
	///     <c>public delegate TResult Transform&lt;in T, out TResult&gt;(T input)</c>. When it's <c>null</c>,
	///     the page puts one together from the other properties.
	/// </summary>
	public string? Signature { get; init; }

	/// <summary>Generic type parameters.</summary>
	public List<ApiTypeParameter> TypeParameters { get; init; } = [];

	/// <summary>Base type, if any.</summary>
	public string? BaseType { get; init; }

	/// <summary>Implemented interfaces.</summary>
	public List<string> ImplementedInterfaces { get; init; } = [];

	/// <summary>Members of this type.</summary>
	public List<ApiMember> Members { get; init; } = [];

	/// <summary>XML documentation block.</summary>
	public XmlDocBlock? Documentation { get; init; }

	/// <summary>Attributes on this type.</summary>
	public List<ApiAttribute> Attributes { get; init; } = [];

	/// <summary>The containing namespace.</summary>
	public string? Namespace { get; init; }

	/// <summary>The assembly this type belongs to.</summary>
	public string? Assembly { get; init; }

	/// <summary>The source file path, if known.</summary>
	public string? SourcePath { get; init; }

	/// <summary>Whether the type is marked obsolete.</summary>
	public bool IsObsolete { get; init; }

	/// <summary>Obsolete message, if applicable.</summary>
	public string? ObsoleteMessage { get; init; }

	/// <summary>The original source code of the type declaration, if available.</summary>
	public string? SourceCode { get; init; }

	/// <summary>
	///     A link to the type's declaration in its repository, such as a GitHub page with a line
	///     anchor. The ASP.NET Core host reads it from the SourceLink data in the assembly's PDB;
	///     the CLI leaves it empty.
	/// </summary>
	public string? SourceUrl { get; init; }

	/// <inheritdoc />
	public override string ToString() => $"ApiType({Kind} {FullName})";
}

/// <summary>
///     The kind of a type.
/// </summary>
public enum ApiTypeKind
{
	/// <summary>A class.</summary>
	Class,

	/// <summary>A struct.</summary>
	Struct,

	/// <summary>A record class or record struct.</summary>
	Record,

	/// <summary>An interface.</summary>
	Interface,

	/// <summary>An enum.</summary>
	Enum,

	/// <summary>A delegate.</summary>
	Delegate
}

/// <summary>
///     Access modifier for a type or member.
/// </summary>
public enum ApiAccessibility
{
	/// <summary>Public access.</summary>
	Public,

	/// <summary>Protected access.</summary>
	Protected,

	/// <summary>Internal access.</summary>
	Internal,

	/// <summary>Protected internal access.</summary>
	ProtectedInternal,

	/// <summary>Private protected access.</summary>
	PrivateProtected,

	/// <summary>Private access.</summary>
	Private
}

/// <summary>
///     A member of a type (method, property, field, event, constructor, operator, indexer).
/// </summary>
public sealed record ApiMember
{
	/// <summary>Member name.</summary>
	public required string Name { get; init; }

	/// <summary>The kind of member.</summary>
	public required ApiMemberKind Kind { get; init; }

	/// <summary>The display signature (e.g., "void DoWork(string name, int count)").</summary>
	public required string Signature { get; init; }

	/// <summary>Return type, if applicable.</summary>
	public string? ReturnType { get; init; }

	/// <summary>Access modifier.</summary>
	public ApiAccessibility Accessibility { get; init; } = ApiAccessibility.Public;

	/// <summary>Whether the member is static.</summary>
	public bool IsStatic { get; init; }

	/// <summary>Whether the member is virtual.</summary>
	public bool IsVirtual { get; init; }

	/// <summary>Whether the member is abstract.</summary>
	public bool IsAbstract { get; init; }

	/// <summary>Whether the member is an override.</summary>
	public bool IsOverride { get; init; }

	/// <summary>Whether the member is sealed.</summary>
	public bool IsSealed { get; init; }

	/// <summary>
	///     Whether this is an extension method declared with <c>this</c> on its first parameter. Members of a
	///     C# 14 <c>extension</c> block are described by <see cref="Extension" /> instead.
	/// </summary>
	public bool IsExtensionMethod { get; init; }

	/// <summary>
	///     The <c>extension</c> block that declares the member, for a member of a C# 14 extension block;
	///     otherwise <c>null</c>. Such a member is an instance extension member unless <see cref="IsStatic" />
	///     is set, and its <see cref="Parameters" /> and <see cref="TypeParameters" /> are its own, without the
	///     block's receiver and type parameters.
	/// </summary>
	public ApiExtensionBlock? Extension { get; init; }

	/// <summary>Parameters for methods and indexers.</summary>
	public List<ApiParameter> Parameters { get; init; } = [];

	/// <summary>Generic type parameters for generic methods.</summary>
	public List<ApiTypeParameter> TypeParameters { get; init; } = [];

	/// <summary>XML documentation block.</summary>
	public XmlDocBlock? Documentation { get; init; }

	/// <summary>Attributes on this member.</summary>
	public List<ApiAttribute> Attributes { get; init; } = [];

	/// <summary>Whether the member is marked obsolete.</summary>
	public bool IsObsolete { get; init; }

	/// <summary>Obsolete message, if applicable.</summary>
	public string? ObsoleteMessage { get; init; }

	/// <inheritdoc />
	public override string ToString() => $"ApiMember({Kind} {Name})";
}

/// <summary>
///     The kind of a member.
/// </summary>
public enum ApiMemberKind
{
	/// <summary>A constructor.</summary>
	Constructor,

	/// <summary>A method.</summary>
	Method,

	/// <summary>A property.</summary>
	Property,

	/// <summary>A field.</summary>
	Field,

	/// <summary>An event.</summary>
	Event,

	/// <summary>An operator.</summary>
	Operator,

	/// <summary>An indexer.</summary>
	Indexer
}

/// <summary>
///     A parameter on a method, constructor, or indexer.
/// </summary>
public sealed record ApiParameter
{
	/// <summary>Parameter name.</summary>
	public required string Name { get; init; }

	/// <summary>Parameter type (display name).</summary>
	public required string Type { get; init; }

	/// <summary>Default value, if any.</summary>
	public string? DefaultValue { get; init; }

	/// <summary>Whether this parameter has a default value.</summary>
	public bool HasDefaultValue { get; init; }

	/// <summary>Whether this is a params parameter.</summary>
	public bool IsParams { get; init; }

	/// <summary>Whether this is a ref parameter.</summary>
	public bool IsRef { get; init; }

	/// <summary>Whether this is an out parameter.</summary>
	public bool IsOut { get; init; }

	/// <summary>Whether this is an in parameter.</summary>
	public bool IsIn { get; init; }

	/// <summary>Whether the parameter type is nullable.</summary>
	public bool IsNullable { get; init; }
}

/// <summary>
///     A C# 14 extension block, such as <c>extension(Shape shape) { ... }</c>, shared by the members it
///     declares.
/// </summary>
public sealed record ApiExtensionBlock
{
	/// <summary>
	///     The block's declaration as C# writes it, without namespaces, for example
	///     <c>extension(ref Point point)</c> or
	///     <c>extension&lt;T&gt;(List&lt;T&gt; list) where T : IComparable&lt;T&gt;</c>.
	/// </summary>
	public required string Signature { get; init; }

	/// <summary>
	///     The receiver parameter. Its name is empty when the block leaves it out, as a block with only
	///     static members may: <c>extension(Shape)</c>.
	/// </summary>
	public required ApiParameter Receiver { get; init; }

	/// <summary>The block's type parameters, with their constraints.</summary>
	public List<ApiTypeParameter> TypeParameters { get; init; } = [];

	/// <summary>
	///     The block's documentation: its summary, the <c>&lt;param&gt;</c> for the receiver and a
	///     <c>&lt;typeparam&gt;</c> for each type parameter.
	/// </summary>
	public XmlDocBlock? Documentation { get; init; }

	/// <summary>
	///     The documentation ID the compiler gives the block, such as
	///     <c>T:MyLib.ShapeExtensions.&lt;G&gt;$4B0A9C.&lt;M&gt;$1F2E3D</c>. The compiler documents the block's
	///     members on its grouping type, <c>&lt;G&gt;$4B0A9C</c>, which blocks for one receiver type share.
	/// </summary>
	public string? DocumentationId { get; init; }
}

/// <summary>
///     A generic type parameter with optional constraints.
/// </summary>
public sealed record ApiTypeParameter
{
	/// <summary>The type parameter name (e.g., "T").</summary>
	public required string Name { get; init; }

	/// <summary>Constraints on this type parameter.</summary>
	public List<string> Constraints { get; init; } = [];
}

/// <summary>
///     An attribute applied to a type or member.
/// </summary>
public sealed record ApiAttribute
{
	/// <summary>The attribute type name.</summary>
	public required string Name { get; init; }

	/// <summary>Constructor arguments.</summary>
	public List<string> Arguments { get; init; } = [];
}

/// <summary>
///     Parsed XML documentation block for a type or member.
/// </summary>
public sealed record XmlDocBlock
{
	/// <summary>The summary text (HTML).</summary>
	public string Summary { get; init; } = "";

	/// <summary>The remarks text (HTML).</summary>
	public string Remarks { get; init; } = "";

	/// <summary>Parameter documentation, keyed by parameter name.</summary>
	public Dictionary<string, string> Parameters { get; init; } = [];

	/// <summary>Type parameter documentation, keyed by type parameter name.</summary>
	public Dictionary<string, string> TypeParameters { get; init; } = [];

	/// <summary>Return value documentation.</summary>
	public string Returns { get; init; } = "";

	/// <summary>Value documentation (for properties).</summary>
	public string Value { get; init; } = "";

	/// <summary>Exception documentation.</summary>
	public List<ExceptionDoc> Exceptions { get; init; } = [];

	/// <summary>Example code blocks.</summary>
	public List<string> Examples { get; init; } = [];

	/// <summary>See-also references.</summary>
	public List<string> SeeAlso { get; init; } = [];

	/// <summary>Whether this documentation was inherited via inheritdoc.</summary>
	public bool IsInherited { get; init; }

	/// <summary>
	///     Whether the source comment contains an <c>&lt;inheritdoc/&gt;</c> tag. Stays true
	///     when the tag could not be resolved, for example when the base is a framework type
	///     such as <see cref="object.ToString" /> whose documentation is not part of the
	///     model. Lets coverage reporting tell "documented by inheritance" apart from
	///     "never documented", which an empty <see cref="Summary" /> alone cannot.
	/// </summary>
	public bool HasInheritDocTag { get; init; }

	/// <summary>
	///     The documentation ID named by <c>&lt;inheritdoc cref="..."/&gt;</c> (for example
	///     <c>M:MyLib.Parser.Parse(System.String)</c>), or <c>null</c> when the comment has no cref.
	/// </summary>
	public string? InheritDocCref { get; init; }
}

/// <summary>
///     Documentation for a thrown exception.
/// </summary>
public sealed record ExceptionDoc
{
	/// <summary>The exception type (cref).</summary>
	public required string Type { get; init; }

	/// <summary>Description of when this exception is thrown.</summary>
	public required string Description { get; init; }
}
