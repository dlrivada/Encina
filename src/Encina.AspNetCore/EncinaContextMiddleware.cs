using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Middleware that binds the request identity of every HTTP request: it builds an
/// <see cref="InboundRequestInfo"/> from the <see cref="HttpContext"/> and runs the rest of the
/// pipeline inside one inbound identity scope, so every <c>IEncina.Send</c>, <c>Publish</c> and
/// <c>Stream</c> of the request sees the caller's <see cref="RequestIdentity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Applications add it with <c>app.UseEncinaContext()</c>. The flow of one request:
/// </para>
/// <list type="number">
/// <item><description>After a detected misordering (below), every request is answered 500 without
/// running the rest of the pipeline.</description></item>
/// <item><description>A connection request (a WebSocket upgrade, any extended CONNECT, a GET whose
/// <c>Accept</c> lists <c>text/event-stream</c>, a SignalR hub endpoint) binds no identity: the rest
/// of the pipeline runs under the anonymous connection marker, because the connection outlives the
/// request that opened it.</description></item>
/// <item><description>Any other request runs inside one inbound scope opened with the identity
/// mapped from <see cref="HttpContext.User"/>; when the request completes the scope ends, so a task
/// that outlives it reads the anonymous identity.</description></item>
/// <item><description>A refused scope answers 500 without running the rest of the pipeline (a host
/// misconfiguration, such as <c>UseEncinaContext()</c> registered twice); a request aborted before
/// the scope opened gets no response.</description></item>
/// <item><description>When no endpoint was resolved before the middleware and the request ended on a
/// hub endpoint with an unchanged path, <c>UseEncinaContext()</c> runs before <c>UseRouting()</c>:
/// Critical 202, once, and every later request of this pipeline is answered 500. When the path
/// changed inside the pipeline (a rewrite registered after the middleware), Warning 203 and no
/// latch.</description></item>
/// </list>
/// <para>
/// The latch is a field of this instance, so it lives as long as the pipeline that built it and
/// never affects another host in the same process.
/// </para>
/// </remarks>
internal sealed class EncinaContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EncinaAspNetCoreOptions _options;
    private readonly ILogger _logger;
    private int _misordered;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaContextMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The header names.</param>
    /// <param name="logger">The logger (EventIds 202-203).</param>
    public EncinaContextMiddleware(
        RequestDelegate next,
        IOptions<EncinaAspNetCoreOptions> options,
        ILogger<EncinaContextMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether this middleware detected that it runs before routing.
    /// </summary>
    internal bool IsMisordered => Volatile.Read(ref _misordered) != 0;

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="scopes">The scope factory (the internal host-adapter members).</param>
    /// <returns>A task that completes with the request.</returns>
    public Task InvokeAsync(HttpContext context, IInternalRequestContextScopeFactory scopes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scopes);

        if (IsMisordered)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }

        // Only a request with no endpoint yet can reveal a misordering; the hot path reads the endpoint once.
        var endpointBefore = context.GetEndpoint();
        var pathBefore = endpointBefore is null ? context.Request.Path : default;
        return ConnectionRequestDetector.IsConnectionRequest(context)
            ? RunConnectionAsync(context, scopes, endpointBefore, pathBefore)
            : RunInboundAsync(context, scopes, endpointBefore, pathBefore);
    }

    /// <summary>
    /// Classifies a request after the rest of the pipeline ran: a misordering (no endpoint before, a
    /// hub endpoint after, same path), a re-route to a hub inside the pipeline (path changed), or
    /// nothing.
    /// </summary>
    internal static OrderingSignal Classify(Endpoint? before, Endpoint? after, PathString pathBefore, PathString pathAfter)
    {
        if (before is not null || !ConnectionRequestDetector.IsHubEndpoint(after))
        {
            return OrderingSignal.None;
        }

        return pathBefore == pathAfter ? OrderingSignal.BeforeRouting : OrderingSignal.PathChanged;
    }

    private async Task RunConnectionAsync(HttpContext context, IInternalRequestContextScopeFactory scopes, Endpoint? endpointBefore, PathString pathBefore)
    {
        var result = await scopes.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ => _next(context), context.RequestAborted)
            .ConfigureAwait(false);
        Complete(context, result, endpointBefore, pathBefore);
    }

    private async Task RunInboundAsync(HttpContext context, IInternalRequestContextScopeFactory scopes, Endpoint? endpointBefore, PathString pathBefore)
    {
        var info = HttpContextInboundRequestExtensions.CreateInboundRequestInfo(context, _options);
        var result = await scopes.RunHostInboundAsync(info, (requestContext, _) => RunNextAsync(context, requestContext), context.RequestAborted)
            .ConfigureAwait(false);
        Complete(context, result, endpointBefore, pathBefore);
    }

    private async Task<Either<EncinaError, Unit>> RunNextAsync(HttpContext context, IRequestContext requestContext)
    {
        context.Response.Headers[_options.CorrelationIdHeader] = requestContext.CorrelationId;
        await _next(context).ConfigureAwait(false);
        return Unit.Default;
    }

    // A refused scope never ran the pipeline: an aborted request gets nothing, any other refusal is a
    // host misconfiguration (the factory already logged its code) and fails closed with 500.
    private void Complete(HttpContext context, Either<EncinaError, Unit> result, Endpoint? endpointBefore, PathString pathBefore)
    {
        result.Match(
            Right: _ => CheckOrdering(context, endpointBefore, pathBefore),
            Left: error => Refuse(context, error));
    }

    private static void Refuse(HttpContext context, EncinaError error)
    {
        if (!error.GetCode().Exists(static code => code == EncinaErrorCodes.RequestCancelled))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }

    private void CheckOrdering(HttpContext context, Endpoint? endpointBefore, PathString pathBefore)
    {
        var signal = Classify(endpointBefore, context.GetEndpoint(), pathBefore, context.Request.Path);
        if (signal == OrderingSignal.PathChanged)
        {
            EncinaContextMiddlewareLog.EncinaContextPathChanged(_logger);
        }
        else if (signal == OrderingSignal.BeforeRouting && Interlocked.Exchange(ref _misordered, 1) == 0)
        {
            EncinaContextMiddlewareLog.EncinaContextBeforeRouting(_logger);
        }
    }

    /// <summary>
    /// What a completed request revealed about the pipeline order.
    /// </summary>
    internal enum OrderingSignal
    {
        /// <summary>Nothing to report.</summary>
        None,

        /// <summary><c>UseEncinaContext()</c> runs before <c>UseRouting()</c>: latch and Critical 202.</summary>
        BeforeRouting,

        /// <summary>A rewrite or re-execute inside the pipeline reached a hub: Warning 203, no latch.</summary>
        PathChanged
    }
}
