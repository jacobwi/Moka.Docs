using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension for changelog blocks.
///     Uses the <c>::: changelog</c> syntax to render a rich timeline UI.
/// </summary>
public sealed class ChangelogExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<ChangelogParser>())
			// Insert at position 0 so it runs before CustomContainerParser
		{
			pipeline.BlockParsers.Insert(0, new ChangelogParser());
		}
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			if (!htmlRenderer.ObjectRenderers.Contains<ChangelogRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new ChangelogRenderer());
			}
		}
	}
}

#region AST Block

/// <summary>
///     Represents a changelog container block in the Markdown AST.
///     Contains the raw lines between :::changelog and the closing :::.
/// </summary>
public sealed class ChangelogBlock(BlockParser parser) : LeafBlock(parser)
{
	/// <summary>Raw lines of content inside the :::changelog block.</summary>
	public List<string> RawLines { get; } = new();
}

#endregion

#region Parser

/// <summary>
///     Parses <c>::: changelog</c> blocks. Collects all inner lines as raw text
///     so the renderer can parse version entries, types, categories, and items.
/// </summary>
public sealed class ChangelogParser : BlockParser
{
	public ChangelogParser()
	{
		OpeningCharacters = [':'];
	}

	/// <inheritdoc />
	public override BlockState TryOpen(BlockProcessor processor)
	{
		if (processor.IsCodeIndent)
		{
			return BlockState.None;
		}

		StringSlice line = processor.Line;
		int start = line.Start;

		if (line.CurrentChar != ':')
		{
			return BlockState.None;
		}

		int colons = MarkdigHelpers.CountAndSkipChar(ref line, ':');
		if (colons < 3)
		{
			return BlockState.None;
		}

		line.TrimStart();
		string remaining = line.ToString().Trim();

		if (!string.Equals(remaining, "changelog", StringComparison.OrdinalIgnoreCase))
		{
			return BlockState.None;
		}

		var block = new ChangelogBlock(this)
		{
			Span = new SourceSpan(start, line.End),
			Column = processor.Column
		};

		processor.NewBlocks.Push(block);
		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		if (block is not ChangelogBlock changelogBlock)
		{
			return BlockState.None;
		}

		StringSlice line = processor.Line;

		// Check for closing :::
		if (line.CurrentChar == ':')
		{
			StringSlice saved = line;
			int colons = MarkdigHelpers.CountAndSkipChar(ref line, ':');
			if (colons >= 3)
			{
				string after = line.ToString().Trim();
				if (string.IsNullOrEmpty(after))
				{
					block.UpdateSpanEnd(line.End);
					return BlockState.BreakDiscard;
				}
			}

			processor.Line = saved;
		}

		// Collect the raw line
		changelogBlock.RawLines.Add(processor.Line.ToString());
		return BlockState.ContinueDiscard;
	}
}

#endregion

#region Data Model

/// <summary>
///     A single release entry parsed from the changelog block.
/// </summary>
internal sealed class ChangelogEntry
{
	public string Version { get; set; } = "";
	public string? Date { get; set; }
	public string Type { get; set; } = "patch"; // major, minor, patch, initial
	public List<ChangelogCategory> Categories { get; } = new();
}

/// <summary>
///     A category of changes within a release (e.g., Added, Fixed, Breaking).
/// </summary>
internal sealed class ChangelogCategory
{
	public string Name { get; set; } = "";
	public List<string> Items { get; } = new();
}

#endregion

#region Renderer

/// <summary>
///     Renders a <see cref="ChangelogBlock" /> as a rich timeline HTML structure.
///     Parses the raw lines to extract version entries, types, categories, and items.
/// </summary>
public sealed class ChangelogRenderer : HtmlObjectRenderer<ChangelogBlock>
{
	private static readonly Regex VersionHeaderRegex = new(
		@"^##\s+v?(\S+?)(?:\s*(?:—|-)\s*(.+))?$",
		RegexOptions.Compiled);

	private static readonly Regex TypeLineRegex = new(
		@"^type:\s*(\w+)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	private static readonly Regex CategoryHeaderRegex = new(
		@"^###\s+(.+)$",
		RegexOptions.Compiled);

	private static readonly Regex ListItemRegex = new(
		@"^[-*]\s+(.+)$",
		RegexOptions.Compiled);

