namespace Moka.Docs.Core.Theming;

/// <summary>One piece of text the default theme shows readers, with its English wording.</summary>
/// <param name="Key">The key a site overrides it by in <c>theme.options.strings</c>, such as <c>toc.title</c>.</param>
/// <param name="English">The English text. <c>{name}</c> marks a value filled in where the text is used.</param>
/// <param name="Description">Where the text shows.</param>
public sealed record ThemeStringInfo(string Key, string English, string Description);

/// <summary>
///     Every piece of text the default theme shows readers (labels, buttons, messages and the names screen
///     readers announce), with its English wording and the built-in translations. A site picks a translation
///     with <c>site.language</c> and changes any text with <c>theme.options.strings</c>; <see cref="ThemeText" />
///     puts the two together.
/// </summary>
/// <remarks>
///     A key ending in <c>.one</c> or <c>.other</c> is one plural form of a counted text: the <c>.one</c> form
///     for a count of one, <c>.other</c> for the rest. Overrides may add the <c>.zero</c>, <c>.two</c>,
///     <c>.few</c> and <c>.many</c> forms a language needs; the theme's script picks them with the browser's
///     plural rules.
/// </remarks>
public static partial class ThemeStrings
{
	/// <summary>The plural forms an override may give a counted text, beyond the built-in <c>one</c> and <c>other</c>.</summary>
	public static IReadOnlyList<string> PluralForms { get; } = ["zero", "one", "two", "few", "many", "other"];

	/// <summary>
	///     Every text the theme shows, in the order the documentation lists them. Style, color, code style
	///     and Layout section names follow the appearance data in this namespace.
	/// </summary>
	public static IReadOnlyList<ThemeStringInfo> All { get; } = BuildCatalog();

	/// <summary>Every key of <see cref="All" />.</summary>
	public static IReadOnlyList<string> Keys { get; } = [.. All.Select(s => s.Key)];

	private static readonly Dictionary<string, ThemeStringInfo> _byKey = All.ToDictionary(s => s.Key, StringComparer.Ordinal);

	/// <summary>
	///     The languages with a built-in translation, as <c>site.language</c> names them: Spanish, French,
	///     German, Brazilian Portuguese, Japanese and simplified Chinese. They are starting points that a site
	///     can correct key by key.
	/// </summary>
	public static IReadOnlyList<string> BuiltInLanguages { get; } = ["es", "fr", "de", "pt", "ja", "zh-Hans"];

	/// <summary>The text with key <paramref name="key" />, or <c>null</c> for a key the theme doesn't have.</summary>
	/// <param name="key">A key such as <c>toc.title</c>; case matters.</param>
	/// <returns>The entry, or <c>null</c>.</returns>
	public static ThemeStringInfo? Find(string key) => _byKey.GetValueOrDefault(key);

	/// <summary>
	///     Whether <paramref name="key" /> is a key <c>theme.options.strings</c> can set: a key of <see cref="All" />,
	///     or another plural form (<see cref="PluralForms" />) of a counted text.
	/// </summary>
	/// <param name="key">A key from the site's configuration.</param>
	/// <returns><c>true</c> when the theme uses the key.</returns>
	public static bool IsKnown(string key)
	{
		if (_byKey.ContainsKey(key))
		{
			return true;
		}

		int dot = key.LastIndexOf('.');
		return dot > 0 && PluralForms.Contains(key[(dot + 1)..]) && _byKey.ContainsKey(key[..dot] + ".other");
	}

	/// <summary>
	///     The built-in translation of one language from <see cref="BuiltInLanguages" />, keyed like
	///     <see cref="All" />. A key it leaves out falls back to English.
	/// </summary>
	/// <param name="language">A name from <see cref="BuiltInLanguages" />, in any case.</param>
	/// <returns>The translated texts, or an empty dictionary for a language without a translation.</returns>
	public static IReadOnlyDictionary<string, string> Translation(string language) =>
		language.ToLowerInvariant() switch
		{
			"es" => _spanish,
			"fr" => _french,
			"de" => _german,
			"pt" => _portuguese,
			"ja" => _japanese,
			"zh-hans" => _chinese,
			_ => new Dictionary<string, string>()
		};

