using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SimpleMediator.Messaging.Outbox;

namespace SimpleMediator.MongoDB.Outbox;

/// <summary>
/// MongoDB implementation of <see cref="IOutboxMessage"/>.
/// </summary>
public sealed class OutboxMessage : IOutboxMessage
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <inheritdoc />
    [BsonElement("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("createdAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("processedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    [BsonElement("retryCount")]
    public int RetryCount { get; set; }

    /// <inheritdoc />
    [BsonElement("nextRetryAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    [BsonIgnore]
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}
