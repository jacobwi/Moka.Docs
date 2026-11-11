using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Works out what a type page lists besides the type's own members: the members it inherits from
///     documented base types and interfaces, and the documented extension methods and extension block
///     members that apply to it.
/// </summary>
/// <remarks>
///     A type reference such as a base type, an interface, an extension method's <c>this</c> parameter or an
///     extension block's receiver
///     is matched against the documented types by name, with each type argument list replaced by its
///     arity, so <c>Base&lt;int&gt;</c> finds <c>Base&lt;T&gt;</c>. Both hosts write base types and interfaces
///     with their namespace, but names don't have to carry it:
///     <list type="bullet">
///         <item>
///             <description>
///                 The CLI compiles each project on its own, so a type from another analyzed project is
///                 written without its namespace, and an interface from there is recorded as the base type.
///             </description>
///         </item>
///         <item>
///             <description>
///                 The reflection host writes parameter types, and so extension method receivers, without
///                 their namespace.
///             </description>
///         </item>
///     </list>
///     Documented types are keyed by their documentation ID (<c>Ns.Outer`1.Inner</c>), which both hosts'
///     models give the same way, although the reflection host's full names leave type parameters out. A name
///     matches the documented types whose key ends with it, and the one whose key is the whole name wins.
///     When several match otherwise, the one in the referencing type's namespace wins, then the one in each
///     enclosing namespace, the way C# looks names up. A qualified name never matches a type in another
///     namespace, so <c>System.Exception</c> doesn't find a documented type that happens to be called
///     <c>Exception</c>.
/// </remarks>
internal sealed partial class ApiTypeRelations
{
	// The phases render every type page against the same list, as ApiCrefResolver does.
	private static readonly ConditionalWeakTable<IReadOnlyList<ApiType>, ApiTypeRelations> _cache = new();
	private static readonly IReadOnlyDictionary<string, string> _noTypeArguments = new Dictionary<string, string>();

	// Constraints that say what kind of type an argument is, not which type it has to derive from.
	private static readonly HashSet<string> _specialConstraints = new(StringComparer.Ordinal)
	{
		"class", "class?", "struct", "notnull", "unmanaged", "new()", "default", "allows ref struct"
	};

	private readonly List<(ApiExtensionMember Extension, List<ApiType> Constraints)> _constrainedExtensions = [];

