using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.Themes.Default;

/// <summary>
///     Writes the right-to-left counterpart of the rules in a stylesheet that place something on the left or the
///     right: each such rule again, scoped to <c>html[dir="rtl"]</c>, with left and right swapped. The styles, the
///     layout variants and the components are mirrored this way, so a change to one of their rules reaches
///     rtl.css without a second edit.
/// </summary>
/// <remarks>
///     A rule that sets one side only also clears that side in the copy: <c>border-left: 3px solid</c> becomes
///     <c>border-right: 3px solid; border-left: none</c>. A box with a border all round (a card, an admonition,
///     a details block) keeps a 1px border on the cleared side instead. Code blocks keep their left-to-right
///     layout, so rules for them are not mirrored.
/// </remarks>
internal static partial class RtlMirror
{
	private const string _rtl = "[dir=\"rtl\"]";

	// Selectors of boxes with a border on every side, whose cleared side keeps a hairline instead of none.
	private static readonly string[] _boxed =
		[".component-card", ".admonition", ".component-details", ".tabs", ".package-install-widget", ".source-viewer", ".toc-inline", "th", "td"];

	// Code and the widgets that hold code stay left to right in a right-to-left page.
	private static readonly string[] _leftToRight = ["pre", "code", ".line-numbers", ".code-", ".install-panel"];

	/// <summary>The mirrored rules of <paramref name="css" />, one per line, inside their <c>@media</c> blocks.</summary>
	public static string Mirror(string css)
	{
		var sb = new StringBuilder();
		foreach ((string? media, string selector, string body) in Rules(Comment().Replace(css, "")))
		{
			string[] selectors = [.. SplitTopLevel(selector, ',').Select(s => Whitespace().Replace(s.Trim(), " ")).Where(s => s.Length > 0)];
			if (selectors.Length == 0 || selectors.Any(KeepsLeftToRight))
			{
				continue;
			}

			List<string> declarations = MirrorDeclarations(body, selectors.Any(IsBoxed));
			if (declarations.Count == 0)
			{
				continue;
			}

			string rule = string.Join(", ", selectors.Select(Scope)) + " { " + string.Join("; ", declarations) + "; }";
			sb.Append(media is null ? rule : media + " { " + rule + " }").Append("\r\n");
		}

		return sb.ToString();
	}

	private static bool KeepsLeftToRight(string selector) =>
		_leftToRight.Any(token => Regex.IsMatch(selector, @"(^|[\s>+~(,])" + Regex.Escape(token) + (token.EndsWith('-') ? "" : @"\b")));

	// The element the rule styles is a box: its last compound names one, and it isn't a pseudo-element of it.
	private static bool IsBoxed(string selector)
	{
		string subject = SplitTopLevel(selector, ' ').Last(s => s.Length > 0);
		return !subject.Contains("::", StringComparison.Ordinal)
		       && _boxed.Any(token => Regex.IsMatch(subject, @"(^|[\s>+~(,:])" + Regex.Escape(token) + @"\b"));
	}

	// html[data-style="moka"] .x gets the direction on the html element itself; a selector without html is put under it.
	// The direction sits in :where(), so the copy has its original's specificity: it wins over the original by coming
	// later, and a rule that wins over the original in a left-to-right page still wins over the copy.
	private static string Scope(string selector)
	{
		if (selector.StartsWith("html", StringComparison.Ordinal) && (selector.Length == 4 || selector[4] is '[' or ':' or ' ' or '.'))
		{
			return "html:where(" + _rtl + ")" + selector[4..];
		}

		if (selector.StartsWith(":where(html", StringComparison.Ordinal))
		{
			return ":where(html" + _rtl + selector[":where(html".Length..];
		}

		return ":where(html" + _rtl + ") " + selector;
	}

