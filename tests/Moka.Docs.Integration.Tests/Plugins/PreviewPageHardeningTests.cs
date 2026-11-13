using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moka.Docs.Plugins.BlazorPreview;

namespace Moka.Docs.Integration.Tests.Plugins;

/// <summary>
///     The Content-Security-Policy and message guard the Blazor preview plugin adds to the preview
///     host's pages. The guard itself runs under Node, when it is on PATH, against stand-ins for
///     <c>window</c>, <c>location</c> and <c>history</c>.
/// </summary>
public sealed class PreviewPageHardeningTests
{
	private const string _assembly = "/base/_preview-assemblies/0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.bin";

	// As a host with OverrideHtmlAssetPlaceholders publishes it on Windows: the import map has CR LF line endings.
	private const string _published = "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n  <meta charset=\"utf-8\" />\n" +
	                                   "  <meta name=\"viewport\" content=\"width=device-width\" />\n  <base href=\"./\" />\n" +
	                                   "  <link href=\"_framework/dotnet.6pn3zp6njs.js\" rel=\"preload\" as=\"script\" />\n" +
	                                   "  <script type=\"importmap\">{\r\n  \"imports\": {\r\n    \"./_framework/dotnet.js\": \"./_framework/dotnet.6pn3zp6njs.js\"\r\n  }\r\n}</script>\n" +
	                                   "</head>\n<body>\n  <div id=\"app\" style=\"padding:1rem\">Loading</div>\n" +
	                                   "  <script src=\"_content/Moka.Blazor.Repl.Host/wasmPreview.js\"></script>\n" +
	                                   "  <script src=\"_framework/blazor.webassembly.w3qd1tpl0e.js\"></script>\n</body>\n</html>\n";

