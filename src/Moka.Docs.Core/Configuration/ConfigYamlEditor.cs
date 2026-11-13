using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Moka.Docs.Core.Configuration;

/// <summary>One value <see cref="ConfigYamlEditor" /> sets, or removes when <see cref="Value" /> is <c>null</c>.</summary>
/// <param name="Path">Mapping keys from the top of the file, joined by dots: <c>theme.options.layout.navbar</c>.</param>
/// <param name="Value">The new value, or <c>null</c> to remove the key.</param>
/// <param name="IsBoolean">Whether to write the value as a YAML boolean, <c>true</c> or <c>false</c>.</param>
internal sealed record ConfigYamlChange(string Path, string? Value, bool IsBoolean = false);

/// <summary>The edited YAML, or why the file was left alone.</summary>
/// <param name="Yaml">The edited text, or <c>null</c> when the edit was refused.</param>
/// <param name="Refusal">Why the file can't be edited, or <c>null</c> when it was.</param>
internal sealed record ConfigYamlEdit(string? Yaml, string? Refusal);

/// <summary>
///     Edits block-style YAML as text, so the author's comments, key order, quoting, blank lines and
///     line endings stay as they were everywhere outside the keys being changed. YamlDotNet can't do
///     that: its representation model drops comments and its serializer rewrites the whole file.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>An existing value is replaced where it stands, in the quoting style it had.</item>
///         <item>
///             A missing key goes at the end of its parent mapping with that mapping's indentation; a
///             missing parent is written as a new block, indented by the file's own indentation step.
///         </item>
///         <item>
///             A removed key takes its line with it, and a parent mapping left empty is removed too
///             unless it holds comments.
///         </item>
///         <item>
///             Files it can't edit safely are refused: more than one document, flow mappings or
///             sequences, anchors, aliases or merge keys along the edited paths, block scalars as
///             edited values, duplicate keys and tab characters in the edited section.
///         </item>
///         <item>
///             Every edit is parsed again and compared with the original, with the changes applied, as a
///             tree of values; any other difference refuses the edit.
///         </item>
///     </list>
/// </remarks>
internal static class ConfigYamlEditor
{
	private static readonly Regex _numberLike = new(
		@"^[-+]?(\.?[0-9][0-9_]*(\.[0-9_]*)?([eE][-+]?[0-9]+)?|0x[0-9a-fA-F_]+|0o[0-7_]+|0b[01_]+|\.(inf|Inf|INF|nan|NaN|NAN))$",
		RegexOptions.Compiled);

	private static readonly HashSet<string> _reservedWords = new(StringComparer.OrdinalIgnoreCase)
	{
		"null", "~", "true", "false", "yes", "no", "on", "off", "y", "n"
	};

	/// <summary>
	///     Applies <paramref name="changes" /> to <paramref name="yaml" />. Changes that leave a value as
	///     it is written are skipped, so a file that already holds every value comes back unchanged.
	/// </summary>
	/// <param name="yaml">The file's text.</param>
	/// <param name="changes">The values to set or remove. Paths must not overlap.</param>
	/// <returns>The edited text, or why the file was refused.</returns>
	public static ConfigYamlEdit Apply(string yaml, IReadOnlyList<ConfigYamlChange> changes)
	{
		string? refusal = Load(yaml, changes.Select(c => c.Path), out MappingNode? root);
		if (refusal is not null || root is null)
		{
			return new ConfigYamlEdit(null, refusal ?? "it can't be read");
		}

		var editor = new Editor(yaml, root);
		editor.EditMapping(root, EditTree.From(changes), "", 0);
		if (editor.Refusal is not null)
		{
			return new ConfigYamlEdit(null, editor.Refusal);
		}

		string edited;
		try
		{
			edited = editor.Result();
		}
		catch (InvalidOperationException)
		{
			return new ConfigYamlEdit(null, "the changes overlap in the file");
		}

		return ReadsBackAsIntended(root, edited, changes)
			? new ConfigYamlEdit(edited, null)
			: new ConfigYamlEdit(null, "the edited file didn't read back with only these values changed, so it was left alone");
	}

