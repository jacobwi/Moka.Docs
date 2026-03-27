using System.CommandLine;
using System.Globalization;
using System.Text;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Scaffolding command: <c>mokadocs new page|plugin|component</c>.
/// </summary>
internal static class NewCommand
{
	// ──────────────────────── component markdown templates ───────────────────────

	private static readonly Dictionary<string, string> ComponentTemplates = new()
	{
		["card"] = """
		           ---
		           title: Card Component Example
		           layout: default
		           ---

		           # Card Component

		           :::card{title="Feature Highlight" icon="zap"}
		           Describe a key feature or concept here.
		           :::

		           :::card{title="Outlined Card" icon="square" variant="outlined"}
		           This card uses a border-only style.
		           :::

		           :::card{title="Filled Card" icon="palette" variant="filled"}
		           This card has a solid background for emphasis.
		           :::
		           """,

		["steps"] = """
		            ---
		            title: Steps Component Example
		            layout: default
		            ---

		            # Steps Component

		            :::steps
		            ### Step One

		            Describe the first step here.

		            ### Step Two

		            Describe the second step here.

		            ### Step Three

		            Describe the third step here.
		            :::
		            """,

		["link-cards"] = """
		                 ---
		                 title: Link Cards Component Example
		                 layout: default
		                 ---

		                 # Link Cards Component

		                 :::link-cards
		                 - [Getting Started](/getting-started/quickstart) — Set up your first project
		                 - [Configuration](/configuration/site-config) — Customize your site
		                 - [Markdown Features](/guide/markdown) — Explore supported syntax
		                 - [Deployment](/advanced/deployment) — Deploy to production
		                 :::
		                 """,

		["code-group"] = """
		                 ---
		                 title: Code Group Component Example
		                 layout: default
		                 ---

		                 # Code Group Component

		                 :::code-group
		                 ```csharp title="C#"
		                 Console.WriteLine("Hello, world!");
		                 ```
		                 ```python title="Python"
		                 print("Hello, world!")
		                 ```
		                 ```bash title="Bash"
		                 echo "Hello, world!"
		                 ```
		                 :::
		                 """,

		["changelog"] = """
		                ---
		                title: Changelog Component Example
		                layout: default
		                ---

		                # Changelog

		                :::changelog

		                ## v1.1.0 — 2025-07-01

		                ### Added
		                - New feature description here
		                - Another new feature

		                ### Fixed
		                - Bug fix description here

		                ## v1.0.0 — 2025-06-01

		                ### Added
		                - Initial release feature one
		                - Initial release feature two

		                :::
		                """
	};

	/// <summary>Creates the <c>new</c> command with its subcommands.</summary>
	public static Command Create()
	{
		var command = new Command("new", "Scaffold new pages, plugins, or component examples");

		command.Add(CreatePageCommand());
		command.Add(CreatePluginCommand());
		command.Add(CreateComponentCommand());

		return command;
	}

	// ───────────────────────────────── helpers ──────────────────────────────────

	private static string ToTitleCase(string kebab)
	{
		TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
		return ti.ToTitleCase(kebab.Replace('-', ' '));
	}

	private static string ToPascalCase(string kebab)
	{
		return string.Concat(
			kebab.Split('-', StringSplitOptions.RemoveEmptyEntries)
				.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
	}

	private static void EnsureDirectoryAndWrite(string filePath, string content)
	{
		string dir = Path.GetDirectoryName(filePath)!;
		Directory.CreateDirectory(dir);
		File.WriteAllText(filePath, content);
	}

	// ─────────────────────────── mokadocs new page ──────────────────────────────

	private static Command CreatePageCommand()
	{
		var nameArg = new Argument<string>("name") { Description = "Page name (e.g. getting-started, api-overview)" };

		var titleOpt = new Option<string?>("--title") { Description = "Page title (defaults to name in Title Case)" };

		var pathOpt = new Option<string>("--path")
			{ Description = "Output directory", DefaultValueFactory = _ => "./docs" };

		var layoutOpt = new Option<string>("--layout")
			{ Description = "Layout template (default, landing, api-type)", DefaultValueFactory = _ => "default" };

		var orderOpt = new Option<int?>("--order") { Description = "Sort order number" };

		var command = new Command("page", "Create a new Markdown documentation page")
		{
			nameArg, titleOpt, pathOpt, layoutOpt, orderOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!;
			string title = parseResult.GetValue(titleOpt) ?? ToTitleCase(name);
			string outputDir = parseResult.GetValue(pathOpt)!;
			string layout = parseResult.GetValue(layoutOpt)!;
			int? order = parseResult.GetValue(orderOpt);

			AnsiConsole.MarkupLine("[bold green]mokadocs new page[/] — Scaffolding new page...");

			var sb = new StringBuilder();
			sb.AppendLine("---");
			sb.AppendLine($"title: {title}");
			if (order.HasValue)
			{
				sb.AppendLine($"order: {order.Value}");
			}

			sb.AppendLine($"layout: {layout}");
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine($"# {title}");
			sb.AppendLine();
			sb.AppendLine("Write your content here.");
			sb.AppendLine();

			string filePath = Path.GetFullPath(Path.Combine(outputDir, $"{name}.md"));
			if (File.Exists(filePath))
			{
				AnsiConsole.MarkupLine($"[yellow]{filePath} already exists. Skipping.[/]");
				return 0;
			}

			EnsureDirectoryAndWrite(filePath, sb.ToString());

			AnsiConsole.MarkupLine($"[green]Created:[/] {filePath}");
			return 0;
		});

		return command;
	}

