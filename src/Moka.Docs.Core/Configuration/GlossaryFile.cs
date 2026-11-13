using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Moka.Docs.Core.Configuration;

/// <summary>
///     Reads a glossary file from the docs folder: a YAML mapping of terms to their definitions, the same shape
///     as <c>glossary:</c> in mokadocs.yaml.
/// </summary>
/// <example>
///     <code>
///     API: The types and members a library makes public.
///     Route: The URL path of a page, such as /guide/markdown.
///     </code>
/// </example>
public static class GlossaryFile
{
	/// <summary>The names the build looks for at the root of the docs folder, in this order.</summary>
	public static IReadOnlyList<string> FileNames { get; } = ["glossary.yml", "glossary.yaml"];

	/// <summary>
	///     Parses a glossary file. A file whose only key is <c>glossary</c> is read from inside that key, so a
	///     block copied from mokadocs.yaml works too.
	/// </summary>
	/// <param name="yaml">The file's content.</param>
	/// <returns>The terms, and a message (naming its line) for each entry that couldn't be used.</returns>
	public static GlossaryReadResult Parse(string yaml)
	{
		var terms = new Dictionary<string, string>(StringComparer.Ordinal);
		var warnings = new List<string>();
		if (string.IsNullOrWhiteSpace(yaml))
		{
			return new GlossaryReadResult(terms, warnings);
		}

		var stream = new YamlStream();
		try
		{
			stream.Load(new StringReader(yaml));
		}
		catch (YamlException ex)
		{
			warnings.Add($"it isn't valid YAML and was ignored ({YamlConfigChecks.Describe(ex)})");
			return new GlossaryReadResult(terms, warnings);
		}

		if (stream.Documents.Count == 0)
		{
			return new GlossaryReadResult(terms, warnings);
		}

		if (stream.Documents[0].RootNode is not YamlMappingNode root)
		{
			warnings.Add("it should be a list of 'term: definition' lines and was ignored");
			return new GlossaryReadResult(terms, warnings);
		}

		if (root.Children.Count == 1 && root.Children.First() is { Key: YamlScalarNode { Value: "glossary" }, Value: YamlMappingNode inner })
		{
			root = inner;
		}

		foreach ((YamlNode key, YamlNode value) in root.Children)
		{
			int line = (int)key.Start.Line;
			string term = (key as YamlScalarNode)?.Value?.Trim() ?? "";
			if (term.Length == 0)
			{
				warnings.Add($"an entry has no term and was ignored (line {line})");
				continue;
			}

			if (value is not YamlScalarNode { Value: { } definition } || string.IsNullOrWhiteSpace(definition))
			{
				warnings.Add($"the term '{term}' has no definition (a text value) and was ignored (line {line})");
				continue;
			}

			terms[term] = definition.Trim();
		}

		return new GlossaryReadResult(terms, warnings);
	}
}

/// <summary>The terms read from a glossary file, and why any entries were left out.</summary>
/// <param name="Terms">Each term and its definition.</param>
/// <param name="Warnings">Problems that left entries out, each naming its line.</param>
public sealed record GlossaryReadResult(IReadOnlyDictionary<string, string> Terms, IReadOnlyList<string> Warnings);
