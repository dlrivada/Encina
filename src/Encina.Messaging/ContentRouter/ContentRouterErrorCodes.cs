namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Error codes for Content-Based Router operations.
/// </summary>
public static class ContentRouterErrorCodes
{
    /// <summary>
    /// No matching route was found for the message content.
    /// </summary>
    public const string NoMatchingRoute = "content_router.no_matching_route";

    /// <summary>
    /// Multiple routes matched when only one was expected.
    /// </summary>
    public const string MultipleRoutesMatched = "content_router.multiple_routes_matched";

    /// <summary>
    /// The router definition has no routes configured.
    /// </summary>
    public const string NoRoutesConfigured = "content_router.no_routes_configured";

    /// <summary>
    /// Route execution failed.
    /// </summary>
    public const string RouteExecutionFailed = "content_router.route_execution_failed";

    /// <summary>
    /// The router was cancelled.
    /// </summary>
    public const string RouterCancelled = "content_router.cancelled";

    /// <summary>
    /// Route condition evaluation failed.
    /// </summary>
    public const string ConditionEvaluationFailed = "content_router.condition_evaluation_failed";
}
