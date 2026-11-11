using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;

namespace Moka.Docs.Versioning;

// Picks the project the way CSharpAnalysisPhase picks the NuGet install widget's package (the first
// configured project that packs, else the first one), so the version label and the widget agree.
// Unlike the widget it returns null when nothing sets a version, instead of MSBuild's default 1.0.0.
internal static class PackageVersionReader
{
	public static string? Read(ISourceTree tree, string configDirectory, IReadOnlyList<ProjectSource> projects)
	{
		string? first = null;
		foreach (ProjectSource project in projects)
		{
			if (string.IsNullOrWhiteSpace(project.Path)
			    || tree.Combine(configDirectory, project.Path) is not { } path
			    || Parse(tree.Read(path)) is null)
			{
				continue;
			}

			first ??= path;
			if (IsPackable(tree, path))
			{
				return ReadVersion(tree, path);
			}
		}

		return first is null ? null : ReadVersion(tree, first);
	}

	public static string? FormatLabel(string? packageVersion) =>
		VersionNumber.TryParse(packageVersion, out VersionNumber number) ? $"v{number.Major}.{number.Minor}" : null;

	private static string? ReadVersion(ISourceTree tree, string projectPath) =>
		ReadVersion(Parse(tree.Read(projectPath))) ?? FromDirectoryBuildProps(tree, projectPath, ReadVersion);

	private static bool IsPackable(ISourceTree tree, string projectPath)
	{
		if (string.Equals(ReadInherited(tree, projectPath, "IsTestProject"), "true", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (ReadInherited(tree, projectPath, "IsPackable") is { } isPackable)
		{
			return !string.Equals(isPackable, "false", StringComparison.OrdinalIgnoreCase);
		}

		// Application SDKs default IsPackable to false; the others default it to true.
		string sdk = Parse(tree.Read(projectPath))?.Root?.Attribute("Sdk")?.Value ?? "";
		return !sdk.Split(';')
			.Select(name => name.Split('/')[0].Trim())
			.Any(name => name.Equals("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)
			             || name.Equals("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase)
			             || name.Equals("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase));
	}

	private static string? ReadInherited(ISourceTree tree, string projectPath, string name) =>
		ReadProperty(Parse(tree.Read(projectPath)), name)
		?? FromDirectoryBuildProps(tree, projectPath, doc => ReadProperty(doc, name));

	private static string? FromDirectoryBuildProps(ISourceTree tree, string projectPath, Func<XDocument?, string?> read)
	{
		for (string? dir = tree.Parent(projectPath); dir is not null; dir = tree.Parent(dir))
		{
			if (tree.Combine(dir, "Directory.Build.props") is { } props && read(Parse(tree.Read(props))) is { } value)
			{
				return value;
			}
		}

		return null;
	}

	private static string? ReadVersion(XDocument? doc)
	{
		string? version = ReadProperty(doc, "PackageVersion") ?? ReadProperty(doc, "Version");
		if (version is not null)
		{
			return version;
		}

		string? prefix = ReadProperty(doc, "VersionPrefix");
		string? suffix = ReadProperty(doc, "VersionSuffix");
		return prefix is null ? null : suffix is null ? prefix : $"{prefix}-{suffix}";
	}

	// A value built from other properties, such as $(Major).1, is skipped: nothing here evaluates MSBuild.
	private static string? ReadProperty(XDocument? doc, string name)
	{
		string? value = doc?.Root?.Elements()
			.Where(e => e.Name.LocalName == "PropertyGroup" && e.Attribute("Condition") is null)
			.Elements()
			.LastOrDefault(e => e.Name.LocalName == name && e.Attribute("Condition") is null)
			?.Value.Trim();

		return string.IsNullOrEmpty(value) || value.Contains("$(", StringComparison.Ordinal) ? null : value;
	}

	private static XDocument? Parse(string? xml)
	{
		if (xml is null)
		{
			return null;
		}

		try
		{
			return XDocument.Parse(xml.TrimStart('﻿'));
		}
		catch (XmlException)
		{
			return null;
		}
	}
}

// Read returns null for a missing or unreadable file, Parent returns null at the top, and Combine
// returns null for a path that leaves the tree.
internal interface ISourceTree
{
	string? Read(string path);

	string? Parent(string path);

	string? Combine(string directory, string relativePath);
}

internal sealed class FileSystemTree(IFileSystem fileSystem) : ISourceTree
{
	public string? Read(string path)
	{
		try
		{
			return fileSystem.File.Exists(path) ? fileSystem.File.ReadAllText(path) : null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	public string? Parent(string path) => fileSystem.Path.GetDirectoryName(path);

	public string? Combine(string directory, string relativePath)
	{
		try
		{
			return fileSystem.Path.GetFullPath(fileSystem.Path.Combine(directory, relativePath));
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return null;
		}
	}
}

// Paths are relative to the repository root with "/" separators, and the root is "". Reads blobs
// with git cat-file, so nothing is checked out.
internal sealed class GitCommitTree(GitRepository git, string commit) : ISourceTree
{
	public string? Read(string path) => path.Length == 0 ? null : git.ReadFile(commit, path);

	public string? Parent(string path)
	{
		if (path.Length == 0)
		{
			return null;
		}

		int slash = path.LastIndexOf('/');
		return slash < 0 ? "" : path[..slash];
	}

	public string? Combine(string directory, string relativePath)
	{
		if (Path.IsPathRooted(relativePath) || relativePath.StartsWith('/'))
		{
			return null;
		}

		var segments = new List<string>(directory.Split('/', StringSplitOptions.RemoveEmptyEntries));
		foreach (string segment in relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment == "..")
			{
				if (segments.Count == 0)
				{
					return null;
				}

				segments.RemoveAt(segments.Count - 1);
			}
			else if (segment != ".")
			{
				segments.Add(segment);
			}
		}

		return string.Join('/', segments);
	}
}
