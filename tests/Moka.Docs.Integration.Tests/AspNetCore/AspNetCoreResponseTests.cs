using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moka.Docs.AspNetCore;
using Moka.Docs.Core.Pipeline;
using Moka.Docs.Plugins;

namespace Moka.Docs.Integration.Tests.AspNetCore;

/// <summary>
///     What the host sends with each file: Brotli or gzip from <c>Accept-Encoding</c>, an ETag per
///     encoding, <c>Cache-Control</c> by URL shape and the preview host's security headers.
/// </summary>
public sealed class AspNetCoreResponseTests : IDisposable
{
	private const string _fingerprintedRuntime = "_framework/dotnet.native.3bptkeu1pi.wasm";
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-responses-" + Guid.NewGuid().ToString("N"));

	public AspNetCoreResponseTests()
	{
		Write("docs/index.md", "---\ntitle: Home\n---\n\nThe home page.\n");
		Write("docs/small.txt", "short");
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
	public async Task Page_IsServedInTheEncodingTheClientAccepts_WithAnETagForEachEncoding()
	{
		// The host kept .br and .gz forms of the preview host in memory and sent everything as it was.
		await using DocsTestHost host = await StartAsync();
		HttpResponseMessage identity = await host.GetAsync("/docs/api");
		byte[] page = await identity.Content.ReadAsByteArrayAsync(Ct);

		HttpResponseMessage brotli = await host.GetAsync("/docs/api", acceptEncoding: "gzip, deflate, br, zstd");
		HttpResponseMessage gzip = await host.GetAsync("/docs/api", acceptEncoding: "gzip");

		identity.Content.Headers.ContentEncoding.Should().BeEmpty();
		brotli.Content.Headers.ContentEncoding.Should().Equal("br");
		gzip.Content.Headers.ContentEncoding.Should().Equal("gzip");
		foreach (HttpResponseMessage response in (HttpResponseMessage[]) [identity, brotli, gzip])
		{
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			response.Headers.Vary.Should().Equal("Accept-Encoding");
			response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
			response.Headers.ETag!.IsWeak.Should().BeFalse();
		}

		new[] { identity, brotli, gzip }.Select(r => r.Headers.ETag!.Tag).Should().OnlyHaveUniqueItems();
		byte[] brotliBody = await brotli.Content.ReadAsByteArrayAsync(Ct);
		brotliBody.Length.Should().BeLessThan(page.Length / 2);
		brotli.Content.Headers.ContentLength.Should().Be(brotliBody.Length);
		Decompress(brotliBody, "br").Should().Equal(page);
		Decompress(await gzip.Content.ReadAsByteArrayAsync(Ct), "gzip").Should().Equal(page);

		// A tag names one encoding's bytes: it revalidates that encoding only.
		string brotliTag = brotli.Headers.ETag!.Tag;
		HttpResponseMessage notModified = await host.GetAsync("/docs/api", brotliTag, "br");
		notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
		notModified.Headers.ETag!.Tag.Should().Be(brotliTag);
		notModified.Headers.Vary.Should().Equal("Accept-Encoding");
		(await host.GetAsync("/docs/api", brotliTag, "gzip")).StatusCode.Should().Be(HttpStatusCode.OK);
		(await host.GetAsync("/docs/api", brotliTag)).StatusCode.Should().Be(HttpStatusCode.OK);
		(await host.GetAsync("/docs/api", identity.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.NotModified);
	}

	[Fact]
	public async Task Head_SendsTheHeadersOfTheNegotiatedEncoding()
	{
		await using DocsTestHost host = await StartAsync();
		HttpResponseMessage get = await host.GetAsync("/docs/api", acceptEncoding: "br");
		byte[] body = await get.Content.ReadAsByteArrayAsync(Ct);

		HttpResponseMessage head = await host.SendAsync(HttpMethod.Head, "/docs/api",
			request => request.Headers.AcceptEncoding.ParseAdd("br"));

		head.StatusCode.Should().Be(HttpStatusCode.OK);
		head.Content.Headers.ContentEncoding.Should().Equal("br");
		head.Content.Headers.ContentLength.Should().Be(body.Length);
		head.Headers.ETag!.Tag.Should().Be(get.Headers.ETag!.Tag);
		(await head.Content.ReadAsByteArrayAsync(Ct)).Should().BeEmpty();
	}

	[Fact]
	public async Task SmallFiles_AreSentAsTheyAre_WithoutVary()
	{
		await using DocsTestHost host = await StartAsync();

		HttpResponseMessage response = await host.GetAsync("/docs/small.txt", acceptEncoding: "br, gzip");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentEncoding.Should().BeEmpty();
		response.Headers.Vary.Should().BeEmpty();
		(await response.Content.ReadAsStringAsync(Ct)).Should().Be("short");
	}

	[Fact]
	public async Task NotFound_ServesThe404PageCompressed_AndRevalidated()
	{
		await using DocsTestHost host = await StartAsync();

		HttpResponseMessage response = await host.GetAsync("/docs/no-such-page", acceptEncoding: "br");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		response.Headers.CacheControl!.NoCache.Should().BeTrue();
		response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		if (site.Files.TryGetValue("404.html", out SiteFile? notFound))
		{
			response.Content.Headers.ContentEncoding.Should().Equal("br");
			Decompress(await response.Content.ReadAsByteArrayAsync(Ct), "br").Should().Equal(notFound.Content);
		}
	}

	[Theory]
	[InlineData("gzip, deflate, br, zstd", "br")]
	[InlineData("br", "br")]
	[InlineData("gzip", "gzip")]
	[InlineData("x-gzip", "gzip")]
	[InlineData("GZIP", "gzip")]
	[InlineData("*", "br")]
	[InlineData("br;q=0, gzip", "gzip")]
	[InlineData("br;q=0.4, gzip;q=0.5", "gzip")]
	[InlineData("gzip, *;q=0", "gzip")]
	[InlineData("gzip, identity;q=0", "gzip")]
	[InlineData("*;q=0", null)]
	[InlineData("br;q=0, gzip;q=0", null)]
	[InlineData("identity", null)]
	[InlineData("deflate", null)]
	[InlineData("br;q=0.5, identity", null)]
	[InlineData("", null)]
	[InlineData(null, null)]
	public void Negotiation_FollowsTheQualityValues_AndPrefersBrotliOnATie(string? acceptEncoding, string? expected)
	{
		SiteFile file = Compressible();

		SiteFileEncoding? selected = SiteCompression.Select(new StringValues(acceptEncoding), file);

		selected?.Name.Should().Be(expected);
		(selected is null).Should().Be(expected is null);
	}

	[Theory]
	[InlineData("text/html; charset=utf-8", 2048, true)]
	[InlineData("text/css; charset=utf-8", 2048, true)]
	[InlineData("application/javascript; charset=utf-8", 2048, true)]
	[InlineData("application/json; charset=utf-8", 2048, true)]
	[InlineData("application/xml", 2048, true)]
	[InlineData("image/svg+xml", 2048, true)]
	[InlineData("text/markdown", 2048, true)]
	[InlineData("application/wasm", 2048, true)]
	[InlineData("text/html; charset=utf-8", 1023, false)]
	[InlineData("image/png", 2048, false)]
	[InlineData("font/woff2", 2048, false)]
	[InlineData("application/zip", 2048, false)]
	[InlineData("video/mp4", 2048, false)]
	public void OnlyTextTypesOfAboutAKilobyte_AreCompressed(string contentType, int length, bool compressed)
	{
		SiteCompression.ShouldCompress(new SiteFile(new byte[length], contentType)).Should().Be(compressed);
	}

	[Fact]
	public void Compress_LeavesOutAnEncodingThatIsNotSmaller()
	{
		byte[] random = new byte[4096];
		new Random(42).NextBytes(random);

		SiteCompression.Compress(random).Should().BeEmpty();
		SiteCompression.Compress(Encoding.UTF8.GetBytes(new string('a', 4096))).Select(e => e.Name).Should().Equal("br", "gzip");
	}

	[Fact]
	public async Task ThemeFile_WithItsContentHashInTheUrl_IsCachedForAYear()
	{
		// Every response said no-cache, so each page view revalidated the theme's CSS and scripts.
		await using DocsTestHost host = await StartAsync();
		string page = await host.GetTextAsync("/docs/");
		Match css = Regex.Match(page, "href=\"(/docs/_theme/css/main\\.css)\\?v=([0-9a-f]{10})\"");
		css.Success.Should().BeTrue();

		HttpResponseMessage versioned = await host.GetAsync(css.Groups[1].Value + "?v=" + css.Groups[2].Value);
		HttpResponseMessage stale = await host.GetAsync(css.Groups[1].Value + "?v=0123456789");
		HttpResponseMessage bare = await host.GetAsync(css.Groups[1].Value);

		versioned.Headers.NonValidated["Cache-Control"].ToString().Should().Be("public, max-age=31536000, immutable");
		stale.Headers.NonValidated["Cache-Control"].ToString().Should().Be("no-cache");
		bare.Headers.NonValidated["Cache-Control"].ToString().Should().Be("no-cache");
		(await host.GetAsync("/docs/")).Headers.NonValidated["Cache-Control"].ToString().Should().Be("no-cache");
		(await host.GetAsync("/docs/search-index.json")).Headers.NonValidated["Cache-Control"].ToString().Should().Be("no-cache");
	}

	[Theory]
	[InlineData("_preview-wasm/" + _fingerprintedRuntime, null, true)]
	[InlineData("_preview-wasm/_framework/blazor.webassembly.66stpw5k9h.js", null, true)]
	[InlineData("_preview-wasm/_framework/icudt.oh1zvcfom8.dat", null, true)]
	[InlineData("_preview-wasm/_framework/de/Shop.resources.a1b2c3d4e5.wasm", null, true)]
	[InlineData("_preview-wasm/_framework/dotnet.js", null, false)]
	[InlineData("_preview-wasm/_framework/blazor.webassembly.js", null, false)]
	[InlineData("_preview-wasm/_framework/Microsoft.AspNetCore.Components.wasm", null, false)]
	[InlineData("_preview-wasm/_framework/System.Text.Json.wasm", null, false)]
	[InlineData("_preview-wasm/index.html", null, false)]
	[InlineData("_preview-wasm/_content/Host/wasmPreview.xjx9jsfwke.js", null, false)]
	[InlineData("_preview-assemblies/HASH.bin", null, true)]
	[InlineData("_preview-assemblies/0000000000000000.bin", null, false)]
	[InlineData("_preview-assemblies/nested/HASH.bin", null, false)]
	[InlineData("_preview-assemblies/HASH.dll", null, false)]
	[InlineData("search-index.json", null, false)]
	[InlineData("_theme/css/main.css", "HASH", true)]
	[InlineData("_theme/css/main.css", "0123456789", false)]
	[InlineData("_theme/css/main.css", "abc", false)]
	public void Immutable_OnlyForUrlsThatNameTheirContent(string key, string? version, bool immutable)
	{
		var file = new SiteFile("preview bytes"u8.ToArray(), "application/octet-stream");
		key = key.Replace("HASH", file.ContentHash);
		var values = new Dictionary<string, StringValues>();
		if (version is not null)
		{
			values["v"] = version.Replace("HASH", file.ContentHash[..10]);
		}

		var query = new QueryCollection(values);

		MokaDocsEndpointExtensions.IsImmutable(query, key, file).Should().Be(immutable);
	}

	[Fact]
	public async Task PreviewHost_IsServedWithItsOwnHeaders_AndPrecompressedForms()
	{
		byte[] runtime = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("(module) ", 400)));
		Write("host/index.html", "<p>preview host</p>");
		Write("host/" + _fingerprintedRuntime, runtime);
		Write("host/" + _fingerprintedRuntime + ".br", Compress(runtime, "br"));
		Write("host/" + _fingerprintedRuntime + ".gz", Compress(runtime, "gzip"));
		Write("host/_framework/dotnet.js", runtime);
		var plugin = new StagingPlugin(Path.Combine(_root, "host"));
		await using DocsTestHost host = await StartAsync(plugin);

		HttpResponseMessage index = await host.GetAsync("/docs/_preview-wasm/index.html");
		HttpResponseMessage wasm = await host.GetAsync("/docs/_preview-wasm/" + _fingerprintedRuntime, acceptEncoding: "br");
		HttpResponseMessage dotnetJs = await host.GetAsync("/docs/_preview-wasm/_framework/dotnet.js", acceptEncoding: "gzip");
		HttpResponseMessage page = await host.GetAsync("/docs/api");

		foreach (HttpResponseMessage preview in (HttpResponseMessage[]) [index, wasm, dotnetJs])
		{
			preview.StatusCode.Should().Be(HttpStatusCode.OK);
			preview.Headers.GetValues("Content-Security-Policy").Should().Equal("frame-ancestors 'self'");
			preview.Headers.GetValues("Cross-Origin-Opener-Policy").Should().Equal("same-origin");
			preview.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
		}

		page.Headers.Contains("Content-Security-Policy").Should().BeFalse();
		page.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
		index.Headers.CacheControl!.NoCache.Should().BeTrue();
		dotnetJs.Headers.CacheControl!.NoCache.Should().BeTrue();
		wasm.Headers.NonValidated["Cache-Control"].ToString().Should().Be("public, max-age=31536000, immutable");

		// The published .br is served as the file's Brotli form, not as a file of its own.
		wasm.Content.Headers.ContentType!.MediaType.Should().Be("application/wasm");
		wasm.Content.Headers.ContentEncoding.Should().Equal("br");
		(await wasm.Content.ReadAsByteArrayAsync(Ct)).Should().Equal(File.ReadAllBytes(Path.Combine(_root, "host", _fingerprintedRuntime + ".br")));
		Decompress(await dotnetJs.Content.ReadAsByteArrayAsync(Ct), "gzip").Should().Equal(runtime);
		InMemorySite site = await host.Docs.GetSiteAsync(Ct);
		site.Files.Keys.Should().NotContain(key => key.EndsWith(".br", StringComparison.Ordinal) || key.EndsWith(".gz", StringComparison.Ordinal));
	}

