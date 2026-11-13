using System.Text;
using System.Text.RegularExpressions;
using Moka.Docs.Core.Theming;

namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	/// <summary>Where the SVG sprite with every icon the theme draws is published.</summary>
	public const string IconSpritePath = "/_theme/icons.svg";

	// The shapes of the layouts' own icons. A layout's <svg> keeps its size, fill and stroke attributes around a
	// <use> of the symbol, so a rule that restyles an icon still applies; a symbol holds only shapes. The search,
	// x, arrow-right, clock and users icons are the Lucide ones.
	private static readonly (string Id, string Shapes)[] _layoutIcons =
	[
		("warning", """<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"/><path d="M12 9v4"/><path d="M12 17h.01"/>"""),
		("logo", """<path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/><path d="M8 7h6"/><path d="M8 11h8"/>"""),
		("pen", """<path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/>"""),
		("chevron", """<polyline points="6 9 12 15 18 9"/>"""),
		("sun", """<circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>"""),
		("sun-small", """<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>"""),
		("moon", """<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>"""),
		("monitor", """<rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/>"""),
		("sliders", """<path d="M21 4h-7M10 4H3M21 12h-9M8 12H3M21 20h-5M12 20H3M14 2v4M8 10v4M16 18v4"/>"""),
		("close", """<path d="M18 6 6 18M6 6l12 12"/>"""),
		("menu-lines", """<path d="M3 12h18M3 6h18M3 18h18"/>"""),
		("panel-left", """<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18"/>"""),
		("nuget-mark", """<path d="M6.5 17.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z"/><path d="M14 14a4 4 0 1 0 0-8 4 4 0 0 0 0 8z"/><circle cx="7" cy="7" r="2"/>"""),
		("external", """<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/>"""),
		("edit", """<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>"""),
		("thumbs-up", """<path d="M7 10v12"/><path d="M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2h0a3.13 3.13 0 0 1 3 3.88Z"/>"""),
		("thumbs-down", """<path d="M17 14V2"/><path d="M9 18.12 10 14H4.17a2 2 0 0 1-1.92-2.56l2.33-8A2 2 0 0 1 6.5 2H20a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-2.76a2 2 0 0 0-1.79 1.11L14 22h0a3.13 3.13 0 0 1-3-3.88Z"/>"""),
		("prev", """<polyline points="15 18 9 12 15 6"/>"""),
		("next", """<polyline points="9 18 15 12 9 6"/>"""),
		("toc", """<line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>"""),
		("arrow", """<path d="M5 12h14M12 5l7 7-7 7"/>"""),
		("github-mark", """<path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.2 11.39.6.11.82-.26.82-.58v-2.03c-3.34.73-4.04-1.61-4.04-1.61-.55-1.39-1.34-1.76-1.34-1.76-1.09-.74.08-.73.08-.73 1.2.08 1.84 1.24 1.84 1.24 1.07 1.83 2.81 1.3 3.5 1 .1-.78.42-1.3.76-1.6-2.67-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.13-.3-.54-1.52.12-3.18 0 0 1-.32 3.3 1.23a11.5 11.5 0 016.02 0c2.28-1.55 3.29-1.23 3.29-1.23.66 1.66.25 2.88.12 3.18.77.84 1.24 1.91 1.24 3.22 0 4.61-2.8 5.63-5.48 5.92.43.37.81 1.1.81 2.22v3.29c0 .32.22.7.82.58A12.01 12.01 0 0024 12c0-6.63-5.37-12-12-12z"/>""")
	];

	private static readonly Lazy<string> _iconSprite = new(BuildIconSprite);

	/// <summary>
	///     Returns the SVG sprite published as <see cref="IconSpritePath" />: a <c>&lt;symbol&gt;</c> for every
	///     icon the layouts draw and every icon in <see cref="LucideIcons" />, with the icon's name as its id.
	///     Pages draw an icon as <c>&lt;svg&gt;&lt;use href="/_theme/icons.svg?v=...#name"/&gt;&lt;/svg&gt;</c>.
	/// </summary>
	public static string GetIconSprite() => _iconSprite.Value;

	private static string BuildIconSprite()
	{
		var sb = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\">\n");
		foreach ((string id, string shapes) in _layoutIcons)
		{
			sb.Append("<symbol id=\"").Append(id).Append("\" viewBox=\"0 0 24 24\">").Append(shapes).Append("</symbol>\n");
		}

		foreach (string name in LucideIcons.AllNames.Order(StringComparer.Ordinal))
		{
			string svg = LucideIcons.Get(name)!;
			int open = svg.IndexOf('>', StringComparison.Ordinal);
			string viewBox = ViewBoxAttribute().Match(svg[..open]).Groups["box"].Value;
			sb.Append("<symbol id=\"").Append(name).Append("\" viewBox=\"").Append(viewBox).Append("\">")
				.Append(svg[(open + 1)..svg.LastIndexOf("</svg>", StringComparison.Ordinal)]).Append("</symbol>\n");
		}

		return sb.Append("</svg>\n").ToString();
	}

	[GeneratedRegex("""viewBox="(?<box>[^"]*)""")]
	private static partial Regex ViewBoxAttribute();
}
