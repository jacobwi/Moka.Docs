using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>Markup helpers shared by the component renderers.</summary>
internal static class ComponentHtml
{
	/// <summary>Writes plain text with each <c>`code`</c> span as <c>&lt;code&gt;</c>, everything escaped.</summary>
	internal static void WriteTextWithCode(HtmlRenderer renderer, string text)
	{
		int i = 0;
		while (i < text.Length)
		{
			int open = text.IndexOf('`', i);
			int close = open >= 0 ? text.IndexOf('`', open + 1) : -1;
			if (close < 0)
			{
				renderer.WriteEscape(text[i..]);
				return;
			}

			renderer.WriteEscape(text[i..open]);
			renderer.Write("<code>").WriteEscape(text[(open + 1)..close]).Write("</code>");
			i = close + 1;
		}
	}

	/// <summary>Writes a block's children except its opening-line label.</summary>
	internal static void WriteChildrenExceptLabel(HtmlRenderer renderer, ContainerBlock block)
	{
		foreach (Block child in block)
		{
			if (child is not ComponentLabelBlock)
			{
				renderer.Write(child);
			}
		}
	}

	/// <summary>
	///     Writes blocks as the content of an inline element such as <c>&lt;figcaption&gt;</c>: one paragraph
	///     as its text alone, anything else as blocks.
	/// </summary>
	internal static void WriteCaption(HtmlRenderer renderer, IReadOnlyList<Block> blocks)
	{
		if (blocks is [ParagraphBlock { Inline: not null } paragraph])
		{
			renderer.WriteLeafInline(paragraph);
			return;
		}

		foreach (Block block in blocks)
		{
			renderer.Write(block);
		}
	}

	/// <summary>The readable text of the paragraphs and headings in <paramref name="blocks" />.</summary>
	internal static string PlainText(IEnumerable<Block> blocks)
	{
		var text = new StringBuilder();
		foreach (Block block in blocks)
		{
			IEnumerable<Block> all = block is ContainerBlock container ? [container, .. container.Descendants<Block>()] : [block];
			foreach (Block inner in all)
			{
				if (inner is LeafBlock { Inline: { } inline })
				{
					if (text.Length > 0)
					{
						text.Append(' ');
					}

					InlineText.Append(text, inline, true);
				}
			}
		}

		return Regex.Replace(text.ToString(), @"\s+", " ").Trim();
	}

	internal static MarkdownDocument? DocumentOf(Block block)
	{
		Block? current = block;
		while (current is not null and not MarkdownDocument)
		{
			current = current.Parent;
		}

		return current as MarkdownDocument;
	}
}

/// <summary>Renders the inline text of a component's opening line on its own.</summary>
internal sealed class ComponentLabelRenderer : HtmlObjectRenderer<ComponentLabelBlock>
{
	protected override void Write(HtmlRenderer renderer, ComponentLabelBlock label) => renderer.WriteLeafInline(label);
}