	[Fact]
	public async Task PrecompressedForm_ThatDoesNotMatchItsFile_IsNotServed()
	{
		// A publish folder that isn't cleared keeps app.js.br from an older publish next to a newer app.js.
		byte[] current = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("console.log('new');\n", 100)));
		byte[] old = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("console.log('old');\n", 100)));
		Write("host/app.js", current);
		Write("host/app.js.br", Compress(old, "br"));
		Write("host/orphan.js.gz", Compress(old, "gzip"));
		await using DocsTestHost host = await StartAsync(new StagingPlugin(Path.Combine(_root, "host")));

		HttpResponseMessage response = await host.GetAsync("/docs/_preview-wasm/app.js", acceptEncoding: "br");

		response.Content.Headers.ContentEncoding.Should().Equal("br");
		Decompress(await response.Content.ReadAsByteArrayAsync(Ct), "br").Should().Equal(current);
		(await host.GetAsync("/docs/_preview-wasm/app.js.br")).StatusCode.Should().Be(HttpStatusCode.NotFound);
		(await host.GetAsync("/docs/_preview-wasm/orphan.js.gz")).StatusCode.Should().Be(HttpStatusCode.OK,
			"a .gz with no file next to it is a file of its own");
	}

	[Fact]
	public async Task StagedDirectory_IsReadOncePerChange_NotOnEveryRebuild()
	{
		// Every rebuild read and hashed the whole published preview host again, about 45 MB.
		Write("host/index.html", "<p>one</p>");
		Write("host/_framework/app.js", string.Concat(Enumerable.Repeat("let x = 1;\n", 200)));
		await using DocsTestHost host = await StartAsync(new StagingPlugin(Path.Combine(_root, "host")));
		var orchestrator = host.App.Services.GetRequiredService<InMemoryBuildOrchestrator>();
		InMemorySite first = await host.Docs.GetSiteAsync(Ct);

		await host.Docs.RebuildAsync(Ct);
		InMemorySite second = await host.Docs.GetSiteAsync(Ct);

		second.Should().NotBeSameAs(first);
		orchestrator.StagedDirectoryReads.Should().Be(1);
		second.Files["_preview-wasm/_framework/app.js"].Should().BeSameAs(first.Files["_preview-wasm/_framework/app.js"]);

		Write("host/index.html", "<p>two, a little longer</p>");
		await host.Docs.RebuildAsync(Ct);

		orchestrator.StagedDirectoryReads.Should().Be(2);
		(await host.GetTextAsync("/docs/_preview-wasm/index.html")).Should().Be("<p>two, a little longer</p>");

		File.Delete(Path.Combine(_root, "host", "index.html"));
		await host.Docs.RebuildAsync(Ct);

		orchestrator.StagedDirectoryReads.Should().Be(3);
		(await host.GetAsync("/docs/_preview-wasm/index.html")).StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Rebuild_ReusesTheCompressedFormsOfUnchangedFiles()
	{
		await using DocsTestHost host = await StartAsync();
		InMemorySite first = await host.Docs.GetSiteAsync(Ct);

		await host.Docs.RebuildAsync(Ct);
		InMemorySite second = await host.Docs.GetSiteAsync(Ct);

		second.Files["api/index.html"].Encodings.Should().NotBeEmpty();
		second.Files["api/index.html"].Encodings.Should().BeSameAs(first.Files["api/index.html"].Encodings);
	}

	[Theory]
	[InlineData("_preview-wasm", "_preview-wasm/")]
	[InlineData("_preview-wasm/", "_preview-wasm/")]
	[InlineData("/_preview-wasm", null)]
	[InlineData("a\\b", "a/b/")]
	[InlineData("./a", "a/")]
	[InlineData("", "")]
	[InlineData("../outside", null)]
	[InlineData("a/../../outside", null)]
	[InlineData("C:/outside", null)]
	public void StagedDirectory_StaysInsideTheSite(string destination, string? prefix)
	{
		InMemoryBuildOrchestrator.OutputPrefix(destination).Should().Be(prefix);
	}

	private static SiteFile Compressible()
	{
		byte[] content = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("<p>compressible</p>", 200)));
		return new SiteFile(content, "text/html; charset=utf-8").WithEncodings(SiteCompression.Compress(content));
	}

	private static byte[] Compress(byte[] content, string coding)
	{
		using var output = new MemoryStream();
		using (Stream encoder = coding == "br"
			       ? new BrotliStream(output, CompressionLevel.SmallestSize, true)
			       : new GZipStream(output, CompressionLevel.SmallestSize, true))
		{
			encoder.Write(content);
		}

		return output.ToArray();
	}

	private static byte[] Decompress(byte[] content, string coding)
	{
		using var input = new MemoryStream(content);
		using Stream decoder = coding == "br"
			? new BrotliStream(input, CompressionMode.Decompress)
			: new GZipStream(input, CompressionMode.Decompress);
		using var output = new MemoryStream();
		decoder.CopyTo(output);
		return output.ToArray();
	}

	private Task<DocsTestHost> StartAsync(StagingPlugin? plugin = null) =>
		DocsTestHost.StartAsync(_root, options =>
		{
			options.DocsPath = "docs";
			if (plugin is not null)
			{
				options.Plugins = [new PluginEntry { Id = StagingPlugin.PluginId }];
			}
		}, register: services =>
		{
			if (plugin is not null)
			{
				services.AddSingleton<IMokaPlugin>(plugin);
			}
		});

	private void Write(string relativePath, string content) => Write(relativePath, Encoding.UTF8.GetBytes(content));

	private void Write(string relativePath, byte[] content)
	{
		string path = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllBytes(path, content);
	}

	/// <summary>Stages a directory under <c>_preview-wasm</c>, as the Blazor preview plugin stages its published host.</summary>
	private sealed class StagingPlugin(string directory) : IMokaPlugin
	{
		public const string PluginId = "staging";
		public string Id => PluginId;
		public string Name => "Staging";
		public string Version => "1.0.0";

		public Task InitializeAsync(IPluginContext context, CancellationToken ct = default) => Task.CompletedTask;

		public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
		{
			buildContext.DeferredOutputDirectories.Add((directory, "_preview-wasm"));
			return Task.CompletedTask;
		}
	}
}
