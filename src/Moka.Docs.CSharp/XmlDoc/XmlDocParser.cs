using System.Text;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moka.Docs.Core.Api;

namespace Moka.Docs.CSharp.XmlDoc;

/// <summary>
///     Parses a .NET XML documentation file (the output of <c>&lt;DocumentationFile&gt;</c>)
///     into a dictionary of member ID → <see cref="XmlDocBlock" />.
/// </summary>
public sealed class XmlDocParser(ILogger<XmlDocParser> logger)
{
    /// <summary>
    ///     Parses an XML documentation file from a file path.
    /// </summary>
    /// <param name="xmlDocPath">Absolute path to the .xml documentation file.</param>
    /// <returns>A dictionary mapping member ID strings to parsed doc blocks.</returns>
    public XmlDocFile Parse(string xmlDocPath)
    {
        if (!File.Exists(xmlDocPath))
        {
            logger.LogWarning("XML documentation file not found: {Path}", xmlDocPath);
            return XmlDocFile.Empty;
        }

        try
        {
            var doc = XDocument.Load(xmlDocPath);
            return ParseDocument(doc);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse XML documentation file: {Path}", xmlDocPath);
            return XmlDocFile.Empty;
        }
    }

    /// <summary>
    ///     Parses an XML documentation file from a string.
    /// </summary>
    public XmlDocFile ParseXml(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return ParseDocument(doc);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse XML documentation content");
            return XmlDocFile.Empty;
        }
    }

    private XmlDocFile ParseDocument(XDocument doc)
    {
        var result = new Dictionary<string, XmlDocBlock>(StringComparer.Ordinal);

        var assemblyName = doc.Root?.Element("assembly")?.Element("name")?.Value ?? "";
        var members = doc.Root?.Element("members")?.Elements("member") ?? [];

        foreach (var member in members)
        {
            var name = member.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;

            try
            {
                var block = ParseMemberElement(member);
                result[name] = block;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to parse documentation for member: {MemberId}", name);
            }
        }

        logger.LogInformation("Parsed {Count} documentation entries from {Assembly}",
            result.Count, assemblyName);

        return new XmlDocFile
        {
            AssemblyName = assemblyName,
            Members = result
        };
    }

    private static XmlDocBlock ParseMemberElement(XElement member)
    {
        return new XmlDocBlock
        {
            Summary = RenderInnerXml(member.Element("summary")),
            Remarks = RenderInnerXml(member.Element("remarks")),
            Returns = RenderInnerXml(member.Element("returns")),
            Value = RenderInnerXml(member.Element("value")),
            Parameters = ParseParamElements(member, "param"),
            TypeParameters = ParseParamElements(member, "typeparam"),
            Exceptions = ParseExceptions(member),
            Examples = ParseExamples(member),
            SeeAlso = ParseSeeAlso(member)
        };
    }

    private static Dictionary<string, string> ParseParamElements(XElement member, string elementName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var param in member.Elements(elementName))
        {
            var name = param.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;

            result[name] = RenderInnerXml(param);
        }

        return result;
    }

    private static List<ExceptionDoc> ParseExceptions(XElement member)
    {
        var result = new List<ExceptionDoc>();

        foreach (var ex in member.Elements("exception"))
        {
            var cref = ex.Attribute("cref")?.Value ?? "";
            // Strip the "T:" prefix from exception type crefs
            if (cref.StartsWith("T:"))
                cref = cref[2..];

            result.Add(new ExceptionDoc
            {
                Type = cref,
                Description = RenderInnerXml(ex)
            });
        }

        return result;
    }

    private static List<string> ParseExamples(XElement member)
    {
        return member.Elements("example")
            .Select(e => RenderInnerXml(e))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static List<string> ParseSeeAlso(XElement member)
    {
        return member.Elements("seealso")
            .Select(e => e.Attribute("cref")?.Value ?? e.Value)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    /// <summary>
    ///     Renders the inner content of an XML doc element to HTML-like text.
    ///     Handles nested elements like &lt;c&gt;, &lt;code&gt;, &lt;para&gt;, &lt;see&gt;, etc.
    /// </summary>
    internal static string RenderInnerXml(XElement? element)
    {
        if (element is null) return "";

        var sb = new StringBuilder();

        foreach (var node in element.Nodes())
            switch (node)
            {
                case XText text:
                    sb.Append(NormalizeWhitespace(text.Value));
                    break;

                case XElement child:
                    RenderChildElement(sb, child);
                    break;
            }

        return sb.ToString().Trim();
    }

    private static void RenderChildElement(StringBuilder sb, XElement element)
    {
        switch (element.Name.LocalName)
        {
            case "c":
                sb.Append("<code>");
                sb.Append(element.Value);
                sb.Append("</code>");
                break;

            case "code":
                sb.Append("<pre><code>");
                sb.Append(element.Value);
                sb.Append("</code></pre>");
                break;

            case "para":
                sb.Append("<p>");
                sb.Append(RenderInnerXml(element));
                sb.Append("</p>");
                break;

            case "paramref":
                var paramName = element.Attribute("name")?.Value ?? "";
                sb.Append($"<code>{paramName}</code>");
                break;

            case "typeparamref":
                var typeParamName = element.Attribute("name")?.Value ?? "";
                sb.Append($"<code>{typeParamName}</code>");
                break;

            case "see":
                var cref = element.Attribute("cref")?.Value;
                var href = element.Attribute("href")?.Value;
                var linkText = element.Value;

                if (!string.IsNullOrEmpty(cref))
                {
                    var displayName = MemberIdParser.GetDisplayName(cref);
                    var escapedCref = HttpUtility.HtmlAttributeEncode(cref);
                    var escapedText = HttpUtility.HtmlEncode(string.IsNullOrEmpty(linkText) ? displayName : linkText);
                    sb.Append($"<a data-cref=\"{escapedCref}\">{escapedText}</a>");
                }
                else if (!string.IsNullOrEmpty(href))
                {
                    var escapedHref = HttpUtility.HtmlAttributeEncode(href);
                    var escapedText = HttpUtility.HtmlEncode(string.IsNullOrEmpty(linkText) ? href : linkText);
                    sb.Append($"<a href=\"{escapedHref}\">{escapedText}</a>");
                }

                break;

            case "list":
                RenderList(sb, element);
                break;

            case "br":
                sb.Append("<br />");
                break;

            default:
                // Unknown element — render its text content
                sb.Append(element.Value);
                break;
        }
    }

    private static void RenderList(StringBuilder sb, XElement listElement)
    {
        var type = listElement.Attribute("type")?.Value ?? "bullet";
        var tag = type == "number" ? "ol" : "ul";

        sb.Append($"<{tag}>");
        foreach (var item in listElement.Elements("item"))
        {
            sb.Append("<li>");
            var desc = item.Element("description");
            if (desc is not null)
                sb.Append(RenderInnerXml(desc));
            else
                sb.Append(RenderInnerXml(item));
            sb.Append("</li>");
        }

        sb.Append($"</{tag}>");
    }

    private static string NormalizeWhitespace(string text)
    {
        // Collapse multiple whitespace characters into single spaces
        var result = new StringBuilder(text.Length);
        var lastWasWhitespace = false;

        foreach (var ch in text)
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasWhitespace)
                {
                    result.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                result.Append(ch);
                lastWasWhitespace = false;
            }

        return result.ToString();
    }
}

/// <summary>
///     Represents a parsed XML documentation file.
/// </summary>
public sealed record XmlDocFile
{
    /// <summary>The assembly name from the XML doc file.</summary>
    public string AssemblyName { get; init; } = "";

    /// <summary>All member documentation entries, keyed by member ID string.</summary>
    public Dictionary<string, XmlDocBlock> Members { get; init; } = [];

    /// <summary>An empty doc file.</summary>
    public static XmlDocFile Empty => new();

    /// <summary>
    ///     Gets the documentation for a member by its ID string.
    /// </summary>
    public XmlDocBlock? GetMemberDoc(string memberId)
    {
        return Members.GetValueOrDefault(memberId);
    }
}