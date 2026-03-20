namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Records a human oversight decision made on an AI system output, as required
/// by Article 14 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 14 requires high-risk AI systems to be designed and developed in such a way
/// that they can be effectively overseen by natural persons during the period in which
/// they are in use. Human oversight measures shall aim to minimise the risks to health,
/// safety, or fundamental rights.
/// </para>
/// <para>
/// This record captures the decision, rationale, and context of human review. In the core
/// package, <c>HumanDecisionRecord</c> is an in-memory domain model. Full persistence
/// across 13 database providers is implemented in the child issue
/// "AI Act Human Oversight &amp; Decision Records" (#839).
/// </para>
/// </remarks>
public sealed record HumanDecisionRecord
{
    /// <summary>
    /// Unique identifier for this human decision.
    /// </summary>
    public required Guid DecisionId { get; init; }

    /// <summary>
    /// Identifier of the AI system whose output was reviewed.
    /// </summary>
    public required string SystemId { get; init; }

    /// <summary>
    /// Identifier of the human reviewer who made the decision.
    /// </summary>
    /// <remarks>
    /// Article 14(4)(a) requires that the natural persons to whom human oversight is assigned
    /// are able to fully understand the capacities and limitations of the high-risk AI system.
    /// </remarks>
    public required string ReviewerId { get; init; }

    /// <summary>
    /// Timestamp when the human review was completed (UTC).
    /// </summary>
    public required DateTimeOffset ReviewedAtUtc { get; init; }

    /// <summary>
    /// The decision made by the human reviewer (e.g., "approved", "rejected", "escalated").
    /// </summary>
    public required string Decision { get; init; }

    /// <summary>
    /// Free-text rationale explaining why the reviewer reached this decision.
    /// </summary>
    /// <remarks>
    /// Documenting rationale is essential for demonstrating effective human oversight
    /// and for audit trail requirements under Article 12.
    /// </remarks>
    public required string Rationale { get; init; }

    /// <summary>
    /// Fully qualified type name of the Encina request that triggered the oversight requirement.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the decision was recorded outside the context of an Encina request pipeline.
    /// </remarks>
    public string? RequestTypeName { get; init; }

    /// <summary>
    /// Correlation identifier linking this decision to the broader operation or trace.
    /// </summary>
    public string? CorrelationId { get; init; }
}
