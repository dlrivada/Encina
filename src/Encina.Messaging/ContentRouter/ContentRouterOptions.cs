namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Configuration options for the Content-Based Router pattern.
/// </summary>
/// <remarks>
/// <para>
/// These options control how the Content-Based Router evaluates routing conditions
/// and handles edge cases like no matches or multiple matches.
/// </para>
/// </remarks>
public sealed class ContentRouterOptions
{
    /// <summary>
    /// Gets or sets whether to enable route caching for improved performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the router caches compiled route conditions to avoid
    /// repeated expression compilation. This is beneficial when routing
    /// many messages of the same type.
    /// </para>
    /// <para>
    /// Note: Caching only affects expression compilation, not the evaluation
    /// of conditions against message content.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool EnableRouteCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of cached route evaluators per message type.
    /// </summary>
    /// <remarks>
    /// This limit prevents memory issues when routing many different message types.
    /// </remarks>
    /// <value>Default: 1000</value>
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to throw when no route matches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the router returns an error if no route matches and no default route is configured.
    /// When false, the router returns a successful result with no handler executed.
    /// </para>
    /// </remarks>
    /// <value>Default: true</value>
    public bool ThrowOnNoMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow multiple routes to match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, all matching routes are executed in order.
    /// When false, only the first matching route is executed.
    /// </para>
    /// </remarks>
    /// <value>Default: false (first match wins)</value>
    public bool AllowMultipleMatches { get; set; }

    /// <summary>
    /// Gets or sets whether to evaluate routes in parallel when multiple matches are allowed.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="AllowMultipleMatches"/> is true.
    /// </remarks>
    /// <value>Default: false</value>
    public bool EvaluateInParallel { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism when evaluating routes in parallel.
    /// </summary>
    /// <value>Default: Environment.ProcessorCount</value>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
