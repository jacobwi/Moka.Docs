using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Resolves NuGet package specifications (e.g. "Newtonsoft.Json", "Humanizer@2.14.1")
///     into loaded assemblies by creating a temporary project and publishing it.
/// </summary>
public sealed class NuGetPackageResolver(ILogger logger)
{
	/// <summary>
	///     Resolves the given package specifications to assemblies.
	///     Each spec is either "PackageName" or "PackageName@Version".
	/// </summary>
	public async Task<ResolvedPackages> ResolveAsync(
		IReadOnlyList<string> packageSpecs,
		CancellationToken ct = default)
	{
		if (packageSpecs.Count == 0)
		{
			return new ResolvedPackages();
		}

		string tempDir = Path.Combine(Path.GetTempPath(), "mokadocs-repl-" + Guid.NewGuid().ToString("N")[..8]);
		Directory.CreateDirectory(tempDir);

		try
		{
			string csproj = BuildProjectFile(packageSpecs);
			string projectPath = Path.Combine(tempDir, "ReplPackages.csproj");
			await File.WriteAllTextAsync(projectPath, csproj, ct);

			logger.LogInformation("Resolving {Count} NuGet package(s) for REPL...", packageSpecs.Count);

			string publishDir = Path.Combine(tempDir, "publish");

			// Run dotnet publish to get all DLLs in one folder
			int exitCode = await RunDotnetAsync(
				$"publish \"{projectPath}\" -c Release -o \"{publishDir}\" --nologo -v quiet",
				tempDir, ct);

			if (exitCode != 0)
			{
				logger.LogError("dotnet publish failed with exit code {ExitCode} while resolving REPL packages",
					exitCode);
				return new ResolvedPackages();
			}

			// Load all DLLs from publish output (skip well-known framework assemblies)
			var assemblies = new List<Assembly>();
			var namespaces = new HashSet<string>();
			string[] frameworkPrefixes = new[]
			{
				"System.", "Microsoft.NETCore", "Microsoft.CSharp",
				"mscorlib", "netstandard", "WindowsBase"
			};

			foreach (string dll in Directory.EnumerateFiles(publishDir, "*.dll"))
			{
				string fileName = Path.GetFileNameWithoutExtension(dll);

				// Skip framework assemblies that are already loaded
				if (frameworkPrefixes.Any(p => fileName.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}

				try
				{
					var asm = Assembly.LoadFrom(dll);
					assemblies.Add(asm);

					// Extract the root namespace from exported types
					foreach (string ns in GetRootNamespaces(asm))
					{
						namespaces.Add(ns);
					}

					logger.LogDebug("Loaded assembly: {Name}", asm.GetName().Name);
				}
				catch (Exception ex)
				{
					logger.LogDebug("Skipped {File}: {Error}", fileName, ex.Message);
				}
			}

			logger.LogInformation("Resolved {AsmCount} assemblies with {NsCount} namespaces",
				assemblies.Count, namespaces.Count);

			return new ResolvedPackages
			{
				Assemblies = assemblies,
				Namespaces = namespaces.OrderBy(n => n).ToList()
			};
		}
		finally
		{
			// Clean up temp directory
			try
			{
				Directory.Delete(tempDir, true);
			}
			catch
			{
				/* best effort */
			}
		}
	}

	/// <summary>
	///     Loads a single assembly from a file path (e.g. a project's output DLL).
	///     Returns the assembly and its root namespaces.
	/// </summary>
	public ResolvedPackages LoadAssemblyFromPath(string dllPath)
	{
		if (!File.Exists(dllPath))
		{
			logger.LogWarning("Assembly not found: {Path}", dllPath);
			return new ResolvedPackages();
		}

		try
		{
			var asm = Assembly.LoadFrom(dllPath);
			var namespaces = GetRootNamespaces(asm).ToList();
			logger.LogInformation("Loaded project assembly: {Name} ({NsCount} namespaces)",
				asm.GetName().Name, namespaces.Count);
			return new ResolvedPackages
			{
				Assemblies = [asm],
				Namespaces = namespaces
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load assembly from {Path}", dllPath);
			return new ResolvedPackages();
		}
	}

	private static string BuildProjectFile(IReadOnlyList<string> packageSpecs)
	{
		var sb = new StringBuilder();
		sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
		sb.AppendLine("  <PropertyGroup>");
		sb.AppendLine("    <TargetFramework>net9.0</TargetFramework>");
		sb.AppendLine("    <OutputType>Library</OutputType>");
		sb.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
		sb.AppendLine("  </PropertyGroup>");
		sb.AppendLine("  <ItemGroup>");

		foreach (string spec in packageSpecs)
		{
			(string name, string? version) = ParseSpec(spec);
			if (version is not null)
			{
				sb.AppendLine($"    <PackageReference Include=\"{name}\" Version=\"{version}\" />");
			}
			else
			{
				sb.AppendLine($"    <PackageReference Include=\"{name}\" Version=\"*\" />");
			}
		}

		sb.AppendLine("  </ItemGroup>");
		sb.AppendLine("</Project>");
		return sb.ToString();
	}

	private static (string Name, string? Version) ParseSpec(string spec)
	{
		int atIndex = spec.LastIndexOf('@');
		if (atIndex > 0)
		{
			return (spec[..atIndex].Trim(), spec[(atIndex + 1)..].Trim());
		}

		return (spec.Trim(), null);
	}

	private async Task<int> RunDotnetAsync(string arguments, string workingDir, CancellationToken ct)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = arguments,
			WorkingDirectory = workingDir,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = new Process { StartInfo = psi };
		process.Start();

		// Read output asynchronously to avoid deadlocks
		Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
		Task<string> stderrTask = process.StandardError.ReadToEndAsync(ct);

		await process.WaitForExitAsync(ct);

		string stdout = await stdoutTask;
		string stderr = await stderrTask;

		if (!string.IsNullOrWhiteSpace(stderr))
		{
			logger.LogDebug("dotnet stderr: {Stderr}", stderr.Trim());
		}

		if (!string.IsNullOrWhiteSpace(stdout))
		{
			logger.LogDebug("dotnet stdout: {Stdout}", stdout.Trim());
		}

		return process.ExitCode;
	}

	private static IEnumerable<string> GetRootNamespaces(Assembly assembly)
	{
		var namespaces = new HashSet<string>();
		try
		{
			foreach (Type type in assembly.GetExportedTypes())
			{
				if (string.IsNullOrEmpty(type.Namespace))
				{
					continue;
				}

				// Use the top-level namespace (e.g. "Newtonsoft" from "Newtonsoft.Json.Linq")
				// but also include immediate child (e.g. "Newtonsoft.Json")
				string[] parts = type.Namespace.Split('.');
				if (parts.Length >= 1)
				{
					namespaces.Add(parts[0]);
				}

				if (parts.Length >= 2)
				{
					namespaces.Add(parts[0] + "." + parts[1]);
				}
			}
		}
		catch
		{
			// Some assemblies may throw on GetExportedTypes
		}

		return namespaces;
	}

	/// <summary>
	///     A resolved package containing the loaded assemblies and discovered root namespaces.
	/// </summary>
	public sealed class ResolvedPackages
	{
		/// <summary>Assemblies loaded from the published output.</summary>
		public IReadOnlyList<Assembly> Assemblies { get; init; } = [];

		/// <summary>Root namespaces discovered from the loaded assemblies.</summary>
		public IReadOnlyList<string> Namespaces { get; init; } = [];
	}
}