/// <summary>Renders <c>:::details</c> as a native <c>&lt;details&gt;</c> element.</summary>
internal sealed class DetailsRenderer : HtmlObjectRenderer<DetailsBlock>
{
	protected override void Write(HtmlRenderer renderer, DetailsBlock block)
	{
		renderer.EnsureLine();
		renderer.Write(block.Open ? "<details class=\"component-details\" open>" : "<details class=\"component-details\">");
		renderer.WriteLine();
		renderer.Write("<summary>");
		if (block.FirstOrDefault() is ComponentLabelBlock label)
		{
			renderer.WriteLeafInline(label);
		}
		else
		{
			renderer.WriteEscape(ContentText.Of(block)["details.summary"]);
		}

		renderer.Write("</summary>");
		renderer.WriteLine();
		renderer.Write("<div class=\"component-details-body\">");
		renderer.WriteLine();
		ComponentHtml.WriteChildrenExceptLabel(renderer, block);
		renderer.Write("</div>");
		renderer.WriteLine();
		renderer.Write("</details>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders <c>:::faq</c>: each <c>###</c> question, with the content up to the next one as its answer, and
///     each nested <c>:::details</c> as a collapsible item. The page's first FAQ also writes FAQPage
///     structured data (JSON-LD) for every question on the page.
/// </summary>
internal sealed class FaqRenderer : HtmlObjectRenderer<FaqBlock>
{
	protected override void Write(HtmlRenderer renderer, FaqBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-faq\">");
		renderer.WriteLine();

		foreach (object unit in FaqItems.Units(block))
		{
			if (unit is FaqItems.Question { Heading: { } heading } question)
			{
				renderer.Write("<details class=\"component-details component-faq-item\">");
				renderer.WriteLine();
				renderer.Write("<summary><h3");
				if (heading.TryGetAttributes()?.Id is { Length: > 0 } id)
				{
					renderer.Write(" id=\"").WriteEscape(id).Write("\"");
				}

				renderer.Write(" class=\"component-faq-question\">");
				renderer.WriteLeafInline(heading);
				renderer.Write("</h3></summary>");
				renderer.WriteLine();
				renderer.Write("<div class=\"component-details-body\">");
				renderer.WriteLine();
				foreach (Block answer in question.Answer)
				{
					renderer.Write(answer);
				}

				renderer.Write("</div>");
				renderer.WriteLine();
				renderer.Write("</details>");
				renderer.WriteLine();
			}
			else if (unit is Block other)
			{
				renderer.Write(other);
			}
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		if (ComponentHtml.DocumentOf(block) is { } document
		    && ReferenceEquals(document.Descendants<FaqBlock>().FirstOrDefault(), block))
		{
			WriteStructuredData(renderer, document);
		}
	}

	// One FAQPage per page, holding the questions of every FAQ on it, with plain-text answers.
	private static void WriteStructuredData(HtmlRenderer renderer, MarkdownDocument document)
	{
		List<(string Name, string Answer)> questions = document.Descendants<FaqBlock>()
			.SelectMany(faq => FaqItems.Units(faq).OfType<FaqItems.Question>())
			.Select(q => (q.Name, q.AnswerText))
			.Where(q => q.Name.Length > 0 && q.AnswerText.Length > 0)
			.ToList();
		if (questions.Count == 0)
		{
			return;
		}

		using var stream = new MemoryStream();
		// The default encoder writes <, > and & as \u escapes, so the text can't close the script element.
		using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.Default }))
		{
			json.WriteStartObject();
			json.WriteString("@context", "https://schema.org");
			json.WriteString("@type", "FAQPage");
			json.WriteStartArray("mainEntity");
			foreach ((string name, string answer) in questions)
			{
				json.WriteStartObject();
				json.WriteString("@type", "Question");
				json.WriteString("name", name);
				json.WriteStartObject("acceptedAnswer");
				json.WriteString("@type", "Answer");
				json.WriteString("text", answer);
				json.WriteEndObject();
				json.WriteEndObject();
			}

			json.WriteEndArray();
			json.WriteEndObject();
		}

		renderer.Write("<script type=\"application/ld+json\">").Write(Encoding.UTF8.GetString(stream.ToArray())).Write("</script>");
		renderer.WriteLine();
	}
}

/// <summary>Splits a <see cref="FaqBlock" /> into questions and the blocks around them, in order.</summary>
internal static class FaqItems
{
	internal static List<object> Units(FaqBlock faq)
	{
		var units = new List<object>();
		Question? current = null;
		foreach (Block child in faq)
		{
			switch (child)
			{
				case HeadingBlock { Level: 3 } heading:
					current = new Question(heading, null);
					units.Add(current);
					break;
				case DetailsBlock details:
					current = null;
					units.Add(new Question(null, details));
					units.Add(details);
					break;
				default:
					if (current is null)
					{
						units.Add(child);
					}
					else
					{
						current.Answer.Add(child);
					}

					break;
			}
		}

		return units;
	}

	/// <summary>A question from a heading (rendered by the FAQ) or a details block (rendered by itself).</summary>
	internal sealed class Question(HeadingBlock? heading, DetailsBlock? details)
	{
		public HeadingBlock? Heading { get; } = heading;

		public List<Block> Answer { get; } = [];

		public string Name => Heading is not null
			? InlineText.Of(Heading.Inline).Trim()
			: details?.FirstOrDefault() is ComponentLabelBlock { Inline: { } label }
				? InlineText.Of(label).Trim()
				: "";

		public string AnswerText => ComponentHtml.PlainText(Heading is not null
			? Answer
			: details?.Where(b => b is not ComponentLabelBlock) ?? []);
	}
}

/// <summary>Renders <c>:::columns</c> as a grid of up to four columns that stacks on narrow screens.</summary>
internal sealed class ColumnsRenderer : HtmlObjectRenderer<ColumnsBlock>
{
	internal const int MaxColumns = 4;