	/// <summary>
	///     Why <paramref name="yaml" /> can't be edited at <paramref name="paths" />, or <c>null</c> when
	///     it can.
	/// </summary>
	/// <param name="yaml">The file's text.</param>
	/// <param name="paths">The dotted paths an edit may set or remove.</param>
	/// <returns>A reason that completes "mokadocs.yaml can't be edited safely: ...", or <c>null</c>.</returns>
	public static string? FindRefusal(string yaml, IEnumerable<string> paths) => Load(yaml, paths, out _);

	/// <summary>
	///     The value written at each of <paramref name="paths" />: the scalar's text, or <c>null</c> when
	///     the key is missing, empty, <c>null</c>/<c>~</c>, or holds something other than a single value.
	///     Every path is <c>null</c> when the file doesn't parse.
	/// </summary>
	/// <param name="yaml">The file's text.</param>
	/// <param name="paths">Dotted paths of mapping keys.</param>
	/// <returns>One entry per path.</returns>
	public static IReadOnlyDictionary<string, string?> ReadValues(string yaml, IEnumerable<string> paths)
	{
		Node? root = null;
		try
		{
			List<Node?> documents = ParseDocuments(yaml);
			root = documents.Count == 1 ? documents[0] : null;
		}
		catch (YamlException)
		{
			// A file that doesn't parse holds no values; Apply and FindRefusal report why.
		}

		var values = new Dictionary<string, string?>(StringComparer.Ordinal);
		foreach (string path in paths)
		{
			values[path] = Find(root, path.Split('.')) is ScalarNode { IsNull: false } scalar ? scalar.Value : null;
		}

		return values;
	}

	/// <summary>
	///     <paramref name="value" /> as YAML: plain when that reads back as the same string, else in double
	///     quotes.
	/// </summary>
	/// <param name="value">Any string.</param>
	/// <returns>A YAML scalar that reads back as <paramref name="value" />.</returns>
	public static string FormatString(string value) => IsSafePlain(value) ? value : DoubleQuoted(value);

	private static string? Load(string yaml, IEnumerable<string> paths, out MappingNode? root)
	{
		root = null;
		if (Regex.IsMatch(yaml, "\r(?!\n)"))
		{
			return "it has carriage returns without line feeds (old Mac line endings)";
		}

		List<Node?> documents;
		try
		{
			documents = ParseDocuments(yaml);
		}
		catch (YamlException ex)
		{
			return $"it doesn't parse ({YamlConfigChecks.Describe(ex)})";
		}

		if (documents.Count != 1)
		{
			return documents.Count == 0 ? "it is empty" : $"it holds {documents.Count} YAML documents";
		}

		if (documents[0] is not MappingNode top)
		{
			return "it doesn't hold a mapping at the top level";
		}

		if (top.IsFlow)
		{
			return "it is written as one flow mapping ({ })";
		}

		if (HasMergeKey(top))
		{
			return "it uses a merge key (<<) at the top level";
		}

		if (top.Entries.Count > 0 && !OnlySpacesBefore(yaml, top.Entries[0].Key.Start))
		{
			return "its first key doesn't start its line";
		}

		foreach (string path in paths)
		{
			if (CheckPath(yaml, top, path.Split('.')) is { } problem)
			{
				return problem;
			}
		}

		root = top;
		return null;
	}