	// ──────────────────────────── mokadocs new plugin ────────────────────────────

	private static Command CreatePluginCommand()
	{
		var nameArg = new Argument<string>("name")
			{ Description = "Plugin name in kebab-case (e.g. my-custom-plugin)" };

		var pathOpt = new Option<string>("--path") { Description = "Output directory", DefaultValueFactory = _ => "." };

		var command = new Command("plugin", "Scaffold a new MokaDocs plugin project")
		{
			nameArg, pathOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!;
			string outputDir = parseResult.GetValue(pathOpt)!;
			string pascal = ToPascalCase(name);

			AnsiConsole.MarkupLine("[bold green]mokadocs new plugin[/] — Scaffolding new plugin...");

			string projectName = $"Moka.Docs.Plugins.{pascal}";
			string projectDir = Path.GetFullPath(Path.Combine(outputDir, projectName));

			if (Directory.Exists(projectDir))
			{
				AnsiConsole.MarkupLine($"[yellow]{projectDir} already exists. Skipping.[/]");
				return 0;
			}

			// ── .csproj ──
			string csproj = $"""
			                 <Project Sdk="Microsoft.NET.Sdk">

			                   <PropertyGroup>
			                     <TargetFramework>net9.0</TargetFramework>
			                     <RootNamespace>{projectName}</RootNamespace>
			                     <ImplicitUsings>enable</ImplicitUsings>
			                     <Nullable>enable</Nullable>
			                   </PropertyGroup>

			                   <ItemGroup>
			                     <ProjectReference Include="..\..\Moka.Docs.Core\Moka.Docs.Core.csproj" />
			                     <ProjectReference Include="..\..\Moka.Docs.Plugins\Moka.Docs.Plugins.csproj" />
			                   </ItemGroup>

			                 </Project>
			                 """;

			EnsureDirectoryAndWrite(
				Path.Combine(projectDir, $"{projectName}.csproj"),
				csproj);

			// ── Plugin class ──
			string pluginClass = $$"""
			                       using Moka.Docs.Core.Pipeline;
			                       using Moka.Docs.Plugins;

			                       namespace {{projectName}};

			                       /// <summary>
			                       ///     A custom MokaDocs plugin.
			                       /// </summary>
			                       public sealed class {{pascal}}Plugin : IMokaPlugin
			                       {
			                           public string Id => "mokadocs-{{name}}";
			                           public string Name => "{{pascal}}";
			                           public string Version => "1.0.0";

			                           public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
			                           {
			                               context.LogInfo($"{Name} plugin initialized.");
			                               return Task.CompletedTask;
			                           }

			                           public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
			                           {
			                               // TODO: Implement plugin logic here.
			                               return Task.CompletedTask;
			                           }
			                       }
			                       """;

			EnsureDirectoryAndWrite(
				Path.Combine(projectDir, $"{pascal}Plugin.cs"),
				pluginClass);

			AnsiConsole.MarkupLine($"[green]Created:[/] {Path.Combine(projectDir, $"{projectName}.csproj")}");
			AnsiConsole.MarkupLine($"[green]Created:[/] {Path.Combine(projectDir, $"{pascal}Plugin.cs")}");
			AnsiConsole.MarkupLine("");
			AnsiConsole.MarkupLine(
				$"Add a reference in your solution to start developing the [bold]{pascal}[/] plugin.");
			return 0;
		});

		return command;
	}

	// ────────────────────────── mokadocs new component ───────────────────────────

	private static Command CreateComponentCommand()
	{
		var nameArg = new Argument<string>("name")
			{ Description = "Component type (card, steps, link-cards, code-group, changelog)" };

		var pathOpt = new Option<string>("--path")
			{ Description = "Output directory", DefaultValueFactory = _ => "./docs" };

		var command = new Command("component", "Scaffold a Markdown page showcasing a component")
		{
			nameArg, pathOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!.ToLowerInvariant();
			string outputDir = parseResult.GetValue(pathOpt)!;

			AnsiConsole.MarkupLine("[bold green]mokadocs new component[/] — Scaffolding component example...");

			if (!ComponentTemplates.TryGetValue(name, out string? template))
			{
				AnsiConsole.MarkupLine(
					$"[red]Unknown component type:[/] {name}. " +
					$"Available: {string.Join(", ", ComponentTemplates.Keys.Order())}");
				return 1;
			}

			string filePath = Path.GetFullPath(Path.Combine(outputDir, $"{name}-example.md"));
			if (File.Exists(filePath))
			{
				AnsiConsole.MarkupLine($"[yellow]{filePath} already exists. Skipping.[/]");
				return 0;
			}

			EnsureDirectoryAndWrite(filePath, template);

			AnsiConsole.MarkupLine($"[green]Created:[/] {filePath}");
			return 0;
		});

		return command;
	}
}
