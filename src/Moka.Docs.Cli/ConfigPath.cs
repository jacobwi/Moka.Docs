namespace Moka.Docs.Cli;

/// <summary>
///     Finds the configuration file a command reads. Every command that loads a project
///     goes through here, so they agree on which file that is.
/// </summary>
internal static class ConfigPath
{
	/// <summary>
	///     The <c>--config</c> value when one was given; otherwise <c>mokadocs.yaml</c> in the
	///     working directory, or <c>mokadocs.yml</c> when only that one exists.
	/// </summary>
	/// <param name="option">The <c>--config</c> value, or <c>null</c>.</param>
	/// <param name="workingDirectory">Directory to look in; defaults to the current directory.</param>
	/// <returns>An absolute path, which may not exist. Callers report a missing file.</returns>
	public static string Resolve(string? option, string? workingDirectory = null)
	{
		if (option is not null)
		{
			return Path.GetFullPath(option);
		}

		string directory = workingDirectory ?? Directory.GetCurrentDirectory();
		string yaml = Path.Combine(directory, "mokadocs.yaml");
		string yml = Path.Combine(directory, "mokadocs.yml");

		return !File.Exists(yaml) && File.Exists(yml) ? yml : yaml;
	}
}
