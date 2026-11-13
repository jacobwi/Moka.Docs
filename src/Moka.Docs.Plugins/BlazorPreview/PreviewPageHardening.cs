using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Docs.Plugins.BlazorPreview;

/// <summary>
///     Adds a Content-Security-Policy <c>&lt;meta&gt;</c> tag and a message guard script to a page
///     of the preview host as it is staged into the site.
/// </summary>
/// <remarks>
///     <para>
///         The Moka.Blazor.Repl.Host page fetches and loads whatever <c>?assembly=</c> names, cross
///         origin too, and its <c>message</c> listener runs <c>{type:'run', assemblyBytes, jsUrls}</c>
///         from any window. Any site could open the page and run code in the docs origin. The package
///         lives in another repository, so the page is fixed here, for generated hosts and the
///         user's own alike.
///     </para>
///     <para>
///         The guard registers a capturing <c>message</c> listener before any other script runs and
///         stops every message that doesn't come from the parent window or the page itself on the
///         same origin. It also stops the page from loading when <c>?assembly=</c> names anything
///         but a <c>_preview-assemblies/{sha256}.bin</c> file next to the preview host. The policy
///         blocks cross-origin fetches and scripts the page doesn't name itself.
///     </para>
/// </remarks>
internal static partial class PreviewPageHardening
{
	/// <summary>Marks the elements this class adds, so staging the page again replaces them.</summary>
	internal const string MarkerAttribute = "data-mokadocs-preview";

	private static readonly HashSet<string> _rawTextElements = new(StringComparer.Ordinal)
	{
		"script", "style", "textarea", "title", "noscript", "xmp", "iframe", "noembed", "noframes"
	};

	/// <summary>
	///     The guard. It runs before the package's listener exists: that one is added from .NET
	///     once the runtime has started, and this one is a capturing listener added while the head
	///     is parsed.
	/// </summary>
	/// <remarks>
	///     Messages a page posts to itself are let through as well as the parent's: they come from
	///     code already running in the page, and the runtime may use them.
	/// </remarks>
	internal static readonly string GuardScript = """
	                                              (function () {
	                                                "use strict";
	                                                var assembly = new URLSearchParams(location.search).get("assembly");
	                                                if (assembly !== null) {
	                                                  var allowed = false;
	                                                  if (/^\/(?![\/\\])/.test(assembly)) {
	                                                    try {
	                                                      var url = new URL(assembly, location.href);
	                                                      var folder = new URL("../_preview-assemblies/", location.href).pathname;
	                                                      allowed = url.origin === location.origin && url.search === "" && url.hash === ""
	                                                        && url.pathname.indexOf(folder) === 0
	                                                        && /^[0-9a-f]{64}\.bin$/.test(url.pathname.slice(folder.length));
	                                                    } catch (e) {
	                                                      allowed = false;
	                                                    }
	                                                  }
	                                                  if (!allowed) {
	                                                    history.replaceState(null, "", location.pathname);
	                                                    window.stop();
	                                                    throw new Error("mokadocs: refused to load a preview assembly from outside _preview-assemblies");
	                                                  }
	                                                }
	                                                window.addEventListener("message", function (event) {
	                                                  if (event.origin === location.origin && (event.source === window.parent || event.source === window)) {
	                                                    return;
	                                                  }
	                                                  event.stopImmediatePropagation();
	                                                }, true);
	                                              })();
	                                              """.ReplaceLineEndings("\n");

	/// <summary>
	///     Returns <paramref name="html" /> with the policy and the guard right after its
	///     <c>&lt;head&gt;</c> tag (after a <c>&lt;meta charset&gt;</c> that comes first), replacing
	///     what an earlier call added.
	/// </summary>
	internal static HardenedPage Harden(string html)
	{
		string page = RemoveInjected(html);
		List<HtmlTag> tags = ScanTags(page);

		var hashes = new List<string> { ScriptHash(GuardScript) };
		var scriptOrigins = new List<string>();
		var styleOrigins = new List<string>();
		bool hasOwnPolicy = false;
		foreach (HtmlTag tag in tags)
		{
			if (tag.Name == "script")
			{
				if (tag.Attributes.TryGetValue("src", out string? src))
				{
					AddOrigin(scriptOrigins, src);
				}
				else
				{
					AddDistinct(hashes, ScriptHash(page[tag.ContentStart..tag.ContentEnd]));
				}
			}
			else if (tag.Name == "link" && IsStylesheet(tag) && tag.Attributes.TryGetValue("href", out string? href))
			{
				AddOrigin(styleOrigins, href);
			}
			else if (tag.Name == "meta" && tag.Attributes.TryGetValue("http-equiv", out string? equiv)
			                            && equiv.Trim().Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase))
			{
				hasOwnPolicy = true;
			}
		}

