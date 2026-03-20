namespace Encina.Compliance.AIAct.Notifications;

/// <summary>
/// Notification published when a request requires human oversight review
/// under Article 14 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 14 requires high-risk AI systems to be designed and developed in such a way
/// that they can be effectively overseen by natural persons. This notification is published
/// when the <c>AIActCompliancePipelineBehavior</c> determines that a request targets a
/// high-risk system requiring human review before execution.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;HumanOversightRequiredNotification&gt;</c>
/// can use this to queue the request for human review, notify assigned reviewers,
/// or integrate with external workflow systems.
/// </para>
/// </remarks>
/// <param name="RequestTypeName">Fully qualified type name of the request requiring oversight.</param>
/// <param name="SystemId">Identifier of the AI system that requires human oversight.</param>
/// <param name="Reason">Explanation of why human oversight is required for this request.</param>
/// <param name="OccurredAtUtc">Timestamp when the oversight requirement was identified (UTC).</param>
public sealed record HumanOversightRequiredNotification(
    string RequestTypeName,
    string SystemId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;
