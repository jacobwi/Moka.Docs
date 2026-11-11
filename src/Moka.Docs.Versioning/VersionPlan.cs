using System.IO.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;

namespace Moka.Docs.Versioning;

internal sealed record PlannedVersion
{
	public required DocVersion Version { get; init; }

	public VersionRef? Ref { get; init; }

	public bool UsesWorkingTree { get; init; }

	// Null for the working tree, and for a ref that git doesn't know or when git is missing.
	public string? Commit { get; init; }
}

// The configured versions with their labels, slugs, git refs and the default worked out. Shared by
// the single-tree commands (VersionManager.ApplyTo) and the versioned build, so both agree on labels.
internal sealed class VersionPlan
{
	internal const string Source = "Versioning";

	private static readonly string[] _defaultConfigNames = ["mokadocs.yaml", "mokadocs.yml"];

	public List<PlannedVersion> Versions { get; } = [];

	public List<Diagnostic> Problems { get; } = [];

	public PlannedVersion? Default => Versions.FirstOrDefault(v => v.Version.IsDefault);

	// The version the working tree is: the default when it is built from the working tree, else the
	// first version that is, else the default.
	public PlannedVersion? Current =>
		Default is { UsesWorkingTree: true } current ? current : Versions.FirstOrDefault(v => v.UsesWorkingTree) ?? Default;

	public static bool NamesRefs(SiteConfig config) =>
		config.Features.Versioning.Enabled && config.Features.Versioning.Versions.Any(v => VersionRef.From(v) is not null);

	// placesVersions is true for a versioned build, where each version gets a folder: a slug that
	// can't be a folder name, or one used twice, is then an error and the version is dropped.
	public static VersionPlan Create(SiteConfig config, IFileSystem fileSystem, string rootDirectory,
		string? configFileName, GitRepository? git, bool placesVersions)
	{
		var plan = new VersionPlan();
		VersioningFeatureConfig versioning = config.Features.Versioning;
		if (!versioning.Enabled)
		{
			return plan;
		}

		if (!versioning.Strategy.Equals("directory", StringComparison.OrdinalIgnoreCase)
		    && !versioning.Strategy.Equals("dropdown-only", StringComparison.OrdinalIgnoreCase))
		{
			plan.Warn($"features.versioning.strategy '{versioning.Strategy}' is not 'directory' or 'dropdown-only'; 'directory' is used");
		}

		List<VersionDefinition> entries = versioning.Versions.Count > 0
			? versioning.Versions
			: [new VersionDefinition { Label = "" }];
		string? currentBranch = entries.Any(e => VersionRef.From(e) is { IsTag: false }) ? git?.CurrentBranch() : null;
		string? workingTreeVersion = null;
		bool workingTreeVersionRead = false;

		var candidates = new List<(VersionDefinition Definition, PlannedVersion Version)>();
		foreach (VersionDefinition entry in entries)
		{
			VersionRef? reference = VersionRef.From(entry);
			if (reference is { IsTag: true } && !string.IsNullOrWhiteSpace(entry.Branch))
			{
				plan.Warn($"Version '{Describe(entry)}' sets both a branch and a tag; the tag is used");
			}

			bool usesWorkingTree = reference is null || (!reference.IsTag && reference.Name == currentBranch);
			string? commit = !usesWorkingTree && reference is not null ? git?.ResolveCommit(reference) : null;

			string? PackageVersion()
			{
				if (!usesWorkingTree)
				{
					return commit is not null && git is not null ? ReadPackageVersion(git, commit, configFileName, config) : null;
				}

				if (!workingTreeVersionRead)
				{
					workingTreeVersion = PackageVersionReader.Read(new FileSystemTree(fileSystem), rootDirectory,
						config.Content.Projects);
					workingTreeVersionRead = true;
				}

				return workingTreeVersion;
			}

			// A label without a number, such as "next", still gets the package's number, so the
			// version ranges in front matter work for it.
			string label = entry.Label.Trim();
			string? number = VersionNumber.TryParse(label, out VersionNumber labelNumber) ? labelNumber.ToString() : null;
			bool prerelease = entry.Prerelease;
			if (number is null)
			{
				string? packageVersion = PackageVersion();
				bool hasNumber = VersionNumber.TryParse(packageVersion, out VersionNumber packageNumber);
				if (label.Length == 0)
				{
					label = PackageVersionReader.FormatLabel(packageVersion) ?? reference?.Name ?? "latest";
					prerelease |= packageVersion?.Contains('-', StringComparison.Ordinal) == true;
				}

				number = hasNumber
					? packageNumber.ToString()
					: VersionNumber.TryParse(label, out labelNumber) ? labelNumber.ToString() : null;
			}

			candidates.Add((entry, new PlannedVersion
			{
				Version = new DocVersion
				{
					Label = label,
					Slug = VersionManager.GenerateSlug(label),
					IsPrerelease = prerelease,
					Number = number
				},
				Ref = reference,
				UsesWorkingTree = usesWorkingTree,
				Commit = commit
			}));
		}

		if (placesVersions)
		{
			var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			candidates.RemoveAll(c =>
			{
				string slug = c.Version.Version.Slug;
				if (!IsFolderName(slug))
				{
					plan.Error($"Version '{c.Version.Version.Label}' has no usable folder name (slug '{slug}'); give it a label that starts with a letter or digit");
					return true;
				}

				if (!slugs.Add(slug))
				{
					plan.Error($"Version '{c.Version.Version.Label}' has the same folder name '{slug}' as an earlier version; it was not built");
					return true;
				}

				return false;
			});
		}

		if (candidates.Count(c => c.Definition.Default) > 1)
		{
			plan.Warn($"Several versions set default: true; '{candidates.First(c => c.Definition.Default).Version.Version.Label}' is the default");
		}

		int defaultIndex = candidates.FindIndex(c => c.Definition.Default);
		if (defaultIndex < 0)
		{
			defaultIndex = candidates.FindIndex(c => !c.Version.Version.IsPrerelease);
		}

		for (int i = 0; i < candidates.Count; i++)
		{
			PlannedVersion version = candidates[i].Version;
			plan.Versions.Add(version with { Version = version.Version with { IsDefault = i == Math.Max(defaultIndex, 0) } });
		}

		return plan;
	}

