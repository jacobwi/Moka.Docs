using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Diagnostics;
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
public sealed partial class ReflectionApiModelBuilder(
	XmlDocParser xmlDocParser,
	InheritDocResolver inheritDocResolver,
	ILogger<ReflectionApiModelBuilder> logger)
{
	/// <summary>The source of the diagnostics the builder reports.</summary>
	internal const string DiagnosticSource = "ReflectionApiModel";

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

	// Written by the compiler for language features, which the declaration shows in its own syntax or not at all.
	private static readonly HashSet<string> _compilerAttributes = new(StringComparer.Ordinal)
	{
		"CompilerGeneratedAttribute", "NullableAttribute", "NullableContextAttribute", "IsReadOnlyAttribute",
		"IsByRefLikeAttribute", "IsUnmanagedAttribute", "TupleElementNamesAttribute", "DynamicAttribute",
		"ExtensionAttribute", "ExtensionMarkerAttribute", "ParamArrayAttribute", "ParamCollectionAttribute",
		"AsyncStateMachineAttribute", "IteratorStateMachineAttribute", "AsyncIteratorStateMachineAttribute",
		"DebuggerStepThroughAttribute", "DebuggerHiddenAttribute", "RequiredMemberAttribute",
		"CompilerFeatureRequiredAttribute", "DecimalConstantAttribute", "FixedBufferAttribute", "DefaultMemberAttribute",
		"ScopedRefAttribute", "RequiresLocationAttribute", "NativeIntegerAttribute"
	};

	/// <summary>
	///     Builds an <see cref="ApiReference" /> from the given assemblies, with the default
	///     <see cref="MokaDocsOptions" /> for everything else.
	/// </summary>
	/// <param name="assemblies">The assemblies to scan.</param>
	/// <param name="includeXmlDocs">Whether to load and attach XML documentation.</param>
	/// <returns>A fully populated API reference model.</returns>
	public ApiReference Build(IReadOnlyList<Assembly> assemblies, bool includeXmlDocs) =>
		Build(new MokaDocsOptions { Assemblies = [.. assemblies], IncludeXmlDocs = includeXmlDocs },
			Directory.GetCurrentDirectory(), new DiagnosticBag());

	/// <summary>
	///     Builds an <see cref="ApiReference" /> from <see cref="MokaDocsOptions.Assemblies" />, applying
	///     the options for XML documentation, namespace filters and source.
	/// </summary>
	/// <param name="options">The options. Their assemblies are scanned.</param>
	/// <param name="contentRoot">The folder relative <see cref="MokaDocsOptions.XmlDocFiles" /> paths resolve from.</param>
	/// <param name="diagnostics">Receives warnings, such as an assembly without XML documentation.</param>
	/// <returns>A fully populated API reference model, without the types and members the options leave out.</returns>
	public ApiReference Build(MokaDocsOptions options, string contentRoot, DiagnosticBag diagnostics)
	{
		// The same assembly listed twice would give every type two pages on one route.
		List<Assembly> assemblies = options.Assemblies
			.DistinctBy(assembly => assembly.FullName ?? assembly.GetName().Name ?? "", StringComparer.Ordinal)
			.ToList();
		Dictionary<string, XmlDocFile> xmlDocs = options.IncludeXmlDocs
			? LoadXmlDocs(assemblies, options.XmlDocFiles, contentRoot, diagnostics)
			: new Dictionary<string, XmlDocFile>(StringComparer.Ordinal);
		var namespaceFilter = new NamespaceFilter(options.IncludeNamespaces, options.ExcludeNamespaces);

		var allTypes = new List<(Type Type, ApiType ApiType)>();

		foreach (Assembly assembly in assemblies)
		{
			string assemblyName = assembly.GetName().Name ?? assembly.FullName ?? "";
			AssemblySource? source = null;

			foreach (Type type in GetDocumentedTypes(assembly, assemblyName, diagnostics))
			{
				ApiType apiType;
				try
				{
					if (ShouldSkipType(type))
					{
						continue;
					}

					apiType = BuildApiType(type, assemblyName, xmlDocs, diagnostics);
				}
				catch (Exception ex) when (IsLoadFailure(ex))
				{
					ReportLeftOut(diagnostics, $"Type '{DisplayName(type)}' in assembly '{assemblyName}'", ex);
					continue;
				}

				// Excluded types are built only to pass their documentation on through <inheritdoc/>,
				// so their source isn't read.
				if ((options.SourceLinks || options.IncludeSourceCode) && namespaceFilter.IsIncluded(type.Namespace))
				{
					source ??= AssemblySource.Open(assembly, options, logger);
					apiType = apiType with
					{
						SourceUrl = options.SourceLinks ? source.Pdb?.GetSourceUrl(type) : null,
						SourceCode = options.IncludeSourceCode ? source.Declarations?.Render(type) : null
					};
				}

				allTypes.Add((type, apiType));
			}
		}

		foreach (string warning in namespaceFilter.CheckEntries(allTypes.Select(t => t.Type.Namespace)))
		{
			diagnostics.Warning(warning, DiagnosticSource);
		}

		IOrderedEnumerable<IGrouping<string, (Type Type, ApiType ApiType)>> namespaceGroups = allTypes
			.GroupBy(t => t.Type.Namespace ?? NamespaceFilter.GlobalNamespace)
			.OrderBy(g => g.Key, StringComparer.Ordinal);

		var namespaces = new List<ApiNamespace>();

		foreach (IGrouping<string, (Type Type, ApiType ApiType)> nsGroup in namespaceGroups)
		{
			var types = nsGroup
				.OrderBy(t => t.Type.Name, StringComparer.Ordinal)
				.Select(t => t.ApiType)
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

		// Resolved before the namespace filter applies, so a kept type still inherits documentation
		// from a base type in an excluded namespace. The filter then removes the excluded types
		// before navigation and pages are built from the model, so nothing links to them.
		reference = inheritDocResolver.Resolve(reference);
		reference = reference with
		{
			Namespaces = reference.Namespaces
				.Where(ns => namespaceFilter.IsIncluded(ns.Name))
				.ToList()
		};

		logger.LogInformation(
			"Built API reference from {AssemblyCount} assemblies: {TypeCount} types in {NamespaceCount} namespaces",
			assemblies.Count,
			reference.Namespaces.Sum(ns => ns.Types.Count),
			reference.Namespaces.Count);

		return reference;
	}

	/// <summary>
	///     The XML documentation of each assembly: the <see cref="MokaDocsOptions.XmlDocFiles" /> that
	///     name it, else the file next to its DLL.
	/// </summary>
	private Dictionary<string, XmlDocFile> LoadXmlDocs(
		List<Assembly> assemblies,
		List<string> xmlDocFiles,
		string contentRoot,
		DiagnosticBag diagnostics)
	{
		var xmlDocs = new Dictionary<string, XmlDocFile>(StringComparer.Ordinal);
		var assemblyNames = new HashSet<string>(
			assemblies.Select(a => a.GetName().Name ?? ""), StringComparer.Ordinal);

		foreach (string entry in xmlDocFiles)
		{
			if (string.IsNullOrWhiteSpace(entry))
			{
				continue;
			}

			string path = Path.GetFullPath(Path.Combine(contentRoot, entry));
			if (!File.Exists(path))
			{
				diagnostics.Warning($"XmlDocFiles entry '{entry}' does not exist: looked for {path}.", DiagnosticSource);
				continue;
			}

			XmlDocFile docFile = xmlDocParser.Parse(path);
			if (docFile.AssemblyName.Length == 0)
			{
				diagnostics.Warning(
					XmlReadFailure(path) is { } reason
						? $"XML documentation file {path} could not be read, so it matches no assembly: {reason}"
						: $"XML documentation file {path} has no <doc><assembly><name> element, so it matches no assembly.",
					DiagnosticSource);
				continue;
			}

			if (!assemblyNames.Contains(docFile.AssemblyName))
			{
				diagnostics.Warning(
					$"XML documentation file {path} documents assembly '{docFile.AssemblyName}', which is not in Assemblies, so it was not used.",
					DiagnosticSource);
				continue;
			}

			// Two files for one assembly add up; a member in both takes the later file's comment.
			if (xmlDocs.TryGetValue(docFile.AssemblyName, out XmlDocFile? earlier))
			{
				var members = new Dictionary<string, XmlDocBlock>(earlier.Members, StringComparer.Ordinal);
				foreach ((string id, XmlDocBlock block) in docFile.Members)
				{
					members[id] = block;
				}

				docFile = earlier with { Members = members };
			}

			xmlDocs[docFile.AssemblyName] = docFile;
		}

		var checkedNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (Assembly assembly in assemblies)
		{
			string name = assembly.GetName().Name ?? "";
			if (xmlDocs.ContainsKey(name) || !checkedNames.Add(name))
			{
				continue;
			}

			// Single-file publishing and assemblies loaded from bytes have no location.
			if (string.IsNullOrEmpty(assembly.Location))
			{
				diagnostics.Warning(
					$"No XML documentation for assembly '{name}': it was not loaded from a file, so there is no folder to look in. " +
					"Add its XML file to MokaDocsOptions.XmlDocFiles.",
					DiagnosticSource);
				continue;
			}

			string xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
			if (!File.Exists(xmlPath))
			{
				diagnostics.Warning(
					$"No XML documentation for assembly '{name}': looked for {xmlPath}. " +
					"Set <GenerateDocumentationFile>true</GenerateDocumentationFile> in its project, " +
					"or add its XML file to MokaDocsOptions.XmlDocFiles.",
					DiagnosticSource);
				continue;
			}

			XmlDocFile docFile = xmlDocParser.Parse(xmlPath);
			if (docFile.AssemblyName.Length == 0 && XmlReadFailure(xmlPath) is { } reason)
			{
				diagnostics.Warning(
					$"XML documentation file {xmlPath} of assembly '{name}' could not be read, so its API pages have no documentation: {reason}",
					DiagnosticSource);
				continue;
			}

			if (docFile.Members.Count > 0)
			{
				xmlDocs[name] = docFile;
			}
		}

		return xmlDocs;
	}

	// XmlDocParser only logs why a file failed, so a file that failed is read again for the reason.
	private static string? XmlReadFailure(string path)
	{
		try
		{
			var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
			using var reader = XmlReader.Create(path, settings);
			while (reader.Read())
			{
			}

			return null;
		}
		catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
		{
			return ex.Message;
		}
	}

	/// <summary>
	///     The exported types, plus the protected nested types of those a consumer can derive from,
	///     and everything visible inside those.
	/// </summary>
	private static List<Type> GetDocumentedTypes(Assembly assembly, string assemblyName, DiagnosticBag diagnostics)
	{
		Type[] exported;
		try
		{
			exported = assembly.GetExportedTypes();
		}
		catch (Exception ex) when (IsLoadFailure(ex))
		{
			// One type whose base type or interface comes from an assembly that isn't deployed fails
			// the call for the whole assembly, so the types are loaded one at a time instead.
			if (LoadTypesOneByOne(assembly, assemblyName, diagnostics) is { } loaded)
			{
				return loaded;
			}

			exported = LoadTypesThatLoad(assembly, assemblyName, diagnostics);
		}

		var types = new List<Type>(exported);
		foreach (Type type in exported)
		{
			// Exported types stop at public nesting. A protected nested type is part of the API a
			// derived class sees, like any other protected member.
			if (type.IsSealed)
			{
				continue;
			}

			foreach (Type nested in GetNestedTypes(type, BindingFlags.NonPublic, assemblyName, diagnostics))
			{
				if (nested.IsNestedFamily || nested.IsNestedFamORAssem)
				{
					types.Add(nested);
					AddVisibleNestedTypes(nested, types, assemblyName, diagnostics);
				}
			}
		}

		return types;
	}

	private static void AddVisibleNestedTypes(Type type, List<Type> types, string assemblyName, DiagnosticBag diagnostics)
	{
		foreach (Type nested in GetNestedTypes(type, BindingFlags.Public | BindingFlags.NonPublic, assemblyName, diagnostics))
		{
			if (nested.IsNestedPublic || (!type.IsSealed && (nested.IsNestedFamily || nested.IsNestedFamORAssem)))
			{
				types.Add(nested);
				AddVisibleNestedTypes(nested, types, assemblyName, diagnostics);
			}
		}
	}

	private static Type[] GetNestedTypes(Type type, BindingFlags visibility, string assemblyName, DiagnosticBag diagnostics)
	{
		try
		{
			return type.GetNestedTypes(visibility);
		}
		catch (Exception ex) when (IsLoadFailure(ex))
		{
			ReportLeftOut(diagnostics, $"The nested types of '{DisplayName(type)}' in assembly '{assemblyName}'", ex);
			return [];
		}
	}

	#region Missing Dependencies

	// The types GetDocumentedTypes finds by reflection, each loaded on its own and named from the
	// metadata when it fails. Null when the assembly has no file to read, as when it was loaded from bytes.
	private static List<Type>? LoadTypesOneByOne(Assembly assembly, string assemblyName, DiagnosticBag diagnostics)
	{
		if (string.IsNullOrEmpty(assembly.Location))
		{
			return null;
		}

		try
		{
			using FileStream stream = File.OpenRead(assembly.Location);
			using var peReader = new PEReader(stream);
			MetadataReader metadata = peReader.GetMetadataReader();

			var types = new List<Type>();
			var failed = new HashSet<TypeDefinitionHandle>();
			foreach (TypeDefinitionHandle handle in metadata.TypeDefinitions)
			{
				if (!IsDocumented(metadata, handle, failed))
				{
					continue;
				}

				try
				{
					types.Add(assembly.ManifestModule.ResolveType(MetadataTokens.GetToken(handle)));
				}
				catch (Exception ex) when (IsLoadFailure(ex))
				{
					failed.Add(handle);
					ReportLeftOut(diagnostics, $"Type '{MetadataTypeName(metadata, handle)}' in assembly '{assemblyName}'", ex);
				}
			}

			return types;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
		{
			return null;
		}
	}

	// Public types, and nested types a consumer sees: public ones, and protected ones in a type that can
	// be derived from. The types nested in one that failed to load are left out with it, without a
	// warning of their own.
	private static bool IsDocumented(MetadataReader metadata, TypeDefinitionHandle handle, HashSet<TypeDefinitionHandle> failed)
	{
		TypeDefinition definition = metadata.GetTypeDefinition(handle);
		TypeAttributes visibility = definition.Attributes & TypeAttributes.VisibilityMask;
		if (visibility == TypeAttributes.Public)
		{
			return true;
		}

		TypeDefinitionHandle declaring = definition.GetDeclaringType();
		if (declaring.IsNil || failed.Contains(declaring) || !IsDocumented(metadata, declaring, failed))
		{
			return false;
		}

		bool declaringIsSealed = (metadata.GetTypeDefinition(declaring).Attributes & TypeAttributes.Sealed) != 0;
		return visibility == TypeAttributes.NestedPublic
		       || (!declaringIsSealed && visibility is TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem);
	}

	private static Type[] LoadTypesThatLoad(Assembly assembly, string assemblyName, DiagnosticBag diagnostics)
	{
		try
		{
			return assembly.GetTypes().Where(type => type.IsVisible).ToArray();
		}
		catch (ReflectionTypeLoadException ex)
		{
			ReportLeftOut(diagnostics, $"Some types in assembly '{assemblyName}'", ex);
			return ex.Types.OfType<Type>().Where(type => type.IsVisible).ToArray();
		}
		catch (Exception ex) when (IsLoadFailure(ex))
		{
			ReportLeftOut(diagnostics, $"Assembly '{assemblyName}'", ex);
			return [];
		}
	}

	// MissingMemberException: the deployed version of an assembly lacks a member the metadata names.
	private static bool IsLoadFailure(Exception ex) =>
		ex is FileNotFoundException or FileLoadException or TypeLoadException or ReflectionTypeLoadException
			or MissingMemberException or BadImageFormatException;

	private static void ReportLeftOut(DiagnosticBag diagnostics, string subject, Exception ex) =>
		diagnostics.Warning($"{subject}: left out of the API reference. {DescribeLoadFailure(ex)}", DiagnosticSource);

	private static string DescribeLoadFailure(Exception ex)
	{
		if (ex is ReflectionTypeLoadException { LoaderExceptions: var loaderExceptions }
		    && loaderExceptions.FirstOrDefault(inner => inner is not null) is { } first)
		{
			return DescribeLoadFailure(first);
		}

		return ex switch
		{
			FileNotFoundException { FileName: { Length: > 0 } file } => $"It needs assembly '{file}', which could not be found.",
			FileLoadException { FileName: { Length: > 0 } file } => $"It needs assembly '{file}', which could not be loaded: {ex.Message}",
			_ => ex.Message
		};
	}

	private static string DisplayName(Type type)
	{
		try
		{
			string[] typeParameters = type.IsGenericTypeDefinition
				? type.GetGenericArguments().Select(t => t.Name).ToArray()
				: [];
			return QualifiedName(type.Namespace, DeclaringChain(type).Select(t => t.Name).ToList(), typeParameters);
		}
		catch (Exception ex) when (IsLoadFailure(ex))
		{
			return (type.FullName ?? type.Name).Replace('+', '.');
		}
	}

	private static string MetadataTypeName(MetadataReader metadata, TypeDefinitionHandle handle)
	{
		TypeDefinition definition = metadata.GetTypeDefinition(handle);
		string[] typeParameters = definition.GetGenericParameters()
			.Select(parameter => metadata.GetString(metadata.GetGenericParameter(parameter).Name))
			.ToArray();

		var levels = new List<string>();
		TypeDefinition current = definition;
		while (true)
		{
			levels.Add(metadata.GetString(current.Name));
			TypeDefinitionHandle declaring = current.GetDeclaringType();
			if (declaring.IsNil)
			{
				break;
			}

			current = metadata.GetTypeDefinition(declaring);
		}

		levels.Reverse();
		return QualifiedName(metadata.GetString(current.Namespace), levels, typeParameters);
	}

	// A nested type repeats the type parameters of the types containing it, outermost first, and each
	// level's metadata name says how many of them are its own.
	private static string QualifiedName(string? ns, IReadOnlyList<string> levels, IReadOnlyList<string> typeParameters)
	{
		var sb = new StringBuilder();
		if (!string.IsNullOrEmpty(ns))
		{
			sb.Append(ns).Append('.');
		}

		int consumed = 0;
		for (int i = 0; i < levels.Count; i++)
		{
			if (i > 0)
			{
				sb.Append('.');
			}

			(string name, int arity) = SplitArity(levels[i]);
			sb.Append(name);
			if (arity > 0 && consumed + arity <= typeParameters.Count)
			{
				sb.Append('<').Append(string.Join(", ", typeParameters.Skip(consumed).Take(arity))).Append('>');
				consumed += arity;
			}
		}

		return sb.ToString();
	}

	#endregion

	private static bool ShouldSkipType(Type type)
	{
		if (IsCompilerGenerated(type))
		{
			return true;
		}

		if (type.Name.Contains('<'))
		{
			return true;
		}

		// A type hidden from IntelliSense hides the types nested in it as well.
		for (Type? current = type; current is not null; current = current.DeclaringType)
		{
			if (IsHiddenFromEditors(current))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Whether the member is marked <c>[EditorBrowsable(EditorBrowsableState.Never)]</c>, the
	///     convention for API that is public only for tooling or compatibility.
	/// </summary>
	private static bool IsHiddenFromEditors(MemberInfo member) => IsHiddenFromEditors(member.GetCustomAttributesData());

	private static bool IsHiddenFromEditors(IList<CustomAttributeData> attributes) =>
		attributes.Any(a =>
			a.AttributeType.FullName == "System.ComponentModel.EditorBrowsableAttribute"
			&& a.ConstructorArguments is [{ Value: { } state }]
			&& Convert.ToInt32(state, CultureInfo.InvariantCulture) == (int)EditorBrowsableState.Never);

	private static ApiType BuildApiType(
		Type type,
		string assemblyName,
		Dictionary<string, XmlDocFile> xmlDocs,
		DiagnosticBag diagnostics)
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

		IList<CustomAttributeData> attributeData = type.GetCustomAttributesData();
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		string typeName = GetSimpleTypeName(type);
		string fullName = string.Join(".", DeclaringChain(type).Select(GetSimpleTypeName));
		if (type.Namespace is not null)
		{
			fullName = $"{type.Namespace}.{fullName}";
		}

		XmlDocBlock? documentation = LookupTypeDoc(type, xmlDocs);

		List<ApiMember> members = BuildMembers(type, kind, documentation, xmlDocs, diagnostics);

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
			Attributes = GetAttributes(attributeData),
			Namespace = type.Namespace,
			Assembly = assemblyName,
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
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
		// A record struct has no <Clone>$ or EqualityContract. Its == is always the compiler's: a
		// record can't declare one, and a plain struct's own == isn't marked compiler-generated.
		if (type.IsValueType)
		{
			MethodInfo? equality = type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
				[type, type]);
			return equality is not null && IsCompilerGenerated(equality);
		}

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

		// Qualified, so a base System.Exception isn't taken for a documented type called Exception.
		return FormatTypeName(baseType, qualified: true);
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
			.Select(i => FormatTypeName(i, qualified: true))
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

	private static List<ApiMember> BuildMembers(
		Type type,
		ApiTypeKind kind,
		XmlDocBlock? typeDocumentation,
		Dictionary<string, XmlDocFile> xmlDocs,
		DiagnosticBag diagnostics)
	{
		var members = new List<ApiMember>();

		if (kind == ApiTypeKind.Enum)
		{
			BuildEnumFields(type, members, xmlDocs, diagnostics);
			return members;
		}

		if (kind == ApiTypeKind.Delegate)
		{
			BuildDelegateInvoke(type, members, typeDocumentation, diagnostics);
			return members;
		}

		// Protected members are reachable only by deriving, so a sealed type (a struct or a
		// static class included) has none worth listing.
		bool includeProtected = !type.IsSealed;

		var extensionImplementations = new HashSet<string>(StringComparer.Ordinal);
		List<ApiMember> extensionMembers = BuildExtensionMembers(type, xmlDocs, extensionImplementations, diagnostics);

		BuildConstructors(type, members, xmlDocs, includeProtected, diagnostics);
		BuildProperties(type, members, xmlDocs, includeProtected, diagnostics);
		BuildMethods(type, members, xmlDocs, includeProtected, extensionImplementations, diagnostics);
		BuildFields(type, members, xmlDocs, includeProtected, diagnostics);
		BuildEvents(type, members, xmlDocs, includeProtected, diagnostics);
		members.AddRange(extensionMembers);

		return members;
	}

	// The Build methods check accessibility before anything that loads types, so a private member that
	// needs a missing assembly is skipped without a warning, as it would have been anyway.
	private static void AddMember(
		List<ApiMember> members,
		Type type,
		MemberInfo member,
		DiagnosticBag diagnostics,
		Func<ApiMember?> build)
	{
		try
		{
			if (build() is { } built)
			{
				members.Add(built);
			}
		}
		catch (Exception ex) when (IsLoadFailure(ex))
		{
			ReportLeftOut(diagnostics, $"Member '{MemberDisplayName(type, member)}'", ex);
		}
	}

	private static string MemberDisplayName(Type type, MemberInfo member) =>
		$"{DisplayName(type)}.{(member is ConstructorInfo ? GetSimpleTypeName(type) : member.Name)}";

	private static void BuildConstructors(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected,
		DiagnosticBag diagnostics)
	{
		foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
		{
			AddMember(members, type, ctor, diagnostics, () => BuildConstructor(type, ctor, xmlDocs, includeProtected));
		}
	}

	private static ApiMember? BuildConstructor(
		Type type,
		ConstructorInfo ctor,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		ApiAccessibility accessibility = GetAccessibility(ctor);
		if (!IsDocumented(accessibility, includeProtected))
		{
			return null;
		}

		IList<CustomAttributeData> attributeData = ctor.GetCustomAttributesData();
		if (IsCompilerGenerated(attributeData) || IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		ParameterInfo[] parameters = ctor.GetParameters();
		string name = GetSimpleTypeName(type);
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		return new ApiMember
		{
			Name = name,
			Kind = ApiMemberKind.Constructor,
			Signature = $"{AccessPrefix(type, accessibility)}{name}({FormatParameterList(parameters, false)})",
			Parameters = BuildParameters(parameters),
			Documentation = LookupMemberDoc(type, GetConstructorMemberId(type, ctor), xmlDocs),
			Accessibility = accessibility,
			Attributes = GetAttributes(attributeData),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	private static void BuildProperties(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected,
		DiagnosticBag diagnostics)
	{
		foreach (PropertyInfo prop in type.GetProperties(_declaredMembers))
		{
			AddMember(members, type, prop, diagnostics, () => BuildProperty(type, prop, xmlDocs, includeProtected));
		}
	}

	private static ApiMember? BuildProperty(
		Type type,
		PropertyInfo prop,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		MethodInfo? getter = prop.GetGetMethod(true);
		MethodInfo? setter = prop.GetSetMethod(true);
		MethodInfo? accessor = getter ?? setter;
		if (accessor is null)
		{
			return null;
		}

		// The property is as accessible as its most accessible accessor.
		ApiAccessibility accessibility = getter is not null && setter is not null
			? AccessibilityRank(GetAccessibility(getter)) >= AccessibilityRank(GetAccessibility(setter))
				? GetAccessibility(getter)
				: GetAccessibility(setter)
			: GetAccessibility(accessor);

		if (!IsDocumented(accessibility, includeProtected))
		{
			return null;
		}

		// A record's EqualityContract is compiler-generated and protected.
		IList<CustomAttributeData> attributeData = prop.GetCustomAttributesData();
		if (IsCompilerGenerated(attributeData) || IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		// GetIndexParameters copies the accessor's parameters with the property as their member, which
		// skips the accessor's nullable context.
		ParameterInfo[] indexParams = getter is not null ? getter.GetParameters() : setter!.GetParameters()[..^1];
		bool isIndexer = indexParams.Length > 0;

		string memberId = isIndexer
			? $"P:{GetDocIdName(type)}.{prop.Name}({string.Join(",", indexParams.Select(p => GetMemberIdTypeName(p.ParameterType)))})"
			: $"P:{GetDocIdName(type)}.{prop.Name}";

		string returnType = FormatTypeName(prop.PropertyType, NullableAnnotations.For(attributeData, type),
			ModifiedFor(prop.PropertyType, prop.GetModifiedPropertyType));

		var signature = new StringBuilder();
		signature.Append(AccessPrefix(type, accessibility));
		signature.Append(MethodModifiers(accessor));
		if (HasAttribute(attributeData, "System.Runtime.CompilerServices.RequiredMemberAttribute"))
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
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		return new ApiMember
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
			Attributes = GetAttributes(attributeData),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	private static void BuildMethods(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected,
		HashSet<string> extensionImplementations,
		DiagnosticBag diagnostics)
	{
		foreach (MethodInfo method in type.GetMethods(_declaredMembers))
		{
			AddMember(members, type, method, diagnostics, () =>
				// The compiler implements every extension block member with a public static method on the
				// static class, which would list each member a second time.
				method.IsPublic && extensionImplementations.Count > 0
				                && extensionImplementations.Contains(GetMethodMemberId(type, method))
					? null
					: BuildMethod(type, method, xmlDocs, includeProtected));
		}
	}

	private static ApiMember? BuildMethod(
		Type type,
		MethodInfo method,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		if (method.Name.StartsWith('<'))
		{
			return null;
		}

		// Property and event accessors have special names; so do operators, which used to be
		// skipped along with the accessors.
		bool isOperator = method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal);
		if (method.IsSpecialName && !isOperator)
		{
			return null;
		}

		ApiAccessibility accessibility = GetAccessibility(method);
		if (!IsDocumented(accessibility, includeProtected))
		{
			return null;
		}

		// Record members (<Clone>$, PrintMembers, Equals...) carry [CompilerGenerated].
		IList<CustomAttributeData> attributeData = method.GetCustomAttributesData();
		if (IsCompilerGenerated(attributeData) || IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		string returnType = FormatReturnType(method);
		string memberId = GetMethodMemberId(type, method);

		bool isOverride = IsOverride(method);
		bool isVirtual = method.IsVirtual && !method.IsFinal;
		bool isExtensionMethod = method.IsStatic
		                         && HasAttribute(attributeData, "System.Runtime.CompilerServices.ExtensionAttribute");
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		return new ApiMember
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
			Attributes = GetAttributes(attributeData),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	private static void BuildFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected,
		DiagnosticBag diagnostics)
	{
		foreach (FieldInfo field in type.GetFields(_declaredMembers))
		{
			AddMember(members, type, field, diagnostics, () => BuildField(type, field, xmlDocs, includeProtected));
		}
	}

	private static ApiMember? BuildField(
		Type type,
		FieldInfo field,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		// Skip backing fields
		if (field.Name.Contains('<') || field.Name.EndsWith("k__BackingField"))
		{
			return null;
		}

		ApiAccessibility accessibility = GetAccessibility(field);
		if (!IsDocumented(accessibility, includeProtected))
		{
			return null;
		}

		IList<CustomAttributeData> attributeData = field.GetCustomAttributesData();
		if (IsCompilerGenerated(attributeData) || IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		string returnType;
		string signature;
		if (FixedBuffer(attributeData) is { } buffer)
		{
			// The field's type is a struct the compiler generates; C# reads the field as a pointer.
			string elementType = FormatTypeName(buffer.ElementType);
			returnType = elementType + "*";
			signature = $"{AccessPrefix(type, accessibility)}fixed {elementType} {field.Name}[{buffer.Length.ToString(CultureInfo.InvariantCulture)}]";
		}
		else
		{
			returnType = FormatTypeName(field.FieldType, NullableAnnotations.For(attributeData, type),
				ModifiedFor(field.FieldType, field.GetModifiedFieldType));
			signature = BuildFieldSignature(type, field, accessibility, returnType, attributeData);
		}

		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		return new ApiMember
		{
			Name = field.Name,
			Kind = ApiMemberKind.Field,
			Signature = signature,
			ReturnType = returnType,
			Accessibility = accessibility,
			IsStatic = field.IsStatic,
			Documentation = LookupMemberDoc(type, $"F:{GetDocIdName(type)}.{field.Name}", xmlDocs),
			Attributes = GetAttributes(attributeData),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	private static void BuildEnumFields(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		DiagnosticBag diagnostics)
	{
		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			AddMember(members, type, field, diagnostics, () => BuildEnumField(type, field, xmlDocs));
		}
	}

	private static ApiMember? BuildEnumField(Type type, FieldInfo field, Dictionary<string, XmlDocFile> xmlDocs)
	{
		IList<CustomAttributeData> attributeData = field.GetCustomAttributesData();
		if (IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		string memberId = $"F:{GetDocIdName(type)}.{field.Name}";
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);

		object? rawValue = field.GetRawConstantValue();
		string signature = rawValue is not null
			? $"{field.Name} = {Convert.ToString(rawValue, CultureInfo.InvariantCulture)}"
			: field.Name;

		return new ApiMember
		{
			Name = field.Name,
			Kind = ApiMemberKind.Field,
			Signature = signature,
			ReturnType = type.Name,
			Accessibility = ApiAccessibility.Public,
			IsStatic = true,
			Documentation = LookupMemberDoc(type, memberId, xmlDocs),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	private static void BuildEvents(
		Type type,
		List<ApiMember> members,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected,
		DiagnosticBag diagnostics)
	{
		foreach (EventInfo evt in type.GetEvents(_declaredMembers))
		{
			AddMember(members, type, evt, diagnostics, () => BuildEvent(type, evt, xmlDocs, includeProtected));
		}
	}

	private static ApiMember? BuildEvent(
		Type type,
		EventInfo evt,
		Dictionary<string, XmlDocFile> xmlDocs,
		bool includeProtected)
	{
		MethodInfo? addMethod = evt.GetAddMethod(true);
		if (addMethod is null)
		{
			return null;
		}

		ApiAccessibility accessibility = GetAccessibility(addMethod);
		if (!IsDocumented(accessibility, includeProtected))
		{
			return null;
		}

		IList<CustomAttributeData> attributeData = evt.GetCustomAttributesData();
		if (IsHiddenFromEditors(attributeData))
		{
			return null;
		}

		string handlerType = evt.EventHandlerType is not null
			? FormatTypeName(evt.EventHandlerType, NullableAnnotations.For(attributeData, type))
			: "EventHandler";

		string signature =
			$"{AccessPrefix(type, accessibility)}{MethodModifiers(addMethod)}event {handlerType} {evt.Name}";
		string memberId = $"E:{GetDocIdName(type)}.{evt.Name}";
		(bool isObsolete, string? obsoleteMessage) = ReadObsolete(attributeData);
		bool isOverride = IsOverride(addMethod);

		return new ApiMember
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
			Attributes = GetAttributes(attributeData),
			IsObsolete = isObsolete,
			ObsoleteMessage = obsoleteMessage
		};
	}

	/// <summary>
	///     A delegate's one meaningful member: its <c>Invoke</c> method, which carries the
	///     parameters and return type. Delegates used to get no members, so their parameters
	///     appeared nowhere.
	/// </summary>
	private static void BuildDelegateInvoke(
		Type type,
		List<ApiMember> members,
		XmlDocBlock? typeDocumentation,
		DiagnosticBag diagnostics)
	{
		MethodInfo? invoke = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		if (invoke is null)
		{
			return;
		}

		AddMember(members, type, invoke, diagnostics, () =>
		{
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

			return new ApiMember
			{
				Name = "Invoke",
				Kind = ApiMemberKind.Method,
				Signature = signature.ToString(),
				ReturnType = FormatReturnType(invoke),
				Parameters = BuildParameters(invoke.GetParameters()),
				Accessibility = accessibility,
				// A delegate's doc comment sits on its declaration and describes Invoke's parameters;
				// there is nowhere to write a separate one. AssemblyAnalyzer shares it the same way.
				Documentation = typeDocumentation
			};
		});
	}

	#region Extension Blocks

	// What the C# 14 compiler emits for the extension blocks of a static class:
	//  - a public nested "grouping" type with a special name such as <G>$4B0A9C, marked [Extension], per
	//    receiver type. It holds a declaration of every block member, receiver left out, each marked
	//    [ExtensionMarker("<M>$...")]. A generic block's type parameters become $T0, $T1, ... on it.
	//  - a nested "marker" type per block inside it, whose static <Extension>$ method has the receiver as
	//    its only parameter and which carries the block's own type parameter names and constraints.
	//  - a public static method on the class itself implementing each member, receiver first for instance
	//    members, with the block's type parameters ahead of the member's own.
	// The XML documentation file documents the members under the grouping type and the block under the
	// marker type.
	private static List<ApiMember> BuildExtensionMembers(
		Type type,
		Dictionary<string, XmlDocFile> xmlDocs,
		HashSet<string> implementations,
		DiagnosticBag diagnostics)
	{
		if (!type.IsAbstract || !type.IsSealed)
		{
			return [];
		}

		var members = new List<(int Order, ApiMember Member)>();
		Dictionary<string, int>? declarationOrder = null;

		foreach (Type grouping in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (!grouping.IsSpecialName
			    || !HasAttribute(grouping.GetCustomAttributesData(), "System.Runtime.CompilerServices.ExtensionAttribute"))
			{
				continue;
			}

			var blocks = new Dictionary<string, ExtensionBlock?>(StringComparer.Ordinal);

			foreach (MethodInfo method in grouping.GetMethods(_declaredMembers))
			{
				try
				{
					if (!method.IsPublic || BlockOf(grouping, method, blocks, xmlDocs) is not { } block)
					{
						continue;
					}

					string implementation = ImplementationId(type, method, block);
					implementations.Add(implementation);

					// Accessors come back as null and are listed with their properties below.
					if (BuildMethod(grouping, method, xmlDocs, false) is { } member)
					{
						members.Add((Order(implementation), block.Attach(member)));
					}
				}
				catch (Exception ex) when (IsLoadFailure(ex))
				{
					ReportLeftOut(diagnostics, $"Member '{DisplayName(type)}.{method.Name}'", ex);
				}
			}

			foreach (PropertyInfo property in grouping.GetProperties(_declaredMembers))
			{
				try
				{
					if (BlockOf(grouping, property, blocks, xmlDocs) is { } block
					    && BuildProperty(grouping, property, xmlDocs, false) is { } member
					    && (property.GetGetMethod(true) ?? property.GetSetMethod(true)) is { } accessor)
					{
						members.Add((Order(ImplementationId(type, accessor, block)), block.Attach(member)));
					}
				}
				catch (Exception ex) when (IsLoadFailure(ex))
				{
					ReportLeftOut(diagnostics, $"Member '{DisplayName(type)}.{property.Name}'", ex);
				}
			}
		}

		// The implementation methods are in declaration order, which the grouping types lose: blocks
		// for one receiver type share a grouping type wherever they are declared.
		return members.OrderBy(m => m.Order).Select(m => m.Member).ToList();

		int Order(string implementation)
		{
			if (declarationOrder is null)
			{
				declarationOrder = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (MethodInfo method in type.GetMethods(_declaredMembers))
				{
					try
					{
						declarationOrder.TryAdd(GetMethodMemberId(type, method), method.MetadataToken);
					}
					catch (Exception ex) when (IsLoadFailure(ex))
					{
						// BuildMethods reports the method if it is documented; here it only loses its place.
					}
				}
			}

			return declarationOrder.GetValueOrDefault(implementation, int.MaxValue);
		}
	}

	private static ExtensionBlock? BlockOf(
		Type grouping,
		MemberInfo member,
		Dictionary<string, ExtensionBlock?> blocks,
		Dictionary<string, XmlDocFile> xmlDocs)
	{
		CustomAttributeData? marker = member.GetCustomAttributesData().FirstOrDefault(a =>
			a.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionMarkerAttribute");
		if (marker?.ConstructorArguments is not [{ Value: string markerName }])
		{
			return null;
		}

		if (!blocks.TryGetValue(markerName, out ExtensionBlock? block))
		{
			block = BuildExtensionBlock(grouping, markerName, xmlDocs);
			blocks[markerName] = block;
		}

		return block;
	}

	private static ExtensionBlock? BuildExtensionBlock(Type grouping, string markerName, Dictionary<string, XmlDocFile> xmlDocs)
	{
		Type? marker = grouping.GetNestedType(markerName, BindingFlags.Public | BindingFlags.NonPublic);
		MethodInfo? receiverMethod = marker?.GetMethod("<Extension>$",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
		if (marker is null || receiverMethod?.GetParameters() is not [{ } receiver])
		{
			return null;
		}

		List<ApiTypeParameter> typeParameters = marker.IsGenericTypeDefinition
			? marker.GetGenericArguments().Select(BuildTypeParameter).ToList()
			: [];

		// A block with only static members can leave the receiver's name out.
		string receiverName = string.IsNullOrEmpty(receiver.Name) ? "" : receiver.Name;

		var signature = new StringBuilder("extension");
		if (typeParameters.Count > 0)
		{
			signature.Append('<').Append(string.Join(", ", typeParameters.Select(tp => tp.Name))).Append('>');
		}

		signature.Append('(').Append(GetParameterModifier(receiver)).Append(FormatParameterType(receiver));
		if (receiverName.Length > 0)
		{
			signature.Append(' ').Append(receiverName);
		}

		signature.Append(')');
		foreach (ApiTypeParameter typeParameter in typeParameters.Where(tp => tp.Constraints.Count > 0))
		{
			signature.Append($" where {typeParameter.Name} : {string.Join(", ", typeParameter.Constraints)}");
		}

		string documentationId = $"T:{GetDocIdName(marker)}";
		var block = new ApiExtensionBlock
		{
			Signature = signature.ToString(),
			Receiver = BuildParameters([receiver])[0] with { Name = receiverName },
			TypeParameters = typeParameters,
			Documentation = LookupMemberDoc(marker, documentationId, xmlDocs),
			DocumentationId = documentationId
		};

		return new ExtensionBlock(block, typeParameters.Count,
			ToImplementationTypeParameters(GetMemberIdTypeName(receiver.ParameterType), typeParameters.Count));
	}

	// Grow(double) in extension(Shape shape) is implemented by M:Lib.ShapeExtensions.Grow(Lib.Shape,System.Double).
	private static string ImplementationId(Type type, MethodInfo member, ExtensionBlock block)
	{
		var parameters = new List<string>();
		if (!member.IsStatic)
		{
			parameters.Add(block.ReceiverId);
		}

		parameters.AddRange(member.GetParameters()
			.Select(p => ToImplementationTypeParameters(GetMemberIdTypeName(p.ParameterType), block.Arity)));

		int arity = block.Arity + (member.IsGenericMethodDefinition ? member.GetGenericArguments().Length : 0);
		string id = $"M:{GetDocIdName(type)}.{member.Name}{(arity > 0 ? $"``{arity}" : "")}";
		return parameters.Count > 0 ? $"{id}({string.Join(",", parameters)})" : id;
	}

	// The implementation method declares the block's type parameters ahead of the member's own, so the
	// block's `0 becomes ``0 and the member's own ``0 moves past the block's.
	private static string ToImplementationTypeParameters(string id, int blockArity) =>
		TypeParameterReferenceRegex().Replace(id, match =>
		{
			int position = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			return "``" + (match.Groups[1].Length == 2 ? blockArity + position : position);
		});

	[GeneratedRegex("(`{1,2})([0-9]+)")]
	private static partial Regex TypeParameterReferenceRegex();

	[GeneratedRegex(@"\$T([0-9]+)")]
	private static partial Regex GroupingTypeParameterRegex();

	private sealed record ExtensionBlock(ApiExtensionBlock Block, int Arity, string ReceiverId)
	{
		// A member built from the grouping type names the block's type parameters $T0, $T1, ...
		public ApiMember Attach(ApiMember member)
		{
			if (Arity == 0)
			{
				return member with { Extension = Block };
			}

			return member with
			{
				Extension = Block,
				Signature = Rename(member.Signature),
				ReturnType = member.ReturnType is null ? null : Rename(member.ReturnType),
				Parameters = member.Parameters.Select(p => p with { Type = Rename(p.Type) }).ToList(),
				TypeParameters = member.TypeParameters
					.Select(tp => tp with { Constraints = tp.Constraints.Select(Rename).ToList() })
					.ToList()
			};
		}

		private string Rename(string text) =>
			GroupingTypeParameterRegex().Replace(text, match =>
				int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int position)
				&& position < Block.TypeParameters.Count
					? Block.TypeParameters[position].Name
					: match.Value);
	}

	#endregion

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

	private static bool IsCompilerGenerated(IList<CustomAttributeData> attributes) =>
		HasAttribute(attributes, "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

	private static bool HasAttribute(IEnumerable<CustomAttributeData> attributes, string fullName) =>
		attributes.Any(a => a.AttributeType.FullName == fullName);

	#endregion

	#region Attribute Building

	// The compiler marks a ref struct, and a constructor of a type with required members, [Obsolete] so
	// that old compilers refuse them, and pairs that with [CompilerFeatureRequired]. It writes neither
	// when the source has its own [Obsolete], so an [Obsolete] next to [CompilerFeatureRequired] is
	// never the author's.
	private static (bool IsObsolete, string? Message) ReadObsolete(IList<CustomAttributeData> attributes)
	{
		CustomAttributeData? obsolete = attributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ObsoleteAttribute");
		if (obsolete is null || IsCompilerFeatureMarker(attributes))
		{
			return (false, null);
		}

		return (true, obsolete.ConstructorArguments is [{ Value: string message }, ..] ? message : null);
	}

	private static bool IsCompilerFeatureMarker(IList<CustomAttributeData> attributes) =>
		HasAttribute(attributes, "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute");

	private static (Type ElementType, int Length)? FixedBuffer(IList<CustomAttributeData> attributes)
	{
		CustomAttributeData? buffer = attributes.FirstOrDefault(a =>
			a.AttributeType.FullName == "System.Runtime.CompilerServices.FixedBufferAttribute");
		return buffer?.ConstructorArguments is [{ Value: Type elementType }, { Value: int length }]
			? (elementType, length)
			: null;
	}

	// The compiler emits a const decimal as a static readonly field with [DecimalConstant].
	private static decimal? DecimalConstant(FieldInfo field, IList<CustomAttributeData> attributes)
	{
		if (!field.IsStatic || !field.IsInitOnly || field.FieldType != typeof(decimal))
		{
			return null;
		}

		CustomAttributeData? constant = attributes.FirstOrDefault(a =>
			a.AttributeType.FullName == "System.Runtime.CompilerServices.DecimalConstantAttribute");
		if (constant?.ConstructorArguments is not [{ Value: byte scale }, { Value: byte sign }, { Value: { } hi }, { Value: { } mid }, { Value: { } lo }])
		{
			return null;
		}

		return new decimal(Bits(lo), Bits(mid), Bits(hi), sign != 0, scale);

		// The attribute has a uint constructor and an int one.
		static int Bits(object part) => part switch
		{
			uint u => unchecked((int)u),
			int i => i,
			_ => 0
		};
	}

	private static List<ApiAttribute> GetAttributes(IList<CustomAttributeData> attributeDataList)
	{
		var result = new List<ApiAttribute>();
		bool compilerFeatureMarker = IsCompilerFeatureMarker(attributeDataList);

		foreach (CustomAttributeData attr in attributeDataList)
		{
			string attrName = attr.AttributeType.Name;

			if (_compilerAttributes.Contains(attrName) || (compilerFeatureMarker && attrName == "ObsoleteAttribute"))
			{
				continue;
			}

			// Remove "Attribute" suffix for display
			if (attrName.EndsWith("Attribute"))
			{
				attrName = attrName[..^"Attribute".Length];
			}

			result.Add(new ApiAttribute
			{
				Name = attrName,
				Arguments = FormatAttributeArguments(attr)
			});
		}

		return result;
	}

	// Metadata stores the values of a params parameter as one array; the source lists them one by one.
	private static List<string> FormatAttributeArguments(CustomAttributeData attribute)
	{
		var arguments = new List<string>();
		IList<CustomAttributeTypedArgument> positional = attribute.ConstructorArguments;
		ParameterInfo[] parameters = attribute.Constructor.GetParameters();

		for (int i = 0; i < positional.Count; i++)
		{
			bool isParamsArray = i == positional.Count - 1 && i < parameters.Length && IsParams(parameters[i]);
			if (isParamsArray && positional[i].Value is IReadOnlyCollection<CustomAttributeTypedArgument> values)
			{
				arguments.AddRange(values.Select(FormatAttributeValue));
			}
			else
			{
				arguments.Add(FormatAttributeValue(positional[i]));
			}
		}

		foreach (CustomAttributeNamedArgument named in attribute.NamedArguments)
		{
			arguments.Add($"{named.MemberName} = {FormatAttributeValue(named.TypedValue)}");
		}

		return arguments;
	}

	private static string FormatAttributeValue(CustomAttributeTypedArgument argument)
	{
		switch (argument.Value)
		{
			case null:
				return "null";
			case IReadOnlyCollection<CustomAttributeTypedArgument> elements:
				string elementType = FormatTypeName(argument.ArgumentType.GetElementType() ?? typeof(object));
				return elements.Count == 0
					? $"new {elementType}[0]"
					: $"new {elementType}[] {{ {string.Join(", ", elements.Select(FormatAttributeValue))} }}";
			case Type type:
				return $"typeof({FormatTypeOfOperand(type)})";
			default:
				return FormatConstant(argument.Value, argument.ArgumentType);
		}
	}

	private static string FormatTypeOfOperand(Type type)
	{
		if (!type.IsGenericTypeDefinition)
		{
			return FormatTypeName(type);
		}

		return string.Join(".", DeclaringChain(type).Select(level =>
		{
			(string name, int arity) = SplitArity(level.Name);
			return arity > 0 ? $"{name}<{new string(',', arity - 1)}>" : name;
		}));
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

	private static string BuildFieldSignature(
		Type type,
		FieldInfo field,
		ApiAccessibility accessibility,
		string fieldType,
		IList<CustomAttributeData> attributes)
	{
		var sb = new StringBuilder();
		sb.Append(AccessPrefix(type, accessibility));

		if (field.IsLiteral)
		{
			sb.Append($"const {fieldType} {field.Name} = {FormatConstant(field.GetRawConstantValue(), field.FieldType)}");
			return sb.ToString();
		}

		if (DecimalConstant(field, attributes) is { } value)
		{
			sb.Append($"const {fieldType} {field.Name} = {value.ToString(CultureInfo.InvariantCulture)}");
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

		if (HasAttribute(attributes, "System.Runtime.CompilerServices.RequiredMemberAttribute"))
		{
			sb.Append("required ");
		}

		if (field.GetRequiredCustomModifiers().Any(m => m.FullName == "System.Runtime.CompilerServices.IsVolatile"))
		{
			sb.Append("volatile ");
		}

		// A ref field's type is a by-ref type, which FormatTypeName writes without the ref.
		if (field.FieldType.IsByRef)
		{
			sb.Append(HasAttribute(attributes, "System.Runtime.CompilerServices.IsReadOnlyAttribute") ? "ref readonly " : "ref ");
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
		string returnType = FormatReturnType(method);
		if (!method.ReturnType.IsByRef)
		{
			return returnType;
		}

		bool isReadOnly = HasAttribute(method.ReturnParameter.GetCustomAttributesData(),
			"System.Runtime.CompilerServices.IsReadOnlyAttribute");
		return (isReadOnly ? "ref readonly " : "ref ") + returnType;
	}

	private static string FormatReturnType(MethodInfo method) =>
		FormatTypeName(method.ReturnType, NullableAnnotations.For(method.ReturnParameter.GetCustomAttributesData(), method),
			ModifiedFor(method.ReturnType, method.ReturnParameter.GetModifiedParameterType));

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
			NullableAnnotations.For(param.GetCustomAttributesData(), param.Member),
			ModifiedFor(param.ParameterType, param.GetModifiedParameterType));

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
	/// <param name="type">The type.</param>
	/// <param name="nullable">The nullable annotations of the position the type is in.</param>
	/// <param name="modified">The modified type, which only function pointers need.</param>
	/// <param name="qualified">
	///     Whether every named type, type arguments included, starts with its namespace, as the CLI's model
	///     writes base types and interfaces: <c>System.Collections.Generic.IEnumerable&lt;Contoso.Order&gt;</c>.
	/// </param>
	private static string FormatTypeName(Type type, NullableAnnotations? nullable = null, Type? modified = null,
		bool qualified = false)
	{
		// A by-ref type (ref, out, in) shares its annotation position with the type it refers to.
		if (type.IsByRef)
		{
			return FormatTypeName(type.GetElementType()!, nullable, modified?.GetElementType(), qualified);
		}

		if (type.IsPointer)
		{
			return FormatTypeName(type.GetElementType()!, nullable, modified?.GetElementType(), qualified) + "*";
		}

		// Reflection doesn't call a function pointer a value type, so it would take the reference type path.
		if (type.IsFunctionPointer)
		{
			return FormatFunctionPointer(type, nullable, modified);
		}

		if (type.IsArray)
		{
			bool arrayAnnotated = nullable?.NextIsAnnotated() == true;
			string elementType = FormatTypeName(type.GetElementType()!, nullable, modified?.GetElementType(), qualified);
			int rank = type.GetArrayRank();
			string commas = rank > 1 ? new string(',', rank - 1) : "";
			return $"{elementType}[{commas}]{(arrayAnnotated ? "?" : "")}";
		}

		// Nullable<T> has no annotation position of its own.
		Type? underlyingNullable = Nullable.GetUnderlyingType(type);
		if (underlyingNullable is not null)
		{
			return FormatTypeName(underlyingNullable, nullable, qualified: qualified) + "?";
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

		string name = FormatNamedType(type, nullable, qualified);
		return annotated ? name + "?" : name;
	}

	private static string FormatNamedType(Type type, NullableAnnotations? nullable, bool qualified)
	{
		if (!type.IsGenericType)
		{
			if (type.FullName is not null && _clrToCSharpTypeNames.TryGetValue(type.FullName, out string? keyword))
			{
				return keyword;
			}

			List<Type> chain = DeclaringChain(type);
			return NamespacePrefix(chain[0], qualified) + string.Join(".", chain.Select(t => t.Name));
		}

		Type definition = type.GetGenericTypeDefinition();
		Type[] arguments = type.GetGenericArguments();

		if (definition.FullName is { } definitionName
		    && definitionName.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
		    && arguments.Length is >= 2 and <= 7)
		{
			return $"({string.Join(", ", arguments.Select(a => FormatTypeName(a, nullable, qualified: qualified)))})";
		}

		// Reflection flattens the arguments of containing types into the nested type's list,
		// outermost first. Each level takes as many as its arity. Nested generic types used to
		// lose everything after the first backtick, so Outer<T>.Inner displayed as Outer<T>.
		List<Type> levels = DeclaringChain(definition);
		var sb = new StringBuilder(NamespacePrefix(levels[0], qualified));
		int consumed = 0;
		for (int i = 0; i < levels.Count; i++)
		{
			if (i > 0)
			{
				sb.Append('.');
			}

			(string name, int arity) = SplitArity(levels[i].Name);
			sb.Append(name);

			if (arity > 0 && consumed + arity <= arguments.Length)
			{
				sb.Append('<');
				sb.Append(string.Join(", ",
					arguments.Skip(consumed).Take(arity).Select(a => FormatTypeName(a, nullable, qualified: qualified))));
				sb.Append('>');
				consumed += arity;
			}
		}

		return sb.ToString();
	}

	private static string NamespacePrefix(Type outermost, bool qualified) =>
		qualified && !string.IsNullOrEmpty(outermost.Namespace) ? outermost.Namespace + "." : "";

	// Calling conventions and the out and in of the parameters are custom modifiers, which only the
	// modified type has. The nullable annotations have a position for the function pointer itself,
	// then the return type's, then the parameters'.
	private static string FormatFunctionPointer(Type type, NullableAnnotations? nullable, Type? modified)
	{
		nullable?.NextIsAnnotated();

		Type returnType = type.GetFunctionPointerReturnType();
		Type? modifiedReturn = modified?.GetFunctionPointerReturnType();
		string formattedReturn = FunctionPointerRefKind(returnType, modifiedReturn, true)
		                         + FormatTypeName(returnType, nullable, modifiedReturn);

		Type[] parameters = type.GetFunctionPointerParameterTypes();
		Type[] modifiedParameters = modified?.GetFunctionPointerParameterTypes() ?? [];
		var parts = new List<string>(parameters.Length + 1);
		for (int i = 0; i < parameters.Length; i++)
		{
			Type? modifiedParameter = i < modifiedParameters.Length ? modifiedParameters[i] : null;
			parts.Add(FunctionPointerRefKind(parameters[i], modifiedParameter, false)
			          + FormatTypeName(parameters[i], nullable, modifiedParameter));
		}

		parts.Add(formattedReturn);

		string conventions = "";
		if (type.IsUnmanagedFunctionPointer)
		{
			const string prefix = "CallConv";
			string[] names = (modified?.GetFunctionPointerCallingConventions() ?? [])
				.Select(convention => convention.Name.StartsWith(prefix, StringComparison.Ordinal) ? convention.Name[prefix.Length..] : convention.Name)
				.ToArray();
			conventions = names.Length > 0 ? $" unmanaged[{string.Join(", ", names)}]" : " unmanaged";
		}

		return $"delegate*{conventions}<{string.Join(", ", parts)}>";
	}

	private static string FunctionPointerRefKind(Type type, Type? modified, bool isReturn)
	{
		if (!type.IsByRef)
		{
			return "";
		}

		Type[] modifiers = modified?.GetRequiredCustomModifiers() ?? [];
		if (modifiers.Any(m => m.FullName == "System.Runtime.InteropServices.OutAttribute"))
		{
			return "out ";
		}

		if (modifiers.Any(m => m.FullName == "System.Runtime.InteropServices.InAttribute"))
		{
			return isReturn ? "ref readonly " : "in ";
		}

		return (modified?.GetOptionalCustomModifiers() ?? []).Any(m => m.FullName == "System.Runtime.CompilerServices.RequiresLocationAttribute")
			? "ref readonly "
			: "ref ";
	}

	// Only function pointers need the modified type.
	private static Type? ModifiedFor(Type type, Func<Type> getModifiedType) =>
		ContainsFunctionPointer(type) ? getModifiedType() : null;

	private static bool ContainsFunctionPointer(Type type) =>
		type.IsFunctionPointer || (type.HasElementType && ContainsFunctionPointer(type.GetElementType()!));

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
				             || (!paramType.IsValueType && !paramType.IsPointer && !paramType.IsFunctionPointer
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
