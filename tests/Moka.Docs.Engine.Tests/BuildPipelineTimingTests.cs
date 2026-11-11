using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Engine;

namespace Moka.Docs.Engine.Tests;

public sealed class BuildPipelineTimingTests
{
	[Fact]
	public async Task ExecuteAsync_RecordsAndLogsHowLongEachPhaseTook()
	{
		var logger = new ListLogger();
		var pipeline = new BuildPipeline([new SlowPhase("Late", 900, 0), new SlowPhase("Early", 100, 30)], logger)
		{
			PluginHook = (_, _) => Task.CompletedTask
		};

		await pipeline.ExecuteAsync(new BuildContext
		{
			Config = new SiteConfig { Site = new SiteMetadata { Title = "T" } },
			FileSystem = new MockFileSystem(),
			RootDirectory = "/project",
			OutputDirectory = "/project/_site"
		}, TestContext.Current.CancellationToken);

		pipeline.LastTimings.Select(t => t.Name).Should().Equal("Early", "Plugins", "Late");
		pipeline.LastTimings[0].Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(25));
		logger.Messages.Should().Contain(m => m.StartsWith("Phase Early took ", StringComparison.Ordinal));
		logger.Messages.Should().Contain(m => m.StartsWith("Build pipeline complete in ", StringComparison.Ordinal)
		                                      && m.Contains("Early ") && m.Contains("Late "));
	}

	private sealed class SlowPhase(string name, int order, int delayMs) : IBuildPhase
	{
		public string Name => name;

		public int Order => order;

		public Task ExecuteAsync(BuildContext context, CancellationToken ct = default) => Task.Delay(delayMs, ct);
	}

	private sealed class ListLogger : ILogger<BuildPipeline>
	{
		public List<string> Messages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
	}
}
