using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine.Phases;

namespace Moka.Docs.Plugins.PythonApi;

/// <summary>
///     MokaDocs plugin that generates API reference documentation for Python libraries.
///     <para>
///         The plugin invokes a bundled Python analyzer script (<c>mokadocs_python_analyzer.py</c>)
///         via subprocess, which uses the standard library's <c>ast</c> module to parse
///         <c>.py</c> source files and output a JSON file matching the mokadocs
///         <see cref="ApiReference" /> schema. The C# side then deserializes the JSON and
///         generates doc pages using the shared <see cref="ApiPageRenderer" />, so Python
///         API pages look identical to C# API pages.
///     </para>
/// </summary>
public sealed class PythonApiPlugin : IMokaPlugin
{
	private const string _defaultLabel = "Python API";
	private const string _defaultRoutePrefix = "/python-api";
	private const string _defaultDocstringFormat = "google";
	private const string _pythonScriptResourceName = "mokadocs_python_analyzer.py";

	private string? _sourceDir;
	private string _label = _defaultLabel;
	private string _routePrefix = _defaultRoutePrefix;
	private string _docstringFormat = _defaultDocstringFormat;
	private string _pythonPath = "python3";

	/// <inheritdoc />
	public string Id => "mokadocs-python-api";

	/// <inheritdoc />
	public string Name => "Python API Reference";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("Python API reference plugin initialized");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		// ── Parse options ──────────────────────────────────────────────────
		if (context.Options.TryGetValue("source", out object? srcObj)
		    && srcObj is string srcStr
		    && !string.IsNullOrWhiteSpace(srcStr))
		{
			_sourceDir = Path.GetFullPath(Path.Combine(buildContext.RootDirectory, srcStr));
		}

		if (string.IsNullOrWhiteSpace(_sourceDir))
		{
			context.LogError(
				"mokadocs-python-api: the 'source' option is required. " +
				"Set it to the path of your Python source directory.");
			return;
		}

		if (!Directory.Exists(_sourceDir))
		{
			context.LogError($"mokadocs-python-api: source directory does not exist: {_sourceDir}");
			return;
		}

		if (context.Options.TryGetValue("label", out object? labelObj) && labelObj is string labelStr)
		{
			_label = labelStr;
		}

		if (context.Options.TryGetValue("routePrefix", out object? rpObj) && rpObj is string rpStr)
		{
			_routePrefix = rpStr.TrimEnd('/');
			if (!_routePrefix.StartsWith('/'))
			{
				_routePrefix = "/" + _routePrefix;
			}
		}

		if (context.Options.TryGetValue("docstringFormat", out object? fmtObj) && fmtObj is string fmtStr)
		{
			_docstringFormat = fmtStr;
		}

		if (context.Options.TryGetValue("pythonPath", out object? pyObj) && pyObj is string pyStr)
		{
			_pythonPath = pyStr;
		}

		// ── Extract the bundled Python analyzer script ─────────────────────
		string scriptPath = ExtractAnalyzerScript();

		// ── Invoke the Python subprocess ──────────────────────────────────
		string jsonOutputPath = Path.Combine(Path.GetTempPath(), $"mokadocs-python-api-{Guid.NewGuid():N}.json");