	private static string? CheckPath(string yaml, MappingNode top, string[] segments)
	{
		MappingNode current = top;
		for (int i = 0; i < segments.Length; i++)
		{
			string prefix = string.Join('.', segments, 0, i + 1);
			List<KeyValuePair<Node, Node>> found = Entries(current, segments[i]);
			if (found.Count > 1)
			{
				return $"{prefix} appears more than once";
			}

			if (found.Count == 0)
			{
				return null;
			}

			Node value = found[0].Value;
			if (i == 0 && yaml.AsSpan(LineStart(yaml, found[0].Key.Start), EntryEnd(yaml, found[0]) - LineStart(yaml, found[0].Key.Start)).Contains('\t'))
			{
				return $"the {prefix} section has a tab character";
			}

			if (value is AliasNode || value.HasAnchor)
			{
				return $"{prefix} uses an anchor or an alias (& or *)";
			}

			if (i == segments.Length - 1)
			{
				return value switch
				{
					ScalarNode { Style: ScalarStyle.Literal or ScalarStyle.Folded } => $"{prefix} is a block scalar (| or >)",
					ScalarNode => null,
					_ => $"{prefix} holds a list or a mapping, not a single value"
				};
			}

			switch (value)
			{
				case MappingNode { IsFlow: true }:
					return $"{prefix} is written as a flow mapping ({{ }})";
				case MappingNode nested when HasMergeKey(nested):
					return $"{prefix} uses a merge key (<<)";
				case MappingNode nested:
					current = nested;
					break;
				case ScalarNode { IsNull: true }:
					return null;
				default:
					return $"{prefix} isn't a mapping";
			}
		}

		return null;
	}

	private static bool HasMergeKey(MappingNode mapping) =>
		mapping.Entries.Any(e => e.Key is ScalarNode { Value: "<<", Style: ScalarStyle.Plain });

	private static List<KeyValuePair<Node, Node>> Entries(MappingNode mapping, string key) =>
		mapping.Entries.Where(e => e.Key is ScalarNode { IsNull: false } k && k.Value == key).ToList();

	private static Node? Find(Node? node, string[] segments)
	{
		foreach (string segment in segments)
		{
			if (node is not MappingNode mapping || Entries(mapping, segment) is not [var entry])
			{
				return null;
			}

			node = entry.Value;
		}

		return node;
	}

	#region Parsing

	private static List<Node?> ParseDocuments(string yaml)
	{
		var parser = new Parser(new StringReader(yaml));
		parser.Consume<StreamStart>();
		var documents = new List<Node?>();
		while (parser.TryConsume<DocumentStart>(out _))
		{
			documents.Add(parser.Accept<DocumentEnd>(out _) ? null : ReadNode(parser));
			parser.Consume<DocumentEnd>();
		}

		parser.Consume<StreamEnd>();
		return documents;
	}

	private static Node ReadNode(IParser parser)
	{
		ParsingEvent current = parser.Current ?? throw new YamlException("The YAML ended in the middle of a value.");
		parser.MoveNext();
		switch (current)
		{
			case Scalar scalar:
				return new ScalarNode
				{
					Start = (int)scalar.Start.Index,
					End = (int)scalar.End.Index,
					Value = scalar.Value,
					Style = scalar.Style,
					HasAnchor = !scalar.Anchor.IsEmpty
				};
			case AnchorAlias alias:
				return new AliasNode { Start = (int)alias.Start.Index, End = (int)alias.End.Index };
			case MappingStart start:
			{
				var mapping = new MappingNode
				{
					Start = (int)start.Start.Index,
					IsFlow = start.Style == MappingStyle.Flow,
					HasAnchor = !start.Anchor.IsEmpty
				};
				while (true)
				{
					if (parser.Current is MappingEnd end && parser.MoveNext())
					{
						mapping.FlowEnd = (int)end.Start.Index + 1;
						return mapping;
					}

					Node key = ReadNode(parser);
					mapping.Entries.Add(new KeyValuePair<Node, Node>(key, ReadNode(parser)));
				}
			}
			case SequenceStart start:
			{
				var sequence = new SequenceNode
				{
					Start = (int)start.Start.Index,
					IsFlow = start.Style == SequenceStyle.Flow,
					HasAnchor = !start.Anchor.IsEmpty
				};
				while (true)
				{
					if (parser.Current is SequenceEnd end && parser.MoveNext())
					{
						sequence.FlowEnd = (int)end.Start.Index + 1;
						return sequence;
					}

					sequence.Items.Add(ReadNode(parser));
				}
			}
			default:
				throw new YamlException(current.Start, current.End, $"Unexpected {current.GetType().Name} in the YAML.");
		}
	}

	private abstract class Node
	{
		public int Start { get; init; }

		public bool HasAnchor { get; init; }
	}

