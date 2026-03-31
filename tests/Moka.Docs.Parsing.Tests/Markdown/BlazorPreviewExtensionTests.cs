using FluentAssertions;
using Markdig;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Parsing.Tests.Markdown;

public sealed class BlazorPreviewExtensionTests
{
	private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
		.Use<BlazorPreviewExtension>()
		.Build();

	private static string Render(string md) => Markdig.Markdown.ToHtml(md, _pipeline);

	[Fact]
	public void BlazorPreviewBlock_ProducesPreviewContainer()
	{
		const string md = """
		                  ```blazor-preview
		                  <h1>Hello @name</h1>
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("class=\"blazor-preview-container\"");
		html.Should().Contain("data-blazor-preview=\"true\"");
		html.Should().Contain("blazor-preview-source");
		html.Should().Contain("blazor-preview-render");
		html.Should().Contain("class=\"language-razor\"");
	}

	[Fact]
	public void RazorPreviewAlias_ProducesPreviewContainer()
	{
		const string md = """
		                  ```razor-preview
		                  <p>Razor content</p>
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("blazor-preview-container");
		html.Should().Contain("data-blazor-preview=\"true\"");
	}

	[Fact]
	public void RegularCsharpBlock_NotAffected()
	{
		const string md = """
		                  ```csharp
		                  Console.WriteLine("Hello");
		                  ```
		                  """;

		string html = Render(md);

		html.Should().NotContain("blazor-preview-container");
		html.Should().NotContain("data-blazor-preview");
	}

	[Fact]
	public void BlazorPreview_HasSourceAndRenderSections()
	{
		const string md = """
		                  ```blazor-preview
		                  <button>Click me</button>
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("blazor-preview-source");
		html.Should().Contain("blazor-preview-render");
	}

	[Fact]
	public void BlazorPreview_HtmlEncodesContent()
	{
		const string md = """
		                  ```blazor-preview
		                  <div class="test">Content</div>
		                  ```
		                  """;

		string html = Render(md);

		// The source code section should have HTML-encoded content
		html.Should().Contain("blazor-preview-source");
		html.Should().Contain("&lt;div");
	}

	[Fact]
	public void EmptyBlazorPreviewBlock_StillRendersContainer()
	{
		const string md = """
		                  ```blazor-preview
		                  ```
		                  """;

		string html = Render(md);

		html.Should().Contain("blazor-preview-container");
	}

	[Fact]
	public void RegularRazorBlock_NotAffected()
	{
		const string md = """
		                  ```razor
		                  <p>Regular razor</p>
		                  ```
		                  """;

		string html = Render(md);

		html.Should().NotContain("blazor-preview-container");
	}
}