		string policy = BuildPolicy(hashes, scriptOrigins, styleOrigins);
		string injected = $"\n<meta http-equiv=\"Content-Security-Policy\" {MarkerAttribute} content=\"{policy}\">" +
		                  $"\n<script {MarkerAttribute}>{GuardScript}</script>";
		return new HardenedPage(page.Insert(InsertionPoint(page, tags), injected), policy, hasOwnPolicy);
	}

	/// <summary>
	///     Returns <paramref name="html" /> without the stylesheet links to a scoped CSS bundle
	///     (<c>{assembly}.styles.css</c> next to the page) that <paramref name="exists" /> doesn't find.
	/// </summary>
	/// <remarks>
	///     The SDK only writes the bundle when the host or a library it references has
	///     <c>.razor.css</c> files, so the link in the scaffolded page 404ed in every preview of a host
	///     without a library. The scaffold keeps the link for when a library brings scoped styles.
	/// </remarks>
	/// <param name="html">The page.</param>
	/// <param name="exists">Whether a file, relative to the page's folder, exists.</param>
	internal static string RemoveMissingScopedCss(string html, Func<string, bool> exists)
	{
		List<HtmlTag> missing = ScanTags(html)
			.Where(t => t.Name == "link" && IsStylesheet(t)
			                             && t.Attributes.TryGetValue("href", out string? href)
			                             && ScopedCssBundle(href) is { } bundle && !exists(bundle))
			.ToList();
		if (missing.Count == 0)
		{
			return html;
		}

		var sb = new StringBuilder(html);
		foreach (HtmlTag tag in Enumerable.Reverse(missing))
		{
			int start = LineStartBefore(html, tag.Start);
			sb.Remove(start, tag.End - start);
		}

		return sb.ToString();
	}

	/// <summary>
	///     The file name of a scoped CSS bundle the page links next to itself, or <c>null</c> when
	///     <paramref name="href" /> is anything else.
	/// </summary>
	private static string? ScopedCssBundle(string href)
	{
		string value = href.Trim();
		if (value.StartsWith("./", StringComparison.Ordinal))
		{
			value = value[2..];
		}

		const string suffix = ".styles.css";
		return value.Length > suffix.Length && value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
		                                    && value.IndexOfAny(['/', '\\', ':', '?', '#']) < 0
			? value
			: null;
	}

	/// <summary>
	///     Where the line holding a tag starts, line break included, when only spaces come before the
	///     tag on it; otherwise the tag's own start.
	/// </summary>
	private static int LineStartBefore(string html, int tagStart)
	{
		int start = tagStart;
		while (start > 0 && html[start - 1] is ' ' or '\t')
		{
			start--;
		}

		if (start == 0 || html[start - 1] != '\n')
		{
			return start == 0 ? 0 : tagStart;
		}

		start--;
		return start > 0 && html[start - 1] == '\r' ? start - 1 : start;
	}

	/// <summary>
	///     The CSP source expression for an inline script: the SHA-256 of its text, after the CR LF
	///     and lone CR to LF conversion the HTML parser does before the browser hashes it.
	/// </summary>
	internal static string ScriptHash(string scriptText)
	{
		string normalized = scriptText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
		return $"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))}'";
	}

	/// <remarks>
	///     Follows the client-side example in the Blazor CSP article (blazor/security/
	///     content-security-policy.md), adapted to the preview:
	///     <list type="bullet">
	///         <item><description><c>'self'</c> in <c>img-src</c>, so images from the docs origin load over plain http too (serve).</description></item>
	///         <item><description><c>font-src</c>: library stylesheets load fonts from CDNs or <c>data:</c> URLs, which index.html can't list; fonts run no code.</description></item>
	///         <item><description><c>style-src 'unsafe-inline'</c>: components and the host page write style attributes and <c>&lt;style&gt;</c> elements.</description></item>
	///         <item><description><c>connect-src 'self'</c>: the runtime fetches <c>_framework</c> files and the preview assembly from the docs origin, and nothing else.</description></item>
	///         <item><description>No <c>upgrade-insecure-requests</c>: <c>mokadocs serve</c> runs on plain http.</description></item>
	///     </list>
	///     Script and stylesheet origins the page itself links to are added, so a host that loads a
	///     CDN script keeps working. <c>frame-ancestors</c> can't be set from a meta tag.
	/// </remarks>
	private static string BuildPolicy(List<string> hashes, List<string> scriptOrigins, List<string> styleOrigins)
	{
		string scriptSources = string.Join(' ', ["'self'", "'wasm-unsafe-eval'", .. scriptOrigins, .. hashes]);
		string styleSources = string.Join(' ', ["'self'", "'unsafe-inline'", .. styleOrigins]);
		return "base-uri 'self'; default-src 'self'; img-src 'self' data: https:; font-src 'self' data: https:; " +
		       $"object-src 'none'; script-src {scriptSources}; style-src {styleSources}; connect-src 'self'";
	}

	private static bool IsStylesheet(HtmlTag tag) =>
		tag.Attributes.TryGetValue("rel", out string? rel)
		&& rel.Split(' ', '\t', '\n', '\r', '\f').Contains("stylesheet", StringComparer.OrdinalIgnoreCase);

	private static void AddOrigin(List<string> origins, string url)
	{
		string value = url.Trim();
		string? source = null;
		if (value.StartsWith("//", StringComparison.Ordinal))
		{
			// Protocol-relative: a host source without a scheme matches the page's own scheme.
			if (Uri.TryCreate("https:" + value, UriKind.Absolute, out Uri? relative))
			{
				source = relative.IdnHost + (relative.IsDefaultPort ? "" : ":" + relative.Port);
			}
		}
		else if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absolute)
		         && (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
		{
			source = $"{absolute.Scheme}://{absolute.IdnHost}" + (absolute.IsDefaultPort ? "" : ":" + absolute.Port);
		}

		if (source is not null && SourceExpression().IsMatch(source))
		{
			AddDistinct(origins, source.ToLowerInvariant());
		}
	}

	private static void AddDistinct(List<string> list, string value)
	{
		if (!list.Contains(value, StringComparer.Ordinal))
		{
			list.Add(value);
		}
	}

	private static int InsertionPoint(string page, List<HtmlTag> tags)
	{
		HtmlTag? head = tags.FirstOrDefault(t => t.Name == "head");
		if (head is not null)
		{
			// A charset declaration has to stay within the first 1024 bytes.
			HtmlTag? charset = tags.FirstOrDefault(t => t.Start >= head.End && t.Name == "meta"
			                                                                && t.Attributes.ContainsKey("charset"));
			bool loadsBeforeCharset = charset is not null && tags.Any(t =>
				t.Start >= head.End && t.Start < charset.Start && t.Name is "script" or "link" or "style" or "base");
			return charset is not null && !loadsBeforeCharset ? charset.End : head.End;
		}

		// Without a <head> tag the parser opens the head itself for a <meta> that comes before any
		// content, and a CSP meta tag counts there.
		if (tags.FirstOrDefault(t => t.Name == "html") is { } htmlTag)
		{
			return htmlTag.End;
		}

		Match doctype = Doctype().Match(page);
		return doctype.Success ? doctype.Index + doctype.Length : 0;
	}

	private static string RemoveInjected(string html)
	{
		List<HtmlTag> injected = ScanTags(html).Where(t => t.Attributes.ContainsKey(MarkerAttribute)).ToList();
		if (injected.Count == 0)
		{
			return html;
		}

		var sb = new StringBuilder(html);
		foreach (HtmlTag tag in Enumerable.Reverse(injected))
		{
			int start = tag.Start > 0 && html[tag.Start - 1] == '\n' ? tag.Start - 1 : tag.Start;
			sb.Remove(start, tag.ElementEnd - start);
		}

		return sb.ToString();
	}

	/// <summary>
	///     Start tags outside comments, with the text of raw-text elements (scripts, styles,
	///     titles) skipped so markup inside them isn't read as tags.
	/// </summary>
	private static List<HtmlTag> ScanTags(string html)
	{
		var tags = new List<HtmlTag>();
		int i = 0;
		while (i < html.Length)
		{
			int lt = html.IndexOf('<', i);
			if (lt < 0 || lt + 1 >= html.Length)
			{
				break;
			}

			if (html.AsSpan(lt).StartsWith("<!--", StringComparison.Ordinal))
			{
				i = CommentEnd(html, lt + 4);
				continue;
			}

			if (!char.IsAsciiLetter(html[lt + 1]))
			{
				i = lt + 1;
				continue;
			}

			HtmlTag tag = ReadStartTag(html, lt);
			if (_rawTextElements.Contains(tag.Name))
			{
				int endTag = FindEndTag(html, tag.Name, tag.End);
				int endTagClose = endTag < 0 ? -1 : html.IndexOf('>', endTag);
				tag = tag with
				{
					ContentEnd = endTag < 0 ? html.Length : endTag,
					ElementEnd = endTagClose < 0 ? html.Length : endTagClose + 1
				};
			}

			tags.Add(tag);
			i = tag.ElementEnd;
		}

		return tags;
	}

	private static int CommentEnd(string html, int bodyStart)
	{
		if (html.AsSpan(bodyStart).StartsWith(">", StringComparison.Ordinal))
		{
			return bodyStart + 1;
		}

		if (html.AsSpan(bodyStart).StartsWith("->", StringComparison.Ordinal))
		{
			return bodyStart + 2;
		}

		int close = html.IndexOf("-->", bodyStart, StringComparison.Ordinal);
		return close < 0 ? html.Length : close + 3;
	}

	private static HtmlTag ReadStartTag(string html, int lt)
	{
		int i = lt + 1;
		int nameStart = i;
		while (i < html.Length && !IsSpace(html[i]) && html[i] != '/' && html[i] != '>')
		{
			i++;
		}

		string name = html[nameStart..i].ToLowerInvariant();
		var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
		while (i < html.Length)
		{
			while (i < html.Length && (IsSpace(html[i]) || html[i] == '/'))
			{
				i++;
			}

			if (i >= html.Length || html[i] == '>')
			{
				break;
			}

			int attributeStart = i;
			while (i < html.Length && !IsSpace(html[i]) && html[i] != '/' && html[i] != '>'
			       && (html[i] != '=' || i == attributeStart))
			{
				i++;
			}

			string attributeName = html[attributeStart..i].ToLowerInvariant();
			string value = "";
			int afterName = i;
			while (i < html.Length && IsSpace(html[i]))
			{
				i++;
			}

			if (i < html.Length && html[i] == '=')
			{
				i++;
				while (i < html.Length && IsSpace(html[i]))
				{
					i++;
				}

				if (i < html.Length && html[i] is '"' or '\'')
				{
					char quote = html[i];
					int close = html.IndexOf(quote, i + 1);
					int valueEnd = close < 0 ? html.Length : close;
					value = html[(i + 1)..valueEnd];
					i = close < 0 ? html.Length : close + 1;
				}
				else
				{
					int valueStart = i;
					while (i < html.Length && !IsSpace(html[i]) && html[i] != '>')
					{
						i++;
					}

					value = html[valueStart..i];
				}
			}
			else
			{
				i = afterName;
			}

			attributes.TryAdd(attributeName, WebUtility.HtmlDecode(value));
		}

		int end = i < html.Length ? i + 1 : html.Length;
		return new HtmlTag(name, attributes, lt, end, end, end, end);
	}

	private static int FindEndTag(string html, string name, int from)
	{
		int i = from;
		while ((i = html.IndexOf("</", i, StringComparison.Ordinal)) >= 0)
		{
			int afterName = i + 2 + name.Length;
			if (afterName <= html.Length
			    && html.AsSpan(i + 2, name.Length).Equals(name, StringComparison.OrdinalIgnoreCase)
			    && (afterName == html.Length || IsSpace(html[afterName]) || html[afterName] is '/' or '>'))
			{
				return i;
			}

			i += 2;
		}

		return -1;
	}

	private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';

	[GeneratedRegex("^(https?://)?[a-z0-9.-]+(:[0-9]{1,5})?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex SourceExpression();

	[GeneratedRegex(@"\A\uFEFF?\s*<!doctype[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex Doctype();

	/// <summary>A start tag, and for raw-text elements where their text and end tag end.</summary>
	private sealed record HtmlTag(
		string Name,
		Dictionary<string, string> Attributes,
		int Start,
		int End,
		int ContentStart,
		int ContentEnd,
		int ElementEnd);
}

/// <summary>A preview host page with the policy and the guard added.</summary>
/// <param name="Html">The page.</param>
/// <param name="Policy">The Content-Security-Policy it now carries.</param>
/// <param name="HasOwnPolicy">
///     Whether the page already had a policy of its own, which the browser enforces as well and
///     which has to allow the guard.
/// </param>
internal sealed record HardenedPage(string Html, string Policy, bool HasOwnPolicy);
