using System.CommandLine;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Validates configuration, checks links, and reports warnings.
/// </summary>
internal static class ValidateCommand
{
	/// <summary>Creates the validate command.</summary>
	public static Command Create()
	{
		var command = new Command("validate", "Validate config, check links, and report warnings");

		command.SetAction(_ =>
		{
			AnsiConsole.MarkupLine("[bold blue]MokaDocs[/] — Validating...");
			AnsiConsole.MarkupLine("[yellow]Validation not yet implemented. Coming in Phase 5.[/]");
			return 0;
		});

		return command;
	}
}
