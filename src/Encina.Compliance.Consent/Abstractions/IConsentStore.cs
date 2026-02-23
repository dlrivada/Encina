using LanguageExt;

namespace Encina.Compliance.Consent;

/// <summary>
/// Store for managing consent records across their lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The consent store provides CRUD operations for consent records, supporting the full
/// consent lifecycle: recording new consent, querying existing consent, validating
/// consent status, and processing withdrawals.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store consent records in-memory (for development/testing), in a
/// database (for production), or in any other suitable backing store. All 13 database
/// providers are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record new consent
/// var consent = new ConsentRecord
/// {
///     Id = Guid.NewGuid(),
///     SubjectId = "user-123",
///     Purpose = ConsentPurposes.Marketing,
///     Status = ConsentStatus.Active,
///     ConsentVersionId = "marketing-v2",
///     GivenAtUtc = DateTimeOffset.UtcNow,
///     Source = "web-form",
///     Metadata = new Dictionary&lt;string, object?&gt;()
/// };
///
/// var result = await consentStore.RecordConsentAsync(consent, cancellationToken);
/// </code>
/// </example>
public interface IConsentStore
{
    /// <summary>
    /// Records a new consent given by a data subject.
    /// </summary>
    /// <param name="consent">The consent record to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the consent
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current consent record for a data subject and specific purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(record)</c> if a consent record exists for the given subject and purpose,
    /// <c>None</c> if no consent exists, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all consent records for a data subject across all purposes.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all consent records for the subject, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no consents exist.
    /// </returns>
    /// <remarks>
    /// This method is useful for building consent dashboards where data subjects can
    /// view and manage all their consent preferences.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws consent for a data subject and specific purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to withdraw consent for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the consent
    /// could not be withdrawn (e.g., no active consent found).
    /// </returns>
    /// <remarks>
    /// Article 7(3) requires that withdrawal of consent must be as easy as giving consent.
    /// This method sets the consent status to <see cref="ConsentStatus.Withdrawn"/> and
    /// records the withdrawal timestamp.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a data subject has valid (active, non-expired) consent for a specific purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if valid consent exists, <c>false</c> otherwise,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// This is a convenience method for quick consent checks in the pipeline behavior.
    /// For detailed validation results, use <see cref="IConsentValidator.ValidateAsync"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records multiple consent records in a single batch operation.
    /// </summary>
    /// <param name="consents">The consent records to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="BulkOperationResult"/> with per-item success/failure counts,
    /// or an <see cref="EncinaError"/> if the batch operation itself failed catastrophically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each consent record is processed independently. A failure in one record does not
    /// prevent the remaining records from being processed. Individual failures are captured
    /// in <see cref="BulkOperationResult.Errors"/>.
    /// </para>
    /// <para>
    /// Domain events (<see cref="ConsentGrantedEvent"/>) are published for each successfully
    /// recorded consent when an <see cref="IEncina"/> mediator is available.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var consents = new[]
    /// {
    ///     new ConsentRecord { SubjectId = "user-1", Purpose = "marketing", ... },
    ///     new ConsentRecord { SubjectId = "user-2", Purpose = "analytics", ... }
    /// };
    /// var result = await store.BulkRecordConsentAsync(consents, cancellationToken);
    /// </code>
    /// </example>
    ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws consent for multiple purposes for a single data subject in a single batch operation.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purposes">The processing purposes to withdraw consent for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="BulkOperationResult"/> with per-purpose success/failure counts,
    /// or an <see cref="EncinaError"/> if the batch operation itself failed catastrophically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each purpose is withdrawn independently. If consent for a particular purpose does not
    /// exist or was already withdrawn, it is recorded as a failure in
    /// <see cref="BulkOperationResult.Errors"/>.
    /// </para>
    /// <para>
    /// Domain events (<see cref="ConsentWithdrawnEvent"/>) are published for each successfully
    /// withdrawn consent when an <see cref="IEncina"/> mediator is available.
    /// </para>
    /// <para>
    /// Per Article 7(3), withdrawal of consent must be as easy as giving consent. This bulk
    /// method supports scenarios where a data subject revokes all consent at once (e.g.,
    /// account deletion or "withdraw all" dashboard action).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default);
}
