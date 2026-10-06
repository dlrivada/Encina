using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Recognizes the requests that open a long-lived connection, on which the tenant middleware writes
/// no request context (the connection would keep the connect-time tenant for its whole life).
/// </summary>
/// <remarks>
/// A private copy of the predicate of <c>Encina.AspNetCore</c>'s request middleware: this package
/// does not reference <c>Encina.AspNetCore</c>. A shared test table runs both copies over the same
/// cases, so keep them identical.
/// </remarks>
internal static class ConnectionRequests
{
    private const string WebSocketToken = "websocket";
    private const string EventStreamMediaType = "text/event-stream";

    /// <summary>
    /// Determines whether <paramref name="context"/> opens a connection: a WebSocket upgrade (detected
    /// from the request itself), any extended CONNECT request, a GET whose <c>Accept</c> header lists
    /// <c>text/event-stream</c>, or a request routed to a SignalR hub endpoint.
    /// </summary>
    internal static bool IsConnectionRequest(HttpContext context) =>
        IsWebSocketUpgrade(context)
        || context.Features.Get<IHttpExtendedConnectFeature>() is { IsExtendedConnect: true }
        || context.WebSockets.IsWebSocketRequest
        || IsEventStreamGet(context.Request)
        || context.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() is not null;

    private static bool IsWebSocketUpgrade(HttpContext context) =>
        context.Features.Get<IHttpUpgradeFeature>() is { IsUpgradableRequest: true }
        && ListsToken(context.Request.Headers.Upgrade, WebSocketToken);

    private static bool IsEventStreamGet(HttpRequest request) =>
        HttpMethods.IsGet(request.Method) && AcceptsEventStream(request.Headers.Accept);

    private static bool AcceptsEventStream(StringValues accept)
    {
        if (StringValues.IsNullOrEmpty(accept))
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParseList(accept, out var mediaTypes)
            ? mediaTypes.Any(static mediaType => mediaType.MediaType.Equals(EventStreamMediaType, StringComparison.OrdinalIgnoreCase))
            : accept.Any(static value => value?.Contains(EventStreamMediaType, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool ListsToken(StringValues values, string token) =>
        values.Any(value => value is not null && value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => entry.Split('/')[0].Equals(token, StringComparison.OrdinalIgnoreCase)));
}
