namespace Moka.Docs.AspNetCore;

/// <summary>
///     The files of directories that plugins stage for the output, such as the Blazor preview
///     host's published app: hundreds of files and tens of megabytes, read and hashed once per
///     change to the directory instead of on every build.
/// </summary>
/// <remarks>
///     <para>
///         A directory is read again when its stamp changes: the number of files, their names,
///         total size and newest write time. Reading the metadata of a few hundred files costs
///         about a millisecond; reading the files cost every rebuild a large-object allocation per
///         file and a full hash of each.
///     </para>
///     <para>
///         <c>X.br</c> and <c>X.gz</c> next to <c>X</c> become <c>X</c>'s Brotli and gzip forms when
///         they decompress to <c>X</c>, and are not served as files of their own. Other files are
///         compressed here, once per stamp, when they're worth it.
///     </para>
/// </remarks>
internal sealed class StagedDirectoryCache
{
	private static readonly StringComparer _pathComparer =
		OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	private static readonly (string Coding, string Suffix)[] _siblings =
		[(SiteCompression.Brotli, ".br"), (SiteCompression.Gzip, ".gz")];

	private readonly Dictionary<string, Entry> _entries = new(_pathComparer);
	private readonly Lock _lock = new();
	private int _reads;

	/// <summary>How many times a directory was read from disk. Tests check that a rebuild reads none.</summary>
	internal int Reads => Volatile.Read(ref _reads);

	/// <summary>
	///     The files under <paramref name="sourceDirectory" />, keyed by their path relative to it
	///     with <c>/</c> separators.
	/// </summary>
	/// <param name="sourceDirectory">The directory on disk.</param>
	/// <param name="unreadable">Gets the files that couldn't be read, with the reason. They are left out.</param>
	/// <param name="ct">Stops the compression of a directory read for the first time.</param>
	internal IReadOnlyList<KeyValuePair<string, SiteFile>> Get(string sourceDirectory,
		List<(string Path, string Reason)> unreadable, CancellationToken ct)
	{
		string key = Path.GetFullPath(sourceDirectory);
		Stamp before = Stamp.Of(key);
		lock (_lock)
		{
			if (_entries.TryGetValue(key, out Entry? cached) && cached.Stamp == before)
			{
				return cached.Files;
			}
		}

		Interlocked.Increment(ref _reads);
		IReadOnlyList<KeyValuePair<string, SiteFile>> files = Read(key, unreadable, ct);

		// A publish that writes while the files are read leaves a mix of old and new, which must
		// not be kept: the next build reads the directory again.
		if (unreadable.Count == 0 && Stamp.Of(key) == before)
		{
			lock (_lock)
			{
				_entries[key] = new Entry(before, files);
			}
		}

		return files;
	}

	private static IReadOnlyList<KeyValuePair<string, SiteFile>> Read(string directory,
		List<(string Path, string Reason)> unreadable, CancellationToken ct)
	{
		var contents = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(directory, file.FullName).Replace('\\', '/');
			try
			{
				contents[relative] = File.ReadAllBytes(file.FullName);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				unreadable.Add((relative, ex.Message));
			}
		}

		string[] served = contents.Keys.Where(path => !IsSibling(path, contents)).ToArray();
		var files = new KeyValuePair<string, SiteFile>[served.Length];
		Parallel.For(0, served.Length, new ParallelOptions { CancellationToken = ct }, i =>
		{
			string path = served[i];
			byte[] content = contents[path];
			var file = new SiteFile(content, SiteFile.GetContentType(Path.GetExtension(path)));

			var encodings = new List<SiteFileEncoding>(2);
			foreach ((string coding, string suffix) in _siblings)
			{
				if (contents.TryGetValue(path + suffix, out byte[]? compressed)
				    && compressed.Length < content.Length
				    && SiteCompression.Decompresses(coding, compressed, content))
				{
					encodings.Add(new SiteFileEncoding(coding, compressed));
				}
			}

			if (encodings.Count > 0)
			{
				file = file.WithEncodings(encodings);
			}
			else if (SiteCompression.ShouldCompress(file))
			{
				file = file.WithEncodings(SiteCompression.Compress(content));
			}

			files[i] = new KeyValuePair<string, SiteFile>(path, file);
		});

		return files;
	}

	/// <summary>Whether a path is the <c>.br</c> or <c>.gz</c> form of another file in the directory.</summary>
	private static bool IsSibling(string path, Dictionary<string, byte[]> contents) =>
		(path.EndsWith(".br", StringComparison.Ordinal) || path.EndsWith(".gz", StringComparison.Ordinal))
		&& contents.ContainsKey(path[..^3]);

	private sealed record Entry(Stamp Stamp, IReadOnlyList<KeyValuePair<string, SiteFile>> Files);

	/// <summary>What changes when a directory's files do, read from metadata alone.</summary>
	private readonly record struct Stamp(int Count, long Length, long NewestWrite, long Names)
	{
		public static Stamp Of(string directory)
		{
			int count = 0;
			long length = 0;
			long newest = 0;
			long names = 0;
			foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories))
			{
				count++;
				length += file.Length;
				newest = Math.Max(newest, file.LastWriteTimeUtc.Ticks);

				// A sum doesn't depend on the order the file system lists the files in. A rename
				// keeps the size and write time, so the names have to count too.
				names += _pathComparer.GetHashCode(file.FullName);
			}

			return new Stamp(count, length, newest, names);
		}
	}
}
