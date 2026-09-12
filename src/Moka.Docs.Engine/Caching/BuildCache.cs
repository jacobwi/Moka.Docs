using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;
using Moka.Docs.CSharp.Metadata;

namespace Moka.Docs.Engine.Caching;

/// <summary>
///     Caches the result of Roslyn C# analysis between builds.
/// </summary>
/// <remarks>
///     Analysing source with Roslyn is around three quarters of a typical build, so this
///     is the only phase worth caching. Entries live in <c>.mokadocs/cache/</c> next to
///     mokadocs.yaml, keyed by project path, and are invalidated by a fingerprint of every
///     source file the analyzer reads.
/// </remarks>
public sealed class BuildCache(ILogger<BuildCache> logger)
{
	private const string _cacheDirName = ".mokadocs";

	/// <summary>
	///     Identifies the code that produced a cached model: the API model types, the Roslyn
	///     analyzer, and this cache. Taken from each assembly's module version id, which a
	///     deterministic build derives from the assembly's contents.
	/// </summary>
	/// <remarks>
	///     The source fingerprint alone says whether the user's files changed, not whether
	///     MokaDocs did. Without this, upgrading MokaDocs kept serving models built by the old
	///     analyzer until the user happened to edit a file, so analyzer fixes silently did not
	///     apply to anyone with a warm cache.
	/// </remarks>
	private static readonly string _toolIdentity = string.Join("|",
		typeof(ApiReference).Assembly.ManifestModule.ModuleVersionId,
		typeof(AssemblyAnalyzer).Assembly.ManifestModule.ModuleVersionId,
		typeof(BuildCache).Assembly.ManifestModule.ModuleVersionId);

	private static readonly JsonSerializerOptions _json = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter() },
		WriteIndented = false
	};

	/// <summary>
	///     Returns the cached API model for a project, or <c>null</c> on a miss.
	/// </summary>
	/// <param name="rootDirectory">Directory containing mokadocs.yaml.</param>
	/// <param name="projectPath">Absolute path to the .csproj.</param>
	/// <param name="fingerprint">Fingerprint of the project's current sources.</param>
	/// <returns>The cached model, or <c>null</c>.</returns>
	public ApiReference? TryGet(string rootDirectory, string projectPath, string fingerprint)
	{
		string file = GetCacheFilePath(rootDirectory, projectPath);
		if (!File.Exists(file))
		{
			return null;
		}

		try
		{
			CacheEntry? entry = JsonSerializer.Deserialize<CacheEntry>(File.ReadAllText(file), _json);
			if (entry is null || entry.Fingerprint != fingerprint || entry.ToolIdentity != _toolIdentity)
			{
				return null;
			}

			logger.LogInformation("Cache hit for {Project}", Path.GetFileName(projectPath));
			return entry.Api;
		}
		catch (Exception ex)
		{
			// A corrupt or stale-format entry must never fail the build.
			logger.LogDebug(ex, "Ignoring unreadable cache entry {File}", file);
			return null;
		}
	}

	/// <summary>
	///     Stores the API model for a project.
	/// </summary>
	/// <param name="rootDirectory">Directory containing mokadocs.yaml.</param>
	/// <param name="projectPath">Absolute path to the .csproj.</param>
	/// <param name="fingerprint">Fingerprint of the project's current sources.</param>
	/// <param name="api">The model to store.</param>
	public void Set(string rootDirectory, string projectPath, string fingerprint, ApiReference api)
	{
		string file = GetCacheFilePath(rootDirectory, projectPath);

		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(file)!);
			var entry = new CacheEntry { Fingerprint = fingerprint, ToolIdentity = _toolIdentity, Api = api };
			File.WriteAllText(file, JsonSerializer.Serialize(entry, _json));
		}
		catch (Exception ex)
		{
			// Caching is an optimisation. A read-only or full disk should not break a build.
			logger.LogDebug(ex, "Could not write cache entry {File}", file);
		}
	}

	/// <summary>
	///     Deletes the whole cache directory.
	/// </summary>
	/// <param name="rootDirectory">Directory containing mokadocs.yaml.</param>
	/// <returns><c>true</c> if a cache directory existed and was removed.</returns>
	public bool Clear(string rootDirectory)
	{
		string dir = Path.Combine(rootDirectory, _cacheDirName);
		if (!Directory.Exists(dir))
		{
			return false;
		}

		try
		{
			Directory.Delete(dir, true);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogDebug(ex, "Could not delete cache directory {Dir}", dir);
			return false;
		}
	}

	/// <summary>
	///     Fingerprints every source file the analyzer will read, so any edit, addition or
	///     deletion changes the key.
	/// </summary>
	/// <param name="sourceDirectory">The project directory Roslyn walks.</param>
	/// <param name="includeInternals">Whether internal types are included, which changes the output.</param>
	/// <returns>A hex digest, or an empty string when the directory is missing.</returns>
	/// <remarks>
	///     Deliberately uses <see cref="Directory" /> rather than the build's
	///     <c>IFileSystem</c>: it has to observe exactly the same files as
	///     <c>AssemblyAnalyzer.AnalyzeDirectory</c>, which also reads real disk. Fingerprinting
	///     a different file set than the analyzer reads would hand back stale models.
	/// </remarks>
	public static string ComputeFingerprint(string sourceDirectory, bool includeInternals)
	{
		if (!Directory.Exists(sourceDirectory))
		{
			return "";
		}

		var sb = new StringBuilder();
		sb.Append("internals=").Append(includeInternals).Append('\n');

		var files = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal))
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
				StringComparison.Ordinal))
			.OrderBy(f => f, StringComparer.Ordinal)
			.ToList();

		foreach (string f in files)
		{
			var info = new FileInfo(f);
			sb.Append(f).Append('|')
				.Append(info.Length).Append('|')
				.Append(info.LastWriteTimeUtc.Ticks).Append('\n');
		}

		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
	}

	private static string GetCacheFilePath(string rootDirectory, string projectPath)
	{
		string key = Convert
			.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectPath.ToLowerInvariant())))[..16];
		return Path.Combine(rootDirectory, _cacheDirName, "cache", $"api-{key}.json");
	}

	private sealed class CacheEntry
	{
		public string Fingerprint { get; set; } = "";

		public string ToolIdentity { get; set; } = "";
		public ApiReference? Api { get; set; }
	}
}
