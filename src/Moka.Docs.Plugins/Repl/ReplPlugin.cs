using System.Text;
using Moka.Docs.Core.Content;
using Moka.Docs.Core.Pipeline;

namespace Moka.Docs.Plugins.Repl;

/// <summary>
///     MokaDocs plugin that adds interactive C# REPL functionality to documentation pages.
///     Code blocks written as <c>```csharp-repl</c> are enhanced with a Run button and
///     output panel. In <c>mokadocs serve</c> mode, code is executed server-side via
///     Roslyn scripting. In static builds, a message directs users to use the dev server.
/// </summary>
public sealed class ReplPlugin : IMokaPlugin
{
	#region Inline CSS

	private const string InlineCss = """
	                                 <style>
	                                 .repl-container {
	                                     position: relative;
	                                     border: 1px solid var(--border-color, #e2e8f0);
	                                     border-radius: 8px;
	                                     margin: 1.5em 0;
	                                     overflow: hidden;
	                                     background: var(--code-bg, #1e1e2e);
	                                 }
	                                 .repl-container pre {
	                                     margin: 0;
	                                     border: none;
	                                     border-radius: 0;
	                                     padding-bottom: 3em;
	                                 }
	                                 .repl-container code {
	                                     font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                 }
	                                 .repl-toolbar {
	                                     display: flex;
	                                     align-items: center;
	                                     gap: 0.5em;
	                                     padding: 0.4em 0.75em;
	                                     background: var(--code-toolbar-bg, #181825);
	                                     border-top: 1px solid var(--border-color, #313244);
	                                 }
	                                 .repl-run-btn {
	                                     display: inline-flex;
	                                     align-items: center;
	                                     gap: 0.35em;
	                                     padding: 0.35em 0.9em;
	                                     font-size: 0.8rem;
	                                     font-weight: 600;
	                                     color: #fff;
	                                     background: #16a34a;
	                                     border: none;
	                                     border-radius: 4px;
	                                     cursor: pointer;
	                                     transition: background 0.15s;
	                                 }
	                                 .repl-run-btn:hover { background: #15803d; }
	                                 .repl-run-btn:disabled {
	                                     background: #6b7280;
	                                     cursor: not-allowed;
	                                 }
	                                 .repl-run-btn svg {
	                                     width: 14px;
	                                     height: 14px;
	                                     fill: currentColor;
	                                 }
	                                 .repl-status {
	                                     font-size: 0.75rem;
	                                     color: #94a3b8;
	                                     font-family: monospace;
	                                 }
	                                 .repl-output {
	                                     padding: 0.75em 1em;
	                                     font-family: 'JetBrains Mono', 'Cascadia Code', 'Fira Code', monospace;
	                                     font-size: 0.85rem;
	                                     line-height: 1.5;
	                                     background: var(--repl-output-bg, #11111b);
	                                     border-top: 1px solid var(--border-color, #313244);
	                                     color: #a6e3a1;
	                                     white-space: pre-wrap;
	                                     word-break: break-word;
	                                     max-height: 300px;
	                                     overflow-y: auto;
	                                 }
	                                 .repl-output.repl-error {
	                                     color: #f38ba8;
	                                 }
	                                 .repl-output.repl-unavailable {
	                                     color: #fab387;
	                                 }
	                                 </style>
	                                 """;

	#endregion

	#region Inline JS

