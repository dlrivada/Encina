using Encina.Messaging.Sagas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Sagas;

/// <summary>
/// MongoDB implementation of <see cref="ISagaState"/>.
/// </summary>
public sealed class SagaState : ISagaState
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid SagaId { get; set; }

    /// <inheritdoc />
    [BsonElement("sagaType")]
    public string SagaType { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("data")]
    public string Data { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    /// <inheritdoc />
    [BsonElement("currentStep")]
    public int CurrentStep { get; set; }

    /// <inheritdoc />
    [BsonElement("startedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("completedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    [BsonElement("lastUpdatedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <inheritdoc />
    [BsonElement("timeoutAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? TimeoutAtUtc { get; set; }
}
