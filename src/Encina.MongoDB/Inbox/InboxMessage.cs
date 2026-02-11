using Encina.Messaging.Inbox;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Inbox;

/// <summary>
/// MongoDB implementation of <see cref="IInboxMessage"/>.
/// </summary>
public sealed class InboxMessage : IInboxMessage
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string MessageId { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("requestType")]
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("response")]
    public string? Response { get; set; }

    /// <inheritdoc />
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    [BsonElement("receivedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("processedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("expiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("retryCount")]
    public int RetryCount { get; set; }

    /// <inheritdoc />
    [BsonElement("nextRetryAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    [BsonIgnore]
    public bool IsProcessed => ProcessedAtUtc.HasValue && Response is not null;

    /// <inheritdoc />
    public bool IsExpired() =>
        TimeProvider.System.GetUtcNow().UtcDateTime > ExpiresAtUtc;
}
