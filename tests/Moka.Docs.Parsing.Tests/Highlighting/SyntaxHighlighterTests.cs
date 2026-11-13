using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Parsing.Highlighting;

namespace Moka.Docs.Parsing.Tests.Highlighting;

public sealed class SyntaxHighlighterTests
{
	// Each expected string is what the theme's browser highlighter made of the same code before
	// highlighting moved to the build, with quotes escaped as Markdig escapes them. Sites keep their
	// colors only while these hold.
	private static readonly (string Language, string Code, string Expected)[] _browserOutput =
	[
		("csharp", "using System;\n/* block\n   comment */\npublic sealed record Point(int X, int Y);\nvar list = new List<string> { \"a\\\"b\", 'c' };\nif (a<b && b>c) { return 1.5f + 0x1F + 10ul; } // trailing\nDictionary<string, List<int>> map = new();\nstring s = $\"{x} & {y}\"; café = 1;", "<span class=\"token keyword\">using</span> <span class=\"token type-name\">System</span>;\n<span class=\"token comment\">/* block\n   comment */</span>\n<span class=\"token keyword\">public</span> <span class=\"token keyword\">sealed</span> <span class=\"token keyword\">record</span> <span class=\"token type-name\">Point</span>(<span class=\"token keyword\">int</span> <span class=\"token type-name\">X</span>, <span class=\"token keyword\">int</span> <span class=\"token type-name\">Y</span>);\n<span class=\"token keyword\">var</span> list = <span class=\"token keyword\">new</span> <span class=\"token type-name\">List</span>&lt;<span class=\"token keyword\">string</span>&gt; { <span class=\"token string\">&quot;a\\&quot;b&quot;</span>, <span class=\"token string\">'c'</span> };\n<span class=\"token keyword\">if</span> (a&lt;b &amp;&amp; b&gt;c) { <span class=\"token keyword\">return</span> <span class=\"token number\">1.5f</span> + 0x1F + <span class=\"token number\">10ul</span>; } <span class=\"token comment\">// trailing</span>\n<span class=\"token type-name\">Dictionary</span>&lt;<span class=\"token keyword\">string</span>, <span class=\"token type-name\">List</span>&lt;<span class=\"token keyword\">int</span>&gt;&gt; map = <span class=\"token keyword\">new</span>();\n<span class=\"token keyword\">string</span> s = $<span class=\"token string\">&quot;{x} &amp; {y}&quot;</span>; café = <span class=\"token number\">1</span>;"),
		("cs", "public async Task<int> RunAsync() => await Foo<Bar>();", "<span class=\"token keyword\">public</span> <span class=\"token keyword\">async</span> <span class=\"token type-name\">Task</span>&lt;<span class=\"token keyword\">int</span>&gt; <span class=\"token type-name\">RunAsync</span>() =&gt; <span class=\"token keyword\">await</span> <span class=\"token type-name\">Foo</span>&lt;<span class=\"token type-name\">Bar</span>&gt;();"),
		("javascript", "const a = `tpl ${x}`; let b = 'q'; // c\n/* m */ function f() { return a < b ? null : undefined; }", "<span class=\"token keyword\">const</span> a = <span class=\"token string\">`tpl ${x}`</span>; <span class=\"token keyword\">let</span> b = <span class=\"token string\">'q'</span>; <span class=\"token comment\">// c</span>\n<span class=\"token comment\">/* m */</span> <span class=\"token keyword\">function</span> f() { <span class=\"token keyword\">return</span> a &lt; b ? <span class=\"token keyword\">null</span> : <span class=\"token keyword\">undefined</span>; }"),
		("js", "export default async function x() { await y(); }", "<span class=\"token keyword\">export</span> <span class=\"token keyword\">default</span> <span class=\"token keyword\">async</span> <span class=\"token keyword\">function</span> x() { <span class=\"token keyword\">await</span> y(); }"),
		("typescript", "interface A { x: number } const y: A = { x: 1 };", "interface A { x: number } <span class=\"token keyword\">const</span> y: A = { x: <span class=\"token number\">1</span> };"),
		("ts", "type T = Array<string>;", "type T = Array&lt;string&gt;;"),
		("jsx", "const el = <div className=\"x\">{a}</div>;", "<span class=\"token keyword\">const</span> el = &lt;div className=<span class=\"token string\">&quot;x&quot;</span>&gt;{a}&lt;/div&gt;;"),
		("tsx", "const el: JSX.Element = <A b=\"c\" />;", "<span class=\"token keyword\">const</span> el: JSX.Element = &lt;A b=<span class=\"token string\">&quot;c&quot;</span> /&gt;;"),
		("html", "<!-- c -->\n<div class=\"a\" id=\"b\">x &amp; y</div>\n<br/><input disabled>", "<span class=\"token comment\">&lt;!-- c --&gt;</span>\n<span class=\"token tag-name\">&lt;div </span>class=<span class=\"token string\">&quot;a&quot;</span><span class=\"token attr-name\"> id=</span><span class=\"token string\">&quot;b&quot;</span>&gt;x &amp;amp; y<span class=\"token tag-name\">&lt;/div&gt;</span>\n&lt;br/&gt;<span class=\"token tag-name\">&lt;input </span>disabled&gt;"),
		("xml", "<?xml version=\"1.0\"?>\n<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>\n</Project>", "&lt;?xml<span class=\"token attr-name\"> version=</span><span class=\"token string\">&quot;1.0&quot;</span>?&gt;\n<span class=\"token tag-name\">&lt;Project </span>Sdk=<span class=\"token string\">&quot;Microsoft.NET.Sdk&quot;</span>&gt;\n  <span class=\"token tag-name\">&lt;PropertyGroup&gt;</span><span class=\"token tag-name\">&lt;TargetFramework&gt;</span>net10.0<span class=\"token tag-name\">&lt;/TargetFramework&gt;</span><span class=\"token tag-name\">&lt;/PropertyGroup&gt;</span>\n<span class=\"token tag-name\">&lt;/Project&gt;</span>"),
		("css", "@media (max-width: 768px) {\n  .a { color: red !important; margin: 0 1.5rem; }\n}\n/* c */ a::before { content: \"x\"; }", "<span class=\"token keyword\">@media</span> (<span class=\"token property\">max-width</span>: <span class=\"token number\">768px</span>) {\n  .a { <span class=\"token property\">color</span>: red <span class=\"token keyword\">!important</span>; <span class=\"token property\">margin</span>: <span class=\"token number\">0</span> <span class=\"token number\">1.5rem</span>; }\n}\n<span class=\"token comment\">/* c */</span> <span class=\"token property\">a</span>::before { <span class=\"token property\">content</span>: <span class=\"token string\">&quot;x&quot;</span>; }"),
		("json", "{\n  \"a\": 1.5,\n  \"b\": [true, false, null],\n  \"c\": \"x\\\"y\"\n}", "{\n  <span class=\"token string\">&quot;a&quot;</span>: <span class=\"token number\">1.5</span>,\n  <span class=\"token string\">&quot;b&quot;</span>: [<span class=\"token keyword\">true</span>, <span class=\"token keyword\">false</span>, <span class=\"token keyword\">null</span>],\n  <span class=\"token string\">&quot;c&quot;</span>: <span class=\"token string\">&quot;x\\&quot;y&quot;</span>\n}"),
		("yaml", "site:\n  title: \"My Docs\" # comment\n  enabled: true\n  count: 3\n  name: 'x'", "site:\n  title: <span class=\"token string\">&quot;My Docs&quot;</span> <span class=\"token comment\"># comment</span>\n  enabled: <span class=\"token keyword\">true</span>\n  count: <span class=\"token number\">3</span>\n  name: <span class=\"token string\">'x'</span>"),
		("yml", "a: yes\nb: no", "a: <span class=\"token keyword\">yes</span>\nb: <span class=\"token keyword\">no</span>"),
		("bash", "#!/bin/bash\nif [ -f x ]; then echo \"hi $USER\"; fi\ndotnet build -c Release # build\nexport A='b'", "<span class=\"token comment\">#!/bin/bash</span>\n<span class=\"token keyword\">if</span> [ -f x ]; <span class=\"token keyword\">then</span> <span class=\"token keyword\">echo</span> <span class=\"token string\">&quot;hi $USER&quot;</span>; <span class=\"token keyword\">fi</span>\n<span class=\"token keyword\">dotnet</span> build -c Release <span class=\"token comment\"># build</span>\n<span class=\"token keyword\">export</span> A=<span class=\"token string\">'b'</span>"),
		("sh", "cd /tmp && ls -la", "<span class=\"token keyword\">cd</span> /tmp &amp;&amp; <span class=\"token keyword\">ls</span> -la"),
		("shell", "npm install && git status", "<span class=\"token keyword\">npm</span> install &amp;&amp; <span class=\"token keyword\">git</span> status"),
		("python", "def f(self, x=1):\n    \"\"\"doc\"\"\"\n    return None if x is not True else 'y'  # c\nprint(f\"{x}\")", "<span class=\"token keyword\">def</span> f(<span class=\"token keyword\">self</span>, x=<span class=\"token number\">1</span>):\n    <span class=\"token string\">&quot;&quot;</span><span class=\"token string\">&quot;doc&quot;</span><span class=\"token string\">&quot;&quot;</span>\n    <span class=\"token keyword\">return</span> <span class=\"token keyword\">None</span> <span class=\"token keyword\">if</span> x <span class=\"token keyword\">is</span> <span class=\"token keyword\">not</span> <span class=\"token keyword\">True</span> <span class=\"token keyword\">else</span> <span class=\"token string\">'y'</span>  <span class=\"token comment\"># c</span>\n<span class=\"token keyword\">print</span>(f<span class=\"token string\">&quot;{x}&quot;</span>)"),
		("py", "import os\nclass A: pass", "<span class=\"token keyword\">import</span> os\n<span class=\"token keyword\">class</span> A: <span class=\"token keyword\">pass</span>"),
		("sql", "SELECT a, COUNT(*) FROM t WHERE x = 'y' AND b > 1 -- c\nselect * from u order by id limit 10;", "<span class=\"token keyword\">SELECT</span> a, <span class=\"token keyword\">COUNT</span>(*) <span class=\"token keyword\">FROM</span> t <span class=\"token keyword\">WHERE</span> x = <span class=\"token string\">'y'</span> <span class=\"token keyword\">AND</span> b &gt; <span class=\"token number\">1</span> <span class=\"token comment\">-- c</span>\n<span class=\"token keyword\">select</span> * <span class=\"token keyword\">from</span> u <span class=\"token keyword\">order</span> <span class=\"token keyword\">by</span> id <span class=\"token keyword\">limit</span> <span class=\"token number\">10</span>;"),
		("rust", "fn main() { let mut v: Vec<i32> = Vec::new(); v.push(1i32); // c\n println!(\"{}\", v.len()); }", "<span class=\"token keyword\">fn</span> main() { <span class=\"token keyword\">let</span> <span class=\"token keyword\">mut</span> v: <span class=\"token keyword\">Vec</span>&lt;i32&gt; = <span class=\"token keyword\">Vec</span>::new(); v.push(<span class=\"token number\">1i32</span>); <span class=\"token comment\">// c</span>\n println!(<span class=\"token string\">&quot;{}&quot;</span>, v.len()); }"),
		("go", "package main\nimport \"fmt\"\nfunc main() { s := `raw`; fmt.Println(s, nil) } /* c */", "<span class=\"token keyword\">package</span> main\n<span class=\"token keyword\">import</span> <span class=\"token string\">&quot;fmt&quot;</span>\n<span class=\"token keyword\">func</span> main() { s := <span class=\"token string\">`raw`</span>; fmt.Println(s, <span class=\"token keyword\">nil</span>) } <span class=\"token comment\">/* c */</span>"),
		("razor", "@page \"/counter\"\n@* comment *@\n<button class=\"btn\" @onclick=\"Inc\">Click</button>\n@code {\n    private int count = 0;\n    void Inc() => count++;\n}", "@page <span class=\"token string\">&quot;/counter&quot;</span>\n<span class=\"token comment\">@* comment *@</span>\n<span class=\"token tag-name\">&lt;button </span><span class=\"token keyword\">class</span>=<span class=\"token string\">&quot;btn&quot;</span><span class=\"token attr-name\"> @onclick=</span><span class=\"token string\">&quot;Inc&quot;</span>&gt;<span class=\"token type-name\">Click</span><span class=\"token tag-name\">&lt;/button&gt;</span>\n@code {\n    <span class=\"token keyword\">private</span> <span class=\"token keyword\">int</span> count = <span class=\"token number\">0</span>;\n    <span class=\"token keyword\">void</span> <span class=\"token type-name\">Inc</span>() =&gt; count++;\n}"),
		("blazor", "<h3>Hello, @Name!</h3>", "<span class=\"token tag-name\">&lt;h3&gt;</span><span class=\"token type-name\">Hello</span>, @<span class=\"token type-name\">Name</span>!<span class=\"token tag-name\">&lt;/h3&gt;</span>"),
		("cshtml", "@model Foo\n<p>@Model.Bar</p>", "@model <span class=\"token type-name\">Foo</span>\n<span class=\"token tag-name\">&lt;p&gt;</span>@<span class=\"token type-name\">Model</span>.<span class=\"token type-name\">Bar</span><span class=\"token tag-name\">&lt;/p&gt;</span>"),
		("java", "public final class A extends B implements C { long x = 10L; String s = \"y\"; }", "<span class=\"token keyword\">public</span> <span class=\"token keyword\">final</span> <span class=\"token keyword\">class</span> <span class=\"token type-name\">A</span> <span class=\"token keyword\">extends</span> <span class=\"token type-name\">B</span> <span class=\"token keyword\">implements</span> <span class=\"token type-name\">C</span> { <span class=\"token keyword\">long</span> x = <span class=\"token number\">10L</span>; <span class=\"token type-name\">String</span> s = <span class=\"token string\">&quot;y&quot;</span>; }"),
		("kotlin", "data class P(val x: Int) { fun f() = when (x) { 1 -> \"a\" else -> null } }", "<span class=\"token keyword\">data</span> <span class=\"token keyword\">class</span> <span class=\"token type-name\">P</span>(<span class=\"token keyword\">val</span> x: <span class=\"token type-name\">Int</span>) { <span class=\"token keyword\">fun</span> f() = <span class=\"token keyword\">when</span> (x) { <span class=\"token number\">1</span> -&gt; <span class=\"token string\">&quot;a&quot;</span> <span class=\"token keyword\">else</span> -&gt; <span class=\"token keyword\">null</span> } }"),
		("kt", "val x = 1L", "<span class=\"token keyword\">val</span> x = <span class=\"token number\">1L</span>"),
		("swift", "struct S: Codable { let x: Int; func f() async throws -> String { return \"\" } }", "<span class=\"token keyword\">struct</span> <span class=\"token type-name\">S</span>: <span class=\"token type-name\">Codable</span> { <span class=\"token keyword\">let</span> x: <span class=\"token type-name\">Int</span>; <span class=\"token keyword\">func</span> f() <span class=\"token keyword\">async</span> <span class=\"token keyword\">throws</span> -&gt; <span class=\"token type-name\">String</span> { <span class=\"token keyword\">return</span> <span class=\"token string\">&quot;&quot;</span> } }"),
		("php", "<?php\n// c\n# d\nfunction f($a) { return isset($a) ? 'x' : \"y\"; }", "&lt;?php\n<span class=\"token comment\">// c</span>\n<span class=\"token comment\"># d</span>\n<span class=\"token keyword\">function</span> f($a) { <span class=\"token keyword\">return</span> <span class=\"token keyword\">isset</span>($a) ? <span class=\"token string\">'x'</span> : <span class=\"token string\">&quot;y&quot;</span>; }"),
		("ruby", "class A < B\n  def f(x) = x.nil? ? 'a' : \"b\" # c\nend\nputs defined?(x)", "<span class=\"token keyword\">class</span> A &lt; B\n  <span class=\"token keyword\">def</span> f(x) = x.<span class=\"token keyword\">nil</span>? ? <span class=\"token string\">'a'</span> : <span class=\"token string\">&quot;b&quot;</span> <span class=\"token comment\"># c</span>\n<span class=\"token keyword\">end</span>\n<span class=\"token keyword\">puts</span> defined?(x)"),
		("rb", "require 'json'", "<span class=\"token keyword\">require</span> <span class=\"token string\">'json'</span>"),
		("docker", "FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build\nWORKDIR /src\nCOPY . .\nRUN dotnet publish -c Release # c\nENTRYPOINT [\"dotnet\", \"app.dll\"]", "<span class=\"token keyword\">FROM</span> mcr.microsoft.com/dotnet/sdk:10.0 <span class=\"token keyword\">AS</span> build\n<span class=\"token keyword\">WORKDIR</span> /src\n<span class=\"token keyword\">COPY</span> . .\n<span class=\"token keyword\">RUN</span> dotnet publish -c Release <span class=\"token comment\"># c</span>\n<span class=\"token keyword\">ENTRYPOINT</span> [<span class=\"token string\">&quot;dotnet&quot;</span>, <span class=\"token string\">&quot;app.dll&quot;</span>]"),
		("dockerfile", "FROM alpine\nENV A=b", "<span class=\"token keyword\">FROM</span> alpine\n<span class=\"token keyword\">ENV</span> A=b"),
		("powershell", "function Get-X { param([string]$Name) if ($Name) { return \"Hi $Name\" } } # c\nforeach ($i in 1..3) { Write-Host $i }", "<span class=\"token keyword\">function</span> Get-X { <span class=\"token keyword\">param</span>([string]$Name) <span class=\"token keyword\">if</span> ($Name) { <span class=\"token keyword\">return</span> <span class=\"token string\">&quot;Hi $Name&quot;</span> } } <span class=\"token comment\"># c</span>\n<span class=\"token keyword\">foreach</span> ($i <span class=\"token keyword\">in</span> <span class=\"token number\">1</span>..<span class=\"token number\">3</span>) { Write-Host $i }"),
		("ps1", "$x = 'a'", "$x = <span class=\"token string\">'a'</span>"),
		("pwsh", "Get-ChildItem | Where-Object { $_.Length -gt 10 }", "Get-ChildItem | Where-Object { $_.Length -gt <span class=\"token number\">10</span> }"),
		("toml", "[package]\nname = \"x\" # c\nversion = '1.0'\nenabled = true\ncount = 3", "[package]\n<span class=\"token property\">name</span> = <span class=\"token string\">&quot;x&quot;</span> <span class=\"token comment\"># c</span>\n<span class=\"token property\">version</span> = <span class=\"token string\">'1.0'</span>\n<span class=\"token property\">enabled</span> = <span class=\"token keyword\">true</span>\n<span class=\"token property\">count</span> = <span class=\"token number\">3</span>"),
		("fsharp", "let f x = x + 1 // c\n(* block *)\ntype P = { X: int }\nlet s = \"a\"", "<span class=\"token keyword\">let</span> f x = x + <span class=\"token number\">1</span> <span class=\"token comment\">// c</span>\n<span class=\"token comment\">(* block *)</span>\n<span class=\"token keyword\">type</span> <span class=\"token type-name\">P</span> = { <span class=\"token type-name\">X</span>: int }\n<span class=\"token keyword\">let</span> s = <span class=\"token string\">&quot;a&quot;</span>"),
		("fs", "open System\nmatch x with | Some y -> y | None -> 0", "<span class=\"token keyword\">open</span> <span class=\"token type-name\">System</span>\n<span class=\"token keyword\">match</span> x <span class=\"token keyword\">with</span> | <span class=\"token type-name\">Some</span> y -&gt; y | <span class=\"token type-name\">None</span> -&gt; <span class=\"token number\">0</span>"),
		("markdown", "# Title\n## Sub\nSome [link](http://x.y) and <!-- c -->\n- item", "<span class=\"token keyword\"># Title</span>\n<span class=\"token keyword\">## Sub</span>\nSome <span class=\"token string\">[link](http://x.y)</span> and <span class=\"token comment\">&lt;!-- c --&gt;</span>\n- item"),
		("md", "### H3\n[a](b)", "<span class=\"token keyword\">### H3</span>\n<span class=\"token string\">[a](b)</span>"),
		("diff", "@@ -1,3 +1,3 @@\n context\n-old line\n+new line", "<span class=\"token comment\">@@ -1,3 +1,3 @@</span>\n context\n<span class=\"token string\">-old line</span>\n<span class=\"token keyword\">+new line</span>"),
		("ini", "; comment\n[section]\nkey = \"value\"\n# other\nname.x = 1", "<span class=\"token comment\">; comment</span>\n[section]\n<span class=\"token property\">key</span> = <span class=\"token string\">&quot;value&quot;</span>\n<span class=\"token comment\"># other</span>\n<span class=\"token property\">name.x</span> = 1"),
		("cfg", "a=b", "<span class=\"token property\">a</span>=b"),
		("conf", "x.y = 'z'", "<span class=\"token property\">x.y</span> = <span class=\"token string\">'z'</span>"),
		("graphql", "type Query { user(id: ID!): User } # c\nquery { user(id: \"1\") { name } }", "<span class=\"token keyword\">type</span> Query { user(id: ID!): User } <span class=\"token comment\"># c</span>\n<span class=\"token keyword\">query</span> { user(id: <span class=\"token string\">&quot;1&quot;</span>) { name } }"),
		("gql", "mutation M { x }", "<span class=\"token keyword\">mutation</span> M { x }"),
		("proto", "syntax = \"proto3\";\nmessage M { int32 id = 1; repeated string names = 2; } // c", "<span class=\"token keyword\">syntax</span> = <span class=\"token string\">&quot;proto3&quot;</span>;\n<span class=\"token keyword\">message</span> M { <span class=\"token type-name\">int32</span> id = <span class=\"token number\">1</span>; <span class=\"token keyword\">repeated</span> <span class=\"token type-name\">string</span> names = <span class=\"token number\">2</span>; } <span class=\"token comment\">// c</span>"),
		("protobuf", "enum E { A = 0; }", "<span class=\"token keyword\">enum</span> E { A = <span class=\"token number\">0</span>; }"),
		("csharp", "var s = \"unterminated\nnext line\"; List<int x = 1;", "<span class=\"token keyword\">var</span> s = <span class=\"token string\">&quot;unterminated\nnext line&quot;</span>; <span class=\"token type-name\">List</span>&lt;<span class=\"token keyword\">int</span> x = <span class=\"token number\">1</span>;"),
		("csharp", "\n\n", "\n\n"),
		("html", "<a href=\"x\">a<b</a>", "<span class=\"token tag-name\">&lt;a </span>href=<span class=\"token string\">&quot;x&quot;</span>&gt;a&lt;b<span class=\"token tag-name\">&lt;/a&gt;</span>"),
	];

