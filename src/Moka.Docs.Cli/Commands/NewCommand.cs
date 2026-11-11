using System.CommandLine;
using System.Globalization;
using System.Text;
using Moka.Docs.Cli.Diagnostics;
using Spectre.Console;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     Scaffolding command: <c>mokadocs new page|plugin|component</c>.
/// </summary>
internal static class NewCommand
{
	// ──────────────────────── component markdown templates ───────────────────────

	private static readonly Dictionary<string, string> _componentTemplates = new()
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

		           :::card{title="Info Card" icon="info" variant="info"}
		           The info variant adds a blue left border.
		           :::

		           :::card{title="Success Card" icon="check" variant="success"}
		           Other variants are success and warning.
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
		                 - [Getting Started](/getting-started/quickstart) - Set up your first project
		                 - [Configuration](/configuration/site-config) - Customize your site
		                 - [Markdown Features](/guide/markdown) - Explore supported syntax
		                 - [Deployment](/advanced/deployment) - Deploy to production
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

		                ## v1.1.0 - 2025-07-01

		                ### Added
		                - New feature description here
		                - Another new feature

		                ### Fixed
		                - Bug fix description here

		                ## v1.0.0 - 2025-06-01

		                ### Added
		                - Initial release feature one
		                - Initial release feature two

