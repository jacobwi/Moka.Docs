using System.Text;
using System.Text.Json;
using Moka.Docs.Core;
using Moka.Docs.Core.Configuration;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Serve;

/// <summary>How the theme lab shows and saves an option.</summary>
internal enum ThemeLabOptionKind
{
	/// <summary>One name from a fixed list.</summary>
	Choice,

	/// <summary>On or off.</summary>
	Toggle,

	/// <summary>Free text.</summary>
	Text
}

/// <summary>One value of a <see cref="ThemeLabOptionKind.Choice" /> option.</summary>
internal sealed record ThemeLabChoice
{
	public required string Value { get; init; }

	public required string Label { get; init; }

	public string? Description { get; init; }

	/// <summary>Whether a page only shows this value after a rebuild (see <see cref="ThemeLabCatalog" />).</summary>
	public bool Rebuild { get; init; }

	/// <summary>For a style: whether it starts in dark mode for a reader who hasn't picked one.</summary>
	public bool DarkFirst { get; init; }

	/// <summary>For a preset: the options it sets, keyed like <see cref="ThemeLabOption.Key" />.</summary>
	public IReadOnlyDictionary<string, string>? Sets { get; init; }
}

/// <summary>A theme option the lab edits, with what it needs to preview and save it.</summary>
internal sealed record ThemeLabOption
{
	/// <summary>The path under <c>theme.options</c>, e.g. <c>layout.navbar</c>.</summary>
	public required string Key { get; init; }

	public required string Label { get; init; }

	public required string Group { get; init; }

	public required ThemeLabOptionKind Kind { get; init; }

	public IReadOnlyList<ThemeLabChoice> Choices { get; init; } = [];

	/// <summary>The value the configuration reader uses when neither the file nor a preset sets one.</summary>
	public required string Default { get; init; }

	/// <summary>
	///     The attribute on <c>&lt;html&gt;</c> that switches this option in the browser, or <c>null</c>
	///     when every change needs a rebuild.
	/// </summary>
	public string? Attribute { get; init; }

	/// <summary>Whether the layouts leave <see cref="Attribute" /> out for the default value.</summary>
	public bool RemoveAttributeAtDefault { get; init; }

	/// <summary>The <c>MOKADOCS_*</c> variable that wins over the file, or <c>null</c> when none does.</summary>
	public string? EnvironmentVariable { get; init; }

	/// <summary>The theme's localStorage keys for a reader's own pick that hides this option's value.</summary>
	public IReadOnlyList<string> ReaderStorageKeys { get; init; } = [];

	/// <summary>
	///     Whether every choice switches without a rebuild on a page built with
	///     <c>theme.options.layoutSelector</c>, which carries the markup of every variant of the part.
	/// </summary>
	public bool LiveWithLayoutSelector { get; init; }

	/// <summary>The longest text a <see cref="ThemeLabOptionKind.Text" /> option takes.</summary>
	public int MaxLength { get; init; }

	/// <summary>The value the loaded configuration ended up with, for checking a save.</summary>
	public required Func<ThemeOptions, string> Read { get; init; }

	/// <summary>The option's path in mokadocs.yaml.</summary>
	public string YamlPath => ThemeLabCatalog.OptionsPath + Key;

