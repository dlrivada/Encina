using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Default implementation of <see cref="IPseudonymizer"/> using AES-256-GCM for reversible
/// pseudonymization and HMAC-SHA256 for deterministic pseudonymization.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="System.Security.Cryptography.AesGcm"/> for authenticated
/// encryption (reversible) and <see cref="System.Security.Cryptography.HMACSHA256"/> for
/// keyed hashing (deterministic, one-way). Key material is obtained from the registered
/// <see cref="IKeyProvider"/>.
/// </para>
/// <para>
/// For object-level pseudonymization, reflection is used to discover writable <c>string</c>
/// properties. Property metadata is cached per type in a static <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// Nonces (12 bytes for AES-GCM) and authentication tags (16 bytes) are generated using
/// <see cref="RandomNumberGenerator"/> and prepended to the ciphertext for self-contained storage.
/// </para>
/// </remarks>
public sealed class DefaultPseudonymizer : IPseudonymizer
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly IKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultPseudonymizer"/>.
    /// </summary>
    /// <param name="keyProvider">Provider for cryptographic key material.</param>
    public DefaultPseudonymizer(IKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);

        _keyProvider = keyProvider;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, T>> PseudonymizeAsync<T>(
        T data,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(keyId);

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken)
                .ConfigureAwait(false);

            return await keyResult.MatchAsync(
                RightAsync: async key =>
                {
                    var properties = GetStringProperties(typeof(T));
                    var copy = ShallowCopy(data);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(copy) as string;
                        if (value is null)
                        {
                            continue;
                        }

                        var encrypted = EncryptAesGcm(value, key);
                        property.SetValue(copy, Convert.ToBase64String(encrypted));
                    }

                    return Right<EncinaError, T>(copy);
                },
                Left: error => Left<EncinaError, T>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, T>(
                AnonymizationErrors.PseudonymizationFailed(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, T>> DepseudonymizeAsync<T>(
        T data,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(keyId);

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken)
                .ConfigureAwait(false);

            return await keyResult.MatchAsync(
                RightAsync: async key =>
                {
                    var properties = GetStringProperties(typeof(T));
                    var copy = ShallowCopy(data);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(copy) as string;
                        if (value is null)
                        {
                            continue;
                        }

                        try
                        {
                            var cipherData = Convert.FromBase64String(value);
                            var decrypted = DecryptAesGcm(cipherData, key);
                            property.SetValue(copy, decrypted);
                        }
                        catch (FormatException)
                        {
                            // Not a Base64 string — skip (not pseudonymized)
                        }
                        catch (CryptographicException)
                        {
                            return Left<EncinaError, T>(
                                AnonymizationErrors.DecryptionFailed(keyId));
                        }
                    }

                    return Right<EncinaError, T>(copy);
                },
                Left: error => Left<EncinaError, T>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, T>(
                AnonymizationErrors.DepseudonymizationFailed(ex.Message));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> PseudonymizeValueAsync(
        string value,
        string keyId,
        PseudonymizationAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(keyId);

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken)
                .ConfigureAwait(false);

            return keyResult.Match(
                Right: key =>
                {
                    return algorithm switch
                    {
                        PseudonymizationAlgorithm.Aes256Gcm =>
                            Right<EncinaError, string>(
                                Convert.ToBase64String(EncryptAesGcm(value, key))),

                        PseudonymizationAlgorithm.HmacSha256 =>
                            Right<EncinaError, string>(
                                ComputeHmac(value, key)),

                        _ => Left<EncinaError, string>(
                            AnonymizationErrors.PseudonymizationFailed(
                                $"Unsupported algorithm: {algorithm}"))
                    };
                },
                Left: error => Left<EncinaError, string>(error));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, string>(
                AnonymizationErrors.PseudonymizationFailed(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> DepseudonymizeValueAsync(
        string pseudonym,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pseudonym);
        ArgumentNullException.ThrowIfNull(keyId);

        try
        {
            var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken)
                .ConfigureAwait(false);

            return keyResult.Match(
                Right: key =>
                {
                    try
                    {
                        var cipherData = Convert.FromBase64String(pseudonym);
                        var decrypted = DecryptAesGcm(cipherData, key);
                        return Right<EncinaError, string>(decrypted);
                    }
                    catch (FormatException ex)
                    {
                        return Left<EncinaError, string>(
                            AnonymizationErrors.DepseudonymizationFailed(
                                $"Invalid pseudonym format (not Base64): {ex.Message}"));
                    }
                    catch (CryptographicException ex)
                    {
                        return Left<EncinaError, string>(
                            AnonymizationErrors.DecryptionFailed(keyId, ex));
                    }
                },
                Left: error => Left<EncinaError, string>(error));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, string>(
                AnonymizationErrors.DepseudonymizationFailed(ex.Message));
        }
    }

    /// <summary>
    /// Encrypts a plaintext string using AES-256-GCM.
    /// Returns: [nonce (12 bytes)] [tag (16 bytes)] [ciphertext].
    /// </summary>
    private static byte[] EncryptAesGcm(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Pack as: nonce + tag + ciphertext
        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertext.CopyTo(result, NonceSizeBytes + TagSizeBytes);

        return result;
    }

    /// <summary>
    /// Decrypts an AES-256-GCM ciphertext packed as [nonce][tag][ciphertext].
    /// </summary>
    private static string DecryptAesGcm(byte[] packed, byte[] key)
    {
        if (packed.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Invalid ciphertext: too short.");
        }

        var nonce = packed.AsSpan(0, NonceSizeBytes);
        var tag = packed.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = packed.AsSpan(NonceSizeBytes + TagSizeBytes);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Computes an HMAC-SHA256 hash of the value using the provided key.
    /// </summary>
    private static string ComputeHmac(string value, byte[] key)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var hash = HMACSHA256.HashData(key, valueBytes);
        return Convert.ToBase64String(hash);
    }

    private static PropertyInfo[] GetStringProperties(Type type) =>
        PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true }
                    && p.PropertyType == typeof(string))
                .ToArray());

    private static T ShallowCopy<T>(T source)
    {
        var cloneMethod = typeof(T).GetMethod(
            "MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (cloneMethod is not null)
        {
            return (T)cloneMethod.Invoke(source, null)!;
        }

        var copy = Activator.CreateInstance<T>();
        foreach (var property in GetStringProperties(typeof(T)))
        {
            property.SetValue(copy, property.GetValue(source));
        }

        return copy;
    }
}
