using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Cli;

/// <summary>
///     <c>mokadocs build --watch</c> on a real project, with real builds and real edits.
/// </summary>
public sealed class BuildWatchTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-buildwatch-" + Guid.NewGuid().ToString("N"));

	public BuildWatchTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"), "---\ntitle: Home\n---\n\nVersion one.\n");
		File.WriteAllText(Path.Combine(_root, "mokadocs.yaml"), "site:\n  title: Watch\n");
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task OutputInsideTheDocsFolder_RebuildsOncePerEdit()
	{
		// build --watch didn't ignore the output folder, so with output at ./docs/_site every build's
		// writes started the next one: 39 rebuilds in 14 seconds.
		var config = new SiteConfig
		{
			Site = new SiteMetadata { Title = "Watch" },
			Content = new ContentConfig { Docs = "./docs" },
			Build = new BuildConfig { Output = "./docs/_site", Cache = false }
		};
		string docs = Path.Combine(_root, "docs");
		string output = Path.Combine(docs, "_site");
		await using ServiceProvider provider = BuildCommand.BuildServices(config, false);
		BuildPipeline pipeline = provider.GetRequiredService<BuildPipeline>();
		await BuildCommand.InitializePluginsAsync(provider, pipeline);

		int builds = 0;
		int running = 0;
		int overlapped = 0;
		async Task Build()
		{
			if (Interlocked.Increment(ref running) > 1)
			{
				Interlocked.Increment(ref overlapped);
			}

			await pipeline.ExecuteAsync(new BuildContext
			{
				Config = config,
				FileSystem = new FileSystem(),
				RootDirectory = _root,
				OutputDirectory = output
			}, Ct);
			Interlocked.Decrement(ref running);
			Interlocked.Increment(ref builds);
		}

		await Build();
		File.Exists(Path.Combine(output, "index.html")).Should().BeTrue();
		builds = 0;

		using var stop = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		Task watching = BuildCommand.WatchAsync(docs, Path.Combine(_root, "mokadocs.yaml"),
			BuildCommand.WatchIgnoredDirectories(_root, output), NullLogger<FileWatcher>.Instance, Build, stop.Token);
		try
		{
			await Task.Delay(500, Ct);
			File.WriteAllText(Path.Combine(docs, "index.md"), "---\ntitle: Home\n---\n\nVersion two.\n");

			DateTime until = DateTime.UtcNow.AddSeconds(30);
			while (Volatile.Read(ref builds) == 0 && DateTime.UtcNow < until)
			{
				await Task.Delay(50, Ct);
			}

			await Task.Delay(3000, Ct);
		}
		finally
		{
			await stop.CancelAsync();
			await watching;
		}

		builds.Should().Be(1, "writing the site into the docs folder must not start another build");
		overlapped.Should().Be(0);
		File.ReadAllText(Path.Combine(output, "index.html")).Should().Contain("Version two.");
	}

	[Fact]
	public void WatchIgnoredDirectories_CoverTheOutputAndTheCache()
	{
		string output = Path.Combine(_root, "docs", "_site");

		BuildCommand.WatchIgnoredDirectories(_root, output)
			.Should().BeEquivalentTo([output, Path.Combine(_root, ".mokadocs")]);
	}
}