		                :::
		                """
	};

	/// <summary>The component example pages, keyed by component name.</summary>
	internal static IReadOnlyDictionary<string, string> ComponentTemplates => _componentTemplates;

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

	/// <summary>
	///     The plugin class name for a PascalCase plugin name: <c>MyCustom</c> and
	///     <c>MyCustomPlugin</c> both become <c>MyCustomPlugin</c>. Appending the suffix
	///     unconditionally produced <c>MyCustomPluginPlugin</c>.
	/// </summary>
	internal static string ToPluginClassName(string pascalName) =>
		pascalName.Length > "Plugin".Length && pascalName.EndsWith("Plugin", StringComparison.Ordinal)
			? pascalName
			: pascalName + "Plugin";

	/// <summary>
	///     <c>MyCustomPlugin</c> and <c>my-custom-plugin</c> both become
	///     <c>my-custom-plugin</c>, so the plugin id does not depend on how the name was typed.
	/// </summary>
	internal static string ToKebabCase(string name)
	{
		var sb = new StringBuilder(name.Length + 4);
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (char.IsUpper(c) && i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
			{
				sb.Append('-');
			}

			sb.Append(char.ToLowerInvariant(c));
		}

		return sb.ToString();
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

		var titleOpt = new Option<string?>("--title", "-t")
			{ Description = "Page title (defaults to name in Title Case)" };

		var pathOpt = new Option<string>("--path", "-p")
			{ Description = "Output directory", DefaultValueFactory = _ => "./docs" };

		var layoutOpt = new Option<string>("--layout")
			{ Description = "Layout template (default or landing)", DefaultValueFactory = _ => "default" };

		var orderOpt = new Option<int?>("--order") { Description = "Sort order number" };

		var command = new Command("page", "Create a new Markdown documentation page")
		{
			nameArg, titleOpt, pathOpt, layoutOpt, orderOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!;
			// A name such as guides/overview creates a subfolder; the title comes from the page.
			string title = parseResult.GetValue(titleOpt) ?? ToTitleCase(Path.GetFileName(name));
			string outputDir = parseResult.GetValue(pathOpt)!;
			string layout = parseResult.GetValue(layoutOpt)!;
			int? order = parseResult.GetValue(orderOpt);

			AnsiConsole.MarkupLine("[bold green]mokadocs new page[/] - Scaffolding new page...");

			string filePath = Path.GetFullPath(Path.Combine(outputDir, $"{name}.md"));
			if (File.Exists(filePath))
			{
				AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(filePath)} already exists. Skipping.[/]");
				return 0;
			}

			EnsureDirectoryAndWrite(filePath, BuildPageMarkdown(title, layout, order));

			AnsiConsole.MarkupLine($"[green]Created:[/] {Markup.Escape(filePath)}");
			return 0;
		});

		return command;
	}

	/// <summary>
	///     The Markdown for a new page. The title is quoted when YAML needs it: written raw, a
	///     title such as <c>Api: v2</c> produced front matter that failed to parse.
	/// </summary>
	internal static string BuildPageMarkdown(string title, string layout, int? order)
	{
		var sb = new StringBuilder();
		sb.AppendLine("---");
		sb.AppendLine($"title: {DoctorChecks.QuoteYamlScalar(title)}");
		if (order.HasValue)
		{
			sb.AppendLine($"order: {order.Value}");
		}

		sb.AppendLine($"layout: {DoctorChecks.QuoteYamlScalar(layout)}");
		sb.AppendLine("---");
		sb.AppendLine();
		sb.AppendLine($"# {title}");
		sb.AppendLine();
		sb.AppendLine("Write your content here.");
		return sb.ToString();
	}

	// ──────────────────────────── mokadocs new plugin ────────────────────────────

	private static Command CreatePluginCommand()
	{
		var nameArg = new Argument<string>("name")
			{ Description = "Plugin name in kebab-case (e.g. my-custom-plugin)" };

		var pathOpt = new Option<string>("--path", "-p")
			{ Description = "Output directory", DefaultValueFactory = _ => "." };

		var command = new Command("plugin", "Scaffold a new MokaDocs plugin project")
		{
			nameArg, pathOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!;
			string outputDir = parseResult.GetValue(pathOpt)!;
			string pascal = ToPascalCase(name);
			string className = ToPluginClassName(pascal);
			string pluginId = $"mokadocs-{ToKebabCase(name)}";

			AnsiConsole.MarkupLine("[bold green]mokadocs new plugin[/] - Scaffolding new plugin...");

			string projectName = $"Moka.Docs.Plugins.{pascal}";
			string projectDir = Path.GetFullPath(Path.Combine(outputDir, projectName));

			if (Directory.Exists(projectDir))
			{
				AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(projectDir)} already exists. Skipping.[/]");
				return 0;
			}

			string kebab = ToKebabCase(name);
			EnsureDirectoryAndWrite(
				Path.Combine(projectDir, $"{projectName}.csproj"),
				BuildPluginProject(projectName, CliVersion.Current));
			EnsureDirectoryAndWrite(
				Path.Combine(projectDir, $"{className}.cs"),
				BuildPluginClass(projectName, className, pascal, pluginId, "/" + kebab));

			string declaredPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), projectDir).Replace('\\', '/');
			if (!Path.IsPathRooted(declaredPath) && !declaredPath.StartsWith('.'))
			{
				declaredPath = "./" + declaredPath;
			}

			AnsiConsole.MarkupLine(
				$"[green]Created:[/] {Markup.Escape(Path.Combine(projectDir, $"{projectName}.csproj"))}");
			AnsiConsole.MarkupLine($"[green]Created:[/] {Markup.Escape(Path.Combine(projectDir, $"{className}.cs"))}");
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"Plugin id: [bold]{Markup.Escape(pluginId)}[/]");
			AnsiConsole.MarkupLine(
				"Declare it in mokadocs.yaml, with the path relative to that file. mokadocs builds the project " +
				"when it loads the plugin:");
			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine("plugins:");
			AnsiConsole.WriteLine($"  - name: {pluginId}");
			AnsiConsole.WriteLine($"    path: {DoctorChecks.QuoteYamlScalar(declaredPath)}");
			return 0;
		});

		return command;
	}

	/// <summary>
	///     The project file of a new plugin: a net9.0 library, which both the net9.0 and the
	///     net10.0 build of mokadocs can load, referencing <c>Moka.Docs.Plugins</c> at the tool's
	///     own version.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The project references this used to write were relative to the MokaDocs source tree,
	///         so the scaffold only built when generated inside a clone of this repository.
	///     </para>
	///     <para>
	///         Package pruning is on, as it is by default only for net10.0 projects: without it the
	///         restore also fetched packages the framework already has, such as System.Memory, which
	///         Roslyn lists for net9.0.
	///     </para>
	/// </remarks>
	internal static string BuildPluginProject(string projectName, string mokaDocsVersion) =>
		$"""
		 <Project Sdk="Microsoft.NET.Sdk">

