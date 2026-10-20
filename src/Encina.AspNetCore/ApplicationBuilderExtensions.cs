using Microsoft.AspNetCore.Builder;

namespace Encina.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to configure Encina middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the middleware that binds the request identity: every HTTP request runs inside one
    /// inbound identity scope built from the authenticated <c>HttpContext.User</c>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The scope carries the caller's <see cref="RequestIdentity"/> (mapped through
    /// <see cref="RequestIdentityOptions"/>), the correlation id, the tenant (the principal's tenant
    /// claim, otherwise the tenant header), the idempotency key, and the IP address, user agent and
    /// data region hint. It ends when the request completes, so work that outlives the request reads
    /// the anonymous identity.
    /// </para>
    /// <para>
    /// <b>Order.</b> <c>UseRouting()</c>, then <c>UseRewriter()</c>, <c>UseStatusCodePagesWithReExecute()</c>
    /// and <c>UseExceptionHandler()</c>, then <c>UseAuthentication()</c>, then <c>UseEncinaContext()</c>.
    /// The middleware reads the routed endpoint to recognize SignalR hub endpoints. When it runs before
    /// routing and a request reaches a hub endpoint with an unchanged path, it logs Critical 202 once and
    /// answers 500 to every later request (fail closed); a rewrite registered after it that reaches a
    /// hub logs Warning 203 instead.
    /// </para>
    /// <para>
    /// <b>Connections.</b> WebSocket upgrades, extended CONNECT requests, GET requests whose
    /// <c>Accept</c> lists <c>text/event-stream</c> and SignalR hub endpoints (including Blazor's
    /// <c>/_blazor</c>) carry no request identity: they run under an anonymous connection marker, so
    /// their dispatches are denied by every identity gate. A Blazor Server circuit gets its identity per
    /// activity from <c>AddEncinaBlazorAuthorization()</c>; a server-sent event endpoint opts in per event
    /// with <see cref="HttpContextInboundRequestExtensions.CreateInboundRequestInfo(Microsoft.AspNetCore.Http.HttpContext)"/>.
    /// A POST that streams its response is an ordinary request and keeps its identity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// app.UseRouting();
    /// app.UseExceptionHandler("/error");
    /// app.UseAuthentication();
    /// app.UseEncinaContext(); // after routing and authentication
    /// app.UseAuthorization();
    ///
    /// app.MapControllers();
    /// app.Run();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseEncinaContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EncinaContextMiddleware>();
    }
}
