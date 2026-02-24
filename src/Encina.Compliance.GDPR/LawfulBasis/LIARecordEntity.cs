namespace Encina.Compliance.GDPR;

/// <summary>
/// Persistence entity for <see cref="LIARecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a LIA record,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Collection properties (<see cref="AlternativesConsideredJson"/> and <see cref="SafeguardsJson"/>)
/// are serialized as JSON strings to support flat table storage across all providers.
/// </para>
/// <para>
/// Use <see cref="LIARecordMapper"/> to convert between this entity and <see cref="LIARecord"/>.
/// </para>
/// </remarks>
public sealed class LIARecordEntity
{
    // --- Identification ---

    /// <summary>Unique identifier for this LIA record (maps to <see cref="LIARecord.Id"/>).</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable name of this LIA.</summary>
    public required string Name { get; set; }

    /// <summary>The processing purpose this LIA covers.</summary>
    public required string Purpose { get; set; }

    // --- Purpose Test ---

    /// <summary>Description of the legitimate interest being pursued.</summary>
    public required string LegitimateInterest { get; set; }

    /// <summary>The benefits of the processing.</summary>
    public required string Benefits { get; set; }

    /// <summary>The consequences of not carrying out the processing.</summary>
    public required string ConsequencesIfNotProcessed { get; set; }

    // --- Necessity Test ---

    /// <summary>Justification for necessity of the processing.</summary>
    public required string NecessityJustification { get; set; }

    /// <summary>Alternatives considered, serialized as JSON array string.</summary>
    public required string AlternativesConsideredJson { get; set; }

    /// <summary>Notes on data minimisation measures.</summary>
    public required string DataMinimisationNotes { get; set; }

    // --- Balancing Test ---

    /// <summary>Description of the nature of the personal data.</summary>
    public required string NatureOfData { get; set; }

    /// <summary>Data subject's reasonable expectations regarding the processing.</summary>
    public required string ReasonableExpectations { get; set; }

    /// <summary>Assessment of impact on data subjects' rights and freedoms.</summary>
    public required string ImpactAssessment { get; set; }

    /// <summary>Safeguards implemented, serialized as JSON array string.</summary>
    public required string SafeguardsJson { get; set; }

    // --- Outcome ---

    /// <summary>Integer value of the <see cref="LIAOutcome"/> enum.</summary>
    public required int OutcomeValue { get; set; }

    /// <summary>Summary conclusion of the assessment.</summary>
    public required string Conclusion { get; set; }

    /// <summary>Conditions attached to the approval, if any.</summary>
    public string? Conditions { get; set; }

    // --- Governance ---

    /// <summary>Timestamp when the assessment was conducted (UTC).</summary>
    public DateTimeOffset AssessedAtUtc { get; set; }

    /// <summary>Name or role of the person who conducted the assessment.</summary>
    public required string AssessedBy { get; set; }

    /// <summary>Whether the DPO was involved in the assessment.</summary>
    public bool DPOInvolvement { get; set; }

    /// <summary>Timestamp when the next periodic review is due (UTC).</summary>
    public DateTimeOffset? NextReviewAtUtc { get; set; }
}
