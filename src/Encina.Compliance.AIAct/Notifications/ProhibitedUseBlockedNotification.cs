using Encina.Compliance.AIAct.Model;

namespace Encina.Compliance.AIAct.Notifications;

/// <summary>
/// Notification published when a request is blocked because it would involve
/// a prohibited AI practice under Article 5 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 5(1) establishes AI practices that are prohibited due to their potential
/// to violate fundamental rights. Violations are subject to administrative fines of
/// up to EUR 35 million or 7% of total worldwide annual turnover (Art. 99(3)).
/// </para>
/// <para>
/// This notification is published by the <c>AIActCompliancePipelineBehavior</c> when
/// enforcement mode is <see cref="AIActEnforcementMode.Block"/> or
/// <see cref="AIActEnforcementMode.Warn"/>. Handlers can use it for incident logging,
/// security alerting, or compliance reporting.
/// </para>
/// </remarks>
/// <param name="RequestTypeName">Fully qualified type name of the blocked request.</param>
/// <param name="SystemId">Identifier of the AI system associated with the prohibited use.</param>
/// <param name="Practice">The specific prohibited practice that was detected.</param>
/// <param name="OccurredAtUtc">Timestamp when the blocked attempt occurred (UTC).</param>
public sealed record ProhibitedUseBlockedNotification(
    string RequestTypeName,
    string SystemId,
    ProhibitedPractice Practice,
    DateTimeOffset OccurredAtUtc) : INotification;
