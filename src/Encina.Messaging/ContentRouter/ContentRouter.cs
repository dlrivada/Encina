using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Routes messages to handlers based on content inspection.
/// </summary>
/// <remarks>
/// <para>
/// The Content-Based Router is an Enterprise Integration Pattern that inspects
/// the content of a message and routes it to the appropriate handler(s) based
/// on configurable routing rules.
/// </para>
/// <para>
/// This implementation supports:
/// <list type="bullet">
/// <item><description>Multiple conditional routes with priorities</description></item>
/// <item><description>Default route for fallback handling</description></item>
/// <item><description>Optional parallel route execution</description></item>
/// <item><description>Route caching for performance optimization</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ContentRouter : IContentRouter
{
    private readonly ContentRouterOptions _options;
    private readonly ILogger<ContentRouter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentRouter"/> class.
    /// </summary>
    /// <param name="options">The router options.</param>
    /// <param name="logger">The logger.</param>
    public ContentRouter(ContentRouterOptions options, ILogger<ContentRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ContentRouterResult<TResult>>> RouteAsync<TMessage, TResult>(
        BuiltContentRouterDefinition<TMessage, TResult> definition,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(message);

        var routingId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        ContentRouterLog.RoutingStarted(_logger, routingId, typeof(TMessage).Name, definition.RouteCount);

        try
        {
            // Find matching routes
            var matchingRoutes = FindMatchingRoutes(definition, message, routingId);

            if (matchingRoutes.Count == 0)
            {
                // Use default route if available
                if (definition.DefaultRoute is not null)
                {
                    ContentRouterLog.UsingDefaultRoute(_logger, routingId);
                    return await ExecuteDefaultRouteAsync(
                        definition.DefaultRoute, message, routingId, stopwatch, cancellationToken)
                        .ConfigureAwait(false);
                }

                // No matches and no default
                if (_options.ThrowOnNoMatch)
                {
                    ContentRouterLog.NoMatchingRoute(_logger, routingId, typeof(TMessage).Name);
                    return EncinaErrors.Create(
                        ContentRouterErrorCodes.NoMatchingRoute,
                        $"No matching route found for message of type '{typeof(TMessage).Name}'");
                }

                stopwatch.Stop();
                ContentRouterLog.RoutingCompletedNoMatch(_logger, routingId, stopwatch.Elapsed);
                return ContentRouterResult.Empty<TResult>();
            }

            // Execute matching routes
            var results = _options.AllowMultipleMatches
                ? await ExecuteMultipleRoutesAsync(matchingRoutes, message, routingId, cancellationToken)
                    .ConfigureAwait(false)
                : await ExecuteSingleRouteAsync(matchingRoutes[0], message, routingId, cancellationToken)
                    .ConfigureAwait(false);

            // Check for errors in results
            var errorResult = results.FirstOrDefault(r => r.HasError);
            if (errorResult is not null)
            {
                stopwatch.Stop();
                ContentRouterLog.RouteExecutionFailed(_logger, routingId, errorResult.RouteName, errorResult.Error.Message);
                return errorResult.Error;
            }

            stopwatch.Stop();

            var successResults = results
                .Where(r => !r.HasError)
                .Select(r => new RouteExecutionResult<TResult>(
                    r.RouteName,
                    r.Result,
                    r.Duration,
                    r.ExecutedAtUtc))
                .ToList();

            ContentRouterLog.RoutingCompleted(_logger, routingId, successResults.Count, stopwatch.Elapsed);

            return new ContentRouterResult<TResult>(
                successResults,
                matchingRoutes.Count,
                stopwatch.Elapsed,
                usedDefaultRoute: false);
        }
        catch (OperationCanceledException)
        {
            ContentRouterLog.RoutingCancelled(_logger, routingId);
            return EncinaErrors.Create(ContentRouterErrorCodes.RouterCancelled, "Routing was cancelled");
        }
        catch (Exception ex)
        {
            ContentRouterLog.RoutingException(_logger, routingId, ex.Message, ex);
            return EncinaErrors.Create(ContentRouterErrorCodes.RouteExecutionFailed, ex.Message);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ContentRouterResult<Unit>>> RouteAsync<TMessage>(
        BuiltContentRouterDefinition<TMessage, Unit> definition,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return RouteAsync<TMessage, Unit>(definition, message, cancellationToken);
    }

    private List<RouteDefinition<TMessage, TResult>> FindMatchingRoutes<TMessage, TResult>(
        BuiltContentRouterDefinition<TMessage, TResult> definition,
        TMessage message,
        Guid routingId)
        where TMessage : class
    {
        var matchingRoutes = new List<RouteDefinition<TMessage, TResult>>();

        foreach (var route in definition.Routes)
        {
            try
            {
                if (route.Matches(message))
                {
                    ContentRouterLog.RouteMatched(_logger, routingId, route.Name);
                    matchingRoutes.Add(route);

                    // If not allowing multiple matches, stop at first match
                    if (!_options.AllowMultipleMatches)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ContentRouterLog.ConditionEvaluationFailed(_logger, routingId, route.Name, ex.Message, ex);
                // Skip this route on condition evaluation failure
            }
        }

        return matchingRoutes;
    }

    private async ValueTask<Either<EncinaError, ContentRouterResult<TResult>>> ExecuteDefaultRouteAsync<TMessage, TResult>(
        RouteDefinition<TMessage, TResult> defaultRoute,
        TMessage message,
        Guid routingId,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        var stepStopwatch = Stopwatch.StartNew();
        var executedAt = DateTime.UtcNow;

        try
        {
            var result = await defaultRoute.Handler(message, cancellationToken).ConfigureAwait(false);
            stepStopwatch.Stop();

            return result.Match(
                Right: value =>
                {
                    stopwatch.Stop();
                    var routeResult = new RouteExecutionResult<TResult>(
                        defaultRoute.Name,
                        value,
                        stepStopwatch.Elapsed,
                        executedAt);

                    ContentRouterLog.RoutingCompleted(_logger, routingId, 1, stopwatch.Elapsed);

                    return Right<EncinaError, ContentRouterResult<TResult>>( // NOSONAR S6966: LanguageExt Right is a pure function
                        new ContentRouterResult<TResult>(
                            [routeResult],
                            matchedRouteCount: 0,
                            stopwatch.Elapsed,
                            usedDefaultRoute: true));
                },
                Left: error =>
                {
                    stopwatch.Stop();
                    return Left<EncinaError, ContentRouterResult<TResult>>(error); // NOSONAR S6966: LanguageExt Left is a pure function
                });
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            stopwatch.Stop();
            ContentRouterLog.RouteExecutionFailed(_logger, routingId, defaultRoute.Name, ex.Message);
            return EncinaErrors.Create(ContentRouterErrorCodes.RouteExecutionFailed, ex.Message);
        }
    }

    private async ValueTask<List<InternalRouteResult<TResult>>> ExecuteSingleRouteAsync<TMessage, TResult>(
        RouteDefinition<TMessage, TResult> route,
        TMessage message,
        Guid routingId,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        var result = await ExecuteRouteAsync(route, message, routingId, cancellationToken)
            .ConfigureAwait(false);
        return [result];
    }

    private async ValueTask<List<InternalRouteResult<TResult>>> ExecuteMultipleRoutesAsync<TMessage, TResult>(
        List<RouteDefinition<TMessage, TResult>> routes,
        TMessage message,
        Guid routingId,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        if (_options.EvaluateInParallel)
        {
            return await ExecuteRoutesInParallelAsync(routes, message, routingId, cancellationToken)
                .ConfigureAwait(false);
        }

        var results = new List<InternalRouteResult<TResult>>(routes.Count);

        foreach (var route in routes)
        {
            var result = await ExecuteRouteAsync(route, message, routingId, cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);

            // Stop on first error
            if (result.HasError)
            {
                break;
            }
        }

        return results;
    }

    private async ValueTask<List<InternalRouteResult<TResult>>> ExecuteRoutesInParallelAsync<TMessage, TResult>(
        List<RouteDefinition<TMessage, TResult>> routes,
        TMessage message,
        Guid routingId,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var results = new ConcurrentBag<InternalRouteResult<TResult>>();

        await Parallel.ForEachAsync(routes, options, async (route, ct) =>
        {
            var result = await ExecuteRouteAsync(route, message, routingId, ct)
                .ConfigureAwait(false);
            results.Add(result);
        }).ConfigureAwait(false);

        return [.. results];
    }

    private async ValueTask<InternalRouteResult<TResult>> ExecuteRouteAsync<TMessage, TResult>(
        RouteDefinition<TMessage, TResult> route,
        TMessage message,
        Guid routingId,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        var stopwatch = Stopwatch.StartNew();
        var executedAt = DateTime.UtcNow;

        ContentRouterLog.RouteExecuting(_logger, routingId, route.Name);

        try
        {
            var result = await route.Handler(message, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return result.Match(
                Right: value =>
                {
                    ContentRouterLog.RouteExecuted(_logger, routingId, route.Name, stopwatch.Elapsed);
                    return InternalRouteResult<TResult>.Success(route.Name, value, stopwatch.Elapsed, executedAt);
                },
                Left: error =>
                {
                    ContentRouterLog.RouteExecutionFailed(_logger, routingId, route.Name, error.Message);
                    return InternalRouteResult<TResult>.Failure(route.Name, error, stopwatch.Elapsed, executedAt);
                });
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            ContentRouterLog.RoutingCancelled(_logger, routingId);
            var error = EncinaErrors.Create(ContentRouterErrorCodes.RouterCancelled, "Route execution was cancelled");
            return InternalRouteResult<TResult>.Failure(route.Name, error, stopwatch.Elapsed, executedAt);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ContentRouterLog.RouteExecutionFailed(_logger, routingId, route.Name, ex.Message);
            var error = EncinaErrors.Create(ContentRouterErrorCodes.RouteExecutionFailed, ex.Message);
            return InternalRouteResult<TResult>.Failure(route.Name, error, stopwatch.Elapsed, executedAt);
        }
    }

    private sealed class InternalRouteResult<TResult>
    {
        public string RouteName { get; }
        public TResult Result { get; }
        public EncinaError Error { get; }
        public bool HasError { get; }
        public TimeSpan Duration { get; }
        public DateTime ExecutedAtUtc { get; }

        private InternalRouteResult(string routeName, TResult result, EncinaError error, bool hasError, TimeSpan duration, DateTime executedAtUtc)
        {
            RouteName = routeName;
            Result = result;
            Error = error;
            HasError = hasError;
            Duration = duration;
            ExecutedAtUtc = executedAtUtc;
        }

        public static InternalRouteResult<TResult> Success(string routeName, TResult result, TimeSpan duration, DateTime executedAtUtc)
            => new(routeName, result, default, hasError: false, duration, executedAtUtc);

        public static InternalRouteResult<TResult> Failure(string routeName, EncinaError error, TimeSpan duration, DateTime executedAtUtc)
            => new(routeName, default!, error, hasError: true, duration, executedAtUtc);
    }
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class ContentRouterLog
{
    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} started (message type: {MessageType}, routes: {RouteCount})")]
    public static partial void RoutingStarted(ILogger logger, Guid routingId, string messageType, int routeCount);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} route matched: {RouteName}")]
    public static partial void RouteMatched(ILogger logger, Guid routingId, string routeName);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} executing route: {RouteName}")]
    public static partial void RouteExecuting(ILogger logger, Guid routingId, string routeName);

    [LoggerMessage(
        EventId = 503,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} route {RouteName} executed ({Duration})")]
    public static partial void RouteExecuted(ILogger logger, Guid routingId, string routeName, TimeSpan duration);

    [LoggerMessage(
        EventId = 504,
        Level = LogLevel.Warning,
        Message = "Content routing {RoutingId} route {RouteName} failed: {ErrorMessage}")]
    public static partial void RouteExecutionFailed(ILogger logger, Guid routingId, string routeName, string errorMessage);

    [LoggerMessage(
        EventId = 505,
        Level = LogLevel.Information,
        Message = "Content routing {RoutingId} completed ({MatchedRoutes} routes executed, {Duration})")]
    public static partial void RoutingCompleted(ILogger logger, Guid routingId, int matchedRoutes, TimeSpan duration);

    [LoggerMessage(
        EventId = 506,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} using default route")]
    public static partial void UsingDefaultRoute(ILogger logger, Guid routingId);

    [LoggerMessage(
        EventId = 507,
        Level = LogLevel.Warning,
        Message = "Content routing {RoutingId} no matching route for message type: {MessageType}")]
    public static partial void NoMatchingRoute(ILogger logger, Guid routingId, string messageType);

    [LoggerMessage(
        EventId = 508,
        Level = LogLevel.Debug,
        Message = "Content routing {RoutingId} completed with no matches ({Duration})")]
    public static partial void RoutingCompletedNoMatch(ILogger logger, Guid routingId, TimeSpan duration);

    [LoggerMessage(
        EventId = 509,
        Level = LogLevel.Warning,
        Message = "Content routing {RoutingId} was cancelled")]
    public static partial void RoutingCancelled(ILogger logger, Guid routingId);

    [LoggerMessage(
        EventId = 510,
        Level = LogLevel.Error,
        Message = "Content routing {RoutingId} failed with exception: {ErrorMessage}")]
    public static partial void RoutingException(ILogger logger, Guid routingId, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 511,
        Level = LogLevel.Warning,
        Message = "Content routing {RoutingId} condition evaluation failed for route {RouteName}: {ErrorMessage}")]
    public static partial void ConditionEvaluationFailed(ILogger logger, Guid routingId, string routeName, string errorMessage, Exception exception);
}