	private readonly ConcurrentDictionary<ApiType, IReadOnlyList<ApiExtensionMember>> _extensions =
		new(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<ApiType, List<ApiExtensionMember>> _extensionsByReceiver =
		new(ReferenceEqualityComparer.Instance);

	private readonly ConcurrentDictionary<ApiType, IReadOnlyList<ApiInheritedGroup>> _inherited =
		new(ReferenceEqualityComparer.Instance);

	private readonly ConcurrentDictionary<ApiType, Lineage> _lineages = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<string, List<(string Key, ApiType Type)>> _typesByName = new(StringComparer.Ordinal);

	private ApiTypeRelations(IEnumerable<ApiType> types)
	{
		// Python pages live under the plugin's route prefix, and Python has no extension methods.
		List<ApiType> documented = types.Where(t => !ApiPageRenderer.IsPython(t)).ToList();
		ApiTypeRoutes routes = ApiTypeRoutes.For(documented);

		foreach (ApiType type in documented)
		{
			string key = routes.DocumentationId(type);
			string name = key[(key.LastIndexOf('.') + 1)..];
			if (!_typesByName.TryGetValue(name, out List<(string Key, ApiType Type)>? sameName))
			{
				sameName = [];
				_typesByName[name] = sameName;
			}

			sameName.Add((key, type));
		}

		foreach (ApiType type in documented)
		foreach (ApiMember member in type.Members)
		{
			if (member.Extension is { } block)
			{
				IndexExtension(type, member, block.Receiver.Type, block.TypeParameters);
			}
			else if (member.IsExtensionMethod && member.Parameters.Count > 0)
			{
				IndexExtension(type, member, member.Parameters[0].Type, member.TypeParameters);
			}
		}
	}

	/// <summary>
	///     Gets the relations between the types of a page's type list, or none when there is no list.
	/// </summary>
	internal static ApiTypeRelations For(IReadOnlyList<ApiType>? allTypes) =>
		// A shared empty instance would keep every type it was asked about for the life of the process.
		allTypes is null ? new ApiTypeRelations([]) : _cache.GetValue(allTypes, list => new ApiTypeRelations(list));

	/// <summary>
	///     The members a class, struct, record or interface inherits from documented types, grouped by the
	///     type that declares them, nearest first.
	/// </summary>
	/// <remarks>
	///     A class inherits from its base type chain, an interface from the interfaces it extends. Constructors
	///     and operators are left out, and so is every member that the type, or a type between it and the
	///     declaring type, declares again with the same name and parameter types: an override, or a member
	///     hidden with <c>new</c>. Same-named members of two interfaces neither of which extends the other are
	///     both listed. A sealed type also leaves out protected members.
	/// </remarks>
	internal IReadOnlyList<ApiInheritedGroup> InheritedMembers(ApiType type) =>
		_inherited.GetOrAdd(type, BuildInheritedMembers);

	/// <summary>
	///     The documented extension methods whose <c>this</c> parameter, and the documented extension block
	///     members whose receiver, is the type, a documented base type or a documented interface it
	///     implements, ordered by name. Static extension block members count too: C# finds them through a
	///     derived or implementing type as well.
	/// </summary>
	internal IReadOnlyList<ApiExtensionMember> ExtensionMembers(ApiType type) =>
		_extensions.GetOrAdd(type, BuildExtensionMembers);

	/// <summary>
	///     A member as the inherited and extension lists show it: its name with its parameter types, such as
	///     <c>Parse(string, int)</c>, <c>this[int]</c> or <c>operator +(Money, Money)</c>, after replacing the
	///     declaring type's type parameters with the arguments the page's type passes.
	/// </summary>
	internal static string MemberText(ApiMember member, IReadOnlyDictionary<string, string> typeArguments)
	{
		if (member.Kind is not (ApiMemberKind.Method or ApiMemberKind.Indexer or ApiMemberKind.Operator))
		{
			return member.Name;
		}

		Dictionary<string, string> scope = MethodScope(member, typeArguments, (name, _) => name);
		string parameters = string.Join(", ",
			member.Parameters.Select(p => ApiPageRenderer.ShortTypeName(Substitute(p.Type, scope))));

		if (member.Kind == ApiMemberKind.Indexer)
		{
			return $"this[{parameters}]";
		}

		if (member.Kind == ApiMemberKind.Operator)
		{
			return $"{ApiPageRenderer.OperatorName(member)}({parameters})";
		}

		string typeParameters = member.TypeParameters.Count > 0
			? $"<{string.Join(", ", member.TypeParameters.Select(tp => tp.Name))}>"
			: "";
		return $"{member.Name}{typeParameters}({parameters})";
	}

	#region Inherited members

	private IReadOnlyList<ApiInheritedGroup> BuildInheritedMembers(ApiType type)
	{
		if (type.Kind is not (ApiTypeKind.Class or ApiTypeKind.Struct or ApiTypeKind.Record or ApiTypeKind.Interface)
		    || ApiPageRenderer.IsPython(type))
		{
			return [];
		}

		Lineage lineage = GetLineage(type);
		bool isInterface = type.Kind == ApiTypeKind.Interface;
		List<ApiAncestor> sources = isInterface ? lineage.Interfaces : lineage.BaseTypes;
		var declared = new HashSet<string>(
			type.Members.Select(m => SignatureKey(m, _noTypeArguments)), StringComparer.Ordinal);
		List<HashSet<string>> declaredBySource = sources
			.Select(a => a.Type.Members.Select(m => SignatureKey(m, a.TypeArguments)).ToHashSet(StringComparer.Ordinal))
			.ToList();

		// Nothing derives from a sealed type, so a reader can't reach the protected members it inherits.
		// The reflection host leaves a sealed type's own protected members out for the same reason, which
		// would otherwise list a protected member the type overrides as inherited.
		bool derivable = !type.IsSealed && !type.IsStatic;

		var groups = new List<ApiInheritedGroup>();
		for (int i = 0; i < sources.Count; i++)
		{
			ApiAncestor ancestor = sources[i];
			List<int> hiders = Enumerable.Range(0, sources.Count)
				.Where(j => isInterface ? Extends(sources[j].Type, ancestor.Type) : j < i)
				.ToList();

			List<ApiMember> members = ancestor.Type.Members
				.Where(m => IsInheritable(m, derivable))
				.Where(m => SignatureKey(m, ancestor.TypeArguments) is var key
				            && !declared.Contains(key)
				            && !hiders.Any(j => declaredBySource[j].Contains(key)))
				.OrderBy(m => m.Name, StringComparer.Ordinal)
				.ThenBy(m => m.Parameters.Count)
				.ToList();

			if (members.Count > 0)
			{
				groups.Add(new ApiInheritedGroup(ancestor, members));
			}
		}

		return groups;
	}

	/// <summary>
	///     Whether an interface extends another one, directly or not. Only a member of an interface that
	///     extends the declaring one hides it: in <c>IC : IA, IB</c>, <c>IA.Reset()</c> and <c>IB.Reset()</c>
	///     are both inherited, while a <c>new</c> member hides the one it replaces on every path, as C# looks
	///     interface members up.
	/// </summary>
	private bool Extends(ApiType derived, ApiType baseInterface) =>
		!ReferenceEquals(derived, baseInterface)
		&& GetLineage(derived).Interfaces.Any(a => ReferenceEquals(a.Type, baseInterface));

	private static bool IsInheritable(ApiMember member, bool derivable) =>
		member.Kind is not (ApiMemberKind.Constructor or ApiMemberKind.Operator)
		&& member.Accessibility switch
		{
			ApiAccessibility.Private => false,
			ApiAccessibility.Protected or ApiAccessibility.ProtectedInternal or ApiAccessibility.PrivateProtected => derivable,
			_ => true
		};

	/// <summary>
	///     What a member is matched on to tell whether a derived type declares it again: its name and its
	///     parameter types, without namespaces or nullable annotations. Type parameters of the declaring type
	///     become the arguments the derived type passes, and a generic method's own become their position,
	///     so <c>Map&lt;T&gt;(T)</c> matches an override written as <c>Map&lt;U&gt;(U)</c>.
	/// </summary>
	private static string SignatureKey(ApiMember member, IReadOnlyDictionary<string, string> typeArguments)
	{
		Dictionary<string, string> scope = MethodScope(member, typeArguments, (_, position) => $"``{position}");
		IEnumerable<string> parameters = member.Parameters.Select(p =>
			WhitespaceRegex().Replace(ApiPageRenderer.ShortTypeName(Substitute(p.Type, scope)), "").Replace("?", ""));
		string name = member.Kind == ApiMemberKind.Indexer ? "this[]" : member.Name;
		return $"{name}({string.Join(",", parameters)})";
	}

	/// <summary>
	///     The type argument map inside a member: the declaring type's, except where a generic method's own
	///     type parameter of the same name takes over.
	/// </summary>
	private static Dictionary<string, string> MethodScope(ApiMember member,
		IReadOnlyDictionary<string, string> typeArguments, Func<string, int, string> methodTypeParameter)
	{
		var scope = new Dictionary<string, string>(typeArguments, StringComparer.Ordinal);
		for (int i = 0; i < member.TypeParameters.Count; i++)
		{
			string name = member.TypeParameters[i].Name;
			scope[name] = methodTypeParameter(name, i);
		}

		return scope;
	}

	#endregion

	#region Extension methods

	// The receiver is an extension method's this parameter or an extension block's receiver, and the type
	// parameters it can name are the method's or the block's.
	private void IndexExtension(ApiType declaringType, ApiMember member, string receiverType,
		List<ApiTypeParameter> typeParameters)
	{
		string receiver = receiverType.Trim();
		bool annotated = receiver.EndsWith('?');
		receiver = receiver.TrimEnd('?');
		var extension = new ApiExtensionMember(declaringType, member);

		if (typeParameters.FirstOrDefault(tp => tp.Name == receiver) is { } typeParameter)
		{
			IndexConstrainedExtension(extension, typeParameter, annotated);
			return;
		}

		if (!IsNamedType(receiver) || Find(receiver, declaringType.Namespace) is not { } target)
		{
			return;
		}

		// T? on a struct is Nullable<T>, and a T doesn't convert to that for an extension method call.
		if (annotated && target.Kind is ApiTypeKind.Struct or ApiTypeKind.Enum)
		{
			return;
		}

		if (!_extensionsByReceiver.TryGetValue(target, out List<ApiExtensionMember>? members))
		{
			members = [];
			_extensionsByReceiver[target] = members;
		}

		members.Add(extension);
	}

	/// <summary>
	///     Indexes <c>this T value</c>, or <c>extension&lt;T&gt;(T value)</c>, by the documented types <c>T</c> is
	///     constrained to. Unless every type constraint names a documented type, the member isn't listed: a
	///     page can't tell whether its type satisfies the rest.
	/// </summary>
	private void IndexConstrainedExtension(ApiExtensionMember extension, ApiTypeParameter typeParameter, bool annotated)
	{
		var constraints = new List<ApiType>();
		foreach (string constraint in typeParameter.Constraints.Select(c => c.Trim()))
		{
			if (_specialConstraints.Contains(constraint))
			{
				// T? with a struct constraint is Nullable<T>.
				if (annotated && constraint == "struct")
				{
					return;
				}

				continue;
			}

			if (Find(constraint, extension.DeclaringType.Namespace) is not { } constraintType)
			{
				return;
			}

			constraints.Add(constraintType);
		}

		if (constraints.Count > 0)
		{
			_constrainedExtensions.Add((extension, constraints));
		}
	}

	private IReadOnlyList<ApiExtensionMember> BuildExtensionMembers(ApiType type)
	{
		if (ApiPageRenderer.IsPython(type))
		{
			return [];
		}

		Lineage lineage = GetLineage(type);
		List<ApiType> receivers = [type, ..lineage.BaseTypes.Select(a => a.Type), ..lineage.Interfaces.Select(a => a.Type)];
		var receiverSet = new HashSet<ApiType>(receivers, ReferenceEqualityComparer.Instance);

		var extensions = new List<ApiExtensionMember>();
		foreach (ApiType receiver in receivers)
		{
			if (_extensionsByReceiver.TryGetValue(receiver, out List<ApiExtensionMember>? members))
			{
				extensions.AddRange(members);
			}
		}

		extensions.AddRange(_constrainedExtensions
			.Where(c => c.Constraints.All(receiverSet.Contains))
			.Select(c => c.Extension));

		return extensions
			.OrderBy(e => e.Member.Name, StringComparer.Ordinal)
			.ThenBy(e => e.DeclaringType.Name, StringComparer.Ordinal)
			.ThenBy(e => e.Member.Parameters.Count)
			.ToList();
	}

	/// <summary>
	///     Whether a receiver names a class, struct, interface, enum or delegate, possibly
	///     generic, rather than an array, pointer or tuple. An extension on <c>Shape[]</c> doesn't apply to
	///     <c>Shape</c>.
	/// </summary>
	private static bool IsNamedType(string reference)
	{
		int depth = 0;
		foreach (char c in reference)
		{
			switch (c)
			{
				case '<':
					depth++;
					break;
				case '>':
					depth--;
					break;
				default:
					if (depth == 0 && !(char.IsLetterOrDigit(c) || c is '_' or '.' or '@'))
					{
						return false;
					}

					break;
			}
		}

		return reference.Length > 0 && depth == 0;
	}

	#endregion

	#region Lineage

	private Lineage GetLineage(ApiType type) => _lineages.GetOrAdd(type, BuildLineage);

	/// <summary>
	///     The documented base types nearest first, then the documented interfaces: the type's own, those of
	///     each base type, then the interfaces those extend. The same order <c>InheritDocResolver</c> uses.
	/// </summary>
	private Lineage BuildLineage(ApiType type)
	{
		var visited = new HashSet<ApiType>(ReferenceEqualityComparer.Instance) { type };
		var baseTypes = new List<ApiAncestor>();
		var interfaces = new List<ApiAncestor>();
		var pending = new Queue<(string Reference, ApiType From, IReadOnlyDictionary<string, string> TypeArguments)>();

		foreach (string iface in type.ImplementedInterfaces)
		{
			pending.Enqueue((iface, type, _noTypeArguments));
		}

		ApiType current = type;
		IReadOnlyDictionary<string, string> typeArguments = _noTypeArguments;
		while (current.BaseType is { } reference && Resolve(reference, current, typeArguments) is { } ancestor)
		{
			// The CLI records an interface from another analyzed project as the base type, because the
			// compiler couldn't tell what it was. An interface ends the class chain.
			if (ancestor.Type.Kind == ApiTypeKind.Interface)
			{
				pending.Enqueue((reference, current, typeArguments));
				break;
			}

			if (!visited.Add(ancestor.Type))
			{
				break;
			}

			baseTypes.Add(ancestor);
			foreach (string iface in ancestor.Type.ImplementedInterfaces)
			{
				pending.Enqueue((iface, ancestor.Type, ancestor.TypeArguments));
			}

			current = ancestor.Type;
			typeArguments = ancestor.TypeArguments;
		}

		while (pending.Count > 0)
		{
			(string reference, ApiType from, IReadOnlyDictionary<string, string> arguments) = pending.Dequeue();
			if (Resolve(reference, from, arguments) is not { } ancestor || !visited.Add(ancestor.Type))
			{
				continue;
			}

			interfaces.Add(ancestor);
			foreach (string extended in ancestor.Type.ImplementedInterfaces)
			{
				pending.Enqueue((extended, ancestor.Type, ancestor.TypeArguments));
			}
		}

		return new Lineage(baseTypes, interfaces);
	}

	/// <summary>
	///     Finds the documented type a reference written on <paramref name="from" /> names, and the type
	///     arguments it passes, in terms of the page's type.
	/// </summary>
	private ApiAncestor? Resolve(string reference, ApiType from, IReadOnlyDictionary<string, string> typeArguments)
	{
		string substituted = Substitute(reference.Trim(), typeArguments);
		if (Find(substituted, from.Namespace) is not { } type)
		{
			return null;
		}

		List<string> arguments = TypeArgumentsOf(substituted);
		if (arguments.Count != type.TypeParameters.Count)
		{
			// The reflection host lists a nested type's inherited type parameters too: Outer<T>.Inner has T.
			arguments = SplitLevels(substituted.TrimEnd('?')).SelectMany(TypeArgumentsOf).ToList();
		}

		IReadOnlyDictionary<string, string> map = arguments.Count > 0 && arguments.Count == type.TypeParameters.Count
			? type.TypeParameters.Zip(arguments).ToDictionary(p => p.First.Name, p => p.Second, StringComparer.Ordinal)
			: _noTypeArguments;

		return new ApiAncestor(type, substituted, map);
	}

	/// <summary>
	///     Whether a base type or interface reference written on <paramref name="from" /> names
	///     <paramref name="target" />, by the same rules as the inherited members.
	/// </summary>
	internal bool Names(string reference, ApiType from, ApiType target) =>
		Find(reference, from.Namespace) is { } found && ReferenceEquals(found, target);

	#endregion

	#region Type lookup

	/// <summary>
	///     Finds the documented type a reference names. See the class remarks for the matching rules.
	/// </summary>
	private ApiType? Find(string reference, string? contextNamespace)
	{
		string key = TypeKey(reference.Trim().TrimEnd('?'));
		if (key.StartsWith("global::", StringComparison.Ordinal))
		{
			key = key["global::".Length..];
		}

		string name = key[(key.LastIndexOf('.') + 1)..];
		if (name.Length == 0 || !_typesByName.TryGetValue(name, out List<(string Key, ApiType Type)>? sameName))
		{
			return null;
		}

		List<(string Key, ApiType Type)> matches = sameName
			.Where(t => t.Key == key || t.Key.EndsWith("." + key, StringComparison.Ordinal))
			.ToList();

		if (matches.Count <= 1)
		{
			return matches.Count == 1 ? matches[0].Type : null;
		}

		int exact = matches.FindIndex(t => t.Key == key);
		if (exact >= 0)
		{
			return matches[exact].Type;
		}

		string? ns = contextNamespace;
		while (true)
		{
			string scope = ns ?? "";
			List<(string Key, ApiType Type)> inScope = matches
				.Where(t => string.Equals(t.Type.Namespace ?? "", scope, StringComparison.Ordinal))
				.ToList();
			if (inScope.Count > 0)
			{
				return inScope.Count == 1 ? inScope[0].Type : null;
			}

			if (string.IsNullOrEmpty(ns))
			{
				return null;
			}

			int dot = ns.LastIndexOf('.');
			ns = dot >= 0 ? ns[..dot] : null;
		}
	}

	/// <summary>
	///     A type name with each type argument list replaced by its arity, the way documentation IDs write
	///     generics: <c>Ns.Base&lt;Ns.User&gt;</c> and <c>Ns.Base&lt;T&gt;</c> both become <c>Ns.Base`1</c>.
	///     Matches the keys <c>InheritDocResolver</c> uses.
	/// </summary>
	private static string TypeKey(string name)
	{
		var key = new StringBuilder(name.Length);
		int angleDepth = 0;
		int otherDepth = 0;
		int arity = 0;

		foreach (char c in name)
		{
			switch (c)
			{
				case '<':
					if (++angleDepth == 1 && otherDepth == 0)
					{
						arity = 1;
					}

					break;
				case '>':
					if (--angleDepth == 0 && otherDepth == 0)
					{
						key.Append('`').Append(arity);
					}

					break;
				// Tuple and array type arguments carry commas that do not separate arguments.
				case '(' or '[':
					otherDepth++;
					break;
				case ')' or ']':
					otherDepth--;
					break;
				case ',' when angleDepth == 1 && otherDepth == 0:
					arity++;
					break;
				case ' ':
					break;
				default:
					if (angleDepth == 0 && otherDepth == 0)
					{
						key.Append(c);
					}

					break;
			}
		}

		return key.ToString();
	}

	/// <summary>
	///     The type arguments of a reference that ends in an argument list: <c>Base&lt;int, List&lt;T&gt;&gt;</c>
	///     gives <c>int</c> and <c>List&lt;T&gt;</c>.
	/// </summary>
	private static List<string> TypeArgumentsOf(string reference)
	{
		string text = reference.TrimEnd('?');
		if (!text.EndsWith('>'))
		{
			return [];
		}

		int depth = 0;
		int open = -1;
		for (int i = text.Length - 1; i >= 0; i--)
		{
			if (text[i] == '>')
			{
				depth++;
			}
			else if (text[i] == '<' && --depth == 0)
			{
				open = i;
				break;
			}
		}

		if (open < 0)
		{
			return [];
		}

		var arguments = new List<string>();
		int start = open + 1;
		depth = 0;
		for (int i = start; i < text.Length - 1; i++)
		{
			switch (text[i])
			{
				case '<' or '(' or '[':
					depth++;
					break;
				case '>' or ')' or ']':
					depth--;
					break;
				case ',' when depth == 0:
					arguments.Add(text[start..i].Trim());
					start = i + 1;
					break;
			}
		}

		arguments.Add(text[start..^1].Trim());
		return arguments;
	}

	/// <summary>
	///     The dot-separated parts of a name, leaving the dots inside type argument lists alone:
	///     <c>Ns.Outer&lt;Ns.User&gt;.Inner</c> gives <c>Ns</c>, <c>Outer&lt;Ns.User&gt;</c> and <c>Inner</c>.
	/// </summary>
	private static List<string> SplitLevels(string reference)
	{
		var levels = new List<string>();
		int depth = 0;
		int start = 0;
		for (int i = 0; i < reference.Length; i++)
		{
			switch (reference[i])
			{
				case '<' or '(' or '[':
					depth++;
					break;
				case '>' or ')' or ']':
					depth--;
					break;
				case '.' when depth == 0:
					levels.Add(reference[start..i]);
					start = i + 1;
					break;
			}
		}

		levels.Add(reference[start..]);
		return levels;
	}

	/// <summary>
	///     Replaces type parameter names with what they stand for. Names next to a dot are part of a
	///     qualified name, not type parameters.
	/// </summary>
	private static string Substitute(string text, IReadOnlyDictionary<string, string> typeArguments) =>
		typeArguments.Count == 0
			? text
			: TypeParameterNameRegex().Replace(text,
				m => typeArguments.TryGetValue(m.Value, out string? argument) ? argument : m.Value);

	#endregion

	[GeneratedRegex(@"(?<![\w.])[A-Za-z_][A-Za-z0-9_]*(?![\w.])")]
	private static partial Regex TypeParameterNameRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	private sealed record Lineage(List<ApiAncestor> BaseTypes, List<ApiAncestor> Interfaces);
}

/// <summary>
///     A documented type that another type inherits from or implements.
/// </summary>
/// <param name="Type">The documented type.</param>
/// <param name="Reference">
///     The reference as written on the deriving type, with type parameters replaced by what the page's type
///     passes, such as <c>Base&lt;int&gt;</c>.
/// </param>
/// <param name="TypeArguments">The page type's arguments for each of <paramref name="Type" />'s type parameters.</param>
internal sealed record ApiAncestor(ApiType Type, string Reference, IReadOnlyDictionary<string, string> TypeArguments);

/// <summary>
///     The members a type inherits from one documented ancestor.
/// </summary>
/// <param name="From">The ancestor that declares the members.</param>
/// <param name="Members">The members, ordered by name.</param>
internal sealed record ApiInheritedGroup(ApiAncestor From, List<ApiMember> Members);

/// <summary>
///     An extension method or extension block member, and the static class that declares it.
/// </summary>
/// <param name="DeclaringType">The static class.</param>
/// <param name="Member">The extension method or extension block member.</param>
internal sealed record ApiExtensionMember(ApiType DeclaringType, ApiMember Member);
