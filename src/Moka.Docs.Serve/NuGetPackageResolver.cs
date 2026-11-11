using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Moka.Docs.Serve;

/// <summary>
///     Resolves NuGet package specifications (e.g. "Newtonsoft.Json", "Humanizer@2.14.1")
///     into loaded assemblies by creating a temporary project and publishing it.
/// </summary>
public sealed partial class NuGetPackageResolver(ILogger logger)
{
	private const int _maxPackageIdLength = 100;
	private const string _versionNumbers = @"[0-9]+(?:\.[0-9]+){0,3}";
	private const string _versionLabel = "[0-9A-Za-z-]+";
	private const string _exactVersion = _versionNumbers
	                                     + "(?:-" + _versionLabel + @"(?:\." + _versionLabel + ")*)?"
	                                     + @"(?:\+" + _versionLabel + @"(?:\." + _versionLabel + ")*)?";
	private const string _floatingVersion = @"\*(?:-\*)?"
	                                        + @"|[0-9]+(?:\.[0-9]+){0,2}\.\*(?:-\*)?"
	                                        + "|" + _versionNumbers + "-(?:" + _versionLabel + @"\.)*(?:" + _versionLabel + @")?\*";
	private const string _versionRange = @"[\[(]\s*(?:" + _exactVersion + @")?\s*(?:,\s*(?:" + _exactVersion + @")?\s*)?[\])]";

	/// <summary>
	///     Splits a package spec, <c>Id</c> or <c>Id@Version</c>, and checks both parts against
	///     NuGet's grammar: an id made of letters, digits and <c>_</c>, <c>-</c> or <c>.</c>
	///     separators (at most 100 characters), and a version, a floating version such as
	///     <c>1.*</c>, or a version range such as <c>[1.0,2.0)</c>.
	/// </summary>
	/// <remarks>
	///     A spec that passes is safe to write into a generated project file: neither part can
	///     hold quotes, angle brackets, <c>$</c>, <c>%</c> or <c>;</c>, which would break out of
	///     the attribute or change what MSBuild reads.
	/// </remarks>
	/// <param name="spec">The spec, for example <c>Humanizer@2.14.1</c>.</param>
	/// <param name="id">The package id, or an empty string when the spec is invalid.</param>
	/// <param name="version">The version, or <c>null</c> when the spec has none or is invalid.</param>
	/// <returns><c>true</c> when the id and the version, if present, are valid.</returns>
	public static bool TryParsePackageSpec(string spec, out string id, out string? version)
	{
		id = "";
		version = null;
		int at = spec.IndexOf('@', StringComparison.Ordinal);
		string candidateId = (at < 0 ? spec : spec[..at]).Trim();
		string? candidateVersion = at < 0 ? null : spec[(at + 1)..].Trim();

		if (candidateId.Length is 0 or > _maxPackageIdLength || !PackageIdPattern().IsMatch(candidateId))
		{
			return false;
		}

		if (candidateVersion is not null && !PackageVersionPattern().IsMatch(candidateVersion))
		{
			return false;
		}

		id = candidateId;
		version = candidateVersion;
		return true;
	}

	[GeneratedRegex(@"^\w+(?:[_.-]\w+)*\z")]
	private static partial Regex PackageIdPattern();

	[GeneratedRegex("^(?:" + _exactVersion + "|" + _floatingVersion + "|" + _versionRange + @")\z")]
	private static partial Regex PackageVersionPattern();

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
		if (packageSpecs.FirstOrDefault(spec => !TryParsePackageSpec(spec, out _, out _)) is { } invalid)
		{
			logger.LogWarning("REPL package '{Spec}' is not a NuGet package id with an optional @version", invalid);
			return new ResolvedPackages
			{
				Error = $"'{invalid}' is not a NuGet package id with an optional @version (for example Humanizer@2.14.1)"
			};
		}

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
			TryParsePackageSpec(spec, out string name, out string? version);
			sb.AppendLine($"    <PackageReference Include=\"{name}\" Version=\"{version ?? "*"}\" />");
		}

		sb.AppendLine("  </ItemGroup>");
		sb.AppendLine("</Project>");
		return sb.ToString();
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