	private static readonly Lazy<bool> _nodeAvailable = new(() =>
	{
		try
		{
			using Process? node = Process.Start(new ProcessStartInfo("node", "--version")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false
			});
			return node is not null && node.WaitForExit(10_000) && node.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			return false;
		}
	});

	[Fact]
	public void PolicyAndGuard_ComeAfterTheCharset_AndBeforeEverythingThatLoads()
	{
		// The policy only covers elements parsed after it, and the guard must exist before the
		// package's listener; the charset has to stay in the first 1024 bytes.
		string html = PreviewPageHardening.Harden(_published).Html;

		int charset = html.IndexOf("<meta charset", StringComparison.Ordinal);
		int policy = html.IndexOf("<meta http-equiv=\"Content-Security-Policy\" data-mokadocs-preview", StringComparison.Ordinal);
		int guard = html.IndexOf("<script data-mokadocs-preview>", StringComparison.Ordinal);
		policy.Should().BeGreaterThan(charset);
		guard.Should().BeGreaterThan(policy);
		html.IndexOf("<script", StringComparison.Ordinal).Should().Be(guard, "the guard is the first script");
		guard.Should().BeLessThan(html.IndexOf("<meta name=\"viewport\"", StringComparison.Ordinal));
	}

	[Fact]
	public void Policy_AllowsTheGuardAndTheImportMap_ByTheHashOfTheTextTheBrowserSees()
	{
		// The HTML parser turns CR LF into LF before the browser hashes an inline script, so a hash
		// of the file's bytes would block the import map and with it the whole runtime.
		string policy = PreviewPageHardening.Harden(_published).Policy;

		string importMap = "{\n  \"imports\": {\n    \"./_framework/dotnet.js\": \"./_framework/dotnet.6pn3zp6njs.js\"\n  }\n}";
		policy.Should().Contain(Sha256Source(importMap))
			.And.Contain(Sha256Source(PreviewPageHardening.GuardScript))
			.And.NotContain(Sha256Source(importMap.Replace("\n", "\r\n", StringComparison.Ordinal)));
	}

	[Fact]
	public void Policy_IsTheBlazorClientSidePolicy_WithConnectionsAndScriptsKeptToTheDocsOrigin()
	{
		string policy = PreviewPageHardening.Harden(_published).Policy;

		string[] directives = policy.Split("; ");
		directives.Should().Contain(["base-uri 'self'", "default-src 'self'", "img-src 'self' data: https:", "object-src 'none'",
			"style-src 'self' 'unsafe-inline'", "connect-src 'self'"]);
		directives.Should().ContainSingle(d => d.StartsWith("script-src ", StringComparison.Ordinal))
			.Which.Should().StartWith("script-src 'self' 'wasm-unsafe-eval' 'sha256-")
			.And.NotContain("unsafe-inline").And.NotContain(" 'unsafe-eval'").And.NotContain("https:");
	}

	[Fact]
	public void HardeningAgain_ReplacesWhatAnEarlierRunAdded()
	{
		string once = PreviewPageHardening.Harden(_published).Html;
		string older = once.Replace(PreviewPageHardening.GuardScript, "oldGuard();", StringComparison.Ordinal);

		PreviewPageHardening.Harden(once).Html.Should().Be(once);
		string again = PreviewPageHardening.Harden(older).Html;
		again.Should().Be(once);
		Regex.Matches(again, "data-mokadocs-preview").Should().HaveCount(2);
	}

	[Fact]
	public void ScriptsAndStylesheetsFromOtherOrigins_ThatThePageLinks_AreAllowed()
	{
		// A preview host that loads a library from a CDN kept working without a policy.
		string page = "<html><head><meta charset=\"utf-8\">" +
		              "<script src=\"https://cdn.example.com/lib.js?v=1\"></script>" +
		              "<script src=\"//unpkg.example.org:8443/x.js\"></script>" +
		              "<link rel=\"stylesheet\" href=\"https://fonts.example.com/css?family=Inter\">" +
		              "<link rel=\"icon\" href=\"https://icons.example.net/favicon.png\">" +
		              "<script src=\"javascript:alert(1)\"></script>" +
		              "<script src=\"https://bad.example/';script-src *\"></script>" +
		              "</head></html>";

		string policy = PreviewPageHardening.Harden(page).Policy;

		string scripts = policy.Split("; ").Single(d => d.StartsWith("script-src ", StringComparison.Ordinal));
		string styles = policy.Split("; ").Single(d => d.StartsWith("style-src ", StringComparison.Ordinal));
		scripts.Should().Contain(" https://cdn.example.com ").And.Contain(" unpkg.example.org:8443 ")
			.And.Contain(" https://bad.example ").And.NotContain("javascript").And.NotContain("*");
		styles.Should().Be("style-src 'self' 'unsafe-inline' https://fonts.example.com");
		policy.Should().NotContain("icons.example.net");
	}

	[Fact]
	public void ScriptsInCommentsAndText_AreNotReadAsScripts()
	{
		string page = "<html><head><!-- <head><script>commented()</script> --><title>a <script>b</script></title>" +
		              "<meta charset=\"utf-8\"></head><body><textarea><script>c()</script></textarea></body></html>";

		HardenedPage hardened = PreviewPageHardening.Harden(page);

		Regex.Matches(hardened.Policy, "'sha256-").Should().HaveCount(1, "only the guard is a script");
		hardened.Html.Should().StartWith("<html><head><!-- <head><script>commented()</script> -->",
			"the policy goes after the real head tag, not the one in the comment");
	}

	[Theory]
	[InlineData("<p>preview host</p>", 0)]
	[InlineData("<!DOCTYPE html><p>x</p>", 15)]
	[InlineData("<!DOCTYPE html>\n<html lang=\"en\"><body>x</body></html>", 32)]
	public void PageWithoutAHeadTag_GetsThePolicyBeforeItsContent(string page, int position)
	{
		// The parser opens the head itself for a <meta> before any content, and the policy counts there.
		string html = PreviewPageHardening.Harden(page).Html;

		html.IndexOf("\n<meta http-equiv=\"Content-Security-Policy\"", StringComparison.Ordinal).Should().Be(position);
		html.Remove(position, html.IndexOf("</script>", StringComparison.Ordinal) + "</script>".Length - position)
			.Should().Be(page);
	}

	[Theory]
	[InlineData("<head>\r\n  <link rel=\"stylesheet\" href=\"Host.styles.css\" />\r\n</head>", "<head>\r\n</head>")]
	[InlineData("<head><link href='./Host.styles.css' rel=stylesheet><title>x</title></head>", "<head><title>x</title></head>")]
	[InlineData("<head>\n<!-- <link rel=\"stylesheet\" href=\"Host.styles.css\"> -->\n</head>", null)]
	[InlineData("<head>\n<link rel=\"stylesheet\" href=\"Present.styles.css\">\n</head>", null)]
	[InlineData("<head>\n<link rel=\"stylesheet\" href=\"css/app.css\">\n</head>", null)]
	[InlineData("<head>\n<link rel=\"stylesheet\" href=\"_content/Lib/Lib.styles.css\">\n</head>", null)]
	[InlineData("<head>\n<link rel=\"stylesheet\" href=\"https://cdn.example.com/Host.styles.css\">\n</head>", null)]
	[InlineData("<head>\n<link rel=\"preload\" href=\"Host.styles.css\">\n</head>", null)]
	public void ScopedCssBundleLink_GoesOnlyWhenTheBundleIsMissing(string page, string? expected)
	{
		string html = PreviewPageHardening.RemoveMissingScopedCss(page, file => file == "Present.styles.css");

		html.Should().Be(expected ?? page);
	}

	[Fact]
	public void PageWithAPolicyOfItsOwn_IsFlagged()
	{
		string page = "<head><meta http-equiv=\"content-security-policy\" content=\"default-src 'none'\"></head>";

		PreviewPageHardening.Harden(page).HasOwnPolicy.Should().BeTrue();
		PreviewPageHardening.Harden(_published).HasOwnPolicy.Should().BeFalse();
	}

	[Fact]
	public void Guard_LetsThroughOnlyMessagesFromTheParentOrThePageOnTheSameOrigin()
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");

		JsonElement result = RunGuard("""
			const page = 'https://docs.example/base/_preview-wasm/index.html';
			const framed = load(page, true);
			const top = load(page, false);
			const other = {};
			out({
			  capture: framed.listeners.every(l => l.capture === true),
			  parent: framed.deliver('https://docs.example', framed.win.parent),
			  self: framed.deliver('https://docs.example', framed.win),
			  parentOtherOrigin: framed.deliver('https://evil.example', framed.win.parent),
			  parentNullOrigin: framed.deliver('null', framed.win.parent),
			  otherWindow: framed.deliver('https://docs.example', other),
			  noSource: framed.deliver('https://docs.example', null),
			  openedByAnotherSite: top.deliver('https://evil.example', other)
			});
			""");

		result.GetProperty("capture").GetBoolean().Should().BeTrue("a capturing listener runs before the package's");
		result.GetProperty("parent").GetBoolean().Should().BeFalse("the docs page posts 'run' to its iframes");
		result.GetProperty("self").GetBoolean().Should().BeFalse();
		result.GetProperty("parentOtherOrigin").GetBoolean().Should().BeTrue();
		result.GetProperty("parentNullOrigin").GetBoolean().Should().BeTrue();
		result.GetProperty("otherWindow").GetBoolean().Should().BeTrue();
		result.GetProperty("noSource").GetBoolean().Should().BeTrue();
		result.GetProperty("openedByAnotherSite").GetBoolean().Should().BeTrue();
	}

	[Theory]
	[InlineData(_assembly, true)]
	[InlineData("", true)]
	[InlineData("https://evil.example/_preview-assemblies/0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.bin", false)]
	[InlineData("https://docs.example" + _assembly, false)]
	[InlineData("//evil.example" + _assembly, false)]
	[InlineData("/\\evil.example" + _assembly, false)]
	[InlineData("/base/uploads/0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.bin", false)]
	[InlineData("/base/_preview-assemblies/../uploads/0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.bin", false)]
	[InlineData("/base/_preview-assemblies/%2e%2e/uploads/0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef.bin", false)]
	[InlineData("/base/_preview-assemblies/0123456789abcdef.dll", false)]
	[InlineData(_assembly + "?x=1", false)]
	[InlineData("data:application/octet-stream;base64,AAAA", false)]
	public void Guard_LoadsOnlyAssembliesFromThePreviewAssembliesFolder(string assembly, bool allowed)
	{
		// The page fetched and loaded any ?assembly= URL, so a link could run any code in the docs origin.
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		string query = assembly.Length == 0 ? "" : "?assembly=" + Uri.EscapeDataString(assembly) + "&entry=X";

		JsonElement result = RunGuard($$"""
			const g = load('https://docs.example/base/_preview-wasm/index.html{{query}}', true);
			out({ error: g.error, stopped: g.win.stopped, replaced: g.history.replaced });
			""");

		if (allowed)
		{
			result.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
			result.GetProperty("stopped").GetBoolean().Should().BeFalse();
		}
		else
		{
			result.GetProperty("error").GetString().Should().Contain("refused");
			result.GetProperty("stopped").GetBoolean().Should().BeTrue("the runtime must not start");
			result.GetProperty("replaced").GetString().Should().Be("/base/_preview-wasm/index.html",
				"the query is dropped before Blazor reads it");
		}
	}

	[Fact]
	public void GuardAndPageScript_AreValidJavaScript()
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		string folder = Path.Combine(Path.GetTempPath(), "mokadocs-guard-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);
		try
		{
			string guard = Path.Combine(folder, "guard.js");
			string assets = Path.Combine(folder, "assets.js");
			File.WriteAllText(guard, PreviewPageHardening.GuardScript);
			File.WriteAllBytes(assets, PreviewPageAssets.Script);
			foreach (string script in (string[]) [guard, assets])
			{
				(int exitCode, string _, string errors) = RunNode(["--check", script]);
				exitCode.Should().Be(0, errors);
			}
		}
		finally
		{
			Directory.Delete(folder, true);
		}
	}

	private static string Sha256Source(string script) =>
		$"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(script)))}'";

	/// <summary>
	///     Runs <paramref name="test" /> after a <c>load(href, framed)</c> that runs the guard against
	///     stand-ins and returns its listeners, a <c>deliver(origin, source)</c> that reports whether
	///     the guard stopped a message, and what it did to the window and history.
	/// </summary>
	private static JsonElement RunGuard(string test)
	{
		string harness = $$"""
			const guard = {{JsonSerializer.Serialize(PreviewPageHardening.GuardScript)}};
			function load(href, framed) {
			  const listeners = [];
			  const win = { stopped: false, addEventListener(type, fn, capture) { listeners.push({ type, fn, capture }); }, stop() { this.stopped = true; } };
			  win.parent = framed ? {} : win;
			  const history = { replaced: null, replaceState(state, title, url) { this.replaced = url; } };
			  let error = null;
			  try {
			    new Function('window', 'location', 'history', guard)(win, new URL(href), history);
			  } catch (e) {
			    error = e.message;
			  }
			  function deliver(origin, source) {
			    let stopped = false;
			    const event = { origin, source, data: { type: 'run' }, stopImmediatePropagation() { stopped = true; } };
			    listeners.filter(l => l.type === 'message').forEach(l => l.fn(event));
			    return stopped;
			  }
			  return { win, history, error, listeners, deliver };
			}
			function out(value) { process.stdout.write(JSON.stringify(value)); }
			{{test}}
			""";
		(int exitCode, string output, string errors) = RunNode(["-e", harness]);
		exitCode.Should().Be(0, errors);
		return JsonDocument.Parse(output).RootElement;
	}

	private static (int ExitCode, string Output, string Errors) RunNode(string[] arguments)
	{
		var startInfo = new ProcessStartInfo("node")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};
		foreach (string argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using Process node = Process.Start(startInfo)!;
		Task<string> output = node.StandardOutput.ReadToEndAsync();
		Task<string> errors = node.StandardError.ReadToEndAsync();
		node.WaitForExit(30_000).Should().BeTrue();
		return (node.ExitCode, output.GetAwaiter().GetResult(), errors.GetAwaiter().GetResult());
	}
}
