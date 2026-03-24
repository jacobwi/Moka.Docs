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
            foreach (var assembly in assemblies)
            {
                var xmlPath = GetXmlDocPath(assembly);
                if (xmlPath is not null)
                {
                    var docFile = xmlDocParser.Parse(xmlPath);
                    if (docFile.Members.Count > 0) xmlDocs[assembly.GetName().Name ?? ""] = docFile;
                }
            }

        var allTypes = new List<(Type Type, string AssemblyName)>();

        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "";
            try
            {
                var exportedTypes = assembly.GetExportedTypes();
                foreach (var type in exportedTypes)
                {
                    if (ShouldSkipType(type)) continue;
                    allTypes.Add((type, assemblyName));
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.LogWarning(ex, "Could not load all types from assembly {Assembly}", assemblyName);
                foreach (var type in ex.Types)
                {
                    if (type is null || ShouldSkipType(type)) continue;
                    allTypes.Add((type, assemblyName));
                }
            }
        }

        var namespaceGroups = allTypes
            .GroupBy(t => t.Type.Namespace ?? "(global)")
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var namespaces = new List<ApiNamespace>();

        foreach (var nsGroup in namespaceGroups)
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
            return true;

        if (type.Name.Contains('<'))
            return true;

        return false;
    }

    private static string? GetXmlDocPath(Assembly assembly)
    {
        var location = assembly.Location;
        if (string.IsNullOrEmpty(location))
            return null;

        var xmlPath = Path.ChangeExtension(location, ".xml");
        return File.Exists(xmlPath) ? xmlPath : null;
    }

    private ApiType BuildApiType(
        Type type,
        string assemblyName,
        Dictionary<string, XmlDocFile> xmlDocs)
    {
        var kind = ResolveTypeKind(type);
        var isStatic = type.IsAbstract && type.IsSealed;
        var isAbstract = type.IsAbstract && !type.IsSealed;
        var isRecord = IsRecordType(type);
        var isSealed = type.IsSealed && !type.IsAbstract;

        var baseType = GetBaseTypeName(type);
        var interfaces = GetDirectInterfaces(type);
        var typeParameters = GetTypeParameters(type);

        var obsoleteAttr = type.GetCustomAttribute<ObsoleteAttribute>();
        var attributes = GetAttributes(type.GetCustomAttributesData());

        var typeName = GetSimpleTypeName(type);
        var fullName = type.Namespace is not null ? $"{type.Namespace}.{typeName}" : typeName;

        var documentation = LookupTypeDoc(type, xmlDocs);

        var members = BuildMembers(type, kind, xmlDocs);

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
            return ApiTypeKind.Interface;

        if (type.IsEnum)
            return ApiTypeKind.Enum;

        if (type.IsValueType)
            return ApiTypeKind.Struct;

        if (typeof(Delegate).IsAssignableFrom(type) && type != typeof(Delegate) && type != typeof(MulticastDelegate))
            return ApiTypeKind.Delegate;

        return ApiTypeKind.Class;
    }

    private static bool IsRecordType(Type type)
    {
        // Records have a compiler-generated <Clone>$ method and EqualityContract property
        var hasCloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
        var hasEqualityContract = type.GetProperty("EqualityContract",
            BindingFlags.NonPublic | BindingFlags.Instance) is not null;

        return hasCloneMethod && hasEqualityContract;
    }

    private static string? GetBaseTypeName(Type type)
    {
        var baseType = type.BaseType;
        if (baseType is null)
            return null;

        var fullName = baseType.FullName ?? baseType.Name;

        // Skip fundamental base types
        if (fullName is "System.Object" or "System.ValueType" or "System.Enum"
            or "System.Delegate" or "System.MulticastDelegate")
            return null;

        return FormatTypeName(baseType);
    }

    private static List<string> GetDirectInterfaces(Type type)
    {
        var allInterfaces = type.GetInterfaces();
        var baseInterfaces = type.BaseType?.GetInterfaces() ?? [];

        // Also exclude interfaces inherited from other interfaces
        var inherited = new HashSet<Type>(baseInterfaces);
        foreach (var iface in allInterfaces)
        foreach (var parentIface in iface.GetInterfaces())
            inherited.Add(parentIface);

        return allInterfaces
            .Where(i => !inherited.Contains(i))
            .Select(FormatTypeName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ApiTypeParameter> GetTypeParameters(Type type)
    {
        if (!type.IsGenericTypeDefinition)
            return [];

        return type.GetGenericArguments()
            .Select(BuildTypeParameter)
            .ToList();
    }

    private static ApiTypeParameter BuildTypeParameter(Type typeParam)
    {
        var constraints = new List<string>();
        var gpa = typeParam.GenericParameterAttributes;

        if (gpa.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            constraints.Add("struct");
        else if (gpa.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            constraints.Add("class");

        foreach (var constraintType in typeParam.GetGenericParameterConstraints())
        {
            if (constraintType == typeof(ValueType)) continue; // already handled by struct constraint
            constraints.Add(FormatTypeName(constraintType));
        }

        if (gpa.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
            && !gpa.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            constraints.Add("new()");

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
            return members;

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
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var ctor in ctors)
        {
            if (ctor.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;

            var parameters = BuildParameters(ctor.GetParameters());
            var signature = BuildConstructorSignature(type, ctor);
            var memberId = GetConstructorMemberId(type, ctor);
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);

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
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var prop in properties)
        {
            // Skip indexers — they are treated separately
            var indexParams = prop.GetIndexParameters();
            if (indexParams.Length > 0)
            {
                BuildIndexer(type, prop, members, xmlDocs);
                continue;
            }

            var returnType = FormatTypeName(prop.PropertyType);
            var signature = BuildPropertySignature(prop);
            var memberId = $"P:{type.FullName}.{prop.Name}";
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);

            var getter = prop.GetGetMethod();
            var setter = prop.GetSetMethod();

            var isStatic = (getter?.IsStatic ?? setter?.IsStatic) == true;
            var isVirtual = (getter?.IsVirtual ?? setter?.IsVirtual) == true &&
                            !(getter?.IsFinal ?? setter?.IsFinal ?? false);
            var isAbstract = (getter?.IsAbstract ?? setter?.IsAbstract) == true;
            var isOverride = isVirtual && ((getter is not null && getter.GetBaseDefinition() != getter)
                                           || (setter is not null && setter.GetBaseDefinition() != setter));

            var obsoleteAttr = prop.GetCustomAttribute<ObsoleteAttribute>();

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
        var indexParams = prop.GetIndexParameters();
        var parameters = BuildParameters(indexParams);
        var returnType = FormatTypeName(prop.PropertyType);

        var paramTypes = string.Join(",", indexParams.Select(p => GetMemberIdTypeName(p.ParameterType)));
        var memberId = $"P:{type.FullName}.Item({paramTypes})";
        var documentation = LookupMemberDoc(type, memberId, xmlDocs);

        var getter = prop.GetGetMethod();
        var setter = prop.GetSetMethod();
        var accessors = BuildAccessorString(getter is not null, setter is not null);

        var paramList = string.Join(", ", indexParams.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
        var signature = $"{returnType} this[{paramList}] {accessors}";

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
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            if (method.IsSpecialName) continue;
            if (method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;

            // Skip compiler-generated record methods
            if (method.Name is "<Clone>$" or "$" || method.Name.StartsWith('<'))
                continue;

            var isOperator = method.Name.StartsWith("op_");
            var memberKind = isOperator ? ApiMemberKind.Operator : ApiMemberKind.Method;

            var parameters = BuildParameters(method.GetParameters());
            var typeParameters = GetMethodTypeParameters(method);
            var returnType = FormatTypeName(method.ReturnType);
            var signature = BuildMethodSignature(method);
            var memberId = GetMethodMemberId(type, method);
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);

            var isVirtual = method.IsVirtual && !method.IsFinal;
            var isOverride = method.IsVirtual && method.GetBaseDefinition().DeclaringType != type;
            var obsoleteAttr = method.GetCustomAttribute<ObsoleteAttribute>();

            var isExtensionMethod = method.IsStatic
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
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var fields = type.GetFields(flags);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;

            // Skip backing fields
            if (field.Name.Contains('<') || field.Name.EndsWith("k__BackingField"))
                continue;

            var returnType = FormatTypeName(field.FieldType);
            var signature = BuildFieldSignature(field);
            var memberId = $"F:{type.FullName}.{field.Name}";
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);
            var obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

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
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            var memberId = $"F:{type.FullName}.{field.Name}";
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);
            var obsoleteAttr = field.GetCustomAttribute<ObsoleteAttribute>();

            var rawValue = field.GetRawConstantValue();
            var signature = rawValue is not null
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
        var events = type.GetEvents(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var evt in events)
        {
            var handlerType = evt.EventHandlerType is not null
                ? FormatTypeName(evt.EventHandlerType)
                : "EventHandler";

            var signature = $"event {handlerType} {evt.Name}";
            var memberId = $"E:{type.FullName}.{evt.Name}";
            var documentation = LookupMemberDoc(type, memberId, xmlDocs);

            var addMethod = evt.GetAddMethod();
            var isStatic = addMethod?.IsStatic == true;
            var obsoleteAttr = evt.GetCustomAttribute<ObsoleteAttribute>();

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
            return [];

        return method.GetGenericArguments()
            .Select(BuildTypeParameter)
            .ToList();
    }

    #endregion

    #region Attribute Building

    private static List<ApiAttribute> GetAttributes(IList<CustomAttributeData> attributeDataList)
    {
        var result = new List<ApiAttribute>();

        foreach (var attr in attributeDataList)
        {
            var attrName = attr.AttributeType.Name;

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
                continue;

            // Remove "Attribute" suffix for display
            if (attrName.EndsWith("Attribute"))
                attrName = attrName[..^"Attribute".Length];

            var arguments = attr.ConstructorArguments
                .Select(a => a.Value?.ToString() ?? "null")
                .ToList();

            // Also include named arguments
            foreach (var named in attr.NamedArguments)
                arguments.Add($"{named.MemberName} = {named.TypedValue.Value ?? "null"}");

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
        var name = GetSimpleTypeName(type);
        var parameters = FormatParameterList(ctor.GetParameters());
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
            var typeArgs = method.GetGenericArguments();
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
        var typeName = FormatTypeName(prop.PropertyType);
        var getter = prop.GetGetMethod();
        var setter = prop.GetSetMethod();
        var accessors = BuildAccessorString(getter is not null, setter is not null);
        return $"{typeName} {prop.Name} {accessors}";
    }

    private static string BuildFieldSignature(FieldInfo field)
    {
        var typeName = FormatTypeName(field.FieldType);
        if (field.IsLiteral)
        {
            var value = field.GetRawConstantValue();
            var valueStr = value is string s ? $"\"{s}\"" : value?.ToString() ?? "null";
            return $"const {typeName} {field.Name} = {valueStr}";
        }

        var sb = new StringBuilder();
        if (field.IsStatic) sb.Append("static ");
        if (field.IsInitOnly) sb.Append("readonly ");
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
        if (parameters.Length == 0) return "";

        var parts = new List<string>(parameters.Length);

        foreach (var param in parameters)
        {
            var sb = new StringBuilder();

            if (param.IsDefined(typeof(ParamArrayAttribute), false))
                sb.Append("params ");
            else if (param.IsIn)
                sb.Append("in ");
            else if (param.IsOut)
                sb.Append("out ");
            else if (param.ParameterType.IsByRef)
                sb.Append("ref ");

            var paramType = param.ParameterType;
            if (paramType.IsByRef)
                paramType = paramType.GetElementType()!;

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
        var value = param.DefaultValue;
        if (value is null)
            return "null";
        if (value is bool b)
            return b ? "true" : "false";
        if (value is string s)
            return $"\"{s}\"";
        if (value is char c)
            return $"'{c}'";
        if (value.GetType().IsEnum)
            return $"{FormatTypeName(value.GetType())}.{value}";

        return value.ToString() ?? "default";
    }

    #endregion

    #region Type Name Formatting

    private static string FormatTypeName(Type type)
    {
        // Handle by-ref types (ref, out, in)
        if (type.IsByRef)
            return FormatTypeName(type.GetElementType()!);

        // Handle pointer types
        if (type.IsPointer)
            return FormatTypeName(type.GetElementType()!) + "*";

        // Handle array types
        if (type.IsArray)
        {
            var elementType = FormatTypeName(type.GetElementType()!);
            var rank = type.GetArrayRank();
            var commas = rank > 1 ? new string(',', rank - 1) : "";
            return $"{elementType}[{commas}]";
        }

        // Handle Nullable<T>
        var underlyingNullable = Nullable.GetUnderlyingType(type);
        if (underlyingNullable is not null)
            return FormatTypeName(underlyingNullable) + "?";

        // Handle generic types
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var fullName = definition.FullName ?? definition.Name;

            // Remove the arity suffix (e.g., `2)
            var backtickIndex = fullName.IndexOf('`');
            if (backtickIndex > 0)
                fullName = fullName[..backtickIndex];

            // Use C# alias if available
            if (ClrToCSharpTypeNames.TryGetValue(fullName, out var alias))
                return alias;

            // Use simple name (without namespace) for the generic type
            var simpleName = fullName;
            var lastDot = simpleName.LastIndexOf('.');
            if (lastDot >= 0)
                simpleName = simpleName[(lastDot + 1)..];

            // Nested types use + in CLR names
            simpleName = simpleName.Replace('+', '.');

            var typeArgs = type.GetGenericArguments();
            var formattedArgs = string.Join(", ", typeArgs.Select(FormatTypeName));

            return $"{simpleName}<{formattedArgs}>";
        }

        // Handle generic type parameters (T, TKey, etc.)
        if (type.IsGenericParameter)
            return type.Name;

        // Check CLR-to-C# keyword map
        var typeFullName = type.FullName ?? type.Name;
        if (ClrToCSharpTypeNames.TryGetValue(typeFullName, out var csharpName))
            return csharpName;

        // Use simple name for types in common namespaces, full name otherwise
        var name = type.Name;

        // Handle nested types
        if (type.IsNested && type.DeclaringType is not null)
        {
            var declaringName = GetSimpleTypeName(type.DeclaringType);
            return $"{declaringName}.{name}";
        }

        return name;
    }

    private static string GetSimpleTypeName(Type type)
    {
        var name = type.Name;
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
            name = name[..backtickIndex];
        return name;
    }

    #endregion

    #region XML Doc Member ID Generation

    private static string GetMemberIdTypeName(Type type)
    {
        if (type.IsByRef)
            return GetMemberIdTypeName(type.GetElementType()!) + "@";

        if (type.IsArray)
        {
            var elementType = GetMemberIdTypeName(type.GetElementType()!);
            var rank = type.GetArrayRank();
            if (rank == 1)
                return $"{elementType}[]";
            return $"{elementType}[{new string(',', rank - 1)}]";
        }

        if (type.IsPointer)
            return GetMemberIdTypeName(type.GetElementType()!) + "*";

        if (type.IsGenericParameter)
        {
            // Type-level generic parameter: `N, method-level: ``N
            if (type.DeclaringMethod is not null)
                return "``" + type.GenericParameterPosition;
            return "`" + type.GenericParameterPosition;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var fullName = definition.FullName ?? definition.Name;
            var backtickIndex = fullName.IndexOf('`');
            if (backtickIndex > 0)
                fullName = fullName[..backtickIndex];

            var typeArgs = type.GetGenericArguments();
            var formattedArgs = string.Join(",", typeArgs.Select(GetMemberIdTypeName));
            return $"{fullName}{{{formattedArgs}}}";
        }

        return type.FullName ?? type.Name;
    }

    private static string GetConstructorMemberId(Type type, ConstructorInfo ctor)
    {
        var parameters = ctor.GetParameters();
        if (parameters.Length == 0)
            return $"M:{type.FullName}.#ctor";

        var paramTypes = string.Join(",", parameters.Select(p => GetMemberIdTypeName(p.ParameterType)));
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

        var parameters = method.GetParameters();
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
        var assemblyName = type.Assembly.GetName().Name ?? "";
        if (!xmlDocs.TryGetValue(assemblyName, out var docFile))
            return null;

        var memberId = $"T:{type.FullName}";
        return docFile.GetMemberDoc(memberId);
    }

    private static XmlDocBlock? LookupMemberDoc(Type type, string memberId, Dictionary<string, XmlDocFile> xmlDocs)
    {
        var assemblyName = type.Assembly.GetName().Name ?? "";
        if (!xmlDocs.TryGetValue(assemblyName, out var docFile))
            return null;

        return docFile.GetMemberDoc(memberId);
    }

    #endregion

    #region Parameter Building

    private static List<ApiParameter> BuildParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(p =>
        {
            var paramType = p.ParameterType;
            var isByRef = paramType.IsByRef;
            if (isByRef)
                paramType = paramType.GetElementType()!;

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
        var paramType = param.ParameterType;
        if (paramType.IsByRef)
            paramType = paramType.GetElementType()!;

        // Nullable value types
        if (Nullable.GetUnderlyingType(paramType) is not null)
            return true;

        // Check NullableAttribute for reference types
        // The attribute is compiler-generated with byte values: 0=oblivious, 1=not-nullable, 2=nullable
        var nullableAttr = param.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

        if (nullableAttr?.ConstructorArguments is [{ Value: byte value }])
            return value == 2;

        if (nullableAttr?.ConstructorArguments is [{ Value: IReadOnlyList<CustomAttributeTypedArgument> values }]
            && values.Count > 0 && values[0].Value is byte firstByte)
            return firstByte == 2;

        return false;
    }

    #endregion
}