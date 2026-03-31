using System.Text;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Markdig extension for UI components (card, steps, link-cards, code-group).
///     Uses the <c>::: type{attrs}</c> syntax.
/// </summary>
public sealed class ComponentExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		if (!pipeline.BlockParsers.Contains<ComponentParser>())
			// Insert at position 0 so it runs before CustomContainerParser
			// (which also handles ::: syntax but renders generic <div> elements)
		{
			pipeline.BlockParsers.Insert(0, new ComponentParser());
		}
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is HtmlRenderer htmlRenderer)
		{
			if (!htmlRenderer.ObjectRenderers.Contains<CardRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new CardRenderer());
			}

			if (!htmlRenderer.ObjectRenderers.Contains<StepsRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new StepsRenderer());
			}

			if (!htmlRenderer.ObjectRenderers.Contains<LinkCardsRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new LinkCardsRenderer());
			}

			if (!htmlRenderer.ObjectRenderers.Contains<CodeGroupRenderer>())
			{
				htmlRenderer.ObjectRenderers.Add(new CodeGroupRenderer());
			}
		}
	}
}

#region AST Blocks

/// <summary>
///     Represents a card component block.
/// </summary>
public sealed class CardBlock(BlockParser parser) : ContainerBlock(parser)
{
	/// <summary>Card title from the title attribute.</summary>
	public string? Title { get; set; }

	/// <summary>Optional icon name.</summary>
	public string? Icon { get; set; }

	/// <summary>Card variant: default, info, success, warning.</summary>
	public string Variant { get; set; } = "default";
}

/// <summary>
///     Represents a steps container block. Each h3 inside becomes a numbered step.
/// </summary>
public sealed class StepsBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>
///     Represents a link-cards grid container.
/// </summary>
public sealed class LinkCardsBlock(BlockParser parser) : ContainerBlock(parser);

/// <summary>
///     Represents a code-group container with tabbed code blocks.
/// </summary>
public sealed class CodeGroupBlock : ContainerBlock
{
	private static int _counter;

	public CodeGroupBlock(BlockParser parser) : base(parser)
	{
		GroupId = $"codegroup-{Interlocked.Increment(ref _counter)}";
	}

	/// <summary>Unique ID for tab linking.</summary>
	public string GroupId { get; set; }
}

#endregion

#region Parser

