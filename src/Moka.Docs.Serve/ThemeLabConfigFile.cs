using System.Security.Cryptography;
using System.Text;

namespace Moka.Docs.Serve;

/// <summary>The configuration file as the theme lab read it.</summary>
/// <param name="Text">The file's text, without a byte order mark.</param>
/// <param name="HasBom">Whether the file starts with a UTF-8 byte order mark, which a save keeps.</param>
/// <param name="Version">A short hash of the file's bytes, to tell whether it changed since a page loaded it.</param>
internal sealed record ThemeLabConfigFile(string Text, bool HasBom, string Version)
{
	private static readonly UTF8Encoding _strictUtf8 = new(false, true);

	/// <summary>Reads the file.</summary>
	/// <exception cref="InvalidDataException">The file isn't UTF-8.</exception>
	/// <exception cref="IOException">The file can't be read.</exception>
	public static ThemeLabConfigFile Read(string path)
	{
		byte[] bytes = File.ReadAllBytes(path);
		if (bytes is [0xFF, 0xFE, ..] or [0xFE, 0xFF, ..])
		{
			throw new InvalidDataException("it is saved as UTF-16, and the theme lab only writes UTF-8");
		}

		bool bom = bytes is [0xEF, 0xBB, 0xBF, ..];
		try
		{
			string text = _strictUtf8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
			return new ThemeLabConfigFile(text, bom, VersionOf(bytes));
		}
		catch (DecoderFallbackException)
		{
			throw new InvalidDataException("it isn't valid UTF-8");
		}
	}

	/// <summary>
	///     Replaces the file with <paramref name="text" /> in one step: the text goes to a hidden file next
	///     to it, which is then moved over it, so a reader never sees half a file. A symbolic link is
	///     followed and its target replaced.
	/// </summary>
	/// <returns>The new <see cref="Version" />.</returns>
	public static string Write(string path, string text, bool bom)
	{
		var info = new FileInfo(path);
		string target = info.LinkTarget is null ? info.FullName : info.ResolveLinkTarget(true)?.FullName ?? info.FullName;
		byte[] body = Encoding.UTF8.GetBytes(text);
		byte[] bytes = bom ? [0xEF, 0xBB, 0xBF, .. body] : body;
		string temporary = Path.Combine(Path.GetDirectoryName(target)!,
			"." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp");
		try
		{
			File.WriteAllBytes(temporary, bytes);
			if (!OperatingSystem.IsWindows())
			{
				File.SetUnixFileMode(temporary, File.GetUnixFileMode(target));
			}

			File.Move(temporary, target, true);
		}
		catch
		{
			try
			{
				File.Delete(temporary);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// The move's own error is the one worth reporting.
			}

			throw;
		}

		return VersionOf(bytes);
	}

	private static string VersionOf(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes))[..16];
}
