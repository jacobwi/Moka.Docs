using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Moka.Docs.Core;

namespace Moka.Docs.Plugins;

/// <summary>
///     Loads the assembly a <c>plugins[].path</c> names: a .dll, a project that is built first
///     with <c>dotnet build -c Release</c>, or a folder holding either.
/// </summary>
internal static class PluginAssemblyLoader
{
	private static readonly string[] _projectExtensions = [".csproj", ".fsproj", ".vbproj"];

	private static readonly StringComparer _pathComparer =
		OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	/// <summary>
	///     Plugin assemblies already loaded, by path. A file that has not changed is not loaded
	///     again, so reloading the configuration in <c>serve</c> doesn't pile up copies.
	/// </summary>
	private static readonly ConcurrentDictionary<string, LoadedFile> _loaded = new(_pathComparer);

	/// <summary>
	///     Finds, builds when needed, and loads the plugin assembly at <paramref name="path" />, and
	///     lists the classes in it that implement <see cref="IMokaPlugin" />.
	/// </summary>
	/// <param name="path">The full path from the declaration.</param>
	/// <param name="buildTimeout">How long building a project may take.</param>
	/// <param name="ct">Cancels a project build.</param>
	/// <returns>The plugin classes, or why there are none.</returns>
	internal static async Task<PluginAssemblyResult> LoadAsync(string path, TimeSpan buildTimeout, CancellationToken ct)
	{
		(string? assemblyPath, string? error) = await LocateAsync(path, buildTimeout, ct);
		return assemblyPath is null ? PluginAssemblyResult.Failed(error!) : Load(assemblyPath);
	}

	/// <summary>Loads a plugin assembly file and lists its plugin classes.</summary>
	internal static PluginAssemblyResult Load(string assemblyPath)
	{
		string fullPath = Path.GetFullPath(assemblyPath);
		Assembly assembly;
		try
		{
			var file = new FileInfo(fullPath);
			var stamp = (file.LastWriteTimeUtc, file.Length);
			if (!_loaded.TryGetValue(fullPath, out LoadedFile? loaded) || loaded.Stamp != stamp)
			{
				loaded = new LoadedFile(stamp, new PluginLoadContext(fullPath).LoadFromFile(fullPath));
				_loaded[fullPath] = loaded;
			}

			assembly = loaded.Assembly;
		}
		catch (BadImageFormatException)
		{
			return PluginAssemblyResult.Failed($"'{fullPath}' is not a .NET assembly");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return PluginAssemblyResult.Failed($"couldn't read '{fullPath}': {ex.Message}");
		}

		return CheckMokaDocsVersion(assembly) is { } versionError
			? PluginAssemblyResult.Failed(versionError)
			: FindPluginTypes(assembly, fullPath);
	}

	/// <summary>
	///     The target framework to build: the project's only one, or of several the highest a
	///     runtime of <paramref name="runtimeMajor" /> can load. <c>null</c> when it can load none.
	/// </summary>
	/// <param name="targetFramework">The project's <c>TargetFramework</c> property.</param>
	/// <param name="targetFrameworks">The project's <c>TargetFrameworks</c> property.</param>
	/// <param name="runtimeMajor">Major version of the runtime the plugin will run on.</param>
	internal static string? PickTargetFramework(string targetFramework, string targetFrameworks, int runtimeMajor)
	{
		if (targetFramework.Trim() is { Length: > 0 } single)
		{
			return TargetFrameworks.LoadableVersion(single, runtimeMajor) is null ? null : single;
		}

		return targetFrameworks
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(tfm => (Name: tfm, Version: TargetFrameworks.LoadableVersion(tfm, runtimeMajor)))
			.Where(tfm => tfm.Version is not null)
			.OrderByDescending(tfm => tfm.Version)
			.Select(tfm => tfm.Name)
			.FirstOrDefault();
	}

	private static async Task<(string? AssemblyPath, string? Error)> LocateAsync(string path, TimeSpan buildTimeout,
		CancellationToken ct)
	{
		if (Directory.Exists(path))
		{
			string[] projects = Directory.GetFiles(path).Where(IsProject).Order(_pathComparer).ToArray();
			return projects.Length switch
			{
				1 => await BuildProjectAsync(projects[0], buildTimeout, ct),
				> 1 => (null,
					$"the folder holds {projects.Length} projects ({string.Join(", ", projects.Select(Path.GetFileName))}); " +
					"point path at one of them"),
				_ => FindAssemblyInFolder(path)
			};
		}

		if (!File.Exists(path))
		{
			return (null, $"'{path}' doesn't exist");
		}

		if (IsProject(path))
		{
			return await BuildProjectAsync(path, buildTimeout, ct);
		}

		return Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
			? (path, null)
			: (null, $"'{path}' is not a .dll, a project file or a folder");
	}

