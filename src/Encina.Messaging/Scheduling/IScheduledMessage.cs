namespace Encina.Messaging.Scheduling;

/// <summary>
/// Represents a message scheduled for delayed or recurring execution.
/// </summary>
/// <remarks>
/// <para>
/// Scheduled messages allow domain operations to be executed at a specific time
/// or on a recurring schedule. This is different from infrastructure job schedulers
/// like Hangfire/Quartz - this is for domain messages (commands/queries).
/// </para>
/// <para>
/// <b>Schedule Types</b>:
/// <list type="bullet">
/// <item><description><b>OneTime</b>: Execute once at scheduled time</description></item>
/// <item><description><b>Recurring</b>: Execute on a regular schedule (cron expression)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IScheduledMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the request to execute.
    /// </summary>
    string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the serialized request payload.
    /// </summary>
    string Content { get; set; }

    /// <summary>
    /// Gets or sets when to execute the message.
    /// </summary>
    DateTime ScheduledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was created.
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was processed.
    /// </summary>
    DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of processing attempts.
    /// </summary>
    int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when to retry processing (for failed messages).
    /// </summary>
    DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this is a recurring message.
    /// </summary>
    bool IsRecurring { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for recurring messages.
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="IsRecurring"/> is true.
    /// </remarks>
    string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the last execution time for recurring messages.
    /// </summary>
    DateTime? LastExecutedAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether the message has been processed.
    /// </summary>
    bool IsProcessed { get; }

    /// <summary>
    /// Gets a value indicating whether the message is due for execution.
    /// </summary>
    /// <returns>True if the scheduled time has been reached.</returns>
    bool IsDue();

    /// <summary>
    /// Gets a value indicating whether the message should be dead lettered.
    /// </summary>
    /// <param name="maxRetries">Maximum allowed retries.</param>
    /// <returns>True if retry count exceeds maximum.</returns>
    bool IsDeadLettered(int maxRetries);
}
