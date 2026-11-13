using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moka.Docs.AspNetCore;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     Requests against <c>MapMokaDocs()</c> in a running app: caching headers, HEAD, the root
///     base path, route group conventions and the feedback endpoint.
/// </summary>
public sealed class AspNetCoreEndpointTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-endpoints-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreEndpointTests()
	{
		Directory.CreateDirectory(_root);
		Write("docs/index.md", "---\ntitle: Home\n---\n\nThe home page.\n");
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
	public async Task Page_HasAStrongETag_AndAMatchingIfNoneMatchGets304()
	{
		// Every request sent the whole page again; nothing let a browser revalidate.
		await using DocsTestHost host = await StartAsync();

		HttpResponseMessage first = await host.GetAsync("/docs/api");
		first.StatusCode.Should().Be(HttpStatusCode.OK);
		first.Headers.ETag.Should().NotBeNull();
		first.Headers.ETag!.IsWeak.Should().BeFalse();
		first.Headers.CacheControl!.NoCache.Should().BeTrue();
		string etag = first.Headers.ETag.Tag;

		HttpResponseMessage revalidated = await host.GetAsync("/docs/api", etag);
		revalidated.StatusCode.Should().Be(HttpStatusCode.NotModified);
		revalidated.Headers.ETag!.Tag.Should().Be(etag);
		(await revalidated.Content.ReadAsByteArrayAsync(Ct)).Should().BeEmpty();

		(await host.GetAsync("/docs/api", "W/" + etag)).StatusCode.Should().Be(HttpStatusCode.NotModified);
		(await host.GetAsync("/docs/api", "\"other\", " + etag)).StatusCode.Should().Be(HttpStatusCode.NotModified);
		(await host.GetAsync("/docs/api", "\"other\"")).StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ETag_IsTheSameAcrossRebuildsOfTheSameContent()
	{
		await using DocsTestHost host = await StartAsync(options => options.CacheOutput = false);

		HttpResponseMessage first = await host.GetAsync("/docs/api");
		HttpResponseMessage second = await host.GetAsync("/docs/api", first.Headers.ETag!.Tag);

		second.StatusCode.Should().Be(HttpStatusCode.NotModified);
	}

	[Fact]
	public async Task StaticAsset_IsRevalidatedWithItsETag()
	{
		// Theme CSS and scripts have no hash in their URLs. With a day of max-age, readers got new
		// pages with the old theme for up to 24 hours after an upgrade.
		await using DocsTestHost host = await StartAsync();
		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		string css = site.Files.Keys.First(key => key.EndsWith(".css", StringComparison.Ordinal));

		HttpResponseMessage response = await host.GetAsync("/docs/" + css);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Headers.CacheControl!.NoCache.Should().BeTrue();
		response.Headers.CacheControl.MaxAge.Should().BeNull();
		response.Headers.ETag!.Tag.Should().Be(site.Files[css].ETag);
		(await host.GetAsync("/docs/" + css, site.Files[css].ETag)).StatusCode.Should().Be(HttpStatusCode.NotModified);
	}

	[Theory]
	[InlineData(".pdf", "application/pdf")]
	[InlineData(".mp4", "video/mp4")]
	[InlineData(".webm", "video/webm")]
	[InlineData(".avif", "image/avif")]
	[InlineData(".webmanifest", "application/manifest+json")]
	[InlineData(".zip", "application/zip")]
	[InlineData(".wasm", "application/wasm")]
	[InlineData(".css", "text/css; charset=utf-8")]
	[InlineData(".JS", "application/javascript; charset=utf-8")]
	[InlineData(".unknown-type", "application/octet-stream")]
	public void SiteFile_ContentType_CoversTheAssetTypesTheSitePublishes(string extension, string contentType)
	{
		// These went out as application/octet-stream, so browsers downloaded a PDF or video instead of showing it.
		SiteFile.GetContentType(extension).Should().Be(contentType);
	}

	[Fact]
	public void SiteFile_With_GetsANewETag()
	{
		// The tag is computed once; a copy with other content must not keep the old one.
		var file = new SiteFile("one"u8.ToArray(), "text/plain");

		SiteFile changed = file with { Content = "two"u8.ToArray() };

		changed.ETag.Should().NotBe(file.ETag);
		changed.ETag.Should().Be(new SiteFile("two"u8.ToArray(), "text/plain").ETag);
	}

	[Fact]
	public async Task Head_ReturnsTheHeadersWithoutABody()
	{
		await using DocsTestHost host = await StartAsync();
		byte[] page = await (await host.GetAsync("/docs/api")).Content.ReadAsByteArrayAsync(Ct);

		HttpResponseMessage head = await host.Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/docs/api"), Ct);

		head.StatusCode.Should().Be(HttpStatusCode.OK);
		head.Content.Headers.ContentLength.Should().Be(page.Length);
		head.Headers.ETag.Should().NotBeNull();
		(await head.Content.ReadAsByteArrayAsync(Ct)).Should().BeEmpty();
	}

	[Fact]
	public async Task OtherMethods_OnPages_AreNotAllowed()
	{
		// Map() answered every method, so a POST or DELETE to any docs URL got the page.
		await using DocsTestHost host = await StartAsync();

		(await host.PostAsync("/docs/api", "{}")).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
		(await host.Client.DeleteAsync("/docs/api", Ct)).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
	}

	[Theory]
	[InlineData("/", null)]
	[InlineData(null, "/")]
	[InlineData(null, "")]
	public async Task RootBasePath_ServesTheSiteAtTheRoot(string? mapPath, string? optionsPath)
	{
		// A root base path produced the route "//{**path}", which matched nothing.
		await using DocsTestHost host = await StartAsync(
			options =>
			{
				if (optionsPath is not null)
				{
					options.BasePath = optionsPath;
				}
			},
			app => app.MapMokaDocs(mapPath));

		(await host.GetTextAsync("/")).Should().Contain("The home page.");
		string api = await host.GetTextAsync("/api");
		api.Should().Contain("href=\"/api/fixture/shapes/money\"").And.NotContain("href=\"//");
		(await host.PostAsync("/api/feedback", """{"page":"/","helpful":true}""")).StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task InsideAnOuterGroup_TheDocsAreServedUnderItsPrefix()
	{
		await using DocsTestHost host = await StartAsync(map: app => app.MapGroup("/portal").MapMokaDocs());

		(await host.GetTextAsync("/portal/docs/api")).Should().Contain("Fixture.Shapes");
	}

	[Theory]
	[InlineData("GET", "/docs/api")]
	[InlineData("GET", "/docs/search-index.json")]
	[InlineData("POST", "/docs/api/feedback")]
	public async Task MapMokaDocs_ReturnsTheGroup_SoRequireAuthorizationCoversEveryRoute(string method, string path)
	{
		// It returned the IEndpointRouteBuilder it was given, so RequireAuthorization could not chain.
		await using DocsTestHost host = await StartAsync(
			map: app =>
			{
				app.UseAuthentication();
				app.UseAuthorization();
				app.MapMokaDocs().RequireAuthorization("Admins");
			},
			register: AddHeaderAuthentication);

		(await SendAsync(host, method, path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		(await SendAsync(host, method, path, "reader")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
		(await SendAsync(host, method, path, "ada", "admin")).StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task OuterGroupConventions_StillApply()
	{
		await using DocsTestHost host = await StartAsync(
			map: app =>
			{
				app.UseAuthentication();
				app.UseAuthorization();
				app.MapGroup("").RequireAuthorization().MapMokaDocs();
			},
			register: AddHeaderAuthentication);

		(await SendAsync(host, "GET", "/docs/api")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		(await SendAsync(host, "GET", "/docs/api", "reader")).StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Feedback_IsLoggedAndPassedToTheCallback()
	{
		// The widget is on by default and posted to an endpoint this host never mapped, so every vote got a 404.
		var votes = new List<(DocsFeedback Feedback, string Path)>();
		await using DocsTestHost host = await StartAsync(options => options.OnFeedback = (feedback, context) =>
		{
			lock (votes)
			{
				votes.Add((feedback, context.Request.Path.Value!));
			}

			return Task.CompletedTask;
		});

		HttpResponseMessage response = await host.PostAsync("/docs/api/feedback", """{"page":"/docs/api","helpful":false}""");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		(await response.Content.ReadAsStringAsync(Ct)).Should().Be("""{"ok":true}""");
		votes.Should().ContainSingle().Which.Should().Be((new DocsFeedback("/docs/api", false), "/docs/api/feedback"));
		host.Logs.Messages(LogLevel.Information).Should().Contain("Docs feedback for /docs/api: not helpful");
	}

	[Fact]
	public async Task Feedback_Answer_IsJsonWithItsLength()
	{
		// The answers come from the code the dev server uses, which sets Content-Length instead of
		// streaming the JSON.
		await using DocsTestHost host = await StartAsync();

		HttpResponseMessage response = await host.PostAsync("/docs/api/feedback", """{"page":"/docs/api","helpful":true}""");

		response.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
		response.Content.Headers.ContentLength.Should().Be("""{"ok":true}""".Length);
		response.Headers.TransferEncodingChunked.Should().NotBe(true);
		(await response.Content.ReadAsStringAsync(Ct)).Should().Be("""{"ok":true}""");
	}

	[Fact]
	public async Task Feedback_TurnedOff_HidesTheWidgetAndMapsNoEndpoint()
	{
		bool called = false;
		await using DocsTestHost off = await StartAsync(options =>
		{
			options.ShowFeedback = false;
			options.OnFeedback = (_, _) =>
			{
				called = true;
				return Task.CompletedTask;
			};
		});
		await using DocsTestHost on = await StartAsync();

		(await off.PostAsync("/docs/api/feedback", """{"page":"/docs/api","helpful":true}"""))
			.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
		called.Should().BeFalse();
		(await off.GetTextAsync("/docs/api")).Should().NotContain("id=\"feedbackWidget\"");
		(await on.GetTextAsync("/docs/api")).Should().Contain("id=\"feedbackWidget\"");
	}

	[Theory]
	[InlineData(413, "{\"page\":\"/docs/api\",\"helpful\":true,\"pad\":\"PAD\"}")]
	[InlineData(400, "{\"page\":\"LONG\",\"helpful\":true}")]
	[InlineData(400, "{\"page\":\"/docs/api\"}")]
	[InlineData(400, "{\"page\":\"\",\"helpful\":true}")]
	[InlineData(400, "{\"page\":\"/docs/a\\nFAKE LOG LINE\",\"helpful\":true}")]
	[InlineData(400, "not json")]
	public async Task Feedback_OversizedOrInvalid_IsRejected(int status, string body)
	{
		int calls = 0;
		await using DocsTestHost host = await StartAsync(options => options.OnFeedback = (_, _) =>
		{
			Interlocked.Increment(ref calls);
			return Task.CompletedTask;
		});
		body = body.Replace("PAD", new string('x', 5000)).Replace("LONG", "/" + new string('a', 1100));

		HttpResponseMessage response = await host.PostAsync("/docs/api/feedback", body);

		((int)response.StatusCode).Should().Be(status);
		calls.Should().Be(0);
	}

	[Theory]
	[InlineData("text/plain", null, null, 403)] // the POST a cross-origin page can send without a preflight
	[InlineData("application/x-www-form-urlencoded", null, null, 403)]
	[InlineData("application/json", "https://evil.example", null, 403)]
	[InlineData("application/json", null, "cross-site", 403)]
	[InlineData("application/json", null, "same-site", 403)]
	[InlineData("application/json", "OTHER-PORT", "same-origin", 403)]
	[InlineData("application/json", "", "same-origin", 200)] // the docs' own page
	[InlineData("application/json", null, null, 200)] // curl
	public async Task Feedback_OnlyFromTheAppsOwnPages(string contentType, string? origin, string? fetchSite, int status)
	{
		int calls = 0;
		await using DocsTestHost host = await StartAsync(options => options.OnFeedback = (_, _) =>
		{
			Interlocked.Increment(ref calls);
			return Task.CompletedTask;
		});
		if (origin == "OTHER-PORT")
		{
			origin = $"http://127.0.0.1:{host.Client.BaseAddress!.Port + 1}";
		}

		HttpResponseMessage response = await host.PostAsync("/docs/api/feedback", """{"page":"/docs/api","helpful":true}""",
			contentType, origin, fetchSite);

		((int)response.StatusCode).Should().Be(status);
		calls.Should().Be(status == 200 ? 1 : 0);
	}

	[Fact]
	public async Task PathBase_StartsEveryLink_AndARequestWithAnotherPathBaseIsLoggedOnce()
	{
		// Links were built from BasePath alone, so behind UsePathBase, an IIS sub-application or
		// X-Forwarded-Prefix every style, script and link of the docs 404ed.
		await using DocsTestHost host = await StartAsync(options => options.PathBase = "/shop/", app =>
		{
			app.UsePathBase("/shop");
			app.UseRouting();
			app.MapMokaDocs();
		});

		string page = await host.GetTextAsync("/shop/docs/api");
		page.Should().Contain("href=\"/shop/docs/api/fixture/shapes/money\"").And.Contain("data-base-path=\"/shop/docs\"");
		Match css = Regex.Match(page, "href=\"(/shop/docs/_theme/css/main\\.css\\?v=[0-9a-f]{10})\"");
		css.Success.Should().BeTrue("the stylesheet's URL should start with the path base");
		(await host.GetAsync(css.Groups[1].Value)).StatusCode.Should().Be(HttpStatusCode.OK);
		host.Logs.Messages(LogLevel.Warning).Should().NotContain(m => m.Contains("path base"));

		(await host.GetTextAsync("/docs/api")).Should().Contain("href=\"/shop/docs/api/");
		(await host.GetTextAsync("/docs/search-index.json")).Should().NotBeEmpty();

		host.Logs.Messages(LogLevel.Warning).Where(m => m.Contains("path base")).Should().ContainSingle()
			.Which.Should().Contain("path base \"\"").And.Contain("\"/shop\"").And.Contain("MokaDocsOptions.PathBase");
	}

	[Fact]
	public async Task PathBase_NotSet_BehindUsePathBase_LogsWhatToSet()
	{
		await using DocsTestHost host = await StartAsync(map: app =>
		{
			app.UsePathBase("/shop");
			app.UseRouting();
			app.MapMokaDocs();
		});

		(await host.GetTextAsync("/shop/docs/api")).Should().Contain("href=\"/docs/api/fixture/shapes/money\"");

		host.Logs.Messages(LogLevel.Warning).Should().ContainSingle(m => m.Contains("path base \"/shop\""));
	}

	[Theory]
	[InlineData(null, "/docs", "/docs")]
	[InlineData("", "/docs", "/docs")]
	[InlineData("/", "/docs", "/docs")]
	[InlineData("/shop", "/docs", "/shop/docs")]
	[InlineData("shop/", "docs/", "/shop/docs")]
	[InlineData("/shop/eu", "/docs", "/shop/eu/docs")]
	[InlineData("/shop", "/", "/shop")]
	[InlineData(null, "/", "/")]
	public void PathBase_AndBasePath_MakeTheBuildsBasePath(string? pathBase, string basePath, string expected)
	{
		var options = new MokaDocsOptions { PathBase = pathBase, BasePath = basePath };

		SiteConfigFactory.Create(options).Build.BasePath.Should().Be(expected);
	}

	[Fact]
	public async Task ImmutableFiles_AreCachedPrivately_ForSignedInReaders()
	{
		// A shared cache keeps a response for everyone, so docs behind sign-in stay in the reader's browser.
		await using DocsTestHost host = await StartAsync(
			map: app =>
			{
				app.UseAuthentication();
				app.UseAuthorization();
				app.MapMokaDocs().RequireAuthorization("Admins");
			},
			register: AddHeaderAuthentication);
		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		string url = "/docs/_theme/css/main.css?v=" + site.Files["_theme/css/main.css"].ContentHash[..10];

		HttpResponseMessage response = await SendAsync(host, "GET", url, "ada", "admin");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Headers.NonValidated["Cache-Control"].ToString().Should().Be("private, max-age=31536000, immutable");
	}

	[Fact]
	public async Task ConfigureFeedbackEndpoint_RateLimitsTheFeedbackEndpointAlone()
	{
		// MapMokaDocs returned only the group, so a rate limit on votes limited every page and asset too.
		await using DocsTestHost host = await StartAsync(
			options => options.ConfigureFeedbackEndpoint = endpoint => endpoint.RequireRateLimiting("votes"),
			app =>
			{
				app.UseRateLimiter();
				app.MapMokaDocs();
			},
			services => services.AddRateLimiter(limiter =>
			{
				limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
				limiter.AddFixedWindowLimiter("votes", window =>
				{
					window.PermitLimit = 1;
					window.Window = TimeSpan.FromHours(1);
				});
			}));
		const string vote = """{"page":"/docs/api","helpful":true}""";

		(await host.PostAsync("/docs/api/feedback", vote)).StatusCode.Should().Be(HttpStatusCode.OK);
		HttpResponseMessage limited = await host.PostAsync("/docs/api/feedback", vote);

		limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
		for (int i = 0; i < 3; i++)
		{
			(await host.GetAsync("/docs/api")).StatusCode.Should().Be(HttpStatusCode.OK);
		}
	}

	[Fact]
	public async Task ConfigureReplEndpoint_AuthorizesTheReplAlone()
	{
		IEndpointConventionBuilder? feedback = null;
		await using DocsTestHost host = await DocsTestHost.StartAsync(_root, options =>
			{
				options.DocsPath = "docs";
				options.EnableRepl = true;
				options.ReplWorkerStartInfo = () => new ProcessStartInfo("never-started");
				options.ConfigureReplEndpoint = endpoint => endpoint.RequireAuthorization("Admins");
				options.ConfigureFeedbackEndpoint = endpoint => feedback = endpoint;
			}, app =>
			{
				app.UseAuthentication();
				app.UseAuthorization();
				app.MapMokaDocs();
			}, "Development", AddHeaderAuthentication);

		// Invalid JSON is turned away before a worker starts, once authorization lets the request in.
		(await host.PostAsync("/docs/api/repl/execute", "not json")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		(await host.PostAsync("/docs/api/repl/execute", "not json",
				configure: request => request.Headers.Add(HeaderAuthHandler.UserHeader, "reader")))
			.StatusCode.Should().Be(HttpStatusCode.Forbidden);
		HttpResponseMessage admin = await host.PostAsync("/docs/api/repl/execute", "not json", configure: request =>
		{
			request.Headers.Add(HeaderAuthHandler.UserHeader, "ada");
			request.Headers.Add(HeaderAuthHandler.RoleHeader, "admin");
		});

		admin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		admin.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
		(await host.GetAsync("/docs/api")).StatusCode.Should().Be(HttpStatusCode.OK);
		(await host.PostAsync("/docs/api/feedback", """{"page":"/docs/api","helpful":true}""")).StatusCode
			.Should().Be(HttpStatusCode.OK);
		feedback.Should().NotBeNull();
	}

	private Task<DocsTestHost> StartAsync(Action<MokaDocsOptions>? configure = null, Action<WebApplication>? map = null,
		Action<IServiceCollection>? register = null) =>
		DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			configure?.Invoke(options);
		}, map, register: register);

	private static Task<HttpResponseMessage> SendAsync(DocsTestHost host, string method, string path, string? user = null,
		string? role = null)
	{
		void Identify(HttpRequestMessage request)
		{
			if (user is not null)
			{
				request.Headers.Add(HeaderAuthHandler.UserHeader, user);
			}

			if (role is not null)
			{
				request.Headers.Add(HeaderAuthHandler.RoleHeader, role);
			}
		}

		if (method == "POST")
		{
			return host.PostAsync(path, """{"page":"/docs/api","helpful":true}""", configure: Identify);
		}

		var request = new HttpRequestMessage(HttpMethod.Get, path);
		Identify(request);
		return host.Client.SendAsync(request, Ct);
	}

	private static void AddHeaderAuthentication(IServiceCollection services)
	{
		services.AddAuthentication(HeaderAuthHandler.SchemeName)
			.AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>(HeaderAuthHandler.SchemeName, null);
		services.AddAuthorization(options => options.AddPolicy("Admins", policy => policy.RequireRole("admin")));
	}

	private void Write(string relativePath, string content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
	}

	/// <summary>Signs in whoever the request's headers name.</summary>
	private sealed class HeaderAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		public const string SchemeName = "Header";
		public const string UserHeader = "X-Test-User";
		public const string RoleHeader = "X-Test-Role";

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (Request.Headers[UserHeader].ToString() is not { Length: > 0 } user)
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user)], SchemeName);
			if (Request.Headers[RoleHeader].ToString() is { Length: > 0 } role)
			{
				identity.AddClaim(new Claim(ClaimTypes.Role, role));
			}

			return Task.FromResult(AuthenticateResult.Success(
				new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
		}
	}
}