/// <summary>
///     Parses <c>::: card{title="..." icon="..." variant="..."}</c>, <c>::: steps</c>,
///     <c>::: link-cards</c>, and <c>::: code-group</c> blocks.
/// </summary>
public sealed class ComponentParser : BlockParser
{
	private static readonly HashSet<string> _validTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"card", "steps", "link-cards", "code-group"
	};

	public ComponentParser()
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

		if (string.IsNullOrEmpty(remaining))
		{
			return BlockState.None;
		}

		// Parse type and optional attributes: card{title="..." icon="..."}
		int braceIndex = remaining.IndexOf('{');
		string typeName;
		string? attrString = null;

		if (braceIndex > 0)
		{
			typeName = remaining[..braceIndex].Trim();
			int closeBrace = remaining.LastIndexOf('}');
			if (closeBrace > braceIndex)
			{
				attrString = remaining[(braceIndex + 1)..closeBrace];
			}
		}
		else
		{
			// Could have a space-separated rest (but for components we only use the type)
			int spaceIndex = remaining.IndexOf(' ');
			typeName = spaceIndex > 0 ? remaining[..spaceIndex] : remaining;
		}

		if (!_validTypes.Contains(typeName))
		{
			return BlockState.None;
		}

		Dictionary<string, string> attrs = ParseAttributes(attrString);

		ContainerBlock block = typeName.ToLowerInvariant() switch
		{
			"card" => new CardBlock(this)
			{
				Title = attrs.GetValueOrDefault("title"),
				Icon = attrs.GetValueOrDefault("icon"),
				Variant = attrs.GetValueOrDefault("variant", "default"),
				Span = new SourceSpan(start, line.End),
				Column = processor.Column
			},
			"steps" => new StepsBlock(this)
			{
				Span = new SourceSpan(start, line.End),
				Column = processor.Column
			},
			"link-cards" => new LinkCardsBlock(this)
			{
				Span = new SourceSpan(start, line.End),
				Column = processor.Column
			},
			"code-group" => new CodeGroupBlock(this)
			{
				Span = new SourceSpan(start, line.End),
				Column = processor.Column
			},
			_ => throw new InvalidOperationException($"Unexpected component type: {typeName}")
		};

		processor.NewBlocks.Push(block);
		return BlockState.ContinueDiscard;
	}

	/// <inheritdoc />
	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		if (block is not ContainerBlock)
		{
			return BlockState.Continue;
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

		return BlockState.Continue;
	}

	/// <summary>
	///     Parses key="value" pairs from an attribute string.
	/// </summary>
	private static Dictionary<string, string> ParseAttributes(string? attrString)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(attrString))
		{
			return result;
		}

		ReadOnlySpan<char> span = attrString.AsSpan();
		while (span.Length > 0)
		{
			span = span.TrimStart();
			if (span.Length == 0)
			{
				break;
			}

			// Find key
			int eqIndex = span.IndexOf('=');
			if (eqIndex <= 0)
			{
				break;
			}

			string key = span[..eqIndex].Trim().ToString();
			span = span[(eqIndex + 1)..].TrimStart();

			// Find value (quoted)
			if (span.Length > 0 && (span[0] == '"' || span[0] == '\''))
			{
				char quote = span[0];
				span = span[1..];
				int endQuote = span.IndexOf(quote);
				if (endQuote < 0)
				{
					break;
				}

				string value = span[..endQuote].ToString();
				result[key] = value;
				span = span[(endQuote + 1)..];
			}
			else
			{
				// Unquoted value — read until space
				int spaceIdx = span.IndexOf(' ');
				if (spaceIdx < 0)
				{
					result[key] = span.ToString();
					break;
				}

				result[key] = span[..spaceIdx].ToString();
				span = span[spaceIdx..];
			}
		}

		return result;
	}
}

#endregion

#region Renderers

