using System.IO.Abstractions;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Engine;

/// <summary>
///     Rejects output directories whose deletion would destroy the project itself.
/// </summary>
/// <remarks>
///     A build deletes the output directory before writing (<c>build.clean</c>, on by
///     default) and <c>mokadocs clean</c> deletes it outright. A typo such as
///     <c>output: ./docs</c>, <c>output: .</c> or <c>output: ./src</c> would otherwise wipe
///     the Markdown sources, a documented project or the whole repository without asking.
/// </remarks>
public static class OutputDirectoryGuard
{
	/// <summary>
	///     The file every build writes into the output directory. A non-empty directory
	///     without it (or the theme folder older versions wrote) was not made by MokaDocs, and
	///     neither the build's clean step nor <c>mokadocs clean</c> will delete it.
	/// </summary>
	public const string MarkerFileName = ".mokadocs-output";

	/// <summary>
	///     Explains why <paramref name="outputDirectory" /> must not be used, or returns
	///     <c>null</c> when it is safe.
	/// </summary>
	/// <param name="rootDirectory">The directory containing mokadocs.yaml.</param>
	/// <param name="docsDirectory">The resolved <c>content.docs</c> directory.</param>
	/// <param name="outputDirectory">The resolved <c>build.output</c> directory.</param>
	/// <returns>A message describing the conflict, or <c>null</c>.</returns>
	public static string? FindConflict(string rootDirectory, string docsDirectory, string outputDirectory)
	{
		string output = Normalize(outputDirectory);

		if (IsSameOrInside(Normalize(rootDirectory), output))
		{
			return Refusal(outputDirectory, $"the project ('{rootDirectory}')");
		}

		if (IsSameOrInside(Normalize(docsDirectory), output))
		{
			return Refusal(outputDirectory, $"the docs folder ('{docsDirectory}')");
		}

		return null;
	}

	/// <summary>
	///     Explains why <paramref name="outputDirectory" /> must not be used for this
	///     configuration, or returns <c>null</c>. On top of the project and docs folders, the
	///     output must not be or contain a <c>content.projects</c> folder, a custom theme folder
	///     or the source file of <c>site.logo</c>, <c>site.favicon</c> or <c>site.image</c>.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	/// <param name="rootDirectory">The full path of the directory containing mokadocs.yaml.</param>
	/// <param name="outputDirectory">The full path of the output directory.</param>
	/// <returns>A message describing the conflict, or <c>null</c>.</returns>
	public static string? FindConflict(SiteConfig config, string rootDirectory, string outputDirectory)
	{
		string docsDirectory = Resolve(rootDirectory, config.Content.Docs);
		if (FindConflict(rootDirectory, docsDirectory, outputDirectory) is { } basic)
		{
			return basic;
		}

		string output = Normalize(outputDirectory);
		foreach ((string description, string path) in ProtectedPaths(config, rootDirectory))
		{
			if (IsSameOrInside(Normalize(path), output))
			{
				return Refusal(outputDirectory, description);
			}
		}

		return null;
	}

	/// <summary>
	///     Explains why the existing <paramref name="outputDirectory" /> must not be deleted, or
	///     returns <c>null</c> when it is missing, empty, or was written by MokaDocs (it holds
	///     <see cref="MarkerFileName" />, or the <c>_theme/css/main.css</c> that builds before the
	///     marker wrote).
	/// </summary>
	/// <param name="fileSystem">The file system the directory lives on.</param>
	/// <param name="outputDirectory">The output directory.</param>
	/// <returns>A message describing the problem, or <c>null</c>.</returns>
	public static string? FindForeignContent(IFileSystem fileSystem, string outputDirectory)
	{
		if (!fileSystem.Directory.Exists(outputDirectory)
		    || !fileSystem.Directory.EnumerateFileSystemEntries(outputDirectory).Any())
		{
			return null;
		}

		if (fileSystem.File.Exists(fileSystem.Path.Combine(outputDirectory, MarkerFileName))
		    || fileSystem.File.Exists(fileSystem.Path.Combine(outputDirectory, "_theme", "css", "main.css")))
		{
			return null;
		}

		return $"Refusing to delete '{outputDirectory}': it is not empty and has no {MarkerFileName} file, so " +
		       "MokaDocs did not create it. Point build.output at a new folder, or empty this one yourself.";
	}

	private static IEnumerable<(string Description, string Path)> ProtectedPaths(SiteConfig config, string root)
	{
		for (int i = 0; i < config.Content.Projects.Count; i++)
		{
			string projectPath = config.Content.Projects[i].Path;
			if (string.IsNullOrWhiteSpace(projectPath))
			{
				continue;
			}

			string full = Resolve(root, projectPath);
			string folder = Path.HasExtension(full) ? Path.GetDirectoryName(full) ?? full : full;
			yield return ($"the folder of content.projects[{i}] ('{folder}')", folder);
		}

		string theme = config.Theme.Name;
		if (!string.IsNullOrWhiteSpace(theme)
		    && !string.Equals(theme, MokaDefaults.ThemeName, StringComparison.OrdinalIgnoreCase))
		{
			string folder = Resolve(root, theme);
			yield return ($"the theme folder ('{folder}')", folder);
		}

		foreach ((string key, SiteAssetReference? asset) in new[]
		         {
			         ("site.logo", config.Site.Logo), ("site.favicon", config.Site.Favicon), ("site.image", config.Site.Image)
		         })
		{
			if (asset?.SourcePath is { } source)
			{
				yield return ($"the {key} source file ('{source}')", source);
			}
		}
	}

	private static string Refusal(string outputDirectory, string what) =>
		$"Refusing to use '{outputDirectory}' as the output directory: it contains {what}, and the build " +
		"deletes the output directory. Set build.output to a dedicated folder such as ./_site.";

	private static string Resolve(string root, string path) => Path.GetFullPath(Path.Combine(root, path));

	private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

	// Case-insensitive on every OS. On a case-sensitive file system this can refuse a folder
	// that differs from the project only by case, which is the cheap side to be wrong on.
	private static bool IsSameOrInside(string path, string directory)
	{
		if (path.Equals(directory, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		string prefix = Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;
		return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
	}
}
