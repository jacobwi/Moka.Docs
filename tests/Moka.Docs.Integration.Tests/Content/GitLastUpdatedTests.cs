using System.Diagnostics;
using FluentAssertions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;

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

	private void WritePage(string relativePath, string title, string body) =>
		File.WriteAllText(Path.Combine(_root, "docs", relativePath), $"---\ntitle: {title}\n---\n{body}\n");

	private void Commit(string isoDate)
	{
		Git(null, "add", "-A");
		Git(isoDate, "commit", "-q", "-m", "Commit at " + isoDate);
	}

	private void Git(string? isoDate, params string[] arguments)
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
		startInfo.Environment["GIT_AUTHOR_NAME"] = "MokaDocs Tests";
		startInfo.Environment["GIT_AUTHOR_EMAIL"] = "tests@mokadocs.invalid";
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
