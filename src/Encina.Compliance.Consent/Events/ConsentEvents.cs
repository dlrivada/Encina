namespace Encina.Compliance.Consent.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

/// <summary>
/// Raised when a data subject grants consent for a specific processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the consent lifecycle. The aggregate transitions to <see cref="ConsentStatus.Active"/> status.
/// This event captures all proof data required by GDPR Article 7(1) to demonstrate that
/// the data subject has consented to the processing of their personal data.
/// </para>
/// <para>
/// The <paramref name="Source"/>, <paramref name="IpAddress"/>, and <paramref name="ProofOfConsent"/>
/// fields provide evidence of how, when, and where consent was collected — essential for
/// demonstrating compliance under GDPR Article 7(1) ("the controller shall be able to
/// demonstrate that the data subject has consented").
/// </para>
/// </remarks>
/// <param name="ConsentId">Unique identifier for this consent aggregate.</param>
/// <param name="DataSubjectId">Identifier of the data subject who granted consent.</param>
/// <param name="Purpose">The specific processing purpose for which consent was given (per Art. 6(1)(a)).</param>
/// <param name="ConsentVersionId">The version of consent terms the data subject agreed to.</param>
/// <param name="Source">The channel through which consent was collected (e.g., "web-form", "api", "mobile-app").</param>
/// <param name="IpAddress">IP address of the data subject at the time of consent, or <c>null</c> if unavailable.</param>
/// <param name="ProofOfConsent">Hash or reference to the consent form shown, or <c>null</c> if not tracked.</param>
/// <param name="Metadata">Additional key-value metadata associated with this consent (e.g., user agent, A/B variant).</param>
/// <param name="ExpiresAtUtc">When the consent expires (UTC), or <c>null</c> if no expiration is set.</param>
/// <param name="GrantedBy">Identifier of the actor who recorded the consent (may differ from the data subject).</param>
/// <param name="OccurredAtUtc">Timestamp when consent was granted (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record ConsentGranted(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    string ConsentVersionId,
    string Source,
    string? IpAddress,
    string? ProofOfConsent,
    IReadOnlyDictionary<string, object?> Metadata,
    DateTimeOffset? ExpiresAtUtc,
    string GrantedBy,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a data subject withdraws consent for a specific processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the consent from <see cref="ConsentStatus.Active"/> to <see cref="ConsentStatus.Withdrawn"/>.
