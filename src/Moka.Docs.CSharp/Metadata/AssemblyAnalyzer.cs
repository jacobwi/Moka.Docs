using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Analyzes C# source files via Roslyn to extract a complete API model.
///     Uses <see cref="CSharpCompilation" /> to build a semantic model without requiring MSBuild.
/// </summary>
public sealed class AssemblyAnalyzer(ILogger<AssemblyAnalyzer> logger)
{
	/// <summary>
	///     Analyzes a project's C# source files with the settings from its project file: target
	///     framework symbols, implicit and explicit global usings, the nullable context, and the
	///     package and project references restore recorded.
	/// </summary>
	/// <param name="project">The project settings, from <see cref="CSharpProjectInfo.Load" />.</param>
	/// <param name="assemblyName">The assembly name (used for display).</param>
	/// <param name="includeInternals">Whether to include internal types and members.</param>
	/// <returns>The extracted API reference model.</returns>
	public ApiReference AnalyzeProject(CSharpProjectInfo project, string assemblyName, bool includeInternals = false)
	{
		List<string> csFiles = FindSourceFiles(project.ProjectDirectory);
		if (csFiles.Count == 0)
		{
			logger.LogWarning("No C# source files found in {Directory}", project.ProjectDirectory);
			return new ApiReference { Assemblies = [assemblyName] };
		}

		logger.LogInformation("Analyzing {Count} source files in {Directory} for {Framework}",
			csFiles.Count, project.ProjectDirectory, project.TargetFramework ?? "an unknown framework");

		// Without the project's symbols every #if block counts as inactive, so members behind
		// #if NET8_0_OR_GREATER silently disappeared from the docs.
		CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(project.PreprocessorSymbols);
		var syntaxTrees = csFiles
			.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, f))
			.ToList();

		// The SDK writes the global usings to a file under obj/, which the source search skips.
		if (project.GlobalUsings.Count > 0)
		{
			syntaxTrees.Add(CSharpSyntaxTree.ParseText(
				string.Join(Environment.NewLine, project.GlobalUsings), parseOptions, "GlobalUsings.g.cs"));
		}

		var compilation = CSharpCompilation.Create(
			assemblyName,
			syntaxTrees,
			CompilationReferences.Resolve(project.SharedFrameworks, project.ReferencePaths),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: project.NullableContext));

		return AnalyzeCompilation(compilation, assemblyName, includeInternals);
	}

	/// <summary>
	///     Analyzes all C# source files in a directory to build an API model. Unlike
	///     <see cref="AnalyzeProject" />, no project settings are read.
	/// </summary>
	/// <param name="sourceDirectory">The directory containing C# source files.</param>
	/// <param name="assemblyName">The assembly name (used for display).</param>
	/// <param name="includeInternals">Whether to include internal types.</param>
	/// <returns>The extracted API reference model.</returns>
	public ApiReference AnalyzeDirectory(string sourceDirectory, string assemblyName, bool includeInternals = false)
	{
		List<string> csFiles = FindSourceFiles(sourceDirectory);

		if (csFiles.Count == 0)
		{
			logger.LogWarning("No C# source files found in {Directory}", sourceDirectory);
			return new ApiReference { Assemblies = [assemblyName] };
		}

		logger.LogInformation("Analyzing {Count} source files in {Directory}", csFiles.Count, sourceDirectory);

		var syntaxTrees = csFiles
			.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f))
			.ToList();

		return AnalyzeSyntaxTrees(syntaxTrees, assemblyName, includeInternals);
	}

	/// <summary>
	///     Analyzes syntax trees to build an API model, compiled against the running runtime's
	///     shared framework.
	/// </summary>
	public ApiReference AnalyzeSyntaxTrees(
		IReadOnlyList<SyntaxTree> syntaxTrees,
		string assemblyName,
		bool includeInternals = false)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName,
			syntaxTrees,
			CompilationReferences.Resolve([CompilationReferences.CoreFramework], []),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		return AnalyzeCompilation(compilation, assemblyName, includeInternals);
	}

	/// <summary>
	///     Analyzes a Roslyn compilation to extract the API model.
	/// </summary>
	public ApiReference AnalyzeCompilation(
		CSharpCompilation compilation,
		string assemblyName,
		bool includeInternals = false)
	{
		// A partial type has a declaration per part but one symbol. Collecting declarations by
		// symbol gives it one ApiType; extracting per declaration gave every part its own
		// ApiType, and the build wrote one page per part to the same route.
		var declarations = new Dictionary<INamedTypeSymbol, List<MemberDeclarationSyntax>>(SymbolEqualityComparer.Default);
		var symbols = new List<INamedTypeSymbol>();

		foreach (SyntaxTree tree in compilation.SyntaxTrees)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(tree);

			foreach (MemberDeclarationSyntax declaration in tree.GetRoot().DescendantNodes()
				         .OfType<MemberDeclarationSyntax>()
				         .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
			{
				if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol
				    || !ShouldInclude(symbol, includeInternals))
				{
					continue;
				}

				if (!declarations.TryGetValue(symbol, out List<MemberDeclarationSyntax>? parts))
				{
					parts = [];
					declarations[symbol] = parts;
					symbols.Add(symbol);
				}

				parts.Add(declaration);
			}
		}

		var namespaceMap = new Dictionary<string, List<ApiType>>(StringComparer.Ordinal);

		foreach (INamedTypeSymbol symbol in symbols)
		{
			ApiType? apiType = symbol.TypeKind switch
			{
				TypeKind.Enum => ExtractEnumType(symbol),
				TypeKind.Delegate => ExtractDelegateType(symbol),
				_ => ExtractType(symbol, includeInternals)
			};

			if (apiType is null)
			{
				continue;
			}

			// The source panel gets the same members the page lists, so private fields and
			// helpers (and internals unless includeInternals) stay out of it.
			apiType = apiType with
			{
				SourceCode = DocumentedSource.Render(compilation, declarations[symbol],
					member => IsDocumented(member, includeInternals))
			};

			string ns = NamespaceOf(symbol) ?? "(global)";
			if (!namespaceMap.TryGetValue(ns, out List<ApiType>? types))
			{
				types = [];
				namespaceMap[ns] = types;
			}

			types.Add(apiType);
		}

		var namespaces = namespaceMap
			.OrderBy(kv => kv.Key, StringComparer.Ordinal)
			.Select(kv => new ApiNamespace
			{
				Name = kv.Key,
				Types = kv.Value.OrderBy(t => t.Name).ToList()
			})
			.ToList();

		logger.LogInformation("Extracted {TypeCount} types in {NsCount} namespaces from {Assembly}",
			namespaces.Sum(n => n.Types.Count), namespaces.Count, assemblyName);

		return new ApiReference
		{
			Assemblies = [assemblyName],
			Namespaces = namespaces
		};
	}

	#region Type Extraction

	private static ApiType? ExtractType(INamedTypeSymbol symbol, bool includeInternals)
	{
		ApiTypeKind? kind = symbol.TypeKind switch
		{
			TypeKind.Class => symbol.IsRecord ? ApiTypeKind.Record : ApiTypeKind.Class,
			TypeKind.Struct => symbol.IsRecord ? ApiTypeKind.Record : ApiTypeKind.Struct,
			TypeKind.Interface => ApiTypeKind.Interface,
			_ => null
		};

		if (kind is null)
		{
			return null;
		}

		return new ApiType
		{
			Name = symbol.Name,
			FullName = symbol.ToDisplayString(),
			Kind = kind.Value,
			Accessibility = MapAccessibility(symbol.DeclaredAccessibility),
			IsStatic = symbol.IsStatic,
			IsAbstract = symbol.IsAbstract && kind != ApiTypeKind.Interface,
			// Structs are implicitly sealed and Roslyn says so. Only a class can be declared sealed.
			IsSealed = symbol.IsSealed && symbol.TypeKind == TypeKind.Class,
			IsRecord = symbol.IsRecord,
			BaseType = symbol.BaseType?.ToDisplayString() is { } bt && bt != "object" ? bt : null,
			ImplementedInterfaces = symbol.Interfaces
				.Select(i => i.ToDisplayString())
				.OrderBy(i => i)
				.ToList(),
			TypeParameters = ExtractTypeParameters(symbol.TypeParameters),
			Members = ExtractMembers(symbol, includeInternals),
			Namespace = NamespaceOf(symbol),
			Assembly = symbol.ContainingAssembly?.Name,
			IsObsolete = HasAttribute(symbol, "ObsoleteAttribute"),
			ObsoleteMessage = GetObsoleteMessage(symbol),
			Attributes = ExtractAttributes(symbol),
			Documentation = ExtractXmlDoc(symbol)
		};
	}

	private static ApiType ExtractEnumType(INamedTypeSymbol symbol)
	{
		var members = symbol.GetMembers()
			.OfType<IFieldSymbol>()
			.Where(f => f.HasConstantValue)
			.Select(f => new ApiMember
			{
				Name = f.Name,
				Kind = ApiMemberKind.Field,
				Signature = $"{f.Name} = {f.ConstantValue}",
				IsStatic = true,
				Documentation = ExtractXmlDoc(f)
			})
			.ToList();

		return new ApiType
		{
			Name = symbol.Name,
			FullName = symbol.ToDisplayString(),
			Kind = ApiTypeKind.Enum,
			Accessibility = MapAccessibility(symbol.DeclaredAccessibility),
			Members = members,
			Namespace = NamespaceOf(symbol),
			Assembly = symbol.ContainingAssembly?.Name,
			IsObsolete = HasAttribute(symbol, "ObsoleteAttribute"),
			ObsoleteMessage = GetObsoleteMessage(symbol),
			Attributes = ExtractAttributes(symbol),
			Documentation = ExtractXmlDoc(symbol)
		};
	}

	private static ApiType ExtractDelegateType(INamedTypeSymbol symbol)
	{
		IMethodSymbol? invokeMethod = symbol.DelegateInvokeMethod;
		List<ApiParameter> parameters = invokeMethod?.Parameters
			.Select(ExtractParameter)
			.ToList() ?? [];

		// A delegate's doc comment sits on its declaration and there is nowhere to write a
		// separate one for the compiler-generated Invoke, so Invoke shares it. Without this the
		// Invoke row rendered blank and coverage reported a symbol no author can document.
		XmlDocBlock? documentation = ExtractXmlDoc(symbol);

		return new ApiType
		{
			Name = symbol.Name,
			FullName = symbol.ToDisplayString(),
			Kind = ApiTypeKind.Delegate,
			Accessibility = MapAccessibility(symbol.DeclaredAccessibility),
			TypeParameters = ExtractTypeParameters(symbol.TypeParameters),
			Namespace = NamespaceOf(symbol),
			Assembly = symbol.ContainingAssembly?.Name,
			Members =
			[
				new ApiMember
				{
					Name = "Invoke",
					Kind = ApiMemberKind.Method,
					Signature = MemberSignatures.ForDelegate(symbol),
					ReturnType = invokeMethod?.ReturnType.ToDisplayString(),
					Parameters = parameters,
					Documentation = documentation
				}
			],
			IsObsolete = HasAttribute(symbol, "ObsoleteAttribute"),
			Documentation = documentation
		};
	}

	#endregion

	#region Member Extraction

	private static List<ApiMember> ExtractMembers(INamedTypeSymbol typeSymbol, bool includeInternals)
	{
		var members = new List<ApiMember>();

		foreach (ISymbol member in typeSymbol.GetMembers())
		{
			// Skip compiler-generated members
			if (member.IsImplicitlyDeclared)
			{
				continue;
			}

			// Only private members used to be skipped, so internal and private protected
			// members of public types showed up in the public API reference.
			if (!IsDocumentedAccessibility(member.DeclaredAccessibility, includeInternals))
			{
				continue;
			}

			ApiMember? apiMember = member switch
			{
				IMethodSymbol method => ExtractMethod(method),
				IPropertySymbol property => ExtractProperty(property),
				IFieldSymbol field => ExtractField(field),
				IEventSymbol @event => ExtractEvent(@event),
				_ => null
			};

			if (apiMember is not null)
			{
				members.Add(apiMember);
			}
		}

		return members;
	}

	private static ApiMember? ExtractMethod(IMethodSymbol method)
	{
		// Skip property accessors, event accessors, etc.
		if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
		    or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
		{
			return null;
		}

		ApiMemberKind kind = method.MethodKind switch
		{
			MethodKind.Constructor or MethodKind.StaticConstructor => ApiMemberKind.Constructor,
			MethodKind.UserDefinedOperator or MethodKind.Conversion => ApiMemberKind.Operator,
			_ => ApiMemberKind.Method
		};

		string name = method.MethodKind switch
		{
			MethodKind.Constructor or MethodKind.StaticConstructor => method.ContainingType.Name,
			_ => method.Name
		};

		return new ApiMember
		{
			Name = name,
			Kind = kind,
			Signature = MemberSignatures.ForMember(method),
			ReturnType = method.ReturnType.ToDisplayString(),
			Accessibility = MapAccessibility(method.DeclaredAccessibility),
			IsStatic = method.IsStatic,
			IsVirtual = method.IsVirtual,
			IsAbstract = method.IsAbstract,
			IsOverride = method.IsOverride,
			IsSealed = method.IsSealed,
			IsExtensionMethod = method.IsExtensionMethod,
			Parameters = method.Parameters.Select(ExtractParameter).ToList(),
			TypeParameters = ExtractTypeParameters(method.TypeParameters),
			IsObsolete = HasAttribute(method, "ObsoleteAttribute"),
			ObsoleteMessage = GetObsoleteMessage(method),
			Attributes = ExtractAttributes(method),
			Documentation = ExtractXmlDoc(method)
		};
	}

	private static ApiMember ExtractProperty(IPropertySymbol property)
	{
		return new ApiMember
		{
			Name = property.Name,
			Kind = property.IsIndexer ? ApiMemberKind.Indexer : ApiMemberKind.Property,
			Signature = MemberSignatures.ForMember(property),
			ReturnType = property.Type.ToDisplayString(),
			Accessibility = MapAccessibility(property.DeclaredAccessibility),
			IsStatic = property.IsStatic,
			IsVirtual = property.IsVirtual,
			IsAbstract = property.IsAbstract,
			IsOverride = property.IsOverride,
			IsSealed = property.IsSealed,
			Parameters = property.IsIndexer
				? property.Parameters.Select(ExtractParameter).ToList()
				: [],
			IsObsolete = HasAttribute(property, "ObsoleteAttribute"),
			ObsoleteMessage = GetObsoleteMessage(property),
			Attributes = ExtractAttributes(property),
			Documentation = ExtractXmlDoc(property)
		};
	}

	private static ApiMember? ExtractField(IFieldSymbol field)
	{
		// Skip backing fields
		if (field.AssociatedSymbol is not null)
		{
			return null;
		}

		return new ApiMember
		{
			Name = field.Name,
			Kind = ApiMemberKind.Field,
			Signature = MemberSignatures.ForMember(field),
			ReturnType = field.Type.ToDisplayString(),
			Accessibility = MapAccessibility(field.DeclaredAccessibility),
			IsStatic = field.IsStatic,
			IsObsolete = HasAttribute(field, "ObsoleteAttribute"),
			Attributes = ExtractAttributes(field),
			Documentation = ExtractXmlDoc(field)
		};
	}

	private static ApiMember ExtractEvent(IEventSymbol @event)
	{
		return new ApiMember
		{
			Name = @event.Name,
			Kind = ApiMemberKind.Event,
			Signature = MemberSignatures.ForMember(@event),
			ReturnType = @event.Type.ToDisplayString(),
			Accessibility = MapAccessibility(@event.DeclaredAccessibility),
			IsStatic = @event.IsStatic,
			IsVirtual = @event.IsVirtual,
			IsAbstract = @event.IsAbstract,
			IsOverride = @event.IsOverride,
			IsSealed = @event.IsSealed,
			IsObsolete = HasAttribute(@event, "ObsoleteAttribute"),
			Attributes = ExtractAttributes(@event),
			Documentation = ExtractXmlDoc(@event)
		};
	}

	#endregion

	#region Helpers

	private static ApiParameter ExtractParameter(IParameterSymbol param)
	{
		return new ApiParameter
		{
			Name = param.Name,
			// Short names, as in the signature. Once types from other assemblies resolved, the
			// fully qualified "IReadOnlyList<Moka.Docs.Core.Api.ApiType>" reached the page
			// heading, which keeps only the text after the last dot: "ApiType>".
			Type = MemberSignatures.ForType(param.Type),
			HasDefaultValue = param.HasExplicitDefaultValue,
			DefaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null,
			IsParams = param.IsParams,
			IsRef = param.RefKind == RefKind.Ref,
			IsOut = param.RefKind == RefKind.Out,
			IsIn = param.RefKind == RefKind.In,
			IsNullable = param.NullableAnnotation == NullableAnnotation.Annotated
		};
	}

	private static List<ApiTypeParameter> ExtractTypeParameters(
		ImmutableArray<ITypeParameterSymbol> typeParams)
	{
		return typeParams.Select(tp => new ApiTypeParameter
		{
			Name = tp.Name,
			Constraints = GetConstraints(tp)
		}).ToList();
	}

	private static List<string> GetConstraints(ITypeParameterSymbol tp)
	{
		var constraints = new List<string>();

		if (tp.HasReferenceTypeConstraint)
		{
			constraints.Add("class");
		}

		if (tp.HasValueTypeConstraint)
		{
			constraints.Add("struct");
		}

		if (tp.HasNotNullConstraint)
		{
			constraints.Add("notnull");
		}

		if (tp.HasUnmanagedTypeConstraint)
		{
			constraints.Add("unmanaged");
		}

		foreach (ITypeSymbol c in tp.ConstraintTypes)
		{
			constraints.Add(c.ToDisplayString());
		}

		if (tp.HasConstructorConstraint)
		{
			constraints.Add("new()");
		}

		return constraints;
	}

	private static List<ApiAttribute> ExtractAttributes(ISymbol symbol)
	{
		return symbol.GetAttributes()
			.Where(a => a.AttributeClass is not null)
			.Where(a => !IsCompilerAttribute(a.AttributeClass!.Name))
			.Select(a => new ApiAttribute
			{
				Name = AttributeDisplayName(a.AttributeClass!.Name),
				// Written as C# source. Value.ToString() dropped the quotes around strings and
				// printed enum arguments as their numbers.
				Arguments = a.ConstructorArguments
					.Select(arg => arg.ToCSharpString())
					.ToList()
			})
			.ToList();
	}

	/// <summary>
	///     The attribute name without its <c>Attribute</c> suffix. Removing every occurrence, as
	///     <c>Replace("Attribute", "")</c> did, turned <c>AttributeUsageAttribute</c> into <c>Usage</c>.
	/// </summary>
	private static string AttributeDisplayName(string name)
	{
		const string suffix = "Attribute";
		return name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)
			? name[..^suffix.Length]
			: name;
	}

	private static XmlDocBlock? ExtractXmlDoc(ISymbol symbol)
	{
		string? xml = symbol.GetDocumentationCommentXml();
		if (string.IsNullOrWhiteSpace(xml))
		{
			return null;
		}

		try
		{
			var doc = XDocument.Parse(xml);
			XElement? root = doc.Root;
			if (root is null)
			{
				return null;
			}

			return new XmlDocBlock
			{
				Summary = XmlDocParser.RenderInnerXml(root.Element("summary")),
				Remarks = XmlDocParser.RenderInnerXml(root.Element("remarks")),
				Returns = XmlDocParser.RenderInnerXml(root.Element("returns")),
				Value = XmlDocParser.RenderInnerXml(root.Element("value")),
				Parameters = ParseDocParams(root, "param"),
				TypeParameters = ParseDocParams(root, "typeparam"),
				Exceptions = root.Elements("exception").Select(e => new ExceptionDoc
				{
					Type = StripDocIdPrefix(e.Attribute("cref")?.Value ?? ""),
					Description = XmlDocParser.RenderInnerXml(e)
				}).ToList(),
				Examples = root.Elements("example")
					.Select(e => XmlDocParser.RenderInnerXml(e))
					.Where(s => !string.IsNullOrWhiteSpace(s))
					.ToList(),
				// The parser's list keeps href URLs and link text; this one used to keep only the cref
				// or the text, so an href entry lost its URL in CLI builds.
				SeeAlso = XmlDocParser.ParseSeeAlso(root),
				HasInheritDocTag = root.Element("inheritdoc") is not null,
				// Roslyn has already bound the cref to a documentation ID ("M:Ns.Type.Method"),
				// or to "!:..." when it could not resolve it.
				InheritDocCref = root.Element("inheritdoc")?.Attribute("cref")?.Value
			};
		}
		catch
		{
			return null;
		}
	}

	private static Dictionary<string, string> ParseDocParams(
		XElement root, string elementName)
	{
		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (XElement p in root.Elements(elementName))
		{
			string? name = p.Attribute("name")?.Value;
			if (!string.IsNullOrEmpty(name))
			{
				result[name] = XmlDocParser.RenderInnerXml(p);
			}
		}

		return result;
	}

	/// <summary>
	///     Whether a type belongs in the reference. Containing types count too: a public class
	///     nested in an internal one is not part of the public API.
	/// </summary>
	private static bool ShouldInclude(INamedTypeSymbol symbol, bool includeInternals)
	{
		for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
		{
			if (!IsDocumentedAccessibility(current.DeclaredAccessibility, includeInternals))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	///     Whether a type or member belongs in the reference, by the same rules as the page and member lists.
	/// </summary>
	private static bool IsDocumented(ISymbol symbol, bool includeInternals) =>
		symbol is INamedTypeSymbol type
			? ShouldInclude(type, includeInternals)
			: IsDocumentedAccessibility(symbol.DeclaredAccessibility, includeInternals);

	private static List<string> FindSourceFiles(string directory)
	{
		// Sorted so the parts of a partial type, and anything else that follows file order,
		// come out the same on every machine.
		return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
			.OrderBy(f => f, StringComparer.Ordinal)
			.ToList();
	}

	/// <summary>
	///     The containing namespace, or null for the global namespace. Roslyn displays that one as
	///     <c>&lt;global namespace&gt;</c>, which ended up in page routes and failed the build on
	///     Windows, where <c>&lt;</c> and <c>&gt;</c> can't appear in a path.
	/// </summary>
	private static string? NamespaceOf(ISymbol symbol) =>
		symbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null;

	/// <summary>
	///     Public and protected symbols are visible to library consumers (protected ones through
	///     inheritance). Internal and private protected ones only with <c>includeInternals</c>.
	/// </summary>
	private static bool IsDocumentedAccessibility(Accessibility accessibility, bool includeInternals) =>
		accessibility switch
		{
			Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal => true,
			Accessibility.Internal or Accessibility.ProtectedAndInternal => includeInternals,
			_ => false
		};

	/// <summary>
	///     Removes a documentation ID prefix: <c>T:</c> for a resolved type, <c>!:</c> for a name
	///     the compiler could not resolve.
	/// </summary>
	/// <remarks>
	///     This used <c>TrimStart('T', ':')</c>, which also removed the first letter of any type
	///     name starting with T, so <c>T:TimeoutException</c> became "imeoutException".
	/// </remarks>
	private static string StripDocIdPrefix(string cref) =>
		cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;

	private static ApiAccessibility MapAccessibility(Accessibility a)
	{
		return a switch
		{
			Accessibility.Public => ApiAccessibility.Public,
			Accessibility.Protected => ApiAccessibility.Protected,
			Accessibility.Internal => ApiAccessibility.Internal,
			Accessibility.ProtectedOrInternal => ApiAccessibility.ProtectedInternal,
			Accessibility.ProtectedAndInternal => ApiAccessibility.PrivateProtected,
			Accessibility.Private => ApiAccessibility.Private,
			_ => ApiAccessibility.Public
		};
	}

	private static bool HasAttribute(ISymbol symbol, string attributeName) =>
		symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);

	private static string? GetObsoleteMessage(ISymbol symbol)
	{
		return symbol.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == "ObsoleteAttribute")
			?.ConstructorArguments.FirstOrDefault().Value?.ToString();
	}

	private static bool IsCompilerAttribute(string name)
	{
		return name is "CompilerGeneratedAttribute" or "NullableAttribute" or "NullableContextAttribute"
			or "AsyncStateMachineAttribute" or "DebuggerStepThroughAttribute"
			or "IteratorStateMachineAttribute" or "IsReadOnlyAttribute"
			or "ParamArrayAttribute" or "TupleElementNamesAttribute"
			or "DynamicAttribute" or "IsUnmanagedAttribute"
			or "ExtensionAttribute";
	}

	#endregion
}
