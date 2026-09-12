using System.Reflection;

namespace Moka.Docs.Cli;

/// <summary>
///     The version shown in command headers and <c>mokadocs info</c>.
/// </summary>
internal static class CliVersion
{
	/// <summary>
	///     The package version, such as <c>1.6.1</c>. The informational version also carries
	///     the commit (<c>1.6.1+0667d04...</c>), which is noise in a header; the assembly
	///     version is always four parts (<c>1.6.1.0</c>), which does not match the package.
	/// </summary>
	public static string Current { get; } = Resolve();

	private static string Resolve()
	{
		Assembly assembly = typeof(CliVersion).Assembly;
		string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
		                 ?? assembly.GetName().Version?.ToString(3)
		                 ?? "0.0.0";

		int plus = version.IndexOf('+');
		return plus >= 0 ? version[..plus] : version;
	}
}
