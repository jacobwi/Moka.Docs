using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Moka.Docs.Core.Content;

/// <summary>
///     The versions a page applies to, read from the <c>version</c> key of its front matter.
/// </summary>
/// <remarks>
///     The syntax is npm's semver ranges, plus NuGet intervals:
///     <list type="bullet">
///         <item>
///             <description>
///                 <c>2.0</c> or <c>2.0.x</c>: every 2.0.* version. <c>2</c> and <c>2.x</c> are every
///                 2.* version, <c>*</c> is every version, <c>2.0.1</c> is that version alone.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>&gt;=2.0</c>, <c>&gt;2.0</c>, <c>&lt;3</c>, <c>&lt;=2.4</c>, <c>=2.0.1</c>: comparisons.
///                 A missing part stands for any value, so <c>&gt;2.0</c> starts at 2.1.0 and
///                 <c>&lt;=2.4</c> includes every 2.4.* version.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>^1.2</c> (from 1.2.0, same major version) and <c>~1.2.3</c> (from 1.2.3, same minor
///                 version).
///             </description>
///         </item>
///         <item>
///             <description><c>1.0 - 2.0</c>: from 1.0.0 through every 2.0.* version.</description>
///         </item>
///         <item>
///             <description>
///                 <c>[1.0,2.0)</c>, <c>(,3.0]</c>, <c>[2.0]</c>: NuGet intervals. A bracket includes the
///                 bound, a parenthesis leaves it out, and missing parts are zeros.
///             </description>
///         </item>
///     </list>
///     Comparators separated by spaces must all match, and <c>||</c> separates alternatives of which
///     one must match. A version may start with <c>v</c>. Versions compare by major, minor and patch
///     number only: a prerelease suffix such as <c>-beta</c> is ignored on both sides, so
///     <c>3.0.0-beta</c> is in <c>&gt;=3.0</c>.
/// </remarks>
public sealed class VersionRange
{
	private static readonly Regex _partial = new(
		@"^[vV]?(?<major>\d+|[xX*])(?:\.(?<minor>\d+|[xX*]))?(?:\.(?<patch>\d+|[xX*]))?(?:[-+][0-9A-Za-z.+-]*)?$",
		RegexOptions.CultureInvariant);

	private static readonly string[] _operators = [">=", "<=", ">", "<", "=", "^", "~"];

	private readonly List<List<Comparator>> _alternatives;

	private VersionRange(string text, List<List<Comparator>> alternatives)
	{
		Text = text;
		_alternatives = alternatives;
	}

	/// <summary>The range as it was written, trimmed.</summary>
	public string Text { get; }

	/// <summary>
	///     Parses a range.
	/// </summary>
	/// <param name="text">The range, such as <c>&gt;=2.0</c> or <c>1.x || ^3.1</c>.</param>
	/// <param name="range">The parsed range, or <c>null</c> when the text isn't one.</param>
	/// <param name="error">Why the text isn't a range, or <c>null</c> when it is.</param>
	/// <returns>Whether the text is a valid range.</returns>
	public static bool TryParse(string? text, [NotNullWhen(true)] out VersionRange? range,
		[NotNullWhen(false)] out string? error)
	{
		range = null;
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "the range is empty";
			return false;
		}

		var alternatives = new List<List<Comparator>>();
		foreach (string alternative in text.Split("||"))
		{
			string trimmed = alternative.Trim();
			if (trimmed.Length == 0)
			{
				error = "'||' needs a range on each side";
				return false;
			}

			List<Comparator>? comparators = trimmed[0] is '[' or '('
				? ParseInterval(trimmed, out string? problem)
				: ParseComparators(trimmed, out problem);
			if (comparators is null)
			{
				error = problem ?? $"'{trimmed}' is not a version range";
				return false;
			}

			alternatives.Add(comparators);
		}

