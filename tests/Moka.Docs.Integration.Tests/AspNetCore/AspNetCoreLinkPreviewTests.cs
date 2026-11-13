using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     <c>MokaDocsOptions.SiteUrl</c>, <c>SocialImage</c> and <c>Llms</c>, the host's counterparts of
///     <c>site.url</c>, <c>site.image</c> and <c>build.llms</c>.
/// </summary>
public sealed class AspNetCoreLinkPreviewTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-preview-tags-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreLinkPreviewTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\n\nThe home page.\n");
		Write("docs/images/card.png", "png");
		Write("outside.png", "png");
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
	public void Defaults_AreNoSiteUrlNoImageAndMarkdownCopies()
	{
		SiteConfig config = SiteConfigFactory.Create(new MokaDocsOptions());

		config.Site.Url.Should().BeEmpty();
		config.Site.Image.Should().BeNull();
		config.Build.Llms.Should().BeTrue();
	}

	[Theory]
	[InlineData("https://cdn.example.com/card.png", "https://cdn.example.com/card.png")]
	[InlineData("//cdn.example.com/card.png", "//cdn.example.com/card.png")]
	[InlineData("data:image/png;base64,AAAA", "data:image/png;base64,AAAA")]
	public void SocialImage_Url_IsUsedAsWritten(string image, string publishUrl)
	{
		SiteAssetReference? reference = SiteConfigFactory.CreateSocialImage(image, Path.Combine(_root, "docs"));

		reference.Should().NotBeNull();
		reference!.IsAbsoluteUrl.Should().BeTrue();
		reference.SourcePath.Should().BeNull();
		reference.PublishUrl.Should().Be(publishUrl);
	}

	[Theory]
	[InlineData("images/card.png")]
	[InlineData("./images/card.png")]
	[InlineData("/images/card.png")]
	[InlineData("images\\card.png")]
	[InlineData(" images/./card.png ")]
	public void SocialImage_Path_IsAFileUnderTheDocsFolder(string image)
	{
		string docs = Path.Combine(_root, "docs");

		SiteAssetReference? reference = SiteConfigFactory.CreateSocialImage(image, docs);

		reference.Should().NotBeNull();
		reference!.IsAbsoluteUrl.Should().BeFalse();
		reference.SourcePath.Should().Be(Path.Combine(docs, "images", "card.png"));
		reference.PublishUrl.Should().Be("/images/card.png");
	}

	[Fact]
	public void SocialImage_OutsideTheDocsFolder_IsLeftOut()
	{
		string docs = Path.Combine(_root, "docs");

		SiteConfigFactory.CreateSocialImage("../outside.png", docs).Should().BeNull();
		SiteConfigFactory.CreateSocialImage("  ", docs).Should().BeNull();
	}

	[Fact]
	public async Task SiteUrlAndSocialImage_GivePagesCanonicalLinksAndAnAbsoluteImage()
	{
		// The host had no public URL and no image option, so its pages never had link preview tags.
		InMemorySite site = await BuildAsync(options =>
		{
			options.SiteUrl = "https://contoso.com";
			options.SocialImage = "images/card.png";
		});

		string home = Html(site, "index.html");
		home.Should().Contain("<link rel=\"canonical\" href=\"https://contoso.com/docs/\" />")
			.And.Contain("<meta property=\"og:image\" content=\"https://contoso.com/docs/images/card.png\" />")
			.And.Contain("<meta name=\"twitter:card\" content=\"summary_large_image\" />");
		Html(site, "api/index.html").Should().Contain("content=\"https://contoso.com/docs/images/card.png\"");
		site.Files.Keys.Should().Contain("images/card.png");
		Encoding.UTF8.GetString(site.Files["llms.txt"].Content).Should().Contain("(https://contoso.com/docs/");
		site.Diagnostics.Should().NotContain(d => d.Source == "SocialImage" || d.Source == "Discovery");
	}

	[Fact]
	public async Task SiteUrl_StartsWithThePathBase()
	{
		InMemorySite site = await BuildAsync(options =>
		{
			options.SiteUrl = "https://contoso.com/";
			options.PathBase = "/shop";
			options.SocialImage = "https://cdn.example.com/card.png";
		});

		Html(site, "index.html").Should().Contain("<link rel=\"canonical\" href=\"https://contoso.com/shop/docs/\" />")
			.And.Contain("<meta property=\"og:image\" content=\"https://cdn.example.com/card.png\" />");
	}

	[Fact]
	public async Task SocialImage_WithoutSiteUrl_IsReported()
	{
		InMemorySite site = await BuildAsync(options => options.SocialImage = "images/card.png");

		Html(site, "index.html").Should().NotContain("og:image").And.NotContain("rel=\"canonical\"");
		site.Diagnostics.Should().ContainSingle(d => d.Source == "SocialImage")
			.Which.Message.Should().Contain("SiteUrl isn't");
	}

	[Fact]
	public async Task SocialImage_OutsideDocsPath_IsReportedAndNotPublished()
	{
		InMemorySite site = await BuildAsync(options =>
		{
			options.SiteUrl = "https://contoso.com";
			options.SocialImage = "../outside.png";
		});

		Html(site, "index.html").Should().NotContain("og:image").And.Contain("rel=\"canonical\"");
		site.Files.Keys.Should().NotContain(key => key.EndsWith("outside.png", StringComparison.Ordinal));
		site.Diagnostics.Should().ContainSingle(d => d.Source == "SocialImage")
			.Which.Message.Should().Contain("is outside DocsPath");
	}

	[Fact]
	public async Task Llms_Off_PublishesNoMarkdownCopies()
	{
		InMemorySite site = await BuildAsync(options => options.Llms = false);

		site.Files.Keys.Should().NotContain(key => key.EndsWith(".md", StringComparison.Ordinal))
			.And.NotContain("llms.txt").And.NotContain("llms-full.txt");
		Html(site, "index.html").Should().NotContain("data-markdown-url");
	}

	private async Task<InMemorySite> BuildAsync(Action<MokaDocsOptions> configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IHostEnvironment>(new ContentRootEnvironment(_root));
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.DocsPath = "docs";
			configure(options);
		});

		await using ServiceProvider provider = services.BuildServiceProvider();
		return await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);
	}

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	private static string Html(InMemorySite site, string path)
	{
		site.Files.Keys.Should().Contain(path);
		return Encoding.UTF8.GetString(site.Files[path].Content);
	}

	private sealed class ContentRootEnvironment(string contentRoot) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Production;
		public string ApplicationName { get; set; } = "Moka.Docs.Integration.Tests";
		public string ContentRootPath { get; set; } = contentRoot;
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