		try
		{
			bool success = await InvokePythonAnalyzerAsync(
				scriptPath, _sourceDir, jsonOutputPath, _docstringFormat, context, ct);

			if (!success)
			{
				return; // Error already logged.
			}

			// ── Deserialize JSON into ApiModels ───────────────────────────
			string json = await File.ReadAllTextAsync(jsonOutputPath, ct);
			PythonAnalysisDto? dto = JsonSerializer.Deserialize<PythonAnalysisDto>(json);
			if (dto is null)
			{
				context.LogError("mokadocs-python-api: Python analyzer returned empty or invalid JSON.");
				return;
			}

			ApiReference apiRef = PythonApiMapper.ToApiReference(dto);

			if (apiRef.Namespaces.Count == 0)
			{
				context.LogWarning(
					$"mokadocs-python-api: no public Python types found in '{_sourceDir}'. " +
					"Check that your source directory contains .py files with public classes or functions.");
				return;
			}

			// ── Generate doc pages ────────────────────────────────────────
			GeneratePages(apiRef, buildContext, context);
		}
		finally
		{
			// Clean up temp files
			try
			{
				File.Delete(jsonOutputPath);
			}
			catch
			{
				// ignored
			}

			try
			{
				File.Delete(scriptPath);
			}
			catch
			{
				// ignored
			}
		}
	}

	// ── Python script extraction ──────────────────────────────────────────

	/// <summary>
	///     Extracts the bundled <c>mokadocs_python_analyzer.py</c> from the assembly's
	///     embedded resources to a temp file so it can be invoked via subprocess.
	/// </summary>
	private static string ExtractAnalyzerScript()
	{
		Assembly assembly = typeof(PythonApiPlugin).Assembly;
		string resourceName = assembly.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith(_pythonScriptResourceName, StringComparison.OrdinalIgnoreCase))
			?? throw new InvalidOperationException(
				$"mokadocs-python-api: embedded resource '{_pythonScriptResourceName}' not found in {assembly.FullName}. " +
				"Ensure the .py file is included as an EmbeddedResource in the csproj.");

		using Stream stream = assembly.GetManifestResourceStream(resourceName)
		                      ?? throw new InvalidOperationException(
			                      $"mokadocs-python-api: could not open embedded resource stream for '{resourceName}'.");

		string tempPath = Path.Combine(Path.GetTempPath(), $"mokadocs_{_pythonScriptResourceName}");
		using FileStream fs = File.Create(tempPath);
		stream.CopyTo(fs);
		return tempPath;
	}

	// ── Python subprocess invocation ──────────────────────────────────────

	private async Task<bool> InvokePythonAnalyzerAsync(
		string scriptPath, string sourceDir, string outputPath, string docstringFormat,
		IPluginContext context, CancellationToken ct)
	{
		// Try python3 first, fall back to python
		string[] pythonCandidates = _pythonPath == "python3"
			? ["python3", "python"]
			: [_pythonPath];

		foreach (string python in pythonCandidates)
		{
			string args = $"\"{scriptPath}\" \"{sourceDir}\" --output \"{outputPath}\" --format {docstringFormat}";

			var psi = new ProcessStartInfo(python, args)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = sourceDir
			};

			try
			{
				using Process? proc = Process.Start(psi);
				if (proc is null)
				{
					continue; // Python not found, try next candidate
				}

				string stdout = await proc.StandardOutput.ReadToEndAsync(ct);
				string stderr = await proc.StandardError.ReadToEndAsync(ct);
				await proc.WaitForExitAsync(ct);

				if (proc.ExitCode != 0)
				{
					context.LogError(
						$"mokadocs-python-api: Python analyzer exited with code {proc.ExitCode}.\n" +
						(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr));
					return false;
				}

				if (!File.Exists(outputPath))
				{
					context.LogError(
						$"mokadocs-python-api: Python analyzer completed but output file not found: {outputPath}");
					return false;
				}

				// Log any stderr warnings
				if (!string.IsNullOrWhiteSpace(stderr))
				{
					foreach (string line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
					{
						if (line.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
						{
							context.LogWarning($"mokadocs-python-api: {line}");
						}
					}
				}

				return true;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				// Python executable not found on PATH, try next
				continue;
			}
		}

		context.LogError(
			$"mokadocs-python-api: could not find Python on PATH. " +
			$"Tried: {string.Join(", ", pythonCandidates)}. " +
			"Install Python 3.9+ and ensure it's on your system PATH, " +
			"or set the 'pythonPath' plugin option to the full path of your Python executable.");
		return false;
	}

	// ── Page generation (reuses ApiPageRenderer) ──────────────────────────

	private void GeneratePages(ApiReference apiRef, BuildContext buildContext, IPluginContext context)
	{
		// Generate index page
		var indexHtml = new StringBuilder();
		indexHtml.AppendLine($"<h1>{WebUtility.HtmlEncode(_label)}</h1>");

		foreach (ApiNamespace ns in apiRef.Namespaces)
		{
			indexHtml.AppendLine($"<h2>{WebUtility.HtmlEncode(ns.Name)}</h2>");
			indexHtml.AppendLine("<div class=\"table-responsive\"><table class=\"api-member-table\">");
			indexHtml.AppendLine("<thead><tr><th>Name</th><th>Kind</th><th>Description</th></tr></thead>");
			indexHtml.AppendLine("<tbody>");

			foreach (ApiType type in ns.Types)
			{
				string route = BuildTypeRoute(ns.Name, type.Name);
				string kindBadge = type.Kind.ToString().ToLowerInvariant();
				string summary = type.Documentation?.Summary ?? "";
				indexHtml.AppendLine("<tr>");
				indexHtml.AppendLine(
					$"<td><a href=\"{route}\">{WebUtility.HtmlEncode(type.Name)}</a></td>");
				indexHtml.AppendLine(
					$"<td><span class=\"api-badge api-badge-{kindBadge}\">{type.Kind}</span></td>");
				indexHtml.AppendLine($"<td>{summary}</td>");
				indexHtml.AppendLine("</tr>");
			}

			indexHtml.AppendLine("</tbody></table></div>");
		}

		buildContext.Pages.Add(new DocPage
		{
			FrontMatter = new FrontMatter
			{
				Title = _label,
				Description = $"API reference for the Python library",
				Layout = "default"
			},
			Content = new PageContent
			{
				Html = indexHtml.ToString(),
				PlainText = ""
			},
			Route = _routePrefix,
			Origin = PageOrigin.ApiGenerated
		});

		// Generate per-type pages
		List<ApiType> allTypes = apiRef.Namespaces.SelectMany(n => n.Types).ToList();

		foreach (ApiNamespace ns in apiRef.Namespaces)
		foreach (ApiType type in ns.Types)
		{
			string route = BuildTypeRoute(ns.Name, type.Name);
			string apiHtml = ApiPageRenderer.RenderType(type, allTypes);
			TableOfContents toc = ApiPageRenderer.BuildTocForType(type);

			buildContext.Pages.Add(new DocPage
			{
				FrontMatter = new FrontMatter
				{
					Title = type.Name,
					Description = type.Documentation?.Summary ?? $"API documentation for {type.FullName}",
					Layout = "default"
				},
				Content = new PageContent
				{
					Html = apiHtml,
					PlainText = type.Documentation?.Summary ?? ""
				},
				TableOfContents = toc,
				Route = route,
				Origin = PageOrigin.ApiGenerated
			});
		}

		context.LogInfo(
			$"mokadocs-python-api: generated {allTypes.Count} type page(s) across " +
			$"{apiRef.Namespaces.Count} module(s) at {_routePrefix}");
	}

	private string BuildTypeRoute(string namespaceName, string typeName)
	{
		string nsPath = namespaceName.Replace('.', '/');
		string safeName = typeName.Replace('<', '-').Replace('>', '-');
		return $"{_routePrefix}/{nsPath}/{safeName}".ToLowerInvariant();
	}
}
