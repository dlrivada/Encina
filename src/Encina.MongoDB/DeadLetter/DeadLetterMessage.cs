using Encina.Messaging.DeadLetter;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.DeadLetter;

/// <summary>
/// MongoDB implementation of <see cref="IDeadLetterMessage"/>.
/// </summary>
/// <remarks>
/// A plain data holder: it never reads the clock. Expiry is evaluated against an instant supplied
/// by the caller (<see cref="IsExpiredAt"/>).
/// </remarks>
public sealed class DeadLetterMessage : IDeadLetterMessage
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <inheritdoc />
    [BsonElement("requestType")]
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("requestContent")]
    public string RequestContent { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("exceptionType")]
    [BsonIgnoreIfNull]
    public string? ExceptionType { get; set; }

    /// <inheritdoc />
    [BsonElement("exceptionStackTrace")]
    [BsonIgnoreIfNull]
    public string? ExceptionStackTrace { get; set; }

    /// <inheritdoc />
    [BsonElement("correlationId")]
    [BsonIgnoreIfNull]
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    [BsonElement("sourcePattern")]
    public string SourcePattern { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("sourceMessageId")]
    public string SourceMessageId { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("tenantId")]
    [BsonIgnoreIfNull]
    public string? TenantId { get; set; }

    /// <inheritdoc />
    [BsonElement("totalRetryAttempts")]
    public int TotalRetryAttempts { get; set; }

    /// <inheritdoc />
    [BsonElement("firstFailedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FirstFailedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("deadLetteredAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DeadLetteredAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("expiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ExpiresAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("replayClaimedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReplayClaimedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("replayedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReplayedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("replayResult")]
    [BsonIgnoreIfNull]
    public string? ReplayResult { get; set; }

    /// <inheritdoc />
    [BsonIgnore]
    public bool IsReplayed => ReplayedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsExpiredAt(DateTime utcNow) => ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= utcNow;
}
