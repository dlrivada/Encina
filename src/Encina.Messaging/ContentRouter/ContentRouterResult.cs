namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Factory methods for creating <see cref="ContentRouterResult{TResult}"/> instances.
/// </summary>
public static class ContentRouterResult
{
    /// <summary>
    /// Creates an empty result when no routes matched and no default was configured.
    /// </summary>
    /// <typeparam name="TResult">The type of result from handler execution.</typeparam>
    /// <returns>An empty routing result.</returns>
    public static ContentRouterResult<TResult> Empty<TResult>() =>
        new([], 0, TimeSpan.Zero, usedDefaultRoute: false);
}

/// <summary>
/// Represents the result of content-based routing.
/// </summary>
/// <typeparam name="TResult">The type of result from handler execution.</typeparam>
public sealed class ContentRouterResult<TResult>
{
    /// <summary>
    /// Gets the results from each matched route handler.
    /// </summary>
    public IReadOnlyList<RouteExecutionResult<TResult>> RouteResults { get; }

    /// <summary>
    /// Gets the number of routes that matched the message content.
    /// </summary>
    public int MatchedRouteCount { get; }

    /// <summary>
    /// Gets the total execution time for all route handlers.
    /// </summary>
    public TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets a value indicating whether the default route was used.
    /// </summary>
    public bool UsedDefaultRoute { get; }

    /// <summary>
    /// Gets a value indicating whether any routes matched.
    /// </summary>
    public bool HasMatches => MatchedRouteCount > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentRouterResult{TResult}"/> class.
    /// </summary>
    /// <param name="routeResults">The results from route executions.</param>
    /// <param name="matchedRouteCount">The number of matched routes.</param>
    /// <param name="totalDuration">The total execution duration.</param>
    /// <param name="usedDefaultRoute">Whether the default route was used.</param>
    public ContentRouterResult(
        IReadOnlyList<RouteExecutionResult<TResult>> routeResults,
        int matchedRouteCount,
        TimeSpan totalDuration,
        bool usedDefaultRoute)
    {
        RouteResults = routeResults;
        MatchedRouteCount = matchedRouteCount;
        TotalDuration = totalDuration;
        UsedDefaultRoute = usedDefaultRoute;
    }
}

/// <summary>
/// Represents the result of executing a single route handler.
/// </summary>
/// <typeparam name="TResult">The type of result from handler execution.</typeparam>
public sealed class RouteExecutionResult<TResult>
{
    /// <summary>
    /// Gets the name of the route that was executed.
    /// </summary>
    public string RouteName { get; }

    /// <summary>
    /// Gets the result from the handler execution.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Gets the execution duration for this route.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the timestamp when this route was executed.
    /// </summary>
    public DateTime ExecutedAtUtc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteExecutionResult{TResult}"/> class.
    /// </summary>
    /// <param name="routeName">The route name.</param>
    /// <param name="result">The handler result.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="executedAtUtc">The execution timestamp.</param>
    public RouteExecutionResult(
        string routeName,
        TResult result,
        TimeSpan duration,
        DateTime executedAtUtc)
    {
        RouteName = routeName;
        Result = result;
        Duration = duration;
        ExecutedAtUtc = executedAtUtc;
    }
}
