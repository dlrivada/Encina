namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Records a change in an AI system's risk classification, capturing the previous
/// and new risk levels along with the reason for reclassification.
/// </summary>
/// <remarks>
/// <para>
/// Article 6(3) acknowledges that AI systems may be reclassified as high-risk or
/// downgraded based on changes to their intended purpose, deployment context, or
/// the adoption of delegated acts by the European Commission.
/// </para>
/// <para>
/// This record is created when <c>IAISystemRegistry.ReclassifyAsync</c> is called
/// and a corresponding <see cref="Notifications.AISystemReclassifiedNotification"/>
/// is published for audit purposes.
/// </para>
/// </remarks>
public sealed record ReclassificationRecord
{
    /// <summary>
    /// Identifier of the AI system whose risk level was changed.
    /// </summary>
    public required string SystemId { get; init; }

    /// <summary>
    /// The risk level before reclassification.
    /// </summary>
    public required AIRiskLevel PreviousRiskLevel { get; init; }

    /// <summary>
    /// The risk level after reclassification.
    /// </summary>
    public required AIRiskLevel NewRiskLevel { get; init; }

    /// <summary>
    /// Explanation of why the reclassification was performed.
    /// </summary>
    /// <example>"Intended purpose changed from general advice to employment decision support."</example>
    public required string Reason { get; init; }

    /// <summary>
    /// Timestamp when the reclassification was performed (UTC).
    /// </summary>
    public required DateTimeOffset ReclassifiedAtUtc { get; init; }

    /// <summary>
    /// Identifier of the person or process that performed the reclassification.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the reclassification was triggered automatically (e.g., by
    /// a delegated act or periodic review).
    /// </remarks>
    public string? ReclassifiedBy { get; init; }
}
