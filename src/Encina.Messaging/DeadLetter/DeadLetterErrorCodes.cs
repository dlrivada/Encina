namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Error codes for Dead Letter Queue operations.
/// </summary>
public static class DeadLetterErrorCodes
{
    /// <summary>
    /// Message not found in the Dead Letter Queue.
    /// </summary>
    public const string NotFound = "dlq.not_found";

    /// <summary>
    /// Message has already been replayed.
    /// </summary>
    public const string AlreadyReplayed = "dlq.already_replayed";

    /// <summary>
    /// Message has expired and cannot be replayed.
    /// </summary>
    public const string Expired = "dlq.expired";

    /// <summary>
    /// Failed to deserialize the request for replay.
    /// </summary>
    public const string DeserializationFailed = "dlq.deserialization_failed";

    /// <summary>
    /// Replay execution failed.
    /// </summary>
    public const string ReplayFailed = "dlq.replay_failed";

    /// <summary>
    /// Failed to store message in DLQ.
    /// </summary>
    public const string StoreFailed = "dlq.store_failed";

    /// <summary>
    /// Failed to retrieve DLQ statistics.
    /// </summary>
    public const string StatisticsError = "dlq.statistics_error";

    /// <summary>
    /// Failed to clean up expired messages.
    /// </summary>
    public const string CleanupFailed = "dlq.cleanup_failed";

    /// <summary>
    /// Failed to delete DLQ message(s).
    /// </summary>
    public const string DeleteFailed = "dlq.delete_failed";

    /// <summary>
    /// Another replay holds the claim on the message, so this replay did not dispatch it.
    /// </summary>
    public const string ReplayInProgress = "dlq.replay_in_progress";

    /// <summary>
    /// A store received a dead letter message of a type it cannot persist.
    /// </summary>
    public const string InvalidMessageType = "dlq.invalid_message_type";

    /// <summary>
    /// A store failed to read a single dead letter message.
    /// </summary>
    public const string GetFailed = "dlq.get_failed";

    /// <summary>
    /// A store failed to list or count dead letter messages.
    /// </summary>
    public const string QueryFailed = "dlq.query_failed";

    /// <summary>
    /// A store failed to claim a message for replay.
    /// </summary>
    public const string ClaimFailed = "dlq.claim_failed";

    /// <summary>
    /// A store failed to record the replay outcome of a message.
    /// </summary>
    public const string MarkReplayedFailed = "dlq.mark_replayed_failed";

    /// <summary>
    /// Multi-tenancy is in use, no tenant is resolved and the caller did not set
    /// <see cref="DeadLetterFilter.AllTenants"/>, so <see cref="IDeadLetterManager"/> denied the operation.
    /// An <c>encina.authorization.*</c> code: transports map it to 403.
    /// </summary>
    public const string TenantRequired = "encina.authorization.dlq_tenant_required";

    /// <summary>
    /// Capturing the terminal failure of a built-in source into the dead letter queue threw an unexpected
    /// exception; the source treats it as a failed capture and tries again later.
    /// </summary>
    public const string CaptureFailed = "dlq.capture_failed";

    /// <summary>
    /// The dead letter queue cannot store the terminal failure of a source at all: the capture threw an
    /// <see cref="ArgumentException"/> (its input rules reject an identity value or instant of the message, or an
    /// argument the queue's configuration or store derives from it is invalid), or the source pattern has no
    /// <c>IntegrateWith*</c> flag. Trying again cannot succeed, so the source records its terminal state (see
    /// <c>DeadLetterSourceCapture.IsRetryable</c>).
    /// </summary>
    public const string CaptureRejected = "dlq.capture_rejected";

    /// <summary>
    /// Outcome code stored in <c>ReplayResult</c> when a replay succeeded.
    /// </summary>
    public const string ReplaySucceeded = "success";
}
