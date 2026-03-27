using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests;

public sealed class BlazorPreviewServiceTests
{
	private readonly BlazorPreviewService _service = new(
		NullLogger<BlazorPreviewService>.Instance);

	#region Basic Rendering

	[Fact]
	public async Task RenderAsync_SimpleMarkup_ReturnsHtml()
	{
		const string source = "<h1>Hello World</h1>";

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("<h1>Hello World</h1>");
	}

	[Fact]
	public async Task RenderAsync_EmptySource_ReturnsError()
	{
		BlazorPreviewResult result = await _service.RenderAsync("");

		result.Error.Should().NotBeNull();
		result.Html.Should().BeNull();
	}

	[Fact]
	public async Task RenderAsync_NullSource_ReturnsError()
	{
		BlazorPreviewResult result = await _service.RenderAsync(null!);

		result.Error.Should().NotBeNull();
	}

	[Fact]
	public async Task RenderAsync_WhitespaceSource_ReturnsError()
	{
		BlazorPreviewResult result = await _service.RenderAsync("   ");

		result.Error.Should().NotBeNull();
	}

	[Fact]
	public async Task RenderAsync_ExceedsMaxLength_ReturnsError()
	{
		string source = new('x', 50_001);

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().Contain("exceeds maximum length");
	}

	#endregion

	#region Variable Substitution

	[Fact]
	public async Task RenderAsync_VariableSubstitution_ReplacesStringField()
	{
		const string source = """
		                      <h1>@title</h1>

		                      @code {
		                          private string title = "Hello World";
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("Hello World");
		result.Html.Should().NotContain("@title");
	}

	[Fact]
	public async Task RenderAsync_VariableSubstitution_ReplacesIntField()
	{
		const string source = """
		                      <p>Count: @count</p>

		                      @code {
		                          private int count = 42;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("Count: 42");
	}

	[Fact]
	public async Task RenderAsync_VariableSubstitution_ReplacesBoolField()
	{
		const string source = """
		                      <p>Active: @isActive</p>

		                      @code {
		                          private bool isActive = true;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("Active: True");
	}

	[Fact]
	public async Task RenderAsync_UnknownVariable_LeftAsIs()
	{
		const string source = """
		                      <p>@unknownVar</p>

		                      @code {
		                          private string name = "Test";
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("@unknownVar");
	}

	#endregion

	#region Directive Stripping

	[Fact]
	public async Task RenderAsync_StripsOnClickDirective()
	{
		const string source = """
		                      <button @onclick="HandleClick">Click me</button>

		                      @code {
		                          private void HandleClick() { }
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("<button");
		result.Html.Should().Contain("Click me");
		result.Html.Should().NotContain("@onclick");
	}

	[Fact]
	public async Task RenderAsync_StripsBindDirective()
	{
		const string source = """
		                      <input @bind="name" />

		                      @code {
		                          private string name = "Test";
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("@bind");
	}

	[Fact]
	public async Task RenderAsync_StripsRefDirective()
	{
		const string source = """
		                      <div @ref="myDiv">Content</div>

		                      @code {
		                          private ElementReference myDiv;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("@ref");
	}

	#endregion

	#region @if Block Evaluation

	[Fact]
	public async Task RenderAsync_IfBlock_TruthyValue_ShowsContent()
	{
		const string source = """
		                      @if (showMessage) {<p>Visible!</p>}

		                      @code {
		                          private bool showMessage = true;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("Visible!");
		result.Html.Should().NotContain("@if");
	}

	[Fact]
	public async Task RenderAsync_IfBlock_FalsyValue_HidesContent()
	{
		const string source = """
		                      @if (showMessage) {<p>Hidden!</p>}

		                      @code {
		                          private bool showMessage = false;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("Hidden!");
	}

	[Fact]
	public async Task RenderAsync_IfBlock_ZeroIsFalsy()
	{
		const string source = """
		                      @if (count) {<p>Has items</p>}

		                      @code {
		                          private int count = 0;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("Has items");
	}

	[Fact]
	public async Task RenderAsync_IfBlock_UnknownVariable_HidesContent()
	{
		const string source = """
		                      @if (unknown) {<p>Should be hidden</p>}

		                      @code {
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("Should be hidden");
	}

	#endregion

	#region @foreach Block Iteration

	[Fact]
	public async Task RenderAsync_ForeachBlock_IteratesCollection()
	{
		const string source = """
		                      <ul>
		                      @foreach (var item in items) {<li>@item</li>}
		                      </ul>

		                      @code {
		                          private List<string> items = new() { "Apple", "Banana", "Cherry" };
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("Apple");
		result.Html.Should().Contain("Banana");
		result.Html.Should().Contain("Cherry");
		result.Html.Should().NotContain("@foreach");
	}

	[Fact]
	public async Task RenderAsync_ForeachBlock_UnknownCollection_ProducesNothing()
	{
		const string source = """
		                      @foreach (var item in unknownList) {<li>@item</li>}

		                      @code {
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("<li>");
	}

	#endregion

	#region Field Initializer Parsing

	[Fact]
	public async Task RenderAsync_DoubleField_ResolvedCorrectly()
	{
		const string source = """
		                      <p>@price</p>

		                      @code {
		                          private double price = 9.99;
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("9.99");
	}

	[Fact]
	public async Task RenderAsync_NoCodeBlock_JustMarkup()
	{
		const string source = "<div><p>Simple markup without code</p></div>";

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().Contain("<div><p>Simple markup without code</p></div>");
	}

	[Fact]
	public async Task RenderAsync_UsingDirectives_StrippedFromOutput()
	{
		const string source = """
		                      @using System.Collections.Generic

		                      <p>Content</p>

		                      @code {
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("@using");
		result.Html.Should().Contain("Content");
	}

	[Fact]
	public async Task RenderAsync_HtmlSpecialChars_Encoded()
	{
		const string source = """
		                      <p>@message</p>

		                      @code {
		                          private string message = "<script>alert('xss')</script>";
		                      }
		                      """;

		BlazorPreviewResult result = await _service.RenderAsync(source, TestContext.Current.CancellationToken);

		result.Error.Should().BeNull();
		result.Html.Should().NotContain("<script>");
		result.Html.Should().Contain("&lt;script&gt;");
	}

	#endregion
}