/// <summary>
///     Renders a card block as styled HTML.
/// </summary>
public sealed class CardRenderer : HtmlObjectRenderer<CardBlock>
{
	private static readonly Dictionary<string, string> _iconMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["rocket"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z\"/><path d=\"m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z\"/><path d=\"M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0\"/><path d=\"M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5\"/></svg>",
		["book"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20\"/></svg>",
		["code"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"16 18 22 12 16 6\"/><polyline points=\"8 6 2 12 8 18\"/></svg>",
		["settings"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z\"/></svg>",
		["zap"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polygon points=\"13 2 3 14 12 14 11 22 21 10 12 10 13 2\"/></svg>",
		["shield"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/></svg>",
		["package"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><line x1=\"16.5\" y1=\"9.4\" x2=\"7.5\" y2=\"4.21\"/><path d=\"M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z\"/><polyline points=\"3.27 6.96 12 12.01 20.73 6.96\"/><line x1=\"12\" y1=\"22.08\" x2=\"12\" y2=\"12\"/></svg>",
		["star"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polygon points=\"12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2\"/></svg>",
		["check"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"20 6 9 17 4 12\"/></svg>",
		["globe"] =
			"<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"2\" y1=\"12\" x2=\"22\" y2=\"12\"/><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"/></svg>"
	};

	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, CardBlock block)
	{
		string variant = block.Variant;
		string variantClass = variant != "default" ? $" component-card-{variant}" : "";

		renderer.EnsureLine();
		renderer.Write($"<div class=\"component-card{variantClass}\">");
		renderer.WriteLine();

		if (!string.IsNullOrEmpty(block.Title))
		{
			renderer.Write("<div class=\"component-card-header\">");
			if (!string.IsNullOrEmpty(block.Icon) && _iconMap.TryGetValue(block.Icon, out string? svg))
			{
				renderer.Write($"<span class=\"component-card-icon\">{svg}</span>");
			}

			renderer.Write($"<span class=\"component-card-title\">{block.Title}</span>");
			renderer.Write("</div>");
			renderer.WriteLine();
		}

		renderer.Write("<div class=\"component-card-body\">");
		renderer.WriteLine();
		renderer.WriteChildren(block);
		renderer.Write("</div>");
		renderer.WriteLine();
		renderer.Write("</div>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders a steps block as numbered step-by-step HTML.
/// </summary>
public sealed class StepsRenderer : HtmlObjectRenderer<StepsBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, StepsBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-steps\">");
		renderer.WriteLine();

		int stepNumber = 0;

		foreach (Block child in block)
		{
			if (child is HeadingBlock heading && heading.Level == 3)
			{
				// Close previous step if not the first
				if (stepNumber > 0)
				{
					renderer.Write("</div>"); // close step-content
					renderer.WriteLine();
					renderer.Write("</div>"); // close step
					renderer.WriteLine();
				}

				stepNumber++;

				renderer.Write("<div class=\"component-step\">");
				renderer.WriteLine();
				renderer.Write(
					$"<div class=\"component-step-indicator\"><span class=\"component-step-number\">{stepNumber}</span></div>");
				renderer.WriteLine();
				renderer.Write("<div class=\"component-step-content\">");
				renderer.WriteLine();

				// Render heading text as step title
				renderer.Write("<h3 class=\"component-step-title\">");
				if (heading.Inline != null)
				{
					renderer.WriteChildren(heading.Inline);
				}

				renderer.Write("</h3>");
				renderer.WriteLine();
			}
			else
			{
				// If we haven't started a step yet but have content, start an implicit step
				if (stepNumber == 0)
				{
					stepNumber++;
					renderer.Write("<div class=\"component-step\">");
					renderer.WriteLine();
					renderer.Write(
						$"<div class=\"component-step-indicator\"><span class=\"component-step-number\">{stepNumber}</span></div>");
					renderer.WriteLine();
					renderer.Write("<div class=\"component-step-content\">");
					renderer.WriteLine();
				}

				// Render the child block using its registered renderer
				renderer.Write(child);
			}
		}

		// Close last step
		if (stepNumber > 0)
		{
			renderer.Write("</div>"); // close step-content
			renderer.WriteLine();
			renderer.Write("</div>"); // close step
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders a link-cards block as a responsive grid.
/// </summary>
public sealed class LinkCardsRenderer : HtmlObjectRenderer<LinkCardsBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, LinkCardsBlock block)
	{
		renderer.EnsureLine();
		renderer.Write("<div class=\"component-link-cards\">");
		renderer.WriteLine();

		// Walk through the AST to find list items with links
		foreach (Block child in block)
		{
			if (child is ListBlock list)
			{
				foreach (Block listItem in list)
				{
					if (listItem is ListItemBlock item)
					{
						RenderLinkCard(renderer, item);
					}
				}
			}
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}

	private static void RenderLinkCard(HtmlRenderer renderer, ListItemBlock item)
	{
		// Extract the link and description from the list item
		string? href = null;
		string? title = null;
		string? description = null;

		foreach (Block block in item)
		{
			if (block is ParagraphBlock para && para.Inline != null)
			{
				foreach (Inline inline in para.Inline)
				{
					if (inline is LinkInline link)
					{
						href = link.Url;
						// Get link text
						var sb = new StringBuilder();
						foreach (Inline linkChild in link)
						{
							if (linkChild is LiteralInline lit)
							{
								sb.Append(lit.Content);
							}
						}

						title = sb.ToString();
					}
					else if (inline is LiteralInline literal)
					{
						string text = literal.Content.ToString().Trim();
						// The description follows " — " or " - " after the link
						if (text.StartsWith("—") || text.StartsWith("-"))
						{
							description = text.TrimStart('—', '-', ' ');
						}
						else if (!string.IsNullOrWhiteSpace(text))
						{
							description = text;
						}
					}
				}
			}
		}

		if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
		{
			return;
		}

		renderer.Write($"<a class=\"component-link-card\" href=\"{href}\">");
		renderer.WriteLine();
		renderer.Write("<div class=\"component-link-card-content\">");
		renderer.WriteLine();
		renderer.Write($"<span class=\"component-link-card-title\">{title}</span>");
		renderer.WriteLine();
		if (!string.IsNullOrEmpty(description))
		{
			renderer.Write($"<span class=\"component-link-card-desc\">{description}</span>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();
		renderer.Write("<span class=\"component-link-card-arrow\">");
		renderer.Write(
			"<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"9 18 15 12 9 6\"/></svg>");
		renderer.Write("</span>");
		renderer.WriteLine();
		renderer.Write("</a>");
		renderer.WriteLine();
	}
}

/// <summary>
///     Renders a code-group block as a tabbed interface.
/// </summary>
public sealed class CodeGroupRenderer : HtmlObjectRenderer<CodeGroupBlock>
{
	/// <inheritdoc />
	protected override void Write(HtmlRenderer renderer, CodeGroupBlock block)
	{
		string groupId = block.GroupId;

		// Collect code blocks and their titles
		var codeBlocks = new List<(string Title, FencedCodeBlock Code)>();

		foreach (Block child in block)
		{
			if (child is FencedCodeBlock fenced)
			{
				// Try to get title from info string: ```csharp title="C#"
				string info = fenced.Info ?? "";
				string title = ExtractTitle(fenced.Arguments) ?? LanguageDisplayName(info);
				codeBlocks.Add((title, fenced));
			}
		}

		if (codeBlocks.Count == 0)
		{
			// Fallback: just render children normally
			renderer.WriteChildren(block);
			return;
		}

		renderer.EnsureLine();
		renderer.Write($"<div class=\"tabs component-code-group\" data-tab-group=\"{groupId}\">");
		renderer.WriteLine();

		// Tab headers
		renderer.Write("<div class=\"tab-headers\" role=\"tablist\">");
		renderer.WriteLine();

		for (int i = 0; i < codeBlocks.Count; i++)
		{
			string active = i == 0 ? " active" : "";
			string selected = i == 0 ? "true" : "false";
			renderer.Write(
				$"<button class=\"tab-header{active}\" role=\"tab\" aria-selected=\"{selected}\" data-tab-index=\"{i}\">{codeBlocks[i].Title}</button>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();

		// Tab content panels — render each code block in its own panel
		for (int i = 0; i < codeBlocks.Count; i++)
		{
			string activeClass = i == 0 ? " active" : "";
			string hidden = i == 0 ? "" : " hidden";
			renderer.Write($"<div class=\"tab-content{activeClass}\" role=\"tabpanel\"{hidden}>");
			renderer.WriteLine();

			// Render the fenced code block using the default code renderer
			renderer.Write(codeBlocks[i].Code);

			renderer.Write("</div>");
			renderer.WriteLine();
		}

		renderer.Write("</div>");
		renderer.WriteLine();
	}

	/// <summary>
	///     Extracts title="..." from the arguments string of a fenced code block.
	/// </summary>
	private static string? ExtractTitle(string? arguments)
	{
		if (string.IsNullOrWhiteSpace(arguments))
		{
			return null;
		}

		const string prefix = "title=";
		int idx = arguments.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return null;
		}

		string rest = arguments[(idx + prefix.Length)..];
		if (rest.Length < 2)
		{
			return null;
		}

		char quote = rest[0];
		if (quote != '"' && quote != '\'')
		{
			return null;
		}

		int endQuote = rest.IndexOf(quote, 1);
		if (endQuote < 0)
		{
			return null;
		}

		return rest[1..endQuote];
	}

	/// <summary>
	///     Returns a display-friendly language name.
	/// </summary>
	private static string LanguageDisplayName(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang))
		{
			return "Code";
		}

		return lang.ToLowerInvariant() switch
		{
			"csharp" or "cs" => "C#",
			"fsharp" or "fs" => "F#",
			"javascript" or "js" => "JavaScript",
			"typescript" or "ts" => "TypeScript",
			"python" or "py" => "Python",
			"bash" or "sh" or "shell" => "Shell",
			"xml" => "XML",
			"json" => "JSON",
			"yaml" or "yml" => "YAML",
			"html" => "HTML",
			"css" => "CSS",
			"sql" => "SQL",
			"powershell" or "ps1" => "PowerShell",
			"dockerfile" => "Dockerfile",
			"markdown" or "md" => "Markdown",
			_ => lang
		};
	}
}

#endregion
