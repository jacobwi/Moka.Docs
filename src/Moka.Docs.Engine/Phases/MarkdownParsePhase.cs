using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Core.Theming;
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

		GitFileHistory history = await ReadGitHistoryAsync(context, docsPath, ct);
		ContentReferences references = BuildReferences(context, docsPath);

		List<string> files = context.DiscoveredMarkdownFiles;
		var parsed = new ParsedFile[files.Count];

		// The real disk is read in parallel too. MockFileSystem, used by the ASP.NET Core host,
		// is read up front on this thread and only the parsing runs in parallel.
		bool concurrentReads = context.FileSystem is FileSystem;
		string?[] preloaded = new string?[files.Count];
		if (!concurrentReads)
		{
			for (int i = 0; i < files.Count; i++)
			{
				preloaded[i] = TryRead(context, docsPath, files[i], out Exception? readError);
				if (readError is not null)
				{
					parsed[i] = new ParsedFile(null, null, readError);
				}
			}
		}

		Parallel.For(0, files.Count, new ParallelOptions { CancellationToken = ct }, i =>
		{
			if (parsed[i] is not null)
			{
				return;
			}

			Exception? readError = null;
			string? markdown = concurrentReads
				? TryRead(context, docsPath, files[i], out readError)
				: preloaded[i];
			if (markdown is null)
			{
				parsed[i] = new ParsedFile(null, null, readError);
				return;
			}

			try
			{
				MarkdownParseResult result = markdownParser.Parse(markdown, files[i], references);
				DateTimeOffset? modified = concurrentReads ? GetLastModified(context, docsPath, files[i]) : null;
				parsed[i] = new ParsedFile(result, modified, null);
			}
			catch (Exception ex)
			{
				parsed[i] = new ParsedFile(null, null, ex);
			}
		});

		// Pages and diagnostics are added in discovery order, so the output does not depend on
		// which thread finished first.
		for (int i = 0; i < files.Count; i++)
		{
			AddPage(context, docsPath, files[i], parsed[i], history, concurrentReads);
		}

		logger.LogInformation("Parsed {Count} Markdown pages", context.Pages.Count);
	}

	private void AddPage(BuildContext context, string docsPath, string relativePath, ParsedFile parsed,
		GitFileHistory history, bool modifiedTimeRead)
	{
		if (parsed.Result is not { } result)
		{
			// A page that fails to parse is missing from the site, which is broken output.
			string reason = parsed.Error?.Message ?? "unknown error";
			context.Diagnostics.Error($"{relativePath}: the page could not be parsed and was left out ({reason})",
				Name);
			logger.LogWarning(parsed.Error, "Failed to parse Markdown file: {Path}", relativePath);
			return;
		}

		if (result.FrontMatterError is { } frontMatterError)
		{
			// The page used to become "Untitled" with its front matter printed as
			// content, and nothing said why.
			context.Diagnostics.Warning(
				$"{relativePath}: front matter could not be read and was ignored ({frontMatterError})", Name);
		}

		foreach (string warning in result.Warnings)
		{
			context.Diagnostics.Warning($"{relativePath}: {warning}", Name);
		}

		FrontMatter frontMatter = result.FrontMatter;
		if (!string.IsNullOrWhiteSpace(frontMatter.Route) && FindRouteOverrideProblem(frontMatter.Route) is { } problem)
		{
			context.Diagnostics.Error(
				$"{relativePath}: front matter route '{frontMatter.Route}' is not allowed ({problem}); " +
				"the page uses its file path instead", Name);
			frontMatter = frontMatter with { Route = null };
		}

		// Resolved here, once, so the link preview image and the blog's cover image agree on a path
		// written relative to the page's file.
		if (frontMatter.Image is { } image && RelativeLinks.ToRootRelative(image, relativePath) is { } rootedImage)
		{
			frontMatter = frontMatter with { Image = rootedImage };
		}

		if (frontMatter.HeroImage is { } heroImage && RelativeLinks.ToRootRelative(heroImage, relativePath) is { } rootedHeroImage)
		{
			frontMatter = frontMatter with { HeroImage = rootedHeroImage };
		}

		string route = BuildRoute(relativePath, frontMatter);

		string gitPath = relativePath.Replace('\\', '/');
		DateTimeOffset? lastModified = history.Dates.TryGetValue(gitPath, out DateTimeOffset committed)
			? committed
			: modifiedTimeRead
				? parsed.LastModified
				: GetLastModified(context, docsPath, relativePath);

		context.Pages.Add(new DocPage
		{
			FrontMatter = frontMatter,
			Content = new PageContent
			{
				Html = result.Html,
				PlainText = result.PlainText
			},
			TableOfContents = result.TableOfContents,
			SourcePath = relativePath,
			Route = route,
			Origin = PageOrigin.Markdown,
			LastModified = lastModified,
			Contributors = history.Authors.TryGetValue(gitPath, out IReadOnlyList<PageContributor>? contributors)
				? contributors
				: []
		});
	}

	/// <summary>
	///     What the pages can refer to: the API model (C# analysis and the ASP.NET Core host's reflection both
	///     run before this phase) for <c>xref:</c> links, the glossary, and the documented package's version.
	/// </summary>
	private ContentReferences BuildReferences(BuildContext context, string docsPath)
	{
		var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (context.PackageInfo is { } package)
		{
			versions[package.Name] = package.Version;
		}

		return new ContentReferences
		{
			ResolveXref = ApiCrefResolver.XrefResolver(context.ApiModel),
			Glossary = ReadGlossary(context, docsPath),
			PackageVersions = versions,
			Text = ThemeText.For(context.Config)
		};
	}

	/// <summary>
	///     The glossary from mokadocs.yaml, plus a glossary.yml (or .yaml) file at the root of the docs folder.
	///     A term in both keeps the mokadocs.yaml definition.
	/// </summary>
	private Dictionary<string, string> ReadGlossary(BuildContext context, string docsPath)
	{
		var glossary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach ((string term, string definition) in context.Config.Glossary)
		{
			if (!glossary.TryAdd(term, definition))
			{
				context.Diagnostics.Warning(
					$"mokadocs.yaml: the glossary has '{term}' more than once (terms ignore case); the first definition is used", Name);
			}
		}

		foreach (string name in GlossaryFile.FileNames)
		{
			string path = context.FileSystem.Path.Combine(docsPath, name);
			if (!context.FileSystem.File.Exists(path))
			{
				continue;
			}

			string yaml;
			try
			{
				yaml = context.FileSystem.File.ReadAllText(path);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				context.Diagnostics.Warning($"{name}: the glossary could not be read and was ignored ({ex.Message})", Name);
				continue;
			}

			GlossaryReadResult file = GlossaryFile.Parse(yaml);
			foreach (string warning in file.Warnings)
			{
				context.Diagnostics.Warning($"{name}: {warning}", Name);
			}

			foreach ((string term, string definition) in file.Terms)
			{
				if (!glossary.TryAdd(term, definition))
				{
					context.Diagnostics.Warning(
						$"{name}: '{term}' is already in the glossary (terms ignore case); the definition read first is used", Name);
				}
			}
		}

		return glossary;
	}

	/// <summary>
	///     Explains why a front matter <c>route:</c> can't be used, or returns <c>null</c>. A route
	///     becomes a folder under the output directory, so <c>..</c> segments, characters that
	///     some operating system forbids in file names, query strings and fragments are refused.
	/// </summary>
	/// <param name="route">The route as written in front matter.</param>
	/// <returns>The reason, or <c>null</c> when the route is fine.</returns>
	public static string? FindRouteOverrideProblem(string route)
	{
		string trimmed = route.Trim();
		return OutputPaths.FindUnsafeRouteProblem(trimmed) ?? OutputPaths.FindNonPortableRouteProblem(trimmed);
	}

	/// <summary>
	///     Last commit dates for the docs folder's files, and their authors when
	///     <c>theme.options.showContributors</c> is on, or empty maps when they can't come from git.
	///     Pages missing from the dates use their file's modified time.
	/// </summary>
	private async Task<GitFileHistory> ReadGitHistoryAsync(BuildContext context, string docsPath, CancellationToken ct)
	{
		// Git reads the real disk. The ASP.NET Core host builds over an in-memory file system whose
		// paths don't exist there, and could even match an unrelated folder that does.
		if (context.FileSystem is not FileSystem || context.DiscoveredMarkdownFiles.Count == 0
		                                         || !context.FileSystem.Directory.Exists(docsPath))
		{
			return GitFileHistory.Empty;
		}

		var wanted = context.DiscoveredMarkdownFiles.Select(f => f.Replace('\\', '/')).ToList();
		return await GitCommitDates.ReadHistoryAsync(docsPath, logger, ct, wanted,
			context.Config.Theme.Options.ShowContributors);
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

		// Only the extension goes. Removing ".md" anywhere turned notes.md-old/page.md into
		// /notes-old/page.
		string path = relativePath.Replace('\\', '/');
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
		{
			path = path[..^3];
		}

		string route = "/" + path;

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

	private static string? TryRead(BuildContext context, string docsPath, string relativePath, out Exception? error)
	{
		error = null;
		try
		{
			return context.FileSystem.File.ReadAllText(context.FileSystem.Path.Combine(docsPath, relativePath));
		}
		catch (Exception ex)
		{
			error = ex;
			return null;
		}
	}

	private static DateTimeOffset? GetLastModified(BuildContext context, string docsPath, string relativePath)
	{
		try
		{
			IFileInfo info = context.FileSystem.FileInfo.New(context.FileSystem.Path.Combine(docsPath, relativePath));
			return info.Exists ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero) : null;
		}
		catch
		{
			return null;
		}
	}

	private sealed record ParsedFile(MarkdownParseResult? Result, DateTimeOffset? LastModified, Exception? Error);
}
