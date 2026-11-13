using System.Globalization;
using System.Text;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>A small label in running text or a heading, <c>:badge[New]{variant=success}</c>.</summary>
public sealed class BadgeInline : LeafInline
{
	/// <summary>The variants a badge can have; the first is the default.</summary>
	public static IReadOnlyList<string> Variants { get; } = ["info", "success", "warning", "danger", "neutral"];

	/// <summary>The badge's text.</summary>
	public string Text { get; set; } = "";

	/// <summary>One of <see cref="Variants" />.</summary>
	public string Variant { get; set; } = "info";
}

/// <summary>
///     A key or key combination, <c>:kbd[Ctrl+Shift+P]</c>, rendered as one <c>&lt;kbd&gt;</c> per key. Its
///     child is the text as written, which headings' ids and the table of contents use.
/// </summary>
public sealed class KbdInline : ContainerInline
{
	/// <summary>The keys, in order.</summary>
	public IReadOnlyList<string> Keys { get; set; } = [];
}

/// <summary>
///     A glossary term in running text, <c>:term[API]</c>, with the term's definition in a tooltip. Its child
///     is the text as written.
/// </summary>
public sealed class TermInline : ContainerInline
{
	/// <summary>The glossary entry to look up: <c>term="..."</c> when given, else the text.</summary>
	public string Term { get; set; } = "";

	/// <summary>
	///     The definition, filled in by <see cref="MarkdownParser" /> from the site's glossary; <c>null</c>
	///     when the glossary has no such term, and the text is then shown on its own.
	/// </summary>
	public string? Definition { get; set; }

	/// <summary>The term's number on its page, from 1, for the tooltip's id.</summary>
	public int Number { get; set; }
}

/// <summary>
///     Parses <c>:badge[text]{...}</c>, <c>:kbd[keys]</c> and <c>:term[text]{term="..."}</c>. Text in the
///     brackets is plain: a <c>]</c> in it is written <c>\]</c>.
/// </summary>
internal sealed class ComponentInlineParser : InlineParser
{
	private static readonly object _termCount = new();

	public ComponentInlineParser()
	{
		OpeningCharacters = [':'];
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		string text = slice.Text;
		int start = slice.Start;
		int end = slice.End;

		// A word right before the colon (as in "word:badge[x]") means it isn't a component.
		if (start > 0 && char.IsLetterOrDigit(text[start - 1]))
		{
			return false;
		}

		int i = start + 1;
		while (i <= end && char.IsAsciiLetterLower(text[i]))
		{
			i++;
		}

		string name = text[(start + 1)..i];
		if (name is not ("badge" or "kbd" or "term") || i > end || text[i] != '[')
		{
			return false;
		}

		int close = FindClosingBracket(text, i + 1, end);
		if (close < 0)
		{
			return false;
		}

		string content = Unescape(text[(i + 1)..close]).Trim();
		if (content.Length == 0)
		{
			return false;
		}

		int after = close + 1;
		Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase);
		if (after <= end && text[after] == '{')
		{
			int brace = ComponentHeader.FindClosingBrace(text, after, end);
			if (brace > after)
			{
				attributes = ComponentAttributes.Parse(text.AsSpan(after + 1, brace - after - 1));
				after = brace + 1;
			}
		}

		processor.GetSourcePosition(start, out int line, out int column);
		Inline inline = name switch
		{
			"badge" => Badge(processor, content, attributes, line),
			"kbd" => Kbd(content),
			_ => Term(processor, content, attributes)
		};

