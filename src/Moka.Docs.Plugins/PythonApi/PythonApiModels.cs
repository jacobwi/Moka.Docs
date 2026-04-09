using System.Text.Json.Serialization;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Plugins.PythonApi;

/// <summary>
///     JSON deserialization DTOs for the output of <c>mokadocs_python_analyzer.py</c>.
///     These mirror the structure emitted by the Python script and map 1:1 to the existing
///     <see cref="ApiReference" /> / <see cref="ApiType" /> / <see cref="ApiMember" /> types
///     via <see cref="ToApiReference" />.
///     <para>
///         The Python script uses lowercase-first JSON keys by convention; the DTOs use
///         <see cref="JsonPropertyNameAttribute" /> to match.
///     </para>
/// </summary>
internal static class PythonApiMapper
{
	/// <summary>
	///     Deserializes the Python analyzer's JSON output and maps it to the mokadocs-native
	///     <see cref="ApiReference" /> model so it can be passed to <c>ApiPageRenderer</c>
	///     without any rendering-layer changes.
	/// </summary>
	public static ApiReference ToApiReference(PythonAnalysisDto dto)
	{
		var namespaces = dto.Namespaces
			.Select(ns => new ApiNamespace
			{
				Name = ns.Name,
				Types = ns.Types.Select(MapType).ToList()
			})
			.ToList();

		return new ApiReference
		{
			Assemblies = dto.Assemblies,
			Namespaces = namespaces
		};
	}

	private static ApiType MapType(PythonTypeDto t) => new()
	{
		Name = t.Name,
		FullName = t.FullName ?? t.Name,
		Kind = ParseEnum(t.Kind, ApiTypeKind.Class),
		Accessibility = ParseEnum(t.Accessibility, ApiAccessibility.Public),
		IsStatic = t.IsStatic,
		IsAbstract = t.IsAbstract,
		IsSealed = t.IsSealed,
		IsRecord = t.IsRecord,
		TypeParameters = (t.TypeParameters ?? []).Select(MapTypeParam).ToList(),
		BaseType = t.BaseType,
		ImplementedInterfaces = t.ImplementedInterfaces ?? [],
		Members = (t.Members ?? []).Select(MapMember).ToList(),
		Documentation = MapDoc(t.Documentation),
		Attributes = (t.Attributes ?? []).Select(MapAttr).ToList(),
		Namespace = t.Namespace,
		Assembly = t.Assembly,
		SourcePath = t.SourcePath,
		IsObsolete = t.IsObsolete,
		ObsoleteMessage = t.ObsoleteMessage,
		SourceCode = t.SourceCode
	};

	private static ApiMember MapMember(PythonMemberDto m) => new()
	{
		Name = m.Name,
		Kind = ParseEnum(m.Kind, ApiMemberKind.Method),
		Signature = m.Signature ?? "",
		ReturnType = m.ReturnType,
		Accessibility = ParseEnum(m.Accessibility, ApiAccessibility.Public),
		IsStatic = m.IsStatic,
		IsVirtual = m.IsVirtual,
		IsAbstract = m.IsAbstract,
		IsOverride = m.IsOverride,
		IsSealed = m.IsSealed,
		IsExtensionMethod = m.IsExtensionMethod,
		Parameters = (m.Parameters ?? []).Select(MapParam).ToList(),
		TypeParameters = (m.TypeParameters ?? []).Select(MapTypeParam).ToList(),
		Documentation = MapDoc(m.Documentation),
		Attributes = (m.Attributes ?? []).Select(MapAttr).ToList(),
		IsObsolete = m.IsObsolete,
		ObsoleteMessage = m.ObsoleteMessage
	};

	private static ApiParameter MapParam(PythonParamDto p) => new()
	{
		Name = p.Name,
		Type = p.Type ?? "",
		DefaultValue = p.DefaultValue,
		HasDefaultValue = p.HasDefaultValue,
		IsParams = p.IsParams,
		IsRef = p.IsRef,
		IsOut = p.IsOut,
		IsIn = p.IsIn,
		IsNullable = p.IsNullable
	};

	private static ApiTypeParameter MapTypeParam(PythonTypeParamDto tp) => new()
	{
		Name = tp.Name,
		Constraints = tp.Constraints ?? []
	};

	private static ApiAttribute MapAttr(PythonAttrDto a) => new()
	{
		Name = a.Name,
		Arguments = string.IsNullOrEmpty(a.Arguments) ? [] : [a.Arguments]
	};

	private static XmlDocBlock? MapDoc(PythonDocDto? d)
	{
		if (d is null)
		{
			return null;
		}

		return new XmlDocBlock
		{
			Summary = d.Summary ?? "",
			Remarks = d.Remarks ?? "",
			Parameters = d.Parameters ?? new Dictionary<string, string>(),
			TypeParameters = d.TypeParameters ?? new Dictionary<string, string>(),
			Returns = d.Returns ?? "",
			Exceptions = (d.Exceptions ?? [])
				.Select(e => new ExceptionDoc
				{
					Type = e.Type ?? "",
					Description = e.Description ?? ""
				})
				.ToList(),
			Examples = d.Examples ?? [],
			SeeAlso = d.SeeAlso ?? []
		};
	}

	private static T ParseEnum<T>(string? value, T defaultValue) where T : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return Enum.TryParse(value, true, out T result) ? result : defaultValue;
	}
}

