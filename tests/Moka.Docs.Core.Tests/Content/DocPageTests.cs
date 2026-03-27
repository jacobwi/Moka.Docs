using FluentAssertions;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Core.Tests.Content;

public sealed class DocPageTests
{
	[Fact]
	public void DocPage_ToString_ReturnsExpectedFormat()
	{
		var page = new DocPage
		{
			FrontMatter = new FrontMatter { Title = "Getting Started" },
			Content = PageContent.Empty,
			Route = "/getting-started"
		};

		page.ToString().Should().Be("DocPage(/getting-started, Getting Started)");
	}

	[Fact]
	public void FrontMatter_Defaults_AreCorrect()
	{
		var fm = new FrontMatter { Title = "Test" };

		fm.Layout.Should().Be("default");
		fm.Visibility.Should().Be(PageVisibility.Public);
		fm.Toc.Should().BeTrue();
		fm.Expanded.Should().BeTrue();
		fm.Order.Should().Be(0);
		fm.Tags.Should().BeEmpty();
	}
}
