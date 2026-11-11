using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Diagnostics;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Versioning;

/// <summary>
///     Runs one build pipeline over <paramref name="context" />, with <paramref name="routesPhase" />
///     added to the phases the host registers. The versioned build calls it once per version.
/// </summary>
/// <param name="context">The version's build context.</param>
/// <param name="routesPhase">A phase the pipeline must run; it pauses the build until every version's pages are known.</param>
/// <param name="ct">Cancels the build.</param>
public delegate Task VersionPipelineRunner(BuildContext context, IBuildPhase routesPhase, CancellationToken ct);

/// <summary>
///     Builds every version listed in <c>features.versioning.versions</c> into its own folder of one
///     site: <c>{output}/{slug}/</c> for each version, and the default version at the site root too.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 A version with a <c>tag</c> or <c>branch</c> is built from that ref, checked out into
///                 a temporary git worktree in the system temp folder that is removed afterwards. The
///                 working tree is used for a version without a ref and for the one whose branch is
///                 checked out. The user's working tree and index are never touched.
///             </description>
///         </item>
///         <item>
///             <description>
///                 A version from a ref is built with the mokadocs.yaml found at the same path in that
///                 ref, or the current one when it has none. Where the site is published comes from the
///                 current config: <c>site.url</c>, <c>build</c> and <c>features.versioning</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Each pipeline runs until every version's pages are known, then the versions render
///                 and write one at a time, the site root first because its clean step empties the
///                 whole output folder. The version dropdown on every page then links to the same page
///                 in each other version that has it, and to that version's home page otherwise.
///             </description>
///         </item>
///         <item>
///             <description>
///                 A ref that doesn't exist, a checkout that fails or a version whose build throws is an
///                 error naming the version; the other versions are still built.
///             </description>
///         </item>
///     </list>
/// </remarks>
public sealed class VersionedBuildOrchestrator(ILogger<VersionedBuildOrchestrator> logger)
{
	/// <summary>
	///     Whether <c>mokadocs build</c> builds the versions of this config from git: versioning is on,
	///     the strategy isn't <c>dropdown-only</c>, and at least one version names a branch or tag.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	public static bool IsVersionedBuild(SiteConfig config) =>
		VersionPlan.NamesRefs(config)
		&& !config.Features.Versioning.Strategy.Equals("dropdown-only", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	///     Checks, without building anything, that every git ref a versioned build would check out
	///     exists, and that every version has a folder name of its own. Problems are added to
	///     <paramref name="diagnostics" /> as errors. Does nothing for a config that isn't a
	///     <see cref="IsVersionedBuild">versioned build</see>.
	/// </summary>
	/// <param name="config">The site configuration.</param>
	/// <param name="rootDirectory">The folder of mokadocs.yaml.</param>
	/// <param name="diagnostics">Receives the problems.</param>
	public static void CheckRefs(SiteConfig config, string rootDirectory, DiagnosticBag diagnostics)
	{
		ArgumentNullException.ThrowIfNull(config);
		ArgumentNullException.ThrowIfNull(diagnostics);

		if (!IsVersionedBuild(config))
		{
			return;
		}

		GitRepository? git = GitRepository.Open(rootDirectory);
		VersionPlan plan = VersionPlan.Create(config, new FileSystem(), rootDirectory, null, git, true);
		plan.ReportTo(diagnostics, DiagnosticSeverity.Error);
		foreach (PlannedVersion version in plan.Versions.Where(v => !v.UsesWorkingTree && v.Commit is null))
		{
			diagnostics.Error(MissingRef(version, git is null, rootDirectory), VersionPlan.Source);
		}
	}

	/// <summary>Builds the versions.</summary>
	/// <param name="request">What to build and where.</param>
	/// <param name="runPipeline">Runs one version's pipeline.</param>
	/// <param name="ct">Cancels the build.</param>
	/// <returns>Every pipeline that ran and the diagnostics of all of them, each prefixed with its version.</returns>
	public async Task<VersionedBuildResult> RunAsync(VersionedBuildRequest request, VersionPipelineRunner runPipeline,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(runPipeline);

		var result = new VersionedBuildResult();
		SiteConfig config = request.Config;
		string configPath = Path.GetFullPath(request.ConfigPath);
		string rootDirectory = Path.GetDirectoryName(configPath)!;
		string outputDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.OutputDirectory));

		if (!config.Features.Versioning.Enabled)
		{
			result.Diagnostics.Error("Building versions needs features.versioning.enabled: true", VersionPlan.Source);
			return result;
		}

