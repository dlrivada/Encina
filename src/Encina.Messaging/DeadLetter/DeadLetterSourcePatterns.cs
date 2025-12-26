namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Well-known source pattern names for dead letter messages.
/// </summary>
public static class DeadLetterSourcePatterns
{
    /// <summary>
    /// Message originated from the Recoverability Pipeline after exhausting all retries.
    /// </summary>
    public const string Recoverability = "Recoverability";

    /// <summary>
    /// Message originated from the Outbox Pattern after exceeding max retries.
    /// </summary>
    public const string Outbox = "Outbox";

    /// <summary>
    /// Message originated from the Inbox Pattern after exceeding max retries.
    /// </summary>
    public const string Inbox = "Inbox";

    /// <summary>
    /// Message originated from the Scheduling Pattern after exceeding max retries.
    /// </summary>
    public const string Scheduling = "Scheduling";

    /// <summary>
    /// Message originated from the Saga Pattern when saga was not found.
    /// </summary>
    public const string Saga = "Saga";

    /// <summary>
    /// Message originated from the Choreography Pattern.
    /// </summary>
    public const string Choreography = "Choreography";
}
