using FluentAssertions;
using Moka.Docs.Cli.Commands;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     <c>mokadocs doctor</c> knew nothing of the blog: a link to its feed was a broken link, and
///     cover images and avatars, which only front matter and mokadocs.yaml name, were orphans.
/// </summary>
public sealed class DoctorBlogTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-doctor-blog-" + Guid.NewGuid().ToString("N"));

	public DoctorBlogTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs", "blog"));
		Directory.CreateDirectory(Path.Combine(_root, "docs", "img"));
		File.WriteAllText(Path.Combine(_root, "mokadocs.yaml"), """
		                                                        site:
		                                                          title: Doctor Blog
		                                                          url: https://example.com
		                                                        content:
		                                                          docs: ./docs
		                                                        features:
		                                                          blog:
		                                                            enabled: true
		                                                            authors:
		                                                              jane:
		                                                                name: Jane
		                                                                avatar: img/jane.png
		                                                        build:
		                                                          cache: false
		                                                        """);
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n# Home\n\nFollow the [feed](/blog/rss.xml).\n");
		File.WriteAllText(Path.Combine(_root, "docs", "blog", "launch.md"),
			"---\ntitle: Launch\ndate: 2026-01-01\nauthor: jane\nimage: ./cover.png\n---\n# Launch\n\nWe launched.\n");
		File.WriteAllBytes(Path.Combine(_root, "docs", "blog", "cover.png"), [0x89, 0x50, 0x4E, 0x47]);
		File.WriteAllBytes(Path.Combine(_root, "docs", "img", "jane.png"), [0x89, 0x50, 0x4E, 0x47]);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task Doctor_AcceptsFeedLinks_CoverImagesAndAvatars()
	{
		int exitCode = await DoctorCommand.Create()
			.Parse(["--config", Path.Combine(_root, "mokadocs.yaml")])
			.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

		exitCode.Should().Be(0, "every check passes: the feed exists, and both images are referenced");
		Directory.Exists(Path.Combine(_root, "_site")).Should().BeFalse("doctor builds without writing");
	}
}