		var fileSystem = new FileSystem();
		GitRepository? git = VersionPlan.NamesRefs(config) ? GitRepository.Open(rootDirectory) : null;
		VersionPlan plan = VersionPlan.Create(config, fileSystem, rootDirectory, Path.GetFileName(configPath), git, true);
		plan.ReportTo(result.Diagnostics);

		var available = new List<PlannedVersion>();
		foreach (PlannedVersion version in plan.Versions)
		{
			if (version.UsesWorkingTree || version.Commit is not null)
			{
				available.Add(version);
			}
			else
			{
				result.Diagnostics.Error(MissingRef(version, git is null, rootDirectory), VersionPlan.Source);
			}
		}

		List<PlannedVersion>? selected = Select(available, request, result.Diagnostics);
		if (selected is null || selected.Count == 0)
		{
			return result;
		}

		string sitePrefix = config.Build.BasePath == "/" ? "" : config.Build.BasePath;
		List<DocVersion> versions = available
			.Select(v => v.Version with { UrlPath = v.Version.IsDefault ? sitePrefix : $"{sitePrefix}/{v.Version.Slug}" })
			.ToList();

		string tempRoot = Path.Combine(request.WorkDirectory ?? Path.GetTempPath(),
			"mokadocs-versions-" + Guid.NewGuid().ToString("N")[..12]);
		var worktrees = new List<string>();
		try
		{
			var jobs = new List<VersionBuildJob>();
			foreach (PlannedVersion version in selected)
			{
				ct.ThrowIfCancellationRequested();

				VersionSource? source = version.UsesWorkingTree
					? new VersionSource(rootDirectory, config, "the working tree")
					: CheckOut(version, git, tempRoot, rootDirectory, outputDirectory, Path.GetFileName(configPath), config,
						worktrees, result.Diagnostics);
				if (source is null)
				{
					continue;
				}

				DocVersion current = versions.First(v => v.Slug == version.Version.Slug);
				if (current.IsDefault)
				{
					jobs.Insert(0, CreateJob(request, version, current, source, versions, outputDirectory, true, fileSystem));
				}

				jobs.Add(CreateJob(request, version, current, source, versions, outputDirectory, false, fileSystem));
			}

			if (jobs.Count > 0)
			{
				await RunJobsAsync(jobs, versions, outputDirectory, fileSystem, runPipeline, result, ct);
			}
		}
		finally
		{
			foreach (string worktree in worktrees)
			{
				git?.RemoveWorktree(worktree);
			}

			GitRepository.DeleteDirectory(tempRoot);
		}

