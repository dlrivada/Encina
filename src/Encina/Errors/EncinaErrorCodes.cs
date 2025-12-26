namespace Encina;

/// <summary>
/// Canonical error codes emitted by the Encina pipeline.
/// </summary>
/// <remarks>
/// Centralizing codes reduces drift between behaviors, handlers and documentation.
/// </remarks>
public static class EncinaErrorCodes
{
    /// <summary>Error when a request instance is null.</summary>
    public const string RequestNull = "encina.request.null";

    /// <summary>Error when a notification instance is null.</summary>
    public const string NotificationNull = "encina.notification.null";

    /// <summary>No notification handler registered.</summary>
    public const string NotificationMissingHandle = "encina.notification.missing_handle";

    /// <summary>Notification handler returned an invalid task or result.</summary>
    public const string NotificationInvalidReturn = "encina.notification.invalid_return";

    /// <summary>Notification handler threw an exception.</summary>
    public const string NotificationInvokeException = "encina.notification.invoke_exception";

    /// <summary>No request handler registered.</summary>
    public const string HandlerMissing = "encina.handler.missing";

    /// <summary>No request handler registered.</summary>
    public const string RequestHandlerMissing = "encina.request.handler_missing";

    /// <summary>Request handler type mismatch.</summary>
    public const string RequestHandlerTypeMismatch = "encina.request.handler_type_mismatch";

    /// <summary>Request handler returned an invalid result.</summary>
    public const string HandlerInvalidResult = "encina.handler.invalid_result";

    /// <summary>Request handler canceled execution.</summary>
    public const string HandlerCancelled = "encina.handler.cancelled";

    /// <summary>Request handler threw an exception.</summary>
    public const string HandlerException = "encina.handler.exception";

    /// <summary>Request canceled before completion.</summary>
    public const string RequestCancelled = "encina.request.cancelled";

    /// <summary>Pipeline behavior received a null request.</summary>
    public const string BehaviorNullRequest = "encina.behavior.null_request";

    /// <summary>Pipeline behavior received a null next delegate.</summary>
    public const string BehaviorNullNext = "encina.behavior.null_next";

    /// <summary>Pipeline behavior canceled execution.</summary>
    public const string BehaviorCancelled = "encina.behavior.cancelled";

    /// <summary>Pipeline behavior threw an exception.</summary>
    public const string BehaviorException = "encina.behavior.exception";

    /// <summary>Unexpected failure while executing the pipeline.</summary>
    public const string PipelineException = "encina.pipeline.exception";

    /// <summary>Operation timed out.</summary>
    public const string Timeout = "encina.timeout";

    /// <summary>Notification processing canceled.</summary>
    public const string NotificationCancelled = "encina.notification.cancelled";

    /// <summary>Notification processing threw an exception.</summary>
    public const string NotificationException = "encina.notification.exception";

    /// <summary>Multiple notification handlers failed during parallel dispatch.</summary>
    public const string NotificationMultipleFailures = "encina.notification.multiple_failures";

    /// <summary>Request pre-processor canceled execution.</summary>
    public const string PreProcessorCancelled = "encina.preprocessor.cancelled";

    /// <summary>Request pre-processor threw an exception.</summary>
    public const string PreProcessorException = "encina.preprocessor.exception";

    /// <summary>Request post-processor canceled execution.</summary>
    public const string PostProcessorCancelled = "encina.postprocessor.cancelled";

    /// <summary>Request post-processor threw an exception.</summary>
    public const string PostProcessorException = "encina.postprocessor.exception";

    /// <summary>Rate limit exceeded for request.</summary>
    public const string RateLimitExceeded = "encina.ratelimit.exceeded";
}