	private static List<ThemeStringInfo> BuildCatalog()
	{
		List<ThemeStringInfo> catalog =
		[
			new("a11y.skipToContent", "Skip to content", "The first link on every page, for keyboard users"),
			new("nav.label", "Documentation", "Name of the sidebar for screen readers"),
			new("nav.toggleSection", "Toggle {label}", "Button that opens or closes a sidebar section or a table of contents entry; {label} is its title"),
			new("nav.toggleNavigation", "Toggle navigation", "Button that opens the sidebar on phones"),
			new("nav.toggleSidebar", "Toggle sidebar", "Button that hides the sidebar on wide screens (sidebarToggle)"),
			new("nav.sections", "Sections", "Name of the tabs row (layout.navbar: tabs) for screen readers"),
			new("breadcrumbs.label", "Breadcrumb", "Name of the breadcrumbs for screen readers"),
			new("breadcrumbs.home", "Home", "The first breadcrumb"),
			new("banner.label", "Announcement", "Name of the banner for screen readers"),
			new("banner.learnMore", "Learn more", "Banner link when banner.linkText isn't set"),
			new("banner.dismiss", "Dismiss announcement", "Button that closes the banner"),
			new("version.label", "Version", "Read before the version on the version menu button"),
			new("version.latest", "latest", "Badge on the default version"),
			new("version.prerelease", "pre", "Badge on a prerelease version"),
			new("search.button", "Search", "Search button in the header"),
			new("search.shortcut", "Ctrl K", "Shortcut on the search button, except on macOS and iOS"),
			new("search.label", "Search documentation", "Title of the search dialog and label of its field"),
			new("search.placeholder", "Search documentation...", "Search field before anything is typed"),
			new("search.close", "Close search", "Button that closes the search dialog"),
			new("search.results", "Search results", "Name of the result list for screen readers"),
			new("search.prompt", "Type to search.", "Search dialog before anything is typed"),
			new("search.loading", "Loading the search index...", "Search dialog while the index downloads"),
			new("search.unavailable", "Search is unavailable.", "Search dialog when the index can't be loaded"),
			new("search.noResults", "No results for \"{query}\".", "Search dialog when nothing matches; {query} is what was typed"),
			new("search.resultCount.one", "{count} result", "Read by screen readers after a search; {count} is the number of results"),
			new("search.resultCount.other", "{count} results", "The same, for any other count"),
			new("search.categoryDocs", "Documentation", "Category of a docs page in search results"),
			new("search.categoryBlog", "Blog", "Category of a blog page in search results"),
			new("appearance.darkMode", "Dark mode", "Dark mode button in the header"),
			new("appearance.title", "Appearance", "Appearance button and panel title"),
			new("appearance.close", "Close appearance settings", "Button that closes the Appearance panel"),
			new("appearance.mode", "Mode", "Appearance panel: light, dark or system"),
			new("appearance.light", "Light", "Appearance panel: light mode, and the light code themes"),
			new("appearance.dark", "Dark", "Appearance panel: dark mode, and the dark code themes"),
			new("appearance.system", "System", "Appearance panel: follow the system's mode"),
			new("appearance.style", "Style", "Appearance panel section"),
			new("appearance.color", "Color", "Appearance panel section"),
			new("appearance.codeTheme", "Code theme", "Appearance panel section"),
			new("appearance.codeStyle", "Code style", "Appearance panel section"),
			new("appearance.textSize", "Text size", "Appearance panel section"),
			new("appearance.readingWidth", "Reading width", "Appearance panel section"),
			new("appearance.width.narrow", "Narrow", "Appearance panel: reading width"),
			new("appearance.width.default", "Default", "Appearance panel: reading width"),
			new("appearance.width.wide", "Wide", "Appearance panel: reading width"),
			new("appearance.width.wider", "Wider", "Appearance panel: reading width"),
			new("appearance.width.full", "Full", "Appearance panel: reading width"),
			new("appearance.layout", "Layout", "Appearance panel section (layoutSelector)"),
			new("appearance.siteDefault", "{label} (default)", "Layout section: the site's own choice; {label} is its name"),
			new("appearance.reset", "Reset to site defaults", "Button at the end of the Appearance panel"),
			new("page.copy", "Copy page", "Button that copies the page's Markdown"),
			new("page.viewMarkdown", "View as Markdown", "Link to the page's Markdown"),
			new("page.edit", "Edit this page", "Edit link (editLink)"),
			new("page.lastUpdated", "Last updated: {date}", "Under the page; {date} is the date written out in the site's language"),
			new("page.contributors", "Contributors:", "Before the list of contributors (showContributors)"),
			new("page.backToTop", "Back to top", "Floating button"),
			new("toc.title", "On this page", "Title of the table of contents"),
			new("toc.top", "Top", "Link at the top of the table of contents"),
			new("toc.toggle", "Toggle table of contents", "Button that opens the table of contents on narrow screens"),
			new("toc.position.one", "Section {index} of {count}", "Under the table of contents; {index} is the current section, {count} how many there are"),
			new("toc.position.other", "Section {index} of {count}", "The same, for any other count"),
			new("pageNav.label", "Previous and next pages", "Name of the previous and next links for screen readers"),
			new("pageNav.previous", "Previous", "Above the previous page's title"),
			new("pageNav.next", "Next", "Above the next page's title"),
			new("pageNav.newerPost", "Newer post", "Above the newer post's title on a blog post"),
			new("pageNav.olderPost", "Older post", "Above the older post's title on a blog post"),
			new("feedback.prompt", "Was this page helpful?", "Feedback widget"),
			new("feedback.yes", "Yes", "Feedback widget button"),
			new("feedback.no", "No", "Feedback widget button"),
			new("feedback.yesLabel", "Yes, this page was helpful", "Name of the Yes button for screen readers"),
			new("feedback.noLabel", "No, this page was not helpful", "Name of the No button for screen readers"),
			new("feedback.thanks", "Thanks for your feedback!", "Feedback widget after a vote"),
			new("footer.builtWith", "Built with {mokadocs}", "Footer; {mokadocs} is the MokaDocs link and version"),
			new("footer.builtWithLove", "Built with {heart} using {mokadocs}", "Home page footer; {heart} is a heart"),
			new("footer.label", "Footer", "Name of the footer link columns (layout.footer: columns) for screen readers"),
			new("footer.docs", "Docs", "Footer column of top-level pages when the sidebar has no sections"),
			new("footer.more", "More", "Footer column of top-level pages after the section columns"),
			new("landing.getStarted", "Get Started", "Home page button, and a call to action without linkText"),
			new("landing.viewOnGithub", "View on GitHub", "Home page button"),
			new("landing.search", "Search the documentation", "Search box on the portal home page"),
			new("landing.sections", "Documentation sections", "Name of the portal home page's section cards for screen readers"),
			new("landing.keyFigures", "Key figures", "Name of the showcase home page's figures for screen readers"),
			new("code.copy", "Copy", "Copy button on code blocks and install commands"),
			new("code.copyLabel", "Copy code", "Name of a code block's Copy button for screen readers"),
			new("code.copied", "Copied", "Code block Copy button after copying"),
			new("code.failed", "Failed", "Code block Copy button when copying failed"),
			new("code.label", "{language} code", "Name of a code block for screen readers; {language} is its language"),
			new("code.labelPlain", "Code", "Name of a code block without a language for screen readers"),
			new("copy.copied", "Copied!", "Copy page and install buttons after copying"),
			new("copy.failed", "Copy failed", "Copy buttons when copying failed"),
			new("heading.anchor", "Link to {heading}", "Name of a heading's # link for screen readers; {heading} is the heading"),
			new("image.zoom", "Zoom image", "Name of a zoomable image without alt text for screen readers (imageZoom)"),
			new("image.zoomNamed", "Zoom image: {alt}", "The same for an image with alt text"),
			new("image.dialog", "Image", "Name of the zoomed image for screen readers"),
			new("image.dialogNamed", "Image: {alt}", "The same for an image with alt text"),
			new("image.close", "Close image", "Button that closes the zoomed image"),
			new("install.nugetLink", "View {package} on NuGet.org", "NuGet link of the install widget; {package} is the package ID"),
			new("install.with", "Install {package} with", "Name of the install widget's tabs for screen readers"),
			new("install.packageManager", "Package Manager", "Install widget tab"),
			new("install.copyLabel", "Copy to clipboard", "Name of the install widget's Copy button for screen readers"),
			new("admonition.note", "Note", "Title of a note block without its own"),
			new("admonition.tip", "Tip", "Title of a tip block without its own"),
			new("admonition.info", "Info", "Title of an info block without its own"),
			new("admonition.warning", "Warning", "Title of a warning block without its own"),
			new("admonition.caution", "Caution", "Title of a caution block without its own"),
			new("admonition.danger", "Danger", "Title of a danger block without its own"),
			new("admonition.important", "Important", "Title of an important block without its own"),
			new("details.summary", "Details", "Title of a :::details block without its own"),
			new("fileTree.folder", ", folder", "Read by screen readers after a folder's name in a :::file-tree"),
			new("video.title", "Video", "Name of a :::video without a title, for screen readers"),
			new("video.youtube", "YouTube video {id}", "The same for a YouTube video; {id} is the video's ID"),
			new("video.vimeo", "Vimeo video {id}", "The same for a Vimeo video"),
			new("video.download", "Download the video: {title}", "Link inside a :::video for browsers that can't play it"),
			new("api.title", "API Reference", "Title of the API index page, and the category of API pages in search results"),
			new("api.name", "Name", "API index table header"),
			new("api.kind", "Kind", "API index table header"),
			new("api.description", "Description", "API index table header"),
			new("blog.readMore", "Read more", "Link under a post on the blog's index"),
			new("blog.readMoreAbout", "about {title}", "Read by screen readers after Read more; {title} is the post's title"),
			new("blog.noPosts", "No posts yet.", "Blog index or tag page without posts"),
			new("blog.readingTime.one","{count} min read", "Reading time of a post; {count} is minutes"),
			new("blog.readingTime.other", "{count} min read", "The same, for any other count"),
			new("blog.draft", "Draft", "Marks a draft post when drafts are shown"),
			new("blog.by", "By", "Read by screen readers before a post's authors"),
			new("blog.and", "and", "Between a post's last two authors"),
			new("blog.tags", "Tags", "Title of the tags page, and the name of a post's tags for screen readers"),
			new("blog.tagsDescription", "Every tag used in {blog}", "Description of the tags page; {blog} is the blog's title"),
			new("blog.noTags", "No tags yet.", "Tags page of a blog without tags"),
			new("blog.allTags", "All tags", "Link on a tag page"),
			new("blog.postCount.one", "{count} post", "Number of posts with a tag"),
			new("blog.postCount.other", "{count} posts", "The same, for any other count"),
			new("blog.taggedTitle", "Posts tagged \"{tag}\"", "Title of a tag page; {tag} is the tag"),
			new("blog.taggedDescription", "Posts tagged {tag} in {blog}", "Description of a tag page"),
			new("blog.pages", "Pages", "Name of the blog's page links for screen readers"),
			new("blog.newerPosts", "Newer posts", "Blog page link"),
			new("blog.olderPosts", "Older posts", "Blog page link"),
			new("blog.page", "Page {page} of {count}", "Between the blog's page links"),
			new("blog.pageTitle", "{title} (page {page})", "Title of the blog's later pages; {title} is the blog's or the tag page's title"),
			new("notFound.title", "Page not found", "Title of the 404 page"),
			new("notFound.text", "There's no page at this address. It may have moved, or the link may be mistyped.", "404 page"),
			new("notFound.home", "Go to the home page", "404 page link"),
			new("notFound.search", "search the docs", "404 page link that opens search"),
			new("notFound.links", "{home}, or {search}.", "404 page: {home} and {search} are the two links"),
			new("notFound.linksNoSearch", "{home}.", "404 page without search: {home} is the home page link"),
			new("redirect.section", "This section starts at {link}.", "Page a section folder redirects from; {link} links its first page"),
			new("redirect.page", "Redirecting to {link}...", "Page an address redirects from (blog page folders)"),
			new("building.title", "Building the documentation", "ASP.NET Core host: title of the page shown until the first build is done"),
			new("building.text", "The documentation is being built. This page reloads in a few seconds.", "ASP.NET Core host: that page")
		];

		foreach (ThemeStyleInfo style in ThemeStyles.All)
		{
			catalog.Add(new ThemeStringInfo("style." + style.Name, style.Label, "Appearance panel: name of a style"));
			catalog.Add(new ThemeStringInfo("style." + style.Name + ".description", style.Description, "Appearance panel: under the style's name"));
		}

		foreach (ColorPresetInfo preset in ColorPresets.All)
		{
			catalog.Add(new ThemeStringInfo("color." + preset.Name, preset.Label, "Appearance panel: name of a color"));
		}

		foreach (CodeStyleInfo codeStyle in CodeStyles.All)
		{
			catalog.Add(new ThemeStringInfo("codeStyle." + codeStyle.Name, codeStyle.Label, "Appearance panel: name of a code style"));
		}

		var variants = new SortedSet<string>(StringComparer.Ordinal);
		foreach (ThemeLayoutPart part in ThemeLayouts.ReaderParts)
		{
			catalog.Add(new ThemeStringInfo("layout." + part.Key, part.Label, "Appearance panel, Layout section: name of a part of the page"));
			foreach (string name in part.Names)
			{
				if (ThemeLayoutLabels.HasOwnVariantLabel(part.Key, name))
				{
					catalog.Add(new ThemeStringInfo("layout." + part.Key + "." + name, part.LabelOf(name),
						"Appearance panel, Layout section: a choice for this part only"));
				}
				else
				{
					variants.Add(name);
				}
			}
		}

		foreach (string name in variants)
		{
			catalog.Add(new ThemeStringInfo("layout.variant." + name, ThemeLayoutLabels.Variant("*", name),
				"Appearance panel, Layout section: a choice"));
		}

		return catalog;
	}

	/// <summary>
	///     The key of the Layout section's name for a variant of a part: a part-only key such as
	///     <c>layout.sidebarScope.section</c> when the part has its own wording, else <c>layout.variant.name</c>.
	/// </summary>
	/// <param name="part">The layout part.</param>
	/// <param name="variant">One of the part's variant names.</param>
	/// <returns>A key of <see cref="All" /> for a reader part's variant.</returns>
	public static string LayoutVariantKey(ThemeLayoutPart part, string variant)
	{
		ArgumentNullException.ThrowIfNull(part);
		return ThemeLayoutLabels.HasOwnVariantLabel(part.Key, variant)
			? "layout." + part.Key + "." + variant
			: "layout.variant." + variant;
	}
}