	private sealed class ScalarNode : Node
	{
		public int End { get; init; }

		public required string Value { get; init; }

		public ScalarStyle Style { get; init; }

		public bool IsNull => Style is ScalarStyle.Plain or ScalarStyle.Any && Value is "" or "~" or "null" or "Null" or "NULL";
	}

	private sealed class AliasNode : Node
	{
		public int End { get; init; }
	}

	private sealed class MappingNode : Node
	{
		public bool IsFlow { get; init; }

		public List<KeyValuePair<Node, Node>> Entries { get; } = [];

		public int FlowEnd { get; set; }
	}

	private sealed class SequenceNode : Node
	{
		public bool IsFlow { get; init; }

		public List<Node> Items { get; } = [];

		public int FlowEnd { get; set; }
	}

	#endregion

	#region Text positions

	// The end of the last character that belongs to a node: YamlDotNet's end marks for block
	// collections point at the next token, which can sit lines further down past comments.
	private static int ContentEnd(Node node) => node switch
	{
		ScalarNode scalar => scalar.End,
		AliasNode alias => alias.End,
		MappingNode { IsFlow: true } mapping => mapping.FlowEnd,
		MappingNode { Entries.Count: > 0 } mapping => ContentEnd(mapping.Entries[^1].Value),
		SequenceNode { IsFlow: true } sequence => sequence.FlowEnd,
		SequenceNode { Items.Count: > 0 } sequence => ContentEnd(sequence.Items[^1]),
		_ => node.Start
	};

	private static int LineStart(string text, int index)
	{
		int newline = index > 0 ? text.LastIndexOf('\n', index - 1) : -1;
		return newline + 1;
	}

	// The start of the line after the one holding text[index].
	private static int AfterLine(string text, int index)
	{
		int newline = index < 0 ? -1 : text.IndexOf('\n', Math.Min(index, text.Length));
		return newline < 0 ? text.Length : newline + 1;
	}

	private static int EntryEnd(string text, KeyValuePair<Node, Node> entry) =>
		AfterLine(text, Math.Max(ContentEnd(entry.Value), ContentEnd(entry.Key)) - 1);

	private static bool OnlySpacesBefore(string text, int index)
	{
		for (int i = LineStart(text, index); i < index; i++)
		{
			if (text[i] != ' ')
			{
				return false;
			}
		}

		return true;
	}

	#endregion

	#region Scalars

