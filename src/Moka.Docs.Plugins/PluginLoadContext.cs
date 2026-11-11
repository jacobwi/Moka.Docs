using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace Moka.Docs.Plugins;

/// <summary>
///     The load context of one plugin assembly. The plugin's own dependencies come from its
///     folder, as its deps.json lists them; the MokaDocs libraries and the framework come from
///     the host, so <see cref="IMokaPlugin" /> and the build's types are the host's own.
/// </summary>
/// <remarks>
///     <para>
///         A dependency the host also has is shared when the host's copy can stand in for the
///         plugin's: the same major version and at least the version the plugin asks for. Types
///         that cross between plugin and host, such as the build's <c>IFileSystem</c>, then stay
///         the same type. Any other version is loaded from the plugin's folder, next to the
///         host's.
///     </para>
///     <para>
///         Assemblies are read into memory instead of mapped from disk. A mapped file stays
///         locked on Windows, so rebuilding a plugin project while <c>serve</c> runs failed to
///         overwrite it. <see cref="Assembly.Location" /> is therefore empty for plugin
///         assemblies.
///     </para>
/// </remarks>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
	private readonly string _directory;
	private readonly AssemblyDependencyResolver? _resolver;

	/// <summary>Creates the context for the plugin assembly at <paramref name="mainAssemblyPath" />.</summary>
	internal PluginLoadContext(string mainAssemblyPath)
		: base("mokadocs plugin " + Path.GetFileNameWithoutExtension(mainAssemblyPath))
	{
		_directory = Path.GetDirectoryName(mainAssemblyPath)!;
		try
		{
			_resolver = new AssemblyDependencyResolver(mainAssemblyPath);
		}
		catch (InvalidOperationException)
		{
			// No hostpolicy to read the deps.json with, as in a single-file host. The plugin's
			// folder is probed by file name instead.
			_resolver = null;
		}
	}

	/// <summary>Loads an assembly file into this context from memory, with its symbols when they sit beside it.</summary>
	internal Assembly LoadFromFile(string path)
	{
		using var assembly = new MemoryStream(File.ReadAllBytes(path));
		string symbolsPath = Path.ChangeExtension(path, ".pdb");
		using MemoryStream? symbols = File.Exists(symbolsPath) ? new MemoryStream(File.ReadAllBytes(symbolsPath)) : null;
		return LoadFromStream(assembly, symbols);
	}

	/// <inheritdoc />
	protected override Assembly? Load(AssemblyName assemblyName)
	{
		if (HostAssemblies.IsMokaDocs(assemblyName))
		{
			return null;
		}

		string? path = Resolve(assemblyName);
		return path is null || HostAssemblies.CanShare(assemblyName) ? null : LoadFromFile(path);
	}

	/// <inheritdoc />
	protected override nint LoadUnmanagedDll(string unmanagedDllName) =>
		_resolver?.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path ? LoadUnmanagedDllFromPath(path) : 0;

	private string? Resolve(AssemblyName assemblyName)
	{
		if (_resolver is not null)
		{
			return _resolver.ResolveAssemblyToPath(assemblyName);
		}

		string candidate = Path.Combine(_directory, assemblyName.Name + ".dll");
		return File.Exists(candidate) ? candidate : null;
	}
}

/// <summary>
///     What the host process has on its trusted platform assembly list: the framework and the
///     host application's own dependencies.
/// </summary>
internal static class HostAssemblies
{
	private const string _mokaDocsPrefix = "Moka.Docs.";

	private static readonly Lazy<Dictionary<string, string>> _trusted = new(ReadTrustedPlatformAssemblies);
	private static readonly ConcurrentDictionary<string, Version?> _versions = new(StringComparer.OrdinalIgnoreCase);
	private static readonly string? _sharedFrameworkRoot = FindSharedFrameworkRoot();

	/// <summary>Whether the name is one of the MokaDocs libraries the host runs on, which plugins always share.</summary>
	internal static bool IsMokaDocs(AssemblyName name) =>
		name.Name is { } simple
		&& simple.StartsWith(_mokaDocsPrefix, StringComparison.OrdinalIgnoreCase)
		&& _trusted.Value.ContainsKey(simple);

	/// <summary>
	///     Whether the host's copy of the assembly can stand in for the version the plugin asks for:
	///     a framework assembly of at least that version, or another dependency of the same major
	///     version and at least that version.
	/// </summary>
	internal static bool CanShare(AssemblyName requested)
	{
		if (requested.Name is not { } simple || !_trusted.Value.TryGetValue(simple, out string? path))
		{
			return false;
		}

		Version? host = VersionOf(simple);
		if (host is null || requested.Version is not { } wanted)
		{
			return true;
		}

		return host >= wanted && (IsFramework(path) || host.Major == wanted.Major);
	}

	/// <summary>The version of the host's assembly with this simple name, or <c>null</c> when the host has none.</summary>
	internal static Version? VersionOf(string simpleName) =>
		_versions.GetOrAdd(simpleName, static name =>
		{
			if (!_trusted.Value.TryGetValue(name, out string? path))
			{
				return null;
			}

			try
			{
				return AssemblyName.GetAssemblyName(path).Version;
			}
			catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
			{
				return null;
			}
		});

	private static bool IsFramework(string path) =>
		_sharedFrameworkRoot is not null
		&& path.StartsWith(_sharedFrameworkRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

	private static Dictionary<string, string> ReadTrustedPlatformAssemblies()
	{
		var trusted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string list = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
		foreach (string path in list.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			trusted.TryAdd(Path.GetFileNameWithoutExtension(path), path);
		}

		return trusted;
	}

	/// <summary>
	///     The <c>shared</c> folder of the dotnet installation the host runs on: the core library sits
	///     in <c>shared/Microsoft.NETCore.App/{version}</c>.
	/// </summary>
	private static string? FindSharedFrameworkRoot()
	{
		string coreLibrary = typeof(object).Assembly.Location;
		if (coreLibrary.Length == 0)
		{
			return null;
		}

		string? shared = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(coreLibrary)));
		return shared is null ? null : shared + Path.DirectorySeparatorChar;
	}
}
