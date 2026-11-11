using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization.NamingConventions;

namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Finds mokadocs.yaml keys that no setting reads, and turns YamlDotNet errors into messages
///     that name the line, the column and the actual cause.
/// </summary>
internal static class YamlConfigChecks
{
	private static readonly Regex _locationPrefix = new(
		@"^\(Line: \d+, Col: \d+, Idx: \d+\) - \(Line: \d+, Col: \d+, Idx: \d+\): ",
		RegexOptions.Compiled);

	private static readonly Dictionary<string, string> _retiredKeys = new(StringComparer.Ordinal)
	{
		["features.search.provider"] = "search always uses the built-in search-index.json"
	};

	/// <summary>
	///     One warning per key that the deserializer would skip, with its dotted path and line,
	///     such as <c>unknown key 'site.titel' (line 2)</c>.
	/// </summary>
	/// <remarks>
	///     The reader ignores unmatched properties so that a newer config still loads, which also
	///     meant a typo like <c>basepath:</c> silently did nothing. Values typed as dictionaries
	///     (plugin options) take any key and are not checked.
	/// </remarks>
	public static List<string> FindUnknownKeys(string yaml, Type rootType)
	{
		var warnings = new List<string>();
		var stream = new YamlStream();
		try
		{
			stream.Load(new StringReader(yaml));
		}
		catch (YamlException)
		{
			return warnings;
		}

		if (stream.Documents.Count > 0)
		{
			Walk(stream.Documents[0].RootNode, rootType, "", warnings);
		}

		return warnings;
	}

	/// <summary>
	///     <c>line L, column C: message: inner message</c>, without YamlDotNet's position prefix.
	///     A wrong value type only said "Exception during deserialization" before; the reason was
	///     in the inner exception.
	/// </summary>
	public static string Describe(YamlException ex)
	{
		string message = _locationPrefix.Replace(ex.Message, "");
		if (ex.InnerException is { } inner)
		{
			message = $"{message.TrimEnd('.')}: {inner.Message}";
		}

		return $"line {ex.Start.Line}, column {ex.Start.Column}: {message}";
	}

	private static void Walk(YamlNode node, Type type, string path, List<string> warnings)
	{
		if (ListElementType(type) is { } element)
		{
			if (node is YamlSequenceNode sequence)
			{
				int index = 0;
				foreach (YamlNode item in sequence)
				{
					Walk(item, element, $"{path}[{index++}]", warnings);
				}
			}

			return;
		}

		// Keyed entries such as features.blog.authors take any key, but their values are settings.
		if (DictionaryValueType(type) is { } valueType && IsSettingsType(valueType) && node is YamlMappingNode entries)
		{
			foreach ((YamlNode entryKey, YamlNode entryValue) in entries.Children)
			{
				if (entryKey is YamlScalarNode { Value: { } name })
				{
					Walk(entryValue, valueType, $"{path}.{name}", warnings);
				}
			}

			return;
		}

		if (!IsSettingsType(type) || node is not YamlMappingNode mapping)
		{
			return;
		}

		Dictionary<string, PropertyInfo> properties = type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.ToDictionary(p => CamelCaseNamingConvention.Instance.Apply(p.Name), StringComparer.Ordinal);

		foreach ((YamlNode keyNode, YamlNode valueNode) in mapping.Children)
		{
			if (keyNode is not YamlScalarNode { Value: { } key })
			{
				continue;
			}

			string keyPath = path.Length == 0 ? key : $"{path}.{key}";
			if (properties.TryGetValue(key, out PropertyInfo? property))
			{
				Walk(valueNode, property.PropertyType, keyPath, warnings);
				continue;
			}

			if (_retiredKeys.TryGetValue(keyPath, out string? why))
			{
				warnings.Add($"'{keyPath}' (line {keyNode.Start.Line}) is no longer read and is ignored: {why}");
				continue;
			}

			string message = $"unknown key '{keyPath}' (line {keyNode.Start.Line})";
			string? sameButCase = properties.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
			warnings.Add(sameButCase is null
				? message
				: $"{message}; keys are case sensitive, did you mean '{sameButCase}'?");
		}
	}

	private static bool IsSettingsType(Type type) =>
		type is { IsClass: true, Namespace: "Moka.Docs.Core.Configuration" }
		&& !typeof(IDictionary).IsAssignableFrom(type);

	private static Type? ListElementType(Type type) =>
		type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
			? type.GetGenericArguments()[0]
			: null;

	private static Type? DictionaryValueType(Type type) =>
		type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
			? type.GetGenericArguments()[1]
			: null;
}
