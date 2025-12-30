using Encina.Messaging.Scheduling;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="IScheduledMessage"/> for testing.
/// </summary>
public sealed class FakeScheduledMessage : IScheduledMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime ScheduledAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsRecurring { get; set; }

    /// <inheritdoc />
    public string? CronExpression { get; set; }

    /// <inheritdoc />
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <summary>
    /// Gets or sets whether this message has been cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <inheritdoc />
    public bool IsDue() => DateTime.UtcNow >= ScheduledAtUtc && !IsProcessed && !IsCancelled;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;

    /// <summary>
    /// Creates a deep copy of this message.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeScheduledMessage Clone() => new()
    {
        Id = Id,
        RequestType = RequestType,
        Content = Content,
        ScheduledAtUtc = ScheduledAtUtc,
        CreatedAtUtc = CreatedAtUtc,
        ProcessedAtUtc = ProcessedAtUtc,
        ErrorMessage = ErrorMessage,
        RetryCount = RetryCount,
        NextRetryAtUtc = NextRetryAtUtc,
        IsRecurring = IsRecurring,
        CronExpression = CronExpression,
        LastExecutedAtUtc = LastExecutedAtUtc,
        IsCancelled = IsCancelled
    };
}
