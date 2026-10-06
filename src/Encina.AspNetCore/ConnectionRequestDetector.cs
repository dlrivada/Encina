using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Encina.AspNetCore;

/// <summary>
/// Recognizes the requests that open a long-lived connection, which must not keep the identity of
/// the request that opened them (#1705, Design 2 step 1).
/// </summary>
/// <remarks>
/// <c>Encina.Tenancy.AspNetCore</c> keeps a private copy of this predicate (it does not reference this
/// package); a shared test table runs both copies over the same cases.
/// </remarks>
internal static class ConnectionRequestDetector
{
    private const string WebSocketToken = "websocket";
    private const string EventStreamMediaType = "text/event-stream";

    /// <summary>
    /// Determines whether <paramref name="context"/> opens a connection: a WebSocket upgrade (detected
    /// from the request itself, before <c>UseWebSockets</c> runs), any extended CONNECT request, a GET
    /// whose <c>Accept</c> header lists <c>text/event-stream</c>, or a request routed to a hub
    /// endpoint.
    /// </summary>
    internal static bool IsConnectionRequest(HttpContext context) =>
        IsWebSocketUpgrade(context)
        || context.Features.Get<IHttpExtendedConnectFeature>() is { IsExtendedConnect: true }
        || context.WebSockets.IsWebSocketRequest
        || IsEventStreamGet(context.Request)
        || IsHubEndpoint(context.GetEndpoint());

    /// <summary>Determines whether <paramref name="endpoint"/> is a SignalR hub endpoint (including <c>/negotiate</c>).</summary>
    internal static bool IsHubEndpoint(Endpoint? endpoint) => endpoint?.Metadata.GetMetadata<HubMetadata>() is not null;

    // HTTP/1.1: an upgradable request whose Upgrade header lists the websocket token.
    private static bool IsWebSocketUpgrade(HttpContext context) =>
        context.Features.Get<IHttpUpgradeFeature>() is { IsUpgradableRequest: true }
        && ListsToken(context.Request.Headers.Upgrade, WebSocketToken);

    // Classic SSE and EventSource always send a GET; a POST that streams its response keeps its identity.
    private static bool IsEventStreamGet(HttpRequest request) =>
        HttpMethods.IsGet(request.Method) && AcceptsEventStream(request.Headers.Accept);

    // Parsed media types (parameters and q ignored, wildcards excluded); a malformed header errs on the skip.
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

    // Comma-separated protocol tokens, each optionally "name/version".
    private static bool ListsToken(StringValues values, string token) =>
        values.Any(value => value is not null && value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => entry.Split('/')[0].Equals(token, StringComparison.OrdinalIgnoreCase)));
}
