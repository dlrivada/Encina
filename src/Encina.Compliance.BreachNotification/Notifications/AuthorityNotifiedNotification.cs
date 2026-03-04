namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Notification published when a supervisory authority has been notified about
/// a personal data breach.
/// </summary>
/// <remarks>
/// <para>
/// Published after <c>IBreachNotifier.NotifyAuthorityAsync</c> successfully delivers
/// the breach notification to the supervisory authority per GDPR Article 33(1).
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;AuthorityNotifiedNotification&gt;</c>
/// can use this to update internal tracking systems, send confirmation to stakeholders,
/// or trigger downstream compliance workflows.
/// </para>
/// </remarks>
/// <param name="BreachId">Identifier of the breach that was reported.</param>
/// <param name="NotifiedAtUtc">Timestamp when the authority was notified (UTC).</param>
/// <param name="Authority">Name or identifier of the supervisory authority notified.</param>
public sealed record AuthorityNotifiedNotification(
    string BreachId,
    DateTimeOffset NotifiedAtUtc,
    string Authority) : INotification;
