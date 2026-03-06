using System.Diagnostics;
using System.Security.Cryptography;

using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="ISubjectKeyProvider"/> using Marten's document store.
/// </summary>
/// <remarks>
/// <para>
/// Persists per-subject encryption keys as <see cref="SubjectKeyDocument"/> entities in PostgreSQL
/// via Marten's <see cref="IDocumentSession"/>. Each key version is stored as a separate document
/// with the ID convention <c>"subject:{subjectId}:v{version}"</c>.
/// </para>
/// <para>
/// <b>Recommended for production use.</b> Keys survive process restarts and benefit from
/// PostgreSQL's ACID guarantees. A computed index on <c>SubjectId</c> ensures efficient
/// lookups when querying all key versions for a given subject.
/// </para>
/// <para>
/// When a data subject exercises their right to be forgotten, all key documents for that
/// subject are hard-deleted from PostgreSQL, ensuring no key material remains in the database.
/// </para>
/// </remarks>
public sealed class PostgreSqlSubjectKeyProvider : ISubjectKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    private readonly IDocumentSession _session;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PostgreSqlSubjectKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSubjectKeyProvider"/> class.
    /// </summary>
    /// <param name="session">The Marten document session for PostgreSQL operations.</param>
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

        _session = session;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, byte[]>> GetOrCreateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            // Check if subject has been forgotten
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            // Check for existing active key
            var existingKeys = await _session.Query<SubjectKeyDocument>()
                .Where(d => d.SubjectId == subjectId && d.Status == SubjectKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (existingKeys.Count > 0)
            {
                // Return the highest version active key
                var activeKey = existingKeys.OrderByDescending(k => k.Version).First();
                return Right(activeKey.KeyMaterial);
            }

            // Create the first key for this subject
            var keyMaterial = new byte[DefaultKeySizeInBytes];
            RandomNumberGenerator.Fill(keyMaterial);
            var version = 1;
            var keyId = FormatKeyId(subjectId, version);
            var now = _timeProvider.GetUtcNow();

            var doc = new SubjectKeyDocument
            {
                Id = keyId,
                SubjectId = subjectId,
                KeyMaterial = keyMaterial,
                Version = version,
                Status = SubjectKeyStatus.Active,
                CreatedAtUtc = now
            };

            _session.Store(doc);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Created initial encryption key for subject {SubjectId}, version {Version}",
                subjectId,
                version);

            return Right(keyMaterial);
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
            // Check if subject has been forgotten
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            if (version.HasValue)
            {
                var keyId = FormatKeyId(subjectId, version.Value);
                var doc = await _session.LoadAsync<SubjectKeyDocument>(
                    keyId, cancellationToken).ConfigureAwait(false);

                if (doc is null)
                {
                    return Left(Security.Encryption.EncryptionErrors.KeyNotFound(keyId));
                }

                return Right(doc.KeyMaterial);
            }

            // Return active (latest) key
            var activeKeys = await _session.Query<SubjectKeyDocument>()
                .Where(d => d.SubjectId == subjectId && d.Status == SubjectKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (activeKeys.Count == 0)
            {
                return Left(Security.Encryption.EncryptionErrors.KeyNotFound(
                    FormatKeyId(subjectId, 1)));
            }

            var activeKey = activeKeys.OrderByDescending(k => k.Version).First();
            return Right(activeKey.KeyMaterial);
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

        using var activity = CryptoShreddingDiagnostics.StartForget(subjectId);
        var stopwatch = Stopwatch.GetTimestamp();

        try
        {
            // Check if already forgotten
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            // Find all key documents for this subject
            var allKeys = await _session.Query<SubjectKeyDocument>()
                .Where(d => d.SubjectId == subjectId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var keysDeleted = allKeys.Count;
            var now = _timeProvider.GetUtcNow();

            // Hard-delete all key documents
            foreach (var key in allKeys)
            {
                _session.Delete(key);
            }

            // Store a forgotten marker so we know this subject has been forgotten
            var marker = new SubjectForgottenMarker
            {
                Id = FormatForgottenMarkerId(subjectId),
                SubjectId = subjectId,
                ForgottenAtUtc = now,
                KeysDeleted = keysDeleted
            };

            _session.Store(marker);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            CryptoShreddingDiagnostics.ForgetTotal.Add(1);
            CryptoShreddingDiagnostics.RecordSuccess(activity);
            _logger.SubjectForgotten(subjectId, keysDeleted);

            var result = new CryptoShreddingResult
            {
                SubjectId = subjectId,
                KeysDeleted = keysDeleted,
                FieldsAffected = 0, // Field count is determined by the caller
                ShreddedAtUtc = now
            };

            return Right(result);
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
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
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            return Right(forgottenDoc is not null);
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

        using var activity = CryptoShreddingDiagnostics.StartKeyRotation(subjectId);

        try
        {
            // Check if subject has been forgotten
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
            }

            // Find the current active key
            var activeKeys = await _session.Query<SubjectKeyDocument>()
                .Where(d => d.SubjectId == subjectId && d.Status == SubjectKeyStatus.Active)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (activeKeys.Count == 0)
            {
                return Left(Security.Encryption.EncryptionErrors.KeyNotFound(
                    FormatKeyId(subjectId, 1)));
            }

            var oldDoc = activeKeys.OrderByDescending(k => k.Version).First();
            var oldKeyId = oldDoc.Id;
            var oldVersion = oldDoc.Version;

            // Mark old key as rotated
            oldDoc.Status = SubjectKeyStatus.Rotated;
            _session.Store(oldDoc);

            // Create new key version
            var newVersion = oldVersion + 1;
            var newKeyId = FormatKeyId(subjectId, newVersion);
            var newKeyMaterial = new byte[DefaultKeySizeInBytes];
            RandomNumberGenerator.Fill(newKeyMaterial);
            var now = _timeProvider.GetUtcNow();

            var newDoc = new SubjectKeyDocument
            {
                Id = newKeyId,
                SubjectId = subjectId,
                KeyMaterial = newKeyMaterial,
                Version = newVersion,
                Status = SubjectKeyStatus.Active,
                CreatedAtUtc = now
            };

            _session.Store(newDoc);
            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            CryptoShreddingDiagnostics.KeyRotationTotal.Add(1);
            CryptoShreddingDiagnostics.RecordSuccess(activity);
            _logger.KeyRotated(subjectId, oldVersion, newVersion);

            var result = new KeyRotationResult
            {
                SubjectId = subjectId,
                OldKeyId = oldKeyId,
                NewKeyId = newKeyId,
                OldVersion = oldVersion,
                NewVersion = newVersion,
                RotatedAtUtc = now
            };

            return Right(result);
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
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
            // Check if subject has been forgotten
            var forgottenDoc = await _session.LoadAsync<SubjectForgottenMarker>(
                FormatForgottenMarkerId(subjectId),
                cancellationToken).ConfigureAwait(false);

            if (forgottenDoc is not null)
            {
                var forgottenInfo = new SubjectEncryptionInfo
                {
                    SubjectId = subjectId,
                    Status = SubjectStatus.Forgotten,
                    ActiveKeyVersion = 0,
                    TotalKeyVersions = 0,
                    CreatedAtUtc = forgottenDoc.ForgottenAtUtc,
                    ForgottenAtUtc = forgottenDoc.ForgottenAtUtc
                };

                return Right(forgottenInfo);
            }

            // Find all keys for this subject
            var allKeys = await _session.Query<SubjectKeyDocument>()
                .Where(d => d.SubjectId == subjectId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (allKeys.Count == 0)
            {
                return Left(CryptoShreddingErrors.InvalidSubjectId(subjectId));
            }

            var activeKey = allKeys
                .Where(k => k.Status == SubjectKeyStatus.Active)
                .OrderByDescending(k => k.Version)
                .FirstOrDefault();

            var earliest = allKeys.OrderBy(k => k.CreatedAtUtc).First();

            var info = new SubjectEncryptionInfo
            {
                SubjectId = subjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = activeKey?.Version ?? 0,
                TotalKeyVersions = allKeys.Count,
                CreatedAtUtc = earliest.CreatedAtUtc
            };

            return Right(info);
        }
        catch (Exception ex)
        {
            return Left(CryptoShreddingErrors.KeyStoreError("GetSubjectInfo", ex));
        }
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
}
