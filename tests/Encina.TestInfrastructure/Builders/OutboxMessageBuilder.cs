using Encina.Dapper.Sqlite.Outbox;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating OutboxMessage test data.
/// Provides fluent API for constructing test messages with sensible defaults.
/// </summary>
public sealed class OutboxMessageBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _notificationType = "TestNotification";
    private string _content = "{}";
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private DateTime? _processedAtUtc;
    private string? _errorMessage;
    private int _retryCount;
    private DateTime? _nextRetryAtUtc;

    /// <summary>
    /// Sets the message ID.
    /// </summary>
    public OutboxMessageBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the notification type.
    /// </summary>
    public OutboxMessageBuilder WithNotificationType(string notificationType)
    {
        _notificationType = notificationType;
        return this;
    }

    /// <summary>
    /// Sets the message content (JSON).
    /// </summary>
    public OutboxMessageBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public OutboxMessageBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the message as processed.
    /// </summary>
    public OutboxMessageBuilder AsProcessed(DateTime? processedAtUtc = null)
    {
        _processedAtUtc = processedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the message as failed with an error.
    /// </summary>
    public OutboxMessageBuilder WithError(string errorMessage, int retryCount = 1, DateTime? nextRetryAtUtc = null)
    {
        _errorMessage = errorMessage;
        _retryCount = retryCount;
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the retry count.
    /// </summary>
    public OutboxMessageBuilder WithRetryCount(int retryCount)
    {
        _retryCount = retryCount;
        return this;
    }

    /// <summary>
    /// Sets the next retry timestamp.
    /// </summary>
    public OutboxMessageBuilder WithNextRetryAtUtc(DateTime? nextRetryAtUtc)
    {
        _nextRetryAtUtc = nextRetryAtUtc;
        return this;
    }

    /// <summary>
    /// Builds the OutboxMessage instance.
    /// </summary>
    public OutboxMessage Build() => new()
    {
        Id = _id,
        NotificationType = _notificationType,
        Content = _content,
        CreatedAtUtc = _createdAtUtc,
        ProcessedAtUtc = _processedAtUtc,
        ErrorMessage = _errorMessage,
        RetryCount = _retryCount,
        NextRetryAtUtc = _nextRetryAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static OutboxMessageBuilder Create() => new();
}
