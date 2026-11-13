using Moka.Docs.Core.Theming;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     An announcement above the header of every page, the counterpart of <c>theme.options.banner</c> in
///     mokadocs.yaml: one line of plain text with an optional link, which readers can dismiss.
/// </summary>
public sealed class MokaDocsBannerOptions
{
	/// <summary>The announcement, as plain text. Empty shows no banner.</summary>
	public string Text { get; set; } = "";

	/// <summary>
	///     Where the banner's link goes: an absolute URL, or a path from the site root such as
	///     <c>"/blog/1.9"</c>, which gets <see cref="MokaDocsOptions.BasePath" />. <c>null</c> for no link.
	/// </summary>
	public string? Link { get; set; }

	/// <summary>The link's text. <c>null</c> shows <c>"Learn more"</c>.</summary>
	public string? LinkText { get; set; }

	/// <summary>
	///     What a reader's dismissal is remembered by: a reader who dismissed the banner sees it again
	///     once this changes. <c>null</c> uses a hash of <see cref="Text" /> and <see cref="Link" />, so
	///     changing either shows the banner again.
	/// </summary>
	public string? Id { get; set; }

	/// <summary>Whether the banner has a button that hides it for the reader. Default: true.</summary>
	public bool Dismissible { get; set; } = true;

	/// <summary>
	///     The look: <c>"info"</c> (the default, a tint of the primary color), <c>"warning"</c> (an amber
	///     tint) or <c>"accent"</c> (a solid bar in the primary color). An unknown name renders as
	///     <c>"info"</c>, and the build logs a warning.
	/// </summary>
	public string Variant { get; set; } = BannerVariants.Info;
}