	private static readonly Dictionary<string, (string CssClass, string Icon, string Label)> CategoryInfo =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["Added"] = ("changelog-added", "\u271a", "Added"),
			["Changed"] = ("changelog-changed", "\u270e", "Changed"),
			["Fixed"] = ("changelog-fixed", "\ud83d\udd27", "Fixed"),
			["Breaking"] = ("changelog-breaking", "\u26a0", "Breaking"),
			["Deprecated"] = ("changelog-deprecated", "\u26a1", "Deprecated"),
			["Removed"] = ("changelog-removed", "\u2715", "Removed"),
			["Security"] = ("changelog-security", "\ud83d\udee1", "Security")
		};

	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, ChangelogBlock block)
	{
		List<ChangelogEntry> entries = ParseEntries(block.RawLines);

		renderer.EnsureLine();
		renderer.Write("<div class=\"changelog\">");
		renderer.WriteLine();

		foreach (ChangelogEntry entry in entries)
		{
			WriteEntry(renderer, entry);
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}

	private static List<ChangelogEntry> ParseEntries(List<string> lines)
	{
		var entries = new List<ChangelogEntry>();
		ChangelogEntry? current = null;
		ChangelogCategory? currentCategory = null;

		foreach (string rawLine in lines)
		{
			string line = rawLine.TrimEnd();

			// Check for version header: ## v2.1.0 - 2026-03-15
			Match versionMatch = VersionHeaderRegex.Match(line);
			if (versionMatch.Success)
			{
				current = new ChangelogEntry
				{
					Version = versionMatch.Groups[1].Value,
					Date = versionMatch.Groups[2].Success ? versionMatch.Groups[2].Value.Trim() : null
				};
				entries.Add(current);
				currentCategory = null;
				continue;
			}

			if (current == null)
			{
				continue;
			}

			// Check for type line: type: major
			Match typeMatch = TypeLineRegex.Match(line);
			if (typeMatch.Success)
			{
				current.Type = typeMatch.Groups[1].Value.ToLowerInvariant();
				continue;
			}

			// Check for category header: ### Added
			Match categoryMatch = CategoryHeaderRegex.Match(line);
			if (categoryMatch.Success)
			{
				string categoryName = categoryMatch.Groups[1].Value.Trim();
				currentCategory = new ChangelogCategory { Name = categoryName };
				current.Categories.Add(currentCategory);
				continue;
			}

			// Check for list item
			Match itemMatch = ListItemRegex.Match(line);
			if (itemMatch.Success && currentCategory != null)
			{
				currentCategory.Items.Add(itemMatch.Groups[1].Value);
			}
		}

		return entries;
	}

	private static void WriteEntry(HtmlRenderer renderer, ChangelogEntry entry)
	{
		string escapedVersion = HttpUtility.HtmlAttributeEncode(entry.Version);
		string escapedType = HttpUtility.HtmlAttributeEncode(entry.Type);

		renderer.Write(
			$"<div class=\"changelog-entry\" data-version=\"{escapedVersion}\" data-type=\"{escapedType}\">");
		renderer.WriteLine();

		// Timeline column
		renderer.Write("<div class=\"changelog-timeline\">");
		renderer.Write("<div class=\"changelog-dot\"></div>");
		renderer.Write("<div class=\"changelog-line\"></div>");
		renderer.Write("</div>");
		renderer.WriteLine();

		// Content column
		renderer.Write("<div class=\"changelog-content\">");
		renderer.WriteLine();

		// Header
		renderer.Write("<div class=\"changelog-header\">");
		renderer.Write($"<span class=\"changelog-version\">v{HttpUtility.HtmlEncode(entry.Version)}</span>");
		renderer.Write(
			$"<span class=\"changelog-badge changelog-badge-{escapedType}\">{CapitalizeFirst(entry.Type)}</span>");

		if (!string.IsNullOrEmpty(entry.Date))
		{
			string formattedDate = FormatDate(entry.Date);
			renderer.Write($"<span class=\"changelog-date\">{HttpUtility.HtmlEncode(formattedDate)}</span>");
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		// Categories
		foreach (ChangelogCategory category in entry.Categories)
		{
			WriteCategory(renderer, category);
		}

		renderer.Write("</div>"); // .changelog-content
		renderer.WriteLine();
		renderer.Write("</div>"); // .changelog-entry
		renderer.WriteLine();
	}

	private static void WriteCategory(HtmlRenderer renderer, ChangelogCategory category)
	{
		(string CssClass, string Icon, string Label) info = CategoryInfo.GetValueOrDefault(category.Name);
		string cssClass = info.CssClass ?? "changelog-other";
		string icon = info.Icon ?? "\u2022";
		string label = info.Label ?? category.Name;

		renderer.Write(
			$"<div class=\"changelog-category\" data-category=\"{HttpUtility.HtmlAttributeEncode(category.Name.ToLowerInvariant())}\">");
		renderer.WriteLine();
		renderer.Write(
			$"<h4 class=\"changelog-category-title {cssClass}\">{icon} {HttpUtility.HtmlEncode(label)}</h4>");
		renderer.WriteLine();

		if (category.Items.Count > 0)
		{
			renderer.Write("<ul>");
			renderer.WriteLine();
			foreach (string item in category.Items)
			{
				renderer.Write("<li>");
				WriteInlineMarkdown(renderer, item);
				renderer.Write("</li>");
				renderer.WriteLine();
			}

			renderer.Write("</ul>");
			renderer.WriteLine();
		}

		renderer.Write("</div>"); // .changelog-category
		renderer.WriteLine();
	}

	/// <summary>
	///     Writes inline markdown content, handling backtick code spans and basic formatting.
	/// </summary>
	private static void WriteInlineMarkdown(HtmlRenderer renderer, string text)
	{
		int i = 0;
		while (i < text.Length)
		{
			if (text[i] == '`')
			{
				// Find closing backtick
				int end = text.IndexOf('`', i + 1);
				if (end > i)
				{
					string code = text[(i + 1)..end];
					renderer.Write("<code>");
					renderer.WriteEscape(code);
					renderer.Write("</code>");
					i = end + 1;
					continue;
				}
			}

			if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
			{
				// Bold **text**
				int end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
				if (end > i)
				{
					string bold = text[(i + 2)..end];
					renderer.Write("<strong>");
					renderer.WriteEscape(bold);
					renderer.Write("</strong>");
					i = end + 2;
					continue;
				}
			}

			// Regular character — escape it
			renderer.WriteEscape(text.AsSpan(i, 1));
			i++;
		}
	}

	private static string FormatDate(string dateStr)
	{
		if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
		{
			return dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
		}

		return dateStr;
	}

	private static string CapitalizeFirst(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}

		return char.ToUpperInvariant(s[0]) + s[1..];
	}
}

#endregion
