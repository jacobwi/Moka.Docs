using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Discovery;
using Moka.Docs.Parsing.Markdown;

namespace Moka.Docs.Engine.Phases;

/// <summary>
///     Parses all discovered Markdown files into <see cref="DocPage" /> objects.
/// </summary>
public sealed class MarkdownParsePhase(
	MarkdownParser markdownParser,
	ILogger<MarkdownParsePhase> logger) : IBuildPhase
{
	/// <inheritdoc />
	public string Name => "MarkdownParse";

	/// <inheritdoc />
	public int Order => 400;

	/// <inheritdoc />
	public async Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		string docsPath = context.FileSystem.Path.GetFullPath(
			context.FileSystem.Path.Combine(context.RootDirectory, context.Config.Content.Docs));

		IReadOnlyDictionary<string, DateTimeOffset> commitDates = await ReadCommitDatesAsync(context, docsPath, ct);

		foreach (string relativePath in context.DiscoveredMarkdownFiles)
		{
			ct.ThrowIfCancellationRequested();

			string fullPath = context.FileSystem.Path.Combine(docsPath, relativePath);
			try
			{
				string markdown = context.FileSystem.File.ReadAllText(fullPath);
				MarkdownParseResult result = markdownParser.Parse(markdown, relativePath);
				if (result.FrontMatterError is { } frontMatterError)
				{
					// The page used to become "Untitled" with its front matter printed as
					// content, and nothing said why.
					context.Diagnostics.Warning(
						$"{relativePath}: front matter could not be read and was ignored ({frontMatterError})", Name);
				}

				string route = BuildRoute(relativePath, result.FrontMatter);

				string gitPath = relativePath.Replace(context.FileSystem.Path.DirectorySeparatorChar, '/');
				DateTimeOffset? lastModified = commitDates.TryGetValue(gitPath, out DateTimeOffset committed)
					? committed
					: GetLastModified(context, fullPath);

				var page = new DocPage
				{
					FrontMatter = result.FrontMatter,
					Content = new PageContent
					{
						Html = result.Html,
						PlainText = result.PlainText
					},
					TableOfContents = result.TableOfContents,
					SourcePath = relativePath,
					Route = route,
					Origin = PageOrigin.Markdown,
					LastModified = lastModified
				};

				context.Pages.Add(page);
			}
			catch (Exception ex)
			{
				context.Diagnostics.Warning($"Failed to parse {relativePath}: {ex.Message}", Name);
				logger.LogWarning(ex, "Failed to parse Markdown file: {Path}", relativePath);
			}
		}

		logger.LogInformation("Parsed {Count} Markdown pages", context.Pages.Count);
	}

	/// <summary>
	///     Last commit dates for the docs folder's files, or an empty map when they can't come
	///     from git. Pages missing from the map use their file's modified time.
	/// </summary>
	private async Task<IReadOnlyDictionary<string, DateTimeOffset>> ReadCommitDatesAsync(
		BuildContext context, string docsPath, CancellationToken ct)
	{
		// Git reads the real disk. The ASP.NET Core host builds over an in-memory file system whose
		// paths don't exist there, and could even match an unrelated folder that does.
		if (context.FileSystem is not FileSystem || context.DiscoveredMarkdownFiles.Count == 0
		                                         || !context.FileSystem.Directory.Exists(docsPath))
		{
			return new Dictionary<string, DateTimeOffset>();
		}

		return await GitCommitDates.ReadAsync(docsPath, logger, ct);
	}

	/// <summary>
	///     Computes the site route for a Markdown file. Public so tooling that reasons about
	///     routes (such as <c>mokadocs doctor</c>) uses exactly the rule the build does.
	/// </summary>
	/// <param name="relativePath">Path of the file relative to the docs directory.</param>
	/// <param name="frontMatter">The file's front matter; a <c>route</c> value wins.</param>
	/// <returns>The root-relative route, e.g. <c>/guide/markdown</c>.</returns>
	public static string BuildRoute(string relativePath, FrontMatter frontMatter)
	{
		// Use custom route from front matter if specified. A route without a leading slash
		// produced relative sidebar links, and a trailing slash matched no nav path.
		if (!string.IsNullOrWhiteSpace(frontMatter.Route))
		{
			string custom = "/" + frontMatter.Route.Trim().Trim('/');
			return custom;
		}

		// Convert file path to URL route
		string route = "/" + relativePath
			.Replace('\\', '/')
			.Replace(".md", "", StringComparison.OrdinalIgnoreCase);

		// index.md → parent directory route
		if (route.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
		{
			route = route[..^6];
		}

		if (string.IsNullOrEmpty(route))
		{
			route = "/";
		}

		return route;
	}

	private static DateTimeOffset? GetLastModified(BuildContext context, string fullPath)
	{
		try
		{
			IFileInfo info = context.FileSystem.FileInfo.New(fullPath);
			return info.Exists ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero) : null;
		}
		catch
		{
			return null;
		}
	}
}
