using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension for admonition blocks (note, tip, warning, danger, info).
///     Uses the <c>::: type</c> syntax.
/// </summary>
public sealed class AdmonitionExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<AdmonitionParser>())
		{
			pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new AdmonitionParser());
		}
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer &&
		    !htmlRenderer.ObjectRenderers.Contains<AdmonitionRenderer>())
		{
			htmlRenderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new AdmonitionRenderer());
		}
	}
}

/// <summary>
///     Represents an admonition block in the AST.
/// </summary>
public sealed class AdmonitionBlock : ContainerBlock
{
	/// <summary>Creates a new admonition block.</summary>
	public AdmonitionBlock(BlockParser parser) : base(parser)
	{
	}

	/// <summary>The admonition type (note, tip, warning, danger, info).</summary>
	public string AdmonitionType { get; set; } = "note";

	/// <summary>Optional custom title. If null, uses the type name as title.</summary>
	public string? Title { get; set; }
}

/// <summary>
///     Parses <c>::: type [title]</c> blocks.
/// </summary>
public sealed class AdmonitionParser : BlockParser
{
	private static readonly HashSet<string> _validTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"note", "tip", "warning", "danger", "info", "caution", "important"
	};

	/// <summary>Creates a new admonition parser.</summary>
	public AdmonitionParser()
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

		// Must start with :::
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

		if (string.IsNullOrEmpty(remaining))
		{
			return BlockState.None;
		}

		// Parse type and optional title
		int spaceIndex = remaining.IndexOf(' ');
		string type;
		string? title = null;

		if (spaceIndex > 0)
		{
			type = remaining[..spaceIndex];
			title = remaining[(spaceIndex + 1)..].Trim();
			if (string.IsNullOrEmpty(title))
			{
				title = null;
			}
		}
		else
		{
			type = remaining;
		}

		if (!_validTypes.Contains(type))
		{
			return BlockState.None;
		}

		var block = new AdmonitionBlock(this)
		{
			AdmonitionType = type.ToLowerInvariant(),
			Title = title,
			Span = new SourceSpan(start, line.End),
			Column = processor.Column
		};

		processor.NewBlocks.Push(block);
		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
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

			// Restore if not closing
			processor.Line = saved;
		}

		return BlockState.Continue;
	}
}

/// <summary>
///     Renders admonition blocks as styled HTML.
/// </summary>
public sealed class AdmonitionRenderer : HtmlObjectRenderer<AdmonitionBlock>
{
	private static readonly Dictionary<string, string> _icons = new(StringComparer.OrdinalIgnoreCase)
	{
		["note"] = "📝",
		["tip"] = "💡",
		["warning"] = "⚠️",
		["danger"] = "🚨",
		["info"] = "ℹ️",
		["caution"] = "⚠️",
		["important"] = "❗"
	};

	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, AdmonitionBlock block)
	{
		string type = block.AdmonitionType;
		string title = block.Title ?? char.ToUpper(type[0]) + type[1..];
		string icon = _icons.GetValueOrDefault(type, "📝");

		renderer.EnsureLine();
		renderer.Write($"<div class=\"admonition admonition-{type}\">");
		renderer.WriteLine();
		renderer.Write($"<div class=\"admonition-title\"><span class=\"admonition-icon\">{icon}</span>{title}</div>");
		renderer.WriteLine();
		renderer.Write("<div class=\"admonition-content\">");
		renderer.WriteLine();

		renderer.WriteChildren(block);

		renderer.Write("</div>");
		renderer.WriteLine();
		renderer.Write("</div>");
		renderer.WriteLine();
	}
}
