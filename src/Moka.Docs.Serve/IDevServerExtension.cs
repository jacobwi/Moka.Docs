using Microsoft.AspNetCore.Routing;

namespace Moka.Docs.Serve;

/// <summary>
///     Adds dev-only behavior to a <see cref="DevServer" />: endpoints, and HTML added to the pages
///     it serves. Register extensions in <see cref="DevServer.Extensions" /> before
///     <see cref="DevServer.StartAsync" />.
/// </summary>
public interface IDevServerExtension
{
	/// <summary>
	///     HTML added before <c>&lt;/body&gt;</c> of every page the server adds its reload script to,
	///     after that script, or <c>null</c> for none. Blazor preview frames (<c>/_preview-wasm/</c>)
	///     get neither.
	/// </summary>
	string? PageHtml { get; }

	/// <summary>
	///     Maps the extension's endpoints. Paths are matched with the base path removed, and
	///     <c>GET</c> and <c>HEAD</c> requests for files in the site are served before endpoints
	///     are matched. A request to <c>/api/</c> with any other method has to pass the server's
	///     same-origin check before it reaches an endpoint, so put endpoints that change anything
	///     there.
	/// </summary>
	/// <param name="endpoints">The server's endpoint route builder.</param>
	void MapEndpoints(IEndpointRouteBuilder endpoints);
}
