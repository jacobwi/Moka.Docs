using System.Text;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension for tabbed content blocks.
///     Uses the <c>=== "Tab Title"</c> syntax, closed by a bare <c>===</c> line.
/// </summary>
public sealed class TabbedContentExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<TabGroupParser>())
			// Position 0: a bare === line is a setext h1 underline in CommonMark, so this
			// has to win against the paragraph parser as well as ThematicBreakParser.
		{
			pipeline.BlockParsers.Insert(0, new TabGroupParser());
		}
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			if (!htmlRenderer.ObjectRenderers.Contains<TabGroupRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new TabGroupRenderer());
			}
		}
	}
}

#region Blocks

/// <summary>
///     A group of tabs in the AST.
/// </summary>
/// <remarks>
///     Tabs are not separate container blocks. All tab content lands directly in this
///     container, and <see cref="TabStartIndices" /> records where each tab's content
///     begins. <see cref="TabGroupRenderer" /> slices the children back apart at render
///     time. This mirrors how <c>::: steps</c> and <c>::: code-group</c> work, and avoids
///     pushing sibling containers from a block parser, which Markdig does not support.
/// </remarks>
public sealed class TabGroupBlock : ContainerBlock
{
	private static int _counter;

	/// <summary>Creates a new tab group block.</summary>
	public TabGroupBlock(BlockParser parser) : base(parser)
	{
		GroupId = $"tabs-{Interlocked.Increment(ref _counter)}";
	}

	/// <summary>Unique ID for this tab group (links tab headers to content panels).</summary>
	public string GroupId { get; set; }

	/// <summary>Tab titles, in document order.</summary>
	public List<string> TabTitles { get; } = [];

	/// <summary>
	///     Index into this container's children where each tab's content starts.
	///     Always the same length as <see cref="TabTitles" />.
	/// </summary>
	public List<int> TabStartIndices { get; } = [];
}

#endregion

#region Parser

/// <summary>
///     Parses <c>=== "Title"</c> tab blocks, closed by a bare <c>===</c>.
/// </summary>
public sealed class TabGroupParser : BlockParser
{
	/// <summary>Creates a new tab group parser.</summary>
	public TabGroupParser()
	{
		OpeningCharacters = ['='];
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

		string? title = TryReadTabTitle(ref line);
		if (title is null)
		{
			// A bare === is a setext heading underline, not a tab. Leave it alone.
			return BlockState.None;
		}

		var group = new TabGroupBlock(this)
		{
			Span = new SourceSpan(start, line.End),
			Column = processor.Column
		};

		group.TabTitles.Add(title);
		group.TabStartIndices.Add(0);

		// Exactly one block is pushed. Pushing the group and a child tab together
		// corrupts the block tree and sends the processor into an infinite loop.
		processor.NewBlocks.Push(group);
		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		if (block is not TabGroupBlock group)
		{
			return BlockState.Continue;
		}

		StringSlice line = processor.Line;
		if (line.CurrentChar != '=')
		{
			return BlockState.Continue;
		}

		StringSlice saved = line;
		int equals = MarkdigHelpers.CountAndSkipChar(ref line, '=');
		if (equals >= 3)
		{
			string remaining = line.ToString().Trim();

			// Bare === closes the group.
			if (string.IsNullOrEmpty(remaining))
			{
				block.UpdateSpanEnd(line.End);
				return BlockState.BreakDiscard;
			}

			// === "Title" starts the next tab. Record where its content begins.
			string? title = ExtractQuotedTitle(remaining);
			if (title is not null)
			{
				group.TabTitles.Add(title);
				group.TabStartIndices.Add(group.Count);
				block.UpdateSpanEnd(line.End);
				return BlockState.ContinueDiscard;
			}
		}

		processor.Line = saved;
		return BlockState.Continue;
	}

	/// <summary>
	///     Reads <c>=== "Title"</c> from the start of a line, advancing past it.
	///     Returns <c>null</c> when the line is not a tab opener.
	/// </summary>
	private static string? TryReadTabTitle(ref StringSlice line)
	{
		if (line.CurrentChar != '=')
		{
			return null;
		}

		int equals = MarkdigHelpers.CountAndSkipChar(ref line, '=');
		if (equals < 3)
		{
			return null;
		}

		line.TrimStart();
		return ExtractQuotedTitle(line.ToString().Trim());
	}

	private static string? ExtractQuotedTitle(string text)
	{
		text = text.Trim();
		if (text.Length < 2)
		{
			return null;
		}

		char quote = text[0];
		if (quote != '"' && quote != '\'')
		{
			return null;
		}

		int endQuote = text.IndexOf(quote, 1);
		if (endQuote < 0)
		{
			return null;
		}

		return text[1..endQuote];
	}
}

#endregion

#region Renderer

/// <summary>
///     Renders a tab group as a header row plus one content panel per tab.
/// </summary>
public sealed class TabGroupRenderer : HtmlObjectRenderer<TabGroupBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, TabGroupBlock block)
	{
		if (block.TabTitles.Count == 0)
		{
			return;
		}

		renderer.EnsureLine();
		renderer.Write("<div class=\"tabs\" data-tab-group=\"").Write(block.GroupId).Write("\">");
		renderer.WriteLine();

		#region Headers

		renderer.Write("<div class=\"tab-headers\" role=\"tablist\">");
		renderer.WriteLine();

		for (int i = 0; i < block.TabTitles.Count; i++)
		{
			bool first = i == 0;
			renderer.Write("<button class=\"tab-header");
			if (first)
			{
				renderer.Write(" active");
			}

			renderer.Write("\" role=\"tab\" aria-selected=\"")
				.Write(first ? "true" : "false")
				.Write("\" data-tab-index=\"")
				.Write(i.ToString())
				.Write("\">");
			renderer.WriteEscape(block.TabTitles[i]);
			renderer.Write("</button>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		#endregion

		#region Panels

		for (int i = 0; i < block.TabTitles.Count; i++)
		{
			int from = block.TabStartIndices[i];
			int to = i + 1 < block.TabStartIndices.Count ? block.TabStartIndices[i + 1] : block.Count;
			bool first = i == 0;

			renderer.Write("<div class=\"tab-content");
			if (first)
			{
				renderer.Write(" active");
			}

			renderer.Write("\" role=\"tabpanel\"");
			if (!first)
			{
				renderer.Write(" hidden");
			}

			renderer.Write(">");
			renderer.WriteLine();

			for (int c = from; c < to && c < block.Count; c++)
			{
				renderer.Write(block[c]);
			}

			renderer.Write("</div>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		#endregion
	}
}

#endregion
