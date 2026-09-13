using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Parsing.FrontMatter;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Integration.Tests.Cli;

public sealed class NewCommandTests
{
	[Theory]
	[InlineData("Api: v2")]
	[InlineData("C# #1 tips")]
	[InlineData("- dashes first")]
	[InlineData("Getting Started")]
	public void BuildPageMarkdown_ProducesFrontMatterThatParsesBackToTheTitle(string title)
	{
		// Written raw, "Api: v2" made the front matter fail to parse.
		string markdown = NewCommand.BuildPageMarkdown(title, "default", 2);

		FrontMatterResult result = new FrontMatterExtractor().Extract(markdown);

		result.FrontMatter.Title.Should().Be(title);
		result.FrontMatter.Order.Should().Be(2);
	}

	[Theory]
	[InlineData("MyCustomPlugin", "my-custom-plugin")]
	[InlineData("my-custom-plugin", "my-custom-plugin")]
	[InlineData("Footer2Plugin", "footer2-plugin")]
	[InlineData("footer", "footer")]
	public void ToKebabCase_GivesTheSameIdHoweverTheNameIsTyped(string name, string expected) =>
		NewCommand.ToKebabCase(name).Should().Be(expected);

	[Theory]
	[InlineData("MyCustom", "MyCustomPlugin")]
	[InlineData("MyCustomPlugin", "MyCustomPlugin")]
	[InlineData("Footer2", "Footer2Plugin")]
	[InlineData("Plugin", "PluginPlugin")]
	public void ToPluginClassName_AddsThePluginSuffixOnlyWhenMissing(string pascalName, string expected) =>
		NewCommand.ToPluginClassName(pascalName).Should().Be(expected);

	[Theory]
	[InlineData("MyCustomPlugin")]
	[InlineData("my-custom-plugin")]
	public async Task NewPlugin_NameEndingInPlugin_DoesNotDoubleTheSuffix(string name)
	{
		// The class and its file were named MyCustomPluginPlugin.
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-newplugin-" + Guid.NewGuid().ToString("N"));
		try
		{
			int exitCode = await NewCommand.Create().Parse(["plugin", name, "--path", root]).InvokeAsync();

			string projectDir = Path.Combine(root, "Moka.Docs.Plugins.MyCustomPlugin");
			exitCode.Should().Be(0);
			Directory.GetFiles(projectDir).Select(Path.GetFileName)
				.Should().BeEquivalentTo("Moka.Docs.Plugins.MyCustomPlugin.csproj", "MyCustomPlugin.cs");
			File.ReadAllText(Path.Combine(projectDir, "MyCustomPlugin.cs"))
				.Should().Contain("public sealed class MyCustomPlugin : IMokaPlugin")
				.And.Contain("public string Id => \"mokadocs-my-custom-plugin\";");
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, true);
			}
		}
	}

	[Fact]
	public void CardTemplate_UsesOnlyIconsAndVariantsThatRender()
	{
		// It used variant="outlined" and "filled" and the icons square and palette. None of them
		// exist, so two of the three example cards had no icon and a style with no CSS.
		string template = NewCommand.ComponentTemplates["card"];

		string html = new MarkdownParser().Parse(template).Html;

		Regex.Matches(html, "class=\"component-card-icon\"").Count
			.Should().Be(Regex.Matches(template, "icon=\"").Count);
		Regex.Matches(html, "class=\"component-card component-card-(\\w+)\"")
			.Select(m => m.Groups[1].Value)
			.Should().HaveCount(Regex.Matches(template, "variant=\"").Count)
			.And.OnlyContain(v => v == "info" || v == "success" || v == "warning");
	}

	[Fact]
	public void ComponentTemplates_LeaveNoUnparsedContainerFences()
	{
		foreach ((string name, string template) in NewCommand.ComponentTemplates)
		{
			string html = new MarkdownParser().Parse(template).Html;

			html.Should().NotContain(":::", $"the {name} example should render as a component");
		}
	}
}