// ── JSON DTOs ────────────────────────────────────────────────────────────

internal sealed class PythonAnalysisDto
{
	[JsonPropertyName("assemblies")] public List<string> Assemblies { get; set; } = [];
	[JsonPropertyName("namespaces")] public List<PythonNamespaceDto> Namespaces { get; set; } = [];
}

internal sealed class PythonNamespaceDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("types")] public List<PythonTypeDto> Types { get; set; } = [];
}

internal sealed class PythonTypeDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("fullName")] public string? FullName { get; set; }
	[JsonPropertyName("kind")] public string? Kind { get; set; }
	[JsonPropertyName("accessibility")] public string? Accessibility { get; set; }
	[JsonPropertyName("isStatic")] public bool IsStatic { get; set; }
	[JsonPropertyName("isAbstract")] public bool IsAbstract { get; set; }
	[JsonPropertyName("isSealed")] public bool IsSealed { get; set; }
	[JsonPropertyName("isRecord")] public bool IsRecord { get; set; }
	[JsonPropertyName("typeParameters")] public List<PythonTypeParamDto>? TypeParameters { get; set; }
	[JsonPropertyName("baseType")] public string? BaseType { get; set; }

	[JsonPropertyName("implementedInterfaces")]
	public List<string>? ImplementedInterfaces { get; set; }

	[JsonPropertyName("members")] public List<PythonMemberDto>? Members { get; set; }
	[JsonPropertyName("documentation")] public PythonDocDto? Documentation { get; set; }
	[JsonPropertyName("attributes")] public List<PythonAttrDto>? Attributes { get; set; }
	[JsonPropertyName("namespace")] public string? Namespace { get; set; }
	[JsonPropertyName("assembly")] public string? Assembly { get; set; }
	[JsonPropertyName("sourcePath")] public string? SourcePath { get; set; }
	[JsonPropertyName("isObsolete")] public bool IsObsolete { get; set; }
	[JsonPropertyName("obsoleteMessage")] public string? ObsoleteMessage { get; set; }
	[JsonPropertyName("sourceCode")] public string? SourceCode { get; set; }
}

internal sealed class PythonMemberDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("kind")] public string? Kind { get; set; }
	[JsonPropertyName("signature")] public string? Signature { get; set; }
	[JsonPropertyName("returnType")] public string? ReturnType { get; set; }
	[JsonPropertyName("accessibility")] public string? Accessibility { get; set; }
	[JsonPropertyName("isStatic")] public bool IsStatic { get; set; }
	[JsonPropertyName("isVirtual")] public bool IsVirtual { get; set; }
	[JsonPropertyName("isAbstract")] public bool IsAbstract { get; set; }
	[JsonPropertyName("isOverride")] public bool IsOverride { get; set; }
	[JsonPropertyName("isSealed")] public bool IsSealed { get; set; }

	[JsonPropertyName("isExtensionMethod")]
	public bool IsExtensionMethod { get; set; }

	[JsonPropertyName("parameters")] public List<PythonParamDto>? Parameters { get; set; }
	[JsonPropertyName("typeParameters")] public List<PythonTypeParamDto>? TypeParameters { get; set; }
	[JsonPropertyName("documentation")] public PythonDocDto? Documentation { get; set; }
	[JsonPropertyName("attributes")] public List<PythonAttrDto>? Attributes { get; set; }
	[JsonPropertyName("isObsolete")] public bool IsObsolete { get; set; }
	[JsonPropertyName("obsoleteMessage")] public string? ObsoleteMessage { get; set; }
}

internal sealed class PythonParamDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("type")] public string? Type { get; set; }
	[JsonPropertyName("defaultValue")] public string? DefaultValue { get; set; }
	[JsonPropertyName("hasDefaultValue")] public bool HasDefaultValue { get; set; }
	[JsonPropertyName("isParams")] public bool IsParams { get; set; }
	[JsonPropertyName("isRef")] public bool IsRef { get; set; }
	[JsonPropertyName("isOut")] public bool IsOut { get; set; }
	[JsonPropertyName("isIn")] public bool IsIn { get; set; }
	[JsonPropertyName("isNullable")] public bool IsNullable { get; set; }
}

internal sealed class PythonTypeParamDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("constraints")] public List<string>? Constraints { get; set; }
	[JsonPropertyName("documentation")] public string? Documentation { get; set; }
}

internal sealed class PythonAttrDto
{
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("arguments")] public string? Arguments { get; set; }
}

internal sealed class PythonDocDto
{
	[JsonPropertyName("summary")] public string? Summary { get; set; }
	[JsonPropertyName("remarks")] public string? Remarks { get; set; }
	[JsonPropertyName("parameters")] public Dictionary<string, string>? Parameters { get; set; }
	[JsonPropertyName("typeParameters")] public Dictionary<string, string>? TypeParameters { get; set; }
	[JsonPropertyName("returns")] public string? Returns { get; set; }
	[JsonPropertyName("exceptions")] public List<PythonExceptionDto>? Exceptions { get; set; }
	[JsonPropertyName("examples")] public List<string>? Examples { get; set; }
	[JsonPropertyName("seeAlso")] public List<string>? SeeAlso { get; set; }
}

internal sealed class PythonExceptionDto
{
	[JsonPropertyName("type")] public string? Type { get; set; }
	[JsonPropertyName("description")] public string? Description { get; set; }
}
