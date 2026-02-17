using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

namespace Encina.Testing.FsCheck;

/// <summary>
/// Concrete implementation of <see cref="IOutboxMessage"/> for property-based testing.
/// </summary>
public sealed class ArbitraryOutboxMessage : IOutboxMessage
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string NotificationType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public int RetryCount { get; set; }

    /// <inheritdoc/>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc/>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}

/// <summary>
/// Concrete implementation of <see cref="IInboxMessage"/> for property-based testing.
/// </summary>
public sealed class ArbitraryInboxMessage : IInboxMessage
{
    /// <inheritdoc/>
    public string MessageId { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Response { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public DateTime ReceivedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime ExpiresAtUtc { get; set; }

    /// <inheritdoc/>
    public int RetryCount { get; set; }

    /// <inheritdoc/>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc/>
    public bool IsExpired() => IsExpired(TimeProvider.System.GetUtcNow().UtcDateTime);

    /// <summary>
    /// Determines whether this message has expired as of the specified time.
    /// </summary>
    /// <param name="asOf">The reference time to check expiry against.</param>
    /// <returns><c>true</c> if the message has expired; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This overload enables unit testing of expiry scenarios with controlled timestamps.
    /// </remarks>
    public bool IsExpired(DateTime asOf) => asOf >= ExpiresAtUtc;
}

/// <summary>
/// Concrete implementation of <see cref="ISagaState"/> for property-based testing.
/// </summary>
public sealed class ArbitrarySagaState : ISagaState
{
    /// <inheritdoc/>
    public Guid SagaId { get; set; }

    /// <inheritdoc/>
    public string SagaType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Data { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Status { get; set; } = string.Empty;

    /// <inheritdoc/>
    public int CurrentStep { get; set; }

    /// <inheritdoc/>
    public DateTime StartedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? CompletedAtUtc { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? TimeoutAtUtc { get; set; }
}

/// <summary>
/// Concrete implementation of <see cref="IScheduledMessage"/> for property-based testing.
/// </summary>
public sealed class ArbitraryScheduledMessage : IScheduledMessage
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTime ScheduledAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public int RetryCount { get; set; }

    /// <inheritdoc/>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsRecurring { get; set; }

    /// <inheritdoc/>
    public string? CronExpression { get; set; }

    /// <inheritdoc/>
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <inheritdoc/>
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc/>
    public bool IsDue() => IsDue(TimeProvider.System.GetUtcNow().UtcDateTime);

    /// <summary>
    /// Determines whether the message is due for processing as of the specified time.
    /// </summary>
    /// <param name="asOf">The reference time to compare against.</param>
    /// <returns>True if the scheduled time has passed; otherwise, false.</returns>
    public bool IsDue(DateTime asOf) => asOf >= ScheduledAtUtc;

    /// <inheritdoc/>
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}
