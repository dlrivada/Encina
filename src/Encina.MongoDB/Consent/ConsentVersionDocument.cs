using Encina.Compliance.Consent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Consent;

/// <summary>
/// MongoDB document representation of a <see cref="ConsentVersion"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the consent_versions collection. Tracks versions of consent terms
/// for specific processing purposes, enabling reconsent detection when terms change.
/// </para>
/// <para>
/// Uses snake_case naming convention for MongoDB field names.
/// The <see cref="VersionId"/> serves as the document's <c>_id</c> field.
/// </para>
/// </remarks>
public sealed class ConsentVersionDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for this consent version.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string VersionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing purpose this version applies to.
    /// </summary>
    [BsonElement("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp from which this version is effective (UTC).
    /// </summary>
    [BsonElement("effective_from_utc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EffectiveFromUtc { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description of what changed in this version.
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether existing consents under previous versions must be explicitly renewed.
    /// </summary>
    [BsonElement("requires_explicit_reconsent")]
    public bool RequiresExplicitReconsent { get; set; }

    /// <summary>
    /// Creates a <see cref="ConsentVersionDocument"/> from a <see cref="ConsentVersion"/>.
    /// </summary>
    /// <param name="version">The consent version to convert.</param>
    /// <returns>A new document representation of the consent version.</returns>
    public static ConsentVersionDocument FromVersion(ConsentVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new ConsentVersionDocument
        {
            VersionId = version.VersionId,
            Purpose = version.Purpose,
            EffectiveFromUtc = version.EffectiveFromUtc.UtcDateTime,
            Description = version.Description,
            RequiresExplicitReconsent = version.RequiresExplicitReconsent
        };
    }

    /// <summary>
    /// Converts this document to a <see cref="ConsentVersion"/> record.
    /// </summary>
    /// <returns>A consent version record.</returns>
    public ConsentVersion ToVersion() => new()
    {
        VersionId = VersionId,
        Purpose = Purpose,
        EffectiveFromUtc = new DateTimeOffset(EffectiveFromUtc, TimeSpan.Zero),
        Description = Description,
        RequiresExplicitReconsent = RequiresExplicitReconsent
    };
}
