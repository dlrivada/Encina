namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Represents a supplementary measure identified during a Transfer Impact Assessment (TIA)
/// to ensure "essentially equivalent" data protection for international transfers.
/// </summary>
/// <remarks>
/// <para>
/// Following the Schrems II judgment (CJEU C-311/18), the EDPB Recommendations 01/2020
/// require data exporters to identify and implement supplementary measures when the
/// destination country's legal framework does not provide adequate protection on its own.
/// </para>
/// <para>
/// Each measure is categorized by <see cref="Type"/> (<see cref="SupplementaryMeasureType.Technical"/>,
/// <see cref="SupplementaryMeasureType.Contractual"/>, or <see cref="SupplementaryMeasureType.Organizational"/>)
/// and tracks whether it has been implemented and when.
/// </para>
/// </remarks>
public sealed record SupplementaryMeasure
{
    /// <summary>
    /// Unique identifier for this supplementary measure.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The category of this supplementary measure.
    /// </summary>
    /// <remarks>
    /// Determines whether this is a technical measure (e.g., encryption, pseudonymization),
    /// a contractual measure (e.g., audit rights, transparency obligations), or an
    /// organizational measure (e.g., access policies, staff training).
    /// </remarks>
    public required SupplementaryMeasureType Type { get; init; }

    /// <summary>
    /// Human-readable description of the supplementary measure.
    /// </summary>
    /// <remarks>
    /// Should clearly describe what the measure entails, how it addresses identified risks,
    /// and any relevant technical or contractual details. This description becomes part of
    /// the compliance audit trail.
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Indicates whether this supplementary measure has been implemented.
    /// </summary>
    /// <remarks>
    /// A TIA may identify required measures before they are implemented. Transfers should
    /// not be authorized until all required supplementary measures are in place.
    /// </remarks>
    public required bool IsImplemented { get; init; }

    /// <summary>
    /// Timestamp when this supplementary measure was implemented (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the measure has not yet been implemented (<see cref="IsImplemented"/> is <c>false</c>).
    /// Provides evidence of when protections were put in place for audit purposes.
    /// </remarks>
    public DateTimeOffset? ImplementedAtUtc { get; init; }
}
