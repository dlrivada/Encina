namespace Encina;

/// <summary>
/// Defines constant tag names for OpenTelemetry activities to avoid string duplication.
/// </summary>
/// <remarks>
/// These constants follow OpenTelemetry semantic conventions where applicable,
/// with Encina-specific prefixes for custom attributes.
/// </remarks>
internal static class ActivityTagNames
{
    // Request/Response tags
    internal const string RequestKind = "Encina.request_kind";
    internal const string RequestType = "Encina.request_type";
    internal const string RequestName = "Encina.request_name";
    internal const string ResponseType = "Encina.response_type";

    // Handler tags
    internal const string Handler = "Encina.handler";
    internal const string HandlerCount = "Encina.handler_count";

    // Notification tags
    internal const string NotificationKind = "Encina.notification_kind";
    internal const string NotificationType = "Encina.notification_type";
    internal const string NotificationName = "Encina.notification_name";
    internal const string DispatchStrategy = "Encina.dispatch_strategy";

    // Error/Failure tags
    internal const string FailureReason = "Encina.failure_reason";
    internal const string FunctionalFailure = "Encina.functional_failure";
    internal const string FailureCode = "Encina.failure_code";
    internal const string FailureMessage = "Encina.failure_message";
    internal const string PipelineFailure = "Encina.pipeline_failure";
    internal const string Cancelled = "Encina.cancelled";

    // Exception tags (OpenTelemetry semantic conventions)
    internal const string ExceptionType = "exception.type";
    internal const string ExceptionMessage = "exception.message";

    // Stream tags
    internal const string ItemType = "Encina.item_type";
    internal const string ItemName = "Encina.item_name";
    internal const string StreamItemCount = "Encina.stream_item_count";

    // Sharding tags
    internal const string ShardId = "db.shard.id";
    internal const string ShardKey = "db.shard.key";
    internal const string ShardCount = "encina.sharding.shard.count";
    internal const string RouterType = "encina.sharding.router.type";
    internal const string ScatterGatherStrategy = "encina.sharding.scatter.strategy";

    // Compound shard key tags
    internal const string CompoundKeyComponents = "encina.sharding.compound_key.components";
    internal const string CompoundKeyPartial = "encina.sharding.compound_key.partial";
    internal const string CompoundKeyStrategyPerComponent = "encina.sharding.compound_key.strategy_per_component";
}