	public ThemeLabChoice? FindChoice(string? value) =>
		value is null ? null : Choices.FirstOrDefault(c => c.Value.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
///     The options the theme lab offers, built from the Core lists (<see cref="ThemePresets" />,
///     <see cref="ThemeStyles" />, <see cref="ColorPresets" />, <see cref="CodeThemes" />,
///     <see cref="CodeStyles" />, <see cref="ThemeLayouts" />, <see cref="BannerVariants" />), so a new
///     name there shows up in the lab without a change here. Also the one place that knows which
///     changes the browser can show without a rebuild.
/// </summary>
internal static class ThemeLabCatalog
{
	internal const string OptionsPath = "theme.options.";

	private const string _readerStyleKey = "mokadocs-style";
	private const string _readerColorKey = "mokadocs-color-theme";
	private const string _readerCodeThemeKey = "mokadocs-code-theme";
	private const string _readerCodeStyleKey = "mokadocs-code-style";

	// Layout variants a page can't show by switching its <html> attribute: the templates render other
	// markup for them (tabs row, link columns, no footer, the inline or bare "On this page", the split
	// or plain hero, every home page but the default). Every other variant is CSS scoped to the
	// attribute, or read by the theme script on every pass (autohide). ThemeLabRebuildTests builds a
	// site with each variant and fails when this list and the rendered pages disagree. A site with
	// theme.options.layoutSelector on renders the markup of every variant of ThemeLayouts.ReaderParts,
	// so there only the landing page's parts (hero, home) need a rebuild.
	private static readonly Dictionary<string, string[]> _rebuildVariants = new(StringComparer.Ordinal)
	{
		["navbar"] = ["tabs"],
		["footer"] = ["columns", "none"],
		["toc"] = ["minimal", "inline"],
		["home"] = ["portal", "showcase", "minimal", "docs"],
		["hero"] = ["split", "minimal"]
	};

	/// <summary>Whether a page needs a rebuild to show <paramref name="variant" /> of <paramref name="part" />.</summary>
	internal static bool NeedsRebuild(ThemeLayoutPart part, string variant) =>
		_rebuildVariants.TryGetValue(part.Key, out string[]? variants)
		&& variants.Contains(variant, StringComparer.OrdinalIgnoreCase);

	/// <summary>Every option in the order the lab shows them. Defaults are read from <see cref="MokaDefaults" /> now.</summary>
	internal static IReadOnlyList<ThemeLabOption> CreateOptions()
	{
		var options = new List<ThemeLabOption>
		{
			new()
			{
				Key = "preset",
				Label = "Preset",
				Group = "Look",
				Kind = ThemeLabOptionKind.Choice,
				Default = "",
				Choices =
				[
					new ThemeLabChoice { Value = "", Label = "None", Description = "No preset: each option starts from its own default." },
					.. ThemePresets.All.Select(p => new ThemeLabChoice
					{
						Value = p.Name,
						Label = p.Label,
						Description = p.Description,
						Sets = p.Options
					})
				],
				EnvironmentVariable = "MOKADOCS_THEME_PRESET",
				ReaderStorageKeys = [_readerStyleKey, _readerColorKey, _readerCodeThemeKey, _readerCodeStyleKey],
				Read = o => o.Preset
			},
			new()
			{
				Key = "style",
				Label = "Style",
				Group = "Look",
				Kind = ThemeLabOptionKind.Choice,
				Default = MokaDefaults.Style,
				Choices = [.. ThemeStyles.All.Select(s => new ThemeLabChoice
				{
					Value = s.Name, Label = s.Label, Description = s.Description, DarkFirst = s.DarkFirst
				})],
				Attribute = "data-style",
				EnvironmentVariable = "MOKADOCS_STYLE",
				ReaderStorageKeys = [_readerStyleKey],
				Read = o => o.Style
			},
			new()
			{
				Key = "defaultColorTheme",
				Label = "Color preset",
				Group = "Look",
				Kind = ThemeLabOptionKind.Choice,
				Default = ColorPresets.Default,
				Choices = [.. ColorPresets.All.Select(p => new ThemeLabChoice { Value = p.Name, Label = p.Label })],
				Attribute = "data-color-theme",
				ReaderStorageKeys = [_readerColorKey],
				Read = o => o.DefaultColorTheme
			},
			new()
			{
				Key = "codeTheme",
				Label = "Code theme",
				Group = "Look",
				Kind = ThemeLabOptionKind.Choice,
				Default = MokaDefaults.CodeTheme,
				Choices = [.. CodeThemes.All.Select(t => new ThemeLabChoice { Value = t.Name, Label = t.Label })],
				Attribute = "data-code-theme",
				EnvironmentVariable = "MOKADOCS_CODE_THEME",
				ReaderStorageKeys = [_readerCodeThemeKey],
				Read = o => o.CodeTheme
			},
			new()
			{
				Key = "codeStyle",
				Label = "Code style",
				Group = "Look",
				Kind = ThemeLabOptionKind.Choice,
				Default = MokaDefaults.CodeStyle,
				Choices = [.. CodeStyles.All.Select(s => new ThemeLabChoice { Value = s.Name, Label = s.Label })],
				Attribute = "data-code-style",
				EnvironmentVariable = "MOKADOCS_CODE_STYLE",
				ReaderStorageKeys = [_readerCodeStyleKey],
				Read = o => o.CodeStyle
			}
		};

		foreach (ThemeLayoutPart part in ThemeLayouts.Parts)
		{
			string attributeName = Separated(part.Key, '-').ToLowerInvariant();
			bool readerPart = ThemeLayouts.ReaderParts.Contains(part);
			options.Add(new ThemeLabOption
			{
				Key = "layout." + part.Key,
				Label = Words(part.Key),
				Group = "Layout",
				Kind = ThemeLabOptionKind.Choice,
				Default = ThemeLayouts.Default,
				Choices = [.. part.Names.Select(n => new ThemeLabChoice { Value = n, Label = n, Rebuild = NeedsRebuild(part, n) })],
				Attribute = "data-" + attributeName,
				RemoveAttributeAtDefault = true,
				EnvironmentVariable = "MOKADOCS_LAYOUT_" + Separated(part.Key, '_').ToUpperInvariant(),
				ReaderStorageKeys = readerPart ? ["mokadocs-layout-" + attributeName] : [],
				LiveWithLayoutSelector = readerPart,
				Read = o => o.Layout.NameOf(part)
			});
		}

		options.Add(new ThemeLabOption
		{
			Key = "sidebarToggle",
			Label = "Sidebar toggle button",
			Group = "Layout",
			Kind = ThemeLabOptionKind.Toggle,
			Default = MokaDefaults.SidebarToggle ? "true" : "false",
			EnvironmentVariable = "MOKADOCS_SIDEBAR_TOGGLE",
			Read = o => o.SidebarToggle ? "true" : "false"
		});
		options.Add(new ThemeLabOption
		{
			Key = "banner.text",
			Label = "Text",
			Group = "Banner",
			Kind = ThemeLabOptionKind.Text,
			Default = "",
			MaxLength = 300,
			EnvironmentVariable = "MOKADOCS_BANNER_TEXT",
			Read = o => o.Banner?.Text ?? ""
		});
		options.Add(new ThemeLabOption
		{
			Key = "banner.link",
			Label = "Link",
			Group = "Banner",
			Kind = ThemeLabOptionKind.Text,
			Default = "",
			MaxLength = 2048,
			EnvironmentVariable = "MOKADOCS_BANNER_LINK",
			Read = o => o.Banner?.Link ?? ""
		});
		options.Add(new ThemeLabOption
		{
			Key = "banner.variant",
			Label = "Variant",
			Group = "Banner",
			Kind = ThemeLabOptionKind.Choice,
			Default = BannerVariants.Info,
			Choices = [.. BannerVariants.Names.Select(n => new ThemeLabChoice { Value = n, Label = n })],
			Read = o => o.Banner?.Variant ?? BannerVariants.Info
		});
		return options;
	}

	/// <summary>
	///     Checks a save request's values against the options and turns them into the values to write:
	///     a choice's canonical name, <c>true</c>/<c>false</c>, trimmed text, or <c>null</c> to remove
	///     the key. A preset of "" and empty text also remove it.
	/// </summary>
	/// <param name="options">The lab's options.</param>
	/// <param name="values">The request's values, keyed like <see cref="ThemeLabOption.Key" />.</param>
	/// <param name="error">What's wrong with the request, or <c>null</c>.</param>
	internal static Dictionary<string, string?> Validate(IReadOnlyList<ThemeLabOption> options,
		IReadOnlyDictionary<string, JsonElement> values, out string? error)
	{
		var valid = new Dictionary<string, string?>(StringComparer.Ordinal);
		error = null;
		foreach ((string key, JsonElement value) in values)
		{
			if (options.FirstOrDefault(o => o.Key == key) is not { } option)
			{
				error = $"'{key}' isn't an option the theme lab can save.";
				return [];
			}

			if (value.ValueKind == JsonValueKind.Null)
			{
				valid[key] = null;
				continue;
			}

			string? problem = option.Kind switch
			{
				ThemeLabOptionKind.Toggle => value.ValueKind is JsonValueKind.True or JsonValueKind.False
					? Keep(valid, key, value.GetBoolean() ? "true" : "false")
					: $"{key} takes true, false or null.",
				ThemeLabOptionKind.Choice => value.ValueKind == JsonValueKind.String && option.FindChoice(value.GetString()) is { } choice
					? Keep(valid, key, choice.Value.Length == 0 ? null : choice.Value)
					: $"'{Shorten(value)}' isn't a {option.Label.ToLowerInvariant()} name. Use one of: {string.Join(", ", option.Choices.Where(c => c.Value.Length > 0).Select(c => c.Value))}.",
				_ => value.ValueKind == JsonValueKind.String ? CheckText(valid, option, value.GetString()!) : $"{key} takes a string or null."
			};
			if (problem is not null)
			{
				error = problem;
				return [];
			}
		}

		return valid;
	}

	/// <summary>
	///     The edits that take mokadocs.yaml from <paramref name="written" /> to <paramref name="wanted" />,
	///     skipping values it already has. Names compare without case, as the reader resolves them.
	/// </summary>
	internal static List<ConfigYamlChange> Changes(IReadOnlyList<ThemeLabOption> options,
		IReadOnlyDictionary<string, string?> wanted, IReadOnlyDictionary<string, string?> written)
	{
		var changes = new List<ConfigYamlChange>();
		foreach (ThemeLabOption option in options)
		{
			if (!wanted.TryGetValue(option.Key, out string? value))
			{
				continue;
			}

			string? current = written.GetValueOrDefault(option.YamlPath);
			if (!SameValue(option, current, value))
			{
				changes.Add(new ConfigYamlChange(option.YamlPath, value, option.Kind == ThemeLabOptionKind.Toggle));
			}
		}

		return changes;
	}

	/// <summary>
	///     What the configuration should read for each option, given the values written in the file,
	///     compared with what <paramref name="loaded" /> holds. Options a <c>MOKADOCS_*</c> variable
	///     sets are skipped. Returns the first mismatch, or <c>null</c>.
	/// </summary>
	internal static string? Mismatch(IReadOnlyList<ThemeLabOption> options, ThemeOptions loaded,
		IReadOnlyDictionary<string, string?> written, Func<string, string?> environment)
	{
		string? presetName = EnvironmentValue(environment, "MOKADOCS_THEME_PRESET")
		                     ?? written.GetValueOrDefault(OptionsPath + "preset");
		IReadOnlyDictionary<string, string> presetSets = ThemePresets.Find(presetName)?.Options
		                                                 ?? new Dictionary<string, string>();
		string bannerText = EnvironmentValue(environment, "MOKADOCS_BANNER_TEXT")
		                    ?? written.GetValueOrDefault(OptionsPath + "banner.text") ?? "";

		foreach (ThemeLabOption option in options)
		{
			if (option.EnvironmentVariable is { } variable && EnvironmentValue(environment, variable) is not null)
			{
				continue;
			}

			if (option.Key is "banner.link" or "banner.variant" && bannerText.Trim().Length == 0)
			{
				continue;
			}

			string? explicitValue = written.GetValueOrDefault(option.YamlPath);
			if (option.Kind == ThemeLabOptionKind.Toggle && explicitValue is not null)
			{
				if (!bool.TryParse(explicitValue, out bool flag))
				{
					continue;
				}

				explicitValue = flag ? "true" : "false";
			}

			string expected = (explicitValue ?? (option.Key == "preset" ? null : presetSets.GetValueOrDefault(option.Key)) ?? option.Default).Trim();
			string actual = option.Read(loaded);
			bool same = option.Kind == ThemeLabOptionKind.Text
				? actual == expected
				: actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
			if (!same)
			{
				return $"{option.YamlPath} read back as '{actual}' instead of '{expected}'";
			}
		}

		return null;
	}

	/// <summary>The value of <c>MOKADOCS_*</c> <paramref name="name" />, or <c>null</c> when it isn't set or is empty.</summary>
	internal static string? EnvironmentValue(Func<string, string?> environment, string name) =>
		environment(name) is { Length: > 0 } value ? value : null;

	private static bool SameValue(ThemeLabOption option, string? written, string? wanted)
	{
		if (written is null || wanted is null)
		{
			return written is null && wanted is null;
		}

		return option.Kind switch
		{
			ThemeLabOptionKind.Toggle => bool.TryParse(written, out bool flag) && flag == bool.Parse(wanted),
			ThemeLabOptionKind.Choice => written.Trim().Equals(wanted, StringComparison.OrdinalIgnoreCase),
			_ => written.Trim() == wanted
		};
	}

	private static string? Keep(Dictionary<string, string?> valid, string key, string? value)
	{
		valid[key] = value;
		return null;
	}

	private static string? CheckText(Dictionary<string, string?> valid, ThemeLabOption option, string text)
	{
		string trimmed = text.Trim();
		if (trimmed.Length > option.MaxLength)
		{
			return $"{option.Key} is longer than {option.MaxLength} characters.";
		}

		if (trimmed.Any(char.IsControl))
		{
			return $"{option.Key} can't hold line breaks or other control characters.";
		}

		return Keep(valid, option.Key, trimmed.Length == 0 ? null : trimmed);
	}

	private static string Shorten(JsonElement value)
	{
		string text = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();
		return text.Length > 40 ? text[..40] + "..." : text;
	}

	// pageNav -> "Page nav", toc -> "TOC".
	private static string Words(string key)
	{
		if (key == "toc")
		{
			return "TOC";
		}

		string words = Separated(key, ' ').ToLowerInvariant();
		return char.ToUpperInvariant(words[0]) + words[1..];
	}

	// heroBackground -> hero{separator}Background
	private static string Separated(string camelCase, char separator)
	{
		var text = new StringBuilder(camelCase.Length + 4);
		foreach (char c in camelCase)
		{
			if (char.IsUpper(c) && text.Length > 0)
			{
				text.Append(separator);
			}

			text.Append(c);
		}

		return text.ToString();
	}
}
