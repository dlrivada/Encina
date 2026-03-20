using Encina.Compliance.AIAct.Model;

namespace Encina.Compliance.AIAct.Notifications;

/// <summary>
/// Notification published when an AI system's risk level has been reclassified
/// in the <c>IAISystemRegistry</c>.
/// </summary>
/// <remarks>
/// <para>
/// Article 6(3) acknowledges that AI systems may be reclassified based on changes
/// to their intended purpose, deployment context, or delegated acts by the European Commission.
/// This notification enables audit trail handlers to record the reclassification event.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;AISystemReclassifiedNotification&gt;</c>
/// can use this to update risk management documentation, trigger conformity reassessment,
/// or notify relevant stakeholders.
/// </para>
/// </remarks>
/// <param name="SystemId">Identifier of the AI system that was reclassified.</param>
/// <param name="PreviousRiskLevel">The risk level before reclassification.</param>
/// <param name="NewRiskLevel">The risk level after reclassification.</param>
/// <param name="Reason">Explanation of why the reclassification was performed.</param>
/// <param name="OccurredAtUtc">Timestamp when the reclassification occurred (UTC).</param>
public sealed record AISystemReclassifiedNotification(
    string SystemId,
    AIRiskLevel PreviousRiskLevel,
    AIRiskLevel NewRiskLevel,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;
