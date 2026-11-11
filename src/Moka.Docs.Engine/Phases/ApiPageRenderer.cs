using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Search;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Generates HTML content for API type documentation pages.
/// </summary>
public static partial class ApiPageRenderer
{
	// Attributes the compiler emits or tooling reads. They don't change how the API is used, and the
	// reflection host sees the compiler's on every async method, nullable annotation and record.
	private static readonly HashSet<string> _hiddenAttributes = new(StringComparer.Ordinal)
	{
		"AsyncIteratorStateMachine", "AsyncStateMachine", "CompilerFeatureRequired", "CompilerGenerated",
		"DebuggerBrowsable", "DebuggerDisplay", "DebuggerHidden", "DebuggerNonUserCode", "DebuggerStepThrough",
		"DebuggerTypeProxy", "DefaultMember", "Dynamic", "ExcludeFromCodeCoverage", "Extension", "GeneratedCode",
		"IsByRefLike", "IsReadOnly", "IsUnmanaged", "IteratorStateMachine", "MethodImpl", "NativeInteger",
		"Nullable", "NullableContext", "NullablePublicOnly", "ParamArray", "RefSafetyRules", "RequiredMember",
		"ScopedRef", "SkipLocalsInit", "StructLayout", "SuppressMessage", "TargetedPatchingOptOut",
		"TupleElementNames", "__DynamicallyInvokable"
	};