	protected override void Write(HtmlRenderer renderer, ColumnsBlock block)
	{
		int count = Math.Clamp(block.Count, 1, MaxColumns);
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-columns\" data-columns=\"")
			.Write(count.ToString(CultureInfo.InvariantCulture)).Write("\">");
		renderer.WriteLine();
		foreach (Block child in block)
		{
			if (child is ColumnBlock)
			{
				renderer.Write(child);
				continue;
			}

			// Content written straight into the columns block is a column of its own.
			renderer.Write("<div class=\"component-column\">");
			renderer.WriteLine();
			renderer.Write(child);
			renderer.Write("</div>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}
}

/// <summary>Renders one <c>:::column</c>.</summary>
internal sealed class ColumnRenderer : HtmlObjectRenderer<ColumnBlock>
{
	protected override void Write(HtmlRenderer renderer, ColumnBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-column\">");
		renderer.WriteLine();
		renderer.WriteChildren(block);
		renderer.Write("</div>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders <c>:::file-tree</c>: nested lists, a folder with entries as a <c>&lt;details&gt;</c> that starts
///     open, an icon by file type, a comment after <c> # </c> and <c>**name**</c> highlighted.
/// </summary>
internal sealed class FileTreeRenderer : HtmlObjectRenderer<FileTreeBlock>
{
	protected override void Write(HtmlRenderer renderer, FileTreeBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-file-tree\">");
		renderer.WriteLine();
		foreach (Block child in block)
		{
			if (child is ListBlock list)
			{
				WriteList(renderer, list);
			}
			else
			{
				renderer.Write(child);
			}
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}

	private static void WriteList(HtmlRenderer renderer, ListBlock list)
	{
		renderer.Write("<ul>");
		renderer.WriteLine();
		foreach (Block child in list)
		{
			if (child is ListItemBlock item)
			{
				WriteItem(renderer, item);
			}
		}

		renderer.Write("</ul>");
		renderer.WriteLine();
	}

	private static void WriteItem(HtmlRenderer renderer, ListItemBlock item)
	{
		FileTreeEntry entry = FileTreeEntry.Parse(item.OfType<ParagraphBlock>().FirstOrDefault() is { } paragraph
			? FileTreeEntry.RawText(paragraph)
			: "");
		ListBlock? children = item.OfType<ListBlock>().FirstOrDefault();
		bool folder = entry.IsFolder || children is not null;
		string kind = entry.IsEllipsis ? "more" : folder ? "folder" : FileTreeEntry.Family(entry.Name);

		renderer.Write("<li class=\"file-tree-").Write(folder ? "folder" : "file");
		if (entry.Highlighted)
		{
			renderer.Write(" file-tree-highlight");
		}

		renderer.Write("\">");
		if (children is not null)
		{
			renderer.Write("<details open><summary class=\"file-tree-entry\">");
			WriteEntry(renderer, entry, kind, folder, ContentText.Of(item)["fileTree.folder"]);
			renderer.Write("</summary>");
			renderer.WriteLine();
			WriteList(renderer, children);
			renderer.Write("</details>");
		}
		else
		{
			renderer.Write("<span class=\"file-tree-entry\">");
			WriteEntry(renderer, entry, kind, folder, ContentText.Of(item)["fileTree.folder"]);
			renderer.Write("</span>");
		}

		renderer.Write("</li>");
		renderer.WriteLine();
	}

	private static void WriteEntry(HtmlRenderer renderer, FileTreeEntry entry, string kind, bool folder, string folderText)
	{
		if (FileTreeEntry.IconName(kind) is { } iconName && LucideIcons.Get(iconName) is { } svg)
		{
			renderer.Write("<span class=\"file-tree-icon file-tree-icon-").Write(kind).Write("\" aria-hidden=\"true\">")
				.Write(svg).Write("</span>");
		}

		renderer.Write("<span class=\"file-tree-name\">").WriteEscape(entry.Name).Write("</span>");
		if (folder)
		{
			renderer.Write("<span class=\"visually-hidden\" data-no-search>").WriteEscape(folderText).Write("</span>");
		}

		if (!string.IsNullOrEmpty(entry.Comment))
		{
			renderer.Write("<span class=\"file-tree-comment\">");
			ComponentHtml.WriteTextWithCode(renderer, entry.Comment);
			renderer.Write("</span>");
		}
	}
}

/// <summary>One line of a file tree: <c>**name/** # comment</c>.</summary>
internal sealed record FileTreeEntry(string Name, string? Comment, bool Highlighted, bool IsFolder)
{
	private static readonly Dictionary<string, string> _families = BuildFamilies();

	public bool IsEllipsis => Name is "..." or "\u2026";

	/// <summary>The entry's text as written: the paragraph's lines, inline Markdown left unparsed.</summary>
	internal static string RawText(ParagraphBlock paragraph) => paragraph.Inline is { } inline
		? InlineText.Of(inline)
		: paragraph.Lines.ToString();

	internal static FileTreeEntry Parse(string raw)
	{
		string text = raw.Replace('\r', ' ').Replace('\n', ' ').Trim();
		string? comment = null;
		int hash = text.IndexOf(" # ", StringComparison.Ordinal);
		if (hash >= 0)
		{
			comment = text[(hash + 3)..].Trim();
			text = text[..hash].Trim();
		}
		else if (text.EndsWith(" #", StringComparison.Ordinal))
		{
			text = text[..^2].Trim();
		}

		bool highlighted = false;
		if (text.Length > 4 && text.StartsWith("**", StringComparison.Ordinal) && text.EndsWith("**", StringComparison.Ordinal))
		{
			highlighted = true;
			text = text[2..^2].Trim();
		}

		if (text.Length > 2 && text[0] == '`' && text[^1] == '`')
		{
			text = text[1..^1];
		}

		text = Unescape(text);
		bool folder = text.Length > 1 && text.EndsWith('/');
		return new FileTreeEntry(folder ? text.TrimEnd('/') : text, comment, highlighted, folder);
	}

	/// <summary>The icon family of a file name: code, data, text, image, media, archive or file.</summary>
	internal static string Family(string name)
	{
		if (_families.TryGetValue(name, out string? byName))
		{
			return byName;
		}

		int dot = name.LastIndexOf('.');
		return dot >= 0 && _families.TryGetValue(name[dot..], out string? byExtension) ? byExtension : "file";
	}

	internal static string? IconName(string kind) => kind switch
	{
		"folder" => "folder",
		"code" => "file-code",
		"data" => "braces",
		"text" => "file-text",
		"image" => "image",
		"media" => "play",
		"archive" => "package",
		"more" => null,
		_ => "file"
	};

	private static string Unescape(string text)
	{
		var sb = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\\' && i + 1 < text.Length && Markdig.Helpers.CharHelper.IsAsciiPunctuation(text[i + 1]))
			{
				i++;
			}

			sb.Append(text[i]);
		}

		return sb.ToString();
	}

	private static Dictionary<string, string> BuildFamilies()
	{
		var families = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		void Add(string family, params string[] names)
		{
			foreach (string name in names)
			{
				families[name] = family;
			}
		}

		Add("code", ".cs", ".csx", ".fs", ".fsx", ".fsi", ".vb", ".razor", ".cshtml", ".js", ".mjs", ".cjs", ".jsx", ".ts",
			".mts", ".tsx", ".py", ".rb", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".h", ".cpp", ".hpp", ".cc", ".php",
			".sh", ".bash", ".zsh", ".ps1", ".psm1", ".bat", ".cmd", ".sql", ".lua", ".dart", ".scala", ".r", ".pl", ".vue",
			".svelte", ".html", ".htm", ".css", ".scss", ".sass", ".less", ".xaml", "Dockerfile", "Makefile");
		Add("data", ".json", ".jsonc", ".yaml", ".yml", ".xml", ".toml", ".ini", ".cfg", ".conf", ".config", ".csproj",
			".fsproj", ".vbproj", ".sqlproj", ".props", ".targets", ".sln", ".slnx", ".env", ".lock", ".resx", ".graphql",
			".proto", ".csv", ".tsv", ".editorconfig", ".gitignore", ".gitattributes", ".dockerignore", ".npmrc", ".nvmrc");
		Add("text", ".md", ".mdx", ".markdown", ".txt", ".rst", ".adoc", ".pdf", ".doc", ".docx", ".rtf", ".log",
			"LICENSE", "README", "CHANGELOG", "NOTICE", "AUTHORS");
		Add("image", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico", ".avif", ".bmp", ".tif", ".tiff");
		Add("media", ".mp4", ".webm", ".mov", ".m4v", ".ogv", ".mp3", ".wav", ".ogg", ".flac");
		Add("archive", ".zip", ".gz", ".tgz", ".tar", ".7z", ".rar", ".nupkg", ".snupkg", ".dll", ".exe");
		return families;
	}
}

/// <summary>
///     Renders <c>:::timeline</c> as an ordered list. A list item's leading bold text is its date; a <c>###</c>
///     heading is a date with the content up to the next one. Dates in ISO form get a <c>&lt;time&gt;</c>.
/// </summary>
internal sealed partial class TimelineRenderer : HtmlObjectRenderer<TimelineBlock>
{
	protected override void Write(HtmlRenderer renderer, TimelineBlock block)
	{
		var lead = new List<Block>();
		var entries = new List<Entry>();
		foreach (Block child in block)
		{
			switch (child)
			{
				// A list under a ### date belongs to that entry; any other list holds dated items.
				case ListBlock list when entries.Count == 0 || entries[^1].Heading is null:
					entries.AddRange(list.OfType<ListItemBlock>().Select(item => new Entry(null, item)));
					break;
				case HeadingBlock { Level: 3 } heading:
					entries.Add(new Entry(heading, null));
					break;
				default:
					if (entries.Count == 0)
					{
						lead.Add(child);
					}
					else
					{
						entries[^1].Body.Add(child);
					}

					break;
			}
		}

		renderer.EnsureLine();
		foreach (Block leadBlock in lead)
		{
			renderer.Write(leadBlock);
		}

		renderer.Write("<ol class=\"component-timeline\">");
		renderer.WriteLine();
		foreach (Entry entry in entries)
		{
			renderer.Write("<li class=\"component-timeline-item\">");
			renderer.WriteLine();
			if (entry.Heading is { } heading)
			{
				WriteHeading(renderer, heading);
				WriteBody(renderer, entry.Body, null, []);
			}
			else if (entry.Item is { } item)
			{
				WriteItem(renderer, item, entry.Body);
			}

			renderer.Write("</li>");
			renderer.WriteLine();
		}

		renderer.Write("</ol>");
		renderer.WriteLine();
	}

	private static void WriteHeading(HtmlRenderer renderer, HeadingBlock heading)
	{
		renderer.Write("<h3");
		if (heading.TryGetAttributes()?.Id is { Length: > 0 } id)
		{
			renderer.Write(" id=\"").WriteEscape(id).Write("\"");
		}

		renderer.Write(" class=\"component-timeline-date\">");
		WriteDate(renderer, InlineText.Of(heading.Inline), () => renderer.WriteLeafInline(heading));
		renderer.Write("</h3>");
		renderer.WriteLine();
	}

	private static void WriteItem(HtmlRenderer renderer, ListItemBlock item, List<Block> extra)
	{
		var blocks = item.ToList();
		if (blocks.FirstOrDefault() is ParagraphBlock { Inline: { FirstChild: EmphasisInline { DelimiterCount: 2 } date } inlines })
		{
			renderer.Write("<div class=\"component-timeline-date\">");
			WriteDate(renderer, InlineText.Of(date), () => renderer.WriteChildren(date));
			renderer.Write("</div>");
			renderer.WriteLine();
			WriteBody(renderer, [.. blocks.Skip(1), .. extra], inlines, [date]);
			return;
		}

		WriteBody(renderer, [.. blocks, .. extra], null, []);
	}

	// The rest of the first paragraph after the date, without a separator such as ":" or " - ", then the
	// item's other blocks.
	private static void WriteBody(HtmlRenderer renderer, List<Block> blocks, ContainerInline? firstLine, List<Inline> skip)
	{
		renderer.Write("<div class=\"component-timeline-body\">");
		renderer.WriteLine();
		if (firstLine is not null)
		{
			List<Inline> rest = firstLine.Where(i => !skip.Contains(i)).ToList();
			if (rest.Count > 0 && rest[0] is LiteralInline { } literal)
			{
				string trimmed = SeparatorRegex().Replace(literal.Content.ToString(), "");
				rest = rest.Skip(1).ToList();
				if (trimmed.Length > 0 || rest.Count > 0)
				{
					renderer.Write("<p>").WriteEscape(trimmed);
					foreach (Inline inline in rest)
					{
						renderer.Write(inline);
					}

					renderer.Write("</p>");
					renderer.WriteLine();
				}
			}
			else if (rest.Count > 0)
			{
				renderer.Write("<p>");
				foreach (Inline inline in rest)
				{
					renderer.Write(inline);
				}

				renderer.Write("</p>");
				renderer.WriteLine();
			}
		}

		foreach (Block block in blocks)
		{
			renderer.Write(block);
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}

	private static void WriteDate(HtmlRenderer renderer, string text, Action writeContent)
	{
		string value = text.Trim();
		if (IsoDateRegex().IsMatch(value))
		{
			renderer.Write("<time datetime=\"").WriteEscape(value).Write("\">");
			writeContent();
			renderer.Write("</time>");
		}
		else
		{
			writeContent();
		}
	}

	[GeneratedRegex(@"^\d{4}(?:-\d{2}(?:-\d{2})?)?$")]
	private static partial Regex IsoDateRegex();

	[GeneratedRegex(@"^\s*(?:[:\u2013\u2014-]\s*)?")]
	private static partial Regex SeparatorRegex();

	private sealed class Entry(HeadingBlock? heading, ListItemBlock? item)
	{
		public HeadingBlock? Heading { get; } = heading;

		public ListItemBlock? Item { get; } = item;

		public List<Block> Body { get; } = [];
	}
}

/// <summary>
///     Renders <c>:::video</c>: an <c>&lt;iframe&gt;</c> for YouTube (youtube-nocookie.com) and Vimeo (with
///     <c>dnt=1</c>), a <c>&lt;video&gt;</c> for a video file. Both load lazily and have a title.
/// </summary>
internal sealed class VideoRenderer : HtmlObjectRenderer<VideoBlock>
{
	private const string _allow = "accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";

	protected override void Write(HtmlRenderer renderer, VideoBlock block)
	{
		renderer.EnsureLine();
		VideoSource? source = VideoSource.Resolve(block.Url);
		if (source is null)
		{
			if (VideoSource.IsWebUrl(block.Url))
			{
				renderer.Write("<p><a href=\"").WriteEscapeUrl(block.Url).Write("\">").WriteEscape(block.Url).Write("</a></p>");
				renderer.WriteLine();
			}

			renderer.WriteChildren(block);
			return;
		}

		ThemeText strings = ContentText.Of(block);
		string title = block.Title
		               ?? (ComponentHtml.PlainText(block) is { Length: > 0 } text ? text : null)
		               ?? source.Title(strings);

		renderer.Write("<figure class=\"component-video\">");
		renderer.WriteLine();
		renderer.Write("<div class=\"component-video-frame\">");
		if (source.MimeType is { } type)
		{
			renderer.Write("<video controls preload=\"none\" aria-label=\"").WriteEscape(title).Write("\"");
			if (block.Poster is { Length: > 0 } poster)
			{
				renderer.Write(" poster=\"").WriteEscapeUrl(poster).Write("\"");
			}

			renderer.Write("><source src=\"").WriteEscapeUrl(source.Url).Write("\" type=\"").Write(type).Write("\">")
				.Write("<a href=\"").WriteEscapeUrl(source.Url).Write("\">").WriteEscape(strings.Format("video.download", ("title", title)))
				.Write("</a></video>");
		}
		else
		{
			renderer.Write("<iframe src=\"").WriteEscapeUrl(source.Url).Write("\" title=\"").WriteEscape(title)
				.Write("\" loading=\"lazy\" allow=\"").Write(_allow)
				.Write("\" referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>");
		}

		renderer.Write("</div>");
		renderer.WriteLine();
		if (block.Count > 0)
		{
			renderer.Write("<figcaption>");
			ComponentHtml.WriteCaption(renderer, block.ToList());
			renderer.Write("</figcaption>");
			renderer.WriteLine();
		}

		renderer.Write("</figure>");
		renderer.WriteLine();
	}
}

/// <summary>Where a <c>:::video</c> URL plays: an embed URL for YouTube or Vimeo, or a video file.</summary>
internal sealed partial record VideoSource(string Url, string DefaultTitle, string? MimeType, string? Provider = null, string? Id = null)
{
	/// <summary>The video's name for screen readers when the block gives none, in the site's language.</summary>
	internal string Title(ThemeText text) => Provider switch
	{
		"youtube" => text.Format("video.youtube", ("id", Id ?? "")),
		"vimeo" => text.Format("video.vimeo", ("id", Id ?? "")),
		_ => Id is { Length: > 0 } name ? name : text["video.title"]
	};

	private static readonly Dictionary<string, string> _fileTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		[".mp4"] = "video/mp4",
		[".m4v"] = "video/mp4",
		[".webm"] = "video/webm",
		[".ogv"] = "video/ogg",
		[".ogg"] = "video/ogg",
		[".mov"] = "video/quicktime"
	};

	internal static bool IsWebUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https";

	internal static VideoSource? Resolve(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return null;
		}

		if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
		{
			string host = uri.Host.ToLowerInvariant();
			foreach (string prefix in (string[])["www.", "m.", "music."])
			{
				if (host.StartsWith(prefix, StringComparison.Ordinal))
				{
					host = host[prefix.Length..];
				}
			}

			if (host is "youtube.com" or "youtube-nocookie.com" or "youtu.be")
			{
				return YouTube(uri, host);
			}

			if (host is "vimeo.com" or "player.vimeo.com")
			{
				return Vimeo(uri);
			}

			return File(url, uri.AbsolutePath);
		}

		// A relative or root-relative path. Any other scheme (javascript:, data:) is refused.
		int colon = url.IndexOf(':');
		int slash = url.IndexOf('/');
		return colon > 0 && (slash < 0 || colon < slash) ? null : File(url, url.Split('?', '#')[0]);
	}

	private static VideoSource? File(string url, string path)
	{
		int dot = path.LastIndexOf('.');
		if (dot < 0 || !_fileTypes.TryGetValue(path[dot..], out string? type))
		{
			return null;
		}

		string name = Uri.UnescapeDataString(path[(path.LastIndexOf('/') + 1)..]);
		return new VideoSource(url, name.Length > 0 ? name : "Video", type, null, name);
	}

	private static VideoSource? YouTube(Uri uri, string host)
	{
		string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		Dictionary<string, string> query = Query(uri);
		string? id = host == "youtu.be"
			? segments.FirstOrDefault()
			: segments switch
			{
				["watch", ..] => query.GetValueOrDefault("v"),
				[("embed" or "shorts" or "live" or "v"), var value, ..] => value,
				_ => null
			};
		if (id is null || !YouTubeIdRegex().IsMatch(id))
		{
			return null;
		}

		int start = Seconds(query.GetValueOrDefault("t") ?? query.GetValueOrDefault("start"));
		string embed = $"https://www.youtube-nocookie.com/embed/{id}" + (start > 0 ? $"?start={start}" : "");
		return new VideoSource(embed, $"YouTube video {id}", null, "youtube", id);
	}

	private static VideoSource? Vimeo(Uri uri)
	{
		string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		int index = Array.FindIndex(segments, s => s.Length > 0 && s.All(char.IsAsciiDigit));
		if (index < 0)
		{
			return null;
		}

		string id = segments[index];
		string? hash = Query(uri).GetValueOrDefault("h")
		               ?? (index + 1 < segments.Length && VimeoHashRegex().IsMatch(segments[index + 1]) ? segments[index + 1] : null);
		string embed = $"https://player.vimeo.com/video/{id}?dnt=1" + (hash is not null && VimeoHashRegex().IsMatch(hash) ? $"&h={hash}" : "");
		return new VideoSource(embed, $"Vimeo video {id}", null, "vimeo", id);
	}

	private static Dictionary<string, string> Query(Uri uri)
	{
		var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			int equals = pair.IndexOf('=');
			string key = Uri.UnescapeDataString(equals >= 0 ? pair[..equals] : pair);
			query.TryAdd(key, equals >= 0 ? Uri.UnescapeDataString(pair[(equals + 1)..]) : "");
		}

		return query;
	}

	// 90, 90s, 1m30s or 1h2m3s.
	private static int Seconds(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return 0;
		}

		if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int plain))
		{
			return plain;
		}

