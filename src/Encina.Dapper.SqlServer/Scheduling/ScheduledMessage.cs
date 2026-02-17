using Encina.Messaging.Scheduling;

namespace Encina.Dapper.SqlServer.Scheduling;

/// <summary>
/// Dapper implementation of scheduled message for delayed command execution.
/// Supports both one-time and recurring message scheduling.
/// </summary>
public sealed class ScheduledMessage : IScheduledMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the scheduled message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full type name of the request.
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON serialized content of the request.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message should be executed (UTC).
    /// </summary>
    public DateTime ScheduledAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was successfully processed (UTC).
    /// Null if not yet processed.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the message was last attempted to be executed (UTC).
    /// </summary>
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets when the next retry should be attempted (UTC).
    /// Null if no retry is scheduled.
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this is a recurring message.
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for recurring messages.
    /// Null for one-time messages.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsDue()
    {
        if (ProcessedAtUtc.HasValue && !IsRecurring)
            return false;

        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var compareTime = NextRetryAtUtc ?? ScheduledAtUtc;
        return now >= compareTime;
    }

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries)
    {
        return RetryCount >= maxRetries && ProcessedAtUtc == null;
    }
}
