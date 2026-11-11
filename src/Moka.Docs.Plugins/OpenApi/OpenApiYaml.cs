using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Reads a YAML OpenAPI document into the same <see cref="JsonNode" /> tree a JSON document gives,
///     so the parser has one model to read.
/// </summary>
/// <remarks>
///     <para>
///         Plain scalars are typed the way YAML 1.2's core schema types them: <c>true</c>, <c>null</c> and
///         numbers become JSON booleans, nulls and numbers, and anything quoted stays a string. A number keeps
///         its text, so <c>version: 1.0</c> reads back as <c>1.0</c>, not <c>1</c>.
///     </para>
///     <para>
///         A document is limited the way a JSON one is: <see cref="MaxDepth" /> levels, checked on the parser's
///         events before YamlDotNet builds its nodes (which it does recursively, so 5,000 levels overflowed the
///         stack), and aliases may expand it only so far (a 300-byte document of nested aliases expanded to a
///         million values). Merge keys (<c>&lt;&lt;: *anchor</c>) are applied here, under the same limit:
///         YamlDotNet's <c>MergingParser</c> copies the merged events, so nested merges grow the same way, and
///         it refuses any document over 100,000 events.
///     </para>
/// </remarks>
internal static partial class OpenApiYaml
{
	/// <summary>How many levels of mappings and sequences a document may nest, as for JSON.</summary>
	internal const int MaxDepth = 64;

	private const int _extraValuesAllowed = 100_000;

	private const string _mergeTag = "tag:yaml.org,2002:merge";

	/// <summary>Parses the first YAML document in <paramref name="content" />.</summary>
	/// <exception cref="YamlException">
	///     The content is not valid YAML, nests too deeply, or uses a mapping, a list or an alias as a key.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	///     The document's aliases expand it too far, an anchor contains its own alias, or a merge key isn't
	///     followed by a mapping.
	/// </exception>
	internal static JsonNode? Parse(string content)
	{
		var parser = new GuardedParser(new Parser(new StringReader(content)));
		var stream = new YamlStream();
		stream.Load(parser);
		if (stream.Documents.Count == 0)
		{
			return null;
		}

		// A document without aliases makes at most one value per event; aliases may add a few times that.
		var converter = new Converter(_extraValuesAllowed + (4L * parser.EventCount));
		return converter.ToJson(stream.Documents[0].RootNode, 0);
	}

	private static JsonNode? ToJson(YamlScalarNode scalar)
	{
		string value = scalar.Value ?? "";
		string tag = scalar.Tag.IsEmpty || scalar.Tag.IsNonSpecific ? "" : scalar.Tag.Value;

		if (tag.Length > 0)
		{
			return tag switch
			{
				"tag:yaml.org,2002:null" => null,
				"tag:yaml.org,2002:bool" when bool.TryParse(value, out bool flag) => JsonValue.Create(flag),
				"tag:yaml.org,2002:int" or "tag:yaml.org,2002:float" when JsonNumberRegex().IsMatch(value) => JsonNode.Parse(value),
				_ => JsonValue.Create(value)
			};
		}

		if (scalar.Style != ScalarStyle.Plain)
		{
			return JsonValue.Create(value);
		}

		return value switch
		{
			"" or "~" or "null" or "Null" or "NULL" => null,
			"true" or "True" or "TRUE" => JsonValue.Create(true),
			"false" or "False" or "FALSE" => JsonValue.Create(false),
			// Parsing the text keeps it as written: 1.0 stays 1.0.
			_ when JsonNumberRegex().IsMatch(value) => JsonNode.Parse(value),
			_ => JsonValue.Create(value)
		};
	}

	private static bool IsMergeKey(YamlScalarNode key) =>
		(key.Style == ScalarStyle.Plain && key.Value == "<<" && (key.Tag.IsEmpty || key.Tag.IsNonSpecific))
		|| (!key.Tag.IsEmpty && key.Tag.Value == _mergeTag);

	[GeneratedRegex(@"^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][-+]?[0-9]+)?$")]
	private static partial Regex JsonNumberRegex();

	/// <summary>
	///     Converts the loaded nodes, counting every value it makes. An alias is the same node object as
	///     its anchor, so each use of it is converted again.
	/// </summary>
	private sealed class Converter(long budget)
	{
		private long _remaining = budget;

