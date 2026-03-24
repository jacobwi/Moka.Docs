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
    ///     Analyzes all C# source files in a directory to build an API model.
    /// </summary>
    /// <param name="sourceDirectory">The directory containing C# source files.</param>
    /// <param name="assemblyName">The assembly name (used for display).</param>
    /// <param name="includeInternals">Whether to include internal types.</param>
    /// <returns>The extracted API reference model.</returns>
    public ApiReference AnalyzeDirectory(string sourceDirectory, string assemblyName, bool includeInternals = false)
    {
        var csFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

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
    ///     Analyzes syntax trees to build an API model.
    /// </summary>
    public ApiReference AnalyzeSyntaxTrees(
        IReadOnlyList<SyntaxTree> syntaxTrees,
        string assemblyName,
        bool includeInternals = false)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        // Add runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var runtimeRefs = new[] { "System.Runtime.dll", "System.Collections.dll", "netstandard.dll" }
            .Select(f => Path.Combine(runtimeDir, f))
            .Where(File.Exists)
            .Select(f => MetadataReference.CreateFromFile(f));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references.Concat(runtimeRefs),
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
        var namespaceMap = new Dictionary<string, List<ApiType>>(StringComparer.Ordinal);

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(typeDecl);
                if (symbol is null) continue;

                if (!ShouldInclude(symbol, includeInternals)) continue;

                var apiType = ExtractType(symbol);
                if (apiType is null) continue;

                // Capture the source code from the syntax node
                apiType = apiType with { SourceCode = typeDecl.NormalizeWhitespace().ToFullString() };

                var ns = symbol.ContainingNamespace?.ToDisplayString() ?? "(global)";
                if (!namespaceMap.TryGetValue(ns, out var types))
                {
                    types = [];
                    namespaceMap[ns] = types;
                }

                types.Add(apiType);
            }

            // Also extract top-level enum declarations
            foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(enumDecl);
                if (symbol is null) continue;

                if (!ShouldInclude(symbol, includeInternals)) continue;

                var apiType = ExtractEnumType(symbol);

                // Capture the source code from the syntax node
                apiType = apiType with { SourceCode = enumDecl.NormalizeWhitespace().ToFullString() };

                var ns = symbol.ContainingNamespace?.ToDisplayString() ?? "(global)";
                if (!namespaceMap.TryGetValue(ns, out var types))
                {
                    types = [];
                    namespaceMap[ns] = types;
                }

                types.Add(apiType);
            }

            // Delegate declarations
            foreach (var delegateDecl in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(delegateDecl);
                if (symbol is null) continue;

                if (!ShouldInclude(symbol, includeInternals)) continue;

                var apiType = ExtractDelegateType(symbol);

                // Capture the source code from the syntax node
                apiType = apiType with { SourceCode = delegateDecl.NormalizeWhitespace().ToFullString() };

                var ns = symbol.ContainingNamespace?.ToDisplayString() ?? "(global)";
                if (!namespaceMap.TryGetValue(ns, out var types))
                {
                    types = [];
                    namespaceMap[ns] = types;
                }

                types.Add(apiType);
            }
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

    private static ApiType? ExtractType(INamedTypeSymbol symbol)
    {
        var kind = symbol.TypeKind switch
        {
            TypeKind.Class => symbol.IsRecord ? ApiTypeKind.Record : ApiTypeKind.Class,
            TypeKind.Struct => symbol.IsRecord ? ApiTypeKind.Record : ApiTypeKind.Struct,
            TypeKind.Interface => ApiTypeKind.Interface,
            _ => (ApiTypeKind?)null
        };

        if (kind is null) return null;

        return new ApiType
        {
            Name = symbol.Name,
            FullName = symbol.ToDisplayString(),
            Kind = kind.Value,
            Accessibility = MapAccessibility(symbol.DeclaredAccessibility),
            IsStatic = symbol.IsStatic,
            IsAbstract = symbol.IsAbstract && kind != ApiTypeKind.Interface,
            IsSealed = symbol.IsSealed,
            IsRecord = symbol.IsRecord,
            BaseType = symbol.BaseType?.ToDisplayString() is { } bt && bt != "object" ? bt : null,
            ImplementedInterfaces = symbol.Interfaces
                .Select(i => i.ToDisplayString())
                .OrderBy(i => i)
                .ToList(),
            TypeParameters = ExtractTypeParameters(symbol.TypeParameters),
            Members = ExtractMembers(symbol),
            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
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
            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
            Assembly = symbol.ContainingAssembly?.Name,
            IsObsolete = HasAttribute(symbol, "ObsoleteAttribute"),
            ObsoleteMessage = GetObsoleteMessage(symbol),
            Attributes = ExtractAttributes(symbol),
            Documentation = ExtractXmlDoc(symbol)
        };
    }

    private static ApiType ExtractDelegateType(INamedTypeSymbol symbol)
    {
        var invokeMethod = symbol.DelegateInvokeMethod;
        var parameters = invokeMethod?.Parameters
            .Select(ExtractParameter)
            .ToList() ?? [];

        return new ApiType
        {
            Name = symbol.Name,
            FullName = symbol.ToDisplayString(),
            Kind = ApiTypeKind.Delegate,
            Accessibility = MapAccessibility(symbol.DeclaredAccessibility),
            TypeParameters = ExtractTypeParameters(symbol.TypeParameters),
            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
            Assembly = symbol.ContainingAssembly?.Name,
            Members =
            [
                new ApiMember
                {
                    Name = "Invoke",
                    Kind = ApiMemberKind.Method,
                    Signature = symbol.ToDisplayString(),
                    ReturnType = invokeMethod?.ReturnType.ToDisplayString(),
                    Parameters = parameters
                }
            ],
            IsObsolete = HasAttribute(symbol, "ObsoleteAttribute"),
            Documentation = ExtractXmlDoc(symbol)
        };
    }

    #endregion

    #region Member Extraction

    private static List<ApiMember> ExtractMembers(INamedTypeSymbol typeSymbol)
    {
        var members = new List<ApiMember>();

        foreach (var member in typeSymbol.GetMembers())
        {
            // Skip compiler-generated members
            if (member.IsImplicitlyDeclared) continue;

            // Skip private members
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            var apiMember = member switch
            {
                IMethodSymbol method => ExtractMethod(method),
                IPropertySymbol property => ExtractProperty(property),
                IFieldSymbol field => ExtractField(field),
                IEventSymbol @event => ExtractEvent(@event),
                _ => null
            };

            if (apiMember is not null)
                members.Add(apiMember);
        }

        return members;
    }

    private static ApiMember? ExtractMethod(IMethodSymbol method)
    {
        // Skip property accessors, event accessors, etc.
        if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
            or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
            return null;

        var kind = method.MethodKind switch
        {
            MethodKind.Constructor or MethodKind.StaticConstructor => ApiMemberKind.Constructor,
            MethodKind.UserDefinedOperator or MethodKind.Conversion => ApiMemberKind.Operator,
            _ => ApiMemberKind.Method
        };

        var name = method.MethodKind switch
        {
            MethodKind.Constructor or MethodKind.StaticConstructor => method.ContainingType.Name,
            _ => method.Name
        };

        return new ApiMember
        {
            Name = name,
            Kind = kind,
            Signature = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
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
        var signature = property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Add get/set/init accessors info
        var accessors = new List<string>();
        if (property.GetMethod is not null) accessors.Add("get");
        if (property.SetMethod is not null)
            accessors.Add(property.SetMethod.IsInitOnly ? "init" : "set");
        if (accessors.Count > 0)
            signature += $" {{ {string.Join("; ", accessors)}; }}";

        return new ApiMember
        {
            Name = property.Name,
            Kind = property.IsIndexer ? ApiMemberKind.Indexer : ApiMemberKind.Property,
            Signature = signature,
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
        if (field.AssociatedSymbol is not null) return null;

        return new ApiMember
        {
            Name = field.Name,
            Kind = ApiMemberKind.Field,
            Signature = $"{field.Type.ToDisplayString()} {field.Name}",
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
            Signature = $"event {@event.Type.ToDisplayString()} {@event.Name}",
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
            Type = param.Type.ToDisplayString(),
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

        if (tp.HasReferenceTypeConstraint) constraints.Add("class");
        if (tp.HasValueTypeConstraint) constraints.Add("struct");
        if (tp.HasNotNullConstraint) constraints.Add("notnull");
        if (tp.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");

        foreach (var c in tp.ConstraintTypes) constraints.Add(c.ToDisplayString());

        if (tp.HasConstructorConstraint) constraints.Add("new()");

        return constraints;
    }

    private static List<ApiAttribute> ExtractAttributes(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(a => a.AttributeClass is not null)
            .Where(a => !IsCompilerAttribute(a.AttributeClass!.Name))
            .Select(a => new ApiAttribute
            {
                Name = a.AttributeClass!.Name.Replace("Attribute", ""),
                Arguments = a.ConstructorArguments
                    .Select(arg => arg.Value?.ToString() ?? "")
                    .ToList()
            })
            .ToList();
    }

    private static XmlDocBlock? ExtractXmlDoc(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null) return null;

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
                    Type = (e.Attribute("cref")?.Value ?? "").TrimStart('T', ':'),
                    Description = XmlDocParser.RenderInnerXml(e)
                }).ToList(),
                Examples = root.Elements("example")
                    .Select(e => XmlDocParser.RenderInnerXml(e))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList(),
                SeeAlso = root.Elements("seealso")
                    .Select(e => e.Attribute("cref")?.Value ?? e.Value)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList()
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
        foreach (var p in root.Elements(elementName))
        {
            var name = p.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
                result[name] = XmlDocParser.RenderInnerXml(p);
        }

        return result;
    }

    private static bool ShouldInclude(INamedTypeSymbol symbol, bool includeInternals)
    {
        if (symbol.DeclaredAccessibility == Accessibility.Public) return true;
        if (includeInternals && symbol.DeclaredAccessibility == Accessibility.Internal) return true;
        if (symbol.DeclaredAccessibility == Accessibility.Protected) return true;
        if (symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal) return true;
        return false;
    }

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

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);
    }

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