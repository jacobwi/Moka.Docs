using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Rendering.Scriban;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The embedded layouts lose their own indentation before Scriban parses them. What an expression writes,
///     the page content above all, stays as it is.
/// </summary>
public sealed class TemplateWhitespaceTests
{
	private static string Lf(string text) => text.Replace("\r\n", "\n");

	[Fact]
	public void Indentation_AndBlankLines_Go()
	{
		string compact = TemplateWhitespace.Compact(Lf("""
		                                                   <div>

		                                                       <span>{{ a }}</span>
		                                                       text
		                                                   </div>
		                                                   """));

		compact.Should().Be("<div>\n<span>{{ a }}</span>\ntext\n</div>");
	}

	[Fact]
	public void ALineOfStatementsOnly_JoinsTheNextLine()
	{
		// The line before it still ends with a line break, so the HTML keeps a space where it had one.
		string compact = TemplateWhitespace.Compact(Lf("""
		                                                   <ul>
		                                                       {{ for item in items }}
		                                                       <li>{{ item }}</li>
		                                                       {{ end }}
		                                                       {{ x = 1 }}{{ if x == 1 }}
		                                                       <p>one</p>
		                                                       {{ else if x }}{{ end }}
		                                                   </ul>
		                                                   """));

		compact.Should().Be("<ul>\n{{ for item in items }}<li>{{ item }}</li>\n{{ end }}{{ x = 1 }}{{ if x == 1 }}<p>one</p>\n{{ else if x }}{{ end }}</ul>");
	}

	[Fact]
	public void TemplateTextInsideAMultiLinePreTextareaScriptOrStyle_StaysAsWritten()
	{
		string compact = TemplateWhitespace.Compact(string.Join("\r\n",
			"    <pre>  first  ",
			"        second",
			"",
			"    </pre>",
			"        <script>",
			"            if (a)",
			"                b();",
			"        </script>",
			"        <style>p { color: red; }</style>",
			"        <textarea>",
			"          kept  ",
			"        </textarea><p>"));

		compact.Should().Be("<pre>  first  \n        second\n\n    </pre>\n<script>\n            if (a)\n                b();\n        </script>\n"
		                    + "<style>p { color: red; }</style>\n<textarea>\n          kept  \n        </textarea><p>");
	}

	[Fact]
	public void DefaultLayout_WritesThePageContentExactly()
	{
		// The indentation used to come from the layout; content written by an expression never had any added
		// (AutoIndent is off) and must not lose any either.
		const string content = "<pre><code>    indented\n\n\tand tabbed   </code></pre>\n  <p>  spaced  </p>";
		var engine = new ScribanTemplateEngine(NullLogger<ScribanTemplateEngine>.Instance);
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();
		var page = new DocPage
		{
			Route = "/guide",
			FrontMatter = new FrontMatter { Title = "Guide" },
			Content = new PageContent { Html = content }
		};
		var context = new ThemeRenderContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "Test" } },
			Templates = theme.Templates,
			Partials = theme.Partials,
			CssFiles = theme.CssFiles,
			JsFiles = theme.JsFiles,
			AllPages = [page]
		};

		string html = engine.RenderPage(page, context, TestContext.Current.CancellationToken);

		html.Should().Contain("\n" + content + "\n");
		html.Replace(content, "", StringComparison.Ordinal).Should().NotContain("\n ").And.NotContain("\n\t").And.NotContain("\r");
	}

	[Fact]
	public void EmbeddedLayouts_HaveNoIndentationLeft()
	{
		ThemeRenderContext theme = EmbeddedThemeProvider.CreateDefault();

		foreach (string template in theme.Templates.Values.Append(EmbeddedThemeProvider.GetSidebarTemplate()))
		{
			template.Should().NotContain("\n ").And.NotContain("\n\t").And.NotContain("\n\n").And.NotContain("\r");
		}
	}
}
