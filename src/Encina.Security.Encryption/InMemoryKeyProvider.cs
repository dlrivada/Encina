using System.Collections.Concurrent;
using System.Security.Cryptography;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.Encryption;

/// <summary>
/// In-memory implementation of <see cref="IKeyProvider"/> for testing and development scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider is designed for:
/// <list type="bullet">
/// <item><description>Unit and integration testing</description></item>
/// <item><description>Development and local debugging</description></item>
/// <item><description>Prototyping encryption features</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Keys are stored in process memory and lost when the
/// process restarts. For production use, integrate with a Key Management Service (KMS) such as
/// Azure Key Vault, AWS KMS, or HashiCorp Vault.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access
/// and <c>volatile</c> for the current key identifier.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var keyProvider = new InMemoryKeyProvider();
///
/// // Add a key for testing
/// keyProvider.AddKey("test-key-v1", new byte[32]);
/// keyProvider.SetCurrentKey("test-key-v1");
///
/// // Or auto-generate via rotation
/// var result = await keyProvider.RotateKeyAsync();
/// var newKeyId = result.Match(Right: id => id, Left: _ => throw new Exception());
/// </code>
/// </example>
public sealed class InMemoryKeyProvider : IKeyProvider
{
    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int DefaultKeySizeInBytes = 32;

    private readonly ConcurrentDictionary<string, byte[]> _keys = new(StringComparer.Ordinal);
    private volatile string? _currentKeyId;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left(EncinaError.New("Operation was cancelled.")));
        }

        if (_keys.TryGetValue(keyId, out var key))
        {
            return ValueTask.FromResult<Either<EncinaError, byte[]>>(Right(key));
        }

        return ValueTask.FromResult<Either<EncinaError, byte[]>>(
            Left(EncryptionErrors.KeyNotFound(keyId)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, string>> GetCurrentKeyIdAsync(
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, string>>(
                Left(EncinaError.New("Operation was cancelled.")));
        }

        var currentKeyId = _currentKeyId;

        if (currentKeyId is null)
        {
            return ValueTask.FromResult<Either<EncinaError, string>>(
                Left(EncryptionErrors.KeyNotFound("(no current key configured)")));
        }

        return ValueTask.FromResult<Either<EncinaError, string>>(Right(currentKeyId));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, string>> RotateKeyAsync(
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, string>>(
                Left(EncinaError.New("Operation was cancelled.")));
        }

        try
        {
            var newKeyId = $"key-{Guid.NewGuid():N}";
            var newKey = new byte[DefaultKeySizeInBytes];
            RandomNumberGenerator.Fill(newKey);

            _keys[newKeyId] = newKey;
            _currentKeyId = newKeyId;

            return ValueTask.FromResult<Either<EncinaError, string>>(Right(newKeyId));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, string>>(
                Left(EncryptionErrors.KeyRotationFailed(ex)));
        }
    }

    /// <summary>
    /// Adds a key to the provider.
    /// </summary>
    /// <param name="keyId">The unique identifier for the key.</param>
    /// <param name="key">The key material (should be 32 bytes for AES-256).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyId"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <remarks>
    /// Intended for test setup. If a key with the same ID already exists, it is replaced.
    /// </remarks>
    public void AddKey(string keyId, byte[] key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(key);

        _keys[keyId] = key;
    }

    /// <summary>
    /// Sets the current active key identifier.
    /// </summary>
    /// <param name="keyId">The key identifier to set as current.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyId"/> is null or whitespace.</exception>
    /// <remarks>
    /// The key must have been previously added via <see cref="AddKey"/> or <see cref="RotateKeyAsync"/>.
    /// This method does NOT validate that the key exists in the store.
    /// </remarks>
    public void SetCurrentKey(string keyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        _currentKeyId = keyId;
    }

    /// <summary>
    /// Gets the number of keys stored in the provider.
    /// </summary>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// </remarks>
    public int Count => _keys.Count;

    /// <summary>
    /// Clears all keys and resets the current key identifier.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests.
    /// </remarks>
    public void Clear()
    {
        _keys.Clear();
        _currentKeyId = null;
    }
}