	private const string InlineJs = """
	                                <script>
	                                (function() {
	                                    const playIcon = '<svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>';
	                                    const spinnerIcon = '<svg viewBox="0 0 24 24" class="repl-spinner"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" stroke-dasharray="31.4 31.4" transform="rotate(-90 12 12)"><animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="0.8s" repeatCount="indefinite"/></circle></svg>';

	                                    document.querySelectorAll('.repl-container[data-repl="true"]').forEach(function(container) {
	                                        var codeEl = container.querySelector('code');
	                                        var outputEl = container.querySelector('.repl-output');
	                                        if (!codeEl || !outputEl) return;

	                                        // Create toolbar
	                                        var toolbar = document.createElement('div');
	                                        toolbar.className = 'repl-toolbar';

	                                        var runBtn = document.createElement('button');
	                                        runBtn.className = 'repl-run-btn';
	                                        runBtn.innerHTML = playIcon + ' Run';
	                                        runBtn.type = 'button';

	                                        var status = document.createElement('span');
	                                        status.className = 'repl-status';

	                                        toolbar.appendChild(runBtn);
	                                        toolbar.appendChild(status);

	                                        // Insert toolbar after the pre element
	                                        var preEl = container.querySelector('pre');
	                                        if (preEl && preEl.nextSibling) {
	                                            container.insertBefore(toolbar, preEl.nextSibling);
	                                        } else {
	                                            container.insertBefore(toolbar, outputEl);
	                                        }

	                                        runBtn.addEventListener('click', function() {
	                                            var code = codeEl.textContent;
	                                            runBtn.disabled = true;
	                                            runBtn.innerHTML = spinnerIcon + ' Running\u2026';
	                                            status.textContent = '';
	                                            outputEl.style.display = 'none';
	                                            outputEl.className = 'repl-output';
	                                            outputEl.textContent = '';

	                                            var startTime = performance.now();

	                                            fetch('/api/repl/execute', {
	                                                method: 'POST',
	                                                headers: { 'Content-Type': 'application/json' },
	                                                body: JSON.stringify({ code: code })
	                                            })
	                                            .then(function(res) { return res.json(); })
	                                            .then(function(data) {
	                                                var elapsed = ((performance.now() - startTime) / 1000).toFixed(2);
	                                                outputEl.style.display = 'block';

	                                                if (data.error) {
	                                                    outputEl.className = 'repl-output repl-error';
	                                                    outputEl.textContent = data.error;
	                                                    status.textContent = 'Error (' + elapsed + 's)';
	                                                } else {
	                                                    outputEl.className = 'repl-output';
	                                                    outputEl.textContent = data.output || '(no output)';
	                                                    status.textContent = 'Completed in ' + elapsed + 's';
	                                                }
	                                            })
	                                            .catch(function(err) {
	                                                outputEl.style.display = 'block';
	                                                outputEl.className = 'repl-output repl-unavailable';
	                                                outputEl.textContent = 'REPL server unavailable. Run "mokadocs serve" to enable interactive code execution.';
	                                                status.textContent = '';
	                                            })
	                                            .finally(function() {
	                                                runBtn.disabled = false;
	                                                runBtn.innerHTML = playIcon + ' Run';
	                                            });
	                                        });
	                                    });
	                                })();
	                                </script>
	                                """;

	#endregion

	/// <inheritdoc />
	public string Id => "mokadocs-repl";

	/// <inheritdoc />
	public string Name => "Live .NET REPL";

	/// <inheritdoc />
	public string Version => "1.0.0";

	/// <inheritdoc />
	public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
	{
		context.LogInfo("REPL plugin initialized — interactive C# code blocks enabled");
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ExecuteAsync(IPluginContext context, BuildContext buildContext, CancellationToken ct = default)
	{
		int pagesWithRepl = 0;

		foreach (DocPage page in buildContext.Pages)
		{
			string html = page.Content.Html;
			if (string.IsNullOrEmpty(html))
			{
				continue;
			}

			if (!html.Contains("data-repl=\"true\"", StringComparison.Ordinal))
			{
				continue;
			}

			// Inject CSS and JS into pages that have REPL blocks
			page.Content = page.Content with
			{
				Html = InjectReplAssets(html)
			};
			pagesWithRepl++;
		}

		if (pagesWithRepl > 0)
		{
			context.LogInfo($"REPL plugin: Enhanced {pagesWithRepl} page(s) with interactive code blocks");
		}

		return Task.CompletedTask;
	}

	/// <summary>
	///     Wraps the page HTML with inline REPL CSS at the top and JS at the bottom.
	/// </summary>
	private static string InjectReplAssets(string html)
	{
		var sb = new StringBuilder(html.Length + InlineCss.Length + InlineJs.Length + 64);
		sb.Append(InlineCss);
		sb.Append(html);
		sb.Append(InlineJs);
		return sb.ToString();
	}
}