		   <PropertyGroup>
		     <TargetFramework>net9.0</TargetFramework>
		     <RootNamespace>{projectName}</RootNamespace>
		     <ImplicitUsings>enable</ImplicitUsings>
		     <Nullable>enable</Nullable>
		     <EnableDynamicLoading>true</EnableDynamicLoading>
		     <RestoreEnablePackagePruning>true</RestoreEnablePackagePruning>
		     <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
		   </PropertyGroup>

		   <ItemGroup>
		     <!-- mokadocs supplies Moka.Docs.Plugins when it loads the plugin, so its files stay out of the build output. -->
		     <PackageReference Include="Moka.Docs.Plugins" Version="{mokaDocsVersion}" ExcludeAssets="runtime" />
		   </ItemGroup>

		 </Project>
		 """;

	/// <summary>
	///     The starter plugin class. It adds a page at <paramref name="route" /> that shows its
	///     <c>message</c> option, so a site that declares it shows the plugin working.
	/// </summary>
	internal static string BuildPluginClass(string projectName, string className, string displayName,
		string pluginId, string route) =>
		$$"""
		  using System.Net;
		  using Moka.Docs.Core.Content;
		  using Moka.Docs.Core.Pipeline;
		  using Moka.Docs.Plugins;

		  namespace {{projectName}};

		  /// <summary>
		  ///     A MokaDocs plugin. It adds the page {{route}}, which shows the message option.
		  /// </summary>
		  public sealed class {{className}} : IMokaPlugin
		  {
		      private string _message = "Hello from {{displayName}}.";

		      public string Id => "{{pluginId}}";
		      public string Name => "{{displayName}}";
		      public string Version => "1.0.0";

		      public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
		      {
		          if (context.Options.TryGetValue("message", out object? message) && message is string text)
		          {
		              _message = text;
		          }

		          return Task.CompletedTask;
		      }

		      public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		      {
		          buildContext.Pages.Add(new DocPage
		          {
		              Route = "{{route}}",
		              FrontMatter = new FrontMatter { Title = Name },
		              Content = new PageContent { Html = $"<p>{WebUtility.HtmlEncode(_message)}</p>", PlainText = _message }
		          });

		          context.LogInfo($"{Name} added the page {{route}}");
		          return Task.CompletedTask;
		      }
		  }
		  """;

	// ────────────────────────── mokadocs new component ───────────────────────────

	private static Command CreateComponentCommand()
	{
		var nameArg = new Argument<string>("name")
			{ Description = "Component type (card, steps, link-cards, code-group, changelog)" };

		var pathOpt = new Option<string>("--path", "-p")
			{ Description = "Output directory", DefaultValueFactory = _ => "./docs" };

		var command = new Command("component", "Scaffold a Markdown page showcasing a component")
		{
			nameArg, pathOpt
		};

		command.SetAction(parseResult =>
		{
			string name = parseResult.GetValue(nameArg)!.ToLowerInvariant();
			string outputDir = parseResult.GetValue(pathOpt)!;

			AnsiConsole.MarkupLine("[bold green]mokadocs new component[/] - Scaffolding component example...");

			if (!_componentTemplates.TryGetValue(name, out string? template))
			{
				AnsiConsole.MarkupLine(
					$"[red]Unknown component type:[/] {Markup.Escape(name)}. " +
					$"Available: {string.Join(", ", _componentTemplates.Keys.Order())}");
				return 1;
			}

			string filePath = Path.GetFullPath(Path.Combine(outputDir, $"{name}-example.md"));
			if (File.Exists(filePath))
			{
				AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(filePath)} already exists. Skipping.[/]");
				return 0;
			}

			EnsureDirectoryAndWrite(filePath, template);

			AnsiConsole.MarkupLine($"[green]Created:[/] {Markup.Escape(filePath)}");
			return 0;
		});

		return command;
	}
}
