using System.Text.Json;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Maps between <see cref="LIARecord"/> domain records and
/// <see cref="LIARecordEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the serialization of <see cref="IReadOnlyList{T}"/> properties
/// (<see cref="LIARecord.AlternativesConsidered"/> and <see cref="LIARecord.Safeguards"/>)
/// to/from JSON strings using <see cref="System.Text.Json.JsonSerializer"/>.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve LIA records without coupling to the domain model.
/// </para>
/// </remarks>
public static class LIARecordMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Converts a domain <see cref="LIARecord"/> to a persistence entity.
    /// </summary>
    /// <param name="record">The domain LIA record to convert.</param>
    /// <returns>A <see cref="LIARecordEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <c>null</c>.</exception>
    public static LIARecordEntity ToEntity(LIARecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new LIARecordEntity
        {
            Id = record.Id,
            Name = record.Name,
            Purpose = record.Purpose,
            LegitimateInterest = record.LegitimateInterest,
            Benefits = record.Benefits,
            ConsequencesIfNotProcessed = record.ConsequencesIfNotProcessed,
            NecessityJustification = record.NecessityJustification,
            AlternativesConsideredJson = JsonSerializer.Serialize(record.AlternativesConsidered, JsonOptions),
            DataMinimisationNotes = record.DataMinimisationNotes,
            NatureOfData = record.NatureOfData,
            ReasonableExpectations = record.ReasonableExpectations,
            ImpactAssessment = record.ImpactAssessment,
            SafeguardsJson = JsonSerializer.Serialize(record.Safeguards, JsonOptions),
            OutcomeValue = (int)record.Outcome,
            Conclusion = record.Conclusion,
            Conditions = record.Conditions,
            AssessedAtUtc = record.AssessedAtUtc,
            AssessedBy = record.AssessedBy,
            DPOInvolvement = record.DPOInvolvement,
            NextReviewAtUtc = record.NextReviewAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="LIARecord"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="LIARecord"/> populated from the entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static LIARecord ToDomain(LIARecordEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LIARecord
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
            Outcome = (LIAOutcome)entity.OutcomeValue,
            Conclusion = entity.Conclusion,
            Conditions = entity.Conditions,
            AssessedAtUtc = entity.AssessedAtUtc,
            AssessedBy = entity.AssessedBy,
            DPOInvolvement = entity.DPOInvolvement,
            NextReviewAtUtc = entity.NextReviewAtUtc
        };
    }

    private static List<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}