		public JsonNode? ToJson(YamlNode node, int depth)
		{
			// Deeper than the events allowed only through an alias, or an anchor that contains its own alias.
			if (depth > MaxDepth)
			{
				throw new InvalidOperationException(
					$"The YAML document nests deeper than {MaxDepth} levels, or an anchor contains its own alias.");
			}

			if (--_remaining < 0)
			{
				throw new InvalidOperationException(
					"The YAML document's aliases expand it to far more values than it has; it looks built to exhaust memory.");
			}

			switch (node)
			{
				case YamlMappingNode mapping:
					return ToJson(mapping, depth);

				case YamlSequenceNode sequence:
					var array = new JsonArray();
					foreach (YamlNode item in sequence.Children)
					{
						array.Add(ToJson(item, depth + 1));
					}

					return array;

				case YamlScalarNode scalar:
					return OpenApiYaml.ToJson(scalar);

				default:
					return null;
			}
		}

		/// <summary>
		///     A mapping, with the entries of any merge key added where the key stands. Keys written in the
		///     mapping itself win over merged ones, and an earlier merged mapping over a later one.
		/// </summary>
		private JsonObject ToJson(YamlMappingNode mapping, int depth)
		{
			// GuardedParser refused every key that isn't a scalar.
			var own = new HashSet<string>(StringComparer.Ordinal);
			foreach (YamlScalarNode key in mapping.Children.Keys.Cast<YamlScalarNode>())
			{
				if (!IsMergeKey(key))
				{
					own.Add(key.Value ?? "");
				}
			}

			var obj = new JsonObject();
			foreach ((YamlNode key, YamlNode value) in mapping.Children)
			{
				var scalarKey = (YamlScalarNode)key;
				if (!IsMergeKey(scalarKey))
				{
					obj[scalarKey.Value ?? ""] = ToJson(value, depth + 1);
					continue;
				}

				foreach (YamlMappingNode source in MergeSources(value))
				{
					JsonObject merged = ToJson(source, depth + 1);
					foreach (string name in merged.Select(entry => entry.Key).ToList())
					{
						if (own.Contains(name) || obj.ContainsKey(name))
						{
							continue;
						}

						JsonNode? entry = merged[name];
						merged.Remove(name);
						obj[name] = entry;
					}
				}
			}

			return obj;
		}

		private static IEnumerable<YamlMappingNode> MergeSources(YamlNode value) => value switch
		{
			YamlMappingNode single => [single],
			YamlSequenceNode list when list.Children.All(item => item is YamlMappingNode) => list.Children.Cast<YamlMappingNode>(),
			_ => throw new InvalidOperationException("A merge key (<<) needs a mapping or a list of mappings.")
		};
	}

	/// <summary>
	///     Passes the parser's events through, refusing a document that nests deeper than
	///     <see cref="MaxDepth" /> or uses a mapping, a list or an alias as a key, and counting the events.
	/// </summary>
	/// <remarks>
	///     YamlDotNet hashes keys when it adds them to a mapping, and an alias key that expands to millions
	///     of values took seconds to hash. OpenAPI never needs a key that isn't a scalar.
	/// </remarks>
	private sealed class GuardedParser(IParser inner) : IParser
	{
		private readonly Stack<(bool Mapping, bool ExpectingKey)> _open = new();

		public long EventCount { get; private set; }

		public ParsingEvent? Current => inner.Current;

		public bool MoveNext()
		{
			if (!inner.MoveNext())
			{
				return false;
			}

			EventCount++;
			ParsingEvent current = inner.Current!;
			switch (current)
			{
				case MappingStart or SequenceStart:
					RefuseAsKey(current);
					if (_open.Count >= MaxDepth)
					{
						throw new YamlException(current.Start, current.End, $"The YAML document nests deeper than {MaxDepth} levels.");
					}

					bool mapping = current is MappingStart;
					_open.Push((mapping, mapping));
					break;

				case MappingEnd or SequenceEnd:
					_open.Pop();
					NodeDone();
					break;

				case AnchorAlias:
					RefuseAsKey(current);
					NodeDone();
					break;

				case Scalar:
					NodeDone();
					break;
			}

			return true;
		}

		private void RefuseAsKey(ParsingEvent current)
		{
			if (_open.TryPeek(out (bool Mapping, bool ExpectingKey) top) && top.Mapping && top.ExpectingKey)
			{
				throw new YamlException(current.Start, current.End,
					"A key in an OpenAPI document must be text, not a mapping, a list or an alias.");
			}
		}

		/// <summary>A key or a value is complete; in a mapping, keys and values take turns.</summary>
		private void NodeDone()
		{
			if (_open.TryPeek(out (bool Mapping, bool ExpectingKey) top) && top.Mapping)
			{
				_open.Pop();
				_open.Push((true, !top.ExpectingKey));
			}
		}
	}
}