	public static TheoryData<string, string, string> BrowserOutput()
	{
		var data = new TheoryData<string, string, string>();
		foreach ((string language, string code, string expected) in _browserOutput)
		{
			data.Add(language, code, expected);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(BrowserOutput))]
	public void Highlight_GivesTheBrowserHighlightersOutput(string language, string code, string expected)
	{
		SyntaxHighlighter.Highlight(code, language).Should().Be(expected);
	}

	[Fact]
	public void BrowserOutput_CoversEveryLanguage()
	{
		HashSet<string?> covered = _browserOutput.Select(row => SyntaxHighlighter.ResolveLanguage(row.Language)).ToHashSet();

		SyntaxHighlighter.Languages.Should().HaveCount(26).And.OnlyContain(language => covered.Contains(language));
	}

	[Theory]
	[InlineData("csharp", "csharp")]
	[InlineData("CSharp", "csharp")]
	[InlineData("cs", "csharp")]
	[InlineData("c#", "csharp")]
	[InlineData("ts", "javascript")]
	[InlineData("tsx", "javascript")]
	[InlineData("xml", "html")]
	[InlineData("shell", "bash")]
	[InlineData("dockerfile", "docker")]
	[InlineData("pwsh", "powershell")]
	[InlineData("protobuf", "proto")]
	[InlineData("conf", "ini")]
	[InlineData("gql", "graphql")]
	[InlineData("text", null)]
	[InlineData("mermaid", null)]
	[InlineData("", null)]
	[InlineData(null, null)]
	public void ResolveLanguage_MapsAliases(string? language, string? expected)
	{
		SyntaxHighlighter.ResolveLanguage(language).Should().Be(expected);
	}

