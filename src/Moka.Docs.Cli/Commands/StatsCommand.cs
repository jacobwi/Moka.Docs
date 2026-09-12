using System.CommandLine;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moka.Docs.Cli.Diagnostics;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Displays project statistics for a MokaDocs documentation project.
/// </summary>
/// <remarks>
///     Numbers come from a dry-run build, so they match what <c>mokadocs build</c> produces.
///     The previous version counted API types in compiled XML documentation files under
///     <c>bin/</c>, which the build never reads: projects without a documentation file showed
///     no API at all, and generated pages were always reported as zero.
/// </remarks>
internal static class StatsCommand
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	/// <summary>Creates the stats command.</summary>
	public static Command Create()
	{
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };
		var jsonOption = new Option<bool>("--json") { Description = "Output as JSON (for CI pipelines)" };

		var command = new Command("stats", "Show project statistics")
		{
			configOption,
			jsonOption
		};

		command.SetAction(async (parseResult, ct) =>
		{
			bool outputJson = parseResult.GetValue(jsonOption);
			string resolvedConfigPath = ConfigPath.Resolve(parseResult.GetValue(configOption));
			string rootDir = Path.GetDirectoryName(resolvedConfigPath)!;

			SiteConfig config;
			try
			{
				config = new SiteConfigReader(new FileSystem()).Read(resolvedConfigPath);
			}
			catch (FileNotFoundException)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Config file not found: {Markup.Escape(resolvedConfigPath)}");
				AnsiConsole.MarkupLine("Run [bold]mokadocs init[/] to create a starter project.");
				return 1;
			}
			catch (SiteConfigException ex)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
				return 1;
			}

			DryRunOutcome outcome = await DryRunBuild.RunAsync(config, rootDir, false, false, ct);
			if (outcome.Failure is not null)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] The build failed: {Markup.Escape(outcome.Failure.Message)}");
				AnsiConsole.MarkupLine("Run [bold]mokadocs validate -v[/] for details.");
				return 1;
			}

			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
			ProjectStats stats = ProjectStats.From(outcome, docsDir);

			if (outputJson)
			{
				// Plain Console, not AnsiConsole: Spectre wraps long lines at the terminal
				// width, which would corrupt JSON piped into another tool.
				Console.Out.WriteLine(JsonSerializer.Serialize(stats.ToJsonShape(), _jsonOptions));
			}
			else
			{
				PrintTable(config, stats);
			}

			return 0;
		});

		return command;
	}

	private static void PrintTable(SiteConfig config, ProjectStats stats)
	{
		AnsiConsole.MarkupLine($"[bold blue]mokadocs stats[/] - {Markup.Escape(config.Site.Title)}");
		AnsiConsole.WriteLine();

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned())
			.AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

		table.AddRow("Markdown Pages", stats.MarkdownPages.ToString("N0"));
		table.AddRow("Generated Pages", stats.GeneratedPages.ToString("N0"));
		table.AddRow("Total Pages", stats.TotalPages.ToString("N0"));
		table.AddRow("Word Count", stats.WordCount.ToString("N0"));

		if (stats.Coverage is { } coverage)
		{
			table.AddRow("[dim]───────────────────[/]", "[dim]───────[/]");
			table.AddRow("API Types", stats.ApiTypes.ToString("N0"));
			table.AddRow("API Members", stats.ApiMembers.ToString("N0"));
			string color = coverage.Percent >= 90 ? "green" : coverage.Percent >= 50 ? "yellow" : "red";
			table.AddRow("XML Doc Coverage", $"[{color}]{coverage.Percent}%[/]");
			table.AddRow("Namespaces", stats.Namespaces.ToString("N0"));
		}

		table.AddRow("[dim]───────────────────[/]", "[dim]───────[/]");
		table.AddRow("Plugins",
			stats.LoadedPlugins == stats.DeclaredPlugins
				? stats.LoadedPlugins.ToString("N0")
				: $"[yellow]{stats.LoadedPlugins} of {stats.DeclaredPlugins} loaded[/]");
		table.AddRow("Search",
			stats.SearchEnabled
				? $"[green]Enabled[/] ({stats.SearchEntries:N0} entries)"
				: "[dim]Disabled[/]");
		table.AddRow("Docs Files", stats.DocsFiles.ToString("N0"));
		table.AddRow("Docs Size", ProjectStats.FormatSize(stats.DocsBytes));

		AnsiConsole.Write(table);
	}
}

/// <summary>The numbers <c>mokadocs stats</c> reports, taken from a dry-run build.</summary>
internal sealed partial record ProjectStats
{
	/// <summary>Pages built from Markdown files, drafts included.</summary>
	public int MarkdownPages { get; init; }

	/// <summary>Pages produced by the build itself: API reference, plugin and index pages.</summary>
	public int GeneratedPages { get; init; }

	/// <summary>All pages.</summary>
	public int TotalPages => MarkdownPages + GeneratedPages;

