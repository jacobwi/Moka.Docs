using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.XmlDoc;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Turns the documentation IDs in XML doc comments into links on API type pages.
/// </summary>
/// <remarks>
///     A reference resolves to the first of these that applies:
///     <list type="bullet">
///         <item>
///             <description>A type or member in the API model, linked to its page and member anchor.</description>
///         </item>
///         <item>
///             <description>
///                 For a name the compiler couldn't bind (<c>!:</c>), the one documented type with that name.
///             </description>
///         </item>
///         <item>
///             <description>A <c>System.*</c> or <c>Microsoft.*</c> API, linked to learn.microsoft.com.</description>
///         </item>
///     </list>
///     Anything else is rendered as text.
/// </remarks>
internal sealed partial class ApiCrefResolver
{
	private const string _learnBaseUrl = "https://learn.microsoft.com/dotnet/api/";

	// The phases render every type page against the same list. Indexing it again for each page made
	// the work grow with the square of the type count.
	private static readonly ConditionalWeakTable<IReadOnlyList<ApiType>, ApiCrefResolver> _cache = new();

	private static readonly Dictionary<string, string> _clrTypeKeywords = new(StringComparer.Ordinal)
	{
		["Boolean"] = "bool",
		["Byte"] = "byte",
		["SByte"] = "sbyte",
		["Char"] = "char",
		["Decimal"] = "decimal",
		["Double"] = "double",
		["Single"] = "float",
		["Int16"] = "short",
		["UInt16"] = "ushort",
		["Int32"] = "int",
		["UInt32"] = "uint",
		["Int64"] = "long",
		["UInt64"] = "ulong",
		["IntPtr"] = "nint",
		["UIntPtr"] = "nuint",
		["Object"] = "object",
		["String"] = "string",
		["Void"] = "void"
	};

