using System.CommandLine;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Configuration;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Displays project statistics for a MokaDocs documentation project.
/// </summary>
internal static class StatsCommand
{
	/// <summary>Creates the stats command.</summary>
	public static Command Create()
	{
		var configOption = new Option<string?>("--config")
			{ Description = "Path to config file (default: mokadocs.yaml)" };
		var jsonOption = new Option<bool>("--json") { Description = "Output as JSON (for CI pipelines)" };

		var command = new Command("stats", "Show project statistics")
		{
			configOption,
			jsonOption
		};

		command.SetAction(async (parseResult, _) =>
		{
			string? configPath = parseResult.GetValue(configOption);
			bool outputJson = parseResult.GetValue(jsonOption);

			string rootDir = Directory.GetCurrentDirectory();
			string resolvedConfigPath = configPath ?? Path.Combine(rootDir, "mokadocs.yaml");

			// Load config
			SiteConfig config;
			try
			{
				var fs = new FileSystem();
				var reader = new SiteConfigReader(fs);
				config = reader.Read(resolvedConfigPath);
			}
			catch (FileNotFoundException)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Config file not found: {resolvedConfigPath}");
				AnsiConsole.MarkupLine("Run [bold]mokadocs init[/] to create a starter project.");
				return 1;
			}
			catch (SiteConfigException ex)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
				return 1;
			}

			// Gather stats
			var stats = new Dictionary<string, object>();

			// Docs folder stats
			string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));
			string[] markdownFiles = Directory.Exists(docsDir)
				? Directory.GetFiles(docsDir, "*.md", SearchOption.AllDirectories)
				: [];

			int markdownPageCount = markdownFiles.Length;
			stats["Markdown Pages"] = markdownPageCount;

			// Word count
			long wordCount = 0;
			foreach (string mdFile in markdownFiles)
			{
				string content = File.ReadAllText(mdFile);
				// Strip front matter
				Match fmMatch = Regex.Match(content, @"^---\s*\n.*?\n---\s*\n?", RegexOptions.Singleline);
				if (fmMatch.Success)
				{
					content = content[fmMatch.Length..];
				}

				wordCount += content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
			}

			stats["Word Count"] = wordCount;

			// API stats from XML doc files
			int apiTypes = 0;
			int apiMembers = 0;
			int typesWithSummary = 0;
			var namespaces = new HashSet<string>(StringComparer.Ordinal);
			int generatedPages = 0;

			foreach (ProjectSource proj in config.Content.Projects)
			{
				string projPath = Path.GetFullPath(Path.Combine(rootDir, proj.Path));
				if (!File.Exists(projPath))
				{
					continue;
				}

				string projDir = Path.GetDirectoryName(projPath)!;
				string projName = Path.GetFileNameWithoutExtension(projPath);
				string binDir = Path.Combine(projDir, "bin");

				if (!Directory.Exists(binDir))
				{
					continue;
				}

				string[] xmlFiles = Directory.GetFiles(binDir, $"{projName}.xml", SearchOption.AllDirectories);
				if (xmlFiles.Length == 0)
				{
					continue;
				}

				try
				{
					string xmlContent = File.ReadAllText(xmlFiles[0]);

					// Count types by kind using member name patterns
					var memberPattern = new Regex(@"<member\s+name=""T:([^""]+)""", RegexOptions.Compiled);
					foreach (Match match in memberPattern.Matches(xmlContent))
					{
						apiTypes++;
						string typeName = match.Groups[1].Value;
						int lastDot = typeName.LastIndexOf('.');
						if (lastDot > 0)
						{
							namespaces.Add(typeName[..lastDot]);
						}
					}

					// Count members (methods, properties, fields, events)
					var methodPattern = new Regex(@"<member\s+name=""M:", RegexOptions.Compiled);
					var propPattern = new Regex(@"<member\s+name=""P:", RegexOptions.Compiled);
					var fieldPattern = new Regex(@"<member\s+name=""F:", RegexOptions.Compiled);
					var eventPattern = new Regex(@"<member\s+name=""E:", RegexOptions.Compiled);

					apiMembers += methodPattern.Matches(xmlContent).Count;
					apiMembers += propPattern.Matches(xmlContent).Count;
					apiMembers += fieldPattern.Matches(xmlContent).Count;
					apiMembers += eventPattern.Matches(xmlContent).Count;

					// Types with summaries
					var summaryPattern = new Regex(
						@"<member\s+name=""T:[^""]+""[^>]*>\s*<summary>",
						RegexOptions.Compiled | RegexOptions.Singleline);
					typesWithSummary += summaryPattern.Matches(xmlContent).Count;
				}
				catch
				{
					// Skip if we can't parse the XML
				}
			}

			// Each API type generates a page
			generatedPages = apiTypes;

			stats["Generated Pages"] = generatedPages;
			stats["Total Pages"] = markdownPageCount + generatedPages;

			if (apiTypes > 0)
			{
				stats["API Types"] = apiTypes;
				stats["API Members"] = apiMembers;
				stats["Namespaces"] = namespaces.Count;

				int coverage = apiTypes > 0 ? typesWithSummary * 100 / apiTypes : 100;
				stats["XML Doc Coverage"] = $"{coverage}%";
			}

			// Plugins
			stats["Plugins"] = config.Plugins.Count;

			// Search
			stats["Search Enabled"] = config.Features.Search.Enabled;

			// Docs folder size
			if (Directory.Exists(docsDir))
			{
				string[] allDocsFiles = Directory.GetFiles(docsDir, "*.*", SearchOption.AllDirectories);
				long totalSize = 0;
				foreach (string f in allDocsFiles)
				{
					totalSize += new FileInfo(f).Length;
				}

				stats["Docs File Count"] = allDocsFiles.Length;
				stats["Docs Size"] = FormatSize(totalSize);
			}

			// Output
			if (outputJson)
			{
				string jsonOutput = JsonSerializer.Serialize(stats, new JsonSerializerOptions
				{
					WriteIndented = true
				});
				AnsiConsole.WriteLine(jsonOutput);
			}
			else
			{
				AnsiConsole.MarkupLine(
					$"[bold blue]mokadocs stats[/] — {Markup.Escape(config.Site.Title)}");
				AnsiConsole.WriteLine();

				Table table = new Table()
					.Border(TableBorder.Rounded)
					.AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned())
					.AddColumn(new TableColumn("[bold]Value[/]").RightAligned());

				table.AddRow("Markdown Pages", markdownPageCount.ToString("N0"));
				table.AddRow("Generated Pages", generatedPages.ToString("N0"));
				table.AddRow("Total Pages", (markdownPageCount + generatedPages).ToString("N0"));
				table.AddRow("Word Count", wordCount.ToString("N0"));

				if (apiTypes > 0)
				{
					table.AddRow("[dim]───────────────────[/]", "[dim]───────[/]");
					table.AddRow("API Types", apiTypes.ToString("N0"));

					// We can't distinguish class/struct/enum/etc from XML doc alone,
					// so show total and note that breakdown requires a build
					table.AddRow("API Members", apiMembers.ToString("N0"));

					int coverage = apiTypes > 0 ? typesWithSummary * 100 / apiTypes : 100;
					string coverageColor = coverage >= 90 ? "green" : coverage >= 50 ? "yellow" : "red";
					table.AddRow("XML Doc Coverage", $"[{coverageColor}]{coverage}%[/]");

					table.AddRow("Namespaces", namespaces.Count.ToString("N0"));
				}

				table.AddRow("[dim]───────────────────[/]", "[dim]───────[/]");
				table.AddRow("Plugins", config.Plugins.Count.ToString("N0"));
				table.AddRow("Search",
					config.Features.Search.Enabled
						? $"[green]Enabled[/] ({config.Features.Search.Provider})"
						: "[dim]Disabled[/]");

				if (Directory.Exists(docsDir))
				{
					string[] allDocsFiles = Directory.GetFiles(docsDir, "*.*", SearchOption.AllDirectories);
					long totalSize = 0;
					foreach (string f in allDocsFiles)
					{
						totalSize += new FileInfo(f).Length;
					}

					table.AddRow("Docs Files", allDocsFiles.Length.ToString("N0"));
					table.AddRow("Docs Size", FormatSize(totalSize));
				}

				AnsiConsole.Write(table);
			}

			return 0;
		});

		return command;
	}

	private static string FormatSize(long bytes)
	{
		return bytes switch
		{
			< 1024 => $"{bytes} B",
			< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
			< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
			_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
		};
	}
}
