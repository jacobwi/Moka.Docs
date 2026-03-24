// MokaDocs — CLI clean command

using System.CommandLine;
using System.IO.Abstractions;
using Moka.Docs.Core.Configuration;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Deletes the configured output directory.
/// </summary>
internal static class CleanCommand
{
    /// <summary>Creates the clean command.</summary>
    public static Command Create()
    {
        var command = new Command("clean", "Delete the output directory");

        command.SetAction(_ =>
        {
            var rootDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(rootDir, "mokadocs.yaml");

            // Try to read the configured output directory
            var outputDir = "_site";
            if (File.Exists(configPath))
                try
                {
                    var reader = new SiteConfigReader(new FileSystem());
                    var config = reader.Read(configPath);
                    outputDir = config.Build.Output;
                }
                catch
                {
                    // Fall back to default
                }

            var fullPath = Path.GetFullPath(Path.Combine(rootDir, outputDir));
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                AnsiConsole.MarkupLine($"[green]Deleted[/] {fullPath}");
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]Output directory does not exist. Nothing to clean.[/]");
            }

            return 0;
        });

        return command;
    }
}