	/// <summary>Words across the Markdown files the build read, front matter excluded.</summary>
	public long WordCount { get; init; }

	/// <summary>Types in the API model.</summary>
	public int ApiTypes { get; init; }

	/// <summary>Members across all API types.</summary>
	public int ApiMembers { get; init; }

	/// <summary>Namespaces in the API model.</summary>
	public int Namespaces { get; init; }

	/// <summary>Documentation coverage, or <c>null</c> when the build produced no API model.</summary>
	public ApiCoverage? Coverage { get; init; }

	/// <summary>Entries under <c>plugins</c> in mokadocs.yaml.</summary>
	public int DeclaredPlugins { get; init; }

	/// <summary>Declared plugins that initialized.</summary>
	public int LoadedPlugins { get; init; }

	/// <summary>Whether <c>features.search.enabled</c> is on.</summary>
	public bool SearchEnabled { get; init; }

	/// <summary>Entries in the generated search index.</summary>
	public int SearchEntries { get; init; }

	/// <summary>Markdown and asset files the build read from the docs folder.</summary>
	public int DocsFiles { get; init; }

	/// <summary>Total size of <see cref="DocsFiles" /> in bytes.</summary>
	public long DocsBytes { get; init; }

	/// <summary>
	///     Collects the statistics from a completed dry run.
	/// </summary>
	/// <remarks>
	///     File counts use the files the discovery phase kept rather than a directory listing,
	///     so a built site inside the docs folder (such as <c>docs/_site</c>) is not counted
	///     as source.
	/// </remarks>
	/// <param name="outcome">A dry run that completed.</param>
	/// <param name="docsDirectory">The resolved <c>content.docs</c> directory.</param>
	public static ProjectStats From(DryRunOutcome outcome, string docsDirectory)
	{
		BuildContext context = outcome.Context;
		int markdownPages = context.Pages.Count(p => p.Origin == PageOrigin.Markdown);

		long words = 0;
		long bytes = 0;
		foreach (string relative in context.DiscoveredMarkdownFiles)
		{
			string path = Path.Combine(docsDirectory, relative);
			words += CountWords(File.ReadAllText(path));
			bytes += new FileInfo(path).Length;
		}

		foreach (string relative in context.DiscoveredAssetFiles)
		{
			bytes += new FileInfo(Path.Combine(docsDirectory, relative)).Length;
		}

		return new ProjectStats
		{
			MarkdownPages = markdownPages,
			GeneratedPages = context.Pages.Count - markdownPages,
			WordCount = words,
			ApiTypes = context.ApiModel?.Namespaces.Sum(n => n.Types.Count) ?? 0,
			ApiMembers = context.ApiModel?.Namespaces.Sum(n => n.Types.Sum(t => t.Members.Count)) ?? 0,
			Namespaces = context.ApiModel?.Namespaces.Count ?? 0,
			Coverage = context.ApiModel is { } api ? DoctorChecks.ComputeApiCoverage(api) : null,
			DeclaredPlugins = context.Config.Plugins.Count,
			LoadedPlugins = outcome.LoadedPluginIds.Count,
			SearchEnabled = context.Config.Features.Search.Enabled,
			SearchEntries = context.SearchIndex?.Count ?? 0,
			DocsFiles = context.DiscoveredMarkdownFiles.Count + context.DiscoveredAssetFiles.Count,
			DocsBytes = bytes
		};
	}

	/// <summary>Counts whitespace-separated words, skipping a leading front matter block.</summary>
	public static long CountWords(string markdown)
	{
		Match frontMatter = FrontMatterPattern().Match(markdown);
		string body = frontMatter.Success ? markdown[frontMatter.Length..] : markdown;
		return body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
	}

	/// <summary>
	///     The JSON document printed by <c>--json</c>. Key names are kept from earlier
	///     versions so CI scripts that read them keep working.
	/// </summary>
	public Dictionary<string, object> ToJsonShape()
	{
		var json = new Dictionary<string, object>
		{
			["Markdown Pages"] = MarkdownPages,
			["Word Count"] = WordCount,
			["Generated Pages"] = GeneratedPages,
			["Total Pages"] = TotalPages
		};

		if (Coverage is { } coverage)
		{
			json["API Types"] = ApiTypes;
			json["API Members"] = ApiMembers;
			json["Namespaces"] = Namespaces;
			json["XML Doc Coverage"] = $"{coverage.Percent}%";
		}

		json["Plugins"] = LoadedPlugins;
		json["Search Enabled"] = SearchEnabled;
		json["Search Entries"] = SearchEntries;
		json["Docs File Count"] = DocsFiles;
		json["Docs Size"] = FormatSize(DocsBytes);
		return json;
	}

	/// <summary>Formats a byte count as B, KB, MB or GB.</summary>
	public static string FormatSize(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
		_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
	};

	[GeneratedRegex(@"^---\s*\n.*?\n---\s*\n?", RegexOptions.Singleline)]
	private static partial Regex FrontMatterPattern();
}