	private static List<string> MirrorDeclarations(string body, bool boxed)
	{
		var declarations = new List<(string Property, string Value, bool Important)>();
		foreach (string part in SplitTopLevel(body, ';'))
		{
			int colon = part.IndexOf(':', StringComparison.Ordinal);
			if (colon <= 0)
			{
				continue;
			}

			string value = Whitespace().Replace(part[(colon + 1)..].Trim(), " ");
			bool important = value.EndsWith("!important", StringComparison.Ordinal);
			if (important)
			{
				value = value[..^"!important".Length].TrimEnd();
			}

			declarations.Add((part[..colon].Trim().ToLowerInvariant(), value, important));
		}

		var properties = declarations.Select(d => d.Property).ToHashSet(StringComparer.Ordinal);
		string? border = declarations.LastOrDefault(d => d.Property == "border").Value;
		string? padding = declarations.LastOrDefault(d => d.Property == "padding").Value;
		var mirrored = new List<string>();
		foreach ((string property, string value, bool important) in declarations)
		{
			string suffix = important ? " !important" : "";
			string? swapped = SwapSide(property);
			if (swapped is not null)
			{
				mirrored.Add($"{swapped}: {value}{suffix}");
				if (!properties.Contains(swapped))
				{
					mirrored.Add($"{property}: {Cleared(property, boxed, border, padding)}{suffix}");
				}
			}
			else if (property is "text-align" or "float" or "clear" && SwapWord(value) is { } side)
			{
				mirrored.Add($"{property}: {side}{suffix}");
			}
			else if (property == "border-radius" && MirrorCorners(value) is { } corners)
			{
				mirrored.Add($"{property}: {corners}{suffix}");
			}
			else if (property is "padding" or "margin" or "border-width" or "border-color" or "border-style" or "inset"
			         && SwapInlineEdges(value) is { } edges)
			{
				mirrored.Add($"{property}: {edges}{suffix}");
			}
			else if (property == "transform" && MirrorTransform(value) is { } transform)
			{
				mirrored.Add($"{property}: {transform}{suffix}");
			}
			else if (property == "box-shadow" && MirrorShadow(value) is { } shadow)
			{
				mirrored.Add($"{property}: {shadow}{suffix}");
			}
		}

		if (mirrored.Count == 0)
		{
			return mirrored;
		}

		// The copy lands after every original, so it repeats the rule's own all-sides values first: without them an
		// earlier rule's copy would win over this rule's border-color or padding, which this rule beat by coming later.
		List<string> symmetric =
		[
			.. declarations
				.Where(d => _symmetric.Contains(d.Property) && !mirrored.Any(m => m.StartsWith(d.Property + ":", StringComparison.Ordinal)))
				.Select(d => $"{d.Property}: {d.Value}{(d.Important ? " !important" : "")}")
		];
		return [.. symmetric, .. mirrored];
	}

	private static readonly HashSet<string> _symmetric =
		new(["border", "border-width", "border-style", "border-color", "padding", "margin"], StringComparer.Ordinal);

	// Each shadow's horizontal offset changes sign: an inset bar on the left goes to the right.
	private static string? MirrorShadow(string value)
	{
		bool changed = false;
		var shadows = new List<string>();
		foreach (string shadow in SplitTopLevel(value, ','))
		{
			List<string> parts = SplitTopLevel(shadow.Trim(), ' ').Where(s => s.Length > 0).ToList();
			int x = parts.FindIndex(p => NumberLiteral().IsMatch(p));
			if (x >= 0 && parts[x] is not ("0" or "0px"))
			{
				parts[x] = Negate(parts[x]);
				changed = true;
			}

			shadows.Add(string.Join(' ', parts));
		}

		return changed ? string.Join(", ", shadows) : null;
	}

	// top right bottom left: the right and left lengths trade places.
	private static string? SwapInlineEdges(string value)
	{
		List<string> v = SplitTopLevel(value, ' ').Where(s => s.Length > 0).ToList();
		return v.Count == 4 && v[1] != v[3] ? $"{v[0]} {v[3]} {v[2]} {v[1]}" : null;
	}

	// A mirrored shape turns the other way: rotate(a) becomes rotate(-a), and moves along x go the other way.
	private static string? MirrorTransform(string value)
	{
		string mirrored = TransformFunction().Replace(value, match =>
		{
			string name = match.Groups["name"].Value;
			string[] args = [.. SplitTopLevel(match.Groups["args"].Value, ',').Select(a => a.Trim())];
			if (name is "rotate" or "rotateZ" or "translateX" or "skewX" && args.Length == 1)
			{
				return $"{name}({Negate(args[0])})";
			}

			if (name is "translate" or "skew" && args.Length is 1 or 2)
			{
				return $"{name}({string.Join(", ", [Negate(args[0]), .. args.Skip(1)])})";
			}

			return match.Value;
		});
		return mirrored == value ? null : mirrored;
	}

	private static string Negate(string length)
	{
		if (length is "0" or "0deg" or "0px" or "0%")
		{
			return length;
		}

		if (NumberLiteral().IsMatch(length))
		{
			return length.StartsWith('-') ? length[1..] : "-" + length;
		}

		return length.StartsWith("calc(", StringComparison.Ordinal) ? $"calc(-1 * {length[4..]})" : $"calc(-1 * {length})";
	}

