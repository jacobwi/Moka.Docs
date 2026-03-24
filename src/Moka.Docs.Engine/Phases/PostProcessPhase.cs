using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
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
        var fs = context.FileSystem;
        var outputDir = context.OutputDirectory;

        if (context.Config.Build.Sitemap) WriteSitemap(context, fs, outputDir);

        if (context.Config.Build.Robots) WriteRobotsTxt(context, fs, outputDir);

        return Task.CompletedTask;
    }

    private void WriteSitemap(BuildContext context, IFileSystem fs, string outputDir)
    {
        var baseUrl = context.Config.Site.Url.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.LogDebug("No site URL configured, skipping sitemap generation");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var page in context.Pages)
        {
            if (page.FrontMatter.Visibility != PageVisibility.Public) continue;

            var url = baseUrl + page.Route;
            var lastmod = page.LastModified?.ToString("yyyy-MM-dd") ?? "";

            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{url}</loc>");
            if (!string.IsNullOrEmpty(lastmod))
                sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        var path = fs.Path.Combine(outputDir, "sitemap.xml");
        fs.File.WriteAllText(path, sb.ToString());
        logger.LogInformation("Generated sitemap.xml with {Count} URLs",
            context.Pages.Count(p => p.FrontMatter.Visibility == PageVisibility.Public));
    }

    private void WriteRobotsTxt(BuildContext context, IFileSystem fs, string outputDir)
    {
        var baseUrl = context.Config.Site.Url.TrimEnd('/');
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");

        if (!string.IsNullOrEmpty(baseUrl))
            sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        var path = fs.Path.Combine(outputDir, "robots.txt");
        fs.File.WriteAllText(path, sb.ToString());
        logger.LogInformation("Generated robots.txt");
    }
}