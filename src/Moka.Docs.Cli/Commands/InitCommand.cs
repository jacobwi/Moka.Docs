using System.CommandLine;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Scaffolds a new mokadocs.yaml and docs/ folder.
/// </summary>
internal static class InitCommand
{
	private const string _defaultConfig =
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

	private const string _defaultIndexPage =
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

		- **Markdown** - Pages with YAML front matter
		- **API reference** - Generated from the doc comments in your C# source
		- **Components** - Cards, steps, tabs and admonitions
		- **Search** - Titles, headings and page text, opened with Ctrl/Cmd+K
		""";

	/// <summary>Creates the init command.</summary>
	public static Command Create()
	{
		var command = new Command("init", "Scaffold a new MokaDocs project (mokadocs.yaml + docs/ folder)");

		command.SetAction(_ =>
		{
			AnsiConsole.MarkupLine("[bold green]mokadocs init[/] - Scaffolding new project...");

			foreach ((string file, bool created) in Scaffold(Directory.GetCurrentDirectory()))
			{
				AnsiConsole.MarkupLine(created
					? $"[green]Created:[/] {Markup.Escape(file)}"
					: $"[yellow]{Markup.Escape(file)} already exists. Skipping.[/]");
			}

			AnsiConsole.MarkupLine("");
			AnsiConsole.MarkupLine("Run [bold]mokadocs build[/] to generate your site.");
			return 0;
		});

		return command;
	}

	/// <summary>
	///     Writes the starter config and home page into <paramref name="directory" />, leaving
	///     every file that already exists untouched. An existing <c>mokadocs.yml</c> counts as
	///     the config, since <c>ConfigPath</c> would read it.
	/// </summary>
	/// <returns>Each file, relative to the directory, and whether it was created.</returns>
	internal static List<(string File, bool Created)> Scaffold(string directory)
	{
		var results = new List<(string File, bool Created)>();

		string existingConfig = File.Exists(Path.Combine(directory, "mokadocs.yml")) ? "mokadocs.yml" : "mokadocs.yaml";
		results.Add((existingConfig, WriteIfMissing(Path.Combine(directory, existingConfig), _defaultConfig)));

		// It used to write docs/index.md whenever mokadocs.yaml was missing, replacing a home
		// page the user already had.
		Directory.CreateDirectory(Path.Combine(directory, "docs"));
		results.Add(("docs/index.md", WriteIfMissing(Path.Combine(directory, "docs", "index.md"), _defaultIndexPage)));

		return results;
	}

	private static bool WriteIfMissing(string path, string content)
	{
		try
		{
			using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
			using var writer = new StreamWriter(stream);
			writer.Write(content);
			return true;
		}
		catch (IOException) when (File.Exists(path))
		{
			return false;
		}
	}
}
