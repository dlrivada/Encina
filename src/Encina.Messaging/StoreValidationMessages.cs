namespace Encina.Messaging;

/// <summary>
/// Constants for validation error messages used across store implementations.
/// </summary>
public static class StoreValidationMessages
{
    /// <summary>
    /// Error message when message ID is empty.
    /// </summary>
    public const string MessageIdCannotBeEmpty = "Message ID cannot be empty.";

    /// <summary>
    /// Error message when message ID is an empty GUID.
    /// </summary>
    public const string MessageIdCannotBeEmptyGuid = "Message ID cannot be empty GUID.";

    /// <summary>
    /// Error message when batch size is not positive.
    /// </summary>
    public const string BatchSizeMustBeGreaterThanZero = "Batch size must be greater than zero.";

    /// <summary>
    /// Error message when max retries is negative.
    /// </summary>
    public const string MaxRetriesCannotBeNegative = "Max retries cannot be negative.";

    /// <summary>
    /// Error message when saga ID is empty.
    /// </summary>
    public const string SagaIdCannotBeEmpty = "Saga ID cannot be empty.";

    /// <summary>
    /// Error message when olderThan parameter is not positive.
    /// </summary>
    public const string OlderThanMustBeGreaterThanZero = "OlderThan must be greater than zero.";

    /// <summary>
    /// Error message when next scheduled date is in the past.
    /// </summary>
    public const string NextScheduledDateCannotBeInPast = "Next scheduled date cannot be in the past.";
}