	private static readonly Dictionary<string, string> _operatorSymbols = new(StringComparer.Ordinal)
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
		["op_CheckedAdditionAssignment"] = "checked +=",
		["op_CheckedSubtractionAssignment"] = "checked -=",
		["op_CheckedMultiplicationAssignment"] = "checked *=",
		["op_CheckedDivisionAssignment"] = "checked /=",
		["op_IncrementAssignment"] = "++",
		["op_DecrementAssignment"] = "--",
		["op_CheckedIncrementAssignment"] = "checked ++",
		["op_CheckedDecrementAssignment"] = "checked --"
	};

	// Rendering order. Member anchors are numbered in this order too, so both have to use it.
	private static readonly (string Title, ApiMemberKind[] Kinds)[] _memberSections =
	[
		("Constructors", [ApiMemberKind.Constructor]),
		("Properties", [ApiMemberKind.Property, ApiMemberKind.Indexer]),
		("Methods", [ApiMemberKind.Method]),
		("Events", [ApiMemberKind.Event]),
		("Fields", [ApiMemberKind.Field]),
		("Operators", [ApiMemberKind.Operator])
	];

	/// <summary>
	///     Renders an API type as HTML documentation.
	/// </summary>
	/// <param name="type">The type to render.</param>
	/// <param name="allTypes">
	///     All types in the API model, used to discover derived types for the dependency graph, to link
	///     <c>cref</c> references to other type pages and to list inherited members and extension methods.
	/// </param>
	public static string RenderType(ApiType type, IReadOnlyList<ApiType>? allTypes = null)
	{
		var sb = new StringBuilder();
		ApiCrefResolver links = ApiCrefResolver.For(type, allTypes);

		#region Type Header with Badge

		string kindBadge = type.Kind.ToString().ToLowerInvariant();
		sb.AppendLine("<div class=\"api-type-header\">");
		sb.AppendLine($"<span class=\"api-badge api-badge-{kindBadge}\">{type.Kind}</span>");
		if (type.IsObsolete)
		{
			sb.AppendLine("<span class=\"api-badge api-badge-obsolete\">Obsolete</span>");
		}

		if (type.IsStatic)
		{
			sb.AppendLine("<span class=\"api-badge api-badge-static\">Static</span>");
		}

		if (type.IsAbstract)
		{
			sb.AppendLine("<span class=\"api-badge api-badge-abstract\">Abstract</span>");
		}

		if (type.IsSealed && type.Kind != ApiTypeKind.Record)
		{
			sb.AppendLine("<span class=\"api-badge api-badge-sealed\">Sealed</span>");
		}

		sb.AppendLine("</div>");

		#endregion

		// Signature
		string declaration = WithAttributes(type.Attributes, IsPython(type), type.IsObsolete,
			type.Signature ?? BuildTypeSignature(type));
		sb.AppendLine($"<pre class=\"api-signature\"><code class=\"language-csharp\">{Esc(declaration)}</code></pre>");

		// Namespace
		if (!string.IsNullOrEmpty(type.Namespace))
		{
			sb.AppendLine($"<p class=\"api-namespace\">Namespace: <code>{Esc(type.Namespace)}</code></p>");
		}

		// Obsolete warning
		if (type.IsObsolete)
		{
			sb.AppendLine(
				$"<div class=\"warning\"><p><strong>Obsolete:</strong> {Esc(type.ObsoleteMessage ?? "This type is deprecated.")}</p></div>");
		}

		// Summary
		if (type.Documentation is { } doc && !string.IsNullOrEmpty(doc.Summary))
		{
			sb.AppendLine($"<div class=\"api-summary\">{links.ResolveHtml(doc.Summary, type)}</div>");
		}

		// Remarks
		if (type.Documentation?.Remarks is { } remarks && !string.IsNullOrEmpty(remarks))
		{
			sb.AppendLine("<h2 id=\"remarks\">Remarks</h2>");
			sb.AppendLine($"<div class=\"api-remarks\">{links.ResolveHtml(remarks, type)}</div>");
		}

		#region Type Parameters

		if (type.TypeParameters.Count > 0)
		{
			sb.AppendLine("<h2 id=\"type-parameters\">Type Parameters</h2>");
			RenderTypeParameterTable(sb, type.TypeParameters, type.Documentation, type, links);
		}

		#endregion

		#region Inheritance

		if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
		{
			sb.AppendLine("<h2 id=\"inheritance\">Inheritance</h2>");
			if (type.BaseType is not null)
			{
				sb.AppendLine($"<p>Inherits from: <code>{Esc(type.BaseType)}</code></p>");
			}

			if (type.ImplementedInterfaces.Count > 0)
			{
				sb.AppendLine(
					$"<p>Implements: {string.Join(", ", type.ImplementedInterfaces.Select(i => $"<code>{Esc(i)}</code>"))}</p>");
			}
		}

		#endregion

		#region Members by Kind

		foreach ((string title, ApiMemberKind[] kinds) in _memberSections)
		{
			RenderMemberSection(sb, title, SectionMembers(type, kinds), type, links);
		}

		foreach (ApiExtensionGroup group in ExtensionGroups(type))
		{
			RenderExtensionGroup(sb, group, type, links);
		}

		#endregion

		#region Inherited Members and Extension Methods

		ApiTypeRelations relations = ApiTypeRelations.For(allTypes);
		RenderInheritedMembers(sb, type, relations, links);
		RenderExtensionMethods(sb, type, relations, links);

		#endregion

		#region Examples

		if (type.Documentation?.Examples is { Count: > 0 } examples)
		{
			sb.AppendLine("<h2 id=\"examples\">Examples</h2>");
			foreach (string example in examples)
			{
				sb.AppendLine($"<div class=\"api-example\">{links.ResolveHtml(example, type)}</div>");
			}
		}

		#endregion

		#region See Also

		if (type.Documentation?.SeeAlso is { Count: > 0 } seeAlso)
		{
			sb.AppendLine("<h2 id=\"see-also\">See Also</h2>");
			RenderSeeAlsoList(sb, seeAlso, type, links);
		}

		#endregion

		#region Type Dependency Graph

		string? mermaidDiagram = BuildTypeDependencyGraph(type, allTypes ?? [], relations);
		if (mermaidDiagram is not null)
		{
			sb.AppendLine("<details class=\"type-graph\">");
			sb.AppendLine("<summary>");
			sb.AppendLine(
				"<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"5\" r=\"3\"/><circle cx=\"5\" cy=\"19\" r=\"3\"/><circle cx=\"19\" cy=\"19\" r=\"3\"/><line x1=\"12\" y1=\"8\" x2=\"5\" y2=\"16\"/><line x1=\"12\" y1=\"8\" x2=\"19\" y2=\"16\"/></svg>");
			sb.AppendLine("Type Relationships");
			sb.AppendLine("</summary>");
			sb.AppendLine($"<pre class=\"mermaid\">{mermaidDiagram}</pre>");
			sb.AppendLine("</details>");
		}

		#endregion

		#region View Source

		if (!string.IsNullOrEmpty(type.SourceCode))
		{
			sb.AppendLine("<details class=\"source-viewer\">");
			sb.AppendLine("<summary>");
			sb.AppendLine(
				"<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/></svg>");
			sb.AppendLine("View Source");
			sb.AppendLine("</summary>");
			sb.AppendLine($"<pre><code class=\"language-csharp\">{Esc(type.SourceCode)}</code></pre>");
			sb.AppendLine("</details>");
		}

		// Only the ASP.NET Core host sets a source URL, from the SourceLink data in the assembly's PDB.
		// The class gives it the look of the theme's "Edit this page" link. The URL comes from a
		// build artifact, so anything but http(s), such as a javascript: URL, gets no link.
		if (Uri.TryCreate(type.SourceUrl, UriKind.Absolute, out Uri? sourceUri)
		    && (sourceUri.Scheme == Uri.UriSchemeHttps || sourceUri.Scheme == Uri.UriSchemeHttp))
		{
			string sourceLabel = $"View source on {sourceUri.Host}";
			sb.AppendLine("<div class=\"edit-link source-link\">");
			sb.AppendLine($"<a href=\"{HttpUtility.HtmlAttributeEncode(type.SourceUrl)}\" target=\"_blank\" rel=\"noopener\">");
			sb.AppendLine(
				"<svg width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M15 3h6v6\"/><path d=\"M10 14 21 3\"/><path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/></svg>");
			sb.AppendLine(Esc(sourceLabel));
			sb.AppendLine("</a>");
			sb.AppendLine("</div>");
		}

		#endregion

		return sb.ToString();
	}

	private static void RenderMemberSection(StringBuilder sb, string title, List<ApiMember> members, ApiType parentType,
		ApiCrefResolver links)
	{
		if (members.Count == 0)
		{
			return;
		}

		string sectionId = title.ToLowerInvariant().Replace(' ', '-');
		sb.AppendLine($"<h2 id=\"{sectionId}\">{Esc(title)}</h2>");
		RenderMembers(sb, members, parentType, links);
	}

	private static void RenderMembers(StringBuilder sb, List<ApiMember> members, ApiType parentType,
		ApiCrefResolver links)
	{
		IReadOnlyDictionary<ApiMember, string> anchors = links.GetAnchors(parentType);
		bool python = IsPython(parentType);

		sb.AppendLine(
			"<div class=\"table-responsive\"><table class=\"api-member-table\"><thead><tr><th>Name</th><th>Description</th></tr></thead><tbody>");

		foreach (ApiMember member in members)
		{
			var badges = new List<string>();
			if (member.IsStatic)
			{
				badges.Add("<span class=\"api-badge-sm api-badge-static\">static</span>");
			}

			if (member.IsVirtual)
			{
				badges.Add("<span class=\"api-badge-sm api-badge-virtual\">virtual</span>");
			}

			if (member.IsAbstract)
			{
				badges.Add("<span class=\"api-badge-sm api-badge-abstract\">abstract</span>");
			}

			if (member.IsOverride)
			{
				badges.Add("<span class=\"api-badge-sm\">override</span>");
			}

			if (member.IsObsolete)
			{
				badges.Add("<span class=\"api-badge-sm api-badge-obsolete\">obsolete</span>");
			}

			string badgeHtml = badges.Count > 0 ? " " + string.Join(" ", badges) : "";
			string summary = links.ResolveHtml(member.Documentation?.Summary ?? "", parentType);

			string compactTitle = MemberTitle(member, parentType, true);
			string fullTitle = MemberTitle(member, parentType, false);
			string titleAttr = compactTitle != fullTitle ? $" title=\"{Esc(fullTitle)}\"" : "";

			// Only members with a detail block have an anchor. The link used to point at an id that
			// nothing on the page had.
			string nameHtml = anchors.TryGetValue(member, out string? anchor)
				? $"<a href=\"#{Esc(anchor)}\" class=\"api-member-link\"{titleAttr}><code>{Esc(compactTitle)}</code></a>"
				: $"<code{titleAttr}>{Esc(compactTitle)}</code>";

			sb.AppendLine("<tr>");
			sb.AppendLine($"<td>{nameHtml}{badgeHtml}</td>");
			sb.AppendLine($"<td>{summary}</td>");
			sb.AppendLine("</tr>");
		}

		sb.AppendLine("</tbody></table></div>");

		#region Detailed Member Docs

		foreach (ApiMember member in members)
		{
			if (!anchors.TryGetValue(member, out string? anchor))
			{
				continue;
			}

			sb.AppendLine($"<h3 id=\"{Esc(anchor)}\">{Esc(MemberTitle(member, parentType, false))}</h3>");

			string signature = WithAttributes(member.Attributes, python, member.IsObsolete, member.Signature);
			sb.AppendLine($"<pre class=\"api-signature\"><code class=\"language-csharp\">{Esc(signature)}</code></pre>");

			if (member.IsObsolete)
			{
				string message = string.IsNullOrWhiteSpace(member.ObsoleteMessage)
					? "This member is deprecated."
					: member.ObsoleteMessage;
				sb.AppendLine($"<div class=\"warning\"><p><strong>Obsolete:</strong> {Esc(message)}</p></div>");
			}

			if (member.Documentation is not { } doc)
			{
				continue;
			}

			if (!string.IsNullOrEmpty(doc.Summary))
			{
				sb.AppendLine($"<p>{links.ResolveHtml(doc.Summary, parentType)}</p>");
			}

			// Type Parameters
			if (member.TypeParameters.Count > 0 || doc.TypeParameters.Count > 0)
			{
				sb.AppendLine("<h4>Type Parameters</h4>");
				List<ApiTypeParameter> typeParameters = member.TypeParameters.Count > 0
					? member.TypeParameters
					: doc.TypeParameters.Keys.Select(name => new ApiTypeParameter { Name = name }).ToList();
				RenderTypeParameterTable(sb, typeParameters, doc, parentType, links);
			}

			// Parameters
			if (doc.Parameters.Count > 0)
			{
				sb.AppendLine("<h4>Parameters</h4>");
				sb.AppendLine(
					"<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Type</th><th>Description</th></tr></thead><tbody>");
				foreach (ApiParameter param in member.Parameters)
				{
					string desc = links.ResolveHtml(doc.Parameters.GetValueOrDefault(param.Name) ?? "", parentType);
					sb.AppendLine(
						$"<tr><td><code>{Esc(param.Name)}</code></td><td><code>{Esc(ParameterModifiers(param) + param.Type)}</code></td><td>{desc}</td></tr>");
				}

				sb.AppendLine("</tbody></table></div>");
			}

			// Returns
			if (!string.IsNullOrEmpty(doc.Returns))
			{
				sb.AppendLine($"<p><strong>Returns:</strong> {links.ResolveHtml(doc.Returns, parentType)}</p>");
			}

			// Value
			if (!string.IsNullOrEmpty(doc.Value))
			{
				sb.AppendLine($"<p><strong>Value:</strong> {links.ResolveHtml(doc.Value, parentType)}</p>");
			}

			// Exceptions
			if (doc.Exceptions.Count > 0)
			{
				sb.AppendLine("<h4>Exceptions</h4>");
				sb.AppendLine(
					"<div class=\"table-responsive\"><table><thead><tr><th>Exception</th><th>Condition</th></tr></thead><tbody>");
				foreach (ExceptionDoc ex in doc.Exceptions)
				{
					sb.AppendLine(
						$"<tr><td>{links.RenderExceptionType(ex.Type, parentType)}</td><td>{links.ResolveHtml(ex.Description, parentType)}</td></tr>");
				}

				sb.AppendLine("</tbody></table></div>");
			}

			// Remarks
			if (!string.IsNullOrEmpty(doc.Remarks))
			{
				sb.AppendLine("<h4>Remarks</h4>");
				sb.AppendLine($"<div class=\"api-remarks\">{links.ResolveHtml(doc.Remarks, parentType)}</div>");
			}

			// Examples
			if (doc.Examples.Count > 0)
			{
				sb.AppendLine("<h4>Examples</h4>");
				foreach (string example in doc.Examples)
				{
					sb.AppendLine($"<div class=\"api-example\">{links.ResolveHtml(example, parentType)}</div>");
				}
			}

			// See Also
			if (doc.SeeAlso.Count > 0)
			{
				sb.AppendLine("<h4>See Also</h4>");
				RenderSeeAlsoList(sb, doc.SeeAlso, parentType, links);
			}
		}

		#endregion
	}

	private static string ParameterModifiers(ApiParameter param)
	{
		var modifiers = new List<string>();
		if (param.IsRef)
		{
			modifiers.Add("ref");
		}

		if (param.IsOut)
		{
			modifiers.Add("out");
		}

		if (param.IsIn)
		{
			modifiers.Add("in");
		}

		if (param.IsParams)
		{
			modifiers.Add("params");
		}

		return modifiers.Count > 0 ? string.Join(" ", modifiers) + " " : "";
	}

	private static void RenderTypeParameterTable(StringBuilder sb, List<ApiTypeParameter> typeParameters,
		XmlDocBlock? documentation, ApiType parentType, ApiCrefResolver links)
	{
		sb.AppendLine(
			"<div class=\"table-responsive\"><table><thead><tr><th>Name</th><th>Constraints</th><th>Description</th></tr></thead><tbody>");
		foreach (ApiTypeParameter tp in typeParameters)
		{
			string constraints = tp.Constraints.Count > 0 ? string.Join(", ", tp.Constraints) : "-";
			string desc = links.ResolveHtml(documentation?.TypeParameters.GetValueOrDefault(tp.Name) ?? "", parentType);
			sb.AppendLine(
				$"<tr><td><code>{Esc(tp.Name)}</code></td><td>{Esc(constraints)}</td><td>{desc}</td></tr>");
		}

		sb.AppendLine("</tbody></table></div>");
	}

	private static void RenderSeeAlsoList(StringBuilder sb, List<string> seeAlso, ApiType parentType,
		ApiCrefResolver links)
	{
		sb.AppendLine("<ul>");
		foreach (string entry in seeAlso.Where(s => !string.IsNullOrWhiteSpace(s)))
		{
			sb.AppendLine($"<li>{links.RenderSeeAlso(entry, parentType)}</li>");
		}

		sb.AppendLine("</ul>");
	}

	/// <summary>
	///     The members of one section, in the order the page lists them.
	/// </summary>
	private static List<ApiMember> SectionMembers(ApiType type, ApiMemberKind[] kinds) =>
		type.Members.Where(m => m.Extension is null && kinds.Contains(m.Kind)).OrderBy(m => m.Name).ToList();

	/// <summary>
	///     The id of the section heading that lists members of <paramref name="kind" />.
	/// </summary>
	internal static string SectionId(ApiMemberKind kind) =>
		_memberSections.First(s => s.Kinds.Contains(kind)).Title.ToLowerInvariant();

	/// <summary>
	///     Gives each member that gets a detail block a unique anchor id.
	/// </summary>
	/// <remarks>
	///     The id is the lowercased metadata name (the type name for a constructor), as it has always been,
	///     so existing links keep working. Overloads used to share one id, which left every overload after
	///     the first unreachable, and a member named like a section (<c>Examples</c>) took that section's
	///     id. Now the first overload keeps the plain id, later ones get <c>-2</c>, <c>-3</c>, and section
	///     ids are never reused.
	/// </remarks>
	internal static IReadOnlyDictionary<ApiMember, string> AssignMemberAnchors(ApiType type)
	{
		var anchors = new Dictionary<ApiMember, string>(ReferenceEqualityComparer.Instance);
		var used = new HashSet<string>(BuildTocForType(type).Entries.Select(e => e.Id), StringComparer.Ordinal);
		bool python = IsPython(type);

		IEnumerable<ApiMember> pageOrder = _memberSections
			.SelectMany(section => SectionMembers(type, section.Kinds))
			.Concat(ExtensionGroups(type).SelectMany(group => group.Members));

		foreach (ApiMember member in pageOrder.Where(m => HasDetails(m, python)))
		{
			string baseId = (member.Kind == ApiMemberKind.Constructor ? type.Name : member.Name).ToLowerInvariant();
			string id = baseId;
			for (int n = 2; !used.Add(id); n++)
			{
				id = $"{baseId}-{n}";
			}

			anchors[member] = id;
		}

		return anchors;
	}

	/// <summary>
	///     Whether a member gets a detail block: any documentation, an obsolete message or an attribute
	///     worth showing. Only a summary, parameters or a return value used to count, so a member
	///     documented with just remarks or exceptions showed nothing but its table row.
	/// </summary>
	private static bool HasDetails(ApiMember member, bool python) =>
		member.IsObsolete || HasDocumentation(member.Documentation) ||
		AttributeLines(member.Attributes, python, member.IsObsolete).Count > 0;

	private static bool HasDocumentation(XmlDocBlock? doc) =>
		doc is not null &&
		(!string.IsNullOrWhiteSpace(doc.Summary) || !string.IsNullOrWhiteSpace(doc.Remarks) ||
		 !string.IsNullOrWhiteSpace(doc.Returns) || !string.IsNullOrWhiteSpace(doc.Value) ||
		 doc.Parameters.Count > 0 || doc.TypeParameters.Count > 0 || doc.Exceptions.Count > 0 ||
		 doc.Examples.Count > 0 || doc.SeeAlso.Count > 0);

	/// <summary>
	///     Whether the Python plugin produced the type. Its types carry the <c>.py</c> file they came from,
	///     which C# types never do, and its decorators and routes follow Python rules.
	/// </summary>
	internal static bool IsPython(ApiType type) =>
		type.SourcePath?.EndsWith(".py", StringComparison.OrdinalIgnoreCase) == true;

	/// <summary>
	///     The name a member is shown under: <c>operator +</c> rather than <c>op_Addition</c>,
	///     <c>this[]</c> for an indexer and the type name for a constructor.
	/// </summary>
	internal static string MemberDisplayName(ApiMember member, ApiType parent) => member.Kind switch
	{
		ApiMemberKind.Constructor => parent.Name,
		ApiMemberKind.Indexer => "this[]",
		ApiMemberKind.Operator => OperatorName(member),
		_ => member.Name
	};

	internal static string OperatorName(ApiMember member)
	{
		string target = ShortTypeName(member.ReturnType ?? "");
		return member.Name switch
		{
			"op_Implicit" => $"implicit operator {target}",
			"op_Explicit" => $"explicit operator {target}",
			"op_CheckedExplicit" => $"explicit operator checked {target}",
			_ => _operatorSymbols.TryGetValue(member.Name, out string? symbol) ? $"operator {symbol}" : member.Name
		};
	}

	/// <summary>
	///     A member's name with its parameters, as the member table and the detail heading show it:
	///     <c>Parse(string s)</c>, <c>this[int index]</c>, <c>operator +(Money a, Money b)</c>.
	/// </summary>
	private static string MemberTitle(ApiMember member, ApiType parent, bool compact)
	{
		// In compact mode (summary tables), abbreviate long parameter lists
		string parameters = compact && member.Parameters.Count > 2
			? "\u2026"
			: string.Join(", ", member.Parameters.Select(p => $"{ShortTypeName(p.Type)} {p.Name}"));

		string typeParameters = member.TypeParameters.Count > 0
			? $"<{string.Join(", ", member.TypeParameters.Select(tp => tp.Name))}>"
			: "";

		return member.Kind switch
		{
			ApiMemberKind.Indexer => $"this[{parameters}]",
			ApiMemberKind.Constructor or ApiMemberKind.Method or ApiMemberKind.Operator =>
				$"{MemberDisplayName(member, parent)}{typeParameters}({parameters})",
			_ => member.Parameters.Count > 0 ? $"{member.Name}({parameters})" : member.Name
		};
	}

	/// <summary>
	///     Removes the namespace from every type name in <paramref name="type" />, generic arguments included.
	///     Taking the text after the last dot turned
	///     <c>Dictionary&lt;string, System.Collections.Generic.List&lt;int&gt;&gt;</c> into <c>List&lt;int&gt;&gt;</c>.
	/// </summary>
	internal static string ShortTypeName(string type) => NamespaceQualifierRegex().Replace(type, "");

	/// <summary>
	///     Puts the attributes worth showing above a declaration, as C# attributes or, for Python, decorators.
	/// </summary>
	private static string WithAttributes(List<ApiAttribute> attributes, bool python, bool obsoleteShown,
		string declaration)
	{
		List<string> lines = AttributeLines(attributes, python, obsoleteShown);
		return lines.Count == 0 ? declaration : string.Join("\n", lines) + "\n" + declaration;
	}

	/// <summary>
	///     The attributes worth showing, one line each, with compiler and tooling attributes left out.
	/// </summary>
	/// <param name="attributes">The attributes of a type or member.</param>
	/// <param name="python">Whether to write them as Python decorators.</param>
	/// <param name="obsoleteShown">
	///     Whether the page already marks the symbol obsolete. The analyzer only recognizes a resolved
	///     <c>ObsoleteAttribute</c>, so an <c>[Obsolete]</c> it couldn't resolve still needs its line.
	/// </param>
	private static List<string> AttributeLines(List<ApiAttribute> attributes, bool python, bool obsoleteShown)
	{
		var lines = new List<string>();

		foreach (ApiAttribute attribute in attributes)
		{
			string name = attribute.Name.Trim();
			if (name.Length == 0)
			{
				continue;
			}

			// Arguments are shown as the analyzers stored them. Strings lost their quotes there, and quoting
			// them again here would also quote typeof and enum arguments, which are stored the same way.
			string arguments = attribute.Arguments.Count > 0 ? $"({string.Join(", ", attribute.Arguments)})" : "";

			if (python)
			{
				// "module" marks the plugin's page for a module's functions. It isn't a decorator.
				if (name != "module")
				{
					lines.Add($"@{name}{arguments}");
				}

				continue;
			}

			if (name.EndsWith("Attribute", StringComparison.Ordinal) && name.Length > "Attribute".Length)
			{
				name = name[..^"Attribute".Length];
			}

			string simpleName = name[(name.LastIndexOf('.') + 1)..];
			if (!(obsoleteShown && simpleName == "Obsolete") && !_hiddenAttributes.Contains(simpleName))
			{
				lines.Add($"[{name}{arguments}]");
			}
		}

		return lines;
	}

	private static string BuildTypeSignature(ApiType type)
	{
		var sb = new StringBuilder();

		sb.Append(type.Accessibility switch
		{
			ApiAccessibility.ProtectedInternal => "protected internal",
			ApiAccessibility.PrivateProtected => "private protected",
			_ => type.Accessibility.ToString().ToLowerInvariant()
		});
		sb.Append(' ');

		if (type.IsStatic)
		{
			sb.Append("static ");
		}

		if (type.IsAbstract && type.Kind != ApiTypeKind.Interface)
		{
			sb.Append("abstract ");
		}

		if (type.IsSealed && type.Kind == ApiTypeKind.Class)
		{
			sb.Append("sealed ");
		}

		sb.Append(type.Kind switch
		{
			ApiTypeKind.Class => type.IsRecord ? "record " : "class ",
			ApiTypeKind.Struct => type.IsRecord ? "record struct " : "struct ",
			ApiTypeKind.Interface => "interface ",
			ApiTypeKind.Enum => "enum ",
			ApiTypeKind.Delegate => "delegate ",
			ApiTypeKind.Record => "record ",
			_ => ""
		});

		sb.Append(type.Name);

		if (type.TypeParameters.Count > 0)
		{
			sb.Append($"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>");
		}

		if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
		{
			var bases = new List<string>();
			if (type.BaseType is not null)
			{
				bases.Add(type.BaseType);
			}

			bases.AddRange(type.ImplementedInterfaces);
			sb.Append($" : {string.Join(", ", bases)}");
		}

		return sb.ToString();
	}

	/// <summary>
	///     Builds a Mermaid class diagram showing the type's inheritance and interface relationships.
	///     Returns null if the diagram would only contain the type itself (no meaningful relationships).
	/// </summary>
	/// <remarks>
	///     Nodes get plain ids (<c>T0</c>, <c>T1</c>) and the type name as their label. Type names used to be
	///     the ids, with the namespace cut at the last dot, so <c>System.IEquatable&lt;MyLib.Money&gt;</c>
	///     became a node called <c>Money~</c> and the type seemed to implement itself.
	/// </remarks>
	private static string? BuildTypeDependencyGraph(ApiType type, IReadOnlyList<ApiType> allTypes, ApiTypeRelations relations)
	{
		const int maxNodes = 20;

		string self = NameWithTypeParameters(type);
		var nodes = new List<string> { self };
		var edges = new List<(string From, string To, string Label, string Arrow)>();

		void AddEdge(string from, string to, string label, string arrow)
		{
			foreach (string node in new[] { from, to })
			{
				if (!nodes.Contains(node))
				{
					nodes.Add(node);
				}
			}

			edges.Add((from, to, label, arrow));
		}

		// Base type (skip Object - not meaningful)
		if (type.BaseType is not null && type.BaseType != "Object" && type.BaseType != "object")
		{
			AddEdge(self, ShortTypeName(type.BaseType), "inherits", "--|>");
		}

		foreach (string iface in type.ImplementedInterfaces)
		{
			if (nodes.Count >= maxNodes)
			{
				break;
			}

			AddEdge(self, ShortTypeName(iface), "implements", "..|>");
		}

		// Derived types - types that list this type as their base
		foreach (ApiType other in allTypes)
		{
			if (nodes.Count >= maxNodes)
			{
				break;
			}

			if (!ReferenceEquals(other, type) && other.BaseType is not null && NamesType(other.BaseType, other, type, relations))
			{
				AddEdge(NameWithTypeParameters(other), self, "inherits", "--|>");
			}
		}

		// Types that implement this type (when this type is an interface)
		if (type.Kind == ApiTypeKind.Interface)
		{
			foreach (ApiType other in allTypes)
			{
				if (nodes.Count >= maxNodes)
				{
					break;
				}

				if (!ReferenceEquals(other, type) && other.ImplementedInterfaces.Any(i => NamesType(i, other, type, relations)))
				{
					AddEdge(NameWithTypeParameters(other), self, "implements", "..|>");
				}
			}
		}

		// Skip diagram if the type is alone (no relationships worth showing)
		if (nodes.Count <= 1)
		{
			return null;
		}

		var sb = new StringBuilder();
		sb.AppendLine("classDiagram");
		for (int i = 0; i < nodes.Count; i++)
		{
			sb.AppendLine($"    class T{i}[\"{MermaidLabel(nodes[i])}\"]");
		}

		// Highlight the current type with a style directive
		sb.AppendLine("    style T0 fill:#f9f,stroke:#333,stroke-width:2px");

		foreach ((string from, string to, string label, string arrow) in edges)
		{
			sb.AppendLine($"    T{nodes.IndexOf(from)} {arrow} T{nodes.IndexOf(to)} : {label}");
		}

		return sb.ToString();
	}

	private static string NameWithTypeParameters(ApiType type) =>
		type.TypeParameters.Count == 0
			? type.Name
			: $"{type.Name}<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>";

	// Whether a base type or interface reference on another type names the type, whatever type arguments it
	// passes. Matching the bare name made every System.Exception subclass derive from a documented Exception.
	private static bool NamesType(string reference, ApiType from, ApiType type, ApiTypeRelations relations)
	{
		if (!IsPython(type) && !IsPython(from))
		{
			return relations.Names(reference, from, type);
		}

		string name = ShortTypeName(reference);
		int open = name.IndexOf('<');
		return (open >= 0 ? name[..open] : name).Trim() == type.Name;
	}

	/// <summary>
	///     A node label in Mermaid's entity syntax, which the diagram shows as written. The text sits in
	///     a <c>pre</c> element, so it can't hold raw HTML either.
	/// </summary>
	private static string MermaidLabel(string name)
	{
		var sb = new StringBuilder(name.Length);
		foreach (char c in name)
		{
			sb.Append(c switch
			{
				'<' => "#lt;",
				'>' => "#gt;",
				'"' => "#quot;",
				'#' => "#35;",
				'&' => "#amp;",
				_ => c.ToString()
			});
		}

		return sb.ToString();
	}

	private static string Esc(string text) => HttpUtility.HtmlEncode(text);

	[GeneratedRegex(@"\b(?:[A-Za-z_][A-Za-z0-9_]*\.)+(?=[A-Za-z_])")]
	private static partial Regex NamespaceQualifierRegex();

	/// <summary>
	///     The search entries for the members on a type's page: one per member, in page order, linked to
	///     its detail block (or its section when it has none), with its summary as plain text.
	/// </summary>
	/// <param name="type">The type whose page it is.</param>
	/// <param name="route">The page's route.</param>
	/// <param name="title">The page's title.</param>
	/// <returns>The entries; none for a type from the Python plugin, whose pages are laid out differently.</returns>
	public static IReadOnlyList<SearchEntry> BuildMemberSearchEntries(ApiType type, string route, string title)
	{
		if (IsPython(type))
		{
			return [];
		}

		IReadOnlyDictionary<ApiMember, string> anchors = AssignMemberAnchors(type);
		return _memberSections
			.SelectMany(section => SectionMembers(type, section.Kinds))
			.Concat(ExtensionGroups(type).SelectMany(group => group.Members))
			.Select(member => new SearchEntry
			{
				Title = title,
				Section = MemberTitle(member, type, false),
				Route = $"{route}#{(anchors.TryGetValue(member, out string? anchor) ? anchor : MemberSectionId(type, member))}",
				Content = ApiDocText.ToPlainText(member.Documentation?.Summary),
				Category = "API Reference"
			})
			.ToList();
	}

	/// <summary>
	///     A type's summary as HTML for a listing such as the API index page, with its <c>cref</c>
	///     references linked the same way as on the type's own page.
	/// </summary>
	/// <param name="type">The type whose summary to render.</param>
	/// <param name="allTypes">All types in the API model, as passed to <see cref="RenderType" />.</param>
	public static string RenderSummary(ApiType type, IReadOnlyList<ApiType> allTypes) =>
		ApiCrefResolver.For(type, allTypes).ResolveHtml(type.Documentation?.Summary ?? "", type);

	/// <summary>
	///     Builds a table of contents for an API type page based on the sections it will render.
	///     Without the other types it leaves out the Inherited Members and Extension Methods sections;
	///     see <see cref="BuildTocForType(ApiType, IReadOnlyList{ApiType})" />.
	/// </summary>
	public static TableOfContents BuildTocForType(ApiType type) => BuildTocForType(type, null);

	/// <summary>
	///     Builds a table of contents for an API type page based on the sections it will render.
	/// </summary>
	/// <param name="type">The type whose page it is.</param>
	/// <param name="allTypes">
	///     All types in the API model, as passed to <see cref="RenderType" />. The Inherited Members and
	///     Extension Methods sections depend on them.
	/// </param>
	public static TableOfContents BuildTocForType(ApiType type, IReadOnlyList<ApiType>? allTypes)
	{
		var entries = new List<TocEntry>();

		if (type.Documentation?.Remarks is { Length: > 0 })
		{
			entries.Add(new TocEntry { Level = 2, Text = "Remarks", Id = "remarks" });
		}

		if (type.TypeParameters.Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = "Type Parameters", Id = "type-parameters" });
		}

		if (type.BaseType is not null || type.ImplementedInterfaces.Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = "Inheritance", Id = "inheritance" });
		}

		// Extension block members have sections of their own.
		List<ApiMember> members = type.Members.Where(m => m.Extension is null).ToList();

		if (members.Any(m => m.Kind == ApiMemberKind.Constructor))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Constructors", Id = "constructors" });
		}

		if (members.Any(m => m.Kind is ApiMemberKind.Property or ApiMemberKind.Indexer))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Properties", Id = "properties" });
		}

		if (members.Any(m => m.Kind == ApiMemberKind.Method))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Methods", Id = "methods" });
		}

		if (members.Any(m => m.Kind == ApiMemberKind.Event))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Events", Id = "events" });
		}

		if (members.Any(m => m.Kind == ApiMemberKind.Field))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Fields", Id = "fields" });
		}

		if (members.Any(m => m.Kind == ApiMemberKind.Operator))
		{
			entries.Add(new TocEntry { Level = 2, Text = "Operators", Id = "operators" });
		}

		entries.AddRange(ExtensionGroups(type).Select(group => new TocEntry { Level = 2, Text = group.Title, Id = group.Id }));

		ApiTypeRelations relations = ApiTypeRelations.For(allTypes);
		if (relations.InheritedMembers(type).Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = "Inherited Members", Id = _inheritedMembersId });
		}

		IReadOnlyList<ApiExtensionMember> extensions = relations.ExtensionMembers(type);
		if (extensions.Count > 0)
		{
			entries.Add(new TocEntry { Level = 2, Text = ExtensionSectionTitle(extensions), Id = _extensionMethodsId });
		}

		if (type.Documentation?.Examples is { Count: > 0 })
		{
			entries.Add(new TocEntry { Level = 2, Text = "Examples", Id = "examples" });
		}

		if (type.Documentation?.SeeAlso is { Count: > 0 })
		{
			entries.Add(new TocEntry { Level = 2, Text = "See Also", Id = "see-also" });
		}

		return new TableOfContents { Entries = entries };
	}
}
