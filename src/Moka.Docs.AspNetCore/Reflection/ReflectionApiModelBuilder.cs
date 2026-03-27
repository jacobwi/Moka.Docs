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
public sealed class ReflectionApiModelBuilder(
	XmlDocParser xmlDocParser,
	InheritDocResolver inheritDocResolver,
	ILogger<ReflectionApiModelBuilder> logger)
{
	private static readonly Dictionary<string, string> ClrToCSharpTypeNames = new(StringComparer.Ordinal)
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
			try
			{
				Type[] exportedTypes = assembly.GetExportedTypes();
				foreach (Type type in exportedTypes)
				{
					if (ShouldSkipType(type))
					{
						continue;
					}

					allTypes.Add((type, assemblyName));
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				logger.LogWarning(ex, "Could not load all types from assembly {Assembly}", assemblyName);
				foreach (Type? type in ex.Types)
				{
					if (type is null || ShouldSkipType(type))
					{
						continue;
					}

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
		bool isSealed = type.IsSealed && !type.IsAbstract;

		string? baseType = GetBaseTypeName(type);
		List<string> interfaces = GetDirectInterfaces(type);
		List<ApiTypeParameter> typeParameters = GetTypeParameters(type);

		ObsoleteAttribute? obsoleteAttr = type.GetCustomAttribute<ObsoleteAttribute>();
		List<ApiAttribute> attributes = GetAttributes(type.GetCustomAttributesData());

		string typeName = GetSimpleTypeName(type);
		string fullName = type.Namespace is not null ? $"{type.Namespace}.{typeName}" : typeName;

		XmlDocBlock? documentation = LookupTypeDoc(type, xmlDocs);

		List<ApiMember> members = BuildMembers(type, kind, xmlDocs);

		return new ApiType
		{
			Name = typeName,
			FullName = fullName,
			Kind = isRecord ? ApiTypeKind.Record : kind,
			Accessibility = ApiAccessibility.Public,
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
			.Select(FormatTypeName)
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
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		var members = new List<ApiMember>();

		if (kind == ApiTypeKind.Enum)
		{
			BuildEnumFields(type, members, xmlDocs);
			return members;
		}

		if (kind == ApiTypeKind.Delegate)
			// Delegates don't have meaningful members to expose
		{
			return members;
		}

		BuildConstructors(type, members, xmlDocs);
		BuildProperties(type, members, xmlDocs);
		BuildMethods(type, members, xmlDocs);
		BuildFields(type, members, xmlDocs);
		BuildEvents(type, members, xmlDocs);

		return members;
	}

	private void BuildConstructors(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		foreach (ConstructorInfo ctor in ctors)
		{
			if (ctor.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
			{
				continue;
			}

			List<ApiParameter> parameters = BuildParameters(ctor.GetParameters());
			string signature = BuildConstructorSignature(type, ctor);
			string memberId = GetConstructorMemberId(type, ctor);
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);

			members.Add(new ApiMember
			{
				Name = GetSimpleTypeName(type),
				Kind = ApiMemberKind.Constructor,
				Signature = signature,
				Parameters = parameters,
				Documentation = documentation,
				Accessibility = ApiAccessibility.Public
			});
		}
	}

	private void BuildProperties(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		PropertyInfo[] properties = type.GetProperties(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

		foreach (PropertyInfo prop in properties)
		{
			// Skip indexers — they are treated separately
			ParameterInfo[] indexParams = prop.GetIndexParameters();
			if (indexParams.Length > 0)
			{
				BuildIndexer(type, prop, members, xmlDocs);
				continue;
			}

			string returnType = FormatTypeName(prop.PropertyType);
			string signature = BuildPropertySignature(prop);
			string memberId = $"P:{type.FullName}.{prop.Name}";
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);

			MethodInfo? getter = prop.GetGetMethod();
			MethodInfo? setter = prop.GetSetMethod();

			bool isStatic = (getter?.IsStatic ?? setter?.IsStatic) == true;
			bool isVirtual = (getter?.IsVirtual ?? setter?.IsVirtual) == true &&
			                 !(getter?.IsFinal ?? setter?.IsFinal ?? false);
			bool isAbstract = (getter?.IsAbstract ?? setter?.IsAbstract) == true;
			bool isOverride = isVirtual && ((getter is not null && getter.GetBaseDefinition() != getter)
			                                || (setter is not null && setter.GetBaseDefinition() != setter));

			ObsoleteAttribute? obsoleteAttr = prop.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = prop.Name,
				Kind = ApiMemberKind.Property,
				Signature = signature,
				ReturnType = returnType,
				Accessibility = ApiAccessibility.Public,
				IsStatic = isStatic,
				IsVirtual = isVirtual && !isAbstract && !isOverride,
				IsAbstract = isAbstract,
				IsOverride = isOverride,
				Documentation = documentation,
				Attributes = GetAttributes(prop.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private void BuildIndexer(
		Type type,
		PropertyInfo prop,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		ParameterInfo[] indexParams = prop.GetIndexParameters();
		List<ApiParameter> parameters = BuildParameters(indexParams);
		string returnType = FormatTypeName(prop.PropertyType);

		string paramTypes = string.Join(",", indexParams.Select(p => GetMemberIdTypeName(p.ParameterType)));
		string memberId = $"P:{type.FullName}.Item({paramTypes})";
		XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);

		MethodInfo? getter = prop.GetGetMethod();
		MethodInfo? setter = prop.GetSetMethod();
		string accessors = BuildAccessorString(getter is not null, setter is not null);

		string paramList = string.Join(", ", indexParams.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
		string signature = $"{returnType} this[{paramList}] {accessors}";

		members.Add(new ApiMember
		{
			Name = "this[]",
			Kind = ApiMemberKind.Indexer,
			Signature = signature,
			ReturnType = returnType,
			Parameters = parameters,
			Accessibility = ApiAccessibility.Public,
			Documentation = documentation
		});
	}

	private void BuildMethods(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		MethodInfo[] methods = type.GetMethods(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

		foreach (MethodInfo method in methods)
		{
			if (method.IsSpecialName)
			{
				continue;
			}

			if (method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
			{
				continue;
			}

			// Skip compiler-generated record methods
			if (method.Name is "<Clone>$" or "$" || method.Name.StartsWith('<'))
			{
				continue;
			}

			bool isOperator = method.Name.StartsWith("op_");
			ApiMemberKind memberKind = isOperator ? ApiMemberKind.Operator : ApiMemberKind.Method;

			List<ApiParameter> parameters = BuildParameters(method.GetParameters());
			List<ApiTypeParameter> typeParameters = GetMethodTypeParameters(method);
			string returnType = FormatTypeName(method.ReturnType);
			string signature = BuildMethodSignature(method);
			string memberId = GetMethodMemberId(type, method);
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);

			bool isVirtual = method.IsVirtual && !method.IsFinal;
			bool isOverride = method.IsVirtual && method.GetBaseDefinition().DeclaringType != type;
			ObsoleteAttribute? obsoleteAttr = method.GetCustomAttribute<ObsoleteAttribute>();

			bool isExtensionMethod = method.IsStatic
			                         && method.IsDefined(typeof(ExtensionAttribute), false);

			members.Add(new ApiMember
			{
				Name = method.Name,
				Kind = memberKind,
				Signature = signature,
				ReturnType = returnType,
				Accessibility = ApiAccessibility.Public,
				IsStatic = method.IsStatic,
				IsVirtual = isVirtual && !method.IsAbstract && !isOverride,
				IsAbstract = method.IsAbstract,
				IsOverride = isOverride,
				IsSealed = method.IsFinal && isOverride,
				IsExtensionMethod = isExtensionMethod,
				Parameters = parameters,
				TypeParameters = typeParameters,
				Documentation = documentation,
				Attributes = GetAttributes(method.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private void BuildFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
		                     BindingFlags.DeclaredOnly;
		FieldInfo[] fields = type.GetFields(flags);

		foreach (FieldInfo field in fields)
		{
			if (field.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
			{
				continue;
			}

			// Skip backing fields
			if (field.Name.Contains('<') || field.Name.EndsWith("k__BackingField"))
			{
				continue;
			}

			string returnType = FormatTypeName(field.FieldType);
			string signature = BuildFieldSignature(field);
			string memberId = $"F:{type.FullName}.{field.Name}";
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);
			ObsoleteAttribute? obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = field.Name,
				Kind = ApiMemberKind.Field,
				Signature = signature,
				ReturnType = returnType,
				Accessibility = ApiAccessibility.Public,
				IsStatic = field.IsStatic,
				Documentation = documentation,
				Attributes = GetAttributes(field.GetCustomAttributesData()),
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
	}

	private void BuildEnumFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

		foreach (FieldInfo field in fields)
		{
			string memberId = $"F:{type.FullName}.{field.Name}";
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);
			ObsoleteAttribute? obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

			object? rawValue = field.GetRawConstantValue();
			string signature = rawValue is not null
				? $"{field.Name} = {rawValue}"
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

	private void BuildEvents(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		EventInfo[] events = type.GetEvents(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

		foreach (EventInfo evt in events)
		{
			string handlerType = evt.EventHandlerType is not null
				? FormatTypeName(evt.EventHandlerType)
				: "EventHandler";

			string signature = $"event {handlerType} {evt.Name}";
			string memberId = $"E:{type.FullName}.{evt.Name}";
			XmlDocBlock? documentation = LookupMemberDoc(type, memberId, xmlDocs);

			MethodInfo? addMethod = evt.GetAddMethod();
			bool isStatic = addMethod?.IsStatic == true;
			ObsoleteAttribute? obsoleteAttr = evt.GetCustomAttribute<ObsoleteAttribute>();

			members.Add(new ApiMember
			{
				Name = evt.Name ?? "",
				Kind = ApiMemberKind.Event,
				Signature = signature,
				ReturnType = handlerType,
				Accessibility = ApiAccessibility.Public,
				IsStatic = isStatic,
				Documentation = documentation,
				IsObsolete = obsoleteAttr is not null,
				ObsoleteMessage = obsoleteAttr?.Message
			});
		}
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

	private static string BuildConstructorSignature(Type type, ConstructorInfo ctor)
	{
		string name = GetSimpleTypeName(type);
		string parameters = FormatParameterList(ctor.GetParameters());
		return $"{name}({parameters})";
	}

	private static string BuildMethodSignature(MethodInfo method)
	{
		var sb = new StringBuilder();
		sb.Append(FormatTypeName(method.ReturnType));
		sb.Append(' ');
		sb.Append(method.Name);

		if (method.IsGenericMethodDefinition)
		{
			Type[] typeArgs = method.GetGenericArguments();
			sb.Append('<');
			sb.Append(string.Join(", ", typeArgs.Select(t => t.Name)));
			sb.Append('>');
		}

		sb.Append('(');
		sb.Append(FormatParameterList(method.GetParameters()));
		sb.Append(')');

		return sb.ToString();
	}

	private static string BuildPropertySignature(PropertyInfo prop)
	{
		string typeName = FormatTypeName(prop.PropertyType);
		MethodInfo? getter = prop.GetGetMethod();
		MethodInfo? setter = prop.GetSetMethod();
		string accessors = BuildAccessorString(getter is not null, setter is not null);
		return $"{typeName} {prop.Name} {accessors}";
	}

	private static string BuildFieldSignature(FieldInfo field)
	{
		string typeName = FormatTypeName(field.FieldType);
		if (field.IsLiteral)
		{
			object? value = field.GetRawConstantValue();
			string valueStr = value is string s ? $"\"{s}\"" : value?.ToString() ?? "null";
			return $"const {typeName} {field.Name} = {valueStr}";
		}

		var sb = new StringBuilder();
		if (field.IsStatic)
		{
			sb.Append("static ");
		}

		if (field.IsInitOnly)
		{
			sb.Append("readonly ");
		}

		sb.Append(typeName);
		sb.Append(' ');
		sb.Append(field.Name);
		return sb.ToString();
	}

	private static string BuildAccessorString(bool hasGetter, bool hasSetter)
	{
		return (hasGetter, hasSetter) switch
		{
			(true, true) => "{ get; set; }",
			(true, false) => "{ get; }",
			(false, true) => "{ set; }",
			_ => "{ }"
		};
	}

	private static string FormatParameterList(ParameterInfo[] parameters)
	{
		if (parameters.Length == 0)
		{
			return "";
		}

		var parts = new List<string>(parameters.Length);

		foreach (ParameterInfo param in parameters)
		{
			var sb = new StringBuilder();

			if (param.IsDefined(typeof(ParamArrayAttribute), false))
			{
				sb.Append("params ");
			}
			else if (param.IsIn)
			{
				sb.Append("in ");
			}
			else if (param.IsOut)
			{
				sb.Append("out ");
			}
			else if (param.ParameterType.IsByRef)
			{
				sb.Append("ref ");
			}

			Type paramType = param.ParameterType;
			if (paramType.IsByRef)
			{
				paramType = paramType.GetElementType()!;
			}

			sb.Append(FormatTypeName(paramType));
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

	private static string FormatDefaultValue(ParameterInfo param)
	{
		object? value = param.DefaultValue;
		if (value is null)
		{
			return "null";
		}

		if (value is bool b)
		{
			return b ? "true" : "false";
		}

		if (value is string s)
		{
			return $"\"{s}\"";
		}

		if (value is char c)
		{
			return $"'{c}'";
		}

		if (value.GetType().IsEnum)
		{
			return $"{FormatTypeName(value.GetType())}.{value}";
		}

		return value.ToString() ?? "default";
	}

	#endregion

	#region Type Name Formatting

	private static string FormatTypeName(Type type)
	{
		// Handle by-ref types (ref, out, in)
		if (type.IsByRef)
		{
			return FormatTypeName(type.GetElementType()!);
		}

		// Handle pointer types
		if (type.IsPointer)
		{
			return FormatTypeName(type.GetElementType()!) + "*";
		}

		// Handle array types
		if (type.IsArray)
		{
			string elementType = FormatTypeName(type.GetElementType()!);
			int rank = type.GetArrayRank();
			string commas = rank > 1 ? new string(',', rank - 1) : "";
			return $"{elementType}[{commas}]";
		}

		// Handle Nullable<T>
		Type? underlyingNullable = Nullable.GetUnderlyingType(type);
		if (underlyingNullable is not null)
		{
			return FormatTypeName(underlyingNullable) + "?";
		}

		// Handle generic types
		if (type.IsGenericType)
		{
			Type definition = type.GetGenericTypeDefinition();
			string fullName = definition.FullName ?? definition.Name;

			// Remove the arity suffix (e.g., `2)
			int backtickIndex = fullName.IndexOf('`');
			if (backtickIndex > 0)
			{
				fullName = fullName[..backtickIndex];
			}

			// Use C# alias if available
			if (ClrToCSharpTypeNames.TryGetValue(fullName, out string? alias))
			{
				return alias;
			}

			// Use simple name (without namespace) for the generic type
			string simpleName = fullName;
			int lastDot = simpleName.LastIndexOf('.');
			if (lastDot >= 0)
			{
				simpleName = simpleName[(lastDot + 1)..];
			}

			// Nested types use + in CLR names
			simpleName = simpleName.Replace('+', '.');

			Type[] typeArgs = type.GetGenericArguments();
			string formattedArgs = string.Join(", ", typeArgs.Select(FormatTypeName));

			return $"{simpleName}<{formattedArgs}>";
		}

		// Handle generic type parameters (T, TKey, etc.)
		if (type.IsGenericParameter)
		{
			return type.Name;
		}

		// Check CLR-to-C# keyword map
		string typeFullName = type.FullName ?? type.Name;
		if (ClrToCSharpTypeNames.TryGetValue(typeFullName, out string? csharpName))
		{
			return csharpName;
		}

		// Use simple name for types in common namespaces, full name otherwise
		string name = type.Name;

		// Handle nested types
		if (type.IsNested && type.DeclaringType is not null)
		{
			string declaringName = GetSimpleTypeName(type.DeclaringType);
			return $"{declaringName}.{name}";
		}

		return name;
	}

	private static string GetSimpleTypeName(Type type)
	{
		string name = type.Name;
		int backtickIndex = name.IndexOf('`');
		if (backtickIndex > 0)
		{
			name = name[..backtickIndex];
		}

		return name;
	}

	#endregion

	#region XML Doc Member ID Generation

	private static string GetMemberIdTypeName(Type type)
	{
		if (type.IsByRef)
		{
			return GetMemberIdTypeName(type.GetElementType()!) + "@";
		}

		if (type.IsArray)
		{
			string elementType = GetMemberIdTypeName(type.GetElementType()!);
			int rank = type.GetArrayRank();
			if (rank == 1)
			{
				return $"{elementType}[]";
			}

			return $"{elementType}[{new string(',', rank - 1)}]";
		}

		if (type.IsPointer)
		{
			return GetMemberIdTypeName(type.GetElementType()!) + "*";
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

		if (type.IsGenericType)
		{
			Type definition = type.GetGenericTypeDefinition();
			string fullName = definition.FullName ?? definition.Name;
			int backtickIndex = fullName.IndexOf('`');
			if (backtickIndex > 0)
			{
				fullName = fullName[..backtickIndex];
			}

			Type[] typeArgs = type.GetGenericArguments();
			string formattedArgs = string.Join(",", typeArgs.Select(GetMemberIdTypeName));
			return $"{fullName}{{{formattedArgs}}}";
		}

		return type.FullName ?? type.Name;
	}

	private static string GetConstructorMemberId(Type type, ConstructorInfo ctor)
	{
		ParameterInfo[] parameters = ctor.GetParameters();
		if (parameters.Length == 0)
		{
			return $"M:{type.FullName}.#ctor";
		}

		string paramTypes = string.Join(",", parameters.Select(p => GetMemberIdTypeName(p.ParameterType)));
		return $"M:{type.FullName}.#ctor({paramTypes})";
	}

	private static string GetMethodMemberId(Type type, MethodInfo method)
	{
		var sb = new StringBuilder();
		sb.Append("M:");
		sb.Append(type.FullName);
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

		string memberId = $"T:{type.FullName}";
		return docFile.GetMemberDoc(memberId);
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
				Type = FormatTypeName(paramType),
				HasDefaultValue = p.HasDefaultValue,
				DefaultValue = p.HasDefaultValue ? FormatDefaultValue(p) : null,
				IsParams = p.IsDefined(typeof(ParamArrayAttribute), false),
				IsRef = isByRef && !p.IsOut && !p.IsIn,
				IsOut = p.IsOut,
				IsIn = p.IsIn,
				IsNullable = IsNullableParameter(p)
			};
		}).ToList();
	}

	private static bool IsNullableParameter(ParameterInfo param)
	{
		Type paramType = param.ParameterType;
		if (paramType.IsByRef)
		{
			paramType = paramType.GetElementType()!;
		}

		// Nullable value types
		if (Nullable.GetUnderlyingType(paramType) is not null)
		{
			return true;
		}

		// Check NullableAttribute for reference types
		// The attribute is compiler-generated with byte values: 0=oblivious, 1=not-nullable, 2=nullable
		CustomAttributeData? nullableAttr = param.GetCustomAttributesData()
			.FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

		if (nullableAttr?.ConstructorArguments is [{ Value: byte value }])
		{
			return value == 2;
		}

		if (nullableAttr?.ConstructorArguments is [{ Value: IReadOnlyList<CustomAttributeTypedArgument> values }]
		    && values.Count > 0 && values[0].Value is byte firstByte)
		{
			return firstByte == 2;
		}

		return false;
	}

	#endregion
}
