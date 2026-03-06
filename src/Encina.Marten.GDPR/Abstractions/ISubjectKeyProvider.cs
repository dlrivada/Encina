using LanguageExt;

namespace Encina.Marten.GDPR.Abstractions;

/// <summary>
/// Manages per-subject encryption key lifecycle for crypto-shredding operations.
/// </summary>
/// <remarks>
/// <para>
/// Each data subject has one or more versioned encryption keys identified by the convention
/// <c>"subject:{subjectId}:v{version}"</c>. The provider handles creation, retrieval,
/// rotation, and deletion of these keys.
/// </para>
/// <para>
/// Key lifecycle:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>Creation</b>: <see cref="GetOrCreateSubjectKeyAsync"/> creates a new AES-256 key
/// on first use for a data subject.
/// </description></item>
/// <item><description>
/// <b>Retrieval</b>: <see cref="GetSubjectKeyAsync"/> retrieves key material for
/// encryption or decryption, optionally targeting a specific key version.
/// </description></item>
/// <item><description>
/// <b>Rotation</b>: <see cref="RotateSubjectKeyAsync"/> creates a new key version;
/// old versions remain available for decrypting existing events (forward-only encryption).
/// </description></item>
/// <item><description>
/// <b>Deletion</b>: <see cref="DeleteSubjectKeysAsync"/> removes ALL key versions,
/// implementing GDPR Article 17 ("right to be forgotten").
/// </description></item>
/// </list>
/// <para>
/// Built-in implementations include <c>InMemorySubjectKeyProvider</c> for testing and
/// <c>PostgreSqlSubjectKeyProvider</c> for production use with Marten's document store.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// </remarks>
public interface ISubjectKeyProvider
{
    /// <summary>
    /// Gets the existing active encryption key for a subject, or creates a new one if none exists.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;byte[]&gt;</c> containing the 256-bit key material on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the subject has been forgotten or key creation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the primary method used during event serialization. It is idempotent:
    /// calling it multiple times for the same subject returns the same active key.
    /// </para>
    /// <para>
    /// If the subject has been cryptographically forgotten (all keys deleted),
    /// returns <c>Left</c> with error code <c>crypto.subject_forgotten</c>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, byte[]>> GetOrCreateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the encryption key material for a specific subject and optional key version.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject.</param>
    /// <param name="version">
    /// The specific key version to retrieve. When <c>null</c>, the current active version
    /// is returned.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;byte[]&gt;</c> containing the key material on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the key is not found or the subject has been forgotten.
    /// </returns>
    /// <remarks>
    /// Used during event deserialization to retrieve the specific key version that encrypted
    /// each event. The key version is stored in the encrypted value's <c>kid</c> field
    /// (format: <c>"subject:{subjectId}:v{version}"</c>).
    /// </remarks>
    ValueTask<Either<EncinaError, byte[]>> GetSubjectKeyAsync(
        string subjectId,
        int? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes ALL encryption key versions for a data subject, implementing the right to be forgotten.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject to forget.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;CryptoShreddingResult&gt;</c> with deletion metrics on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if deletion fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the core crypto-shredding operation. After calling this method, all events
    /// encrypted with the subject's keys become permanently unreadable. The encrypted
    /// ciphertext remains in the immutable event store, but without the key material,
    /// it cannot be decrypted — satisfying GDPR Article 17 without modifying event history.
    /// </para>
    /// <para>
    /// Publishes a <see cref="SubjectForgottenEvent"/> notification after successful deletion.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, CryptoShreddingResult>> DeleteSubjectKeysAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a data subject has been cryptographically forgotten.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;true&gt;</c> if the subject's keys have been deleted (forgotten),
    /// <c>Right&lt;false&gt;</c> if the subject has active keys, or
    /// <c>Left&lt;EncinaError&gt;</c> if the check fails.
    /// </returns>
    /// <remarks>
    /// Used by projections and read models to determine whether to show placeholder values
    /// (e.g., <c>[REDACTED]</c>) instead of attempting decryption.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsSubjectForgottenAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the encryption key for a data subject, creating a new active version.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;KeyRotationResult&gt;</c> with rotation details on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the subject is forgotten or rotation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Creates a new key version (monotonically increasing). The previous key transitions
    /// to <see cref="SubjectKeyStatus.Rotated"/> status and remains available for decrypting
    /// events encrypted with that version (forward-only encryption).
    /// </para>
    /// <para>
    /// New events are encrypted with the latest key version. Existing events retain their
    /// original encryption and are decrypted with the key version stored in the
    /// <c>kid</c> field of the encrypted value.
    /// </para>
    /// <para>
    /// Publishes a <see cref="SubjectKeyRotatedEvent"/> notification after successful rotation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, KeyRotationResult>> RotateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves encryption status and key information for a data subject.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the data subject.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;SubjectEncryptionInfo&gt;</c> with the subject's encryption status on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the subject is not found or the query fails.
    /// </returns>
    /// <remarks>
    /// Provides administrative visibility into the encryption state: active key version,
    /// total key versions, forgotten status, and timestamps. Useful for compliance
    /// dashboards and audit reporting.
    /// </remarks>
    ValueTask<Either<EncinaError, SubjectEncryptionInfo>> GetSubjectInfoAsync(
        string subjectId,
        CancellationToken cancellationToken = default);
}