	[Theory]
	[InlineData("csharp", "if (a<b) { x = \"<br/>\"; }")]
	[InlineData("html", "<a href=\"x\">a<b & c</a>")]
	[InlineData("text", "a<b & c>d \"q\"")]
	[InlineData(null, "</code><script>alert(1)</script>")]
	public void Highlight_EscapesEverythingOutsideItsOwnMarkup(string? language, string code)
	{
		string html = SyntaxHighlighter.Highlight(code, language);

		string withoutTokens = Regex.Replace(html, "<span class=\"token [a-z-]+\">|</span>", "");
		withoutTokens.Should().NotContainAny("<", ">", "\"");
		WebUtility.HtmlDecode(withoutTokens).Should().Be(code);
	}

	[Fact]
	public void Highlight_LeavesUnknownLanguagesAsEscapedText()
	{
		SyntaxHighlighter.Highlight("a<b && \"c\"", "cobol").Should().Be("a&lt;b &amp;&amp; &quot;c&quot;");
	}

	[Fact]
	public void HighlightLines_ClosesATokenAtEachLineEnd()
	{
		IReadOnlyList<string> lines = SyntaxHighlighter.HighlightLines("int a; /* one\ntwo */ int b;\n\nend", "csharp");

		lines.Should().Equal(
			"<span class=\"token keyword\">int</span> a; <span class=\"token comment\">/* one</span>",
			"<span class=\"token comment\">two */</span> <span class=\"token keyword\">int</span> b;",
			"",
			"end");
	}