		range = new VersionRange(text.Trim(), alternatives);
		error = null;
		return true;
	}

	/// <summary>Whether <paramref name="version" /> is in the range.</summary>
	/// <param name="version">The version to test.</param>
	/// <returns><c>true</c> when one of the alternatives matches.</returns>
	public bool Contains(VersionNumber version) =>
		_alternatives.Exists(comparators => comparators.TrueForAll(c => c.Matches(version)));

	/// <inheritdoc />
	public override string ToString() => Text;

	private static List<Comparator>? ParseComparators(string text, out string? error)
	{
		string[] tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		var comparators = new List<Comparator>();
		for (int i = 0; i < tokens.Length; i++)
		{
			if (i + 2 < tokens.Length && tokens[i + 1] == "-")
			{
				if (!TryParsePartial(tokens[i], out Partial low))
				{
					error = $"'{tokens[i]}' is not a version";
					return null;
				}

				if (!TryParsePartial(tokens[i + 2], out Partial high))
				{
					error = $"'{tokens[i + 2]}' is not a version";
					return null;
				}

				comparators.Add(new Comparator(Op.Ge, low.Floor()));
				if (high.Count == 3)
				{
					comparators.Add(new Comparator(Op.Le, high.Floor()));
				}
				else if (high.Count > 0)
				{
					comparators.Add(new Comparator(Op.Lt, high.NextUp()));
				}

				i += 2;
				continue;
			}

			string token = tokens[i];
			string op = _operators.FirstOrDefault(o => token.StartsWith(o, StringComparison.Ordinal)) ?? "";
			string version = token[op.Length..];

			// ">= 2.0" with a space after the operator.
			if (version.Length == 0)
			{
				if (op.Length == 0 || i + 1 >= tokens.Length)
				{
					error = $"'{token}' needs a version after it";
					return null;
				}

				version = tokens[++i];
			}

			if (!TryParsePartial(version, out Partial partial))
			{
				error = $"'{version}' is not a version";
				return null;
			}

			AddComparators(comparators, op, partial);
		}

		error = null;
		return comparators;
	}

	private static void AddComparators(List<Comparator> comparators, string op, Partial version)
	{
		switch (op)
		{
			case "" or "=" when version.Count == 3:
				comparators.Add(new Comparator(Op.Eq, version.Floor()));
				break;
			case "" or "=" when version.Count > 0:
				comparators.Add(new Comparator(Op.Ge, version.Floor()));
				comparators.Add(new Comparator(Op.Lt, version.NextUp()));
				break;
			case ">=" when version.Count > 0:
				comparators.Add(new Comparator(Op.Ge, version.Floor()));
				break;
			case ">":
				comparators.Add(version.Count switch
				{
					0 => Comparator.Never,
					3 => new Comparator(Op.Gt, version.Floor()),
					_ => new Comparator(Op.Ge, version.NextUp())
				});
				break;
			case "<":
				comparators.Add(version.Count == 0 ? Comparator.Never : new Comparator(Op.Lt, version.Floor()));
				break;
			case "<=" when version.Count == 3:
				comparators.Add(new Comparator(Op.Le, version.Floor()));
				break;
			case "<=" when version.Count > 0:
				comparators.Add(new Comparator(Op.Lt, version.NextUp()));
				break;
			case "~" when version.Count > 0:
				comparators.Add(new Comparator(Op.Ge, version.Floor()));
				comparators.Add(new Comparator(Op.Lt, version.Count == 1
					? new VersionNumber(version.Major + 1, 0, 0)
					: new VersionNumber(version.Major, version.Minor + 1, 0)));
				break;
			case "^" when version.Count > 0:
				comparators.Add(new Comparator(Op.Ge, version.Floor()));
				comparators.Add(new Comparator(Op.Lt, CaretLimit(version)));
				break;
		}
	}

	private static VersionNumber CaretLimit(Partial version)
	{
		if (version.Major > 0 || version.Count == 1)
		{
			return new VersionNumber(version.Major + 1, 0, 0);
		}

		if (version.Minor > 0 || version.Count == 2)
		{
			return new VersionNumber(0, version.Minor + 1, 0);
		}

		return new VersionNumber(0, 0, version.Patch + 1);
	}

	private static List<Comparator>? ParseInterval(string text, out string? error)
	{
		char open = text[0];
		char close = text[^1];
		if (text.Length < 2 || close is not (']' or ')'))
		{
			error = $"'{text}' needs a closing ']' or ')'";
			return null;
		}

		string[] bounds = text[1..^1].Split(',');
		if (bounds.Length > 2)
		{
			error = $"'{text}' has more than two bounds";
			return null;
		}

		if (bounds.Length == 1)
		{
			if (open != '[' || close != ']' || !TryParseBound(bounds[0], out VersionNumber exact))
			{
				error = $"'{text}' is not an interval; an exact version is written like [2.0]";
				return null;
			}

			error = null;
			return [new Comparator(Op.Eq, exact)];
		}

		string low = bounds[0].Trim();
		string high = bounds[1].Trim();
		if (low.Length == 0 && high.Length == 0)
		{
			error = $"'{text}' needs at least one bound";
			return null;
		}

		var comparators = new List<Comparator>();
		foreach ((string bound, Op op) in new[]
		         {
			         (low, open == '[' ? Op.Ge : Op.Gt),
			         (high, close == ']' ? Op.Le : Op.Lt)
		         })
		{
			if (bound.Length == 0)
			{
				continue;
			}

			if (!TryParseBound(bound, out VersionNumber version))
			{
				error = $"'{bound}' is not a version";
				return null;
			}

			comparators.Add(new Comparator(op, version));
		}

		error = null;
		return comparators;
	}

	private static bool TryParseBound(string text, out VersionNumber version)
	{
		version = default;
		if (!TryParsePartial(text.Trim(), out Partial partial) || partial.Count == 0 || partial.HasWildcard)
		{
			return false;
		}

		version = partial.Floor();
		return true;
	}

	private static bool TryParsePartial(string text, out Partial partial)
	{
		partial = default;
		Match match = _partial.Match(text);
		if (!match.Success)
		{
			return false;
		}

		int?[] parts = new int?[3];
		int count = 0;
		bool wildcard = false;
		foreach (string name in new[] { "major", "minor", "patch" })
		{
			Group group = match.Groups[name];
			if (!group.Success)
			{
				break;
			}

			if (!int.TryParse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
			{
				wildcard = group.Value is "x" or "X" or "*";
				if (!wildcard)
				{
					return false;
				}

				break;
			}

			// Leaves room for the +1 of an upper bound.
			if (value == int.MaxValue)
			{
				return false;
			}

			parts[count++] = value;
		}

		partial = new Partial(parts[0] ?? 0, parts[1] ?? 0, parts[2] ?? 0, count, wildcard);
		return true;
	}

	private enum Op
	{
		Eq,
		Ge,
		Gt,
		Le,
		Lt,
		Never
	}

	private readonly record struct Partial(int Major, int Minor, int Patch, int Count, bool HasWildcard)
	{
		public VersionNumber Floor() => new(Major, Minor, Patch);

		public VersionNumber NextUp() => Count switch
		{
			1 => new VersionNumber(Major + 1, 0, 0),
			2 => new VersionNumber(Major, Minor + 1, 0),
			_ => new VersionNumber(Major, Minor, Patch + 1)
		};
	}

	private readonly record struct Comparator(Op Op, VersionNumber Version)
	{
		public static Comparator Never => new(Op.Never, default);

		public bool Matches(VersionNumber version) => Op switch
		{
			Op.Eq => version == Version,
			Op.Ge => version >= Version,
			Op.Gt => version > Version,
			Op.Le => version <= Version,
			Op.Lt => version < Version,
			_ => false
		};
	}
}

