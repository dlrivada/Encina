using System.Collections.Concurrent;
using System.Security.Cryptography;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IKeyProvider"/> for testing and development.
/// </summary>
/// <remarks>
/// <para>
/// Keys are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> and are lost
/// when the process terminates. For production use, implement <see cref="IKeyProvider"/>
/// backed by a persistent store (Azure Key Vault, AWS KMS, HSM, etc.).
/// </para>
/// <para>
/// Generates 256-bit (32-byte) cryptographic keys using <see cref="RandomNumberGenerator"/>
/// for secure random number generation.
/// </para>
/// </remarks>
public sealed class InMemoryKeyProvider : IKeyProvider
{
    private const int KeySizeBytes = 32; // 256 bits

    private readonly ConcurrentDictionary<string, (byte[] Key, KeyInfo Info)> _keys = new();
    private volatile string? _activeKeyId;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryKeyProvider"/> and generates an initial active key.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for testable timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public InMemoryKeyProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        var initialKeyId = GenerateKeyId();
        var keyBytes = GenerateKeyBytes();
        var info = new KeyInfo
        {
            KeyId = initialKeyId,
            Algorithm = PseudonymizationAlgorithm.Aes256Gcm,
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            IsActive = true
        };
        _keys[initialKeyId] = (keyBytes, info);
        _activeKeyId = initialKeyId;
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        if (_keys.TryGetValue(keyId, out var entry))
        {
            return ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Right<EncinaError, byte[]>(entry.Key));
        }

        return ValueTask.FromResult<Either<EncinaError, byte[]>>(
            Left<EncinaError, byte[]>(AnonymizationErrors.KeyNotFound(keyId)));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, KeyInfo>> RotateKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        if (!_keys.TryGetValue(keyId, out var oldEntry))
        {
            return ValueTask.FromResult<Either<EncinaError, KeyInfo>>(
                Left<EncinaError, KeyInfo>(AnonymizationErrors.KeyNotFound(keyId)));
        }

        // Mark old key as inactive
        var deactivatedInfo = oldEntry.Info with { IsActive = false };
        _keys[keyId] = (oldEntry.Key, deactivatedInfo);

        // Generate new key
        var newKeyId = GenerateKeyId();
        var newKeyBytes = GenerateKeyBytes();
        var newInfo = new KeyInfo
        {
            KeyId = newKeyId,
            Algorithm = oldEntry.Info.Algorithm,
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            IsActive = true
        };
        _keys[newKeyId] = (newKeyBytes, newInfo);
        _activeKeyId = newKeyId;

        return ValueTask.FromResult<Either<EncinaError, KeyInfo>>(
            Right<EncinaError, KeyInfo>(newInfo));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, string>> GetActiveKeyIdAsync(
        CancellationToken cancellationToken = default)
    {
        var activeId = _activeKeyId;
        if (activeId is null)
        {
            return ValueTask.FromResult<Either<EncinaError, string>>(
                Left<EncinaError, string>(AnonymizationErrors.NoActiveKey()));
        }

        return ValueTask.FromResult<Either<EncinaError, string>>(
            Right<EncinaError, string>(activeId));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<KeyInfo>>> ListKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = _keys.Values
            .Select(e => e.Info)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<KeyInfo>>>(
            Right<EncinaError, IReadOnlyList<KeyInfo>>(keys));
    }

    /// <summary>
    /// Gets the total number of keys in the store (for testing).
    /// </summary>
    public int Count => _keys.Count;

    /// <summary>
    /// Clears all keys from the store (for testing).
    /// </summary>
    public void Clear()
    {
        _keys.Clear();
        _activeKeyId = null;
    }

    private static string GenerateKeyId() => $"key-{Guid.NewGuid():N}";

    private static byte[] GenerateKeyBytes()
    {
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
