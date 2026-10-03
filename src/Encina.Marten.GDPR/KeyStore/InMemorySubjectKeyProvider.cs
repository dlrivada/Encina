using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Marten.GDPR;

/// <summary>
/// In-memory implementation of <see cref="ISubjectKeyProvider"/> for testing and development scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider is designed for:
/// <list type="bullet">
/// <item><description>Unit and integration testing</description></item>
/// <item><description>Development and local debugging</description></item>
/// <item><description>Prototyping crypto-shredding features</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Keys are stored in process memory and lost when the
/// process restarts. For production use, use <see cref="PostgreSqlSubjectKeyProvider"/>
/// which persists keys in Marten's PostgreSQL document store.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access
/// and <see cref="System.Threading.Lock"/> for per-subject synchronization. Creation, rotation and erasure
/// of one subject run under that subject's lock, so concurrent first writers all get the one stored key,
/// each rotation creates exactly one new version, and no key can be created after an erasure. Key material
/// is returned as a copy: erasure zeroes the stored arrays and must never alter a key a caller still holds.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var keyProvider = new InMemorySubjectKeyProvider(TimeProvider.System, logger);
///
/// // Create or retrieve a key for a subject
/// var keyResult = await keyProvider.GetOrCreateSubjectKeyAsync("user-42");
///
/// // Forget a subject (crypto-shredding)
/// var shreddingResult = await keyProvider.DeleteSubjectKeysAsync("user-42");
/// </code>
/// </example>
public sealed class InMemorySubjectKeyProvider : ISubjectKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    private readonly ConcurrentDictionary<string, SubjectState> _subjects = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemorySubjectKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySubjectKeyProvider"/> class.
    /// </summary>
    /// <param name="timeProvider">Provider for testable time-dependent logic.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public InMemorySubjectKeyProvider(
        TimeProvider timeProvider,
        ILogger<InMemorySubjectKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, SubjectEncryptionKey>> GetOrCreateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionKey>>(
                Left(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey")));
        }

        try
        {
            var state = _subjects.GetOrAdd(subjectId, static _ => new SubjectState());

            lock (state.SyncRoot)
            {
                if (state.IsForgotten)
                {
                    return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionKey>>(
                        Left(CryptoShreddingErrors.SubjectForgotten(subjectId)));
                }

                if (state.Keys.Count > 0)
                {
                    // Return the active (latest) key material with its version, read under the same lock.
                    // The caller gets a copy: erasure zeroes the stored array, which must never change a key
                    // a concurrent writer is still encrypting with (#1699).
                    var activeKey = state.Keys[^1];
                    return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionKey>>(
                        Right(new SubjectEncryptionKey { Version = activeKey.Version, KeyMaterial = CopyOf(activeKey.KeyMaterial) }));
                }

                // Create the first key for this subject
                var keyMaterial = new byte[DefaultKeySizeInBytes];
                RandomNumberGenerator.Fill(keyMaterial);
                var version = 1;
                var keyId = FormatKeyId(subjectId, version);
                var now = _timeProvider.GetUtcNow();

                state.Keys.Add(new SubjectKeyEntry(keyId, keyMaterial, version, SubjectKeyStatus.Active, now));
                state.CreatedAtUtc = now;

                // The data subject's own identifier is never logged (#1429, following #1314);
                // correlate via the key version instead.
                _logger.KeyCreated(version);

                return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionKey>>(
                    Right(new SubjectEncryptionKey { Version = version, KeyMaterial = CopyOf(keyMaterial) }));
            }
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionKey>>(
                Left(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey", ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, byte[]>> GetSubjectKeyAsync(
        string subjectId,
        int? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left(CryptoShreddingErrors.KeyStoreError("GetSubjectKey")));
        }

        if (!_subjects.TryGetValue(subjectId, out var state))
        {
            return ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left(Security.Encryption.EncryptionErrors.KeyNotFound(
                    FormatKeyId(subjectId, version ?? 1))));
        }

        lock (state.SyncRoot)
        {
            return ValueTask.FromResult(ReadKey(state, subjectId, version));
        }
    }

    /// <summary>
    /// Reads the requested key version, or the active (latest) one when <paramref name="version"/> is
    /// <c>null</c>, as a copy. The caller holds the subject's lock.
    /// </summary>
    private static Either<EncinaError, byte[]> ReadKey(SubjectState state, string subjectId, int? version)
    {
        if (state.IsForgotten)
        {
            return Left(CryptoShreddingErrors.SubjectForgotten(subjectId));
        }

        var entry = version.HasValue
            ? state.Keys.Find(k => k.Version == version.Value)
            : state.Keys.LastOrDefault();

        return entry is null
            ? Left(Security.Encryption.EncryptionErrors.KeyNotFound(FormatKeyId(subjectId, version ?? 1)))
            : Right(CopyOf(entry.KeyMaterial));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, CryptoShreddingResult>> DeleteSubjectKeysAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, CryptoShreddingResult>>(
                Left(CryptoShreddingErrors.KeyStoreError("DeleteSubjectKeys")));
        }

        using var activity = CryptoShreddingDiagnostics.StartForget();
        var stopwatch = Stopwatch.GetTimestamp();

        try
        {
            var state = _subjects.GetOrAdd(subjectId, static _ => new SubjectState());

            lock (state.SyncRoot)
            {
                if (state.IsForgotten)
                {
                    return ValueTask.FromResult<Either<EncinaError, CryptoShreddingResult>>(
                        Left(CryptoShreddingErrors.SubjectForgotten(subjectId)));
                }

                var keysDeleted = state.Keys.Count;
                var now = _timeProvider.GetUtcNow();

                // Clear all key material
                foreach (var key in state.Keys)
                {
                    CryptographicOperations.ZeroMemory(key.KeyMaterial);
                }

                state.Keys.Clear();
                state.IsForgotten = true;
                state.ForgottenAtUtc = now;

                CryptoShreddingDiagnostics.ForgetTotal.Add(1);
                CryptoShreddingDiagnostics.RecordSuccess(activity);
                _logger.SubjectForgotten(keysDeleted);

                var result = new CryptoShreddingResult
                {
                    SubjectId = subjectId,
                    KeysDeleted = keysDeleted,
                    FieldsAffected = 0, // Field count is determined by the caller
                    ShreddedAtUtc = now
                };

                return ValueTask.FromResult<Either<EncinaError, CryptoShreddingResult>>(Right(result));
            }
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
            return ValueTask.FromResult<Either<EncinaError, CryptoShreddingResult>>(
                Left(CryptoShreddingErrors.KeyStoreError("DeleteSubjectKeys", ex)));
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            CryptoShreddingDiagnostics.ForgetDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsSubjectForgottenAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(
                Left(CryptoShreddingErrors.KeyStoreError("IsSubjectForgotten")));
        }

        if (!_subjects.TryGetValue(subjectId, out var state))
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(Right(false));
        }

        lock (state.SyncRoot)
        {
            return ValueTask.FromResult<Either<EncinaError, bool>>(Right(state.IsForgotten));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, KeyRotationResult>> RotateSubjectKeyAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(
                Left(CryptoShreddingErrors.KeyStoreError("RotateSubjectKey")));
        }

        using var activity = CryptoShreddingDiagnostics.StartKeyRotation();

        try
        {
            if (!_subjects.TryGetValue(subjectId, out var state))
            {
                return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(
                    Left(Security.Encryption.EncryptionErrors.KeyNotFound(
                        FormatKeyId(subjectId, 1))));
            }

            lock (state.SyncRoot)
            {
                if (state.IsForgotten)
                {
                    return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(
                        Left(CryptoShreddingErrors.SubjectForgotten(subjectId)));
                }

                if (state.Keys.Count == 0)
                {
                    return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(
                        Left(Security.Encryption.EncryptionErrors.KeyNotFound(
                            FormatKeyId(subjectId, 1))));
                }

                var oldEntry = state.Keys[^1];
                var oldKeyId = oldEntry.KeyId;
                var oldVersion = oldEntry.Version;

                // Mark old key as rotated
                state.Keys[^1] = oldEntry with { Status = SubjectKeyStatus.Rotated };

                // Create new key version
                var newVersion = oldVersion + 1;
                var newKeyId = FormatKeyId(subjectId, newVersion);
                var newKeyMaterial = new byte[DefaultKeySizeInBytes];
                RandomNumberGenerator.Fill(newKeyMaterial);
                var now = _timeProvider.GetUtcNow();

                state.Keys.Add(new SubjectKeyEntry(newKeyId, newKeyMaterial, newVersion, SubjectKeyStatus.Active, now));

                CryptoShreddingDiagnostics.KeyRotationTotal.Add(1);
                CryptoShreddingDiagnostics.RecordSuccess(activity);
                _logger.KeyRotated(oldVersion, newVersion);

                var result = new KeyRotationResult
                {
                    SubjectId = subjectId,
                    OldKeyId = oldKeyId,
                    NewKeyId = newKeyId,
                    OldVersion = oldVersion,
                    NewVersion = newVersion,
                    RotatedAtUtc = now
                };

                return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(Right(result));
            }
        }
        catch (Exception ex)
        {
            CryptoShreddingDiagnostics.RecordFailed(activity, ex.Message);
            return ValueTask.FromResult<Either<EncinaError, KeyRotationResult>>(
                Left(CryptoShreddingErrors.KeyRotationFailed(subjectId, ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, SubjectEncryptionInfo>> GetSubjectInfoAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionInfo>>(
                Left(CryptoShreddingErrors.KeyStoreError("GetSubjectInfo")));
        }

        if (!_subjects.TryGetValue(subjectId, out var state))
        {
            return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionInfo>>(
                Left(CryptoShreddingErrors.InvalidSubjectId(subjectId)));
        }

        lock (state.SyncRoot)
        {
            var info = new SubjectEncryptionInfo
            {
                SubjectId = subjectId,
                Status = state.IsForgotten ? SubjectStatus.Forgotten : SubjectStatus.Active,
                ActiveKeyVersion = state.IsForgotten ? 0 : (state.Keys.Count > 0 ? state.Keys[^1].Version : 0),
                TotalKeyVersions = state.IsForgotten ? 0 : state.Keys.Count,
                CreatedAtUtc = state.CreatedAtUtc,
                ForgottenAtUtc = state.ForgottenAtUtc
            };

            return ValueTask.FromResult<Either<EncinaError, SubjectEncryptionInfo>>(Right(info));
        }
    }

    /// <summary>
    /// Gets the number of subjects tracked by the provider.
    /// </summary>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// </remarks>
    public int SubjectCount => _subjects.Count;

    /// <summary>
    /// Clears all subjects and resets state.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests; do not call it while other operations run.
    /// Each subject is removed before its key material is zeroed, so no later read can return a zeroed key.
    /// </remarks>
    public void Clear()
    {
        foreach (var subjectId in _subjects.Keys)
        {
            if (!_subjects.TryRemove(subjectId, out var state))
            {
                continue;
            }

            lock (state.SyncRoot)
            {
                foreach (var key in state.Keys)
                {
                    CryptographicOperations.ZeroMemory(key.KeyMaterial);
                }

                state.Keys.Clear();
            }
        }
    }

    /// <summary>
    /// Returns a copy of stored key material, so callers never share the array the provider zeroes on erasure.
    /// </summary>
    private static byte[] CopyOf(byte[] keyMaterial) => (byte[])keyMaterial.Clone();

    /// <summary>
    /// Formats a key identifier following the convention <c>"subject:{subjectId}:v{version}"</c>.
    /// </summary>
    private static string FormatKeyId(string subjectId, int version) =>
        $"subject:{subjectId}:v{version}";

    /// <summary>
    /// Per-subject state container protected by its own <see cref="System.Threading.Lock"/>.
    /// </summary>
    private sealed class SubjectState
    {
        public Lock SyncRoot { get; } = new();
        public List<SubjectKeyEntry> Keys { get; } = [];
        public bool IsForgotten { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? ForgottenAtUtc { get; set; }
    }

    /// <summary>
    /// Represents a single versioned encryption key for a subject.
    /// </summary>
    private sealed record SubjectKeyEntry(
        string KeyId,
        byte[] KeyMaterial,
        int Version,
        SubjectKeyStatus Status,
        DateTimeOffset CreatedAtUtc);
}
