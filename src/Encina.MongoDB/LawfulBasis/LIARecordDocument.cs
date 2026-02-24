using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Compliance.GDPR;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.LawfulBasis;

/// <summary>
/// MongoDB document representation of a <see cref="LIARecordEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the lia_records collection. Uses snake_case naming convention
/// for MongoDB field names to follow MongoDB community conventions.
/// </para>
/// <para>
/// AlternativesConsidered and Safeguards are stored as native BSON arrays
/// for efficient querying, rather than serialized JSON strings.
/// </para>
/// </remarks>
public sealed class LIARecordDocument
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assessment name.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose of processing.
    /// </summary>
    [BsonElement("purpose")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legitimate interest description.
    /// </summary>
    [BsonElement("legitimate_interest")]
    public string LegitimateInterest { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the benefits of processing.
    /// </summary>
    [BsonElement("benefits")]
    public string Benefits { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consequences if data is not processed.
    /// </summary>
    [BsonElement("consequences_if_not_processed")]
    public string ConsequencesIfNotProcessed { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the necessity justification.
    /// </summary>
    [BsonElement("necessity_justification")]
    public string NecessityJustification { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alternatives considered as a native list.
    /// </summary>
    [BsonElement("alternatives_considered")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "MongoDB BSON deserialization requires mutable setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "MongoDB BSON driver maps List<T> to native BSON arrays")]
    public List<string> AlternativesConsidered { get; set; } = [];

    /// <summary>
    /// Gets or sets the data minimisation notes.
    /// </summary>
    [BsonElement("data_minimisation_notes")]
    public string DataMinimisationNotes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nature of data being processed.
    /// </summary>
    [BsonElement("nature_of_data")]
    public string NatureOfData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reasonable expectations assessment.
    /// </summary>
    [BsonElement("reasonable_expectations")]
    public string ReasonableExpectations { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the impact assessment.
    /// </summary>
    [BsonElement("impact_assessment")]
    public string ImpactAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the safeguards as a native list.
    /// </summary>
    [BsonElement("safeguards")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "MongoDB BSON deserialization requires mutable setter")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "MongoDB BSON driver maps List<T> to native BSON arrays")]
    public List<string> Safeguards { get; set; } = [];

    /// <summary>
    /// Gets or sets the outcome value.
    /// </summary>
    [BsonElement("outcome_value")]
    public int OutcomeValue { get; set; }

    /// <summary>
    /// Gets or sets the conclusion.
    /// </summary>
    [BsonElement("conclusion")]
    public string Conclusion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conditions (optional).
    /// </summary>
    [BsonElement("conditions")]
    [BsonIgnoreIfNull]
    public string? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the assessment timestamp in UTC.
    /// </summary>
    [BsonElement("assessed_at_utc")]
    public DateTimeOffset AssessedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the assessor name.
    /// </summary>
    [BsonElement("assessed_by")]
    public string AssessedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether a DPO was involved.
    /// </summary>
    [BsonElement("dpo_involvement")]
    public bool DPOInvolvement { get; set; }

    /// <summary>
    /// Gets or sets the next review date (optional).
    /// </summary>
    [BsonElement("next_review_at_utc")]
    [BsonIgnoreIfNull]
    public DateTimeOffset? NextReviewAtUtc { get; set; }

    /// <summary>
    /// Creates a document from an entity.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A new document instance.</returns>
    public static LIARecordDocument FromEntity(LIARecordEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LIARecordDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Purpose = entity.Purpose,
            LegitimateInterest = entity.LegitimateInterest,
            Benefits = entity.Benefits,
            ConsequencesIfNotProcessed = entity.ConsequencesIfNotProcessed,
            NecessityJustification = entity.NecessityJustification,
            AlternativesConsidered = DeserializeList(entity.AlternativesConsideredJson),
            DataMinimisationNotes = entity.DataMinimisationNotes,
            NatureOfData = entity.NatureOfData,
            ReasonableExpectations = entity.ReasonableExpectations,
            ImpactAssessment = entity.ImpactAssessment,
            Safeguards = DeserializeList(entity.SafeguardsJson),
            OutcomeValue = entity.OutcomeValue,
            Conclusion = entity.Conclusion,
            Conditions = entity.Conditions,
            AssessedAtUtc = entity.AssessedAtUtc,
            AssessedBy = entity.AssessedBy,
            DPOInvolvement = entity.DPOInvolvement,
            NextReviewAtUtc = entity.NextReviewAtUtc
        };
    }

    /// <summary>
    /// Converts this document to an entity.
    /// </summary>
    /// <returns>A new entity instance.</returns>
    public LIARecordEntity ToEntity() => new()
    {
        Id = Id,
        Name = Name,
        Purpose = Purpose,
        LegitimateInterest = LegitimateInterest,
        Benefits = Benefits,
        ConsequencesIfNotProcessed = ConsequencesIfNotProcessed,
        NecessityJustification = NecessityJustification,
        AlternativesConsideredJson = JsonSerializer.Serialize(AlternativesConsidered),
        DataMinimisationNotes = DataMinimisationNotes,
        NatureOfData = NatureOfData,
        ReasonableExpectations = ReasonableExpectations,
        ImpactAssessment = ImpactAssessment,
        SafeguardsJson = JsonSerializer.Serialize(Safeguards),
        OutcomeValue = OutcomeValue,
        Conclusion = Conclusion,
        Conditions = Conditions,
        AssessedAtUtc = AssessedAtUtc,
        AssessedBy = AssessedBy,
        DPOInvolvement = DPOInvolvement,
        NextReviewAtUtc = NextReviewAtUtc
    };

    private static List<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}
