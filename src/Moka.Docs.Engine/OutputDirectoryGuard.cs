namespace Moka.Docs.Engine;

/// <summary>
///     Rejects output directories whose deletion would destroy the project itself.
/// </summary>
/// <remarks>
///     A build deletes the output directory before writing (<c>build.clean</c>, on by
///     default) and <c>mokadocs clean</c> deletes it outright. A typo such as
///     <c>output: ./docs</c> or <c>output: .</c> would otherwise wipe the Markdown sources or
///     the whole project without asking.
/// </remarks>
public static class OutputDirectoryGuard
{
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
			return $"Refusing to use '{outputDirectory}' as the output directory: it contains the project " +
			       $"('{rootDirectory}'), and the build deletes the output directory. Set build.output to a " +
			       "dedicated folder such as ./_site.";
		}

		if (IsSameOrInside(Normalize(docsDirectory), output))
		{
			return $"Refusing to use '{outputDirectory}' as the output directory: it contains the docs folder " +
			       $"('{docsDirectory}'), and the build deletes the output directory. Set build.output to a " +
			       "dedicated folder such as ./_site.";
		}

		return null;
	}

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
