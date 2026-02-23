using System.Text.Json;
using Encina.Compliance.Consent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB document representation of a <see cref="ConsentRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the consents collection. Uses snake_case naming convention for MongoDB
/// field names to follow MongoDB community conventions.
/// </para>
/// <para>
/// Metadata is stored as a native BSON document for efficient querying and indexing,
/// rather than a serialized JSON string.
/// </para>
/// </remarks>
public sealed class ConsentRecordDocument
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Gets or sets the unique identifier for this consent record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the data subject who gave consent.
    /// </summary>
    [BsonElement("subject_id")]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing purpose for which consent was given.
    /// </summary>
    [BsonElement("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of this consent record.
    /// </summary>
    [BsonElement("status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the consent version the data subject agreed to.
    /// </summary>
    [BsonElement("consent_version_id")]
    public string ConsentVersionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the data subject gave consent (UTC).
    /// </summary>
    [BsonElement("given_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime GivenAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data subject withdrew consent (UTC).
    /// </summary>
    [BsonElement("withdrawn_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? WithdrawnAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this consent expires (UTC).
    /// </summary>
    [BsonElement("expires_at_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the source or channel through which consent was collected.
    /// </summary>
    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the data subject at the time consent was given.
    /// </summary>
    [BsonElement("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the hash or reference to the consent form shown to the data subject.
    /// </summary>
    [BsonElement("proof_of_consent")]
    public string? ProofOfConsent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata stored as a native BSON document.
    /// </summary>
    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    /// Creates a <see cref="ConsentRecordDocument"/> from a <see cref="ConsentRecord"/>.
    /// </summary>
    /// <param name="record">The consent record to convert.</param>
    /// <returns>A new document representation of the consent record.</returns>
    public static ConsentRecordDocument FromRecord(ConsentRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new ConsentRecordDocument
        {
            Id = record.Id,
            SubjectId = record.SubjectId,
            Purpose = record.Purpose,
            Status = (int)record.Status,
            ConsentVersionId = record.ConsentVersionId,
            GivenAtUtc = record.GivenAtUtc.UtcDateTime,
            WithdrawnAtUtc = record.WithdrawnAtUtc?.UtcDateTime,
            ExpiresAtUtc = record.ExpiresAtUtc?.UtcDateTime,
            Source = record.Source,
            IpAddress = record.IpAddress,
            ProofOfConsent = record.ProofOfConsent,
            Metadata = SerializeMetadata(record.Metadata)
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="ConsentRecord"/>.
    /// </summary>
    /// <returns>A consent record.</returns>
    public ConsentRecord ToRecord() => new()
    {
        Id = Id,
        SubjectId = SubjectId,
        Purpose = Purpose,
        Status = (ConsentStatus)Status,
        ConsentVersionId = ConsentVersionId,
        GivenAtUtc = new DateTimeOffset(GivenAtUtc, TimeSpan.Zero),
        WithdrawnAtUtc = WithdrawnAtUtc.HasValue ? new DateTimeOffset(WithdrawnAtUtc.Value, TimeSpan.Zero) : null,
        ExpiresAtUtc = ExpiresAtUtc.HasValue ? new DateTimeOffset(ExpiresAtUtc.Value, TimeSpan.Zero) : null,
        Source = Source,
        IpAddress = IpAddress,
        ProofOfConsent = ProofOfConsent,
        Metadata = DeserializeMetadata(Metadata)
    };

    private static BsonDocument? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        return BsonDocument.Parse(json);
    }

    private static Dictionary<string, object?> DeserializeMetadata(BsonDocument? bsonDocument)
    {
        if (bsonDocument is null || bsonDocument.ElementCount == 0)
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var json = bsonDocument.ToJson(new global::MongoDB.Bson.IO.JsonWriterSettings
            {
                OutputMode = global::MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson
            });
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