	private static bool IsProject(string path) =>
		_projectExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     The plugin assembly in a build output folder: the one with a deps.json, which is what
	///     <c>dotnet build</c> writes for the project itself, else the folder's only assembly.
	/// </summary>
	private static (string? AssemblyPath, string? Error) FindAssemblyInFolder(string folder)
	{
		const string depsSuffix = ".deps.json";
		List<string> withDeps = Directory.GetFiles(folder, "*" + depsSuffix)
			.Select(deps => deps[..^depsSuffix.Length] + ".dll")
			.Where(File.Exists)
			.ToList();
		if (withDeps.Count == 1)
		{
			return (withDeps[0], null);
		}

		string[] assemblies = Directory.GetFiles(folder, "*.dll");
		if (withDeps.Count == 0 && assemblies.Length == 1)
		{
			return (assemblies[0], null);
		}

		return assemblies.Length == 0
			? (null, $"'{folder}' holds no project and no .dll")
			: (null, $"'{folder}' holds no project and several assemblies; point path at the plugin's .dll");
	}

	private static async Task<(string? AssemblyPath, string? Error)> BuildProjectAsync(string project,
		TimeSpan timeout, CancellationToken ct)
	{
		string name = Path.GetFileName(project);
		var clock = Stopwatch.StartNew();
		try
		{
			ChildProcessResult evaluation = await RunDotnetAsync(project,
				["msbuild", project, "-nologo", "-getProperty:TargetFramework", "-getProperty:TargetFrameworks"],
				timeout, ct);
			if (evaluation.TimedOut)
			{
				return (null, TimedOut(name, timeout));
			}

			if (evaluation.ExitCode != 0 || ReadJson(evaluation.Output) is not { } properties)
			{
				return (null, $"dotnet couldn't read {name}: {Summarize(evaluation)}");
			}

			string targetFramework = ReadString(properties, "Properties", "TargetFramework");
			string targetFrameworks = ReadString(properties, "Properties", "TargetFrameworks");
			int runtimeMajor = Environment.Version.Major;
			if (PickTargetFramework(targetFramework, targetFrameworks, runtimeMajor) is not { } tfm)
			{
				string targets = targetFramework.Length > 0 ? targetFramework : targetFrameworks;
				return (null,
					$"{name} targets {targets}, and this mokadocs runs on .NET {runtimeMajor}, which loads " +
					$"plugins built for net{runtimeMajor}.0 or earlier");
			}

			TimeSpan left = timeout - clock.Elapsed;
			ChildProcessResult build = await RunDotnetAsync(project,
				["build", project, "-c", "Release", "-f", tfm, "-nologo", "--disable-build-servers", "-getTargetResult:Build"],
				left > TimeSpan.Zero ? left : TimeSpan.FromMilliseconds(1), ct);
			if (build.TimedOut)
			{
				return (null, TimedOut(name, timeout));
			}

			if (build.ExitCode != 0)
			{
				return (null, $"dotnet build failed for {name}: {Summarize(build)}");
			}

			string? targetPath = ReadJson(build.Output) is { } result ? ReadTargetPath(result) : null;
			return targetPath is null
				? (null, $"dotnet build of {name} didn't report the assembly it built")
				: (targetPath, null);
		}
		catch (Win32Exception ex)
		{
			return (null, $"couldn't run dotnet to build {name}: {ex.Message}");
		}
	}

	private static string TimedOut(string project, TimeSpan timeout) =>
		$"building {project} took longer than {timeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture)} seconds and was stopped";

	private static Task<ChildProcessResult> RunDotnetAsync(string project, string[] arguments, TimeSpan timeout,
		CancellationToken ct)
	{
		string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host && File.Exists(host)
			? host
			: "dotnet";
		var startInfo = new ProcessStartInfo(dotnet) { WorkingDirectory = Path.GetDirectoryName(project)! };
		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		startInfo.Environment["DOTNET_NOLOGO"] = "1";
		return ChildProcessRunner.RunAsync(startInfo, timeout, ct);
	}

	/// <summary>The compiler and MSBuild errors in a failed run, or its last lines when it printed none.</summary>
	private static string Summarize(ChildProcessResult result)
	{
		string[] lines = (result.Output + "\n" + result.Error)
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string[] errors = lines
			.Where(line => line.Contains(": error ", StringComparison.OrdinalIgnoreCase))
			.Distinct(StringComparer.Ordinal)
			.Take(3)
			.ToArray();
		string[] shown = errors.Length > 0 ? errors : lines.Where(line => !line.StartsWith('{') && !line.StartsWith('}')).TakeLast(3).ToArray();
		return shown.Length > 0 ? string.Join(" | ", shown) : $"exit code {result.ExitCode}";
	}

