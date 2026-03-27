using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Displays environment and configuration information.
/// </summary>
internal static class InfoCommand
{
	/// <summary>Creates the info command.</summary>
	public static Command Create()
	{
		var command = new Command("info", "Show environment and configuration information");

		command.SetAction(_ =>
		{
			string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.1";

			Table table = new Table()
				.Border(TableBorder.Rounded)
				.Title("[bold blue]MokaDocs Info[/]");

			table.AddColumn("Property");
			table.AddColumn("Value");

			table.AddRow("Version", version);
			table.AddRow("Runtime", RuntimeInformation.FrameworkDescription);
			table.AddRow("OS", RuntimeInformation.OSDescription);
			table.AddRow("Working Directory", Directory.GetCurrentDirectory());

			string configPath = Path.Combine(Directory.GetCurrentDirectory(), "mokadocs.yaml");
			table.AddRow("Config File", File.Exists(configPath) ? "[green]Found[/]" : "[dim]Not found[/]");

			string docsDir = Path.Combine(Directory.GetCurrentDirectory(), "docs");
			table.AddRow("Docs Directory", Directory.Exists(docsDir) ? "[green]Found[/]" : "[dim]Not found[/]");

			string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "_site");
			table.AddRow("Output Directory",
				Directory.Exists(outputDir) ? "[green]Exists[/]" : "[dim]Not built yet[/]");

			AnsiConsole.Write(table);
			return 0;
		});

		return command;
	}
}
