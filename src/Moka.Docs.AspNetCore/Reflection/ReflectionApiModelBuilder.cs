using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.AspNetCore.Reflection;

/// <summary>
///     Converts runtime .NET assemblies into the <see cref="ApiReference" /> model
///     using <see cref="System.Reflection" /> instead of Roslyn source analysis.
///     Produces the same model shape as <c>AssemblyAnalyzer</c>.
/// </summary>
/// <remarks>
///     A type's public members are documented, and so are its protected and protected internal
///     members when the type can be derived from. Signatures read like C# declarations, with
///     accessibility, modifiers and nullable annotations.
/// </remarks>
public sealed class ReflectionApiModelBuilder(
	XmlDocParser xmlDocParser,
	InheritDocResolver inheritDocResolver,
	ILogger<ReflectionApiModelBuilder> logger)
{
	private const BindingFlags _declaredMembers = BindingFlags.Public | BindingFlags.NonPublic |
	                                              BindingFlags.Instance | BindingFlags.Static |
	                                              BindingFlags.DeclaredOnly;

	private static readonly Dictionary<string, string> _clrToCSharpTypeNames = new(StringComparer.Ordinal)
	{
		["System.Boolean"] = "bool",
		["System.Byte"] = "byte",
		["System.SByte"] = "sbyte",
		["System.Char"] = "char",
		["System.Decimal"] = "decimal",
		["System.Double"] = "double",
		["System.Single"] = "float",
		["System.Int32"] = "int",
		["System.UInt32"] = "uint",
		["System.Int64"] = "long",
		["System.UInt64"] = "ulong",
		["System.Int16"] = "short",
		["System.UInt16"] = "ushort",
		["System.String"] = "string",
		["System.Object"] = "object",
		["System.Void"] = "void",
		["System.IntPtr"] = "nint",
		["System.UIntPtr"] = "nuint"
	};

	/// <summary>C# tokens for the metadata names of operators, by the name the compiler emits.</summary>
	private static readonly Dictionary<string, string> _operatorTokens = new(StringComparer.Ordinal)
	{
		["op_UnaryPlus"] = "+",
		["op_UnaryNegation"] = "-",
		["op_LogicalNot"] = "!",
		["op_OnesComplement"] = "~",
		["op_Increment"] = "++",
		["op_Decrement"] = "--",
		["op_True"] = "true",
		["op_False"] = "false",
		["op_Addition"] = "+",
		["op_Subtraction"] = "-",
		["op_Multiply"] = "*",
		["op_Division"] = "/",
		["op_Modulus"] = "%",
		["op_BitwiseAnd"] = "&",
		["op_BitwiseOr"] = "|",
		["op_ExclusiveOr"] = "^",
		["op_LeftShift"] = "<<",
		["op_RightShift"] = ">>",
		["op_UnsignedRightShift"] = ">>>",
		["op_Equality"] = "==",
		["op_Inequality"] = "!=",
		["op_LessThan"] = "<",
		["op_GreaterThan"] = ">",
		["op_LessThanOrEqual"] = "<=",
		["op_GreaterThanOrEqual"] = ">=",
		["op_CheckedUnaryNegation"] = "checked -",
		["op_CheckedIncrement"] = "checked ++",
		["op_CheckedDecrement"] = "checked --",
		["op_CheckedAddition"] = "checked +",
		["op_CheckedSubtraction"] = "checked -",
		["op_CheckedMultiply"] = "checked *",
		["op_CheckedDivision"] = "checked /",
		["op_AdditionAssignment"] = "+=",
		["op_SubtractionAssignment"] = "-=",
		["op_MultiplicationAssignment"] = "*=",
		["op_DivisionAssignment"] = "/=",
		["op_ModulusAssignment"] = "%=",
		["op_BitwiseAndAssignment"] = "&=",
		["op_BitwiseOrAssignment"] = "|=",
		["op_ExclusiveOrAssignment"] = "^=",
		["op_LeftShiftAssignment"] = "<<=",
		["op_RightShiftAssignment"] = ">>=",
		["op_UnsignedRightShiftAssignment"] = ">>>=",
		["op_IncrementAssignment"] = "++",
		["op_DecrementAssignment"] = "--",
		["op_CheckedAdditionAssignment"] = "checked +=",
		["op_CheckedSubtractionAssignment"] = "checked -=",
		["op_CheckedMultiplicationAssignment"] = "checked *=",
		["op_CheckedDivisionAssignment"] = "checked /=",
		["op_CheckedIncrementAssignment"] = "checked ++",
		["op_CheckedDecrementAssignment"] = "checked --"
	};

	/// <summary>
	///     Builds an <see cref="ApiReference" /> from the given assemblies.
	/// </summary>
	/// <param name="assemblies">The assemblies to scan.</param>
	/// <param name="includeXmlDocs">Whether to load and attach XML documentation.</param>
	/// <returns>A fully populated API reference model.</returns>
	public ApiReference Build(IReadOnlyList<Assembly> assemblies, bool includeXmlDocs)
	{
		var xmlDocs = new Dictionary<string, XmlDocFile>(StringComparer.Ordinal);

		if (includeXmlDocs)
		{
			foreach (Assembly assembly in assemblies)
			{
				string? xmlPath = GetXmlDocPath(assembly);
				if (xmlPath is not null)
				{
					XmlDocFile docFile = xmlDocParser.Parse(xmlPath);
					if (docFile.Members.Count > 0)
					{
						xmlDocs[assembly.GetName().Name ?? ""] = docFile;
					}
				}
			}
		}

		var allTypes = new List<(Type Type, string AssemblyName)>();

		foreach (Assembly assembly in assemblies)
		{
			string assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "";
			foreach (Type type in GetDocumentedTypes(assembly, assemblyName))
			{
				if (!ShouldSkipType(type))
				{
					allTypes.Add((type, assemblyName));
				}
			}
		}

		IOrderedEnumerable<IGrouping<string, (Type Type, string AssemblyName)>> namespaceGroups = allTypes
			.GroupBy(t => t.Type.Namespace ?? "(global)")
			.OrderBy(g => g.Key, StringComparer.Ordinal);

		var namespaces = new List<ApiNamespace>();

		foreach (IGrouping<string, (Type Type, string AssemblyName)> nsGroup in namespaceGroups)
		{
			var types = nsGroup
				.OrderBy(t => t.Type.Name, StringComparer.Ordinal)
				.Select(t => BuildApiType(t.Type, t.AssemblyName, xmlDocs))
				.ToList();

			namespaces.Add(new ApiNamespace
			{
				Name = nsGroup.Key,
				Types = types
			});
		}

		var assemblyNames = assemblies
			.Select(a => a.GetName().Name ?? a.FullName ?? "")
			.Where(n => !string.IsNullOrEmpty(n))
			.ToList();

		var reference = new ApiReference
		{
			Namespaces = namespaces,
			Assemblies = assemblyNames
		};

		reference = inheritDocResolver.Resolve(reference);

		logger.LogInformation(
			"Built API reference from {AssemblyCount} assemblies: {TypeCount} types in {NamespaceCount} namespaces",
			assemblies.Count,
			namespaces.Sum(ns => ns.Types.Count),
			namespaces.Count);

		return reference;
	}

	/// <summary>
	///     The exported types, plus the protected nested types of those a consumer can derive from,
	///     and everything visible inside those.
	/// </summary>
	private List<Type> GetDocumentedTypes(Assembly assembly, string assemblyName)
	{
		Type[] exported;
		try
		{
			exported = assembly.GetExportedTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			logger.LogWarning(ex, "Could not load all types from assembly {Assembly}", assemblyName);
			exported = ex.Types.OfType<Type>().ToArray();
		}

		var types = new List<Type>(exported);
		foreach (Type type in exported)
		{
			// Exported types stop at public nesting. A protected nested type is part of the API a
			// derived class sees, like any other protected member.
			foreach (Type nested in type.GetNestedTypes(BindingFlags.NonPublic))
			{
				if (!type.IsSealed && (nested.IsNestedFamily || nested.IsNestedFamORAssem))
				{
					types.Add(nested);
					AddVisibleNestedTypes(nested, types);
				}
			}
		}

		return types;
	}

	private static void AddVisibleNestedTypes(Type type, List<Type> types)
	{
		foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (nested.IsNestedPublic || (!type.IsSealed && (nested.IsNestedFamily || nested.IsNestedFamORAssem)))
			{
				types.Add(nested);
				AddVisibleNestedTypes(nested, types);
			}
		}
	}

	private static bool ShouldSkipType(Type type)
	{
		if (type.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
		{
			return true;
		}

		if (type.Name.Contains('<'))
		{
			return true;
		}

		return false;
	}

	private static string? GetXmlDocPath(Assembly assembly)
	{
		string location = assembly.Location;
		if (string.IsNullOrEmpty(location))
		{
			return null;
		}

		string xmlPath = Path.ChangeExtension(location, ".xml");
		return File.Exists(xmlPath) ? xmlPath : null;
	}

	private ApiType BuildApiType(
		Type type,
		string assemblyName,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		ApiTypeKind kind = ResolveTypeKind(type);
		bool isStatic = type.IsAbstract && type.IsSealed;
		bool isAbstract = type.IsAbstract && !type.IsSealed;
		bool isRecord = IsRecordType(type);
		// Structs, enums and delegates are sealed in metadata, but only a class can be declared
		// sealed; matching the source analyzer keeps a Sealed badge off struct pages.
		bool isSealed = type.IsSealed && !type.IsAbstract && type.IsClass && !typeof(Delegate).IsAssignableFrom(type);

		string? baseType = GetBaseTypeName(type);
		List<string> interfaces = GetDirectInterfaces(type);
		List<ApiTypeParameter> typeParameters = GetTypeParameters(type);

		ObsoleteAttribute? obsoleteAttr = type.GetCustomAttribute<ObsoleteAttribute>();
		List<ApiAttribute> attributes = GetAttributes(type.GetCustomAttributesData());

		string typeName = GetSimpleTypeName(type);
		string fullName = string.Join(".", DeclaringChain(type).Select(GetSimpleTypeName));
		if (type.Namespace is not null)
		{
			fullName = $"{type.Namespace}.{fullName}";
		}

		XmlDocBlock? documentation = LookupTypeDoc(type, xmlDocs);

		List<ApiMember> members = BuildMembers(type, kind, documentation, xmlDocs);

		return new ApiType
		{
			Name = typeName,
			FullName = fullName,
			Kind = isRecord ? ApiTypeKind.Record : kind,
			Accessibility = GetTypeAccessibility(type),
			IsStatic = isStatic,
			IsAbstract = isAbstract,
			IsSealed = isSealed,
			IsRecord = isRecord,
			TypeParameters = typeParameters,
			BaseType = baseType,
			ImplementedInterfaces = interfaces,
			Members = members,
			Documentation = documentation,
			Attributes = attributes,
			Namespace = type.Namespace,
			Assembly = assemblyName,
			IsObsolete = obsoleteAttr is not null,
			ObsoleteMessage = obsoleteAttr?.Message
		};
	}

	private static ApiAccessibility GetTypeAccessibility(Type type) =>
		type.IsNestedFamily ? ApiAccessibility.Protected
		: type.IsNestedFamORAssem ? ApiAccessibility.ProtectedInternal
		: ApiAccessibility.Public;

	private static ApiTypeKind ResolveTypeKind(Type type)
	{
		if (type.IsInterface)
		{
			return ApiTypeKind.Interface;
		}

		if (type.IsEnum)
		{
			return ApiTypeKind.Enum;
		}

		if (type.IsValueType)
		{
			return ApiTypeKind.Struct;
		}

		if (typeof(Delegate).IsAssignableFrom(type) && type != typeof(Delegate) && type != typeof(MulticastDelegate))
		{
			return ApiTypeKind.Delegate;
		}

		return ApiTypeKind.Class;
	}

	private static bool IsRecordType(Type type)
	{
		// Records have a compiler-generated <Clone>$ method and EqualityContract property
		bool hasCloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
		bool hasEqualityContract = type.GetProperty("EqualityContract",
			BindingFlags.NonPublic | BindingFlags.Instance) is not null;

		return hasCloneMethod && hasEqualityContract;
	}

	private static string? GetBaseTypeName(Type type)
	{
		Type? baseType = type.BaseType;
		if (baseType is null)
		{
			return null;
		}

		string fullName = baseType.FullName ?? baseType.Name;

		// Skip fundamental base types
		if (fullName is "System.Object" or "System.ValueType" or "System.Enum"
		    or "System.Delegate" or "System.MulticastDelegate")
		{
			return null;
		}

		return FormatTypeName(baseType);
	}

	private static List<string> GetDirectInterfaces(Type type)
	{
		Type[] allInterfaces = type.GetInterfaces();
		Type[] baseInterfaces = type.BaseType?.GetInterfaces() ?? [];

		// Also exclude interfaces inherited from other interfaces
		var inherited = new HashSet<Type>(baseInterfaces);
		foreach (Type iface in allInterfaces)
		foreach (Type parentIface in iface.GetInterfaces())
		{
			inherited.Add(parentIface);
		}

		return allInterfaces
			.Where(i => !inherited.Contains(i))
			.Select(i => FormatTypeName(i))
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToList();
	}

	private static List<ApiTypeParameter> GetTypeParameters(Type type)
	{
		if (!type.IsGenericTypeDefinition)
		{
			return [];
		}

		return type.GetGenericArguments()
			.Select(BuildTypeParameter)
			.ToList();
	}

	private static ApiTypeParameter BuildTypeParameter(Type typeParam)
	{
		var constraints = new List<string>();
		GenericParameterAttributes gpa = typeParam.GenericParameterAttributes;

		if (gpa.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
		{
			constraints.Add("struct");
		}
		else if (gpa.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
		{
			constraints.Add("class");
		}

		foreach (Type constraintType in typeParam.GetGenericParameterConstraints())
		{
			if (constraintType == typeof(ValueType))
			{
				continue; // already handled by struct constraint
			}

			constraints.Add(FormatTypeName(constraintType));
		}

		if (gpa.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
		    && !gpa.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
		{
			constraints.Add("new()");
		}

		return new ApiTypeParameter
		{
			Name = typeParam.Name,
			Constraints = constraints
		};
	}

	private List<ApiMember> BuildMembers(
		Type type,
		ApiTypeKind kind,
		XmlDocBlock? typeDocumentation,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		var members = new List<ApiMember>();

		if (kind == ApiTypeKind.Enum)
		{
			BuildEnumFields(type, members, xmlDocs);
			return members;
		}

		if (kind == ApiTypeKind.Delegate)
		{
			BuildDelegateInvoke(type, members, typeDocumentation);
			return members;
		}

		// Protected members are reachable only by deriving, so a sealed type (a struct or a
		// static class included) has none worth listing.
		bool includeProtected = !type.IsSealed;

		BuildConstructors(type, members, xmlDocs, includeProtected);
		BuildProperties(type, members, xmlDocs, includeProtected);
		BuildMethods(type, members, xmlDocs, includeProtected);
		BuildFields(type, members, xmlDocs, includeProtected);
		BuildEvents(type, members, xmlDocs, includeProtected);

		return members;
	}

	private static void BuildConstructors(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (ConstructorInfo ctor in ctors)
		{
			if (IsCompilerGenerated(ctor))
			{
				continue;
			}

			ApiAccessibility accessibility = GetAccessibility(ctor);
			if (!IsDocumented(accessibility, includeProtected))
			{
				continue;
			}

			string name = GetSimpleTypeName(type);
			string signature = $"{AccessPrefix(type, accessibility)}{name}({FormatParameterList(ctor.GetParameters(), false)})";
			string memberId = GetConstructorMemberId(type, ctor);

			members.Add(new ApiMember
			{
				Name = name,
				Kind = ApiMemberKind.Constructor,
				Signature = signature,
				Parameters = BuildParameters(ctor.GetParameters()),
				Documentation = LookupMemberDoc(type, memberId, xmlDocs),
				Accessibility = accessibility
			});
		}
	}

	private static void BuildProperties(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		foreach (PropertyInfo prop in type.GetProperties(_declaredMembers))
		{
			MethodInfo? getter = prop.GetGetMethod(true);
			MethodInfo? setter = prop.GetSetMethod(true);
			MethodInfo? accessor = getter ?? setter;

			// A record's EqualityContract is compiler-generated and protected.
			if (accessor is null || IsCompilerGenerated(prop))
			{
				continue;
			}

			// The property is as accessible as its most accessible accessor.
			ApiAccessibility accessibility = getter is not null && setter is not null
				? AccessibilityRank(GetAccessibility(getter)) >= AccessibilityRank(GetAccessibility(setter))
					? GetAccessibility(getter)
					: GetAccessibility(setter)
				: GetAccessibility(accessor);

			if (!IsDocumented(accessibility, includeProtected))
			{
				continue;
			}

			ParameterInfo[] indexParams = prop.GetIndexParameters();
			bool isIndexer = indexParams.Length > 0;

			string memberId = isIndexer
				? $"P:{GetDocIdName(type)}.{prop.Name}({string.Join(",", indexParams.Select(p => GetMemberIdTypeName(p.ParameterType)))})"
				: $"P:{GetDocIdName(type)}.{prop.Name}";

			string returnType = FormatTypeName(prop.PropertyType, NullableAnnotations.For(prop.GetCustomAttributesData(), type));

			var signature = new StringBuilder();
			signature.Append(AccessPrefix(type, accessibility));
			signature.Append(MethodModifiers(accessor));
			if (HasAttribute(prop.GetCustomAttributesData(), "System.Runtime.CompilerServices.RequiredMemberAttribute"))
			{
				signature.Append("required ");
			}

			signature.Append(returnType);
			signature.Append(' ');
			signature.Append(isIndexer ? $"this[{FormatParameterList(indexParams, false)}]" : prop.Name);
			signature.Append(' ');
			signature.Append(BuildAccessorList(getter, setter, accessibility));

			bool isVirtual = accessor.IsVirtual && !accessor.IsFinal;
			bool isOverride = IsOverride(accessor);
			ObsoleteAttribute? obsoleteAttr = prop.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = isIndexer ? "this[]" : prop.Name,
				Kind = isIndexer ? ApiMemberKind.Indexer : ApiMemberKind.Property,
				Signature = signature.ToString(),
				ReturnType = returnType,
				Parameters = isIndexer ? BuildParameters(indexParams) : [],
				Accessibility = accessibility,
				IsStatic = accessor.IsStatic,
				IsVirtual = isVirtual && !accessor.IsAbstract && !isOverride,
				IsAbstract = accessor.IsAbstract,
				IsOverride = isOverride,
				IsSealed = accessor.IsFinal && isOverride,
				Documentation = LookupMemberDoc(type, memberId, xmlDocs),
				Attributes = GetAttributes(prop.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private static void BuildMethods(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		foreach (MethodInfo method in type.GetMethods(_declaredMembers))
		{
			// Record members (<Clone>$, PrintMembers, Equals...) carry [CompilerGenerated].
			if (IsCompilerGenerated(method) || method.Name.StartsWith('<'))
			{
				continue;
			}

			// Property and event accessors have special names; so do operators, which used to be
			// skipped along with the accessors.
			bool isOperator = method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal);
			if (method.IsSpecialName && !isOperator)
			{
				continue;
			}

			ApiAccessibility accessibility = GetAccessibility(method);
			if (!IsDocumented(accessibility, includeProtected))
			{
				continue;
			}

			string returnType = FormatTypeName(method.ReturnType, ReturnAnnotations(method));
			string memberId = GetMethodMemberId(type, method);

			bool isOverride = IsOverride(method);
			bool isVirtual = method.IsVirtual && !method.IsFinal;
			bool isExtensionMethod = method.IsStatic && method.IsDefined(typeof(ExtensionAttribute), false);
			ObsoleteAttribute? obsoleteAttr = method.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = method.Name,
				Kind = isOperator ? ApiMemberKind.Operator : ApiMemberKind.Method,
				Signature = BuildMethodSignature(type, method, accessibility, isOperator, isExtensionMethod),
				ReturnType = returnType,
				Accessibility = accessibility,
				IsStatic = method.IsStatic,
				IsVirtual = isVirtual && !method.IsAbstract && !isOverride,
				IsAbstract = method.IsAbstract,
				IsOverride = isOverride,
				IsSealed = method.IsFinal && isOverride,
				IsExtensionMethod = isExtensionMethod,
				Parameters = BuildParameters(method.GetParameters()),
				TypeParameters = GetMethodTypeParameters(method),
				Documentation = LookupMemberDoc(type, memberId, xmlDocs),
				Attributes = GetAttributes(method.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private static void BuildFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		foreach (FieldInfo field in type.GetFields(_declaredMembers))
		{
			if (IsCompilerGenerated(field))
			{
				continue;
			}

			// Skip backing fields
			if (field.Name.Contains('<') || field.Name.EndsWith("k__BackingField"))
			{
				continue;
			}

			ApiAccessibility accessibility = GetAccessibility(field);
			if (!IsDocumented(accessibility, includeProtected))
			{
				continue;
			}

			string returnType = FormatTypeName(field.FieldType, NullableAnnotations.For(field.GetCustomAttributesData(), type));
			string memberId = $"F:{GetDocIdName(type)}.{field.Name}";
			ObsoleteAttribute? obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = field.Name,
				Kind = ApiMemberKind.Field,
				Signature = BuildFieldSignature(type, field, accessibility, returnType),
				ReturnType = returnType,
				Accessibility = accessibility,
				IsStatic = field.IsStatic,
				Documentation = LookupMemberDoc(type, memberId, xmlDocs),
				Attributes = GetAttributes(field.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private static void BuildEnumFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

		foreach (FieldInfo field in fields)
		{
			string memberId = $"F:{GetDocIdName(type)}.{field.Name}";
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);
			ObsoleteAttribute? obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

			object? rawValue = field.GetRawConstantValue();
			string signature = rawValue is not null
				? $"{field.Name} = {Convert.ToString(rawValue, CultureInfo.InvariantCulture)}"
				: field.Name;

			members.Add(new ApiMember
			{
				Name = field.Name,
				Kind = ApiMemberKind.Field,
				Signature = signature,
				ReturnType = type.Name,
				Accessibility = ApiAccessibility.Public,
				IsStatic = true,
				Documentation = documentation,
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private static void BuildEvents(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		foreach (EventInfo evt in type.GetEvents(_declaredMembers))
		{
			MethodInfo? addMethod = evt.GetAddMethod(true);
			if (addMethod is null)
			{
				continue;
			}

			ApiAccessibility accessibility = GetAccessibility(addMethod);
			if (!IsDocumented(accessibility, includeProtected))
			{
				continue;
			}

			string handlerType = evt.EventHandlerType is not null
				? FormatTypeName(evt.EventHandlerType, NullableAnnotations.For(evt.GetCustomAttributesData(), type))
				: "EventHandler";

			string signature =
				$"{AccessPrefix(type, accessibility)}{MethodModifiers(addMethod)}event {handlerType} {evt.Name}";
			string memberId = $"E:{GetDocIdName(type)}.{evt.Name}";
			ObsoleteAttribute? obsoleteAttr = evt.GetCustomAttribute<ObsoleteAttribute>();
			bool isOverride = IsOverride(addMethod);

			members.Add(new ApiMember
			{
				Name = evt.Name,
				Kind = ApiMemberKind.Event,
				Signature = signature,
				ReturnType = handlerType,
				Accessibility = accessibility,
				IsStatic = addMethod.IsStatic,
				IsVirtual = addMethod.IsVirtual && !addMethod.IsFinal && !addMethod.IsAbstract && !isOverride,
				IsAbstract = addMethod.IsAbstract,
				IsOverride = isOverride,
				Documentation = LookupMemberDoc(type, memberId, xmlDocs),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	/// <summary>
	///     A delegate's one meaningful member: its <c>Invoke</c> method, which carries the
	///     parameters and return type. Delegates used to get no members, so their parameters
	///     appeared nowhere.
	/// </summary>
	private static void BuildDelegateInvoke(Type type, List<ApiMember> members, XmlDocBlock? typeDocumentation)
	{
		MethodInfo? invoke = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		if (invoke is null)
		{
			return;
		}

		ApiAccessibility accessibility = GetTypeAccessibility(type);

		var signature = new StringBuilder();
		signature.Append(Keyword(accessibility));
		signature.Append(" delegate ");
		signature.Append(FormatReturn(invoke));
		signature.Append(' ');
		signature.Append(GetSimpleTypeName(type));
		if (type.IsGenericTypeDefinition)
		{
			signature.Append('<');
			signature.Append(string.Join(", ", type.GetGenericArguments().Select(FormatTypeParameterDeclaration)));
			signature.Append('>');
		}

		signature.Append('(');
		signature.Append(FormatParameterList(invoke.GetParameters(), false));
		signature.Append(')');

		members.Add(new ApiMember
		{
			Name = "Invoke",
			Kind = ApiMemberKind.Method,
			Signature = signature.ToString(),
			ReturnType = FormatTypeName(invoke.ReturnType, ReturnAnnotations(invoke)),
			Parameters = BuildParameters(invoke.GetParameters()),
			Accessibility = accessibility,
			// A delegate's doc comment sits on its declaration and describes Invoke's parameters;
			// there is nowhere to write a separate one. AssemblyAnalyzer shares it the same way.
			Documentation = typeDocumentation
		});
	}

	#region Method Type Parameters

	private static List<ApiTypeParameter> GetMethodTypeParameters(MethodInfo method)
	{
		if (!method.IsGenericMethodDefinition)
		{
			return [];
		}

		return method.GetGenericArguments()
			.Select(BuildTypeParameter)
			.ToList();
	}

	#endregion

	#region Accessibility and Modifiers

	private static ApiAccessibility GetAccessibility(MethodBase method) =>
		method.IsPublic ? ApiAccessibility.Public
		: method.IsFamilyOrAssembly ? ApiAccessibility.ProtectedInternal
		: method.IsFamily ? ApiAccessibility.Protected
		: method.IsAssembly ? ApiAccessibility.Internal
		: method.IsFamilyAndAssembly ? ApiAccessibility.PrivateProtected
		: ApiAccessibility.Private;

	private static ApiAccessibility GetAccessibility(FieldInfo field) =>
		field.IsPublic ? ApiAccessibility.Public
		: field.IsFamilyOrAssembly ? ApiAccessibility.ProtectedInternal
		: field.IsFamily ? ApiAccessibility.Protected
		: field.IsAssembly ? ApiAccessibility.Internal
		: field.IsFamilyAndAssembly ? ApiAccessibility.PrivateProtected
		: ApiAccessibility.Private;

	/// <summary>
	///     Public members, and protected or protected internal ones when a consumer can derive
	///     from the type. Only public members used to be read, so a base class's protected
	///     constructors and virtual methods were missing from its page.
	/// </summary>
	private static bool IsDocumented(ApiAccessibility accessibility, bool includeProtected) =>
		accessibility == ApiAccessibility.Public
		|| (includeProtected && accessibility is ApiAccessibility.Protected or ApiAccessibility.ProtectedInternal);

	private static int AccessibilityRank(ApiAccessibility accessibility) => accessibility switch
	{
		ApiAccessibility.Public => 5,
		ApiAccessibility.ProtectedInternal => 4,
		ApiAccessibility.Protected => 3,
		ApiAccessibility.Internal => 2,
		ApiAccessibility.PrivateProtected => 1,
		_ => 0
	};

	private static string Keyword(ApiAccessibility accessibility) => accessibility switch
	{
		ApiAccessibility.Public => "public",
		ApiAccessibility.ProtectedInternal => "protected internal",
		ApiAccessibility.Protected => "protected",
		ApiAccessibility.Internal => "internal",
		ApiAccessibility.PrivateProtected => "private protected",
		_ => "private"
	};

	/// <summary>
	///     The accessibility keyword and a space. Interface members are public unless they say
	///     otherwise, and their declarations leave it out.
	/// </summary>
	private static string AccessPrefix(Type declaringType, ApiAccessibility accessibility) =>
		declaringType.IsInterface && accessibility == ApiAccessibility.Public ? "" : Keyword(accessibility) + " ";

	/// <summary>
	///     <c>static</c>, <c>virtual</c>, <c>abstract</c>, <c>sealed</c> and <c>override</c>, each
	///     followed by a space, in the order C# code style writes them.
	/// </summary>
	private static string MethodModifiers(MethodInfo method)
	{
		var sb = new StringBuilder();
		if (method.IsStatic)
		{
			sb.Append("static ");
		}

		if (method.DeclaringType?.IsInterface == true)
		{
			// An instance interface member is abstract or virtual without saying so. A static one
			// has to be declared abstract or virtual to be either.
			if (method.IsStatic && method.IsAbstract)
			{
				sb.Append("abstract ");
			}
			else if (method.IsStatic && method.IsVirtual)
			{
				sb.Append("virtual ");
			}

			return sb.ToString();
		}

		bool isOverride = IsOverride(method);
		if (method.IsVirtual && !method.IsFinal && !method.IsAbstract && !isOverride)
		{
			sb.Append("virtual ");
		}

		if (method.IsAbstract)
		{
			sb.Append("abstract ");
		}

		if (method.IsFinal && isOverride)
		{
			sb.Append("sealed ");
		}

		if (isOverride)
		{
			sb.Append("override ");
		}

		return sb.ToString();
	}

	/// <summary>
	///     Whether the method overrides a base class method. A method that implements an interface
	///     is virtual in metadata too, but its base definition is itself.
	/// </summary>
	private static bool IsOverride(MethodInfo method) =>
		method.IsVirtual
		&& method.DeclaringType is { IsInterface: false }
		&& method.GetBaseDefinition().DeclaringType != method.DeclaringType;

	private static bool IsCompilerGenerated(MemberInfo member) =>
		member.IsDefined(typeof(CompilerGeneratedAttribute), false);

	private static bool HasAttribute(IEnumerable<CustomAttributeData> attributes, string fullName) =>
		attributes.Any(a => a.AttributeType.FullName == fullName);

	#endregion

	#region Attribute Building

	private static List<ApiAttribute> GetAttributes(IList<CustomAttributeData> attributeDataList)
	{
		var result = new List<ApiAttribute>();

		foreach (CustomAttributeData attr in attributeDataList)
		{
			string attrName = attr.AttributeType.Name;

			// Skip compiler-internal attributes
			if (attrName is "CompilerGeneratedAttribute"
			    or "NullableAttribute"
			    or "NullableContextAttribute"
			    or "IsReadOnlyAttribute"
			    or "IsByRefLikeAttribute"
			    or "IsUnmanagedAttribute"
			    or "TupleElementNamesAttribute"
			    or "DynamicAttribute"
			    or "ExtensionAttribute"
			    or "ParamArrayAttribute"
			    or "AsyncStateMachineAttribute"
			    or "IteratorStateMachineAttribute"
			    or "AsyncIteratorStateMachineAttribute"
			    or "DebuggerStepThroughAttribute"
			    or "DebuggerHiddenAttribute")
			{
				continue;
			}

			// Remove "Attribute" suffix for display
			if (attrName.EndsWith("Attribute"))
			{
				attrName = attrName[..^"Attribute".Length];
			}

			var arguments = attr.ConstructorArguments
				.Select(a => a.Value?.ToString() ?? "null")
				.ToList();

			// Also include named arguments
			foreach (CustomAttributeNamedArgument named in attr.NamedArguments)
			{
				arguments.Add($"{named.MemberName} = {named.TypedValue.Value ?? "null"}");
			}

			result.Add(new ApiAttribute
			{
				Name = attrName,
				Arguments = arguments
			});
		}

		return result;
	}

	#endregion

	#region Signature Builders

	private static string BuildMethodSignature(
		Type type,
		MethodInfo method,
		ApiAccessibility accessibility,
		bool isOperator,
		bool isExtensionMethod)
	{
		var sb = new StringBuilder();
		sb.Append(AccessPrefix(type, accessibility));
		sb.Append(MethodModifiers(method));

		string parameters = FormatParameterList(method.GetParameters(), isExtensionMethod);
		string returnType = FormatReturn(method);

		if (isOperator && method.Name is "op_Implicit" or "op_Explicit" or "op_CheckedExplicit")
		{
			sb.Append(method.Name switch
			{
				"op_Implicit" => "implicit operator ",
				"op_Explicit" => "explicit operator ",
				_ => "explicit operator checked "
			});
			sb.Append(returnType);
			sb.Append($"({parameters})");
			return sb.ToString();
		}

		sb.Append(returnType);
		sb.Append(' ');

		if (isOperator && _operatorTokens.TryGetValue(method.Name, out string? token))
		{
			sb.Append($"operator {token}({parameters})");
			return sb.ToString();
		}

		sb.Append(method.Name);

		if (method.IsGenericMethodDefinition)
		{
			sb.Append('<');
			sb.Append(string.Join(", ", method.GetGenericArguments().Select(t => t.Name)));
			sb.Append('>');
		}

		sb.Append($"({parameters})");
		return sb.ToString();
	}

	private static string BuildFieldSignature(Type type, FieldInfo field, ApiAccessibility accessibility, string fieldType)
	{
		var sb = new StringBuilder();
		sb.Append(AccessPrefix(type, accessibility));

		if (field.IsLiteral)
		{
			sb.Append($"const {fieldType} {field.Name} = {FormatConstant(field.GetRawConstantValue(), field.FieldType)}");
			return sb.ToString();
		}

		if (field.IsStatic)
		{
			sb.Append("static ");
		}

		if (field.IsInitOnly)
		{
			sb.Append("readonly ");
		}

		if (HasAttribute(field.GetCustomAttributesData(), "System.Runtime.CompilerServices.RequiredMemberAttribute"))
		{
			sb.Append("required ");
		}

		if (field.GetRequiredCustomModifiers().Any(m => m.FullName == "System.Runtime.CompilerServices.IsVolatile"))
		{
			sb.Append("volatile ");
		}

		sb.Append(fieldType);
		sb.Append(' ');
		sb.Append(field.Name);
		return sb.ToString();
	}

	/// <summary>
	///     <c>{ get; set; }</c>, with an accessor's own accessibility when it differs from the
	///     property's, as in <c>{ get; private set; }</c>, and <c>init</c> for init-only setters.
	/// </summary>
	private static string BuildAccessorList(MethodInfo? getter, MethodInfo? setter, ApiAccessibility propertyAccessibility)
	{
		var accessors = new List<string>(2);
		if (getter is not null)
		{
			accessors.Add(AccessorPrefix(getter, propertyAccessibility) + "get;");
		}

		if (setter is not null)
		{
			bool isInit = setter.ReturnParameter.GetRequiredCustomModifiers()
				.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
			accessors.Add(AccessorPrefix(setter, propertyAccessibility) + (isInit ? "init;" : "set;"));
		}

		return accessors.Count == 0 ? "{ }" : $"{{ {string.Join(" ", accessors)} }}";
	}

	private static string AccessorPrefix(MethodInfo accessor, ApiAccessibility propertyAccessibility)
	{
		ApiAccessibility accessibility = GetAccessibility(accessor);
		return accessibility == propertyAccessibility ? "" : Keyword(accessibility) + " ";
	}

	/// <summary>
	///     A method's return type as written in its declaration, including <c>ref</c> and
	///     <c>ref readonly</c> returns.
	/// </summary>
	private static string FormatReturn(MethodInfo method)
	{
		string returnType = FormatTypeName(method.ReturnType, ReturnAnnotations(method));
		if (!method.ReturnType.IsByRef)
		{
			return returnType;
		}

		bool isReadOnly = HasAttribute(method.ReturnParameter.GetCustomAttributesData(),
			"System.Runtime.CompilerServices.IsReadOnlyAttribute");
		return (isReadOnly ? "ref readonly " : "ref ") + returnType;
	}

	private static NullableAnnotations ReturnAnnotations(MethodInfo method) =>
		NullableAnnotations.For(method.ReturnParameter.GetCustomAttributesData(), method);

	private static string FormatTypeParameterDeclaration(Type typeParameter)
	{
		GenericParameterAttributes variance = typeParameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask;
		return variance switch
		{
			GenericParameterAttributes.Contravariant => "in " + typeParameter.Name,
			GenericParameterAttributes.Covariant => "out " + typeParameter.Name,
			_ => typeParameter.Name
		};
	}

	private static string FormatParameterList(IReadOnlyList<ParameterInfo> parameters, bool isExtensionMethod)
	{
		if (parameters.Count == 0)
		{
			return "";
		}

		var parts = new List<string>(parameters.Count);

		for (int i = 0; i < parameters.Count; i++)
		{
			ParameterInfo param = parameters[i];
			var sb = new StringBuilder();

			if (i == 0 && isExtensionMethod)
			{
				sb.Append("this ");
			}

			sb.Append(GetParameterModifier(param));
			sb.Append(FormatParameterType(param));
			sb.Append(' ');
			sb.Append(param.Name ?? "arg");

			if (param.HasDefaultValue)
			{
				sb.Append(" = ");
				sb.Append(FormatDefaultValue(param));
			}

			parts.Add(sb.ToString());
		}

		return string.Join(", ", parts);
	}

	/// <summary>
	///     <c>params</c>, <c>out</c>, <c>ref readonly</c>, <c>in</c> or <c>ref</c>, with a trailing space.
	/// </summary>
	/// <remarks>
	///     <c>in</c> is read from the parameter only when it is passed by reference: interop
	///     signatures mark by-value parameters <c>[In]</c> as well.
	/// </remarks>
	private static string GetParameterModifier(ParameterInfo param)
	{
		if (IsParams(param))
		{
			return "params ";
		}

		if (!param.ParameterType.IsByRef)
		{
			return "";
		}

		if (param.IsOut)
		{
			return "out ";
		}

		if (HasAttribute(param.GetCustomAttributesData(), "System.Runtime.CompilerServices.RequiresLocationAttribute"))
		{
			return "ref readonly ";
		}

		return param.IsIn ? "in " : "ref ";
	}

	private static bool IsParams(ParameterInfo param) =>
		param.GetCustomAttributesData().Any(a => a.AttributeType.FullName
			is "System.ParamArrayAttribute" or "System.Runtime.CompilerServices.ParamCollectionAttribute");

	private static string FormatParameterType(ParameterInfo param) =>
		FormatTypeName(param.ParameterType,
			NullableAnnotations.For(param.GetCustomAttributesData(), param.Member));

	private static string FormatDefaultValue(ParameterInfo param)
	{
		Type type = param.ParameterType.IsByRef ? param.ParameterType.GetElementType()! : param.ParameterType;
		return FormatConstant(param.DefaultValue, type);
	}

	/// <summary>
	///     A constant as C# source writes it. Numbers use the invariant culture; they used the
	///     server's, so a German locale showed <c>1,5</c>.
	/// </summary>
	private static string FormatConstant(object? value, Type type)
	{
		Type valueType = Nullable.GetUnderlyingType(type) ?? type;

		if (value is null or DBNull || value == Missing.Value)
		{
			// `= default` on a struct or type parameter reads back as null; "null" did not compile.
			bool isDefault = type.IsGenericParameter || (type.IsValueType && Nullable.GetUnderlyingType(type) is null);
			return isDefault ? "default" : "null";
		}

		if (valueType.IsEnum)
		{
			return FormatEnumValue(valueType, value);
		}

		return value switch
		{
			bool b => b ? "true" : "false",
			string s => "\"" + EscapeLiteral(s, '"') + "\"",
			char c => "'" + EscapeLiteral(c.ToString(), '\'') + "'",
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString() ?? "default"
		};
	}

	private static string FormatEnumValue(Type enumType, object value)
	{
		string typeName = FormatTypeName(enumType);
		string text;
		try
		{
			text = Enum.ToObject(enumType, value).ToString() ?? "";
		}
		catch (ArgumentException)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
		}

		// A value no member matches formats as its number.
		if (text.Length == 0 || char.IsDigit(text[0]) || text[0] == '-')
		{
			return $"({typeName}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
		}

		return string.Join(" | ", text.Split(", ").Select(name => $"{typeName}.{name}"));
	}

	private static string EscapeLiteral(string value, char quote)
	{
		var sb = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			sb.Append(c switch
			{
				'\\' => "\\\\",
				'\n' => "\\n",
				'\r' => "\\r",
				'\t' => "\\t",
				'\0' => "\\0",
				_ when c == quote => "\\" + quote,
				_ => c.ToString()
			});
		}

		return sb.ToString();
	}

	#endregion

	#region Type Name Formatting

	/// <summary>
	///     A type as C# source refers to it: keywords for built-in types, no namespaces, generic
	///     arguments in angle brackets and containing types before nested ones. With nullable
	///     annotations, reference types the declaration marked with <c>?</c> keep it.
	/// </summary>
	private static string FormatTypeName(Type type, NullableAnnotations? nullable = null)
	{
		// A by-ref type (ref, out, in) shares its annotation position with the type it refers to.
		if (type.IsByRef)
		{
			return FormatTypeName(type.GetElementType()!, nullable);
		}

		if (type.IsPointer)
		{
			return FormatTypeName(type.GetElementType()!, nullable) + "*";
		}

		if (type.IsArray)
		{
			bool arrayAnnotated = nullable?.NextIsAnnotated() == true;
			string elementType = FormatTypeName(type.GetElementType()!, nullable);
			int rank = type.GetArrayRank();
			string commas = rank > 1 ? new string(',', rank - 1) : "";
			return $"{elementType}[{commas}]{(arrayAnnotated ? "?" : "")}";
		}

		// Nullable<T> has no annotation position of its own.
		Type? underlyingNullable = Nullable.GetUnderlyingType(type);
		if (underlyingNullable is not null)
		{
			return FormatTypeName(underlyingNullable, nullable) + "?";
		}

		if (type.IsGenericParameter)
		{
			return type.Name + (nullable?.NextIsAnnotated() == true ? "?" : "");
		}

		// Reference types take a position; value types only when they are generic.
		bool annotated = false;
		if (!type.IsValueType)
		{
			annotated = nullable?.NextIsAnnotated() == true;
		}
		else if (type.IsGenericType)
		{
			nullable?.NextIsAnnotated();
		}

		string name = FormatNamedType(type, nullable);
		return annotated ? name + "?" : name;
	}

	private static string FormatNamedType(Type type, NullableAnnotations? nullable)
	{
		if (!type.IsGenericType)
		{
			if (type.FullName is not null && _clrToCSharpTypeNames.TryGetValue(type.FullName, out string? keyword))
			{
				return keyword;
			}

			return string.Join(".", DeclaringChain(type).Select(t => t.Name));
		}

		Type definition = type.GetGenericTypeDefinition();
		Type[] arguments = type.GetGenericArguments();

		if (definition.FullName is { } definitionName
		    && definitionName.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
		    && arguments.Length is >= 2 and <= 7)
		{
			return $"({string.Join(", ", arguments.Select(a => FormatTypeName(a, nullable)))})";
		}

		// Reflection flattens the arguments of containing types into the nested type's list,
		// outermost first. Each level takes as many as its arity. Nested generic types used to
		// lose everything after the first backtick, so Outer<T>.Inner displayed as Outer<T>.
		var sb = new StringBuilder();
		int consumed = 0;
		foreach (Type level in DeclaringChain(definition))
		{
			if (sb.Length > 0)
			{
				sb.Append('.');
			}

			(string name, int arity) = SplitArity(level.Name);
			sb.Append(name);

			if (arity > 0 && consumed + arity <= arguments.Length)
			{
				sb.Append('<');
				sb.Append(string.Join(", ", arguments.Skip(consumed).Take(arity).Select(a => FormatTypeName(a, nullable))));
				sb.Append('>');
				consumed += arity;
			}
		}

		return sb.ToString();
	}

	/// <summary>
	///     The type and the types containing it, outermost first.
	/// </summary>
	private static List<Type> DeclaringChain(Type type)
	{
		var chain = new List<Type>();
		for (Type? current = type; current is not null; current = current.IsNested ? current.DeclaringType : null)
		{
			chain.Add(current);
		}

		chain.Reverse();
		return chain;
	}

	private static (string Name, int Arity) SplitArity(string metadataName)
	{
		int backtick = metadataName.IndexOf('`');
		if (backtick < 0)
		{
			return (metadataName, 0);
		}

		return int.TryParse(metadataName.AsSpan(backtick + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int arity)
			? (metadataName[..backtick], arity)
			: (metadataName[..backtick], 0);
	}

	private static string GetSimpleTypeName(Type type) => SplitArity(type.Name).Name;

	#endregion

	#region XML Doc Member ID Generation

	/// <summary>
	///     The type's name in documentation IDs. Reflection separates a nested type from its
	///     container with <c>+</c> where the XML file uses <c>.</c>, so nested types and their
	///     members found no documentation.
	/// </summary>
	private static string GetDocIdName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

	private static string GetMemberIdTypeName(Type type)
	{
		if (type.IsByRef)
		{
			return GetMemberIdTypeName(type.GetElementType()!) + "@";
		}

		if (type.IsPointer)
		{
			return GetMemberIdTypeName(type.GetElementType()!) + "*";
		}

		if (type.IsArray)
		{
			string elementType = GetMemberIdTypeName(type.GetElementType()!);
			if (type.IsSZArray)
			{
				return $"{elementType}[]";
			}

			// The compiler writes a lower bound for each dimension: int[,] is System.Int32[0:,0:].
			return $"{elementType}[{string.Join(",", Enumerable.Repeat("0:", type.GetArrayRank()))}]";
		}

		if (type.IsGenericParameter)
		{
			// Type-level generic parameter: `N, method-level: ``N
			if (type.DeclaringMethod is not null)
			{
				return "``" + type.GenericParameterPosition;
			}

			return "`" + type.GenericParameterPosition;
		}

		if (!type.IsGenericType)
		{
			return GetDocIdName(type);
		}

		Type definition = type.GetGenericTypeDefinition();
		Type[] arguments = type.GetGenericArguments();
		var sb = new StringBuilder();
		int consumed = 0;

		foreach (Type level in DeclaringChain(definition))
		{
			if (sb.Length == 0)
			{
				if (!string.IsNullOrEmpty(level.Namespace))
				{
					sb.Append(level.Namespace).Append('.');
				}
			}
			else
			{
				sb.Append('.');
			}

			(string name, int arity) = SplitArity(level.Name);
			sb.Append(name);

			if (arity > 0 && consumed + arity <= arguments.Length)
			{
				sb.Append('{');
				sb.Append(string.Join(",", arguments.Skip(consumed).Take(arity).Select(GetMemberIdTypeName)));
				sb.Append('}');
				consumed += arity;
			}
		}

		return sb.ToString();
	}

	private static string GetConstructorMemberId(Type type, ConstructorInfo ctor)
	{
		ParameterInfo[] parameters = ctor.GetParameters();
		if (parameters.Length == 0)
		{
			return $"M:{GetDocIdName(type)}.#ctor";
		}

		string paramTypes = string.Join(",", parameters.Select(p => GetMemberIdTypeName(p.ParameterType)));
		return $"M:{GetDocIdName(type)}.#ctor({paramTypes})";
	}

	private static string GetMethodMemberId(Type type, MethodInfo method)
	{
		var sb = new StringBuilder();
		sb.Append("M:");
		sb.Append(GetDocIdName(type));
		sb.Append('.');
		sb.Append(method.Name);

		if (method.IsGenericMethodDefinition)
		{
			sb.Append("``");
			sb.Append(method.GetGenericArguments().Length);
		}

		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length > 0)
		{
			sb.Append('(');
			sb.Append(string.Join(",", parameters.Select(p => GetMemberIdTypeName(p.ParameterType))));
			sb.Append(')');
		}

		// Conversion operators differ only by return type, so their IDs end with it.
		if (method.Name is "op_Implicit" or "op_Explicit" or "op_CheckedExplicit")
		{
			sb.Append('~');
			sb.Append(GetMemberIdTypeName(method.ReturnType));
		}

		return sb.ToString();
	}

	#endregion

	#region XML Doc Lookup Helpers

	private static XmlDocBlock? LookupTypeDoc(Type type, Dictionary<string, XmlDocFile> xmlDocs)
	{
		string assemblyName = type.Assembly.GetName().Name ?? "";
		if (!xmlDocs.TryGetValue(assemblyName, out XmlDocFile? docFile))
		{
			return null;
		}

		return docFile.GetMemberDoc($"T:{GetDocIdName(type)}");
	}

	private static XmlDocBlock? LookupMemberDoc(Type type, string memberId, Dictionary<string, XmlDocFile> xmlDocs)
	{
		string assemblyName = type.Assembly.GetName().Name ?? "";
		if (!xmlDocs.TryGetValue(assemblyName, out XmlDocFile? docFile))
		{
			return null;
		}

		return docFile.GetMemberDoc(memberId);
	}

	#endregion

	#region Parameter Building

	private static List<ApiParameter> BuildParameters(ParameterInfo[] parameters)
	{
		return parameters.Select(p =>
		{
			Type paramType = p.ParameterType;
			bool isByRef = paramType.IsByRef;
			if (isByRef)
			{
				paramType = paramType.GetElementType()!;
			}

			return new ApiParameter
			{
				Name = p.Name ?? "arg",
				Type = FormatParameterType(p),
				HasDefaultValue = p.HasDefaultValue,
				DefaultValue = p.HasDefaultValue ? FormatDefaultValue(p) : null,
				IsParams = IsParams(p),
				IsRef = isByRef && !p.IsOut && !p.IsIn,
				IsOut = p.IsOut,
				IsIn = isByRef && p.IsIn,
				IsNullable = Nullable.GetUnderlyingType(paramType) is not null
				             || (!paramType.IsValueType
				                 && NullableAnnotations.For(p.GetCustomAttributesData(), p.Member).NextIsAnnotated())
			};
		}).ToList();
	}

	#endregion

	#region Nullable Annotations

	/// <summary>
	///     Reads the nullable annotations the C# compiler writes into metadata: one flag per type
	///     reference, in the order <see cref="FormatTypeName" /> visits them.
	/// </summary>
	/// <remarks>
	///     <see cref="NullabilityInfoContext" /> answers whether a value may be null rather than
	///     what was declared. It reports an unconstrained <c>T</c> as nullable, which would render
	///     <c>T?</c> where the source says <c>T</c>.
	/// </remarks>
	private sealed class NullableAnnotations
	{
		private const byte _annotated = 2;
		private readonly byte[] _flags;
		private int _position;

		private NullableAnnotations(byte[] flags) => _flags = flags;

		/// <summary>
		///     The annotations for one type reference: its own <c>[Nullable]</c> attribute, else the
		///     nearest <c>[NullableContext]</c> on the member or a containing type.
		/// </summary>
		public static NullableAnnotations For(IEnumerable<CustomAttributeData> attributes, MemberInfo context)
		{
			foreach (CustomAttributeData attribute in attributes)
			{
				if (attribute.AttributeType.FullName != "System.Runtime.CompilerServices.NullableAttribute"
				    || attribute.ConstructorArguments.Count != 1)
				{
					continue;
				}

				object? value = attribute.ConstructorArguments[0].Value;
				if (value is byte single)
				{
					return new NullableAnnotations([single]);
				}

				if (value is IReadOnlyCollection<CustomAttributeTypedArgument> flags)
				{
					return new NullableAnnotations(flags.Select(f => f.Value is byte b ? b : (byte)0).ToArray());
				}
			}

			for (MemberInfo? member = context; member is not null; member = member.DeclaringType)
			{
				CustomAttributeData? contextAttribute = member.GetCustomAttributesData().FirstOrDefault(a =>
					a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
				if (contextAttribute?.ConstructorArguments is [{ Value: byte flag }])
				{
					return new NullableAnnotations([flag]);
				}
			}

			return new NullableAnnotations([]);
		}

		/// <summary>
		///     Whether the next type reference is annotated with <c>?</c>. Moves past it.
		/// </summary>
		public bool NextIsAnnotated()
		{
			// A single flag stands for every position.
			byte flag = _flags.Length switch
			{
				0 => 0,
				1 => _flags[0],
				_ => _position < _flags.Length ? _flags[_position] : (byte)0
			};

			_position++;
			return flag == _annotated;
		}
	}

	#endregion
}
