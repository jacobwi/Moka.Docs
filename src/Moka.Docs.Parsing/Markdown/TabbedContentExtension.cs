using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension for tabbed content blocks.
///     Uses the <c>=== "Tab Title"</c> syntax.
/// </summary>
public sealed class TabbedContentExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<TabGroupParser>())
		{
			pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new TabGroupParser());
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

			if (!htmlRenderer.ObjectRenderers.Contains<TabItemRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new TabItemRenderer());
			}
		}
	}
}

/// <summary>
///     Represents a group of tabs in the AST.
/// </summary>
public sealed class TabGroupBlock : ContainerBlock
{
	/// <summary>Creates a new tab group block.</summary>
	public TabGroupBlock(BlockParser parser) : base(parser)
	{
	}

	/// <summary>Unique ID for this tab group (for linking tab headers to content).</summary>
	public string GroupId { get; set; } = "";
}

/// <summary>
///     Represents a single tab within a tab group.
/// </summary>
public sealed class TabItemBlock : ContainerBlock
{
	/// <summary>Creates a new tab item block.</summary>
	public TabItemBlock(BlockParser parser) : base(parser)
	{
	}

	/// <summary>The tab title.</summary>
	public string Title { get; set; } = "";

	/// <summary>Whether this is the first (default active) tab.</summary>
	public bool IsFirst { get; set; }
}

/// <summary>
///     Parses <c>=== "Title"</c> tab blocks.
/// </summary>
public sealed class TabGroupParser : BlockParser
{
	private static int _groupCounter;

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

		// Must start with ===
		if (line.CurrentChar != '=')
		{
			return BlockState.None;
		}

		int equals = MarkdigHelpers.CountAndSkipChar(ref line, '=');
		if (equals < 3)
		{
			return BlockState.None;
		}

		line.TrimStart();
		string remaining = line.ToString().Trim();

		// Must have a quoted title: === "Tab Title"
		string? title = ExtractQuotedTitle(remaining);
		if (title is null)
		{
			return BlockState.None;
		}

		string groupId = $"tabs-{Interlocked.Increment(ref _groupCounter)}";

		var group = new TabGroupBlock(this)
		{
			GroupId = groupId,
			Span = new SourceSpan(start, line.End),
			Column = processor.Column
		};

		var firstTab = new TabItemBlock(this)
		{
			Title = title,
			IsFirst = true,
			Span = new SourceSpan(start, line.End),
			Column = processor.Column
		};

		group.Add(firstTab);
		processor.NewBlocks.Push(group);
		processor.NewBlocks.Push(firstTab);

		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		// We handle continuation for both TabGroupBlock and TabItemBlock
		if (block is TabItemBlock)
		{
			return TryContinueTabItem(processor, block);
		}

		if (block is TabGroupBlock)
		{
			return TryContinueTabGroup(processor, block);
		}

		return BlockState.Continue;
	}

	private BlockState TryContinueTabItem(BlockProcessor processor, Block block)
	{
		StringSlice line = processor.Line;

		// Check for closing === (no title — end of tab group)
		if (line.CurrentChar == '=')
		{
			StringSlice saved = line;
			int equals = MarkdigHelpers.CountAndSkipChar(ref line, '=');
			if (equals >= 3)
			{
				string remaining = line.ToString().Trim();

				// Closing === (no title)
				if (string.IsNullOrEmpty(remaining))
				{
					block.UpdateSpanEnd(line.End);
					return BlockState.BreakDiscard;
				}

				// New tab === "Title"
				string? title = ExtractQuotedTitle(remaining);
				if (title is not null)
				{
					// Close current tab, open new one
					var tabGroup = block.Parent as TabGroupBlock;
					var newTab = new TabItemBlock(this)
					{
						Title = title,
						IsFirst = false,
						Span = new SourceSpan(line.Start, line.End),
						Column = processor.Column
					};

					tabGroup?.Add(newTab);
					processor.Close(block);
					processor.NewBlocks.Push(newTab);

					return BlockState.ContinueDiscard;
				}
			}

			processor.Line = saved;
		}

		return BlockState.Continue;
	}

	private static BlockState TryContinueTabGroup(BlockProcessor processor, Block block)
	{
		// The tab group continues as long as its child tabs continue
		return BlockState.Continue;
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

/// <summary>
///     Renders a tab group as HTML.
/// </summary>
public sealed class TabGroupRenderer : HtmlObjectRenderer<TabGroupBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, TabGroupBlock block)
	{
		string groupId = block.GroupId;

		renderer.EnsureLine();
		renderer.Write($"<div class=\"tabs\" data-tab-group=\"{groupId}\">");
		renderer.WriteLine();

		// Tab headers
		renderer.Write("<div class=\"tab-headers\" role=\"tablist\">");
		renderer.WriteLine();

		int index = 0;
		foreach (Block child in block)
		{
			if (child is TabItemBlock tab)
			{
				string active = index == 0 ? " active" : "";
				string selected = index == 0 ? "true" : "false";
				renderer.Write(
					$"<button class=\"tab-header{active}\" role=\"tab\" aria-selected=\"{selected}\" data-tab-index=\"{index}\">{tab.Title}</button>");
				renderer.WriteLine();
				index++;
			}
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		// Tab content panels
		renderer.WriteChildren(block);

		renderer.Write("</div>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders a tab item as an HTML panel.
/// </summary>
public sealed class TabItemRenderer : HtmlObjectRenderer<TabItemBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, TabItemBlock block)
	{
		string active = block.IsFirst ? " active" : "";
		string hidden = block.IsFirst ? "" : " hidden";

		renderer.EnsureLine();
		renderer.Write($"<div class=\"tab-content{active}\" role=\"tabpanel\"{hidden}>");
		renderer.WriteLine();

		renderer.WriteChildren(block);

		renderer.Write("</div>");
		renderer.WriteLine();
	}
}
