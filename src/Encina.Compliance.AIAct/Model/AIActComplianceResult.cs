namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Captures the result of an AI Act compliance evaluation for an AI system,
/// summarising the system's risk classification and applicable obligations.
/// </summary>
/// <remarks>
/// <para>
/// Produced by the <c>AIActCompliancePipelineBehavior</c> or <c>IAIActClassifier</c> when
/// evaluating a request against the AI Act requirements. The result indicates whether the
/// system is prohibited, which obligations apply, and any identified violations.
/// </para>
/// <para>
/// When <see cref="IsProhibited"/> is <c>true</c>, the system must not be placed on the
/// market or put into service (Art. 5). When <see cref="Violations"/> is non-empty,
/// the system does not meet the requirements and the enforcement mode determines the action.
/// </para>
/// </remarks>
public sealed record AIActComplianceResult
{
    /// <summary>
    /// Identifier of the AI system that was evaluated.
    /// </summary>
    public required string SystemId { get; init; }

    /// <summary>
    /// The assessed risk level of the AI system.
    /// </summary>
    public required AIRiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Whether the AI system falls under a prohibited practice defined in Article 5.
    /// </summary>
    public required bool IsProhibited { get; init; }

    /// <summary>
    /// Whether the AI system requires human oversight measures under Article 14.
    /// </summary>
    /// <remarks>
    /// <c>true</c> for all high-risk AI systems and for systems explicitly marked
    /// with <c>[RequireHumanOversight]</c>.
    /// </remarks>
    public required bool RequiresHumanOversight { get; init; }

    /// <summary>
    /// Whether the AI system is subject to transparency obligations under Articles 13 or 50.
    /// </summary>
    public required bool RequiresTransparency { get; init; }

    /// <summary>
    /// List of specific compliance violations identified during the evaluation.
    /// </summary>
    /// <remarks>
    /// An empty list indicates full compliance. Each violation is a human-readable
    /// description referencing the specific AI Act article and requirement.
    /// </remarks>
    public IReadOnlyList<string> Violations { get; init; } = [];

    /// <summary>
    /// Timestamp when the compliance evaluation was performed (UTC).
    /// </summary>
    public required DateTimeOffset EvaluatedAtUtc { get; init; }
}
