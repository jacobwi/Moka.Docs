namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     Controls how blazor-preview code blocks are rendered in the output.
/// </summary>
public enum BlazorPreviewMode
{
	/// <summary>
	///     Static server-side rendering via HtmlRenderer. Produces non-interactive HTML.
	///     No JavaScript or WASM runtime required.
	/// </summary>
	Ssr,

	/// <summary>
	///     Interactive client-side rendering via Blazor WebAssembly.
	///     Compiles components to DLLs at build time, loads them in-browser via iframe.
	///     Falls back to SSR HTML when JavaScript is unavailable.
	/// </summary>
	Wasm
}
