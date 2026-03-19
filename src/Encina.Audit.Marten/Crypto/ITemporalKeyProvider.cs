using LanguageExt;

namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Manages time-partitioned encryption key lifecycle for temporal crypto-shredding of audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>ISubjectKeyProvider</c> (which manages per-user keys for GDPR Art. 17),
/// <c>ITemporalKeyProvider</c> manages per-time-period keys for compliance-grade data retention.
/// Keys are partitioned by time period (monthly, quarterly, or yearly) and destroyed in bulk
/// when the retention period expires, achieving crypto-shredding of all audit entries in
/// the affected periods.
/// </para>
/// <para>
/// Key lifecycle:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>Creation</b>: <see cref="GetOrCreateKeyAsync"/> creates a new AES-256 key
/// on first use for a time period.
/// </description></item>
/// <item><description>
/// <b>Retrieval</b>: <see cref="GetKeyAsync"/> retrieves key material for
/// encryption or decryption, optionally targeting a specific key version.
/// </description></item>
/// <item><description>
/// <b>Destruction</b>: <see cref="DestroyKeysBeforeAsync"/> removes ALL key versions
/// for periods older than a cutoff date, implementing temporal crypto-shredding.
/// </description></item>
/// </list>
/// <para>
/// Built-in implementations include <see cref="InMemoryTemporalKeyProvider"/> for testing and
/// <see cref="MartenTemporalKeyProvider"/> for production use with Marten's document store.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <c>Either&lt;EncinaError, T&gt;</c> to represent success or failure without exceptions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Encrypt a new audit entry
/// var period = TemporalPeriodHelper.GetPeriod(entry.TimestampUtc, TemporalKeyGranularity.Monthly);
/// var keyResult = await keyProvider.GetOrCreateKeyAsync(period, cancellationToken);
///
/// // Later, crypto-shred entries older than 7 years
/// var cutoff = DateTime.UtcNow.AddDays(-2555);
/// var destroyResult = await keyProvider.DestroyKeysBeforeAsync(cutoff, cancellationToken);
/// </code>
/// </example>
public interface ITemporalKeyProvider
{
    /// <summary>
    /// Gets the existing active encryption key for a time period, or creates a new one if none exists.
    /// </summary>
    /// <param name="period">The time period identifier (e.g., <c>"2026-03"</c>, <c>"2026-Q1"</c>, <c>"2026"</c>).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;TemporalKeyInfo&gt;</c> containing the key information on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the period has been destroyed or key creation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the primary method used during audit entry recording. It is idempotent:
    /// calling it multiple times for the same period returns the same active key.
    /// </para>
    /// <para>
    /// If the period's keys have been destroyed (crypto-shredded), returns <c>Left</c>
    /// with error code <c>audit.marten.key_not_found</c>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TemporalKeyInfo>> GetOrCreateKeyAsync(
        string period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the encryption key for a specific time period and optional key version.
    /// </summary>
    /// <param name="period">The time period identifier.</param>
    /// <param name="version">
    /// The specific key version to retrieve. When <c>null</c>, the current active version
    /// is returned.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;TemporalKeyInfo&gt;</c> containing the key information on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the key is not found or the period has been destroyed.
    /// </returns>
    /// <remarks>
    /// Used during audit entry decryption (in projections) to retrieve the specific key version
    /// that encrypted each entry. The key version is embedded in the encrypted field's key identifier
    /// (format: <c>"temporal:{period}:v{version}"</c>).
    /// </remarks>
    ValueTask<Either<EncinaError, TemporalKeyInfo>> GetKeyAsync(
        string period,
        int? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys ALL encryption key versions for time periods older than the specified cutoff date.
    /// </summary>
    /// <param name="olderThanUtc">
    /// Periods that ended before this date will have their keys destroyed.
    /// </param>
    /// <param name="granularity">
    /// The temporal granularity to use when determining which periods are affected.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;int&gt;</c> with the number of periods whose keys were destroyed, or
    /// <c>Left&lt;EncinaError&gt;</c> if the destruction fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the core crypto-shredding operation for audit trails. After calling this method,
    /// all audit entries encrypted with the destroyed periods' keys become permanently unreadable.
    /// The encrypted events remain in the immutable event store, but without the key material,
    /// PII fields cannot be decrypted — satisfying GDPR data minimization (Art. 5(1)(e))
    /// without modifying event history (preserving SOX/NIS2 integrity).
    /// </para>
    /// <para>
    /// Destroyed periods are tracked via <see cref="TemporalKeyDestroyedMarker"/> documents
    /// to prevent accidental re-creation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, int>> DestroyKeysBeforeAsync(
        DateTime olderThanUtc,
        TemporalKeyGranularity granularity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a time period's encryption keys have been destroyed.
    /// </summary>
    /// <param name="period">The time period identifier to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;true&gt;</c> if the period's keys have been destroyed,
    /// <c>Right&lt;false&gt;</c> if the period has active keys or has never existed, or
    /// <c>Left&lt;EncinaError&gt;</c> if the check fails.
    /// </returns>
    /// <remarks>
    /// Used by projections and read models to determine whether to show placeholder values
    /// (e.g., <c>[SHREDDED]</c>) instead of attempting decryption.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsKeyDestroyedAsync(
        string period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves information about all active (non-destroyed) temporal key periods.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;IReadOnlyList&lt;TemporalKeyInfo&gt;&gt;</c> with all active keys, or
    /// <c>Left&lt;EncinaError&gt;</c> if the query fails.
    /// </returns>
    /// <remarks>
    /// Useful for health checks and administrative dashboards to monitor the temporal key
    /// landscape: how many periods have active keys, when were they created, etc.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>> GetActiveKeysAsync(
        CancellationToken cancellationToken = default);
}
