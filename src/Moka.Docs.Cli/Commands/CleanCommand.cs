// MokaDocs - CLI clean command

using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Engine;
using Moka.Docs.Engine.Caching;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Deletes the configured output directory and the build cache.
/// </summary>
internal static class CleanCommand
{
	/// <summary>Creates the clean command.</summary>
	public static Command Create()
	{
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };

		var command = new Command("clean", "Delete the output directory and the build cache")
		{
			configOption
		};

		command.SetAction(parseResult =>
		{
			string resolvedConfigPath = ConfigPath.Resolve(parseResult.GetValue(configOption));
			string rootDir = Path.GetDirectoryName(resolvedConfigPath)!;

			// No fallback to a default path. The previous version deleted ./_site when the
			// config failed to parse, which is not necessarily the folder the user builds to.
			SiteConfig config;
			try
			{
				config = new SiteConfigReader(new FileSystem()).Read(resolvedConfigPath);
			}
			catch (FileNotFoundException)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Config file not found: {Markup.Escape(resolvedConfigPath)}");
				return 1;
			}
			catch (SiteConfigException ex)
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
				return 1;
			}

			return Clean(rootDir, config);
		});

		return command;
	}

	/// <summary>
	///     Deletes the output directory and the <c>.mokadocs</c> cache for a loaded project.
	/// </summary>
	/// <returns>0 on success, 1 when a directory is unsafe to delete or could not be deleted.</returns>
	internal static int Clean(string rootDir, SiteConfig config)
	{
		string outputDir = Path.GetFullPath(Path.Combine(rootDir, config.Build.Output));
		string docsDir = Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs));

		string? conflict = OutputDirectoryGuard.FindConflict(rootDir, docsDir, outputDir);
		if (conflict is not null)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(conflict)}");
			return 1;
		}

		if (Directory.Exists(outputDir))
		{
			try
			{
				Directory.Delete(outputDir, true);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Usually a file held open by a running `mokadocs serve` or an editor.
				AnsiConsole.MarkupLine(
					$"[red]Error:[/] Could not delete {Markup.Escape(outputDir)}: {Markup.Escape(ex.Message)}");
				return 1;
			}

			AnsiConsole.MarkupLine($"[green]Deleted[/] {Markup.Escape(outputDir)}");
		}
		else
		{
			AnsiConsole.MarkupLine($"[dim]No output directory at {Markup.Escape(outputDir)}[/]");
		}

		string cacheDir = Path.Combine(rootDir, ".mokadocs");
		if (!Directory.Exists(cacheDir))
		{
			return 0;
		}

		if (!new BuildCache(NullLogger<BuildCache>.Instance).Clear(rootDir))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Could not delete {Markup.Escape(cacheDir)}");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]Deleted[/] {Markup.Escape(cacheDir)}");
		return 0;
	}
}
