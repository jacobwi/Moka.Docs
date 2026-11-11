using System.Diagnostics;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Content;
using Moka.Docs.Engine.Discovery;

namespace Moka.Docs.Integration.Tests.Content;

/// <summary>
///     Page dates come from the last commit that touched each file. They used to be file modified
///     times, so a site built from a fresh clone in CI showed the build date on every page.
/// </summary>
public sealed class GitLastUpdatedTests : IDisposable
{
	private static readonly Lazy<bool> _gitAvailable = new(() =>
	{
		try
		{
			using Process? process = Process.Start(new ProcessStartInfo("git", "--version")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			return process is not null && process.WaitForExit(10_000) && process.ExitCode == 0;
		}
		catch (Exception)
		{
			return false;
		}
	});

	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-gitdates-" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_root))
			{
				// git marks its object files read-only, which Directory.Delete refuses on Windows.
				foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
				{
					File.SetAttributes(file, FileAttributes.Normal);
				}

				Directory.Delete(_root, true);
			}

			File.Delete(_root + ".gitconfig");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task LastModified_CommittedFile_IsItsLastCommitDateAndChangedFilesKeepTheFileTime()
	{
		Assert.SkipUnless(_gitAvailable.Value, "git is not on PATH");

		Directory.CreateDirectory(Path.Combine(_root, "docs", "guide"));
		Git(null, "init", "-q");
		WritePage("index.md", "Home", "v1");
		WritePage("guide/intro.md", "Intro", "v1");
		WritePage("guide/edited.md", "Edited", "v1");
		Commit("2021-03-04T05:06:07Z");
		WritePage("guide/intro.md", "Intro", "v2");
		Commit("2022-08-09T10:11:12Z");
		WritePage("guide/edited.md", "Edited", "uncommitted");
		WritePage("guide/untracked.md", "Untracked", "v1");

		DryRunOutcome outcome = await DryRunBuild.RunAsync(new SiteConfig
		{
			Site = new SiteMetadata { Title = "Git Dates" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./_site", Cache = false }
		}, _root, false, false, TestContext.Current.CancellationToken);

		outcome.Failure.Should().BeNull();
		Dictionary<string, DateTimeOffset?> dates = outcome.Context.Pages.ToDictionary(p => p.Route, p => p.LastModified);
		dates["/"].Should().Be(DateTimeOffset.Parse("2021-03-04T05:06:07Z"));
		dates["/guide/intro"].Should().Be(DateTimeOffset.Parse("2022-08-09T10:11:12Z"));

		// Written by this test moments ago, long after both commits.
		dates["/guide/edited"].Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(10));
		dates["/guide/untracked"].Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(10));
	}

	[Fact]
	public async Task ReadAsync_StopsReadingTheLogOnceEveryWantedFileHasADate()
	{
		Assert.SkipUnless(_gitAvailable.Value, "git is not on PATH");

		// The whole log used to be read into memory, under a 30 second limit, however long the
		// history was. old.md is only in the oldest commit, so it stays unread when only the
		// newer files are asked for.
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		Git(null, "init", "-q");
		WritePage("old.md", "Old", "v1");
		WritePage("index.md", "Home", "v1");
		Commit("2020-01-02T03:04:05Z");
		WritePage("index.md", "Home", "v2");
		WritePage("new.md", "New", "v1");
		Commit("2023-06-07T08:09:10Z");

		string docs = Path.Combine(_root, "docs");
		IReadOnlyDictionary<string, DateTimeOffset> wanted = await GitCommitDates.ReadAsync(
			docs, NullLogger.Instance, TestContext.Current.CancellationToken, ["index.md", "new.md"]);
		IReadOnlyDictionary<string, DateTimeOffset> everything = await GitCommitDates.ReadAsync(
			docs, NullLogger.Instance, TestContext.Current.CancellationToken);

		wanted.Keys.Should().BeEquivalentTo("index.md", "new.md");
		wanted["index.md"].Should().Be(DateTimeOffset.Parse("2023-06-07T08:09:10Z"));
		everything.Keys.Should().BeEquivalentTo("index.md", "new.md", "old.md");
		everything["old.md"].Should().Be(DateTimeOffset.Parse("2020-01-02T03:04:05Z"));
	}

