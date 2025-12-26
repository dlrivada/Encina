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
}