/// Per GDPR Article 7(3), withdrawing consent must be as easy as giving it. Processing based
/// on this consent must cease upon withdrawal.
/// </para>
/// <para>
/// Systems should use this event to immediately stop data processing for the affected purpose,
/// trigger data deletion workflows, and update marketing preferences.
/// </para>
/// </remarks>
/// <param name="ConsentId">The consent aggregate identifier.</param>
/// <param name="DataSubjectId">Identifier of the data subject who withdrew consent.</param>
/// <param name="Purpose">The processing purpose for which consent was withdrawn.</param>
/// <param name="WithdrawnBy">Identifier of the actor who recorded the withdrawal.</param>
/// <param name="Reason">Optional reason provided by the data subject for withdrawal.</param>
/// <param name="OccurredAtUtc">Timestamp when the withdrawal was recorded (UTC).</param>
public sealed record ConsentWithdrawn(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    string WithdrawnBy,
    string? Reason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a consent record passes its expiration date and is no longer valid.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the consent from <see cref="ConsentStatus.Active"/> to <see cref="ConsentStatus.Expired"/>.
/// Unlike <see cref="ConsentWithdrawn"/>, expiration is a passive process — the data subject
/// did not actively withdraw consent, but the consent reached its time limit.
/// </para>
/// <para>
/// Systems should treat expired consent similarly to withdrawn consent by stopping data
/// processing for the affected purpose until reconsent is obtained. This event is typically
/// published by background processors or expiration checkers.
/// </para>
/// </remarks>
/// <param name="ConsentId">The consent aggregate identifier.</param>
/// <param name="DataSubjectId">Identifier of the data subject whose consent expired.</param>
/// <param name="Purpose">The processing purpose for which consent expired.</param>
/// <param name="ExpiredAtUtc">The original expiration timestamp from the consent record (UTC).</param>
/// <param name="OccurredAtUtc">Timestamp when the expiration was detected (UTC).</param>
public sealed record ConsentExpired(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    DateTimeOffset ExpiredAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a data subject renews their existing consent, extending its validity.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the consent back to <see cref="ConsentStatus.Active"/> (if it was expiring or
/// the data subject proactively re-confirmed). The consent version may be updated and the
/// expiration extended. This is distinct from <see cref="ConsentReconsentProvided"/> which
/// responds to a version change requiring reconsent.
/// </para>
/// <para>
/// GDPR Article 7(1) requires demonstrable proof of consent — renewal events provide
/// a fresh evidence trail that the data subject continues to consent.
/// </para>
/// </remarks>
/// <param name="ConsentId">The consent aggregate identifier.</param>
/// <param name="DataSubjectId">Identifier of the data subject who renewed consent.</param>
/// <param name="Purpose">The processing purpose for which consent was renewed.</param>
/// <param name="ConsentVersionId">The consent version under which renewal was provided.</param>
/// <param name="NewExpiresAtUtc">The new expiration date (UTC), or <c>null</c> if no expiration.</param>
/// <param name="RenewedBy">Identifier of the actor who recorded the renewal.</param>
/// <param name="Source">The channel through which renewal was collected, or <c>null</c> if unchanged.</param>
/// <param name="OccurredAtUtc">Timestamp when the renewal was recorded (UTC).</param>
public sealed record ConsentRenewed(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    string ConsentVersionId,
    DateTimeOffset? NewExpiresAtUtc,
    string RenewedBy,
    string? Source,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when consent terms are updated for a processing purpose, potentially requiring reconsent.
/// </summary>
/// <remarks>
/// <para>
/// When <paramref name="RequiresReconsent"/> is <c>true</c>, the consent transitions to
/// <see cref="ConsentStatus.RequiresReconsent"/> — the data subject must provide fresh consent
/// under the new terms before processing can continue. When <c>false</c>, the consent remains
/// <see cref="ConsentStatus.Active"/> (e.g., minor clarifications that don't affect scope).
/// </para>
/// <para>
/// This event supports GDPR Article 7 requirements by ensuring that consent is always linked
/// to the specific terms the data subject was presented with, and that material changes to
/// those terms trigger appropriate reconsent flows.
/// </para>
/// </remarks>
/// <param name="ConsentId">The consent aggregate identifier.</param>
/// <param name="DataSubjectId">Identifier of the data subject affected by the version change.</param>
/// <param name="Purpose">The processing purpose whose consent terms changed.</param>
/// <param name="PreviousVersionId">The consent version the data subject originally agreed to.</param>
/// <param name="NewVersionId">The newly published consent version.</param>
/// <param name="Description">Human-readable description of what changed in the new version.</param>
/// <param name="RequiresReconsent">Whether the data subject must provide fresh consent under the new terms.</param>
/// <param name="ChangedBy">Identifier of the actor who published the new version.</param>
/// <param name="OccurredAtUtc">Timestamp when the version change was recorded (UTC).</param>
public sealed record ConsentVersionChanged(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    string PreviousVersionId,
    string NewVersionId,
    string Description,
    bool RequiresReconsent,
    string ChangedBy,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a data subject provides fresh consent under updated terms after a version change.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the consent from <see cref="ConsentStatus.RequiresReconsent"/> back to
/// <see cref="ConsentStatus.Active"/> under the new consent version. This event captures
/// the same proof data as <see cref="ConsentGranted"/> because it represents a new act of
/// consent that must be independently demonstrable under GDPR Article 7(1).
/// </para>
/// <para>
/// This is distinct from <see cref="ConsentRenewed"/> which extends an existing valid consent.
/// Reconsent is specifically in response to a <see cref="ConsentVersionChanged"/> event that
/// required explicit renewal.
/// </para>
/// </remarks>
/// <param name="ConsentId">The consent aggregate identifier.</param>
/// <param name="DataSubjectId">Identifier of the data subject who provided reconsent.</param>
/// <param name="Purpose">The processing purpose for which reconsent was provided.</param>
/// <param name="NewConsentVersionId">The new consent version the data subject agreed to.</param>
/// <param name="Source">The channel through which reconsent was collected.</param>
/// <param name="IpAddress">IP address of the data subject at the time of reconsent, or <c>null</c>.</param>
/// <param name="ProofOfConsent">Hash or reference to the consent form shown, or <c>null</c>.</param>
/// <param name="Metadata">Additional key-value metadata associated with this reconsent.</param>
/// <param name="ExpiresAtUtc">When the reconsent expires (UTC), or <c>null</c> if no expiration.</param>
/// <param name="GrantedBy">Identifier of the actor who recorded the reconsent.</param>
/// <param name="OccurredAtUtc">Timestamp when reconsent was provided (UTC).</param>
public sealed record ConsentReconsentProvided(
    Guid ConsentId,
    string DataSubjectId,
    string Purpose,
    string NewConsentVersionId,
    string Source,
    string? IpAddress,
    string? ProofOfConsent,
    IReadOnlyDictionary<string, object?> Metadata,
    DateTimeOffset? ExpiresAtUtc,
    string GrantedBy,
    DateTimeOffset OccurredAtUtc) : INotification;