		inline.Span = new SourceSpan(processor.GetSourcePosition(start), processor.GetSourcePosition(after - 1));
		inline.Line = line;
		inline.Column = column;
		processor.Inline = inline;
		slice.Start = after;
		return true;
	}

	private static BadgeInline Badge(InlineProcessor processor, string content, Dictionary<string, string> attributes, int line)
	{
		string? variant = attributes.GetValueOrDefault("variant")
		                  ?? attributes.Keys.FirstOrDefault(key => BadgeInline.Variants.Contains(key, StringComparer.OrdinalIgnoreCase));
		string? known = variant is null
			? BadgeInline.Variants[0]
			: BadgeInline.Variants.FirstOrDefault(v => v.Equals(variant, StringComparison.OrdinalIgnoreCase));
		if (known is null)
		{
			MarkdownWarnings.Add(processor.Document, line,
				$":badge[{content}] has the variant '{variant}', which isn't one of {string.Join(", ", BadgeInline.Variants)}; " +
				$"it is shown as {BadgeInline.Variants[0]}");
		}

		return new BadgeInline { Text = content, Variant = known ?? BadgeInline.Variants[0] };
	}

	private static KbdInline Kbd(string content)
	{
		var kbd = new KbdInline { Keys = SplitKeys(content), IsClosed = true };
		kbd.AppendChild(new LiteralInline(content));
		return kbd;
	}

	private static TermInline Term(InlineProcessor processor, string content, Dictionary<string, string> attributes)
	{
		int number = processor.Document.GetData(_termCount) is int count ? count + 1 : 1;
		processor.Document.SetData(_termCount, number);

		var term = new TermInline
		{
			Term = attributes.GetValueOrDefault("term") is { Length: > 0 } key ? key.Trim() : content,
			Number = number,
			IsClosed = true
		};
		term.AppendChild(new LiteralInline(content));
		return term;
	}

	/// <summary>
	///     The keys of a combination split at <c>+</c>: <c>Ctrl+Shift+P</c> is three keys, and a <c>+</c> next
	///     to another one is the plus key itself, as in <c>Ctrl++</c>.
	/// </summary>
	internal static List<string> SplitKeys(string text)
	{
		string[] parts = text.Split('+');
		var keys = new List<string>();
		for (int i = 0; i < parts.Length; i++)
		{
			string part = parts[i].Trim();
			if (part.Length > 0)
			{
				keys.Add(part);
			}
			else if (keys.Count == 0 || i == parts.Length - 1 || parts[i + 1].Trim().Length == 0)
			{
				keys.Add("+");
				if (i + 1 < parts.Length && parts[i + 1].Trim().Length == 0)
				{
					i++;
				}
			}
		}

		return keys;
	}

	/// <summary>
	///     Just past the last inline component (with its attributes) between <paramref name="start" /> and
	///     <paramref name="end" />, or -1 when there is none.
	/// </summary>
	internal static int EndOfComponents(string text, int start, int end)
	{
		int last = -1;
		for (int i = start; i < end; i++)
		{
			if (text[i] != ':' || (i > 0 && char.IsLetterOrDigit(text[i - 1])))
			{
				continue;
			}

			int open = i + 1;
			while (open <= end && char.IsAsciiLetterLower(text[open]))
			{
				open++;
			}

			if (open > end || text[open] != '[' || text[(i + 1)..open] is not ("badge" or "kbd" or "term"))
			{
				continue;
			}

			int close = FindClosingBracket(text, open + 1, end);
			if (close < 0)
			{
				continue;
			}

			int after = close + 1;
			if (after <= end && text[after] == '{' && ComponentHeader.FindClosingBrace(text, after, end) is var brace and > 0)
			{
				after = brace + 1;
			}

			last = after;
			i = after - 1;
		}

		return last;
	}

	// The ']' that closes the text: a backslash escapes the next character and brackets nest. Text ends at
	// the end of the line.
	private static int FindClosingBracket(string text, int from, int end)
	{
		int depth = 0;
		for (int i = from; i <= end; i++)
		{
			switch (text[i])
			{
				case '\\':
					i++;
					break;
				case '\n' or '\r':
					return -1;
				case '[':
					depth++;
					break;
				case ']' when depth == 0:
					return i;
				case ']':
					depth--;
					break;
			}
		}

		return -1;
	}

	private static string Unescape(string text)
	{
		if (!text.Contains('\\'))
		{
			return text;
		}

		var sb = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\\' && i + 1 < text.Length && CharHelper.IsAsciiPunctuation(text[i + 1]))
			{
				i++;
			}

			sb.Append(text[i]);
		}

		return sb.ToString();
	}
}

