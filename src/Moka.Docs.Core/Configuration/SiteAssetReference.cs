namespace Moka.Docs.Core.Configuration;

/// <summary>
///     A resolved reference to a site-level brand asset such as
///     <see cref="SiteMetadata.Logo" /> or <see cref="SiteMetadata.Favicon" />.
///     <para>
///         Unlike the raw yaml string, this record knows BOTH where the file lives on
///         disk (for asset copying) AND what URL the theme templates should emit (for
///         browser resolution) — including correct handling of paths that traverse
///         out of the <c>content.docs</c> directory via <c>../</c>, and absolute URLs
///         (http/https/data) which pass through unchanged.
///     </para>
///     <para>
///         Created by <see cref="SiteConfigReader" /> from the raw yaml value and the
///         mokadocs.yaml file's directory. Consumers (FileDiscoveryService / BrandAsset-
///         Resolver / OutputPhase / ScribanTemplateEngine) work only with the already-
///         resolved fields, never with the raw yaml string.
///     </para>
/// </summary>
/// <remarks>
///     Path resolution rules, applied in <see cref="SiteConfigReader" /> at load time:
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Absolute URLs</b> (<c>http://</c>, <c>https://</c>, <c>//cdn.example.com/</c>,
///                 <c>data:</c>): passed through unchanged. <see cref="IsAbsoluteUrl" /> is true,
///                 <see cref="SourcePath" /> is null, and <see cref="PublishUrl" /> is the URL itself.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Relative paths inside the yaml directory</b> (e.g. <c>assets/logo.png</c>
///                 or <c>./logo.png</c>): <see cref="SourcePath" /> is resolved from the yaml dir,
///                 and <see cref="PublishUrl" /> mirrors the layout under <c>_site/</c>
///                 (e.g. <c>/assets/logo.png</c>).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Relative paths escaping the yaml directory</b> via <c>../</c> (e.g.
///                 <c>../branding/logo.png</c>): <see cref="SourcePath" /> is resolved, but
///                 <see cref="PublishUrl" /> is flattened to <c>/_media/{filename}</c> to
///                 avoid URL path-normalization issues and cross-directory collisions. The
///                 <c>_media</c> prefix starts with an underscore so a <c>.nojekyll</c>-
///                 protected GitHub Pages deployment ships it correctly.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Site-absolute paths</b> (<c>/custom/logo.png</c>): leading slash is
///                 interpreted as "relative to yaml dir" (NOT filesystem root) for ergonomic
///                 parity with the other relative forms. Users who want CDN-hosted assets
///                 should use a full URL.
///             </description>
///         </item>
///     </list>
/// </remarks>
public sealed record SiteAssetReference
{
	/// <summary>
	///     The raw yaml value exactly as the user wrote it. Kept so the legacy
	///     <c>site.logo</c> / <c>site.favicon</c> scriban template variables can still
	///     expose it for consumers of custom templates that referenced them before
	///     this type existed. New templates should use <c>site.logo_url</c> /
	///     <c>site.favicon_url</c> instead, which apply base-path prefixing and URL
	///     pass-through rules automatically.
	/// </summary>
	public required string RawValue { get; init; }

	/// <summary>
	///     Absolute filesystem path where the asset file lives, or <c>null</c> when
	///     <see cref="IsAbsoluteUrl" /> is true (in which case there's nothing to copy).
	///     <para>
	///         Resolved by the reader from <c>Path.GetFullPath(Path.Combine(yamlDir, raw))</c>,
	///         so <c>..</c> segments are normalized at load time and the value always points
	///         to the real file regardless of how the user wrote it.
	///     </para>
	/// </summary>
	public string? SourcePath { get; init; }

	/// <summary>
	///     The URL the theme should emit for this asset. Always site-root-absolute
	///     (<c>/something</c>) for filesystem assets — the Scriban template engine prepends
	///     the build <see cref="BuildConfig.BasePath" /> to it automatically for GitHub Pages
	///     subpath deploys. For absolute URLs, this is the URL itself and the template
	///     skips the base-path prefix.
	///     <para>
	///         Examples:
	///         <list type="bullet">
	///             <item><description><c>assets/logo.png</c> → <c>/assets/logo.png</c></description></item>
	///             <item><description><c>./logo.png</c> → <c>/logo.png</c></description></item>
	///             <item><description><c>../branding/logo.png</c> → <c>/_media/logo.png</c></description></item>
	///             <item><description><c>https://cdn.example.com/logo.png</c> → <c>https://cdn.example.com/logo.png</c></description></item>
	///         </list>
	///     </para>
	/// </summary>
	public required string PublishUrl { get; init; }

	/// <summary>
	///     <c>true</c> when the raw yaml value was an absolute URL (http/https/protocol-
	///     relative/data URI). In this case there is no local file to copy and the URL
	///     is emitted verbatim without a base-path prefix.
	/// </summary>
	public required bool IsAbsoluteUrl { get; init; }

	/// <summary>
	///     Convenience: <c>true</c> when this asset has a local <see cref="SourcePath" />
	///     that the output phase needs to copy into the built site. <c>false</c> for
	///     absolute-URL assets (which are served from wherever the URL points).
	/// </summary>
	public bool ShouldCopy => !IsAbsoluteUrl && !string.IsNullOrEmpty(SourcePath);
}
