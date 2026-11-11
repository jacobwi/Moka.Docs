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
	[InlineData("<table><tr><td>Key</td><td>Value</td></tr></table>", "Key Value")]
	[InlineData("  spaced\n  out  ", "spaced out")]
	[InlineData("", "")]
	[InlineData(null, "")]
	public void ToPlainText_StripsTagsAndDecodesEntities(string? html, string expected) =>
		ApiDocText.ToPlainText(html).Should().Be(expected);

	[Theory]
	[InlineData("https://example.com/a?b=c", "https://example.com/a?b=c")]
	[InlineData("HTTP://example.com", "HTTP://example.com")]
	[InlineData("mailto:team@example.com", "mailto:team@example.com")]
	[InlineData("/guide/install", "/guide/install")]
	[InlineData("../other#anchor", "../other#anchor")]
	[InlineData("  https://example.com  ", "https://example.com")]
	[InlineData("https://exa\tmple.com", "https://example.com")]
	public void TryGetSafeUrl_HttpMailtoAndRelativeUrls_AreLinked(string url, string expected)
	{
		ApiDocText.TryGetSafeUrl(url, out string safe).Should().BeTrue();
		safe.Should().Be(expected);
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("JavaScript:alert(1)")]
	[InlineData("java\tscript:alert(3)")]
	[InlineData("java\nscript:alert(3)")]
	[InlineData("java\r\nscript:alert(3)")]
	[InlineData("\u0001javascript:alert(3)")]
	[InlineData("java\u0000script:alert(3)")]
	[InlineData(" \tjavascript:alert(3)")]
	[InlineData("vbscript:msgbox(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	[InlineData("file:///etc/passwd")]
	[InlineData("")]
	[InlineData("\t\n")]
	[InlineData(null)]
	public void TryGetSafeUrl_OtherSchemes_AreNotLinked(string? url)
	{
		// The auditor's payload: java&#9;script: arrives as java<TAB>script:, which browsers run.
		ApiDocText.TryGetSafeUrl(url, out string safe).Should().BeFalse();
		safe.Should().BeEmpty();
	}
}
