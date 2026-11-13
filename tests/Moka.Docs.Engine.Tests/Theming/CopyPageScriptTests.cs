using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Moka.Docs.Themes.Default;

namespace Moka.Docs.Engine.Tests.Theming;

/// <summary>
///     The "Copy page" button's script, run under Node with a stub page when Node is on PATH.
/// </summary>
public sealed class CopyPageScriptTests
{
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

	private static string Block()
	{
		string js = EmbeddedThemeProvider.GetJs().Replace("\r\n", "\n");
		int start = js.IndexOf("/* Copy page:", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		int end = js.IndexOf("/* Components:", start, StringComparison.Ordinal);
		return end > start ? js[start..end] : js[start..];
	}

	// A stub page with the button, a fetch that serves the Markdown (or fails), and a clipboard API
	// that is missing, rejects, or works. Prints what was fetched and copied and the button's text.
	private static string[] Run(bool secureContext, string clipboard, bool fetchFails = false, int clicks = 1)
	{
		Assert.SkipUnless(_nodeAvailable.Value, "node is not on PATH");
		string script = $$"""
		                  const copied = [], fetched = [];
		                  function element(tag) {
		                      return {
		                          tagName: tag, hidden: true, textContent: 'Copy page', attributes: {}, listeners: {}, style: {}, value: '',
		                          getAttribute(k) { return k in this.attributes ? this.attributes[k] : null; },
		                          setAttribute(k, v) { this.attributes[k] = String(v); },
		                          addEventListener(t, f) { (this.listeners[t] = this.listeners[t] || []).push(f); },
		                          dispatch(t) { (this.listeners[t] || []).forEach(f => f({ preventDefault() {} })); },
		                          select() {}
		                      };
		                  }
		                  const button = element('button');
		                  button.setAttribute('data-markdown-url', '/Repo/guide/intro.md');
		                  const body = { children: [], appendChild(e) { this.children.push(e); }, removeChild(e) { this.children = this.children.filter(c => c !== e); } };
		                  const document = {
		                      body,
		                      createElement: element,
		                      querySelector: s => s === '.page-copy-markdown' ? button : null,
		                      execCommand(command) { copied.push('selection:' + body.children[body.children.length - 1].value); return true; }
		                  };
		                  let fails = {{(fetchFails ? "true" : "false")}};
		                  function fetch(url) {
		                      fetched.push(url);
		                      return Promise.resolve(fails ? { ok: false, status: 404 } : { ok: true, status: 200, text: () => Promise.resolve('# Intro\n') });
		                  }
		                  const window = { fetch, Promise, isSecureContext: {{(secureContext ? "true" : "false")}} };
		                  const clipboardMode = '{{clipboard}}';
		                  const navigator = clipboardMode === 'none' ? {} : {
		                      clipboard: {
		                          writeText(text) {
		                              if (clipboardMode === 'rejects') return Promise.reject(new Error('denied'));
		                              copied.push('clipboard:' + text);
		                              return Promise.resolve();
		                          }
		                      }
		                  };
		                  const setTimeout = () => {};
		                  {{Block()}}
		                  (async () => {
		                      const settle = async () => { for (let i = 0; i < 20; i++) await new Promise(r => setImmediate(r)); };
		                      const labels = [];
		                      for (let i = 0; i < {{clicks}}; i++) {
		                          button.dispatch('click');
		                          await settle();
		                          labels.push(button.textContent);
		                          fails = false;
		                      }
		                      process.stdout.write(JSON.stringify([String(button.hidden), ...fetched, ...copied, ...labels]));
		                  })();
		                  """;

		string path = Path.Combine(Path.GetTempPath(), "mokadocs-copy-page-" + Guid.NewGuid().ToString("N") + ".js");
		File.WriteAllText(path, script);
		try
		{
			var info = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
			info.ArgumentList.Add(path);
			using Process node = Process.Start(info)!;
			Task<string> stdout = node.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
			Task<string> stderr = node.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
			node.WaitForExit(30_000).Should().BeTrue("node should finish");
			node.ExitCode.Should().Be(0, stderr.Result);
			return JsonSerializer.Deserialize<string[]>(stdout.Result)!;
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void SecureContext_CopiesTheFetchedMarkdownWithTheClipboardApi()
	{
		Run(true, "works").Should().Equal("false", "/Repo/guide/intro.md", "clipboard:# Intro\n", "Copied!");
	}

	[Fact]
	public void PlainHttp_CopiesThroughASelection()
	{
		// navigator.clipboard is missing outside a secure context, and calling it threw.
		Run(false, "none").Should().Equal("false", "/Repo/guide/intro.md", "selection:# Intro\n", "Copied!");
	}

	[Fact]
	public void RefusedClipboard_FallsBackToASelection()
	{
		Run(true, "rejects").Should().Equal("false", "/Repo/guide/intro.md", "selection:# Intro\n", "Copied!");
	}

	[Fact]
	public void FailedDownload_SaysSoAndTheNextClickTriesAgain()
	{
		Run(true, "works", fetchFails: true, clicks: 2).Should().Equal(
			"false", "/Repo/guide/intro.md", "/Repo/guide/intro.md", "clipboard:# Intro\n", "Copy failed", "Copied!");
	}

	[Fact]
	public void SecondCopy_UsesTheDownloadedText()
	{
		Run(true, "works", clicks: 2).Should().Equal(
			"false", "/Repo/guide/intro.md", "clipboard:# Intro\n", "clipboard:# Intro\n", "Copied!", "Copied!");
	}
}
