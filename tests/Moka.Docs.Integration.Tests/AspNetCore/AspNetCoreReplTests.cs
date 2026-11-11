using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Docs.AspNetCore;
using Moka.Docs.Cli.Commands;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     The REPL endpoint in the ASP.NET Core host. EnableRepl added Run buttons that posted to an
///     endpoint this host never mapped, at a URL that ignored the base path.
/// </summary>
/// <remarks>
///     Every test that depends on <see cref="MokaDocsReplWorker.IsEntryPointWired" />, a process-wide
///     flag, lives in this class, so they never run at the same time.
/// </remarks>
public sealed class AspNetCoreReplTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-repl-host-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreReplTests()
	{
		Directory.CreateDirectory(Path.Combine(_root, "docs"));
		File.WriteAllText(Path.Combine(_root, "docs", "index.md"),
			"---\ntitle: Home\n---\n\n```csharp-repl\nConsole.WriteLine(1 + 1);\n```\n");
		MokaDocsReplWorker.IsEntryPointWired = false;
	}

	private static CancellationToken Ct => TestContext.Current.CancellationToken;

	public void Dispose()
	{
		MokaDocsReplWorker.IsEntryPointWired = false;
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
	public async Task ReplScript_PostsUnderTheBasePath()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IHostEnvironment>(new ContentRoot(_root));
		services.AddMokaDocs(options =>
		{
			options.Assemblies = [FixtureAssembly.Instance];
			options.DocsPath = "docs";
			options.BasePath = "/handbook";
			options.EnableRepl = true;
		});
		await using ServiceProvider provider = services.BuildServiceProvider();

		InMemorySite site = await provider.GetRequiredService<MokaDocsService>().GetSiteAsync(Ct);
		string html = Encoding.UTF8.GetString(site.Files["index.html"].Content);

		html.Should().Contain("data-base-path=\"/handbook\"");
		html.Should().Contain("fetch((document.documentElement.dataset.basePath || '') + '/api/repl/execute'");
		html.Should().NotContain("fetch('/api/repl/execute'");
		html.Should().NotContain("mokadocs serve", "the message must not send a reader of an app's docs to the CLI");
	}

	[Fact]
	public async Task Repl_WithAWorker_RunsSnippetsAgainstTheDocumentedTypes_AndStopsItWithTheApp()
	{
		DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);
		int? workerId;
		try
		{
			HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute",
				Json(new { code = "Console.Write(typeof(Fixture.Shapes.Shape).Assembly.GetName().Name);" }));

			response.StatusCode.Should().Be(HttpStatusCode.OK);
			JsonElement result = await ReadJsonAsync(response);
			result.GetProperty("error").GetString().Should().BeEmpty();
			result.GetProperty("output").GetString().Should().Be("Moka.Docs.ReflectionFixture");
			workerId = host.App.Services.GetRequiredService<MokaDocsRepl>().Service.WorkerProcessId;
			workerId.Should().NotBeNull();
		}
		finally
		{
			await host.DisposeAsync();
		}

		HasExited(workerId!.Value).Should().BeTrue("the worker is disposed with the app");
	}

	[Fact]
	public async Task Repl_AReaderWhoLeavesMidRun_DoesNotBreakTheNextRun()
	{
		// Cancelling the run with the request left the worker's answer unread, so the next run
		// read it, or failed on the pipe read still in progress.
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);
		(await host.PostAsync("/docs/api/repl/execute", Json(new { code = "1" }))).StatusCode.Should().Be(HttpStatusCode.OK);

		using (var leave = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
		{
			Func<Task> abandoned = () => host.PostAsync("/docs/api/repl/execute",
				Json(new { code = "System.Threading.Thread.Sleep(1500); Console.Write(\"first\");" }),
				cancellationToken: leave.Token);
			await abandoned.Should().ThrowAsync<OperationCanceledException>();
		}

		HttpResponseMessage next = await host.PostAsync("/docs/api/repl/execute",
			Json(new { code = "Console.Write(\"second\");" }));

		next.StatusCode.Should().Be(HttpStatusCode.OK);
		(await ReadJsonAsync(next)).GetProperty("output").GetString().Should().Be("second");
	}

	[Fact]
	public async Task Repl_WithoutTheWorkerLine_AnswersWithTheFixAndStartsNothing()
	{
		// Starting the app's executable again without the check in Program.cs would start the whole web app twice.
		await using DocsTestHost host = await StartAsync("Development");

		HttpResponseMessage first = await host.PostAsync("/docs/api/repl/execute", Json(new { code = "1" }));
		HttpResponseMessage second = await host.PostAsync("/docs/api/repl/execute", Json(new { code = "2" }));

		first.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
		second.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
		(await ReadJsonAsync(first)).GetProperty("error").GetString()
			.Should().Contain("if (await MokaDocsReplWorker.TryRunAsync()) return;");
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
		host.Logs.Messages(LogLevel.Warning).Where(m => m.Contains("MokaDocsReplWorker.TryRunAsync()"))
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Repl_OutsideDevelopment_IsNotMappedAndSaysWhy()
	{
		await using DocsTestHost host = await StartAsync("Production", options => options.ReplWorkerStartInfo = CliWorker);

		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute", Json(new { code = "1" }));

		response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
		host.Logs.Messages(LogLevel.Warning).Should().Contain(m => m.Contains("not mapped outside the Development"));
	}

	[Fact]
	public async Task Repl_OutsideDevelopment_WithTheOptIn_IsMapped()
	{
		await using DocsTestHost host = await StartAsync("Production", options =>
		{
			options.ReplWorkerStartInfo = CliWorker;
			options.AllowRemoteRepl = true;
		});

		// Invalid JSON gets past the endpoint's checks without starting a worker.
		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute", "not json");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData(false, HttpStatusCode.Forbidden)]
	[InlineData(true, HttpStatusCode.BadRequest)]
	public async Task Repl_FromAnotherMachine_RunsOnlyWithTheOptIn(bool allowRemote, HttpStatusCode status)
	{
		// The origin checks stop other web pages, not other machines: a Development app listening
		// on every interface ran code for anyone on the network.
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.EnableRepl = true;
			options.BuildOnStartup = false;
			options.ReplWorkerStartInfo = CliWorker;
			options.AllowRemoteRepl = allowRemote;
		}, app =>
		{
			app.Use((context, next) =>
			{
				context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");
				return next(context);
			});
			app.MapMokaDocs();
		}, "Development");

		// A refused run is turned away before the body is read; an accepted one fails on the
		// invalid JSON, before any worker starts.
		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute",
			allowRemote ? "not json" : Json(new { code = "1" }));

		response.StatusCode.Should().Be(status);
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
	}

	[Theory]
	[InlineData(false, HttpStatusCode.Forbidden)]
	[InlineData(true, HttpStatusCode.BadRequest)]
	public async Task Repl_AHostNameReboundToLoopback_RunsOnlyWithTheOptIn(bool allowRemote, HttpStatusCode status)
	{
		// DNS rebinding: a page on attacker.example whose name now resolves to 127.0.0.1 sends Host and
		// Origin as attacker.example, so the origin check passed and the caller was loopback.
		await using DocsTestHost host = await StartAsync("Development", options =>
		{
			options.ReplWorkerStartInfo = CliWorker;
			options.AllowRemoteRepl = allowRemote;
		});
		string attacker = $"attacker.example:{host.Client.BaseAddress!.Port}";

		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute",
			allowRemote ? "not json" : Json(new { code = "1" }), origin: "http://" + attacker,
			configure: request => request.Headers.Host = attacker);

		response.StatusCode.Should().Be(status);
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
	}

	[Theory]
	[InlineData("Forwarded", "for=203.0.113.5")]
	[InlineData("X-Forwarded-For", "203.0.113.5")]
	[InlineData("X-Forwarded-Host", "docs.example.com")]
	public async Task Repl_ThroughAProxy_IsRefused(string header, string value)
	{
		// nginx, ngrok or a dev tunnel on the same machine makes every caller loopback.
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);

		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute", Json(new { code = "1" }),
			configure: request => request.Headers.TryAddWithoutValidation(header, value));

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		host.Logs.Messages(LogLevel.Warning).Should().Contain(m => m.Contains(header) && m.Contains("AllowRemoteRepl"));
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
	}

	[Fact]
	public async Task Repl_WithALocalhostHostHeader_IsAccepted()
	{
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);
		string localhost = $"localhost:{host.Client.BaseAddress!.Port}";

		// An accepted request fails on the invalid JSON, before any worker starts.
		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute", "not json",
			origin: "http://" + localhost, configure: request => request.Headers.Host = localhost);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData("localhost", true)]
	[InlineData("localhost:5340", true)]
	[InlineData("LOCALHOST.:5340", true)]
	[InlineData("docs.localhost:5340", true)]
	[InlineData("127.0.0.1:5340", true)]
	[InlineData("127.8.9.10", true)]
	[InlineData("[::1]:5340", true)]
	[InlineData("attacker.example:5340", false)]
	[InlineData("localhost.attacker.example", false)]
	[InlineData("10.0.0.5:5340", false)]
	[InlineData("[2001:db8::1]:5340", false)]
	[InlineData("", false)]
	public void IsLoopbackHost_AcceptsLocalhostNamesAndLoopbackAddressesOnly(string host, bool loopback)
	{
		MokaDocsApiEndpoints.IsLoopbackHost(new HostString(host)).Should().Be(loopback);
	}

	[Fact]
	public async Task Repl_PastTheQueueLimit_Answers429()
	{
		// Runs queued without limit, so a script could hold any number of requests open.
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);
		(await host.PostAsync("/docs/api/repl/execute", Json(new { code = "0" }))).StatusCode.Should().Be(HttpStatusCode.OK);

		Task<HttpResponseMessage> running = host.PostAsync("/docs/api/repl/execute",
			Json(new { code = "System.Threading.Thread.Sleep(3000);" }));
		await WaitUntilAsync(() => Pending(host) == 1);
		Task<HttpResponseMessage>[] waiting = [.. Enumerable.Range(0, 4)
			.Select(i => host.PostAsync("/docs/api/repl/execute", Json(new { code = $"{i}" })))];
		await WaitUntilAsync(() => Pending(host) == 5);

		HttpResponseMessage refused = await host.PostAsync("/docs/api/repl/execute", Json(new { code = "1" }));

		refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
		(await ReadJsonAsync(refused)).GetProperty("error").GetString().Should().Contain("Too many snippets");
		(await running).StatusCode.Should().Be(HttpStatusCode.OK);
		foreach (Task<HttpResponseMessage> queued in waiting)
		{
			(await queued).StatusCode.Should().Be(HttpStatusCode.OK);
		}
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("127.0.0.1", true)]
	[InlineData("::1", true)]
	[InlineData("::ffff:127.0.0.1", true)]
	[InlineData("10.0.0.5", false)]
	[InlineData("::ffff:10.0.0.5", false)]
	[InlineData("2001:db8::1", false)]
	public void IsLocalCaller_AcceptsLoopbackOnly(string? address, bool local)
	{
		MokaDocsApiEndpoints.IsLocalCaller(address is null ? null : IPAddress.Parse(address)).Should().Be(local);
	}

	[Theory]
	[InlineData("text/plain", null, null, 403)]
	[InlineData("application/json", "https://evil.example", null, 403)]
	[InlineData("application/json", null, "cross-site", 403)]
	[InlineData("application/json", "OTHER-PORT", "same-origin", 403)]
	[InlineData("application/json", "", "same-origin", 400)]
	[InlineData("application/json", null, null, 400)]
	public async Task Repl_OnlyFromTheAppsOwnPages(string contentType, string? origin, string? fetchSite, int status)
	{
		// A page on another site could POST code here as text/plain without a CORS preflight.
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);
		if (origin == "OTHER-PORT")
		{
			origin = $"http://127.0.0.1:{host.Client.BaseAddress!.Port + 1}";
		}

		// An accepted request fails on the invalid JSON, before any worker starts.
		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute", "not json", contentType, origin,
			fetchSite);

		((int)response.StatusCode).Should().Be(status);
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
	}

	[Fact]
	public async Task Repl_OversizedBody_IsRejected()
	{
		await using DocsTestHost host = await StartAsync("Development", options => options.ReplWorkerStartInfo = CliWorker);

		HttpResponseMessage response = await host.PostAsync("/docs/api/repl/execute",
			Json(new { code = new string('x', 70_000) }));

		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
		host.App.Services.GetRequiredService<MokaDocsRepl>().IsServiceCreated.Should().BeFalse();
	}

	[Fact]
	public async Task TryRunAsync_InTheWebApp_ReturnsFalseAndMarksTheEntryPointWired()
	{
		Environment.GetEnvironmentVariable(MokaDocsReplWorker.WorkerVariable).Should().BeNull();

		bool ranAsWorker = await MokaDocsReplWorker.TryRunAsync(Ct);

		ranAsWorker.Should().BeFalse();
		MokaDocsReplWorker.IsEntryPointWired.Should().BeTrue();
	}

	// Environment.ProcessPath is a native path. Only Windows splits "C:\apps\x.exe" into a folder and
	// a file name; "/" separates on every OS.
	public static TheoryData<string, string?, string, bool> WorkerStartInfoCases()
	{
		var cases = new TheoryData<string, string?, string, bool>
		{
			{ "/usr/share/dotnet/dotnet", null, "/usr/share/dotnet/dotnet", true },
			{ "/apps/shop/Contoso.Shop", null, "/apps/shop/Contoso.Shop", false },
			{ "/opt/tests/testhost", "/usr/share/dotnet/dotnet", "/usr/share/dotnet/dotnet", true },
			{ "/opt/tests/testhost", null, "dotnet", true }
		};

		if (OperatingSystem.IsWindows())
		{
			cases.Add(@"C:\Program Files\dotnet\dotnet.exe", null, @"C:\Program Files\dotnet\dotnet.exe", true);
			cases.Add(@"C:\apps\shop\Contoso.Shop.exe", null, @"C:\apps\shop\Contoso.Shop.exe", false);
			cases.Add(@"C:\Windows\System32\inetsrv\w3wp.exe", @"C:\dotnet\dotnet.exe", @"C:\dotnet\dotnet.exe", true);
			cases.Add(@"C:\Windows\System32\inetsrv\w3wp.exe", null, "dotnet", true);
		}

		return cases;
	}

	[Theory]
	[MemberData(nameof(WorkerStartInfoCases))]
	public void WorkerStartInfo_StartsThisAppAgain(string processPath, string? dotnetHostPath, string fileName,
		bool passesTheAssembly)
	{
		string assembly = Path.Combine("apps", "shop", "Contoso.Shop.dll");

		ProcessStartInfo startInfo = MokaDocsReplWorker.CreateStartInfo(processPath, assembly, dotnetHostPath);

		startInfo.FileName.Should().Be(fileName);
		startInfo.ArgumentList.Should().Equal(passesTheAssembly ? new[] { assembly } : Array.Empty<string>());
	}

	[Fact]
	public void WorkerStartInfo_ForASingleFileApp_StartsItsOwnExecutable()
	{
		// A single-file app's assembly has no location, which made the REPL refuse to start.
		ProcessStartInfo startInfo = MokaDocsReplWorker.CreateStartInfo("/apps/shop/Contoso.Shop", "", null, "Contoso.Shop");

		startInfo.FileName.Should().Be("/apps/shop/Contoso.Shop");
		startInfo.ArgumentList.Should().BeEmpty();
	}

	[Fact]
	public void WorkerStartInfo_WithoutAnAssemblyLocation_Throws()
	{
		Action create = () => MokaDocsReplWorker.CreateStartInfo("w3wp.exe", null, null);

		create.Should().Throw<InvalidOperationException>().WithMessage("*ReplWorkerStartInfo*");
	}

	[Fact]
	public void WorkerStartInfo_SetsTheWorkerVariablesOnTheChildOnly()
	{
		var options = new MokaDocsOptions { ReplWorkerStartInfo = () => new ProcessStartInfo("worker") };
		using var repl = new MokaDocsRepl(options, LoggerFactory.Create(_ => { }));

		ProcessStartInfo startInfo = repl.CreateWorkerStartInfo();

		startInfo.Environment[MokaDocsReplWorker.WorkerVariable].Should().Be("1");
		startInfo.Environment[MokaDocsReplWorker.ParentProcessIdVariable].Should().Be(Environment.ProcessId.ToString());
		Environment.GetEnvironmentVariable(MokaDocsReplWorker.WorkerVariable).Should().BeNull();
	}

	/// <summary>The CLI copied into the test output, started the way serve starts its worker.</summary>
	private static ProcessStartInfo CliWorker() =>
		ServeCommand.CreateReplWorkerStartInfo(
			Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet",
			Path.Combine(AppContext.BaseDirectory, "mokadocs.dll"),
			Environment.ProcessId);

	private Task<DocsTestHost> StartAsync(string environment, Action<MokaDocsOptions>? configure = null) =>
		DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			options.EnableRepl = true;
			options.BuildOnStartup = false;
			configure?.Invoke(options);
		}, environment: environment);

	private static string Json(object value) => JsonSerializer.Serialize(value);

	private static int Pending(DocsTestHost host) =>
		host.App.Services.GetRequiredService<MokaDocsRepl>() is { IsServiceCreated: true } repl
			? repl.Service.PendingRuns
			: 0;

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		DateTime until = DateTime.UtcNow + TimeSpan.FromSeconds(30);
		while (!condition())
		{
			DateTime.UtcNow.Should().BeBefore(until, "the condition should hold within 30 seconds");
			await Task.Delay(20, Ct);
		}
	}

	private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
		JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement;

	private static bool HasExited(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			return process.HasExited;
		}
		catch (ArgumentException)
		{
			return true;
		}
	}

	private sealed class ContentRoot(string contentRoot) : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Production;
		public string ApplicationName { get; set; } = "Moka.Docs.Integration.Tests";
		public string ContentRootPath { get; set; } = contentRoot;
		public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
			new Microsoft.Extensions.FileProviders.NullFileProvider();
	}
}
