using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Moka.Docs.Plugins.OpenApi;

/// <summary>
///     Reads a YAML OpenAPI document into the same <see cref="JsonNode" /> tree a JSON document gives,
///     so the parser has one model to read.
/// </summary>
/// <remarks>
///     Plain scalars are typed the way YAML 1.2's core schema types them: <c>true</c>, <c>null</c> and
///     numbers become JSON booleans, nulls and numbers, and anything quoted stays a string. A number keeps
///     its text, so <c>version: 1.0</c> reads back as <c>1.0</c>, not <c>1</c>.
/// </remarks>
internal static partial class OpenApiYaml
{
	// Deep enough for any real document; stops an anchor that contains its own alias.
	private const int _maxDepth = 256;

	/// <summary>Parses the first YAML document in <paramref name="content" />.</summary>
	/// <exception cref="YamlException">The content is not valid YAML.</exception>
	internal static JsonNode? Parse(string content)
	{
		var stream = new YamlStream();
		// MergingParser applies "<<: *anchor" merge keys, which some hand-written specs use.
		stream.Load(new MergingParser(new Parser(new StringReader(content))));
		return stream.Documents.Count == 0 ? null : ToJson(stream.Documents[0].RootNode, 0);
	}

	private static JsonNode? ToJson(YamlNode node, int depth)
	{
		if (depth > _maxDepth)
		{
			throw new InvalidOperationException("The YAML document nests too deeply, or an anchor contains its own alias.");
		}

		switch (node)
		{
			case YamlMappingNode mapping:
				var obj = new JsonObject();
				foreach ((YamlNode key, YamlNode value) in mapping.Children)
				{
					string name = key is YamlScalarNode scalarKey ? scalarKey.Value ?? "" : key.ToString();
					obj[name] = ToJson(value, depth + 1);
				}

				return obj;

			case YamlSequenceNode sequence:
				var array = new JsonArray();
				foreach (YamlNode item in sequence.Children)
				{
					array.Add(ToJson(item, depth + 1));
				}

				return array;

			case YamlScalarNode scalar:
				return ToJson(scalar);

			default:
				return null;
		}
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

	[GeneratedRegex(@"^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][-+]?[0-9]+)?$")]
	private static partial Regex JsonNumberRegex();
}
