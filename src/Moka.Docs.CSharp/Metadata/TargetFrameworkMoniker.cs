namespace Moka.Docs.CSharp.Metadata;

/// <summary>
///     A parsed target framework moniker such as <c>net10.0</c>, <c>netstandard2.0</c> or <c>net48</c>.
/// </summary>
internal readonly record struct TargetFrameworkMoniker(string Identifier, Version Version, string? Platform)
	: IComparable<TargetFrameworkMoniker>
{
	private const string _netCoreApp = ".NETCoreApp";
	private const string _netStandard = ".NETStandard";
	private const string _netFramework = ".NETFramework";

	// The versions the .NET SDK generates *_OR_GREATER symbols for (Microsoft.NET.SupportedTargetFrameworks.props).
	// .NET 5 and later are generated from the major version, so they need no list.
	private static readonly Version[] _netCoreAppVersions =
		[new(1, 0), new(1, 1), new(2, 0), new(2, 1), new(2, 2), new(3, 0), new(3, 1)];

	private static readonly Version[] _netStandardVersions =
		[new(1, 0), new(1, 1), new(1, 2), new(1, 3), new(1, 4), new(1, 5), new(1, 6), new(2, 0), new(2, 1)];

	private static readonly Version[] _netFrameworkVersions =
	[
		new(2, 0), new(3, 0), new(3, 5), new(4, 0), new(4, 5), new(4, 5, 1), new(4, 5, 2), new(4, 6),
		new(4, 6, 1), new(4, 6, 2), new(4, 7), new(4, 7, 1), new(4, 7, 2), new(4, 8), new(4, 8, 1)
	];

	/// <summary>Whether this is a .NET Framework moniker such as <c>net48</c>.</summary>
	public bool IsNetFramework => Identifier == _netFramework;

	/// <summary>The framework name NuGet uses, for example <c>.NETCoreApp,Version=v10.0</c>.</summary>
	public string FrameworkName => $"{Identifier},Version=v{Version}";

	/// <inheritdoc />
	public int CompareTo(TargetFrameworkMoniker other)
	{
		int byFamily = FamilyRank.CompareTo(other.FamilyRank);
		if (byFamily != 0)
		{
			return byFamily;
		}

		int byVersion = Version.CompareTo(other.Version);
		if (byVersion != 0)
		{
			return byVersion;
		}

		// net10.0 over net10.0-windows: the plain one is what most of the code compiles for.
		return (Platform is null).CompareTo(other.Platform is null);
	}

	private int FamilyRank => Identifier switch
	{
		_netCoreApp => 3,
		_netStandard => 2,
		_ => 1
	};

	/// <summary>
	///     Parses a moniker, returning <c>null</c> for one this does not recognize.
	/// </summary>
	public static TargetFrameworkMoniker? TryParse(string? moniker)
	{
		if (string.IsNullOrWhiteSpace(moniker))
		{
			return null;
		}

		string value = moniker.Trim().ToLowerInvariant();
		string? platform = null;
		int dash = value.IndexOf('-');
		if (dash >= 0)
		{
			platform = value[(dash + 1)..];
			value = value[..dash];
		}

		if (value.StartsWith("netcoreapp", StringComparison.Ordinal))
		{
			return Create(_netCoreApp, value["netcoreapp".Length..], platform);
		}

		if (value.StartsWith("netstandard", StringComparison.Ordinal))
		{
			return Create(_netStandard, value["netstandard".Length..], platform);
		}

		if (!value.StartsWith("net", StringComparison.Ordinal) || value.Length < 4 || !char.IsDigit(value[3]))
		{
			return null;
		}

		string number = value[3..];

		// net5.0 and later have a dot; .NET Framework monikers are digits only (net48, net472).
		return number.Contains('.')
			? Create(_netCoreApp, number, platform)
			: Create(_netFramework, string.Join(".", number.Select(digit => digit.ToString())), platform);
	}

	/// <summary>
	///     Converts a NuGet framework name such as <c>.NETCoreApp,Version=v10.0</c> back to its
	///     moniker (<c>net10.0</c>), the folder name the SDK builds into.
	/// </summary>
	public static string? AliasFor(string? frameworkName)
	{
		if (string.IsNullOrEmpty(frameworkName))
		{
			return null;
		}

		string[] parts = frameworkName.Split(",Version=v", 2, StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || !Version.TryParse(parts[1], out Version? version))
		{
			return null;
		}

		return parts[0] switch
		{
			_netCoreApp when version.Major >= 5 => $"net{version.Major}.{version.Minor}",
			_netCoreApp => $"netcoreapp{version.Major}.{version.Minor}",
			_netStandard => $"netstandard{version.Major}.{version.Minor}",
			_netFramework => "net" + Digits(version),
			_ => null
		};
	}

	/// <summary>
	///     The preprocessor symbols the .NET SDK defines for this framework, following
	///     Microsoft.NET.Sdk.BeforeCommon.targets: for <c>net9.0</c> that is <c>NET</c>,
	///     <c>NET9_0</c>, <c>NETCOREAPP</c>, <c>NET5_0_OR_GREATER</c> through
	///     <c>NET9_0_OR_GREATER</c> and <c>NETCOREAPP1_0_OR_GREATER</c> through
	///     <c>NETCOREAPP3_1_OR_GREATER</c>.
	/// </summary>
	public List<string> PreprocessorSymbols()
	{
		var symbols = new List<string>();
		Version current = Version;

		switch (Identifier)
		{
			case _netCoreApp when current.Major >= 5:
				symbols.AddRange(["NET", $"NET{current.Major}_{current.Minor}", "NETCOREAPP"]);
				for (int major = 5; major <= current.Major; major++)
				{
					symbols.Add($"NET{major}_0_OR_GREATER");
				}

				symbols.AddRange(_netCoreAppVersions.Select(v => $"NETCOREAPP{v.Major}_{v.Minor}_OR_GREATER"));
				break;

			case _netCoreApp:
				symbols.AddRange(["NETCOREAPP", $"NETCOREAPP{current.Major}_{current.Minor}"]);
				symbols.AddRange(_netCoreAppVersions
					.Where(v => v <= current)
					.Select(v => $"NETCOREAPP{v.Major}_{v.Minor}_OR_GREATER"));
				break;

			case _netStandard:
				symbols.AddRange(["NETSTANDARD", $"NETSTANDARD{current.Major}_{current.Minor}"]);
				symbols.AddRange(_netStandardVersions
					.Where(v => v <= current)
					.Select(v => $"NETSTANDARD{v.Major}_{v.Minor}_OR_GREATER"));
				break;

			case _netFramework:
				symbols.AddRange(["NETFRAMEWORK", "NET" + Digits(current)]);
				symbols.AddRange(_netFrameworkVersions
					.Where(v => v <= current)
					.Select(v => $"NET{Digits(v)}_OR_GREATER"));
				break;
		}

		// net10.0-windows defines WINDOWS (the versioned platform symbols are left out).
		string platform = new((Platform ?? "").TakeWhile(char.IsLetter).ToArray());
		if (platform.Length > 0)
		{
			symbols.Add(platform.ToUpperInvariant());
		}

		return symbols;
	}

	private static TargetFrameworkMoniker? Create(string identifier, string number, string? platform)
	{
		string text = number.Contains('.') ? number : number + ".0";
		return Version.TryParse(text, out Version? version)
			? new TargetFrameworkMoniker(identifier, version, platform)
			: null;
	}

	private static string Digits(Version version) =>
		version.Build >= 0
			? $"{version.Major}{version.Minor}{version.Build}"
			: $"{version.Major}{version.Minor}";
}