	private static bool IsSafePlain(string value)
	{
		if (value.Length == 0 || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
		{
			return false;
		}

		if ("-?:,[]{}#&*!|>'\"%@`".Contains(value[0]) || value.EndsWith(':'))
		{
			return false;
		}

		if (value.Contains(": ", StringComparison.Ordinal) || value.Contains(" #", StringComparison.Ordinal)
		                                                   || value.Any(NeedsEscape))
		{
			return false;
		}

		return !_reservedWords.Contains(value) && !_numberLike.IsMatch(value);
	}

	// Control characters, and the characters YAML 1.1 readers take for line breaks or a byte order mark.
	private static bool NeedsEscape(char c) => char.IsControl(c) || c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029 || c == (char)0xFEFF;

	private static string DoubleQuoted(string value)
	{
		var quoted = new StringBuilder(value.Length + 2).Append('"');
		foreach (char c in value)
		{
			switch (c)
			{
				case '"':
					quoted.Append("\\\"");
					break;
				case '\\':
					quoted.Append("\\\\");
					break;
				case '\n':
					quoted.Append("\\n");
					break;
				case '\r':
					quoted.Append("\\r");
					break;
				case '\t':
					quoted.Append("\\t");
					break;
				default:
					if (NeedsEscape(c))
					{
						quoted.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
					}
					else
					{
						quoted.Append(c);
					}

					break;
			}
		}

		return quoted.Append('"').ToString();
	}

	private static string Format(ConfigYamlChange change, ScalarStyle style)
	{
		string value = change.Value!;
		if (change.IsBoolean)
		{
			return value;
		}

		return style switch
		{
			ScalarStyle.SingleQuoted when !value.Any(NeedsEscape) =>
				"'" + value.Replace("'", "''", StringComparison.Ordinal) + "'",
			ScalarStyle.DoubleQuoted => DoubleQuoted(value),
			_ => FormatString(value)
		};
	}

	private static bool SameValue(ScalarNode scalar, ConfigYamlChange change)
	{
		if (scalar.IsNull)
		{
			return false;
		}

		return change.IsBoolean
			? bool.TryParse(scalar.Value, out bool written) && written == bool.Parse(change.Value!)
			: scalar.Value == change.Value;
	}

	#endregion

	#region Editing

	private sealed class EditTree
	{
		public List<KeyValuePair<string, EditTree>> Children { get; } = [];

		public ConfigYamlChange? Leaf { get; private set; }

		public bool HasValues => Leaf is { Value: not null } || Children.Any(c => c.Value.HasValues);

		public static EditTree From(IEnumerable<ConfigYamlChange> changes)
		{
			var root = new EditTree();
			foreach (ConfigYamlChange change in changes)
			{
				EditTree node = root;
				foreach (string segment in change.Path.Split('.'))
				{
					if (node.Leaf is not null)
					{
						throw new ArgumentException($"'{change.Path}' is inside another change's value.", nameof(changes));
					}

					EditTree? child = node.Children.FirstOrDefault(c => c.Key == segment).Value;
					if (child is null)
					{
						child = new EditTree();
						node.Children.Add(new KeyValuePair<string, EditTree>(segment, child));
					}

					node = child;
				}

				if (node.Leaf is not null || node.Children.Count > 0)
				{
					throw new ArgumentException($"'{change.Path}' is changed twice or overlaps another change.", nameof(changes));
				}

				node.Leaf = change;
			}

			return root;
		}
	}

	private readonly record struct TextOp(int Start, int End, string Text, int Depth);

	private sealed class Editor(string text, MappingNode root)
	{
		private readonly string _newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
		private readonly List<TextOp> _ops = [];
		private readonly int _step = IndentStep(text, root);

		public string? Refusal { get; private set; }

		/// <summary>Edits one mapping. Returns whether every entry it had was removed and nothing added.</summary>
		public bool EditMapping(MappingNode mapping, EditTree edits, string path, int depth)
		{
			int indent = mapping.Entries.Count > 0 ? KeyIndent(mapping.Entries[0]) : 0;
			var added = new StringBuilder();
			int removed = 0;
			bool filled = false;

			foreach ((string name, EditTree edit) in edits.Children)
			{
				string childPath = path.Length == 0 ? name : path + "." + name;
				List<KeyValuePair<Node, Node>> found = Entries(mapping, name);
				if (found.Count > 1)
				{
					Refusal = $"{childPath} appears more than once";
					return false;
				}

				if (found.Count == 0)
				{
					if (edit.Leaf is { Value: not null } leaf)
					{
						added.Append(Line(indent, name, Format(leaf, ScalarStyle.Plain)));
					}
					else if (edit.Leaf is null && edit.HasValues)
					{
						added.Append(Block(indent, name, edit));
					}

					continue;
				}

				KeyValuePair<Node, Node> entry = found[0];
				if (edit.Leaf is { } change)
				{
					if (!EditValue(entry, change, childPath, depth, ref removed))
					{
						return false;
					}

					continue;
				}

				switch (entry.Value)
				{
					case MappingNode { IsFlow: false } nested:
					{
						int mark = _ops.Count;
						bool emptied = EditMapping(nested, edit, childPath, depth + 1);
						if (Refusal is not null)
						{
							return false;
						}

						if (emptied && !HasComment(LineStart(text, entry.Key.Start), EntryEnd(text, entry)))
						{
							_ops.RemoveRange(mark, _ops.Count - mark);
							if (!Remove(entry, childPath, depth))
							{
								return false;
							}

							removed++;
						}

						break;
					}
					case ScalarNode { IsNull: true } empty:
						if (!edit.HasValues)
						{
							break;
						}

						if (empty.End > empty.Start)
						{
							int from = empty.Start;
							while (from > 0 && text[from - 1] == ' ')
							{
								from--;
							}

							_ops.Add(new TextOp(from, empty.End, "", depth));
						}

						_ops.Add(new TextOp(EntryEnd(text, entry), EntryEnd(text, entry),
							EndingAt(EntryEnd(text, entry), Children(KeyIndent(entry) + _step, edit)), depth + 1));
						filled = true;
						break;
					default:
						Refusal = $"{childPath} isn't a mapping";
						return false;
				}
			}

			if (added.Length > 0)
			{
				int at = AfterLine(text, ContentEnd(mapping) - 1);
				_ops.Add(new TextOp(at, at, EndingAt(at, added.ToString()), depth));
			}

			return removed == mapping.Entries.Count && added.Length == 0 && !filled;
		}

		public string Result()
		{
			// Back to front, so earlier positions stay valid. At one position a removal goes first, then
			// insertions from the outside in, which leaves the innermost text first.
			List<TextOp> ordered = _ops
				.OrderByDescending(op => op.Start)
				.ThenBy(op => op.End > op.Start ? 0 : 1)
				.ThenBy(op => op.Depth)
				.ToList();
			var edited = new StringBuilder(text);
			int lowest = int.MaxValue;
			foreach (TextOp op in ordered)
			{
				if (op.End > lowest)
				{
					throw new InvalidOperationException("Overlapping YAML edits.");
				}

				edited.Remove(op.Start, op.End - op.Start).Insert(op.Start, op.Text);
				lowest = op.Start;
			}

			return edited.ToString();
		}

		private bool EditValue(KeyValuePair<Node, Node> entry, ConfigYamlChange change, string path, int depth, ref int removed)
		{
			if (entry.Value is not ScalarNode scalar)
			{
				Refusal = $"{path} holds a list or a mapping, not a single value";
				return false;
			}

			if (change.Value is null)
			{
				if (!Remove(entry, path, depth))
				{
					return false;
				}

				removed++;
				return true;
			}

			if (scalar.Style is ScalarStyle.Literal or ScalarStyle.Folded)
			{
				Refusal = $"{path} is a block scalar (| or >)";
				return false;
			}

			if (SameValue(scalar, change))
			{
				return true;
			}

			_ops.Add(scalar.End == scalar.Start
				? new TextOp(scalar.Start, scalar.Start, " " + Format(change, ScalarStyle.Plain), depth)
				: new TextOp(scalar.Start, scalar.End, Format(change, scalar.Style), depth));
			return true;
		}

		private bool Remove(KeyValuePair<Node, Node> entry, string path, int depth)
		{
			if (!OnlySpacesBefore(text, entry.Key.Start))
			{
				Refusal = $"{path} shares its line with other YAML";
				return false;
			}

			_ops.Add(new TextOp(LineStart(text, entry.Key.Start), EntryEnd(text, entry), "", depth));
			return true;
		}

		private int KeyIndent(KeyValuePair<Node, Node> entry) => entry.Key.Start - LineStart(text, entry.Key.Start);

		private string Line(int indent, string key, string value) => new string(' ', indent) + key + ": " + value + _newline;

		private string Block(int indent, string key, EditTree edits) =>
			new string(' ', indent) + key + ":" + _newline + Children(indent + _step, edits);

		private string Children(int indent, EditTree edits)
		{
			var lines = new StringBuilder();
			foreach ((string name, EditTree edit) in edits.Children)
			{
				if (edit.Leaf is { Value: not null } leaf)
				{
					lines.Append(Line(indent, name, Format(leaf, ScalarStyle.Plain)));
				}
				else if (edit.Leaf is null && edit.HasValues)
				{
					lines.Append(Block(indent, name, edit));
				}
			}

			return lines.ToString();
		}

		// Text added at the very end of a file that has no final line break keeps it that way.
		private string EndingAt(int position, string lines)
		{
			if (position < text.Length || text.Length == 0 || text.EndsWith('\n'))
			{
				return lines;
			}

			return _newline + lines[..^_newline.Length];
		}

		private bool HasComment(int start, int end)
		{
			foreach (string line in text[start..end].Split('\n'))
			{
				string trimmed = line.TrimStart(' ');
				if (trimmed.StartsWith('#') || line.Contains(" #", StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		// The file's own indentation step: how far the first nested mapping is indented past its key.
		private static int IndentStep(string text, MappingNode mapping) => FindStep(text, mapping) is > 0 and var step ? step : 2;

		private static int FindStep(string text, MappingNode mapping)
		{
			foreach ((Node key, Node value) in mapping.Entries)
			{
				if (value is not MappingNode { IsFlow: false, Entries.Count: > 0 } nested)
				{
					continue;
				}

				int step = nested.Entries[0].Key.Start - LineStart(text, nested.Entries[0].Key.Start)
				           - (key.Start - LineStart(text, key.Start));
				if (step > 0)
				{
					return step;
				}

				if (FindStep(text, nested) is > 0 and var inner)
				{
					return inner;
				}
			}

			return 0;
		}
	}

	#endregion

	#region Verification

	private static bool ReadsBackAsIntended(MappingNode original, string edited, IReadOnlyList<ConfigYamlChange> changes)
	{
		List<Node?> documents;
		try
		{
			documents = ParseDocuments(edited);
		}
		catch (YamlException)
		{
			return false;
		}

		if (documents is not [MappingNode after])
		{
			return false;
		}

		var expected = (Dictionary<string, object?>)Canonical(original)!;
		foreach (ConfigYamlChange change in changes)
		{
			string[] path = change.Path.Split('.');
			if (change.Value is null || Find(original, path) is not ScalarNode written || !SameValue(written, change))
			{
				Set(expected, path, change.Value);
			}
		}

		return Same(Prune(expected), Prune(Canonical(after)));
	}

	// Values only: comments, styles and positions are gone, and a null value or an empty mapping
	// counts as a missing key, since that is how the configuration reads it.
	private static object? Canonical(Node node) => node switch
	{
		ScalarNode { IsNull: true } => null,
		ScalarNode scalar => scalar.Value,
		AliasNode => "*alias",
		MappingNode mapping => mapping.Entries
			.GroupBy(e => e.Key is ScalarNode key ? key.Value : "?" + e.Key.Start.ToString(CultureInfo.InvariantCulture))
			.ToDictionary(g => g.Key, g => Canonical(g.Last().Value), StringComparer.Ordinal),
		SequenceNode sequence => sequence.Items.Select(Canonical).ToList(),
		_ => null
	};

	private static void Set(Dictionary<string, object?> map, string[] path, string? value)
	{
		for (int i = 0; i < path.Length - 1; i++)
		{
			if (map.GetValueOrDefault(path[i]) is not Dictionary<string, object?> child)
			{
				if (value is null)
				{
					return;
				}

				child = new Dictionary<string, object?>(StringComparer.Ordinal);
				map[path[i]] = child;
			}

			map = child;
		}

		if (value is null)
		{
			map.Remove(path[^1]);
		}
		else
		{
			map[path[^1]] = value;
		}
	}

	private static object? Prune(object? value)
	{
		if (value is Dictionary<string, object?> map)
		{
			foreach (string key in map.Keys.ToList())
			{
				object? child = Prune(map[key]);
				if (child is null or Dictionary<string, object?> { Count: 0 })
				{
					map.Remove(key);
				}
				else
				{
					map[key] = child;
				}
			}
		}
		else if (value is List<object?> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i] = Prune(list[i]);
			}
		}

		return value;
	}

	private static bool Same(object? a, object? b) => (a, b) switch
	{
		(null, null) => true,
		(string x, string y) => x == y,
		(Dictionary<string, object?> x, Dictionary<string, object?> y) =>
			x.Count == y.Count && x.All(e => y.TryGetValue(e.Key, out object? other) && Same(e.Value, other)),
		(List<object?> x, List<object?> y) => x.Count == y.Count && x.Zip(y).All(p => Same(p.First, p.Second)),
		_ => false
	};

	#endregion
}