		Match match = DurationRegex().Match(value);
		if (!match.Success)
		{
			return 0;
		}

		int Part(string name) => match.Groups[name].Success ? int.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture) : 0;
		return Part("h") * 3600 + Part("m") * 60 + Part("s");
	}

	[GeneratedRegex("^[A-Za-z0-9_-]{6,20}$")]
	private static partial Regex YouTubeIdRegex();

	[GeneratedRegex("^[A-Za-z0-9]{6,20}$")]
	private static partial Regex VimeoHashRegex();

	[GeneratedRegex(@"^(?:(?<h>\d+)h)?(?:(?<m>\d+)m)?(?:(?<s>\d+)s)?$")]
	private static partial Regex DurationRegex();
}

/// <summary>
///     Renders <c>:::figure</c> as <c>&lt;figure&gt;</c>: the first block (an image, a diagram, a table...) and a
///     <c>&lt;figcaption&gt;</c> from the opening line and whatever follows the image.
/// </summary>
internal sealed class FigureRenderer : HtmlObjectRenderer<FigureBlock>
{
	protected override void Write(HtmlRenderer renderer, FigureBlock block)
	{
		ComponentLabelBlock? label = block.FirstOrDefault() as ComponentLabelBlock;
		List<Block> content = block.Where(b => b is not ComponentLabelBlock).ToList();
		var captionInlines = new List<Inline>();

		renderer.EnsureLine();
		renderer.Write("<figure class=\"component-figure\">");
		renderer.WriteLine();

		if (content.FirstOrDefault() is ParagraphBlock { Inline: { } inlines })
		{
			// "![alt](image.png)" and the caption on the next line are one paragraph.
			bool afterBreak = false;
			foreach (Inline inline in inlines)
			{
				if (!afterBreak && inline is LineBreakInline)
				{
					afterBreak = true;
					continue;
				}

				if (afterBreak)
				{
					captionInlines.Add(inline);
				}
				else
				{
					renderer.Write(inline);
				}
			}

			renderer.WriteLine();
		}
		else if (content.FirstOrDefault() is { } media)
		{
			renderer.Write(media);
		}

		List<Block> rest = content.Skip(1).ToList();
		if (label is not null || captionInlines.Count > 0 || rest.Count > 0)
		{
			renderer.Write("<figcaption>");
			if (label is not null)
			{
				renderer.WriteLeafInline(label);
			}

			if (captionInlines.Count > 0)
			{
				if (label is not null)
				{
					renderer.Write(" ");
				}

				foreach (Inline inline in captionInlines)
				{
					renderer.Write(inline);
				}
			}

			if (rest.Count > 0)
			{
				if (label is not null || captionInlines.Count > 0)
				{
					foreach (Block other in rest)
					{
						renderer.Write(other);
					}
				}
				else
				{
					ComponentHtml.WriteCaption(renderer, rest);
				}
			}

			renderer.Write("</figcaption>");
			renderer.WriteLine();
		}

		renderer.Write("</figure>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders <c>:::package</c> with the API index's NuGet install widget markup (Package Manager, .NET CLI
///     and PackageReference tabs with copy buttons), so the theme's styles and script apply to it.
/// </summary>
internal sealed partial class PackageRenderer : HtmlObjectRenderer<PackageBlock>
{
	private const string _nugetIcon =
		"<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6.5 17.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z\"/><path d=\"M14 14a4 4 0 1 0 0-8 4 4 0 0 0 0 8z\"/><circle cx=\"7\" cy=\"7\" r=\"2\"/></svg>";

	private const string _externalIcon =
		"<svg width=\"12\" height=\"12\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/><polyline points=\"15 3 21 3 21 9\"/><line x1=\"10\" y1=\"14\" x2=\"21\" y2=\"3\"/></svg>";

	internal static bool IsPackageId(string id) => id.Length <= 100 && PackageIdRegex().IsMatch(id);

	internal static bool IsVersion(string version) => version.Length <= 64 && VersionRegex().IsMatch(version);

	protected override void Write(HtmlRenderer renderer, PackageBlock block)
	{
		renderer.EnsureLine();
		if (IsPackageId(block.PackageId))
		{
			WriteWidget(renderer, block.PackageId, block.Version, ContentText.Of(block));
		}

		renderer.WriteChildren(block);
	}

	private static void WriteWidget(HtmlRenderer renderer, string id, string? version, ThemeText text)
	{
		string nugetUrl = "https://www.nuget.org/packages/" + id + (version is null ? "" : "/" + version);
		renderer.Write("<div class=\"package-install-widget\">");
		renderer.WriteLine();
		renderer.Write("<div class=\"install-header\"><span class=\"install-package-name\">").WriteEscape(id)
			.Write("</span><a href=\"").WriteEscapeUrl(nugetUrl)
			.Write("\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"install-nuget-link\" aria-label=\"")
			.WriteEscape(text.Format("install.nugetLink", ("package", id))).Write("\">").Write(_nugetIcon).Write("<span>NuGet</span>").Write(_externalIcon)
			.Write("</a></div>");
		renderer.WriteLine();
		renderer.Write("<div class=\"install-tabs\" role=\"tablist\" aria-label=\"").WriteEscape(text.Format("install.with", ("package", id)))
			.Write("\">");
		renderer.WriteLine();
		renderer.Write("<button class=\"install-tab active\" type=\"button\" role=\"tab\" aria-selected=\"true\" data-tab=\"pm\">")
			.WriteEscape(text["install.packageManager"]).Write("</button>");
		renderer.WriteLine();
		renderer.Write("<button class=\"install-tab\" type=\"button\" role=\"tab\" aria-selected=\"false\" data-tab=\"cli\">.NET CLI</button>");
		renderer.WriteLine();
		renderer.Write("<button class=\"install-tab\" type=\"button\" role=\"tab\" aria-selected=\"false\" data-tab=\"ref\">PackageReference</button>");
		renderer.WriteLine();
		renderer.Write("</div>");
		renderer.WriteLine();
		WritePanel(renderer, "pm", true, $"Install-Package {id}" + (version is null ? "" : $" -Version {version}"), text);
		WritePanel(renderer, "cli", false, $"dotnet add package {id}" + (version is null ? "" : $" --version {version}"), text);
		WritePanel(renderer, "ref", false, $"<PackageReference Include=\"{id}\" Version=\"{version ?? "*"}\" />", text);
		renderer.Write("</div>");
		renderer.WriteLine();
	}

	// data-highlighted keeps the build's code highlighting from wrapping the command in a code block.
	private static void WritePanel(HtmlRenderer renderer, string name, bool active, string command, ThemeText text)
	{
		renderer.Write("<div class=\"install-panel").Write(active ? " active" : "").Write("\" data-panel=\"").Write(name).Write("\">");
		renderer.Write("<pre><code data-highlighted=\"true\">").WriteEscape(command).Write("</code></pre>");
		renderer.Write("<button class=\"install-copy-btn js-only\" type=\"button\" aria-label=\"").WriteEscape(text["install.copyLabel"])
			.Write("\">").WriteEscape(text["code.copy"]).Write("</button></div>");
		renderer.WriteLine();
	}

	[GeneratedRegex("^[A-Za-z0-9_][A-Za-z0-9_.-]*$")]
	private static partial Regex PackageIdRegex();

	[GeneratedRegex(@"^[0-9][0-9A-Za-z.+*-]*$|^\*$")]
	private static partial Regex VersionRegex();
}

/// <summary>Renders <c>:::glossary</c> as a definition list of the site's glossary, sorted by term.</summary>
internal sealed class GlossaryRenderer : HtmlObjectRenderer<GlossaryBlock>
{
	protected override void Write(HtmlRenderer renderer, GlossaryBlock block)
	{
		renderer.EnsureLine();
		renderer.WriteChildren(block);
		if (block.Terms.Count == 0)
		{
			return;
		}

		var ids = new HashSet<string>(StringComparer.Ordinal);
		renderer.Write("<dl class=\"component-glossary\">");
		renderer.WriteLine();
		foreach ((string term, string definition) in block.Terms)
		{
			string id = GlossaryId(term, ids);
			renderer.Write("<dt id=\"").WriteEscape(id).Write("\">").WriteEscape(term).Write("</dt>");
			renderer.WriteLine();
			renderer.Write("<dd>");
			ComponentHtml.WriteTextWithCode(renderer, definition);
			renderer.Write("</dd>");
			renderer.WriteLine();
		}

		renderer.Write("</dl>");
		renderer.WriteLine();
	}

	// glossary-api; terms whose ids would repeat (C# and C) get -2, -3.
	internal static string GlossaryId(string term, HashSet<string> taken)
	{
		string anchor = TocGenerator.GenerateAnchorId(term);
		string id = "glossary-" + (anchor.Length > 0 ? anchor : "term");
		string unique = id;
		for (int n = 2; !taken.Add(unique); n++)
		{
			unique = $"{id}-{n}";
		}

		return unique;
	}
}
