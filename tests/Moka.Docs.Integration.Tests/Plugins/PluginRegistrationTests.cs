using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore;
using Moka.Docs.Cli.Commands;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Integration.Tests.AspNetCore;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     Both hosts register plugins through <see cref="PluginServiceExtensions.AddMokaDocsPlugins(IServiceCollection, Action{MokaPluginOptions})" />.
///     There used to be three lists: five plugins in the CLI, the Python plugin alone in
///     <c>AddMokaDocsPlugins</c>, and openapi plus the declared ones in <c>AddMokaDocs</c>.
/// </summary>
public sealed class PluginRegistrationTests
{
	private static readonly SiteConfig _config = new() { Site = new SiteMetadata { Title = "T" } };

	[Fact]
	public async Task Cli_RegistersEveryBuiltInAndLoadsPathsFromTheConfigFolder()
	{
		string root = Path.Combine(Path.GetTempPath(), "mokadocs-registration");
		await using ServiceProvider provider = BuildCommand.BuildServices(_config, false, root);

		provider.GetServices<IMokaPlugin>().Select(p => p.Id).Should().BeEquivalentTo(BuiltInPlugins.Ids);
		provider.GetRequiredService<MokaPluginOptions>().PluginBaseDirectory.Should().Be(root);
	}

	[Fact]
	public void AspNetCoreHost_RegistersTheDeclaredBuiltInsThroughTheSameMethodWithoutPathLoading()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.Plugins = [new PluginEntry { Id = "mokadocs-changelog" }, new PluginEntry { Id = "my-own" }];
		});

		using ServiceProvider provider = services.BuildServiceProvider();

		provider.GetServices<IMokaPlugin>().Select(p => p.Id).Should().Equal("mokadocs-changelog");
		provider.GetRequiredService<MokaPluginOptions>().PluginBaseDirectory.Should().BeNull();
	}

	[Fact]
	public void BuiltInPluginIds_AreThePluginsOwnIds()
	{
		using ServiceProvider provider = Services(s => s.AddMokaDocsPlugins());

		provider.GetServices<IMokaPlugin>().Select(p => p.Id).Should().Equal(BuiltInPlugins.Ids);
	}

	[Fact]
	public void AddMokaDocsPlugins_WithBuiltInPlugins_RegistersOnlyThoseAndLeavesOtherIdsAlone()
	{
		using ServiceProvider provider = Services(s =>
			s.AddMokaDocsPlugins(o => o.BuiltInPlugins = ["OPENAPI", "not-a-built-in", "mokadocs-python-api"]));

		provider.GetServices<IMokaPlugin>().Select(p => p.Id).Should().Equal("openapi", "mokadocs-python-api");
		provider.GetRequiredService<PluginHost>().Should().NotBeNull();
	}

	[Fact]
	public void AddMokaDocsPlugins_CalledTwice_RegistersEachPluginAndTheHostOnce()
	{
		using ServiceProvider provider = Services(s => s
			.AddMokaDocsPlugins(o => o.BuiltInPlugins = ["openapi"])
			.AddMokaDocsPlugins());

		provider.GetServices<IMokaPlugin>().Select(p => p.Id).Should().BeEquivalentTo(BuiltInPlugins.Ids);
		provider.GetServices<PluginHost>().Should().ContainSingle();
	}

	private static ServiceProvider Services(Action<IServiceCollection> register)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(_config);
		register(services);
		return services.BuildServiceProvider();
	}
}
