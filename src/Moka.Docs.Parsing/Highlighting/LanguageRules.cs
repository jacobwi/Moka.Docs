using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Moka.Docs.Parsing.Highlighting;

// The token rules the theme's browser highlighter used before 1.10, ported as written so built pages
// keep their colors. The patterns are JavaScript regular expressions: RegexOptions.ECMAScript keeps
// \w, \d, \s and \b to ASCII as they were there.
internal static class LanguageRules
{
	private static readonly TimeSpan _matchTimeout = TimeSpan.FromSeconds(2);

	private static readonly FrozenDictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["typescript"] = "javascript",
		["xml"] = "html",
		["sh"] = "bash",
		["shell"] = "bash",
		["cs"] = "csharp",
		["c#"] = "csharp",
		["js"] = "javascript",
		["ts"] = "javascript",
		["py"] = "python",
		["yml"] = "yaml",
		["blazor"] = "razor",
		["cshtml"] = "razor",
		["jsx"] = "javascript",
		["tsx"] = "javascript",
		["kt"] = "kotlin",
		["rb"] = "ruby",
		["dockerfile"] = "docker",
		["ps1"] = "powershell",
		["pwsh"] = "powershell",
		["fs"] = "fsharp",
		["md"] = "markdown",
		["protobuf"] = "proto",
		["cfg"] = "ini",
		["conf"] = "ini",
		["gql"] = "graphql"
	}.ToFrozenDictionary(StringComparer.Ordinal);

	// Rules run in this order, and a token found by an earlier rule wins a tie with a later one
	// that starts and ends at the same place.
	private static readonly FrozenDictionary<string, Lazy<TokenRule[]>> _rules = new Dictionary<string, RuleSpec[]>(StringComparer.Ordinal)
	{
		["csharp"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(using|namespace|class|struct|record|interface|enum|delegate|public|private|protected|internal|static|sealed|abstract|virtual|override|async|await|new|return|if|else|for|foreach|while|do|switch|case|break|continue|try|catch|finally|throw|var|const|readonly|ref|out|in|is|as|typeof|sizeof|nameof|this|base|null|true|false|void|int|string|bool|double|float|long|short|byte|char|decimal|object|dynamic|where|select|from|get|set|init|required|event|partial|yield|lock|params|stackalloc|when|not|and|or|with|record)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?(f|d|m|L|u|ul)?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
		],
		["javascript"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"(`(?:[^`\\]|\\.)*`|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')", "g"),
			Rule("keyword", @"\b(const|let|var|function|return|if|else|for|while|do|switch|case|break|continue|try|catch|finally|throw|new|typeof|instanceof|in|of|class|extends|import|export|default|from|async|await|yield|this|super|null|undefined|true|false|void|delete|debugger)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["html"] =
		[
			Rule("comment", @"<!--[\s\S]*?-->", "gm"),
			Rule("string", @"""[^""]*""", "g"),
			Rule("tag-name", @"<\/?[a-zA-Z][a-zA-Z0-9-]*(?:\s|>)", "g"),
			Rule("attr-name", @"\s[a-zA-Z-]+=(?="")", "g"),
		],
		["css"] =
		[
			Rule("comment", @"\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""[^""]*""|'[^']*'", "g"),
			Rule("keyword", @"(@media|@import|@keyframes|@font-face|@supports|!important)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?(px|em|rem|%|vh|vw|s|ms|deg|fr)?\b", "g"),
			Rule("property", @"[\w-]+(?=\s*:)", "g"),
		],
		["json"] =
		[
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(true|false|null)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["yaml"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(true|false|null|yes|no)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["bash"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'[^']*'", "g"),
			Rule("keyword", @"\b(if|then|else|elif|fi|for|while|do|done|case|esac|function|return|in|echo|exit|export|source|cd|ls|rm|cp|mv|mkdir|grep|sed|awk|cat|sudo|apt|npm|dotnet|git|docker|pip|curl|wget)\b", "g"),
			Rule("number", @"\b\d+\b", "g"),
		],
		["python"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(def|class|if|elif|else|for|while|try|except|finally|with|as|import|from|return|yield|raise|pass|break|continue|and|or|not|in|is|None|True|False|self|lambda|global|nonlocal|async|await|print)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["sql"] =
		[
			Rule("comment", @"--.*$", "gm"),
			Rule("string", @"'[^']*'", "g"),
			Rule("keyword", @"\b(SELECT|FROM|WHERE|INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|TABLE|INDEX|VIEW|JOIN|LEFT|RIGHT|INNER|OUTER|ON|AND|OR|NOT|IN|IS|NULL|AS|ORDER|BY|GROUP|HAVING|LIMIT|OFFSET|UNION|INTO|VALUES|SET|DISTINCT|COUNT|SUM|AVG|MAX|MIN|LIKE|BETWEEN|EXISTS|CASE|WHEN|THEN|ELSE|END|PRIMARY|KEY|FOREIGN|REFERENCES|CASCADE)\b", "gi"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["rust"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(fn|let|mut|const|if|else|for|while|loop|match|return|struct|enum|impl|trait|pub|use|mod|self|super|crate|where|async|await|move|ref|true|false|Some|None|Ok|Err|Box|Vec|String|Option|Result)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?(f32|f64|i32|i64|u32|u64|usize|isize)?\b", "g"),
		],
		["go"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"(""(?:[^""\\]|\\.)*""|`[^`]*`)", "g"),
			Rule("keyword", @"\b(func|var|const|type|struct|interface|map|chan|if|else|for|range|switch|case|default|return|break|continue|go|defer|select|package|import|true|false|nil|make|new|len|cap|append|delete|panic|recover)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["razor"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/|@\*[\s\S]*?\*@", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(using|namespace|class|public|private|protected|if|else|for|foreach|while|switch|case|return|var|new|async|await|true|false|null|void|int|string|bool|double|this|base|typeof|readonly|static|partial|override|virtual|abstract|sealed|record|yield|throw|try|catch|finally|get|set|init)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
			Rule("tag-name", @"<\/?[a-zA-Z][a-zA-Z0-9-]*(?:\s|>)", "g"),
			Rule("attr-name", @"\s[a-zA-Z@:-]+=(?="")", "g"),
		],
		["java"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|null|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|var|void|volatile|while|true|false|record|sealed|permits|yield)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?[lLfFdD]?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
		],
		["kotlin"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(abstract|actual|annotation|as|break|by|catch|class|companion|const|constructor|continue|crossinline|data|do|else|enum|expect|external|false|final|finally|for|fun|get|if|import|in|infix|init|inline|inner|interface|internal|is|it|lateinit|noinline|null|object|open|operator|out|override|package|private|protected|public|reified|return|sealed|set|super|suspend|tailrec|this|throw|true|try|typealias|typeof|val|var|vararg|when|where|while)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?[lLfF]?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
		],
		["swift"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(actor|associatedtype|async|await|break|case|catch|class|continue|default|defer|deinit|do|else|enum|extension|fallthrough|false|fileprivate|for|func|guard|if|import|in|init|inout|internal|is|lazy|let|nil|open|operator|override|private|protocol|public|repeat|rethrows|return|self|Self|some|static|struct|subscript|super|switch|throw|throws|true|try|typealias|var|weak|where|while)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
		],
		["php"] =
		[
			Rule("comment", @"\/\/.*$|\/\*[\s\S]*?\*\/|#.*", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(abstract|and|array|as|break|callable|case|catch|class|clone|const|continue|declare|default|die|do|echo|else|elseif|empty|enddeclare|endfor|endforeach|endif|endswitch|endwhile|eval|exit|extends|final|finally|fn|for|foreach|function|global|goto|if|implements|include|include_once|instanceof|insteadof|interface|isset|list|match|namespace|new|null|or|print|private|protected|public|readonly|require|require_once|return|static|switch|throw|trait|true|false|try|unset|use|var|while|yield|yield from)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["ruby"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", "g"),
			Rule("keyword", @"\b(alias|and|begin|break|case|class|def|defined\?|do|else|elsif|end|ensure|false|for|if|in|module|next|nil|not|or|redo|rescue|retry|return|self|super|then|true|undef|unless|until|when|while|yield|require|include|extend|attr_reader|attr_writer|attr_accessor|puts|print|raise)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["docker"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'[^']*'", "g"),
			Rule("keyword", @"\b(FROM|AS|RUN|CMD|LABEL|EXPOSE|ENV|ADD|COPY|ENTRYPOINT|VOLUME|USER|WORKDIR|ARG|ONBUILD|STOPSIGNAL|HEALTHCHECK|SHELL)\b", "g"),
		],
		["powershell"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'[^']*'", "g"),
			Rule("keyword", @"\b(Begin|Break|Catch|Class|Continue|Data|Define|Do|DynamicParam|Else|ElseIf|End|Enum|Exit|Filter|Finally|For|ForEach|From|Function|Hidden|If|In|InlineScript|Param|Process|Return|Static|Switch|Throw|Trap|Try|Until|Using|Var|While|Workflow|parallel|sequence)\b", "gi"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
		],
		["toml"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'[^']*'", "g"),
			Rule("keyword", @"\b(true|false)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?\b", "g"),
			Rule("property", @"[\w.-]+(?=\s*=)", "g"),
		],
		["fsharp"] =
		[
			Rule("comment", @"\/\/.*$|\(\*[\s\S]*?\*\)", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(abstract|and|as|assert|base|begin|class|default|delegate|do|done|downcast|downto|elif|else|end|exception|extern|false|finally|fixed|for|fun|function|global|if|in|inherit|inline|interface|internal|lazy|let|match|member|module|mutable|namespace|new|not|null|of|open|or|override|private|public|rec|return|select|static|struct|then|to|true|try|type|upcast|use|val|void|when|while|with|yield|async|await|task)\b", "g"),
			Rule("number", @"\b\d+(\.\d+)?[lLmMfF]?\b", "g"),
			Rule("type-name", @"\b([A-Z][a-zA-Z0-9]*(?:<[^>]+>)?)\b", "g"),
		],
		["markdown"] =
		[
			Rule("comment", @"<!--[\s\S]*?-->", "gm"),
			Rule("string", @"\[.*?\]\(.*?\)", "g"),
			Rule("keyword", @"^#{1,6}\s.*", "gm"),
		],
		["diff"] =
		[
			Rule("comment", @"^@@.*", "gm"),
			Rule("string", @"^-.*", "gm"),
			Rule("keyword", @"^\+.*", "gm"),
		],
		["ini"] =
		[
			Rule("comment", @"[;#].*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""|'[^']*'", "g"),
			Rule("property", @"[\w.-]+(?=\s*=)", "g"),
		],
		["graphql"] =
		[
			Rule("comment", @"#.*", "g"),
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(type|query|mutation|subscription|fragment|on|schema|extend|input|interface|union|enum|scalar|directive|implements)\b", "g"),
		],
		["proto"] =
		[
			Rule("comment", @"\/\/.*$", "gm"),
			Rule("string", @"""(?:[^""\\]|\\.)*""", "g"),
			Rule("keyword", @"\b(syntax|package|import|option|message|enum|service|rpc|returns|repeated|optional|required|map|oneof|reserved|extend|true|false)\b", "g"),
			Rule("number", @"\b\d+\b", "g"),
			Rule("type-name", @"\b(int32|int64|uint32|uint64|sint32|sint64|fixed32|fixed64|sfixed32|sfixed64|float|double|bool|string|bytes)\b", "g"),
		],
	}.ToFrozenDictionary(
		pair => pair.Key,
		pair => new Lazy<TokenRule[]>(() => pair.Value.Select(spec => spec.Build()).ToArray()),
		StringComparer.Ordinal);

	internal static IEnumerable<string> Languages => _rules.Keys;

	internal static IReadOnlyDictionary<string, string> Aliases => _aliases;

	internal static string? Resolve(string? language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return null;
		}

		string name = language.Trim().ToLowerInvariant();
		if (_aliases.TryGetValue(name, out string? target))
		{
			name = target;
		}

		return _rules.ContainsKey(name) ? name : null;
	}

	internal static TokenRule[] For(string language) => _rules[language].Value;

	private static RuleSpec Rule(string tokenClass, string pattern, string flags) => new(tokenClass, pattern, flags);

	private sealed record RuleSpec(string TokenClass, string Pattern, string Flags)
	{
		public TokenRule Build()
		{
			RegexOptions options = RegexOptions.ECMAScript;
			if (Flags.Contains('m'))
			{
				options |= RegexOptions.Multiline;
			}

			if (Flags.Contains('i'))
			{
				options |= RegexOptions.IgnoreCase;
			}

			return new TokenRule(TokenClass, new Regex(Pattern, options, _matchTimeout));
		}
	}
}

internal sealed record TokenRule(string TokenClass, Regex Pattern);
