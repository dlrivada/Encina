using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Builds the <see cref="InboundRequestInfo"/> of an HTTP request: the one builder that
/// <c>UseEncinaContext()</c> and server-sent event endpoints use.
/// </summary>
public static class HttpContextInboundRequestExtensions
{
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string UserAgentHeader = "User-Agent";

    /// <summary>
    /// Copies everything the request carries about its caller into an <see cref="InboundRequestInfo"/>:
    /// the authenticated principal, the correlation id, the tenant header value, the idempotency key,
    /// the IP address, the user agent and the data region hint.
    /// </summary>
    /// <param name="context">The HTTP context of an active request.</param>
    /// <returns>The inbound information; it holds copies, never the <see cref="HttpContext"/>.</returns>
    /// <remarks>
    /// <para>
    /// The header names come from <see cref="EncinaAspNetCoreOptions"/> registered in
    /// <see cref="HttpContext.RequestServices"/> (the defaults when none is registered). The correlation
    /// id is the <see cref="EncinaAspNetCoreOptions.CorrelationIdHeader"/> value, otherwise the current
    /// activity id; the IP address is the first <c>X-Forwarded-For</c> entry, otherwise the remote
    /// address. Every value except the principal is client-controlled: the scope factory normalizes
    /// them and never refuses a scope because of them.
    /// </para>
    /// <para>
    /// <b>Server-sent events.</b> A GET request whose <c>Accept</c> header lists
    /// <c>text/event-stream</c> runs anonymous under the connection marker, so an SSE endpoint opts in
    /// per unit of work: call <c>RunInboundAsync(context.CreateInboundRequestInfo(), …)</c> around
    /// <b>one event or one dispatch</b>, never around the whole stream, so no identity is kept between
    /// events. The principal is the one the connection authenticated with; the endpoint decides when it
    /// is stale (token expiry, revocation).
    /// </para>
    /// <para>
    /// <b>Limits.</b> The tenant comes from the principal's tenant claim or the
    /// <see cref="EncinaAspNetCoreOptions.TenantIdHeader"/> value only: a tenant resolved from the
    /// route or the subdomain is lost on this path, because the tenant resolution middleware writes no
    /// context on a connection request. <see cref="HttpContext"/> is pooled and reused once the request
    /// completes: call this method while the request is active and never capture the
    /// <see cref="HttpContext"/> in the event producer. The returned record copies every value and is
    /// safe to keep.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("/events", async (HttpContext context, IRequestContextScopeFactory scopes, IEncina encina, CancellationToken ct) =>
    /// {
    ///     context.Response.ContentType = "text/event-stream";
    ///     var info = context.CreateInboundRequestInfo();
    ///     foreach (var tick in Enumerable.Range(0, 3))
    ///     {
    ///         // One scope per event: the identity ends with the event.
    ///         var result = await scopes.RunInboundAsync(info, (_, token) => encina.Send(new NextEvent(tick), token).AsTask(), ct);
    ///         await context.Response.WriteAsync($"data: {result.IsRight}\n\n", ct);
    ///     }
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static InboundRequestInfo CreateInboundRequestInfo(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = context.RequestServices?.GetService<IOptions<EncinaAspNetCoreOptions>>()?.Value ?? new EncinaAspNetCoreOptions();
        return CreateInboundRequestInfo(context, options);
    }

    /// <summary>
    /// Builds the inbound information with the given header names (the middleware's own options).
    /// </summary>
    internal static InboundRequestInfo CreateInboundRequestInfo(HttpContext context, EncinaAspNetCoreOptions options)
    {
        var headers = context.Request.Headers;
        return new InboundRequestInfo(
            context.User,
            CorrelationId: Header(headers, options.CorrelationIdHeader) ?? Activity.Current?.Id,
            TenantHeaderValue: Header(headers, options.TenantIdHeader),
            IdempotencyKey: Header(headers, options.IdempotencyKeyHeader),
            IpAddress: ForwardedFor(headers) ?? context.Connection.RemoteIpAddress?.ToString(),
            UserAgent: Header(headers, UserAgentHeader),
            DataRegion: Header(headers, options.DataRegionHeaderName));
    }

    private static string? Header(IHeaderDictionary headers, string name) =>
        headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString() : null;

    // "client, proxy1, proxy2": the first entry is the original client.
    private static string? ForwardedFor(IHeaderDictionary headers) =>
        Header(headers, ForwardedForHeader)?.Split(',', StringSplitOptions.TrimEntries)[0] is { Length: > 0 } first ? first : null;
}
