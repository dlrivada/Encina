using Encina.Compliance.GDPR;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ProcessingActivity;

/// <summary>
/// MongoDB document representation of a <see cref="ProcessingActivityEntity"/>.
/// </summary>
/// <remarks>
/// Maps to the processing_activities collection. Uses snake_case naming convention
/// for MongoDB field names to follow MongoDB community conventions.
/// </remarks>
public sealed class ProcessingActivityDocument
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified request type name.
    /// </summary>
    [BsonElement("request_type_name")]
    public string RequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of this processing activity.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose of processing.
    /// </summary>
    [BsonElement("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lawful basis enum value.
    /// </summary>
    [BsonElement("lawful_basis_value")]
    public int LawfulBasisValue { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized categories of data subjects.
    /// </summary>
    [BsonElement("categories_of_data_subjects_json")]
    public string CategoriesOfDataSubjectsJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized categories of personal data.
    /// </summary>
    [BsonElement("categories_of_personal_data_json")]
    public string CategoriesOfPersonalDataJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized recipients.
    /// </summary>
    [BsonElement("recipients_json")]
    public string RecipientsJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the third-country transfers description, or <c>null</c> if none.
    /// </summary>
    [BsonElement("third_country_transfers")]
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Gets or sets the safeguards for transfers, or <c>null</c> if none.
    /// </summary>
    [BsonElement("safeguards")]
    public string? Safeguards { get; set; }

    /// <summary>
    /// Gets or sets the retention period as ticks.
    /// </summary>
    [BsonElement("retention_period_ticks")]
    public long RetentionPeriodTicks { get; set; }

    /// <summary>
    /// Gets or sets the security measures description.
    /// </summary>
    [BsonElement("security_measures")]
    public string SecurityMeasures { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    [BsonElement("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp in UTC.
    /// </summary>
    [BsonElement("last_updated_at_utc")]
    public DateTimeOffset LastUpdatedAtUtc { get; set; }

    /// <summary>
    /// Creates a document from an entity.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A new document instance.</returns>
    public static ProcessingActivityDocument FromEntity(ProcessingActivityEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ProcessingActivityDocument
        {
            Id = entity.Id,
            RequestTypeName = entity.RequestTypeName,
            Name = entity.Name,
            Purpose = entity.Purpose,
            LawfulBasisValue = entity.LawfulBasisValue,
            CategoriesOfDataSubjectsJson = entity.CategoriesOfDataSubjectsJson,
            CategoriesOfPersonalDataJson = entity.CategoriesOfPersonalDataJson,
            RecipientsJson = entity.RecipientsJson,
            ThirdCountryTransfers = entity.ThirdCountryTransfers,
            Safeguards = entity.Safeguards,
            RetentionPeriodTicks = entity.RetentionPeriodTicks,
            SecurityMeasures = entity.SecurityMeasures,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastUpdatedAtUtc = entity.LastUpdatedAtUtc
        };
    }

    /// <summary>
    /// Converts this document to an entity.
    /// </summary>
    /// <returns>A new entity instance.</returns>
    public ProcessingActivityEntity ToEntity() => new()
    {
        Id = Id,
        RequestTypeName = RequestTypeName,
        Name = Name,
        Purpose = Purpose,
        LawfulBasisValue = LawfulBasisValue,
        CategoriesOfDataSubjectsJson = CategoriesOfDataSubjectsJson,
        CategoriesOfPersonalDataJson = CategoriesOfPersonalDataJson,
        RecipientsJson = RecipientsJson,
        ThirdCountryTransfers = ThirdCountryTransfers,
        Safeguards = Safeguards,
        RetentionPeriodTicks = RetentionPeriodTicks,
        SecurityMeasures = SecurityMeasures,
        CreatedAtUtc = CreatedAtUtc,
        LastUpdatedAtUtc = LastUpdatedAtUtc
    };
}
