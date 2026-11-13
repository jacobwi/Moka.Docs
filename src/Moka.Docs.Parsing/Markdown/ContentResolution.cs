using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Moka.Docs.Parsing.Markdown;

/// <summary>
///     Fills in what a parsed page refers to outside itself: <c>xref:</c> links become links to API pages,
///     glossary terms get their definitions, <c>:::glossary</c> gets the terms and <c>:::package</c> a known
///     version. What doesn't resolve is reported on the document (<see cref="MarkdownWarnings" />).
/// </summary>
internal static class ContentResolution
{
	private const string _xrefScheme = "xref:";

	internal static void Apply(MarkdownDocument document, ContentReferences references)
	{
		ResolveLinks(document, references);
		ResolveAutolinks(document, references);

		foreach (TermInline term in document.Descendants<TermInline>())
		{
			term.Definition = Define(references, term.Term);
			if (term.Definition is null)
			{
				MarkdownWarnings.Add(document, term.Line,
					$":term[{InlineText.Of(term)}] isn't in the glossary, so it is shown as plain text");
			}
		}

		foreach (GlossaryBlock glossary in document.Descendants<GlossaryBlock>())
		{
			glossary.Terms = references.SortedGlossary;
			if (glossary.Terms.Count == 0)
			{
				MarkdownWarnings.Add(document, glossary.Line,
					"':::glossary' has nothing to list: the site has no glossary terms ('glossary' in mokadocs.yaml " +
					"or a glossary.yml file in the docs folder)");
			}
		}

		foreach (PackageBlock package in document.Descendants<PackageBlock>())
		{
			if (package.Version is null && package.PackageId.Length > 0)
			{
				package.Version = references.VersionOf(package.PackageId);
			}
		}
	}

	/// <summary>Whether a link's URL is an <c>xref:</c> reference.</summary>
	internal static bool IsXref(string? url) =>
		url is { Length: > 5 } && url.StartsWith(_xrefScheme, StringComparison.OrdinalIgnoreCase);

	/// <summary>The reference of an <c>xref:</c> URL: the text after the scheme, percent-decoded.</summary>
	internal static string Reference(string url) => Uri.UnescapeDataString(url[_xrefScheme.Length..].Trim());

	// "APIs" finds "API" and "processes" finds "process" when only the singular is defined.
	private static string? Define(ContentReferences references, string term)
	{
		if (references.Define(term) is { } definition)
		{
			return definition;
		}

		if (term.EndsWith("es", StringComparison.OrdinalIgnoreCase) && references.Define(term[..^2]) is { } withoutEs)
		{
			return withoutEs;
		}

		return term.EndsWith('s') || term.EndsWith('S') ? references.Define(term[..^1]) : null;
	}

	// [text](xref:Ns.Type) links to the type's page and keeps its text. One that doesn't resolve keeps its text
	// without the link; [](xref:Ns.Type) with no text of its own shows the reference as code.
	private static void ResolveLinks(MarkdownDocument document, ContentReferences references)
	{
		foreach (LinkInline link in document.Descendants<LinkInline>().ToList())
		{
			if (link.IsImage || !IsXref(link.Url))
			{
				continue;
			}

			string reference = Reference(link.Url!);
			if (references.ResolveXref?.Invoke(reference) is { } target)
			{
				link.Url = target.Url;
				link.GetAttributes().AddClass("xref");
				if (link.FirstChild is null)
				{
					link.AppendChild(new LiteralInline(target.Text));
				}

				continue;
			}

			Unresolved(document, link.Line, reference);
			if (link.FirstChild is null)
			{
				link.ReplaceBy(Code(link, reference), false);
				continue;
			}

			// The text stays, in a plain container that renders only its children. (Moving the children out
			// one by one with InsertBefore left their PreviousSibling unset, and inlines before them went
			// missing from later walks of the tree.)
			link.ReplaceBy(new ContainerInline { IsClosed = true });
		}
	}

	// <xref:Ns.Type> becomes a link whose text is the type's name, or code when it doesn't resolve.
	private static void ResolveAutolinks(MarkdownDocument document, ContentReferences references)
	{
		foreach (AutolinkInline autolink in document.Descendants<AutolinkInline>().ToList())
		{
			if (autolink.IsEmail || !IsXref(autolink.Url))
			{
				continue;
			}

			string reference = Reference(autolink.Url);
			if (references.ResolveXref?.Invoke(reference) is not { } target)
			{
				Unresolved(document, autolink.Line, reference);
				autolink.ReplaceBy(Code(autolink, reference), false);
				continue;
			}

			var link = new LinkInline(target.Url, "")
			{
				Span = autolink.Span,
				Line = autolink.Line,
				Column = autolink.Column,
				IsClosed = true
			};
			link.GetAttributes().AddClass("xref");
			link.AppendChild(new LiteralInline(target.Text));
			autolink.ReplaceBy(link, false);
		}
	}

	private static CodeInline Code(Inline replaced, string reference) => new(reference)
	{
		Span = replaced.Span,
		Line = replaced.Line,
		Column = replaced.Column,
		Delimiter = '`',
		DelimiterCount = 1
	};

	private static void Unresolved(MarkdownDocument document, int line, string reference) =>
		MarkdownWarnings.Add(document, line,
			$"xref:{reference} doesn't match a documented type or member, so it isn't a link");
}
