namespace Encina.Compliance.Consent;

/// <summary>
/// Domain event published when a data subject grants consent for a specific purpose.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised after a consent record is successfully stored via
/// <see cref="IConsentStore.RecordConsentAsync"/>. It enables downstream processes
/// such as audit logging, analytics, and notification flows to react to new consents.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;ConsentGrantedEvent&gt;</c> can
/// subscribe to this event to trigger workflows like sending confirmation emails,
/// updating marketing preferences, or synchronizing consent state across systems.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject who granted consent.</param>
/// <param name="Purpose">The processing purpose for which consent was granted.</param>
/// <param name="OccurredAtUtc">Timestamp when the consent was granted (UTC).</param>
/// <param name="ConsentVersionId">The version of consent terms the data subject agreed to.</param>
/// <param name="Source">The channel through which consent was collected (e.g., "web-form", "api").</param>
/// <param name="ExpiresAtUtc">
/// When the consent expires (UTC), or <c>null</c> if no expiration is set.
/// </param>
public sealed record ConsentGrantedEvent(
    string SubjectId,
    string Purpose,
    DateTimeOffset OccurredAtUtc,
    string ConsentVersionId,
    string Source,
    DateTimeOffset? ExpiresAtUtc) : INotification;
