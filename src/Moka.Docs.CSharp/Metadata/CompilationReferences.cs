using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Resolves the metadata references a project is compiled against: the shared frameworks of
///     the running .NET runtime plus the project's package and project reference assemblies.
/// </summary>
/// <remarks>
///     Loaded references are kept for the life of the process.
///     <see cref="MetadataReference.CreateFromFile(string, MetadataReferenceProperties, DocumentationProvider)" />
///     reads the whole file into memory, and the shared framework alone is well over a hundred
///     assemblies. Without the cache, every project in a build, and every rebuild in watch mode,
///     would read all of them again.
/// </remarks>
internal static class CompilationReferences
{
	/// <summary>The shared framework every .NET project references.</summary>
	public const string CoreFramework = "Microsoft.NETCore.App";

	private static readonly ConcurrentDictionary<string, AssemblyFile> _assemblies =
		new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	///     Builds the reference list for a compilation.
	/// </summary>
	/// <param name="sharedFrameworks">Shared framework names, such as <c>Microsoft.AspNetCore.App</c>.</param>
	/// <param name="referencePaths">Package and project reference assemblies.</param>
	public static IReadOnlyList<MetadataReference> Resolve(
		IEnumerable<string> sharedFrameworks,
		IEnumerable<string> referencePaths)
	{
		IEnumerable<string> files = SharedFrameworkDirectories(sharedFrameworks)
			.SelectMany(directory => Directory.EnumerateFiles(directory, "*.dll"))
			.Concat(referencePaths);

		// Packages can carry their own copy of an assembly the framework also has (System.Text.Json,
		// System.Memory). The SDK keeps the higher version; two copies in one compilation make
		// every type in them ambiguous.
		var byName = new Dictionary<string, AssemblyFile>(StringComparer.OrdinalIgnoreCase);
		foreach (string file in files)
		{
			if (Load(file) is not { } assembly)
			{
				continue;
			}

			if (!byName.TryGetValue(assembly.Name, out AssemblyFile? existing) || assembly.Version > existing.Version)
			{
				byName[assembly.Name] = assembly;
			}
		}

		return byName.Values.Select(assembly => (MetadataReference)assembly.Reference).ToList();
	}

	private static IEnumerable<string> SharedFrameworkDirectories(IEnumerable<string> names)
	{
		// For example C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12. Empty when the
		// host is single-file, in which case there is no framework directory to reference.
		string? runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
		if (string.IsNullOrEmpty(runtimeDirectory))
		{
			yield break;
		}

		yield return runtimeDirectory;

		string runtimeVersion = Path.GetFileName(runtimeDirectory);
		string? sharedRoot = Path.GetDirectoryName(Path.GetDirectoryName(runtimeDirectory));
		if (sharedRoot is null)
		{
			yield break;
		}

		foreach (string name in names.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (name.Equals(CoreFramework, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string frameworkRoot = Path.Combine(sharedRoot, name);
			if (Directory.Exists(frameworkRoot) && PickVersionDirectory(frameworkRoot, runtimeVersion) is { } directory)
			{
				yield return directory;
			}
		}
	}

	private static string? PickVersionDirectory(string frameworkRoot, string runtimeVersion)
	{
		string exact = Path.Combine(frameworkRoot, runtimeVersion);
		if (Directory.Exists(exact))
		{
			return exact;
		}

		// ASP.NET Core and the runtime can be patched separately, so the versions need not match.
		// Prefer the newest release of the same major version.
		Version? runtime = ParseVersion(runtimeVersion);
		var candidates = Directory.GetDirectories(frameworkRoot)
			.Select(folder => (Folder: folder, Version: ParseVersion(Path.GetFileName(folder))))
			.Where(candidate => candidate.Version is not null)
			.OrderByDescending(candidate => candidate.Version)
			.ToList();

		return candidates
			       .Where(candidate => candidate.Version!.Major == runtime?.Major)
			       .Select(candidate => candidate.Folder)
			       .FirstOrDefault()
		       ?? candidates.Select(candidate => candidate.Folder).FirstOrDefault();
	}

	private static Version? ParseVersion(string text)
	{
		int dash = text.IndexOf('-');
		return Version.TryParse(dash >= 0 ? text[..dash] : text, out Version? version) ? version : null;
	}

	private static AssemblyFile? Load(string path)
	{
		var file = new FileInfo(path);
		if (!file.Exists)
		{
			return null;
		}

		if (_assemblies.TryGetValue(file.FullName, out AssemblyFile? cached)
		    && cached.Length == file.Length
		    && cached.LastWriteTimeUtc == file.LastWriteTimeUtc)
		{
			return cached.IsManaged ? cached : null;
		}

		AssemblyFile assembly;
		try
		{
			AssemblyName name = AssemblyName.GetAssemblyName(file.FullName);
			assembly = new AssemblyFile(file, name.Name ?? Path.GetFileNameWithoutExtension(file.Name),
				name.Version ?? new Version(0, 0), true);
		}
		catch (BadImageFormatException)
		{
			// Native libraries such as coreclr.dll sit next to the managed ones in the shared framework.
			assembly = new AssemblyFile(file, "", new Version(0, 0), false);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}

		_assemblies[file.FullName] = assembly;
		return assembly.IsManaged ? assembly : null;
	}

	private sealed class AssemblyFile
	{
		private readonly Lazy<PortableExecutableReference> _reference;

		public AssemblyFile(FileInfo file, string name, Version version, bool isManaged)
		{
			string path = file.FullName;
			Name = name;
			Version = version;
			IsManaged = isManaged;
			Length = file.Length;
			LastWriteTimeUtc = file.LastWriteTimeUtc;

			// Deferred so an assembly that loses to a higher version is never read.
			_reference = new Lazy<PortableExecutableReference>(() => MetadataReference.CreateFromFile(path));
		}

		public string Name { get; }

		public Version Version { get; }

		public bool IsManaged { get; }

		public long Length { get; }

		public DateTime LastWriteTimeUtc { get; }

		public PortableExecutableReference Reference => _reference.Value;
	}
}
