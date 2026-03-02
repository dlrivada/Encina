using Encina.Compliance.Retention.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Retention;

/// <summary>
/// MongoDB document representation of a <see cref="LegalHold"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the legal_holds collection. Each document represents a legal hold
/// (litigation hold) that suspends data deletion for a specific entity, as
/// permitted by GDPR Article 17(3)(e) for legal claims.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names. A hold is considered
/// active when <see cref="ReleasedAtUtc"/> is <c>null</c>.
/// </para>
/// </remarks>
public sealed class LegalHoldDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this legal hold.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the data entity protected by this hold.
    /// </summary>
    [BsonElement("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable reason for applying the legal hold.
    /// </summary>
    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the user who applied the legal hold.
    /// </summary>
    [BsonElement("applied_by_user_id")]
    public string? AppliedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the legal hold was applied (UTC).
    /// </summary>
    [BsonElement("applied_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AppliedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the legal hold was released (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> while the hold is still active.
    /// </remarks>
    [BsonElement("released_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReleasedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who released the legal hold.
    /// </summary>
    [BsonElement("released_by_user_id")]
    public string? ReleasedByUserId { get; set; }

    /// <summary>
    /// Creates a <see cref="LegalHoldDocument"/> from a <see cref="LegalHold"/>.
    /// </summary>
    /// <param name="hold">The legal hold to convert.</param>
    /// <returns>A new document representation of the legal hold.</returns>
    public static LegalHoldDocument FromHold(LegalHold hold)
    {
        ArgumentNullException.ThrowIfNull(hold);

        return new LegalHoldDocument
        {
            Id = hold.Id,
            EntityId = hold.EntityId,
            Reason = hold.Reason,
            AppliedByUserId = hold.AppliedByUserId,
            AppliedAtUtc = hold.AppliedAtUtc.UtcDateTime,
            ReleasedAtUtc = hold.ReleasedAtUtc?.UtcDateTime,
            ReleasedByUserId = hold.ReleasedByUserId
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="LegalHold"/>.
    /// </summary>
    /// <returns>A legal hold record.</returns>
    public LegalHold ToHold() => new()
    {
        Id = Id,
        EntityId = EntityId,
        Reason = Reason,
        AppliedByUserId = AppliedByUserId,
        AppliedAtUtc = new DateTimeOffset(AppliedAtUtc, TimeSpan.Zero),
        ReleasedAtUtc = ReleasedAtUtc.HasValue
            ? new DateTimeOffset(ReleasedAtUtc.Value, TimeSpan.Zero)
            : null,
        ReleasedByUserId = ReleasedByUserId
    };
}