/// <summary>
///     Reads a heading's <c>{#id .class}</c> attributes only after its last inline component, so the braces of
///     <c>:badge[New]{variant=success}</c> stay with the badge.
/// </summary>
internal sealed class HeadingAttributesAfterComponents(TryParseAttributesDelegate original)
{
	public bool TryParse(BlockProcessor processor, ref StringSlice line, IBlock block)
	{
		int after = ComponentInlineParser.EndOfComponents(line.Text, line.Start, line.End);
		if (after < 0)
		{
			return original(processor, ref line, block);
		}

		if (after > line.End)
		{
			return false;
		}

		StringSlice rest = line;
		rest.Start = after;
		if (!original(processor, ref rest, block))
		{
			return false;
		}

		line.End = rest.End;
		return true;
	}
}

/// <summary>Renders <c>:badge[...]</c> as <c>&lt;span class="component-badge component-badge-{variant}"&gt;</c>.</summary>
internal sealed class BadgeRenderer : HtmlObjectRenderer<BadgeInline>
{
	protected override void Write(HtmlRenderer renderer, BadgeInline badge)
	{
		if (!renderer.EnableHtmlForInline)
		{
			renderer.WriteEscape(badge.Text);
			return;
		}

		renderer.Write("<span class=\"component-badge component-badge-").WriteEscape(badge.Variant).Write("\">")
			.WriteEscape(badge.Text).Write("</span>");
	}
}

/// <summary>Renders <c>:kbd[Ctrl+K]</c> as <c>&lt;kbd&gt;&lt;kbd&gt;Ctrl&lt;/kbd&gt;+&lt;kbd&gt;K&lt;/kbd&gt;&lt;/kbd&gt;</c>.</summary>
internal sealed class KbdRenderer : HtmlObjectRenderer<KbdInline>
{
	protected override void Write(HtmlRenderer renderer, KbdInline kbd)
	{
		if (!renderer.EnableHtmlForInline)
		{
			renderer.WriteEscape(string.Join('+', kbd.Keys));
			return;
		}

		renderer.Write("<kbd class=\"component-kbd\">");
		for (int i = 0; i < kbd.Keys.Count; i++)
		{
			if (i > 0)
			{
				renderer.Write("+");
			}

			renderer.Write("<kbd>").WriteEscape(kbd.Keys[i]).Write("</kbd>");
		}

		renderer.Write("</kbd>");
	}
}

/// <summary>
///     Renders <c>:term[...]</c> as a button described by a tooltip that holds the definition. The tooltip
///     shows on hover and keyboard focus, a click or tap toggles it and Escape hides it (the theme's script).
/// </summary>
internal sealed class TermRenderer : HtmlObjectRenderer<TermInline>
{
	internal static string TipId(int number) => "mokadocs-term-" + number.ToString(CultureInfo.InvariantCulture);

	protected override void Write(HtmlRenderer renderer, TermInline term)
	{
		if (!renderer.EnableHtmlForInline || term.Definition is null)
		{
			renderer.WriteChildren(term);
			return;
		}

		string id = TipId(term.Number);
		renderer.Write("<span class=\"glossary-term\"><button type=\"button\" class=\"glossary-term-button\" aria-describedby=\"")
			.Write(id).Write("\">");
		renderer.WriteChildren(term);
		renderer.Write("</button><span class=\"glossary-tip\" role=\"tooltip\" id=\"").Write(id).Write("\" data-no-search>");
		ComponentHtml.WriteTextWithCode(renderer, term.Definition);
		renderer.Write("</span></span>");
	}
}
