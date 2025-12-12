namespace SimpleMediator;

/// <summary>
/// Canonical error codes emitted by the mediator pipeline.
/// </summary>
/// <remarks>
/// Centralizing codes reduces drift between behaviors, handlers and documentation.
/// </remarks>
public static class MediatorErrorCodes
{
    /// <summary>Error when a request instance is null.</summary>
    public const string RequestNull = "mediator.request.null";

    /// <summary>Error when a notification instance is null.</summary>
    public const string NotificationNull = "mediator.notification.null";

    /// <summary>No notification handler registered.</summary>
    public const string NotificationMissingHandle = "mediator.notification.missing_handle";

    /// <summary>Notification handler returned an invalid task or result.</summary>
    public const string NotificationInvalidReturn = "mediator.notification.invalid_return";

    /// <summary>Notification handler threw an exception.</summary>
    public const string NotificationInvokeException = "mediator.notification.invoke_exception";

    /// <summary>No request handler registered.</summary>
    public const string HandlerMissing = "mediator.handler.missing";

    /// <summary>No request handler registered.</summary>
    public const string RequestHandlerMissing = "mediator.request.handler_missing";

    /// <summary>Request handler type mismatch.</summary>
    public const string RequestHandlerTypeMismatch = "mediator.request.handler_type_mismatch";

    /// <summary>Request handler returned an invalid result.</summary>
    public const string HandlerInvalidResult = "mediator.handler.invalid_result";

    /// <summary>Request handler canceled execution.</summary>
    public const string HandlerCancelled = "mediator.handler.cancelled";

    /// <summary>Request handler threw an exception.</summary>
    public const string HandlerException = "mediator.handler.exception";

    /// <summary>Request canceled before completion.</summary>
    public const string RequestCancelled = "mediator.request.cancelled";

    /// <summary>Pipeline behavior received a null request.</summary>
    public const string BehaviorNullRequest = "mediator.behavior.null_request";

    /// <summary>Pipeline behavior received a null next delegate.</summary>
    public const string BehaviorNullNext = "mediator.behavior.null_next";

    /// <summary>Pipeline behavior canceled execution.</summary>
    public const string BehaviorCancelled = "mediator.behavior.cancelled";

    /// <summary>Pipeline behavior threw an exception.</summary>
    public const string BehaviorException = "mediator.behavior.exception";

    /// <summary>Unexpected failure while executing the pipeline.</summary>
    public const string PipelineException = "mediator.pipeline.exception";

    /// <summary>Operation timed out.</summary>
    public const string Timeout = "mediator.timeout";

    /// <summary>Notification processing canceled.</summary>
    public const string NotificationCancelled = "mediator.notification.cancelled";

    /// <summary>Notification processing threw an exception.</summary>
    public const string NotificationException = "mediator.notification.exception";

    /// <summary>Request pre-processor canceled execution.</summary>
    public const string PreProcessorCancelled = "mediator.preprocessor.cancelled";

    /// <summary>Request pre-processor threw an exception.</summary>
    public const string PreProcessorException = "mediator.preprocessor.exception";

    /// <summary>Request post-processor canceled execution.</summary>
    public const string PostProcessorCancelled = "mediator.postprocessor.cancelled";

    /// <summary>Request post-processor threw an exception.</summary>
    public const string PostProcessorException = "mediator.postprocessor.exception";
}
