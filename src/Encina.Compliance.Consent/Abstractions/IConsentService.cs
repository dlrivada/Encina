using Encina.Compliance.Consent.ReadModels;
using LanguageExt;

namespace Encina.Compliance.Consent.Abstractions;

/// <summary>
/// Service interface for managing consent lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for granting, withdrawing, renewing, and querying consent.
/// The implementation wraps the event-sourced <c>ConsentAggregate</c> via
/// <c>IAggregateRepository&lt;ConsentAggregate&gt;</c>, handling aggregate loading,
/// command execution, persistence, and cache management.
/// </para>
/// <para>
/// Consent is the legal basis for data processing under GDPR Article 6(1)(a).
/// This service implements the full consent lifecycle: Grant → Active → Withdrawn/Expired/RequiresReconsent.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="GrantConsentAsync"/> — Records a grant: the first one, or a renewal, regrant or reconsent on the existing record</description></item>
///   <item><description><see cref="WithdrawConsentAsync"/> — Withdraws active consent (Article 7(3))</description></item>
///   <item><description><see cref="RenewConsentAsync"/> — Renews consent with updated terms</description></item>
///   <item><description><see cref="ProvideReconsentAsync"/> — Provides fresh consent after version change</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetConsentAsync"/> — Retrieves a consent by ID</description></item>
///   <item><description><see cref="GetConsentBySubjectAndPurposeAsync"/> — Looks up consent by subject + purpose</description></item>
///   <item><description><see cref="GetAllConsentsAsync"/> — Lists all consents for a subject</description></item>
///   <item><description><see cref="HasValidConsentAsync"/> — Quick consent validity check</description></item>
///   <item><description><see cref="GetConsentHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IConsentService
{
    // ========================================================================
    // Command operations (write-side via ConsentAggregate)
    // ========================================================================

    /// <summary>
    /// Grants consent for a data subject and specific processing purpose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One record per key (#3769).</b> At most one consent record exists per (tenant, data
    /// subject, purpose). The tenant is the effective tenant: <paramref name="tenantId"/> when given,
    /// otherwise the ambient tenant; a missing tenant where tenant isolation is required, or an
    /// explicit tenant that differs from the ambient one, is refused with
    /// <see cref="ConsentErrors.TenantRequiredCode"/>. A <c>null</c> and an empty tenant are the same
    /// "no tenant" key; the data subject and the purpose are compared ordinally (case-sensitive,
    /// not trimmed). The consent id is derived deterministically from the key, so the event store
    /// rejects a second record atomically, even when two grants for the same key run at once.
    /// </para>
    /// <para>
    /// <b>A grant on an existing record (#3771).</b> When the key already has a record, the grant
    /// is recorded on it as a new act, with its own proof, version and expiry, and the consent id of
    /// the record is returned:
    /// <list type="bullet">
    ///   <item><description><b>Active</b>: recorded as a renewal (<c>ConsentRenewedByGrant</c>).
    ///   Identical repeats (a double click, a redelivered message) append identical events.</description></item>
    ///   <item><description><b>Withdrawn</b> or <b>Expired</b>: recorded as a regrant
    ///   (<c>ConsentRegranted</c>), which makes the consent Active again; the withdrawal or expiry
    ///   stays in the history.</description></item>
    ///   <item><description><b>RequiresReconsent</b>: accepted as the reconsent
    ///   (<c>ConsentReconsentProvided</c>) only when <paramref name="consentVersionId"/> is the
    ///   current consent version; any other version is rejected with
    ///   <see cref="ConsentErrors.ReconsentRequiredCode"/>, pointing to
    ///   <see cref="ProvideReconsentAsync"/>.</description></item>
    /// </list>
    /// A concurrent first grant that loses the race for the key continues as a grant on the existing
    /// record; an optimistic concurrency conflict while recording on an existing record is retried
    /// on the reloaded record (up to five attempts, so up to five concurrent writes on one key are
    /// all recorded), and a conflict that persists is returned unchanged
    /// (<c>marten.concurrency_conflict</c>) so the caller can retry. The cache entries of the
    /// consent are invalidated after the grant is saved.
    /// </para>
    /// <para>
    /// <b>The most recent act wins, by the act's own timestamp.</b> <paramref name="occurredAtUtc"/>
    /// is when the data subject acted, not when Encina received the request; it defaults to the
    /// current time of the injected <see cref="TimeProvider"/>. An act older than the record's
    /// latest act (for example a grant made before a withdrawal but delivered after it) is recorded
    /// in the history without changing the current state, so a late grant never undoes a later
    /// withdrawal. <b>Warning:</b> this timestamp comes from the client application. An
    /// unsynchronised client clock can order acts inconsistently (a grant may be kept only in the
    /// history, or may supersede a later act), and Encina cannot detect or correct it. Withdrawals,
    /// expiries and version changes are timed by Encina and always apply, and a withdrawal or expiry
    /// wins a tie with a grant at the same instant. A timestamp more than five minutes ahead of the
    /// injected clock is refused with <see cref="ConsentErrors.ActInFutureCode"/>, because as the
    /// ordering watermark it would make every later act look older and lock the consent. A late act
    /// still returns the consent id (it is recorded); read the state with
    /// <see cref="HasValidConsentAsync"/> when the outcome matters. With
    /// <c>EncinaMartenOptions.ThrowOnConcurrencyConflict</c> enabled the conflict is not retried
    /// and the grant returns <see cref="ConsentErrors.ServiceErrorCode"/>.
    /// </para>
    /// </remarks>
    /// <param name="dataSubjectId">Identifier of the data subject giving consent.</param>
    /// <param name="purpose">The specific processing purpose.</param>
    /// <param name="consentVersionId">Identifier of the consent terms version.</param>
    /// <param name="source">The channel through which consent was collected (e.g., "web-form", "api").</param>
    /// <param name="grantedBy">Identifier of the actor recording the consent.</param>
    /// <param name="ipAddress">IP address of the data subject at consent time.</param>
    /// <param name="proofOfConsent">Hash or reference to the consent form shown.</param>
    /// <param name="metadata">Additional metadata associated with the consent.</param>
    /// <param name="expiresAtUtc">Optional expiration timestamp.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="occurredAtUtc">
    /// When the data subject acted (UTC), supplied by the client application; <c>null</c> uses the
    /// current time of the injected <see cref="TimeProvider"/>. Orders acts on the same record: see
    /// the remarks for the client-clock warning.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the consent identifier of the key: the identifier of the newly created
    /// consent aggregate, or of the existing one when the key already has a record.
    /// </returns>
    ValueTask<Either<EncinaError, Guid>> GrantConsentAsync(
        string dataSubjectId,
        string purpose,
        string consentVersionId,
        string source,
        string grantedBy,
        string? ipAddress = null,
        string? proofOfConsent = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        DateTimeOffset? occurredAtUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws consent for a specific consent aggregate.
    /// </summary>
    /// <param name="consentId">The consent aggregate identifier.</param>
    /// <param name="withdrawnBy">Identifier of the actor withdrawing consent.</param>
    /// <param name="reason">Optional reason for withdrawal.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 7(3), withdrawal of consent must be as easy as giving consent.
    /// Downstream systems should stop processing data for this purpose.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        Guid consentId,
        string withdrawnBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews an existing consent with updated version and/or expiration.
    /// </summary>
    /// <param name="consentId">The consent aggregate identifier.</param>
    /// <param name="consentVersionId">The new consent version identifier.</param>
    /// <param name="renewedBy">Identifier of the actor renewing consent.</param>
    /// <param name="newExpiresAtUtc">Optional new expiration timestamp.</param>
    /// <param name="source">Optional updated consent collection source.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// The renewal is an act of consent timed by the injected <see cref="TimeProvider"/> and follows
    /// the act-ordering rule of <see cref="GrantConsentAsync"/> (#3771): if the consent's latest act
    /// is later (for example a grant dated ahead by a client clock), the renewal is recorded without
    /// changing the state.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RenewConsentAsync(
        Guid consentId,
        string consentVersionId,
        string renewedBy,
        DateTimeOffset? newExpiresAtUtc = null,
        string? source = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records fresh consent from a data subject after consent terms have changed.
    /// </summary>
    /// <param name="consentId">The consent aggregate identifier.</param>
    /// <param name="newConsentVersionId">The new consent version identifier.</param>
    /// <param name="source">The channel through which reconsent was collected.</param>
    /// <param name="grantedBy">Identifier of the actor recording the reconsent.</param>
    /// <param name="ipAddress">IP address of the data subject at reconsent time.</param>
    /// <param name="proofOfConsent">Hash or reference to the consent form shown.</param>
    /// <param name="metadata">Additional metadata associated with the reconsent.</param>
    /// <param name="expiresAtUtc">Optional new expiration timestamp.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// This operation is only valid when the consent status is <see cref="ConsentStatus.RequiresReconsent"/>.
    /// It reactivates the consent under the new terms with fresh GDPR Article 7(1) proof data.
    /// The reconsent is timed by the injected <see cref="TimeProvider"/> and follows the act-ordering
    /// rule of <see cref="GrantConsentAsync"/> (#3771): if the consent's latest act is later, it is
    /// recorded without changing the state. <see cref="GrantConsentAsync"/> with the current version
    /// is also accepted as the reconsent.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ProvideReconsentAsync(
        Guid consentId,
        string newConsentVersionId,
        string source,
        string grantedBy,
        string? ipAddress = null,
        string? proofOfConsent = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via ConsentReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a consent by its aggregate identifier.
    /// </summary>
    /// <param name="consentId">The consent aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the consent read model.</returns>
    ValueTask<Either<EncinaError, ConsentReadModel>> GetConsentAsync(
        Guid consentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the consent record for a data subject and specific purpose.
    /// </summary>
    /// <param name="dataSubjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to look up.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or <c>Some(readModel)</c> if found, <c>None</c> if no consent exists.
    /// </returns>
    ValueTask<Either<EncinaError, Option<ConsentReadModel>>> GetConsentBySubjectAndPurposeAsync(
        string dataSubjectId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all consent records for a data subject across all purposes.
    /// </summary>
    /// <param name="dataSubjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of consent read models, or an empty list if none exist.</returns>
    /// <remarks>
    /// Useful for building consent dashboards where data subjects can view and manage
    /// all their consent preferences.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ConsentReadModel>>> GetAllConsentsAsync(
        string dataSubjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a data subject has valid (active, non-expired) consent for a specific purpose.
    /// </summary>
    /// <param name="dataSubjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to check.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if valid consent exists, <c>false</c> otherwise.</returns>
    ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string dataSubjectId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a consent aggregate.
    /// </summary>
    /// <param name="consentId">The consent aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this consent,
    /// ordered chronologically. This provides a complete audit trail for GDPR Article 5(2)
    /// accountability requirements.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetConsentHistoryAsync(
        Guid consentId,
        CancellationToken cancellationToken = default);
}
