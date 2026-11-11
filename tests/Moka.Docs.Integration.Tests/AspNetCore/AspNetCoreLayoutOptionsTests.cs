using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Configuration;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     <c>MokaDocsOptions.Layout</c>, the host's counterpart of <c>theme.options.layout</c>.
/// </summary>
public sealed class AspNetCoreLayoutOptionsTests
{
	[Fact]
	public void Layout_DefaultsToTheDefaultVariantOfEveryPart()
	{
		var options = new MokaDocsOptions();

		SiteConfigFactory.Create(options).Theme.Options.Layout.Should().Be(new ThemeLayoutOptions());
		new[]
			{
				options.Layout.Width, options.Layout.Navbar, options.Layout.Footer, options.Layout.Toc,
				options.Layout.Breadcrumbs, options.Layout.PageNav
			}
			.Should().AllBe("default");
	}

	[Fact]
	public void Layout_ReachesTheSiteConfigAsWritten()
	{
		var options = new MokaDocsOptions
		{
			Layout = new MokaDocsLayoutOptions
			{
				Width = "full",
				Navbar = "Floating",
				Footer = "none",
				Toc = "inline",
				Breadcrumbs = "path",
				PageNav = "stacked"
			}
		};

		SiteConfigFactory.Create(options).Theme.Options.Layout.Should().Be(new ThemeLayoutOptions
		{
			Width = "full",
			Navbar = "Floating",
			Footer = "none",
			Toc = "inline",
			Breadcrumbs = "path",
			PageNav = "stacked"
		});
	}

	[Fact]
	public async Task Layout_UnknownVariant_IsReportedByTheHostBuild()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.Layout.Footer = "fat";
		});

		await using ServiceProvider provider = services.BuildServiceProvider();
		InMemorySite site = await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(TestContext.Current.CancellationToken);

		site.Diagnostics.Should().ContainSingle(d => d.Source == "Render")
			.Which.Message.Should().Be("Layout footer 'fat' is not one of default, minimal, columns, none; using 'default'");
	}
}
