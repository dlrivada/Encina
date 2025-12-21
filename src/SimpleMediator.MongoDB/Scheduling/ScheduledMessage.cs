using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SimpleMediator.Messaging.Scheduling;

namespace SimpleMediator.MongoDB.Scheduling;

/// <summary>
/// MongoDB implementation of <see cref="IScheduledMessage"/>.
/// </summary>
public sealed class ScheduledMessage : IScheduledMessage
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <inheritdoc />
    [BsonElement("requestType")]
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("scheduledAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ScheduledAtUtc { get; set; }

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
    [BsonElement("isRecurring")]
    public bool IsRecurring { get; set; }

    /// <inheritdoc />
    [BsonElement("cronExpression")]
    public string? CronExpression { get; set; }

    /// <inheritdoc />
    [BsonElement("lastExecutedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastExecutedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonIgnore]
    public bool IsProcessed => ProcessedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsDue() => DateTime.UtcNow >= ScheduledAtUtc;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries;
}