	// border-left-color -> border-right-color, left -> right, border-top-left-radius -> border-top-right-radius.
	private static string? SwapSide(string property)
	{
		string[] words = property.Split('-');
		int at = Array.FindIndex(words, w => w is "left" or "right");
		if (at < 0 || property.StartsWith("--", StringComparison.Ordinal))
		{
			return null;
		}

		words[at] = words[at] == "left" ? "right" : "left";
		return string.Join('-', words);
	}

	private static string? SwapWord(string value) => value switch
	{
		"left" => "right",
		"right" => "left",
		_ => null
	};

	private static string Cleared(string property, bool boxed, string? border, string? padding)
	{
		if (property is "left" or "right")
		{
			return "auto";
		}

		if (property.StartsWith("padding-", StringComparison.Ordinal))
		{
			return padding is null ? "0" : PaddingSide(padding, property.EndsWith("left", StringComparison.Ordinal));
		}

		if (property.StartsWith("margin-", StringComparison.Ordinal) || property.EndsWith("-radius", StringComparison.Ordinal))
		{
			return "0";
		}

		bool hairline = boxed || border is not null;
		return property switch
		{
			"border-left" or "border-right" => border ?? (boxed ? "1px solid var(--color-border)" : "none"),
			"border-left-width" or "border-right-width" => hairline ? "1px" : "0",
			"border-left-color" or "border-right-color" => hairline ? "var(--color-border)" : "transparent",
			"border-left-style" or "border-right-style" => hairline ? "solid" : "none",
			_ => "initial"
		};
	}

	// The left or right length of a padding shorthand: 1 value is all sides, 2 is block inline, 3 and 4 name right second.
	private static string PaddingSide(string padding, bool left)
	{
		string[] values = padding.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return values.Length switch
		{
			1 => values[0],
			2 or 3 => values[1],
			_ => left ? values[3] : values[1]
		};
	}

	private static string? MirrorCorners(string value)
	{
		if (value.Contains('/', StringComparison.Ordinal))
		{
			return null;
		}

		string[] v = SplitTopLevel(value, ' ').Where(s => s.Length > 0).ToArray();
		return v.Length switch
		{
			2 => $"{v[1]} {v[0]}",
			3 => $"{v[1]} {v[0]} {v[1]} {v[2]}",
			4 => $"{v[1]} {v[0]} {v[3]} {v[2]}",
			_ => null
		};
	}

	// Rules at the top level and one @media level down.
	private static IEnumerable<(string? Media, string Selector, string Body)> Rules(string css)
	{
		int i = 0;
		string? media = null;
		var head = new StringBuilder();
		while (i < css.Length)
		{
			char c = css[i];
			if (c == '{')
			{
				string prelude = head.ToString().Trim();
				head.Clear();
				if (prelude.StartsWith("@keyframes", StringComparison.Ordinal) || prelude.StartsWith("@font-face", StringComparison.Ordinal))
				{
					i = SkipBlock(css, i);
					continue;
				}

				if (prelude.StartsWith('@'))
				{
					media = Whitespace().Replace(prelude, " ");
				}
				else
				{
					int end = css.IndexOf('}', i);
					yield return (media, prelude, css[(i + 1)..end]);
					i = end;
				}
			}
			else if (c == '}')
			{
				media = null;
				head.Clear();
			}
			else
			{
				head.Append(c);
			}

			i++;
		}
	}

	// The index just past the block that opens at css[open].
	private static int SkipBlock(string css, int open)
	{
		int depth = 0;
		for (int i = open; i < css.Length; i++)
		{
			if (css[i] == '{')
			{
				depth++;
			}
			else if (css[i] == '}' && --depth == 0)
			{
				return i + 1;
			}
		}

		return css.Length;
	}

	private static List<string> SplitTopLevel(string text, char separator)
	{
		var parts = new List<string>();
		int depth = 0;
		int start = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] is '(' or '[')
			{
				depth++;
			}
			else if (text[i] is ')' or ']')
			{
				depth--;
			}
			else if (text[i] == separator && depth == 0)
			{
				parts.Add(text[start..i]);
				start = i + 1;
			}
		}

		parts.Add(text[start..]);
		return parts;
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"(?<name>[a-zA-Z]+)\((?<args>(?:[^()]|\((?:[^()]|\([^()]*\))*\))*)\)")]
	private static partial Regex TransformFunction();

	[GeneratedRegex(@"^-?(\d+\.?\d*|\.\d+)[a-z%]*$")]
	private static partial Regex NumberLiteral();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