	private readonly ConcurrentDictionary<ApiType, IReadOnlyDictionary<ApiMember, string>> _anchors =
		new(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<string, ApiType> _typesById = new(StringComparer.Ordinal);
	private readonly Dictionary<string, List<(string Id, ApiType Type)>> _typesByName = new(StringComparer.Ordinal);

	private ApiCrefResolver(IEnumerable<ApiType> types)
	{
		foreach (ApiType type in types)
		{
			// Python pages live under the plugin's route prefix, so an /api link to one would be wrong.
			if (ApiPageRenderer.IsPython(type))
			{
				continue;
			}

			string id = TypeId(type);
			if (!_typesById.TryAdd(id, type))
			{
				continue;
			}

			string name = id[(id.LastIndexOf('.') + 1)..];
			if (!_typesByName.TryGetValue(name, out List<(string Id, ApiType Type)>? sameName))
			{
				sameName = [];
				_typesByName[name] = sameName;
			}

			sameName.Add((id, type));
		}
	}

	/// <summary>
	///     Gets the resolver for a page's type list, or one that only knows <paramref name="type" />.
	/// </summary>
	internal static ApiCrefResolver For(ApiType type, IReadOnlyList<ApiType>? allTypes) =>
		allTypes is null ? new ApiCrefResolver([type]) : _cache.GetValue(allTypes, list => new ApiCrefResolver(list));

	/// <summary>
	///     The anchor id of each member of <paramref name="type" /> that has a detail block.
	/// </summary>
	internal IReadOnlyDictionary<ApiMember, string> GetAnchors(ApiType type) =>
		_anchors.GetOrAdd(type, ApiPageRenderer.AssignMemberAnchors);

	/// <summary>
	///     Replaces the <c>&lt;a data-cref&gt;</c> anchors that <see cref="XmlDocParser" /> writes with links.
	///     A reference that doesn't resolve becomes inline code, or plain text when the comment gave it its
	///     own text.
	/// </summary>
	internal string ResolveHtml(string html, ApiType current)
	{
		if (string.IsNullOrEmpty(html) || !html.Contains("data-cref=", StringComparison.Ordinal))
		{
			return html;
		}

		return CrefAnchorRegex().Replace(html, match =>
		{
			string id = WebUtility.HtmlDecode(match.Groups["id"].Value);
			string text = WebUtility.HtmlDecode(match.Groups["text"].Value);
			bool hasOwnText = text != XmlDocParser.GetCrefDisplayName(id);

			if (Resolve(id, current) is not { } target)
			{
				return hasOwnText ? Esc(text) : $"<code>{Esc(text)}</code>";
			}

			return Link(target.Href, hasOwnText ? text : target.Text);
		});
	}

	/// <summary>
	///     Renders one <see cref="XmlDocBlock.SeeAlso" /> entry: a documentation ID, a <c>[text](target)</c>
	///     link whose target is a URL or a documentation ID, or plain text.
	/// </summary>
	internal string RenderSeeAlso(string entry, ApiType current)
	{
		entry = entry.Trim();

		if (TryParseLink(entry, out string text, out string target))
		{
			if (IsDocumentationId(target))
			{
				return Resolve(target, current) is { } resolved ? Link(resolved.Href, text) : Esc(text);
			}

			return IsSafeUrl(target) ? Link(target, text) : Esc(text);
		}

		if (IsDocumentationId(entry))
		{
			return Resolve(entry, current) is { } resolved
				? Link(resolved.Href, resolved.Text)
				: $"<code>{Esc(XmlDocParser.GetCrefDisplayName(entry))}</code>";
		}

		// The Python plugin stores See Also lines as plain text.
		return $"<code>{Esc(entry)}</code>";
	}

	/// <summary>
	///     Renders the type of an <c>&lt;exception&gt;</c> entry, linked when it resolves.
	/// </summary>
	internal string RenderExceptionType(string exceptionType, ApiType current)
	{
		// AssemblyAnalyzer removes every ID prefix from exception crefs, including the !: of a name the
		// compiler couldn't bind. XmlDocParser only removes T:.
		string id = exceptionType.Length > 1 && exceptionType[1] == ':' ? exceptionType : "T:" + exceptionType;
		string text = id[2..];
		CrefTarget? target = Resolve(id, current) ?? (id[0] == 'T' ? ResolveUnbound(text, current) : null);

		// Inline code already has the link color in the default theme, so a link wrapped around code
		// looked like a name that doesn't resolve. Links are plain text, as they are in the descriptions.
		return target is null ? $"<code>{Esc(text)}</code>" : Link(target.Href, text);
	}

	private CrefTarget? Resolve(string id, ApiType current)
	{
		if (id.Length < 3 || id[1] != ':')
		{
			return null;
		}

		string body = id[2..];
		return id[0] switch
		{
			'T' => _typesById.TryGetValue(body, out ApiType? type) ? TypeTarget(type) : LearnTarget(id),
			'M' or 'P' or 'F' or 'E' => ResolveMember(id[0], body, current) ?? LearnTarget(id),
			'N' => LearnTarget(id),
			'!' => ResolveUnbound(body, current),
			_ => null
		};
	}

	private CrefTarget? ResolveMember(char kind, string body, ApiType current)
	{
		// M:Ns.Type`1.Name``1(System.Int32,`0), with ~ReturnType after the list for conversions.
		string? parameters = null;
		int open = body.IndexOf('(');
		if (open >= 0)
		{
			int close = body.LastIndexOf(')');
			parameters = close > open ? body[(open + 1)..close] : body[(open + 1)..];
			body = body[..open];
		}

		int dot = body.LastIndexOf('.');
		if (dot <= 0 || !_typesById.TryGetValue(body[..dot], out ApiType? parent))
		{
			return null;
		}

		string name = ArityMarkerRegex().Replace(body[(dot + 1)..], "");
		List<ApiMember> candidates = parent.Members.Where(m => kind switch
		{
			'M' when name == "#ctor" => m.Kind == ApiMemberKind.Constructor,
			'M' => m.Kind is ApiMemberKind.Method or ApiMemberKind.Operator && m.Name == name,
			'P' when parameters is not null => m.Kind == ApiMemberKind.Indexer,
			'P' => m.Kind == ApiMemberKind.Property && m.Name == name,
			'F' => m.Kind == ApiMemberKind.Field && m.Name == name,
			_ => m.Kind == ApiMemberKind.Event && m.Name == name
		}).ToList();

		if (candidates.Count == 0)
		{
			return null;
		}

		// A method ID leaves out the parentheses when there are no parameters.
		ApiMember member = candidates.Count == 1
			? candidates[0]
			: PickOverload(candidates, kind == 'M' ? parameters ?? "" : parameters, parent);
		return MemberTarget(parent, member, current);
	}

	/// <summary>
	///     Picks the overload whose parameter types match the ID, then one with the same parameter count,
	///     then the first.
	/// </summary>
	private static ApiMember PickOverload(List<ApiMember> candidates, string? parameterList, ApiType parent)
	{
		if (parameterList is null)
		{
			return candidates[0];
		}

		List<string> wanted = SplitParameters(parameterList).Select(NormalizeIdParameter).ToList();
		List<ApiMember> sameCount = candidates.Where(m => m.Parameters.Count == wanted.Count).ToList();

		ApiMember? exact = sameCount.FirstOrDefault(m =>
		{
			var typeParameters = new HashSet<string>(
				parent.TypeParameters.Concat(m.TypeParameters).Select(tp => tp.Name), StringComparer.Ordinal);
			return m.Parameters.Select(p => NormalizeModelParameter(p.Type, typeParameters)).SequenceEqual(wanted);
		});

		return exact ?? sameCount.FirstOrDefault() ?? candidates[0];
	}

	/// <summary>
	///     Resolves a reference the compiler couldn't bind, such as <c>!:SiteConfig.Build</c>.
	/// </summary>
	/// <remarks>
	///     The build compiles each configured project on its own, so a cref to a type in another project
	///     never binds. A name that matches exactly one documented type still gets a link.
	/// </remarks>
	private CrefTarget? ResolveUnbound(string text, ApiType current)
	{
		int open = text.IndexOf('(');
		string path = GenericArgumentsToArity((open >= 0 ? text[..open] : text).Trim().Replace('{', '<').Replace('}', '>'));
		if (path.Length == 0 || path.Any(char.IsWhiteSpace))
		{
			return null;
		}

		if (FindTypeByName(path) is { } type)
		{
			return TypeTarget(type);
		}

		int dot = path.LastIndexOf('.');
		if (dot > 0 && FindTypeByName(path[..dot]) is { } parent)
		{
			string name = ArityMarkerRegex().Replace(path[(dot + 1)..], "");
			if (parent.Members.FirstOrDefault(m => m.Kind != ApiMemberKind.Constructor && m.Name == name) is { } member)
			{
				return MemberTarget(parent, member, current);
			}
		}

		return null;
	}

	private ApiType? FindTypeByName(string path)
	{
		string name = path[(path.LastIndexOf('.') + 1)..];
		if (!_typesByName.TryGetValue(name, out List<(string Id, ApiType Type)>? sameName))
		{
			return null;
		}

		(string Id, ApiType Type)[] matches = sameName
			.Where(t => t.Id == path || t.Id.EndsWith("." + path, StringComparison.Ordinal))
			.Take(2)
			.ToArray();
		return matches.Length == 1 ? matches[0].Type : null;
	}

	private CrefTarget MemberTarget(ApiType parent, ApiMember member, ApiType current)
	{
		// A member without a detail block has no anchor of its own, but its section always exists.
		string anchor = GetAnchors(parent).TryGetValue(member, out string? id) ? id : ApiPageRenderer.SectionId(member.Kind);
		string href = ReferenceEquals(parent, current) ? $"#{anchor}" : $"{TypeRoute(parent)}#{anchor}";
		string text = member.Kind == ApiMemberKind.Constructor
			? parent.Name
			: $"{parent.Name}.{ApiPageRenderer.MemberDisplayName(member, parent)}";
		return new CrefTarget(href, text);
	}

	private static CrefTarget TypeTarget(ApiType type) =>
		new(TypeRoute(type), type.TypeParameters.Count == 0
			? type.Name
			: $"{type.Name}<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>");

	/// <summary>
	///     Links a <c>System.*</c> or <c>Microsoft.*</c> ID to its page on learn.microsoft.com, where
	///     <c>List`1</c> is <c>list-1</c>, constructors are <c>-ctor</c> and overloads share a page.
	/// </summary>
	private static CrefTarget? LearnTarget(string id)
	{
		string body = id[2..];
		int open = body.IndexOf('(');
		if (open >= 0)
		{
			body = body[..open];
		}

		bool framework = body.StartsWith("System.", StringComparison.Ordinal) ||
		                 body.StartsWith("Microsoft.", StringComparison.Ordinal) ||
		                 (id[0] == 'N' && body is "System" or "Microsoft");
		if (!framework)
		{
			return null;
		}

		string path = body;
		if (id[0] is 'M' or 'P' or 'F' or 'E')
		{
			int dot = body.LastIndexOf('.');
			string member = body[(dot + 1)..];
			path = body[..dot] + "." + (member == "#ctor" ? "-ctor" : ArityMarkerRegex().Replace(member, ""));
		}

		// Explicit interface implementations (#) and static constructors (#cctor) have no page of their own.
		if (!LearnPathRegex().IsMatch(path))
		{
			return null;
		}

		return new CrefTarget(_learnBaseUrl + TypeArityRegex().Replace(path, "-$1").ToLowerInvariant(),
			XmlDocParser.GetCrefDisplayName(id));
	}

	/// <summary>
	///     The documentation ID of a type without its <c>T:</c> prefix, such as <c>Ns.Outer`1.Inner</c>.
	/// </summary>
	private static string TypeId(ApiType type)
	{
		// Roslyn's full name spells type parameters out (Ns.Outer<T>.Inner). The reflection host's full
		// name leaves them off, so its arity comes from the parameter count.
		if (type.FullName.Contains('<'))
		{
			return GenericArgumentsToArity(type.FullName);
		}

		return type.TypeParameters.Count > 0 ? $"{type.FullName}`{type.TypeParameters.Count}" : type.FullName;
	}

	/// <summary>
	///     Mirrors the route that <c>CSharpAnalysisPhase</c> and <c>ReflectionApiPagePhase</c> give a type page.
	/// </summary>
	private static string TypeRoute(ApiType type)
	{
		string name = type.Name.Replace('<', '-').Replace('>', '-').Replace('`', '-').TrimEnd('-');
		return $"/api/{(type.Namespace ?? "(global)").Replace('.', '/')}/{name}".ToLowerInvariant();
	}

	/// <summary>
	///     Replaces each generic argument list with its arity: <c>Dictionary&lt;K, List&lt;V&gt;&gt;</c>
	///     becomes <c>Dictionary`2</c>.
	/// </summary>
	private static string GenericArgumentsToArity(string name)
	{
		var sb = new StringBuilder(name.Length);
		int depth = 0;
		int commas = 0;

		foreach (char ch in name)
		{
			switch (ch)
			{
				case '<':
					if (depth++ == 0)
					{
						commas = 0;
					}

					break;

				case '>' when depth > 0:
					if (--depth == 0)
					{
						sb.Append('`').Append(commas + 1);
					}

					break;

				case ',' when depth == 1:
					commas++;
					break;

				default:
					if (depth == 0)
					{
						sb.Append(ch);
					}

					break;
			}
		}

		return sb.ToString();
	}

	private static List<string> SplitParameters(string parameterList)
	{
		var result = new List<string>();
		if (parameterList.Length == 0)
		{
			return result;
		}

		int depth = 0;
		int start = 0;
		for (int i = 0; i < parameterList.Length; i++)
		{
			switch (parameterList[i])
			{
				case '{' or '[' or '(':
					depth++;
					break;

				case '}' or ']' or ')':
					depth--;
					break;

				case ',' when depth == 0:
					result.Add(parameterList[start..i]);
					start = i + 1;
					break;
			}
		}

		result.Add(parameterList[start..]);
		return result;
	}

	// Documentation IDs write System.Collections.Generic.List{`0} and System.Int32@ for a ref int. In a
	// parameter list, `0 and ``0 stand for type parameters.
	private static string NormalizeIdParameter(string parameter) =>
		CanonicalTypeName(ArityMarkerRegex()
			.Replace(parameter.TrimEnd('@').Replace('{', '<').Replace('}', '>'), "#"));

	// The model writes List<T>, int? and, for types the compiler couldn't resolve, whatever the source said.
	private static string NormalizeModelParameter(string parameter, HashSet<string> typeParameters) =>
		CanonicalTypeName(IdentifierRegex()
			.Replace(parameter, m => typeParameters.Contains(m.Value) ? "#" : m.Value)
			.Replace("?", ""));

	private static string CanonicalTypeName(string type)
	{
		string name = NamespaceQualifierRegex().Replace(WhitespaceRegex().Replace(type, ""), "");
		name = NullableRegex().Replace(name, "$1");
		return IdentifierRegex().Replace(name, m => _clrTypeKeywords.GetValueOrDefault(m.Value, m.Value));
	}

	private static bool TryParseLink(string entry, out string text, out string target)
	{
		text = "";
		target = "";
		if (entry.Length < 4 || entry[0] != '[' || entry[^1] != ')')
		{
			return false;
		}

		int split = entry.LastIndexOf("](", StringComparison.Ordinal);
		if (split < 1)
		{
			return false;
		}

		target = entry[(split + 2)..^1].Trim();
		text = entry[1..split].Trim();
		if (text.Length == 0)
		{
			text = target;
		}

		return target.Length > 0;
	}

	private static bool IsDocumentationId(string value) => DocumentationIdRegex().IsMatch(value);

	private static bool IsSafeUrl(string url)
	{
		Match scheme = UrlSchemeRegex().Match(url);
		return !scheme.Success || scheme.Groups[1].Value.ToLowerInvariant() is "http" or "https" or "mailto";
	}

	private static string Link(string href, string text) =>
		$"<a href=\"{HttpUtility.HtmlAttributeEncode(href)}\">{Esc(text)}</a>";

	private static string Esc(string text) => HttpUtility.HtmlEncode(text);

	[GeneratedRegex("""<a data-cref="(?<id>[^"]*)">(?<text>[^<]*)</a>""")]
	private static partial Regex CrefAnchorRegex();

	[GeneratedRegex(@"^(?:[TNMPFE]:\S+|!:\S.*)$")]
	private static partial Regex DocumentationIdRegex();

	[GeneratedRegex(@"`{1,2}\d+")]
	private static partial Regex ArityMarkerRegex();

	[GeneratedRegex(@"`(\d+)")]
	private static partial Regex TypeArityRegex();

	[GeneratedRegex(@"^[A-Za-z0-9_.`-]+$")]
	private static partial Regex LearnPathRegex();

	[GeneratedRegex(@"(?:[A-Za-z_][A-Za-z0-9_]*\.)+(?=[A-Za-z_#])")]
	private static partial Regex NamespaceQualifierRegex();

	[GeneratedRegex(@"\bNullable<([^<>]*)>")]
	private static partial Regex NullableRegex();

	[GeneratedRegex("[A-Za-z_][A-Za-z0-9_]*")]
	private static partial Regex IdentifierRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex("^([A-Za-z][A-Za-z0-9+.-]*):")]
	private static partial Regex UrlSchemeRegex();

	private sealed record CrefTarget(string Href, string Text);
}
