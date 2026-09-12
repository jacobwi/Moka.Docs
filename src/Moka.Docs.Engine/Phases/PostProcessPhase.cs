using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Generates sitemap.xml and robots.txt in the output directory.
/// </summary>
public sealed class PostProcessPhase(ILogger<PostProcessPhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "PostProcess";

	/// <inheritdoc />
	public int Order => 1200;

	/// <inheritdoc />
	public Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		if (context.DryRun)
		{
			return Task.CompletedTask;
		}

		IFileSystem fs = context.FileSystem;
		string outputDir = context.OutputDirectory;

		if (context.Config.Build.Sitemap)
		{
			WriteSitemap(context, fs, outputDir);
		}

		if (context.Config.Build.Robots)
		{
			WriteRobotsTxt(context, fs, outputDir);
		}

		return Task.CompletedTask;
	}

	private void WriteSitemap(BuildContext context, IFileSystem fs, string outputDir)
	{
		SiteConfig config = context.Config;
		if (string.IsNullOrWhiteSpace(config.Site.Url))
		{
			logger.LogDebug("No site URL configured, skipping sitemap generation");
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

		foreach (DocPage page in context.Pages)
		{
			if (page.FrontMatter.Visibility != PageVisibility.Public)
			{
				continue;
			}

			string url = SiteUrls.Absolute(config.Site.Url, config.Build.BasePath, page.Route);
			string lastmod = page.LastModified?.ToString("yyyy-MM-dd") ?? "";

			sb.AppendLine("  <url>");
			sb.AppendLine($"    <loc>{url}</loc>");
			if (!string.IsNullOrEmpty(lastmod))
			{
				sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
			}

			sb.AppendLine("  </url>");
		}

		sb.AppendLine("</urlset>");

		string path = fs.Path.Combine(outputDir, "sitemap.xml");
		fs.File.WriteAllText(path, sb.ToString());
		logger.LogInformation("Generated sitemap.xml with {Count} URLs",
			context.Pages.Count(p => p.FrontMatter.Visibility == PageVisibility.Public));
	}

	private void WriteRobotsTxt(BuildContext context, IFileSystem fs, string outputDir)
	{
		SiteConfig config = context.Config;
		var sb = new StringBuilder();
		sb.AppendLine("User-agent: *");
		sb.AppendLine("Allow: /");

		// Only point at a sitemap this build actually writes.
		if (config.Build.Sitemap && !string.IsNullOrWhiteSpace(config.Site.Url))
		{
			sb.AppendLine($"Sitemap: {SiteUrls.Absolute(config.Site.Url, config.Build.BasePath, "/sitemap.xml")}");
		}

		string path = fs.Path.Combine(outputDir, "robots.txt");
		fs.File.WriteAllText(path, sb.ToString());
		logger.LogInformation("Generated robots.txt");
	}
}
