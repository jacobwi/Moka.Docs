using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moka.Docs.Engine;

namespace Moka.Docs.Engine.Tests;

public sealed class OutputPathsTests
{
	[Theory]
	[InlineData("/guide/intro")]
	[InlineData("/")]
	[InlineData("/Q&A")]
	[InlineData("/a..b/c")]
	[InlineData("/caf\u00e9 menu")]
	public void FindUnsafeRouteProblem_OrdinaryRoutes_AreFine(string route) =>
		OutputPaths.FindUnsafeRouteProblem(route).Should().BeNull();

	[Theory]
	[InlineData("/../../outside-escape", "'..'")]
	[InlineData("/guide/./intro", "'.'")]
	[InlineData("/faq?lang=en", "query string")]
	[InlineData("/faq#top", "fragment")]
	[InlineData("/bell\u0007", "control character")]
	public void FindUnsafeRouteProblem_RoutesNoUrlCanServe_AreRefused(string route, string reason) =>
		OutputPaths.FindUnsafeRouteProblem(route).Should().Contain(reason);

	[Theory]
	[InlineData("/Admin: Users", ":")]
	[InlineData("/C:\\Users\\x\\escaped", ":")]
	[InlineData("/a*b", "*")]
	[InlineData("/a|b", "|")]
	[InlineData("/a<b>", "<")]
	[InlineData("/say \"hi\"", "\"")]
	[InlineData("/back\\slash", "\\")]
	[InlineData("/trailing./page", "ends with a dot or space")]
	[InlineData("/trailing /page", "ends with a dot or space")]
	public void FindNonPortableRouteProblem_NamesWindowsCannotWrite_AreReported(string route, string reason) =>
		OutputPaths.FindNonPortableRouteProblem(route).Should().Contain(reason);

	[Theory]
	[InlineData("/guide/intro")]
	[InlineData("/v1.2/notes")]
	public void FindNonPortableRouteProblem_PortableRoutes_AreFine(string route) =>
		OutputPaths.FindNonPortableRouteProblem(route).Should().BeNull();

	[Theory]
	[InlineData("/guide/intro", "guide/intro/index.html")]
	[InlineData("/", "index.html")]
	[InlineData("", "index.html")]
	[InlineData("/guide/", "guide/index.html")]
	public void RouteToFilePath_IsDirectoryStyle(string route, string expected) =>
		OutputPaths.RouteToFilePath(route).Should().Be(expected);

	[Fact]
	public void ResolveUnder_PathsInsideTheOutput_AreResolved()
	{
		var fs = new MockFileSystem();
		string output = fs.Path.GetFullPath("/project/_site");

		OutputPaths.ResolveUnder(fs, output, "guide/intro/index.html")
			.Should().Be(fs.Path.GetFullPath("/project/_site/guide/intro/index.html"));
		OutputPaths.ResolveUnder(fs, output + "/", "a/../b.html")
			.Should().Be(fs.Path.GetFullPath("/project/_site/b.html"));
	}

	[Theory]
	[InlineData("../../outside/index.html")]
	[InlineData("../_site-other/index.html")]
	[InlineData("guide/../../index.html")]
	public void ResolveUnder_PathsLeavingTheOutput_AreRefused(string relative)
	{
		var fs = new MockFileSystem();

		OutputPaths.ResolveUnder(fs, fs.Path.GetFullPath("/project/_site"), relative).Should().BeNull();
	}

	[Fact]
	public void ResolveUnder_ARootedPath_IsRefused()
	{
		// Path.Combine drops the output directory when the second part is rooted. A plugin page
		// routed at "C:\...\escaped" was written there, outside the site, on Windows.
		var fs = new MockFileSystem();
		string outside = fs.Path.GetFullPath("/elsewhere/escaped/index.html");

		OutputPaths.ResolveUnder(fs, fs.Path.GetFullPath("/project/_site"), outside).Should().BeNull();
	}
}
