namespace Encina.Compliance.Consent;

/// <summary>
/// Domain event representing the expiration of a previously active consent.
/// </summary>
/// <remarks>
/// <para>
/// This event can be published when a consent record's <see cref="ConsentRecord.ExpiresAtUtc"/>
/// has been reached and the consent transitions to <see cref="ConsentStatus.Expired"/>.
/// </para>
/// <para>
/// Unlike <see cref="ConsentWithdrawnEvent"/>, expiration is a passive process — the data
/// subject did not actively withdraw consent, but the consent reached its time limit.
/// Systems should treat expired consent similarly to withdrawn consent by stopping
/// data processing for the affected purpose until reconsent is obtained.
/// </para>
/// <para>
/// This event is typically published by background processors or expiration checkers
/// rather than inline during read operations, to avoid side effects on queries.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose consent expired.</param>
/// <param name="Purpose">The processing purpose for which consent expired.</param>
/// <param name="OccurredAtUtc">Timestamp when the expiration was detected (UTC).</param>
/// <param name="ExpiredAtUtc">The original expiration timestamp from the consent record (UTC).</param>
public sealed record ConsentExpiredEvent(
    string SubjectId,
    string Purpose,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ExpiredAtUtc) : INotification;
