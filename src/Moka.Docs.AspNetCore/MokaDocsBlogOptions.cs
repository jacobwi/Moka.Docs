using Moka.Docs.Core.Configuration;

namespace Moka.Docs.AspNetCore;

/// <summary>
///     The blog of an embedded docs site, the counterpart of <c>features.blog</c> in mokadocs.yaml.
///     Set <see cref="MokaDocsOptions.Blog" /> to turn it on.
/// </summary>
/// <remarks>
///     The host writes no RSS or Atom feed: feeds need absolute links, and the host does not know
///     the URL readers reach it at.
/// </remarks>
public sealed class MokaDocsBlogOptions
{
	/// <summary>
	///     Folder under <see cref="MokaDocsOptions.DocsPath" /> that holds the posts. Every Markdown
	///     file below it is a post, except its <c>index.md</c>, which introduces the index page.
	///     Default: <c>"blog"</c>.
	/// </summary>
	public string Path { get; set; } = BlogFeatureConfig.DefaultPath;

	/// <summary>
	///     Route of the blog's index page, below <see cref="MokaDocsOptions.BasePath" />. <c>null</c>
	///     (the default) uses <c>/</c> followed by <see cref="Path" />.
	/// </summary>
	public string? Route { get; set; }

	/// <summary>Title of the index page and of the blog's sidebar entry. Default: <c>"Blog"</c>.</summary>
	public string Title { get; set; } = "Blog";

	/// <summary>Text under the index page title.</summary>
	public string Description { get; set; } = "";

	/// <summary>Posts per index page. Default: 10.</summary>
	public int PostsPerPage { get; set; } = BlogFeatureConfig.DefaultPostsPerPage;

	/// <summary>Latest posts listed under the blog's sidebar entry; 0 for none. Default: 5.</summary>
	public int SidebarPosts { get; set; } = BlogFeatureConfig.DefaultSidebarPosts;

	/// <summary>Whether posts and the index page show their authors. Default: true.</summary>
	public bool ShowAuthors { get; set; } = true;

	/// <summary>
	///     Authors that a post's <c>author</c> front matter names by key, matched without regard to
	///     case. A name that is no key is shown as written.
	/// </summary>
	public Dictionary<string, BlogAuthor> Authors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
