using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     Works out the C# files a project compiles from its <c>Compile</c> items, the way MSBuild evaluates
///     them: the SDK's default <c>**/*.cs</c> glob, then each unconditional <c>Include</c> and <c>Remove</c>
///     in order.
/// </summary>
internal static class CompileItems
{
	private static readonly StringComparer _pathComparer =
		OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	private static readonly RegexOptions _regexOptions =
		RegexOptions.CultureInvariant | (OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase : RegexOptions.None);

	/// <summary>
	///     The <c>.cs</c> files below a folder that the SDK compiles by default, sorted. Folders named
	///     <c>bin</c> or <c>obj</c>, and folders whose name starts with a dot, are skipped wherever they are
	///     below it.
	/// </summary>
	/// <remarks>
	///     The check used to look for <c>/bin/</c> and <c>/obj/</c> anywhere in the full path, so a project
	///     that itself lived under a folder called <c>bin</c> had no source files at all. Dot folders such as
	///     <c>.mokadocs/preview-host</c> were analyzed, though the SDK leaves them out with <c>**/.*/**</c>.
	/// </remarks>
	public static List<string> DefaultSourceFiles(string directory)
	{
		var files = new List<string>();
		if (!Directory.Exists(directory))
		{
			return files;
		}

		// A folder link can point back up the tree, so each real folder is walked once.
		var visited = new HashSet<string>(_pathComparer);
		var pending = new Stack<string>();
		pending.Push(Path.GetFullPath(directory));
		while (pending.Count > 0)
		{
			string current = pending.Pop();
			if (!visited.Add(RealPath(current)))
			{
				continue;
			}

			files.AddRange(Directory.EnumerateFiles(current, "*.cs"));

			foreach (string child in Directory.EnumerateDirectories(current))
			{
				string name = Path.GetFileName(child);
				if (!name.StartsWith('.')
				    && !name.Equals("bin", StringComparison.OrdinalIgnoreCase)
				    && !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
				{
					pending.Push(child);
				}
			}
		}

		files.Sort(StringComparer.Ordinal);
		return files;
	}

	/// <summary>
	///     The files the project compiles, sorted.
	/// </summary>
	/// <param name="projectDirectory">The folder that holds the project file. Item paths are relative to it.</param>
	/// <param name="importedItems">Compile items from <c>Directory.Build.props</c>, which come before the SDK's.</param>
	/// <param name="projectItems">Compile items from the project file.</param>
	/// <param name="enableDefaultItems">Whether the SDK's default glob applies.</param>
	/// <param name="defaultItemExcludes">The project's <c>DefaultItemExcludes</c>, applied to the default glob.</param>
	public static List<string> Resolve(
		string projectDirectory,
		IEnumerable<CompileItem> importedItems,
		IEnumerable<CompileItem> projectItems,
		bool enableDefaultItems,
		string? defaultItemExcludes)
	{
		var files = new HashSet<string>(_pathComparer);
		Apply(projectDirectory, importedItems, files);

		if (enableDefaultItems)
		{
			List<Regex> excludes = Patterns(projectDirectory, defaultItemExcludes);
			files.UnionWith(DefaultSourceFiles(projectDirectory).Where(file => !Matches(excludes, file)));
		}

		Apply(projectDirectory, projectItems, files);

		List<string> sorted = files.ToList();
		sorted.Sort(StringComparer.Ordinal);
		return sorted;
	}

	private static void Apply(string projectDirectory, IEnumerable<CompileItem> items, HashSet<string> files)
	{
		foreach (CompileItem item in items)
		{
			if (item.Remove is not null)
			{
				List<Regex> removed = Patterns(projectDirectory, item.Remove);
				files.RemoveWhere(file => Matches(removed, file));
			}

			if (item.Include is not null)
			{
				List<Regex> excluded = Patterns(projectDirectory, item.Exclude);
				files.UnionWith(Split(item.Include)
					.SelectMany(pattern => Expand(projectDirectory, pattern))
					.Where(file => !Matches(excluded, file)));
			}
		}
	}

	/// <summary>
	///     The existing <c>.cs</c> files an <c>Include</c> pattern names. A plain path names one file, which
	///     can sit outside the project folder: a linked file.
	/// </summary>
	private static IEnumerable<string> Expand(string projectDirectory, string pattern)
	{
		string normalized = Normalize(pattern);
		int wildcard = normalized.IndexOfAny(['*', '?']);
		if (wildcard < 0)
		{
			string file = Path.GetFullPath(Path.Combine(projectDirectory, normalized));
			return File.Exists(file) && file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [file] : [];
		}

		int lastSlash = normalized.LastIndexOf('/', wildcard);
		string baseDirectory = Path.GetFullPath(Path.Combine(projectDirectory, lastSlash < 0 ? "." : normalized[..lastSlash]));
		if (!Directory.Exists(baseDirectory))
		{
			return [];
		}

		Regex regex = ToRegex(projectDirectory, pattern);
		return Directory.EnumerateFiles(baseDirectory, "*.cs", SearchOption.AllDirectories)
			.Select(Path.GetFullPath)
			.Where(file => regex.IsMatch(Normalize(file)));
	}

	private static List<Regex> Patterns(string projectDirectory, string? value) =>
		Split(value).Select(pattern => ToRegex(projectDirectory, pattern)).ToList();

	private static bool Matches(List<Regex> patterns, string file)
	{
		if (patterns.Count == 0)
		{
			return false;
		}

		string normalized = Normalize(file);
		return patterns.Any(pattern => pattern.IsMatch(normalized));
	}

	/// <summary>
	///     Turns an MSBuild path pattern, relative to the project folder, into a regex over full paths
	///     written with forward slashes: <c>**</c> matches any number of folders, <c>*</c> and <c>?</c> stay
	///     within one.
	/// </summary>
	private static Regex ToRegex(string projectDirectory, string pattern)
	{
		string normalized = Normalize(pattern);
		int wildcard = normalized.IndexOfAny(['*', '?']);
		int lastSlash = wildcard < 0 ? normalized.Length : normalized.LastIndexOf('/', wildcard);
		string fixedPart = lastSlash < 0 ? "." : normalized[..lastSlash];
		string rest = lastSlash < 0 ? normalized : lastSlash < normalized.Length ? normalized[(lastSlash + 1)..] : "";

		var sb = new StringBuilder("^");
		sb.Append(Regex.Escape(Normalize(Path.GetFullPath(Path.Combine(projectDirectory, fixedPart))).TrimEnd('/')));
		if (rest.Length > 0)
		{
			sb.Append('/');
		}

		for (int i = 0; i < rest.Length; i++)
		{
			char c = rest[i];
			if (c == '*' && i + 1 < rest.Length && rest[i + 1] == '*')
			{
				bool folderStep = i + 2 < rest.Length && rest[i + 2] == '/';
				sb.Append(folderStep ? "(?:.*/)?" : ".*");
				i += folderStep ? 2 : 1;
			}
			else
			{
				sb.Append(c switch
				{
					'*' => "[^/]*",
					'?' => "[^/]",
					_ => Regex.Escape(c.ToString())
				});
			}
		}

		sb.Append('$');
		return new Regex(sb.ToString(), _regexOptions);
	}

	private static string Normalize(string path) => path.Replace('\\', '/');

	private static IEnumerable<string> Split(string? value) =>
		value?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

	private static string RealPath(string directory)
	{
		try
		{
			return new DirectoryInfo(directory).ResolveLinkTarget(true)?.FullName ?? directory;
		}
		catch (IOException)
		{
			return directory;
		}
		catch (UnauthorizedAccessException)
		{
			return directory;
		}
	}
}

/// <summary>
///     An unconditional <c>Compile</c> item: its <c>Include</c>, <c>Exclude</c> and <c>Remove</c>, with
///     properties already filled in.
/// </summary>
internal sealed record CompileItem(string? Include, string? Exclude, string? Remove);
