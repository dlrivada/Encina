using SimpleMediator.Messaging.Scheduling;

namespace SimpleMediator.EntityFrameworkCore.Scheduling;

/// <summary>
/// Entity Framework Core implementation of <see cref="IScheduledMessage"/>.
/// Represents a message scheduled for future execution.
/// </summary>
/// <remarks>
/// <para>
/// Scheduled messages enable delayed execution of commands/notifications. Common use cases:
/// <list type="bullet">
/// <item><description>Send reminder email 24 hours before event</description></item>
/// <item><description>Cancel order if not paid within 30 minutes</description></item>
/// <item><description>Archive records 90 days after creation</description></item>
/// <item><description>Retry failed operations with exponential backoff</description></item>
/// <item><description>Implement timeout patterns for sagas</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Inspired by</b>: Wolverine's scheduled messages, Hangfire, Quartz.NET
/// </para>
/// </remarks>
public sealed class ScheduledMessage : IScheduledMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the scheduled message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the request/notification.
    /// </summary>
    /// <remarks>
    /// Used for deserialization when the message is due.
    /// Format: "Namespace.TypeName, AssemblyName"
    /// </remarks>
    public required string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized message content.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message should be executed.
    /// </summary>
    public DateTime ScheduledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was created/scheduled.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was executed.
    /// </summary>
    /// <value>
    /// <c>null</c> if the message has not been executed yet.
    /// </value>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of times execution has been attempted.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when to retry processing (for failed messages).
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    /// <remarks>
    /// Links the scheduled message back to the original request that scheduled it.
    /// </remarks>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the scheduled message.
    /// </summary>
    /// <remarks>
    /// Can store tenant ID, user ID, reason for scheduling, etc.
    /// Stored as JSON.
    /// </remarks>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether this is a recurring message.
    /// </summary>
    /// <remarks>
    /// If true, the message will be rescheduled after execution based on <see cref="CronExpression"/>.
    /// </remarks>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for recurring messages.
    /// </summary>
    /// <value>
    /// <c>null</c> if the message is not recurring.
    /// Format: Cron expression (e.g., "0 0 * * *" for daily at midnight).
    /// </value>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the last execution time for recurring messages.
    /// </summary>
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether the message has been processed.
    /// </summary>
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <summary>
    /// Gets a value indicating whether the message is due for execution.
    /// </summary>
    /// <returns>True if the scheduled time has been reached.</returns>
    public bool IsDue() => ScheduledAtUtc <= DateTime.UtcNow && !IsProcessed;

    /// <summary>
    /// Gets a value indicating whether the message should be dead lettered.
    /// </summary>
    /// <param name="maxRetries">Maximum allowed retries.</param>
    /// <returns>True if retry count exceeds maximum.</returns>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}
