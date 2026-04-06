namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     Locates the published Blazor WebAssembly preview app files on disk.
///     Checks: plugin option override → NuGet global packages folder → null.
/// </summary>
public sealed class WasmAppAssetResolver
{
	/// <summary>
	///     Resolves the WASM app directory from available sources.
	/// </summary>
	/// <param name="wasmAppPathOption">Optional user-specified path from plugin options.</param>
	/// <param name="rootDir">The project root directory for resolving relative paths.</param>
	public WasmAppAssetResolver(string? wasmAppPathOption, string rootDir)
	{
		// 1. User-specified path (relative to project root or absolute)
		if (!string.IsNullOrWhiteSpace(wasmAppPathOption))
		{
			string resolved = Path.GetFullPath(Path.Combine(rootDir, wasmAppPathOption));
			if (Directory.Exists(resolved) && HasWasmApp(resolved))
			{
				WasmAppDirectory = resolved;
				return;
			}
		}

		// 2. NuGet global packages folder: look for Moka.Blazor.Repl.Wasm package
		string? nugetDir = FindInNuGetCache();
		if (nugetDir is not null)
		{
			WasmAppDirectory = nugetDir;
			return;
		}

		// 3. Not found
		WasmAppDirectory = null;
	}

	/// <summary>
	///     Absolute path to the directory containing the published WASM app
	///     (index.html + _framework/), or null if not found.
	/// </summary>
	public string? WasmAppDirectory { get; }

	/// <summary>Whether the WASM app was found and is usable.</summary>
	public bool IsAvailable => WasmAppDirectory is not null;

	/// <summary>
	///     Checks whether a directory looks like a published Blazor WASM app.
	/// </summary>
	private static bool HasWasmApp(string dir)
	{
		return File.Exists(Path.Combine(dir, "index.html"))
		       && Directory.Exists(Path.Combine(dir, "_framework"));
	}

	/// <summary>
	///     Searches the NuGet global packages folder for the Moka.Blazor.Repl.Wasm package
	///     and returns the path to its published wwwroot content.
	/// </summary>
	private static string? FindInNuGetCache()
	{
		// NuGet global packages folder is typically at ~/.nuget/packages
		string nugetRoot = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".nuget", "packages");

		if (!Directory.Exists(nugetRoot))
		{
			return null;
		}

		string packageDir = Path.Combine(nugetRoot, "moka.blazor.repl.wasm");
		if (!Directory.Exists(packageDir))
		{
			return null;
		}

		// Find the latest version directory
		string? latestVersion = Directory.GetDirectories(packageDir)
			.OrderByDescending(d => d)
			.FirstOrDefault();

		if (latestVersion is null)
		{
			return null;
		}

		// Prefer the highest available TFM (net10.0 > net9.0) for forward compat.
		// Search: content/wasm-app/net10.0/ → content/wasm-app/net9.0/ → content/wasm-app/ (legacy)
		string[] tfms = ["net10.0", "net9.0"];
		string[] basePaths =
		[
			Path.Combine(latestVersion, "content", "wasm-app"),
			Path.Combine(latestVersion, "contentFiles", "any", "any", "wasm-app"),
			Path.Combine(latestVersion, "tools", "wasm-app")
		];

		foreach (string basePath in basePaths)
		{
			// Try TFM-specific subdirectories first
			foreach (string tfm in tfms)
			{
				string tfmPath = Path.Combine(basePath, tfm);
				if (Directory.Exists(tfmPath) && HasWasmApp(tfmPath))
				{
					return tfmPath;
				}
			}

			// Fall back to flat layout (no TFM subdirectory)
			if (Directory.Exists(basePath) && HasWasmApp(basePath))
			{
				return basePath;
			}
		}

		return null;
	}
}
