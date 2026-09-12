namespace Moka.Docs.Core;

/// <summary>
///     Reads target framework folder names, such as <c>net10.0</c>, from a project's bin folder.
/// </summary>
public static class TargetFrameworks
{
	/// <summary>
	///     The version of a target framework folder name when a runtime of the given major version
	///     can load it, else <c>null</c>. .NET Standard ranks lowest; .NET Framework folders such as
	///     net48 are not loadable.
	/// </summary>
	/// <remarks>
	///     Sort folders by this version, not by name: as strings, net9.0 sorts above net10.0.
	/// </remarks>
	/// <param name="folderName">The folder name, such as <c>net10.0</c> or <c>net8.0-windows</c>.</param>
	/// <param name="runtimeMajor">Major version of the runtime that will load the assemblies.</param>
	/// <returns>The framework version, or <c>null</c>.</returns>
	public static Version? LoadableVersion(string folderName, int runtimeMajor)
	{
		if (folderName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
		{
			return new Version(0, 0);
		}

		string numbers = folderName.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase)
			? folderName["netcoreapp".Length..]
			: folderName.StartsWith("net", StringComparison.OrdinalIgnoreCase)
				? folderName["net".Length..]
				: "";

		// Drop a platform suffix such as "-windows".
		int dash = numbers.IndexOf('-');
		if (dash >= 0)
		{
			numbers = numbers[..dash];
		}

		// Modern folders always have a dot (net9.0); dotless ones are .NET Framework (net48).
		return numbers.Contains('.') && Version.TryParse(numbers, out Version? version) && version.Major <= runtimeMajor
			? version
			: null;
	}
}
