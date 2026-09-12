using System.CommandLine;
using System.IO.Abstractions;
using Moka.Docs.Cli.Diagnostics;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Runs the full build as a dry run and reports what the build itself would warn about.
/// </summary>
/// <remarks>
///     Complements <c>doctor</c> rather than overlapping it. <c>doctor</c> inspects project
///     files with targeted checks; <c>validate</c> executes the real pipeline (Markdown
///     parsing, Roslyn analysis, plugins, theme rendering) and surfaces the diagnostics it
///     produces, which static checks cannot see. Nothing is written to the output directory,
///     so it is safe to run against a checkout with a built site already in place.
/// </remarks>
internal static class ValidateCommand
{
	/// <summary>Creates the validate command.</summary>
	public static Command Create()
	{
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };
		var draftOption = new Option<bool>("--draft") { Description = "Include draft pages" };
		var verboseOption = new Option<bool>("--verbose", "-v")
			{ Description = "Show pipeline logging and informational diagnostics" };

		var command = new Command("validate",
			"Run the full build without writing output, and report every warning and error")
		{
			configOption,
			draftOption,
			verboseOption
		};

		command.SetAction(async (parseResult, ct) =>
		{
			string? configPath = parseResult.GetValue(configOption);
			bool draft = parseResult.GetValue(draftOption);
			bool verbose = parseResult.GetValue(verboseOption);

			// Resolve against the yaml's own folder, as build does, so --config pointing
			// elsewhere validates that project rather than the working directory.
			string resolvedConfigPath = ConfigPath.Resolve(configPath);
			string rootDir = Path.GetDirectoryName(resolvedConfigPath)!;

			AnsiConsole.MarkupLine("[bold blue]mokadocs validate[/] - dry run, nothing is written");
			AnsiConsole.WriteLine();

			SiteConfig config;
			try
			{
				config = new SiteConfigReader(new FileSystem()).Read(resolvedConfigPath);
				AnsiConsole.MarkupLine(
					$"  [green]✓[/] Config        {Markup.Escape(Path.GetFileName(resolvedConfigPath))} - \"{Markup.Escape(config.Site.Title)}\"");
			}
			catch (FileNotFoundException)
			{
				AnsiConsole.MarkupLine($"  [red]✗[/] Config        not found: {Markup.Escape(resolvedConfigPath)}");
				return 2;
			}
			catch (SiteConfigException ex)
			{
				AnsiConsole.MarkupLine($"  [red]✗[/] Config        {Markup.Escape(ex.Message)}");
				return 2;
			}

			DryRunOutcome outcome = await DryRunBuild.RunAsync(config, rootDir, draft, verbose, ct);

			if (outcome.Failure is not null)
			{
				AnsiConsole.MarkupLine(
					$"  [red]✗[/] Build         pipeline failed: {Markup.Escape(outcome.Failure.Message)}");
				if (verbose)
				{
					AnsiConsole.WriteException(outcome.Failure);
				}

				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine("  [red]Result: build failed[/]");
				return 2;
			}

			BuildContext context = outcome.Context;
			PrintSummary(context, outcome);

			List<Diagnostic> problems = CollectProblems(config, outcome);
			PrintProblems(problems, context.Diagnostics, verbose);

			int errors = problems.Count(d => d.Severity == DiagnosticSeverity.Error);
			int warnings = problems.Count(d => d.Severity == DiagnosticSeverity.Warning);
			string color = errors > 0 ? "red" : warnings > 0 ? "yellow" : "green";

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"  [{color}]Result: {errors} error(s), {warnings} warning(s)[/]");

			return errors > 0 ? 2 : warnings > 0 ? 1 : 0;
		});

		return command;
	}

	private static void PrintSummary(BuildContext context, DryRunOutcome outcome)
	{
		int markdownPages = context.Pages.Count(p => p.Origin == PageOrigin.Markdown);
		int generatedPages = context.Pages.Count - markdownPages;
		int apiTypes = context.ApiModel?.Namespaces.Sum(n => n.Types.Count) ?? 0;

		AnsiConsole.MarkupLine(
			$"  [green]✓[/] Build         dry run completed in {outcome.Elapsed.TotalSeconds:F2}s");
		AnsiConsole.MarkupLine(
			$"                  {context.Pages.Count} pages ({markdownPages} markdown, {generatedPages} generated)");

		if (apiTypes > 0)
		{
			AnsiConsole.MarkupLine($"                  {apiTypes} API types");
		}

		if (context.SearchIndex is { Count: > 0 } index)
		{
			AnsiConsole.MarkupLine($"                  {index.Count} search index entries");
		}

		if (outcome.LoadedPluginIds.Count > 0)
		{
			AnsiConsole.MarkupLine(
				$"                  plugins: {Markup.Escape(string.Join(", ", outcome.LoadedPluginIds))}");
		}
	}

	/// <summary>
	///     Everything the build reported, plus plugin declarations that silently did nothing.
	///     The plugin host only logs those, so without this a misspelled plugin name passes
	///     validation while its pages never appear.
	/// </summary>
	private static List<Diagnostic> CollectProblems(SiteConfig config, DryRunOutcome outcome)
	{
		var problems = outcome.Context.Diagnostics.All
			.Where(d => d.Severity != DiagnosticSeverity.Info)
			.ToList();

		var registered = new HashSet<string>(outcome.RegisteredPluginIds, StringComparer.OrdinalIgnoreCase);
		var loaded = new HashSet<string>(outcome.LoadedPluginIds, StringComparer.OrdinalIgnoreCase);

		foreach (string problem in DoctorChecks.FindIgnoredPluginDeclarations(config.Plugins))
		{
			problems.Add(new Diagnostic
			{
				Severity = DiagnosticSeverity.Warning,
				Message = problem,
				Source = "Plugins"
			});
		}

		foreach (PluginDeclaration declaration in config.Plugins)
		{
			if (string.IsNullOrWhiteSpace(declaration.Name) || loaded.Contains(declaration.Name))
			{
				continue;
			}

			string message = registered.Contains(declaration.Name)
				? $"Plugin '{declaration.Name}' failed to initialize (run with --verbose for the reason)"
				: $"Plugin '{declaration.Name}' is declared but no plugin has that id";

			problems.Add(new Diagnostic
			{
				Severity = DiagnosticSeverity.Warning,
				Message = message,
				Source = "Plugins"
			});
		}

		return problems;
	}

	private static void PrintProblems(List<Diagnostic> problems, DiagnosticBag bag, bool verbose)
	{
		IEnumerable<Diagnostic> shown = verbose
			? problems.Concat(bag.All.Where(d => d.Severity == DiagnosticSeverity.Info))
			: problems;

		var ordered = shown
			.OrderByDescending(d => d.Severity)
			.ThenBy(d => d.Source, StringComparer.Ordinal)
			.ToList();

		if (ordered.Count == 0)
		{
			return;
		}

		AnsiConsole.WriteLine();
		foreach (Diagnostic d in ordered)
		{
			(string mark, string color) = d.Severity switch
			{
				DiagnosticSeverity.Error => ("✗", "red"),
				DiagnosticSeverity.Warning => ("⚠", "yellow"),
				_ => ("i", "dim")
			};

			string source = string.IsNullOrEmpty(d.Source) ? "" : $"[dim]{Markup.Escape(d.Source)}:[/] ";
			AnsiConsole.MarkupLine($"  [{color}]{mark}[/] {source}{Markup.Escape(d.Message)}");
		}
	}
}
