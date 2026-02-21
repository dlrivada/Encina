using System.Collections.Concurrent;
using Encina.Security.AntiTampering.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.AntiTampering;

/// <summary>
/// In-memory implementation of <see cref="IKeyProvider"/> for testing and development scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider loads keys from <see cref="AntiTamperingOptions.TestKeys"/> registered via
/// the <see cref="AntiTamperingOptions.AddKey"/> fluent method. Additional keys can be added
/// at runtime via <see cref="AddKey"/>.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Keys are stored in process memory and lost when the
/// process restarts. For production use, integrate with a Key Management Service (KMS) such as
/// Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Via options (recommended)
/// services.AddEncinaAntiTampering(options =>
/// {
///     options.AddKey("api-key-v1", "my-secret-value");
/// });
///
/// // Or directly for testing
/// var keyProvider = new InMemoryKeyProvider(Options.Create(new AntiTamperingOptions()));
/// keyProvider.AddKey("test-key", new byte[32]);
/// </code>
/// </example>
public sealed class InMemoryKeyProvider : IKeyProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryKeyProvider"/> class.
    /// </summary>
    /// <param name="options">The anti-tampering options containing test keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public InMemoryKeyProvider(IOptions<AntiTamperingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var kvp in options.Value.TestKeys)
        {
            _keys[kvp.Key] = kvp.Value;
        }
    }

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
            Left(AntiTamperingErrors.KeyNotFound(keyId)));
    }

    /// <summary>
    /// Adds a key to the provider at runtime.
    /// </summary>
    /// <param name="keyId">The unique identifier for the key.</param>
    /// <param name="key">The key material.</param>
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
    /// Gets the number of keys stored in the provider.
    /// </summary>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// </remarks>
    public int Count => _keys.Count;

    /// <summary>
    /// Clears all keys from the provider.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests.
    /// </remarks>
    public void Clear()
    {
        _keys.Clear();
    }
}
