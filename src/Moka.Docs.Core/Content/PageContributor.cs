using System.Globalization;

namespace Moka.Docs.Core.Content;

/// <summary>
///     Someone who committed a change to a page's source file, as git reports them after
///     applying the repository's <c>.mailmap</c>.
/// </summary>
public sealed record PageContributor
{
	/// <summary>The author name.</summary>
	public required string Name { get; init; }

	/// <summary>
	///     The author email, which identifies the person: commits with the same email (ignoring
	///     case) count as one contributor. Empty when git recorded none. The default theme doesn't
	///     show it.
	/// </summary>
	public string Email { get; init; } = "";

	/// <summary>How many commits by this person touched the file.</summary>
	public int Commits { get; init; }

	/// <summary>
	///     Up to two letters for an avatar: the first letters of the first and last words of
	///     the name, or the first letter of a one-word name, upper case.
	/// </summary>
	public string Initials
	{
		get
		{
			string[] words = Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			return words.Length switch
			{
				0 => "?",
				1 => FirstLetter(words[0]),
				_ => FirstLetter(words[0]) + FirstLetter(words[^1])
			};
		}
	}

	/// <inheritdoc />
	public override string ToString() => $"PageContributor({Name}, {Commits})";

	// A text element, not a char: a name starting with an emoji or a letter outside the BMP
	// would otherwise be cut in half.
	private static string FirstLetter(string word) => StringInfo.GetNextTextElement(word).ToUpperInvariant();
}
