using System.CommandLine;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Scaffolds a new mokadocs.yaml and docs/ folder.
/// </summary>
internal static class InitCommand
{
	private const string DefaultConfig =
		"""
		# MokaDocs Configuration
		site:
		  title: "My Project Docs"
		  description: "Documentation for My Project"

		content:
		  docs: ./docs

		theme:
		  name: default

		features:
		  search:
		    enabled: true

		build:
		  output: ./_site
		  clean: true
		""";

	private const string DefaultIndexPage =
		"""
		---
		title: Welcome
		description: Welcome to the documentation
		order: 1
		layout: default
		---

		# Welcome to My Project

		This is the home page of your documentation site, powered by **MokaDocs**.

		## Getting Started

		Edit this file at `docs/index.md` to start writing your documentation.

		## Features

		- **Markdown-first** — Write docs in Markdown with YAML front matter
		- **API Reference** — Auto-generated from your C# XML documentation
		- **Beautiful Themes** — Clean, modern design with dark mode
		- **Instant Search** — Find anything with Cmd/Ctrl+K
		- **Versioning** — Multi-version documentation support
		""";

	/// <summary>Creates the init command.</summary>
	public static Command Create()
	{
		var command = new Command("init", "Scaffold a new MokaDocs project (mokadocs.yaml + docs/ folder)");

		command.SetAction(_ =>
		{
			AnsiConsole.MarkupLine("[bold green]mokadocs init[/] — Scaffolding new project...");

			string configPath = Path.Combine(Directory.GetCurrentDirectory(), "mokadocs.yaml");
			if (File.Exists(configPath))
			{
				AnsiConsole.MarkupLine("[yellow]mokadocs.yaml already exists. Skipping.[/]");
				return 0;
			}

			string docsDir = Path.Combine(Directory.GetCurrentDirectory(), "docs");
			Directory.CreateDirectory(docsDir);

			File.WriteAllText(configPath, DefaultConfig);
			File.WriteAllText(Path.Combine(docsDir, "index.md"), DefaultIndexPage);

			AnsiConsole.MarkupLine("[green]Created:[/] mokadocs.yaml");
			AnsiConsole.MarkupLine("[green]Created:[/] docs/index.md");
			AnsiConsole.MarkupLine("");
			AnsiConsole.MarkupLine("Run [bold]mokadocs build[/] to generate your site.");
			return 0;
		});

		return command;
	}
}