		return result;
	}

	private static List<PlannedVersion>? Select(List<PlannedVersion> available, VersionedBuildRequest request,
		DiagnosticBag diagnostics)
	{
		IEnumerable<PlannedVersion> selected = available;
		if (request.WorkingTreeOnly)
		{
			selected = selected.Where(v => v.UsesWorkingTree);
		}

		if (request.Only.Count == 0)
		{
			return selected.ToList();
		}

		var chosen = new List<PlannedVersion>();
		foreach (string name in request.Only)
		{
			PlannedVersion? match = available.FirstOrDefault(v =>
				v.Version.Slug.Equals(name, StringComparison.OrdinalIgnoreCase)
				|| v.Version.Label.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (match is null)
			{
				diagnostics.Error(
					$"There is no version '{name}' to build. The versions are: {string.Join(", ", available.Select(v => v.Version.Slug))}",
					VersionPlan.Source);
				return null;
			}

			if (!chosen.Contains(match) && selected.Contains(match))
			{
				chosen.Add(match);
			}
		}

		return chosen;
	}

	private VersionSource? CheckOut(PlannedVersion version, GitRepository? git, string tempRoot, string rootDirectory,
		string outputDirectory, string configFileName, SiteConfig current, List<string> worktrees, DiagnosticBag diagnostics)
	{
		string label = version.Version.Label;
		if (git is null || version.Commit is null || version.Ref is null)
		{
			return null;
		}

		string tempFull = Path.GetFullPath(tempRoot);
		foreach (string folder in new[] { rootDirectory, outputDirectory })
		{
			string relative = Path.GetRelativePath(folder, tempFull);
			if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
			{
				diagnostics.Error(
					$"Version '{label}' was not checked out: the temp folder '{tempFull}' is inside '{folder}'. Point TMPDIR or TEMP elsewhere",
					VersionPlan.Source);
				return null;
			}
		}

		string worktree = Path.Combine(tempFull, version.Version.Slug);
		Directory.CreateDirectory(tempFull);
		logger.LogInformation("Checking out {Ref} for version {Label} into {Path}", version.Ref, label, worktree);
		if (!git.TryAddWorktree(worktree, version.Commit, Path.Combine(tempFull, "no-hooks"), out string error))
		{
			diagnostics.Error($"Version '{label}': could not check out {version.Ref}: {error}", VersionPlan.Source);
			GitRepository.DeleteDirectory(worktree);
			return null;
		}

		worktrees.Add(worktree);
		string versionRoot = Path.GetFullPath(Path.Combine(worktree, git.Prefix));
		var fileSystem = new FileSystem();
		foreach (string name in VersionPlan.ConfigNames(configFileName))
		{
			string path = Path.Combine(versionRoot, name);
			if (!fileSystem.File.Exists(path))
			{
				continue;
			}

			try
			{
				return new VersionSource(versionRoot, new SiteConfigReader(fileSystem).Read(path), version.Ref.ToString());
			}
			catch (SiteConfigException ex)
			{
				diagnostics.Warning(
					$"Version '{label}': {name} in {version.Ref} could not be read ({ex.Message}); it was built with the current config",
					VersionPlan.Source);
				return new VersionSource(versionRoot, current, version.Ref.ToString());
			}
		}

		diagnostics.Info($"Version '{label}': {version.Ref} has no {configFileName}; it was built with the current config",
			VersionPlan.Source);
		return new VersionSource(versionRoot, current, version.Ref.ToString());
	}

	private static VersionBuildJob CreateJob(VersionedBuildRequest request, PlannedVersion planned, DocVersion version,
		VersionSource source, List<DocVersion> versions, string outputDirectory, bool isSiteRoot, IFileSystem fileSystem)
	{
		SiteConfig current = request.Config;
		string slug = version.Slug;
		string sitePrefix = current.Build.BasePath == "/" ? "" : current.Build.BasePath;

		EditLinkConfig? editLink = source.Config.Site.EditLink;
		if (editLink is not null && !planned.UsesWorkingTree && planned.Ref is { IsTag: false } branch)
		{
			editLink = editLink with { Branch = branch.Name };
		}

		SiteConfig config = source.Config with
		{
			Site = source.Config.Site with
			{
				Url = isSiteRoot ? current.Site.Url : VersionSiteUrl(current.Site.Url, current.Build.BasePath, slug),
				EditLink = editLink
			},

			// build.output stays the whole site's folder, so discovery skips every version's output
			// when it sits inside the docs folder; the version's own folder is the context's. The
			// site root holds the other versions' folders, so it is only cleaned when they are all
			// rebuilt after it.
			Build = current.Build with
			{
				BasePath = isSiteRoot ? current.Build.BasePath : $"{sitePrefix}/{slug}",
				Output = outputDirectory,
				Clean = current.Build.Clean && (!isSiteRoot || (request.Only.Count == 0 && !request.WorkingTreeOnly))
			},
			Features = source.Config.Features with { Versioning = current.Features.Versioning }
		};

		var context = new BuildContext
		{
			Config = config,
			FileSystem = fileSystem,
			RootDirectory = source.RootDirectory,
			OutputDirectory = isSiteRoot ? outputDirectory : Path.Combine(outputDirectory, slug),
			IncludeDrafts = request.IncludeDrafts,
			UseCache = request.UseCache
		};
		context.Versions.AddRange(versions);
		context.CurrentVersion = version;

		return new VersionBuildJob
		{
			Version = version,
			IsSiteRoot = isSiteRoot,
			Source = source.Description,
			Context = context
		};
	}

	// site.url may or may not include the base path (SiteUrls accepts both). When it does, the
	// version's folder has to be added to it as well, or SiteUrls would add the whole version base
	// path a second time.
	internal static string VersionSiteUrl(string siteUrl, string basePath, string slug)
	{
		if (string.IsNullOrWhiteSpace(siteUrl))
		{
			return siteUrl;
		}

		string origin = siteUrl.Trim().TrimEnd('/');
		string prefix = basePath == "/" ? "" : "/" + basePath.Trim('/');
		return prefix.Length > 0 && origin.EndsWith(prefix, StringComparison.OrdinalIgnoreCase)
			? $"{origin}/{slug}"
			: origin;
	}

	private async Task RunJobsAsync(List<VersionBuildJob> jobs, List<DocVersion> versions, string outputDirectory,
		IFileSystem fileSystem, VersionPipelineRunner runPipeline, VersionedBuildResult result, CancellationToken ct)
	{
		var runs = new List<Task>(jobs.Count);
		try
		{
			// One pipeline at a time: each runs until its pages are known and waits there while the
			// next one starts. Two pipelines over the same folder (the default version's two copies)
			// never run at once, so they can't race on its .mokadocs cache.
			foreach (VersionBuildJob job in jobs)
			{
				ct.ThrowIfCancellationRequested();
				logger.LogInformation("Building {Name} from {Source}", job.Name, job.Source);
				Task run = RunJobAsync(job, runPipeline, ct);
				runs.Add(run);
				await Task.WhenAny(job.Arrived.Task, run);
			}

			List<DocVersion> withRoutes = AddRoutes(versions, jobs, outputDirectory, fileSystem);
			ReportRootCollisions(jobs, versions, result.Diagnostics);

			var seen = new HashSet<(DiagnosticSeverity, string)>();
			for (int i = 0; i < jobs.Count; i++)
			{
				jobs[i].Released.TrySetResult(withRoutes);
				bool succeeded = true;
				try
				{
					await runs[i];
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					throw;
				}
				catch (Exception ex)
				{
					succeeded = false;
					result.Diagnostics.Error($"{jobs[i].Name} failed: {ex.Message}", VersionPlan.Source);
					logger.LogDebug(ex, "{Name} failed", jobs[i].Name);
				}

				foreach (Diagnostic diagnostic in jobs[i].Context.Diagnostics.All)
				{
					string message = $"{jobs[i].Version.Label}: {diagnostic.Message}";
					if (seen.Add((diagnostic.Severity, message)))
					{
						Add(result.Diagnostics, diagnostic with { Message = message });
					}
				}

				result.Builds.Add(new VersionBuildOutcome
				{
					Version = jobs[i].Version,
					IsSiteRoot = jobs[i].IsSiteRoot,
					Source = jobs[i].Source,
					Context = jobs[i].Context,
					Succeeded = succeeded
				});
			}
		}
		finally
		{
			foreach (VersionBuildJob job in jobs)
			{
				job.Released.TrySetCanceled(CancellationToken.None);
			}

			// Every pipeline has to be finished before its worktree is deleted.
			await Task.WhenAll(runs.Select(run => run.ContinueWith(_ => { }, CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)));
		}
	}

	private static async Task RunJobAsync(VersionBuildJob job, VersionPipelineRunner runPipeline, CancellationToken ct)
	{
		await Task.Yield();
		await runPipeline(job.Context, new VersionRoutesPhase(job), ct);
	}

	// Versions that weren't built in this run (with --version) still have the folders an earlier
	// build wrote, so their routes come from the index.html files there.
	private static List<DocVersion> AddRoutes(List<DocVersion> versions, List<VersionBuildJob> jobs,
		string outputDirectory, IFileSystem fileSystem)
	{
		var routes = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
		foreach (VersionBuildJob job in jobs.Where(j => j.Arrived.Task.IsCompletedSuccessfully))
		{
			routes[job.Version.Slug] = job.Arrived.Task.Result;
		}

		foreach (DocVersion version in versions.Where(v => !routes.ContainsKey(v.Slug)))
		{
			string folder = Path.Combine(outputDirectory, version.Slug);
			if (fileSystem.Directory.Exists(folder))
			{
				routes[version.Slug] = fileSystem.Directory.EnumerateFiles(folder, "index.html", SearchOption.AllDirectories)
					.Select(file => "/" + Path.GetRelativePath(folder, Path.GetDirectoryName(file)!).Replace('\\', '/'))
					.Select(route => route == "/." ? "/" : route)
					.ToHashSet(StringComparer.Ordinal);
			}
		}

		return versions.Select(v => routes.TryGetValue(v.Slug, out IReadOnlySet<string>? r) ? v with { Routes = r } : v)
			.ToList();
	}

	private static void ReportRootCollisions(List<VersionBuildJob> jobs, List<DocVersion> versions,
		DiagnosticBag diagnostics)
	{
		if (jobs.FirstOrDefault(j => j.IsSiteRoot) is not { } root || !root.Arrived.Task.IsCompletedSuccessfully)
		{
			return;
		}

		foreach (string route in root.Arrived.Task.Result)
		{
			string first = route.Trim('/').Split('/')[0];
			if (versions.FirstOrDefault(v => v.Slug.Equals(first, StringComparison.OrdinalIgnoreCase)) is { } clash)
			{
				diagnostics.Error(
					$"Page '{route}' of version '{root.Version.Label}' at the site root is in the folder of version '{clash.Label}'; rename the page or the version",
					VersionPlan.Source);
			}
		}
	}

	private static string MissingRef(PlannedVersion version, bool noGit, string rootDirectory) => noGit
		? $"Version '{version.Version.Label}' is built from {version.Ref}, but '{rootDirectory}' is not in a git work tree or git is not installed"
		: $"Version '{version.Version.Label}': {version.Ref} was not found in the repository. Check the name, and fetch it first in CI (actions/checkout needs fetch-depth: 0 for tags and other branches)";

	private static void Add(DiagnosticBag bag, Diagnostic diagnostic)
	{
		switch (diagnostic.Severity)
		{
			case DiagnosticSeverity.Error:
				bag.Error(diagnostic.Message, diagnostic.Source);
				break;
			case DiagnosticSeverity.Warning:
				bag.Warning(diagnostic.Message, diagnostic.Source);
				break;
			default:
				bag.Info(diagnostic.Message, diagnostic.Source);
				break;
		}
	}

	private sealed record VersionSource(string RootDirectory, SiteConfig Config, string Description);
}

