using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.Compliance.CrossBorderTransfer.ReadModels;

/// <summary>
/// Read-only projected view of a Transfer Impact Assessment, built from TIA aggregate events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the <c>TIAAggregate</c> event stream by Marten
/// inline projections. It provides an efficient query view without replaying events,
/// while the underlying event stream maintains the full audit trail.
/// </para>
/// <para>
/// Used by <c>ITIAService</c> query methods to return TIA state to consumers.
/// </para>
/// </remarks>
public sealed record TIAReadModel
{
    /// <summary>
    /// Unique identifier for this Transfer Impact Assessment.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data exporter.
    /// </summary>
    public required string SourceCountryCode { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data importer.
    /// </summary>
    public required string DestinationCountryCode { get; init; }

    /// <summary>
    /// Category of personal data being assessed for transfer.
    /// </summary>
    public required string DataCategory { get; init; }

    /// <summary>
    /// Risk score assigned during assessment, between 0.0 and 1.0.
    /// </summary>
    /// <remarks>
    /// <c>null</c> until risk assessment is performed.
    /// </remarks>
    public double? RiskScore { get; init; }

    /// <summary>
    /// Current lifecycle status of the TIA.
    /// </summary>
    public required TIAStatus Status { get; init; }

    /// <summary>
    /// Summary of risk assessment findings.
    /// </summary>
    public string? Findings { get; init; }

    /// <summary>
    /// Identifier of the person who performed the risk assessment.
    /// </summary>
    public string? AssessorId { get; init; }

    /// <summary>
    /// Timestamp when the DPO completed their review (UTC).
    /// </summary>
    public DateTimeOffset? DPOReviewedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the TIA was completed (UTC).
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; init; }

    /// <summary>
    /// Supplementary measures identified as required during the assessment.
    /// </summary>
    public required IReadOnlyList<SupplementaryMeasure> RequiredSupplementaryMeasures { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Timestamp when the TIA was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp of the last modification to the TIA (UTC).
    /// </summary>
    public required DateTimeOffset LastModifiedAtUtc { get; init; }
}
