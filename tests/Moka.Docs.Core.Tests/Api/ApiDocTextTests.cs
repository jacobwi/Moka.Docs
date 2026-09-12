using FluentAssertions;
using Moka.Docs.Core.Api;

namespace Moka.Docs.Core.Tests.Api;

public sealed class ApiDocTextTests
{
	[Theory]
	[InlineData("Returns a <code>List&lt;T&gt;</code>.", "Returns a List<T>.")]
	[InlineData("<p>First.</p><p>Second.</p>", "First. Second.")]
	[InlineData("Line one<br />line two", "Line one line two")]
	[InlineData("Uses <a data-cref=\"T:System.String\">String</a> and &quot;quotes&quot;", "Uses String and \"quotes\"")]
	[InlineData("<ul><li>One</li><li>Two</li></ul>", "One Two")]
	[InlineData("  spaced\n  out  ", "spaced out")]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void ToPlainText_StripsTagsAndDecodesEntities(string? html, string expected) =>
		ApiDocText.ToPlainText(html).Should().Be(expected);
}