/// <summary>What a versioned build builds, and where.</summary>
public sealed record VersionedBuildRequest
{
	/// <summary>The current configuration, with command-line overrides applied.</summary>
	public required SiteConfig Config { get; init; }

	/// <summary>The path of mokadocs.yaml. Its folder is the project root, and its name is looked for in each ref.</summary>
	public required string ConfigPath { get; init; }

	/// <summary>The site's output folder. Each version goes into a subfolder named after its slug.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether draft pages are built.</summary>
	public bool IncludeDrafts { get; init; }

	/// <summary>Whether builds may reuse cached analysis.</summary>
	public bool UseCache { get; init; } = true;

	/// <summary>
	///     Slugs or labels of the versions to build; empty builds them all. The others keep the folders
	///     an earlier build wrote, and the dropdown still links to them.
	/// </summary>
	public IReadOnlyCollection<string> Only { get; init; } = [];

	/// <summary>Builds only the versions that come from the working tree, as a watch rebuild does.</summary>
	public bool WorkingTreeOnly { get; init; }

	/// <summary>
	///     The folder the temporary checkouts are made in, or <c>null</c> for the system temp folder. It
	///     must be outside the project and output folders. Each build uses a new subfolder and deletes it.
	/// </summary>
	public string? WorkDirectory { get; init; }
}

