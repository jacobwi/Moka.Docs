using System.IO.Abstractions;

namespace Moka.Docs.Engine;

/// <summary>
///     Maps page routes to output files and checks that a route can be written, and stays
///     inside the output directory, before anything is deleted or written.
/// </summary>
public static class OutputPaths
{
	private const string _windowsInvalidChars = "<>:\"|*\\";

	/// <summary>
	///     Explains why <paramref name="route" /> can't be served from a static host, or returns
	///     <c>null</c>. Covers <c>.</c> and <c>..</c> segments, query strings, fragments and
	///     control characters.
	/// </summary>
	/// <param name="route">A root-relative page route such as <c>/guide/intro</c>.</param>
	/// <returns>The reason, or <c>null</c> when the route is fine.</returns>
	public static string? FindUnsafeRouteProblem(string route)
	{
		if (route.Contains('?'))
		{
			return "it contains a query string ('?'), which no URL can reach";
		}

		if (route.Contains('#'))
		{
			return "it contains a fragment ('#'), which no URL can reach";
		}

		if (route.Any(char.IsControl))
		{
			return "it contains a control character";
		}

		foreach (string segment in route.Split('/'))
		{
			if (segment is "." or "..")
			{
				return $"it contains a '{segment}' segment";
			}
		}

		return null;
	}

	/// <summary>
	///     Explains why <paramref name="route" /> can't be written as a folder name on Windows,
	///     or returns <c>null</c>. Covers the characters Windows forbids in file names and
	///     segments ending in a dot or space, which Windows silently drops.
	/// </summary>
	/// <param name="route">A root-relative page route.</param>
	/// <returns>The reason, or <c>null</c> when the route is portable.</returns>
	public static string? FindNonPortableRouteProblem(string route)
	{
		int invalid = route.IndexOfAny(_windowsInvalidChars.ToCharArray());
		if (invalid >= 0)
		{
			return $"it contains '{route[invalid]}', which is not allowed in file names on Windows";
		}

		foreach (string segment in route.Split('/'))
		{
			if (segment.Length > 0 && (segment.EndsWith('.') || segment.EndsWith(' ')))
			{
				return $"the segment '{segment}' ends with a dot or space, which Windows drops from file names";
			}
		}

		return null;
	}

	/// <summary>
	///     Whether the output phase writes a page with this route: it is safe, Windows can write
	///     it when the build runs on Windows, and it stays inside the output directory.
	/// </summary>
	/// <param name="fileSystem">The build's file system.</param>
	/// <param name="outputDirectory">The output directory.</param>
	/// <param name="route">The page route.</param>
	/// <returns><c>true</c> when the page is written.</returns>
	public static bool IsWritable(IFileSystem fileSystem, string outputDirectory, string route) =>
		FindUnsafeRouteProblem(route) is null
		&& (!OperatingSystem.IsWindows() || FindNonPortableRouteProblem(route) is null)
		&& ResolveUnder(fileSystem, outputDirectory, RouteToFilePath(route)) is not null;

	/// <summary>
	///     The output-relative file a page route is written to: <c>/guide/intro</c> becomes
	///     <c>guide/intro/index.html</c> and <c>/</c> becomes <c>index.html</c>.
	/// </summary>
	/// <param name="route">The page route.</param>
	/// <returns>The relative file path, with forward slashes.</returns>
	public static string RouteToFilePath(string route)
	{
		string path = route.Trim('/');
		return string.IsNullOrEmpty(path) ? "index.html" : path + "/index.html";
	}

	/// <summary>
	///     Resolves <paramref name="relativePath" /> against the output directory, or returns
	///     <c>null</c> when the result would land outside it (a <c>..</c> segment, a rooted path,
	///     or a path the file system rejects).
	/// </summary>
	/// <param name="fileSystem">The build's file system.</param>
	/// <param name="outputDirectory">The output directory.</param>
	/// <param name="relativePath">A path relative to the output directory.</param>
	/// <returns>The normalized full path, or <c>null</c>.</returns>
	public static string? ResolveUnder(IFileSystem fileSystem, string outputDirectory, string relativePath)
	{
		try
		{
			string root = NormalizeDirectory(fileSystem, outputDirectory);
			string full = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(root, relativePath));
			return IsSameOrUnder(full, root) ? full : null;
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return null;
		}
	}

	/// <summary>
	///     Whether <paramref name="path" /> is <paramref name="directory" /> or inside it. Both
	///     must already be full paths. The comparison is ordinal: a path that only matches the
	///     directory when case is ignored is not inside it on a case-sensitive file system, and
	///     refusing a write is the cheap side to be wrong on.
	/// </summary>
	/// <param name="path">A full path.</param>
	/// <param name="directory">A full directory path.</param>
	/// <returns><c>true</c> when the path is the directory or below it.</returns>
	public static bool IsSameOrUnder(string path, string directory)
	{
		string dir = Path.TrimEndingDirectorySeparator(directory);
		string candidate = Path.TrimEndingDirectorySeparator(path);
		if (candidate.Equals(dir, StringComparison.Ordinal))
		{
			return true;
		}

		if (dir.Length > 0 && IsSeparator(dir[^1]))
		{
			return candidate.StartsWith(dir, StringComparison.Ordinal);
		}

		return candidate.Length > dir.Length
		       && candidate.StartsWith(dir, StringComparison.Ordinal)
		       && IsSeparator(candidate[dir.Length]);
	}

	internal static string NormalizeDirectory(IFileSystem fileSystem, string directory) =>
		fileSystem.Path.TrimEndingDirectorySeparator(fileSystem.Path.GetFullPath(directory));

	private static bool IsSeparator(char c) => c == '/' || c == Path.DirectorySeparatorChar;
}
