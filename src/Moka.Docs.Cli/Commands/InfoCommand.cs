using System.CommandLine;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Moka.Docs.Core.Configuration;
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
		var configOption = new Option<string?>("--config", "-c")
			{ Description = "Path to configuration file (default: mokadocs.yaml)" };

		var command = new Command("info", "Show environment and configuration information")
		{
			configOption
		};

		command.SetAction(parseResult =>
		{
			string configPath = ConfigPath.Resolve(parseResult.GetValue(configOption));
			string rootDir = Path.GetDirectoryName(configPath)!;

			Table table = new Table()
				.Border(TableBorder.Rounded)
				.Title("[bold blue]MokaDocs Info[/]");

			table.AddColumn("Property");
			table.AddColumn("Value");

			table.AddRow("Version", Markup.Escape(CliVersion.Current));
			table.AddRow("Runtime", Markup.Escape(RuntimeInformation.FrameworkDescription));
			table.AddRow("OS", Markup.Escape(RuntimeInformation.OSDescription));
			table.AddRow("Working Directory", Markup.Escape(Directory.GetCurrentDirectory()));

			if (!File.Exists(configPath))
			{
				table.AddRow("Config File", $"{Markup.Escape(configPath)} [dim](not found)[/]");
				AnsiConsole.Write(table);
				return 0;
			}

			// Docs and output paths come from the config. The previous version always looked
			// for ./docs and ./_site, so a project building to docs/_site read "Not built yet".
			try
			{
				SiteConfig config = new SiteConfigReader(new FileSystem()).Read(configPath);
				table.AddRow("Config File", Markup.Escape(configPath));
				table.AddRow("Docs Directory",
					PathWithStatus(Path.GetFullPath(Path.Combine(rootDir, config.Content.Docs)), "not found"));
				table.AddRow("Output Directory",
					PathWithStatus(Path.GetFullPath(Path.Combine(rootDir, config.Build.Output)), "not built yet"));
			}
			catch (SiteConfigException ex)
			{
				table.AddRow("Config File", $"{Markup.Escape(configPath)} [red](invalid: {Markup.Escape(ex.Message)})[/]");
			}

			AnsiConsole.Write(table);
			return 0;
		});

		return command;
	}

	private static string PathWithStatus(string path, string missingLabel) =>
		Directory.Exists(path) ? Markup.Escape(path) : $"{Markup.Escape(path)} [dim]({missingLabel})[/]";
}
