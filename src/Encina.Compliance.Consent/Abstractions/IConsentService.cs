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
///   <item><description><see cref="GrantConsentAsync"/> — Records initial consent grant</description></item>
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
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created consent aggregate.</returns>
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