/// <summary>
///     A version as a major, minor and patch number, the precision <see cref="VersionRange" /> compares at.
/// </summary>
/// <param name="Major">The major version.</param>
/// <param name="Minor">The minor version.</param>
/// <param name="Patch">The patch version.</param>
public readonly record struct VersionNumber(int Major, int Minor, int Patch) : IComparable<VersionNumber>
{
	private static readonly Regex _leadingVersion = new(@"^\s*[vV]?(\d+)(?:\.(\d+))?(?:\.(\d+))?",
		RegexOptions.CultureInvariant);

	/// <summary>
	///     Reads the version number at the start of a version string or label: <c>1.8.0</c>,
	///     <c>v2.0 Beta</c> and <c>3.1.0-preview.2</c> give 1.8.0, 2.0.0 and 3.1.0. Missing parts are
	///     zeros and anything after the numbers is ignored.
	/// </summary>
	/// <param name="text">The text to read.</param>
	/// <param name="version">The version, or <c>default</c> when the text doesn't start with one.</param>
	/// <returns>Whether the text starts with a version number.</returns>
	public static bool TryParse(string? text, out VersionNumber version)
	{
		version = default;
		Match match = text is null ? Match.Empty : _leadingVersion.Match(text);
		if (!match.Success)
		{
			return false;
		}

		int[] parts = new int[3];
		for (int i = 0; i < 3; i++)
		{
			Group group = match.Groups[i + 1];
			if (group.Success && !int.TryParse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture, out parts[i]))
			{
				return false;
			}
		}

		version = new VersionNumber(parts[0], parts[1], parts[2]);
		return true;
	}

	/// <inheritdoc />
	public int CompareTo(VersionNumber other)
	{
		int major = Major.CompareTo(other.Major);
		if (major != 0)
		{
			return major;
		}

		int minor = Minor.CompareTo(other.Minor);
		return minor != 0 ? minor : Patch.CompareTo(other.Patch);
	}

	/// <summary>Whether <paramref name="left" /> is an earlier version than <paramref name="right" />.</summary>
	public static bool operator <(VersionNumber left, VersionNumber right) => left.CompareTo(right) < 0;

	/// <summary>Whether <paramref name="left" /> is a later version than <paramref name="right" />.</summary>
	public static bool operator >(VersionNumber left, VersionNumber right) => left.CompareTo(right) > 0;

	/// <summary>Whether <paramref name="left" /> is the same as or earlier than <paramref name="right" />.</summary>
	public static bool operator <=(VersionNumber left, VersionNumber right) => left.CompareTo(right) <= 0;

	/// <summary>Whether <paramref name="left" /> is the same as or later than <paramref name="right" />.</summary>
	public static bool operator >=(VersionNumber left, VersionNumber right) => left.CompareTo(right) >= 0;

	/// <inheritdoc />
	public override string ToString() =>
		string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}");
}
