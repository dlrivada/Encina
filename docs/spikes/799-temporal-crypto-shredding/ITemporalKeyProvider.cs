// ============================================================================
// DRAFT INTERFACE — Spike #799 output
// This file is a design artifact, NOT production code.
// Final implementation will live in src/Encina.Audit.Marten/
// ============================================================================

using LanguageExt;

namespace Encina.Audit.Marten.Abstractions;

/// <summary>
/// Manages time-period-based encryption keys for temporal crypto-shredding of audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="Encina.Marten.GDPR.Abstractions.ISubjectKeyProvider"/> which partitions
/// keys per data subject (for GDPR Art. 17), this provider partitions keys per time period
/// (for retention-based purging). Audit entries within the same time period share an encryption
/// key. When the retention period expires, the key is destroyed, rendering all entries in that
/// period permanently unreadable.
/// </para>
/// <para>
/// Key identification format: <c>"audit-temporal:{granularity}:{period}"</c>
/// </para>
/// <list type="bullet">
/// <item><description>Monthly: <c>"audit-temporal:monthly:2026-03"</c></description></item>
/// <item><description>Quarterly: <c>"audit-temporal:quarterly:2026-Q1"</c></description></item>
/// <item><description>Yearly: <c>"audit-temporal:yearly:2026"</c></description></item>
/// </list>
/// <para>
/// Both <c>ITemporalKeyProvider</c> and <c>ISubjectKeyProvider</c> can coexist. When both are
/// configured, PII fields are double-encrypted: first with the subject key, then with the
/// temporal key. Either key deletion renders the data unreadable.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// </remarks>
/// <seealso cref="TemporalKeyGranularity"/>
/// <seealso cref="TemporalKeyDocument"/>
/// <seealso cref="TemporalShreddingResult"/>
public interface ITemporalKeyProvider
{
    /// <summary>
    /// Gets or creates the encryption key for the time period containing the specified timestamp.
    /// </summary>
    /// <param name="timestampUtc">The UTC timestamp used to determine the time period.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;TemporalKeyInfo&gt;</c> containing the key material and period metadata on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the period has been shredded or key creation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the primary method used during audit entry recording. It is idempotent:
    /// calling it multiple times for timestamps within the same period returns the same key.
    /// </para>
    /// <para>
    /// If the period has been shredded (key deleted), returns <c>Left</c> with error code
    /// <c>temporal.period_shredded</c>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TemporalKeyInfo>> GetOrCreateKeyForTimestampAsync(
        DateTime timestampUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the encryption key for a specific period identifier.
    /// </summary>
    /// <param name="periodId">
    /// The period identifier (e.g., <c>"audit-temporal:monthly:2026-03"</c>).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;TemporalKeyInfo&gt;</c> containing the key material on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the period key is not found or has been shredded.
    /// </returns>
    /// <remarks>
    /// Used during audit entry deserialization to retrieve the key that encrypted each entry.
    /// The period identifier is stored alongside the encrypted data.
    /// </remarks>
    ValueTask<Either<EncinaError, TemporalKeyInfo>> GetKeyByPeriodIdAsync(
        string periodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys all temporal keys for periods that end before the specified date,
    /// implementing retention-based crypto-shredding.
    /// </summary>
    /// <param name="olderThanUtc">
    /// The cutoff date. All periods whose end date is before this value will have their keys deleted.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;TemporalShreddingResult&gt;</c> with shredding metrics on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the operation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the core temporal crypto-shredding operation. After calling this method,
    /// all audit entries in the affected periods become permanently unreadable. The encrypted
    /// ciphertext remains in the event store, but without the key material, it cannot be
    /// decrypted.
    /// </para>
    /// <para>
    /// Example: With monthly granularity and <paramref name="olderThanUtc"/> = 2026-04-01,
    /// keys for periods 2026-03, 2026-02, 2026-01, etc. are all destroyed.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TemporalShreddingResult>> ShredPeriodsOlderThanAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific time period has been shredded (key deleted).
    /// </summary>
    /// <param name="periodId">The period identifier to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;true&gt;</c> if the period's key has been deleted (shredded),
    /// <c>Right&lt;false&gt;</c> if the period has an active key, or
    /// <c>Left&lt;EncinaError&gt;</c> if the check fails.
    /// </returns>
    /// <remarks>
    /// Used by projections to determine whether to show <c>[SHREDDED]</c> placeholder values
    /// instead of attempting decryption.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsPeriodShreddedAsync(
        string periodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all temporal key periods with their status (active, expired, shredded).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;IReadOnlyList&lt;TemporalPeriodInfo&gt;&gt;</c> with all period metadata on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the query fails.
    /// </returns>
    /// <remarks>
    /// Provides administrative visibility into the temporal key lifecycle. Useful for compliance
    /// dashboards, retention policy verification, and operational monitoring.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<TemporalPeriodInfo>>> ListPeriodsAsync(
        CancellationToken cancellationToken = default);
}