	[Theory]
	[MemberData(nameof(BrowserOutput))]
	public void HighlightLines_HasTheSameTokensAsHighlight(string language, string code, string expected)
	{
		IReadOnlyList<string> lines = SyntaxHighlighter.HighlightLines(code, language);

		lines.Should().HaveCount(code.Split('\n').Length);
		ClassPerCharacter(string.Join('\n', lines)).Should().Be(ClassPerCharacter(expected));
	}

	[Fact]
	public void HighlightLines_GivesOneEmptyLineForEmptyCode()
	{
		SyntaxHighlighter.HighlightLines("", "csharp").Should().Equal("");
	}

	[Fact]
	public void Highlight_StillReturnsTheCodeWhenAPatternTakesTooLong()
	{
		// Every "A<" starts a type name that scans to the end for a '>': quadratic, so the match
		// times out and the block is shown without colors rather than stalling the build.
		string code = string.Concat(Enumerable.Repeat("A<", 60_000));

		string html = SyntaxHighlighter.Highlight(code, "csharp");

		WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")).Should().Be(code);
	}

	// The token class of every character but line breaks, so a token split at a line end compares
	// equal to the same token in one piece.
	private static string ClassPerCharacter(string html)
	{
		var classes = new StringBuilder();
		string current = ".";
		for (int i = 0; i < html.Length; i++)
		{
			if (html[i] == '<')
			{
				int end = html.IndexOf('>', i);
				string tag = html[i..(end + 1)];
				current = tag.StartsWith("</", StringComparison.Ordinal) ? "." : tag;
				i = end;
				continue;
			}

			if (html[i] != '\n')
			{
				classes.Append(current).Append('|');
			}
		}

		return classes.ToString();
	}
}