	/// <summary>The JSON document MSBuild prints for <c>-getProperty</c> and <c>-getTargetResult</c>.</summary>
	private static JsonElement? ReadJson(string output)
	{
		// A first-run notice can come before the document.
		int start = output.IndexOf('{');
		if (start < 0)
		{
			return null;
		}

		try
		{
			using JsonDocument document = JsonDocument.Parse(output[start..]);
			return document.RootElement.Clone();
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static string ReadString(JsonElement element, params string[] names)
	{
		foreach (string name in names)
		{
			if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
			{
				return "";
			}
		}

		return element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
	}

	private static string? ReadTargetPath(JsonElement result)
	{
		if (!result.TryGetProperty("TargetResults", out JsonElement targets)
		    || !targets.TryGetProperty("Build", out JsonElement build)
		    || !build.TryGetProperty("Items", out JsonElement items)
		    || items.ValueKind != JsonValueKind.Array)
		{
			return null;
		}

		return items.EnumerateArray()
			.Select(item => ReadString(item, "FullPath") is { Length: > 0 } full ? full : ReadString(item, "Identity"))
			.FirstOrDefault(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path));
	}

	/// <summary>
	///     Refuses a plugin built against MokaDocs libraries of another major version, or of a
	///     newer version than the host's.
	/// </summary>
	private static string? CheckMokaDocsVersion(Assembly plugin)
	{
		foreach (AssemblyName reference in plugin.GetReferencedAssemblies())
		{
			if (!HostAssemblies.IsMokaDocs(reference) || reference.Version is not { } built
			                                         || HostAssemblies.VersionOf(reference.Name!) is not { } host)
			{
				continue;
			}

			if (built.Major != host.Major)
			{
				return $"it is built against {reference.Name} {Format(built)}, and this mokadocs is {Format(host)}. " +
				       $"Rebuild it against Moka.Docs.Plugins {host.Major}.x";
			}

			if (built > host)
			{
				return $"it needs {reference.Name} {Format(built)} or later, and this mokadocs is {Format(host)}. " +
				       "Update mokadocs";
			}
		}

		return null;
	}

	private static string Format(Version version) => version.Build >= 0 ? version.ToString(3) : version.ToString();

	private static PluginAssemblyResult FindPluginTypes(Assembly assembly, string path)
	{
		Type[] types;
		string? typeLoadError = null;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = ex.Types.OfType<Type>().ToArray();
			typeLoadError = ex.LoaderExceptions.FirstOrDefault(e => e is not null)?.Message;
		}

		List<Type> plugins = types
			.Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IMokaPlugin).IsAssignableFrom(type))
			.ToList();
		if (plugins.Count > 0)
		{
			return new PluginAssemblyResult(plugins, null);
		}

		if (types.FirstOrDefault(ImplementsAnotherMokaPlugin) is { } foreign)
		{
			Type contract = foreign.GetInterfaces().First(IsForeignMokaPlugin);
			return PluginAssemblyResult.Failed(
				$"{foreign.FullName} implements the IMokaPlugin of '{contract.Assembly.GetName().Name}', not the one in " +
				"Moka.Docs.Plugins that mokadocs runs, so it can't be used. Reference the Moka.Docs.Plugins package");
		}

		return PluginAssemblyResult.Failed(typeLoadError is null
			? $"'{path}' has no class that implements IMokaPlugin"
			: $"couldn't read the classes in '{path}': {typeLoadError}");
	}

	private static bool ImplementsAnotherMokaPlugin(Type type)
	{
		try
		{
			return type.GetInterfaces().Any(IsForeignMokaPlugin);
		}
		catch (Exception ex) when (ex is TypeLoadException or FileNotFoundException or FileLoadException)
		{
			return false;
		}
	}

	private static bool IsForeignMokaPlugin(Type contract) =>
		contract != typeof(IMokaPlugin)
		&& string.Equals(contract.FullName, typeof(IMokaPlugin).FullName, StringComparison.Ordinal);

	private sealed record LoadedFile((DateTime Written, long Length) Stamp, Assembly Assembly);
}

/// <summary>The plugin classes found in a plugin assembly, or why there are none.</summary>
/// <param name="PluginTypes">The classes that implement <see cref="IMokaPlugin" />.</param>
/// <param name="Error">Why loading failed, or <c>null</c>.</param>
internal sealed record PluginAssemblyResult(IReadOnlyList<Type> PluginTypes, string? Error)
{
	/// <summary>A result with no plugins and the reason.</summary>
	internal static PluginAssemblyResult Failed(string error) => new([], error);
}