	[Fact]
	public async Task Contributors_AreEveryAuthorOfThePage_OnePerMailmappedEmail_MostCommitsFirst()
	{
		Assert.SkipUnless(_gitAvailable.Value, "git is not on PATH");

		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		Git(null, "init", "-q");
		File.WriteAllText(Path.Combine(_root, ".mailmap"), "Ann Lee <ann@new.example> <ann@old.example>\n");
		WritePage("guide.md", "Guide", "v1");
		WritePage("other.md", "Other", "v1");
		Commit("2020-01-01T00:00:00Z", "ann", "ann@old.example");
		WritePage("guide.md", "Guide", "v2");
		Commit("2021-01-01T00:00:00Z", "Bob", "BOB@example.com");
		WritePage("guide.md", "Guide", "v3");
		Commit("2022-01-01T00:00:00Z", "Ann L.", "Ann@New.Example");
		WritePage("guide.md", "Guide", "v4");
		Commit("2023-01-01T00:00:00Z", "Robert", "bob@example.com");
		WritePage("guide.md", "Guide", "v5");
		Commit("2024-01-01T00:00:00Z", "Ann Lee", "ann@new.example");
		WritePage("other.md", "Other", "uncommitted");

		DryRunOutcome shown = await DryRunBuild.RunAsync(ContributorsConfig(true), _root, false, false,
			TestContext.Current.CancellationToken);
		DryRunOutcome hidden = await DryRunBuild.RunAsync(ContributorsConfig(false), _root, false, false,
			TestContext.Current.CancellationToken);

		shown.Failure.Should().BeNull();
		Dictionary<string, IReadOnlyList<PageContributor>> byRoute =
			shown.Context.Pages.ToDictionary(p => p.Route, p => p.Contributors);
		byRoute["/guide"].Select(c => (c.Name, c.Commits)).Should().Equal(("Ann Lee", 3), ("Robert", 2));
		byRoute["/guide"][1].Email.Should().Be("bob@example.com", "the newest commit's spelling is kept");
		byRoute["/other"].Select(c => c.Name).Should().Equal(["Ann Lee"], "uncommitted changes don't hide the history");
		hidden.Context.Pages.Should().OnlyContain(p => p.Contributors.Count == 0);
	}

	[Fact]
	public async Task ReadHistoryAsync_WithAuthors_ReadsPastTheNewestCommitOfEveryWantedFile()
	{
		Assert.SkipUnless(_gitAvailable.Value, "git is not on PATH");

		// Without authors the log stops at index.md's newest commit; its first author is older.
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		Git(null, "init", "-q");
		WritePage("index.md", "Home", "v1");
		Commit("2020-01-02T03:04:05Z", "First Author", "first@example.com");
		WritePage("index.md", "Home", "v2");
		Commit("2023-06-07T08:09:10Z", "Second Author", "second@example.com");

		GitFileHistory history = await GitCommitDates.ReadHistoryAsync(Path.Combine(_root, "docs"),
			NullLogger.Instance, TestContext.Current.CancellationToken, ["index.md"], true);
		GitFileHistory datesOnly = await GitCommitDates.ReadHistoryAsync(Path.Combine(_root, "docs"),
			NullLogger.Instance, TestContext.Current.CancellationToken, ["index.md"], false);

		history.Dates["index.md"].Should().Be(DateTimeOffset.Parse("2023-06-07T08:09:10Z"));
		history.Authors["index.md"].Select(a => a.Name).Should().Equal("Second Author", "First Author");
		datesOnly.Dates["index.md"].Should().Be(DateTimeOffset.Parse("2023-06-07T08:09:10Z"));
		datesOnly.Authors.Should().BeEmpty();
	}

	private static SiteConfig ContributorsConfig(bool show) => new()
	{
		Site = new SiteMetadata { Title = "Contributors" },
		Content = new ContentConfig { Docs = "./docs" },
		Build = new BuildConfig { Output = "./_site", Cache = false },
		Theme = new ThemeConfig { Options = new ThemeOptions { ShowContributors = show } }
	};

	private void WritePage(string relativePath, string title, string body) =>
		File.WriteAllText(Path.Combine(_root, "docs", relativePath), $"---\ntitle: {title}\n---\n{body}\n");

	private void Commit(string isoDate, string author = "MokaDocs Tests", string email = "tests@mokadocs.invalid")
	{
		Git(null, "add", "-A");
		GitAs(author, email, isoDate, "commit", "-q", "-m", "Commit at " + isoDate);
	}

	private void Git(string? isoDate, params string[] arguments) =>
		GitAs("MokaDocs Tests", "tests@mokadocs.invalid", isoDate, arguments);

	private void GitAs(string author, string email, string? isoDate, params string[] arguments)
	{
		// Every command here runs quietly, so only stderr is captured, for the failure message.
		var startInfo = new ProcessStartInfo("git")
		{
			WorkingDirectory = _root,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		// The fixture repository must not pick up the machine's git configuration, such as commit
		// signing or hooks, or an outer repository from a hook environment.
		string emptyConfig = _root + ".gitconfig";
		if (!File.Exists(emptyConfig))
		{
			File.WriteAllText(emptyConfig, "");
		}

		startInfo.Environment["GIT_CONFIG_GLOBAL"] = emptyConfig;
		startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
		startInfo.Environment.Remove("GIT_DIR");
		startInfo.Environment.Remove("GIT_WORK_TREE");
		startInfo.Environment.Remove("GIT_INDEX_FILE");
		startInfo.Environment["GIT_AUTHOR_NAME"] = author;
		startInfo.Environment["GIT_AUTHOR_EMAIL"] = email;
		startInfo.Environment["GIT_COMMITTER_NAME"] = "MokaDocs Tests";
		startInfo.Environment["GIT_COMMITTER_EMAIL"] = "tests@mokadocs.invalid";
		if (isoDate is not null)
		{
			startInfo.Environment["GIT_AUTHOR_DATE"] = isoDate;
			startInfo.Environment["GIT_COMMITTER_DATE"] = isoDate;
		}

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("git did not start");
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();
		process.ExitCode.Should().Be(0, $"git {string.Join(' ', arguments)} failed: {error}");
	}
}
