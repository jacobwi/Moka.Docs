using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     Compiles plugin assemblies in memory and writes them to a folder, the way a plugin
///     author's build output looks to <c>plugins[].path</c>.
/// </summary>
internal static class PluginAssemblyBuilder
{
	/// <summary>A plugin with the given id that adds the page <c>/{route}</c> holding its <c>text</c> option.</summary>
	internal static string PagePluginSource(string id, string route, string className = "ProbePlugin",
		string extraBody = "\"\"") =>
		$$"""
		  using System;
		  using System.Threading;
		  using System.Threading.Tasks;
		  using Microsoft.Extensions.Logging;
		  using Moka.Docs.Core.Content;
		  using Moka.Docs.Core.Pipeline;
		  using Moka.Docs.Plugins;

		  namespace Probe;

		  public sealed class {{className}}(ILogger<{{className}}> logger) : IMokaPlugin
		  {
		      private string _text = "default";

		      public string Id => "{{id}}";
		      public string Name => "Probe";
		      public string Version => "1.0.0";

		      public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
		      {
		          if (context.Options.TryGetValue("text", out object? text) && text is string value)
		          {
		              _text = value;
		          }

		          logger.LogDebug("initialized");
		          return Task.CompletedTask;
		      }

		      public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		      {
		          buildContext.Pages.Add(new DocPage
		          {
		              Route = "/{{route}}",
		              FrontMatter = new FrontMatter { Title = "Probe" },
		              Content = new PageContent { Html = "<p>" + _text + "|" + {{extraBody}} + "</p>" }
		          });
		          return Task.CompletedTask;
		      }
		  }
		  """;

	/// <summary>
	///     Compiles <paramref name="source" /> into <c>{folder}/{assemblyName}.dll</c> against the
	///     framework and every assembly of this test run, minus <paramref name="leaveOut" />, plus
	///     <paramref name="references" />.
	/// </summary>
	/// <returns>The path of the assembly.</returns>
	internal static string Compile(string folder, string assemblyName, string source,
		IEnumerable<string>? references = null, IEnumerable<string>? leaveOut = null, string? version = null)
	{
		var left = new HashSet<string>(leaveOut ?? [], StringComparer.OrdinalIgnoreCase);
		string platformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
		List<MetadataReference> metadata = platformAssemblies
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Where(path => !left.Contains(Path.GetFileNameWithoutExtension(path)))
			.Concat(references ?? [])
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToList();

		string versioned = version is null
			? source
			: $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]\n" + source;

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName,
			[CSharpSyntaxTree.ParseText(versioned, new CSharpParseOptions(LanguageVersion.Latest))],
			metadata,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));

		Directory.CreateDirectory(folder);
		string path = Path.Combine(folder, assemblyName + ".dll");
		using (FileStream file = File.Create(path))
		{
			EmitResult result = compilation.Emit(file);
			if (!result.Success)
			{
				throw new InvalidOperationException($"{assemblyName} did not compile: " +
				                                    string.Join(Environment.NewLine,
					                                    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
			}
		}

		return path;
	}
}