/// <summary>The outcome of a versioned build.</summary>
public sealed class VersionedBuildResult
{
	/// <summary>
	///     The build's own problems and those of every version's pipeline, each prefixed with the
	///     version's label. The two copies of the default version report a problem once.
	/// </summary>
	public DiagnosticBag Diagnostics { get; } = new();

	/// <summary>Each pipeline that ran, in the order its output was written.</summary>
	public List<VersionBuildOutcome> Builds { get; } = [];
}

/// <summary>One version's pipeline run.</summary>
public sealed record VersionBuildOutcome
{
	/// <summary>The version that was built.</summary>
	public required DocVersion Version { get; init; }

	/// <summary>Whether this is the default version's copy at the site root.</summary>
	public required bool IsSiteRoot { get; init; }

	/// <summary>Where it was built from, such as <c>the working tree</c> or <c>tag 'v1.0.0'</c>.</summary>
	public required string Source { get; init; }

	/// <summary>The build's context: pages, search index, diagnostics and output folder.</summary>
	public required BuildContext Context { get; init; }

	/// <summary>Whether the pipeline finished without throwing.</summary>
	public bool Succeeded { get; init; }
}

internal sealed class VersionBuildJob
{
	public required DocVersion Version { get; init; }

	public required bool IsSiteRoot { get; init; }

	public required string Source { get; init; }

	public required BuildContext Context { get; init; }

	public TaskCompletionSource<IReadOnlySet<string>> Arrived { get; } =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	public TaskCompletionSource<IReadOnlyList<DocVersion>> Released { get; } =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	public string Name => IsSiteRoot ? $"Version '{Version.Label}' (site root copy)" : $"Version '{Version.Label}'";
}

// Runs after search indexing and before rendering, when a version's pages are final: hands its routes
// to the orchestrator, then waits until every version's routes are known and its turn to write comes.
internal sealed class VersionRoutesPhase(VersionBuildJob job) : IBuildPhase
{
	public string Name => "VersionRoutes";

	public int Order => 850;

	public async Task ExecuteAsync(BuildContext context, CancellationToken ct = default)
	{
		var routes = context.Pages
			.Where(p => context.IncludeDrafts || p.FrontMatter.Visibility != PageVisibility.Draft)
			.Select(p => p.Route)
			.ToHashSet(StringComparer.Ordinal);
		job.Arrived.TrySetResult(routes);

		IReadOnlyList<DocVersion> versions = await job.Released.Task.WaitAsync(ct);
		context.Versions.Clear();
		context.Versions.AddRange(versions);
		context.CurrentVersion = versions.FirstOrDefault(v => v.Slug == job.Version.Slug) ?? context.CurrentVersion;
	}
}
