namespace Moka.Docs.AspNetCore;

/// <summary>
///     A vote from the "Was this page helpful?" widget, passed to
///     <see cref="MokaDocsOptions.OnFeedback" />.
/// </summary>
/// <param name="Page">The path of the page the reader voted on, base path included, such as <c>/docs/guide/setup</c>.</param>
/// <param name="Helpful">Whether the reader found the page helpful.</param>
public sealed record DocsFeedback(string Page, bool Helpful);
