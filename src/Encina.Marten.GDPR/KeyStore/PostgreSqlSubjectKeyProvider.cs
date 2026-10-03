using System.Buffers.Binary;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using JasperFx;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging;

using Npgsql;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="ISubjectKeyProvider"/> using Marten's document store.
/// </summary>
/// <remarks>
/// <para>
/// Persists per-subject encryption keys as <see cref="SubjectKeyDocument"/> entities in PostgreSQL
/// through Marten. Each key version is stored as a separate document with the ID convention
/// <c>"subject:{subjectId}:v{version}"</c>.
/// </para>
/// <para>
/// <b>Recommended for production use.</b> Keys survive process restarts and benefit from
/// PostgreSQL's ACID guarantees. A computed index on <c>SubjectId</c> ensures efficient
/// lookups when querying all key versions for a given subject.
/// </para>
/// <para>
/// <b>Concurrency.</b> The injected <see cref="IDocumentSession"/> only identifies the document store and
/// the tenant: every operation opens its own short-lived session, so the provider never flushes or
/// poisons the caller's unit of work and is safe to call concurrently. Key creation, rotation and erasure
/// for one subject run in one transaction that first takes a PostgreSQL transaction-scoped advisory lock
/// keyed by the tenant and the subject (<c>pg_advisory_xact_lock</c>), so they are serialized: the
/// forgotten-marker check happens under that lock, and no key can be created after an erasure commits.
/// Key documents are written with insert semantics; if a key version already exists (a writer that did not
/// take the lock), the stored winner is reloaded and returned, so every caller gets the stored key (#1699).
/// </para>
/// <para>
/// When a data subject exercises their right to be forgotten, all key documents for that
/// subject are hard-deleted from PostgreSQL, ensuring no key material remains in the database.
/// Erasure is idempotent: a repeated call deletes any key document that is still present, writes the
/// forgotten marker if it is missing, and returns <c>crypto.subject_forgotten</c> when the subject had
/// already been forgotten.
/// </para>
/// </remarks>
public sealed class PostgreSqlSubjectKeyProvider : ISubjectKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    /// <summary>
    /// Prefix hashed into every advisory lock key, so the provider's locks do not collide with the
    /// application's own advisory locks.
    /// </summary>
    private const string LockKeyNamespace = "encina:crypto-shredding:subject-key";

    private const string AdvisoryLockSql = "SELECT pg_advisory_xact_lock(@lockKey)";

    private readonly IDocumentStore _store;
    private readonly string _tenantId;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PostgreSqlSubjectKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSubjectKeyProvider"/> class.
    /// </summary>
    /// <param name="session">
    /// A Marten document session. Only its <see cref="IQuerySession.DocumentStore"/> and
    /// <see cref="IQuerySession.TenantId"/> are used: every operation opens its own session for that tenant.
    /// </param>
    /// <param name="timeProvider">Provider for testable time-dependent logic.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public PostgreSqlSubjectKeyProvider(
        IDocumentSession session,
        TimeProvider timeProvider,
        ILogger<PostgreSqlSubjectKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _store = session.DocumentStore;
        _tenantId = session.TenantId;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SubjectEncryptionKey>> GetOrCreateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            // Fast path without the lock: a forgotten subject or an existing active key needs no write.
            var existing = await ReadKeyStateAsync(subjectId, cancellationToken).ConfigureAwait(false);

            return existing.IsForgotten || existing.ActiveKey is not null
                ? ToSubjectKey(existing, subjectId)
                : await CreateFirstKeyAsync(subjectId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, byte[]>> GetSubjectKeyAsync(
        string subjectId,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            if (!version.HasValue)
            {
                var state = await ReadKeyStateAsync(subjectId, cancellationToken).ConfigureAwait(false);
                return ToSubjectKey(state, subjectId).Map(key => key.KeyMaterial);
            }

            await using var session = _store.QuerySession(_tenantId);

            if (await IsForgottenAsync(session, subjectId, cancellationToken).ConfigureAwait(false))
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            var keyId = FormatKeyId(subjectId, version.Value);
            var doc = await session.LoadAsync<SubjectKeyDocument>(keyId, cancellationToken).ConfigureAwait(false);

            return doc is null
                ? Left(Security.Encryption.EncryptionErrors.KeyNotFound(keyId))
                : Right(doc.KeyMaterial);
        }
        catch (Exception ex)
        {
            return Left(CryptoShreddingErrors.KeyStoreError("GetSubjectKey", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, CryptoShreddingResult>> DeleteSubjectKeysAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        using var activity = CryptoShreddingDiagnostics.StartForget();
        var stopwatch = Stopwatch.GetTimestamp();

        try
        {
            return await EraseUnderLockAsync(subjectId, activity, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Only the exception type reaches the trace: a database message can carry the key id,
            // which embeds the data subject's identifier.
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.GetType().Name);
            return Left(CryptoShreddingErrors.KeyStoreError("DeleteSubjectKeys", ex));
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            CryptoShreddingDiagnostics.ForgetDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsSubjectForgottenAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            await using var session = _store.QuerySession(_tenantId);
            return Right(await IsForgottenAsync(session, subjectId, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return Left(CryptoShreddingErrors.KeyStoreError("IsSubjectForgotten", ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, KeyRotationResult>> RotateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        using var activity = CryptoShreddingDiagnostics.StartKeyRotation();

        try
        {
            return await RotateUnderLockAsync(subjectId, activity, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.GetType().Name);
            return Left(CryptoShreddingErrors.KeyRotationFailed(subjectId, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SubjectEncryptionInfo>> GetSubjectInfoAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            await using var session = _store.QuerySession(_tenantId);

            var forgottenDoc = await session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                return Right(new SubjectEncryptionInfo
                {
                    SubjectId = subjectId,
                    Status = SubjectStatus.Forgotten,
                    ActiveKeyVersion = 0,
                    TotalKeyVersions = 0,
                    CreatedAtUtc = forgottenDoc.ForgottenAtUtc,
                    ForgottenAtUtc = forgottenDoc.ForgottenAtUtc
                });
            }

            var allKeys = await QueryAllKeysAsync(session, subjectId, cancellationToken).ConfigureAwait(false);

            return allKeys.Count == 0
                ? Left(CryptoShreddingErrors.InvalidSubjectId(subjectId))
                : Right(BuildActiveInfo(subjectId, allKeys));
        }
        catch (Exception ex)
        {
            return Left(CryptoShreddingErrors.KeyStoreError("GetSubjectInfo", ex));
        }
    }

    /// <summary>
    /// Computes the 64-bit key of the transaction-scoped advisory lock that serializes key creation,
    /// rotation and erasure for one subject of one tenant.
    /// </summary>
    /// <remarks>
    /// The key is the first 8 bytes of a SHA-256 hash, so it is stable across processes and machines
    /// (unlike <see cref="string.GetHashCode()"/>). Two subjects that share a key only serialize each other.
    /// </remarks>
    internal static long ComputeSubjectLockKey(string tenantId, string subjectId)
    {
        var input = Encoding.UTF8.GetBytes($"{LockKeyNamespace}\n{tenantId}\n{subjectId}");
        var hash = SHA256.HashData(input);
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

    /// <summary>
    /// Determines whether an exception (or one of its inner exceptions) reports that a document with the
    /// same id already exists: Marten's <see cref="DocumentAlreadyExistsException"/> or a PostgreSQL
    /// unique violation (SQLSTATE 23505).
    /// </summary>
    internal static bool IsDuplicateKeyConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DocumentAlreadyExistsException
                or PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates version 1 of the subject's key under the subject lock, or returns the key a concurrent
    /// caller stored first.
    /// </summary>
    private async Task<Either<EncinaError, SubjectEncryptionKey>> CreateFirstKeyAsync(
        string subjectId,
        CancellationToken cancellationToken)
    {
        await using (var session = await OpenLockedSessionAsync(subjectId, cancellationToken).ConfigureAwait(false))
        {
            var state = await ReadKeyStateAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
            if (state.IsForgotten || state.ActiveKey is not null)
            {
                return ToSubjectKey(state, subjectId);
            }

            var doc = NewKeyDocument(subjectId, version: 1);
            session.Insert(doc);

            if (await TrySaveAsync(session, cancellationToken).ConfigureAwait(false))
            {
                _logger.KeyCreated(doc.Version);
                return Right(new SubjectEncryptionKey { Version = doc.Version, KeyMaterial = doc.KeyMaterial });
            }
        }

        // A writer that did not take the lock stored version 1 first: return its key.
        _logger.ConcurrentKeyWriteResolved("GetOrCreateSubjectKey", 1);
        var winner = await ReadKeyStateAsync(subjectId, cancellationToken).ConfigureAwait(false);
        return winner.IsForgotten || winner.ActiveKey is not null
            ? ToSubjectKey(winner, subjectId)
            : Left(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey"));
    }

    /// <summary>
    /// Rotates the active key under the subject lock: every active version becomes rotated and the next
    /// version is inserted, in one transaction.
    /// </summary>
    private async Task<Either<EncinaError, KeyRotationResult>> RotateUnderLockAsync(
        string subjectId,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        SubjectKeyDocument newDoc;
        string oldKeyId;
        int oldVersion;

        await using (var session = await OpenLockedSessionAsync(subjectId, cancellationToken).ConfigureAwait(false))
        {
            if (await IsForgottenAsync(session, subjectId, cancellationToken).ConfigureAwait(false))
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            var activeKeys = await QueryActiveKeysAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
            if (activeKeys.Count == 0)
            {
                return Left(Security.Encryption.EncryptionErrors.KeyNotFound(FormatKeyId(subjectId, 1)));
            }

            var oldDoc = activeKeys.MaxBy(k => k.Version)!;
            oldKeyId = oldDoc.Id;
            oldVersion = oldDoc.Version;

            foreach (var activeKey in activeKeys)
            {
                activeKey.Status = SubjectKeyStatus.Rotated;
                session.Update(activeKey);
            }

            newDoc = NewKeyDocument(subjectId, oldVersion + 1);
            session.Insert(newDoc);

            if (!await TrySaveAsync(session, cancellationToken).ConfigureAwait(false))
            {
                return await ResolveRotationConflictAsync(subjectId, oldKeyId, oldVersion, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        CryptoShreddingDiagnostics.KeyRotationTotal.Add(1);
        CryptoShreddingDiagnostics.RecordSuccess(activity);
        _logger.KeyRotated(oldVersion, newDoc.Version);

        return Right(new KeyRotationResult
        {
            SubjectId = subjectId,
            OldKeyId = oldKeyId,
            NewKeyId = newDoc.Id,
            OldVersion = oldVersion,
            NewVersion = newDoc.Version,
            RotatedAtUtc = newDoc.CreatedAtUtc
        });
    }

    /// <summary>
    /// A writer that did not take the lock stored the next version first: report the stored winner.
    /// </summary>
    private async Task<Either<EncinaError, KeyRotationResult>> ResolveRotationConflictAsync(
        string subjectId,
        string oldKeyId,
        int oldVersion,
        CancellationToken cancellationToken)
    {
        _logger.ConcurrentKeyWriteResolved("RotateSubjectKey", oldVersion + 1);

        var winner = await ReadKeyStateAsync(subjectId, cancellationToken).ConfigureAwait(false);
        if (winner.IsForgotten)
        {
            return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
        }

        return winner.ActiveKey is { } active && active.Version > oldVersion
            ? Right(new KeyRotationResult
            {
                SubjectId = subjectId,
                OldKeyId = oldKeyId,
                NewKeyId = active.Id,
                OldVersion = oldVersion,
                NewVersion = active.Version,
                RotatedAtUtc = active.CreatedAtUtc
            })
            : Left(CryptoShreddingErrors.KeyRotationFailed(subjectId));
    }

    /// <summary>
    /// Deletes every key document of the subject and writes the forgotten marker if it is missing, under
    /// the subject lock and in one transaction.
    /// </summary>
    private async Task<Either<EncinaError, CryptoShreddingResult>> EraseUnderLockAsync(
        string subjectId,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        await using var session = await OpenLockedSessionAsync(subjectId, cancellationToken).ConfigureAwait(false);

        var alreadyForgotten = await IsForgottenAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
        var allKeys = await QueryAllKeysAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
        var keysDeleted = allKeys.Count;
        var now = _timeProvider.GetUtcNow();

        foreach (var key in allKeys)
        {
            session.Delete(key);
        }

        if (!alreadyForgotten)
        {
            session.Store(new SubjectForgottenMarker
            {
                Id = FormatForgottenMarkerId(subjectId),
                SubjectId = subjectId,
                ForgottenAtUtc = now,
                KeysDeleted = keysDeleted
            });
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (alreadyForgotten)
        {
            // Idempotent erasure: key documents left behind are deleted, the outcome stays "already forgotten".
            if (keysDeleted > 0)
            {
                _logger.LeftoverKeysErased(keysDeleted);
            }

            return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
        }

        CryptoShreddingDiagnostics.ForgetTotal.Add(1);
        CryptoShreddingDiagnostics.RecordSuccess(activity);
        _logger.SubjectForgotten(keysDeleted);

        return Right(new CryptoShreddingResult
        {
            SubjectId = subjectId,
            KeysDeleted = keysDeleted,
            FieldsAffected = 0, // Field count is determined by the caller
            ShreddedAtUtc = now
        });
    }

    /// <summary>
    /// Opens a session for the tenant, starts its transaction and takes the subject's transaction-scoped
    /// advisory lock. Committing or disposing the session releases the lock.
    /// </summary>
    private async Task<IDocumentSession> OpenLockedSessionAsync(string subjectId, CancellationToken cancellationToken)
    {
        var session = _store.LightweightSession(_tenantId, IsolationLevel.ReadCommitted);
        try
        {
            await session.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            using var command = new NpgsqlCommand(AdvisoryLockSql);
            command.Parameters.AddWithValue("lockKey", ComputeSubjectLockKey(_tenantId, subjectId));
            await session.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Saves the session's changes; <c>false</c> when an insert hit an existing document id.
    /// </summary>
    private static async Task<bool> TrySaveAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        try
        {
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (IsDuplicateKeyConflict(ex))
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the subject's forgotten marker and active key in a fresh query session (no lock).
    /// </summary>
    private async Task<SubjectKeyState> ReadKeyStateAsync(string subjectId, CancellationToken cancellationToken)
    {
        await using var session = _store.QuerySession(_tenantId);
        return await ReadKeyStateAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SubjectKeyState> ReadKeyStateAsync(
        IQuerySession session,
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (await IsForgottenAsync(session, subjectId, cancellationToken).ConfigureAwait(false))
        {
            return new SubjectKeyState(IsForgotten: true, ActiveKey: null);
        }

        var activeKeys = await QueryActiveKeysAsync(session, subjectId, cancellationToken).ConfigureAwait(false);
        return new SubjectKeyState(IsForgotten: false, ActiveKey: activeKeys.MaxBy(k => k.Version));
    }

    private static async Task<bool> IsForgottenAsync(
        IQuerySession session,
        string subjectId,
        CancellationToken cancellationToken)
    {
        var marker = await session.LoadAsync<SubjectForgottenMarker>(
            FormatForgottenMarkerId(subjectId),
            cancellationToken).ConfigureAwait(false);

        return marker is not null;
    }

    private static Task<IReadOnlyList<SubjectKeyDocument>> QueryActiveKeysAsync(
        IQuerySession session,
        string subjectId,
        CancellationToken cancellationToken) =>
        session.Query<SubjectKeyDocument>()
            .Where(d => d.SubjectId == subjectId && d.Status == SubjectKeyStatus.Active)
            .ToListAsync(cancellationToken);

    private static Task<IReadOnlyList<SubjectKeyDocument>> QueryAllKeysAsync(
        IQuerySession session,
        string subjectId,
        CancellationToken cancellationToken) =>
        session.Query<SubjectKeyDocument>()
            .Where(d => d.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Maps a key state to the result of a key read: forgotten, the active key (material and version from
    /// the same document), or key not found.
    /// </summary>
    private static Either<EncinaError, SubjectEncryptionKey> ToSubjectKey(SubjectKeyState state, string subjectId)
    {
        if (state.IsForgotten)
        {
            return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
        }

        return state.ActiveKey is { } active
            ? Right(new SubjectEncryptionKey { Version = active.Version, KeyMaterial = active.KeyMaterial })
            : Left(Security.Encryption.EncryptionErrors.KeyNotFound(FormatKeyId(subjectId, 1)));
    }

    private static SubjectEncryptionInfo BuildActiveInfo(string subjectId, IReadOnlyList<SubjectKeyDocument> allKeys)
    {
        var activeKey = allKeys
            .Where(k => k.Status == SubjectKeyStatus.Active)
            .MaxBy(k => k.Version);

        return new SubjectEncryptionInfo
        {
            SubjectId = subjectId,
            Status = SubjectStatus.Active,
            ActiveKeyVersion = activeKey?.Version ?? 0,
            TotalKeyVersions = allKeys.Count,
            CreatedAtUtc = allKeys.Min(k => k.CreatedAtUtc)
        };
    }

    private SubjectKeyDocument NewKeyDocument(string subjectId, int version)
    {
        var keyMaterial = new byte[DefaultKeySizeInBytes];
        RandomNumberGenerator.Fill(keyMaterial);

        return new SubjectKeyDocument
        {
            Id = FormatKeyId(subjectId, version),
            SubjectId = subjectId,
            KeyMaterial = keyMaterial,
            Version = version,
            Status = SubjectKeyStatus.Active,
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };
    }

    /// <summary>
    /// Formats a key identifier following the convention <c>"subject:{subjectId}:v{version}"</c>.
    /// </summary>
    private static string FormatKeyId(string subjectId, int version) =>
        $"subject:{subjectId}:v{version}";

    /// <summary>
    /// Formats the forgotten marker document ID.
    /// </summary>
    private static string FormatForgottenMarkerId(string subjectId) =>
        $"forgotten:{subjectId}";

    /// <summary>
    /// The subject's forgotten state and its highest active key, read together.
    /// </summary>
    private readonly record struct SubjectKeyState(bool IsForgotten, SubjectKeyDocument? ActiveKey);
}
