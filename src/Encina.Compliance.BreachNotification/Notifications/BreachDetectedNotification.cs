namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Notification published when a personal data breach has been detected and
/// a formal <see cref="Model.BreachRecord"/> has been created.
/// </summary>
/// <remarks>
/// <para>
/// Published after the <c>IBreachHandler</c> creates a new breach record from a
/// <see cref="Model.PotentialBreach"/> identified by the detection engine.
/// This marks the start of the 72-hour notification window per GDPR Article 33(1).
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;BreachDetectedNotification&gt;</c>
/// can use this to trigger internal alerting, escalation workflows, or incident
/// response procedures.
/// </para>
/// </remarks>
/// <param name="BreachId">Identifier of the breach record that was created.</param>
/// <param name="Severity">Assessed severity of the detected breach.</param>
/// <param name="Nature">Description of the nature of the breach.</param>
/// <param name="OccurredAtUtc">Timestamp when the breach was detected (UTC).</param>
public sealed record BreachDetectedNotification(
    string BreachId,
    Model.BreachSeverity Severity,
    string Nature,
    DateTimeOffset OccurredAtUtc) : INotification;
