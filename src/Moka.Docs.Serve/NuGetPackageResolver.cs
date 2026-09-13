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

		try
		{
			ResolvedPackages published = await PublishAsync(packageSpecs, tempDir, ct);
			if (published.Error is not null)
			{
				return published;
			}

			var assemblies = new List<Assembly>();
			var namespaces = new HashSet<string>();

			foreach (string dll in published.AssemblyPaths)
			{
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
					logger.LogDebug("Skipped {File}: {Error}", Path.GetFileName(dll), ex.Message);
				}
			}

			logger.LogInformation("Resolved {AsmCount} assemblies with {NsCount} namespaces",
				assemblies.Count, namespaces.Count);

			return new ResolvedPackages
			{
				Assemblies = assemblies,
				AssemblyPaths = published.AssemblyPaths,
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
	///     Publishes the packages into <paramref name="directory" /> and returns the assembly paths
	///     without loading them, for a REPL worker process to load. The files stay until the caller
	///     deletes the directory.
	/// </summary>
	internal async Task<ResolvedPackages> PublishAsync(IReadOnlyList<string> packageSpecs, string directory,
		CancellationToken ct)
	{
		Directory.CreateDirectory(directory);

		string csproj = BuildProjectFile(packageSpecs);
		string projectPath = Path.Combine(directory, "ReplPackages.csproj");
		await File.WriteAllTextAsync(projectPath, csproj, ct);

		logger.LogInformation("Resolving {Count} NuGet package(s) for REPL...", packageSpecs.Count);

		string publishDir = Path.Combine(directory, "publish");

		// Run dotnet publish to get all DLLs in one folder
		(int exitCode, string output) = await RunDotnetAsync(
			$"publish \"{projectPath}\" -c Release -o \"{publishDir}\" --nologo -v quiet",
			directory, ct);

		if (exitCode != 0)
		{
			logger.LogError("dotnet publish failed with exit code {ExitCode} while resolving REPL packages",
				exitCode);
			return new ResolvedPackages
			{
				Error = $"dotnet publish exited with code {exitCode}"
				        + (string.IsNullOrWhiteSpace(output) ? "" : $": {output.Trim()}")
			};
		}

		// ReplPackages is the temporary project itself, not one of the packages.
		return new ResolvedPackages
		{
			AssemblyPaths = Directory.EnumerateFiles(publishDir, "*.dll")
				.Where(dll => Path.GetFileNameWithoutExtension(dll) is var name
				              && name != "ReplPackages" && !IsSharedFrameworkAssembly(name))
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList()
		};
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

	/// <summary>
	///     Whether an assembly ships with the running .NET runtime, which has already loaded it.
	/// </summary>
	/// <remarks>
	///     This used to skip every file starting with "System.", which dropped packages such as
	///     System.Reactive and System.IO.Abstractions from the REPL.
	/// </remarks>
	internal static bool IsSharedFrameworkAssembly(string assemblyName)
	{
		string? frameworkDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
		return frameworkDir is not null && File.Exists(Path.Combine(frameworkDir, assemblyName + ".dll"));
	}

	private async Task<(int ExitCode, string Output)> RunDotnetAsync(string arguments, string workingDir,
		CancellationToken ct)
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

		// Both streams: restore errors such as NU1101 (package not found) are written to stdout.
		return (process.ExitCode, $"{stdout}\n{stderr}".Trim());
	}

	internal static IEnumerable<string> GetRootNamespaces(Assembly assembly)
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

		/// <summary>
		///     Paths of the package assemblies. Filled in both modes; a REPL worker process loads these
		///     instead of <see cref="Assemblies" />.
		/// </summary>
		public IReadOnlyList<string> AssemblyPaths { get; init; } = [];

		/// <summary>Root namespaces discovered from the loaded assemblies.</summary>
		public IReadOnlyList<string> Namespaces { get; init; } = [];

		/// <summary>Why resolving failed, or <c>null</c> when it didn't.</summary>
		public string? Error { get; init; }
	}
}
