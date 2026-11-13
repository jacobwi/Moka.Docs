using System.Collections.Frozen;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Parsing.Highlighting;

internal static class CodeLanguages
{
	private static readonly FrozenDictionary<string, string> _displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["csharp"] = "C#",
		["cs"] = "C#",
		["c#"] = "C#",
		["fsharp"] = "F#",
		["fs"] = "F#",
		["javascript"] = "JavaScript",
		["js"] = "JavaScript",
		["jsx"] = "JSX",
		["typescript"] = "TypeScript",
		["ts"] = "TypeScript",
		["tsx"] = "TSX",
		["python"] = "Python",
		["py"] = "Python",
		["java"] = "Java",
		["kotlin"] = "Kotlin",
		["kt"] = "Kotlin",
		["swift"] = "Swift",
		["go"] = "Go",
		["rust"] = "Rust",
		["ruby"] = "Ruby",
		["rb"] = "Ruby",
		["php"] = "PHP",
		["razor"] = "Razor",
		["blazor"] = "Razor",
		["cshtml"] = "Razor",
		["html"] = "HTML",
		["xml"] = "XML",
		["css"] = "CSS",
		["json"] = "JSON",
		["yaml"] = "YAML",
		["yml"] = "YAML",
		["toml"] = "TOML",
		["ini"] = "INI",
		["cfg"] = "INI",
		["conf"] = "INI",
		["bash"] = "Bash",
		["sh"] = "Shell",
		["shell"] = "Shell",
		["powershell"] = "PowerShell",
		["ps1"] = "PowerShell",
		["pwsh"] = "PowerShell",
		["sql"] = "SQL",
		["graphql"] = "GraphQL",
		["gql"] = "GraphQL",
		["proto"] = "Protocol Buffers",
		["protobuf"] = "Protocol Buffers",
		["docker"] = "Dockerfile",
		["dockerfile"] = "Dockerfile",
		["markdown"] = "Markdown",
		["md"] = "Markdown",
		["diff"] = "Diff"
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	public static string Describe(string? language, ThemeText text)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return text["code.labelPlain"];
		}

		string name = _displayNames.TryGetValue(language, out string? display) ? display : language;
		return text.Format("code.label", ("language", name));
	}
}