	public void ReportTo(DiagnosticBag diagnostics, DiagnosticSeverity minimum = DiagnosticSeverity.Info)
	{
		foreach (Diagnostic problem in Problems.Where(p => p.Severity >= minimum))
		{
			if (problem.Severity == DiagnosticSeverity.Error)
			{
				diagnostics.Error(problem.Message, Source);
			}
			else
			{
				diagnostics.Warning(problem.Message, Source);
			}
		}
	}

	// Reads the projects from the config in that commit, or the current config when the commit has none.
	private static string? ReadPackageVersion(GitRepository git, string commit, string? configFileName, SiteConfig current)
	{
		var tree = new GitCommitTree(git, commit);
		SiteConfig config = current;
		foreach (string name in ConfigNames(configFileName))
		{
			if (tree.Combine(git.Prefix, name) is { } path && tree.Read(path) is { } yaml)
			{
				try
				{
					config = SiteConfigReader.Parse(yaml.TrimStart('﻿'));
				}
				catch (SiteConfigException)
				{
					// The build reports it when it reads that config from the checkout.
				}

				break;
			}
		}

		return PackageVersionReader.Read(tree, git.Prefix, config.Content.Projects);
	}

	public static IEnumerable<string> ConfigNames(string? configFileName) =>
		(configFileName is null ? _defaultConfigNames : [configFileName, .. _defaultConfigNames])
		.Distinct(StringComparer.Ordinal);

	// Slugs keep letters, digits, dots and hyphens; one such as ".." or "-" would leave the output folder
	// or be awkward in a URL.
	private static bool IsFolderName(string slug) =>
		slug.Length > 0 && char.IsAsciiLetterOrDigit(slug[0]) && !slug.EndsWith('.') && !slug.Contains("..", StringComparison.Ordinal);

	private static string Describe(VersionDefinition entry) =>
		entry.Label.Trim() is { Length: > 0 } label ? label : VersionRef.From(entry)?.Name ?? "(no label)";

	private void Warn(string message) =>
		Problems.Add(new Diagnostic { Severity = DiagnosticSeverity.Warning, Message = message, Source = Source });

	private void Error(string message) =>
		Problems.Add(new Diagnostic { Severity = DiagnosticSeverity.Error, Message = message, Source = Source });
}
