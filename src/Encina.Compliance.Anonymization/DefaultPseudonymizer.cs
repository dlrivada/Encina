using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Encina.Compliance.Anonymization.Model;
using Encina.Diagnostics;

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
                    if (!AesGcmEnvelope.IsValidKey(key))
                    {
                        return Left<EncinaError, T>(AnonymizationErrors.KeyNotFound(keyId));
                    }

                    var properties = GetStringProperties(typeof(T));
                    var copy = ShallowCopy(data);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(copy) as string;
                        if (value is null)
                        {
                            continue;
                        }

                        var encrypted = AesGcmEnvelope.Encrypt(value, key);
                        property.SetValue(copy, Convert.ToBase64String(encrypted));
                    }

                    return Right<EncinaError, T>(copy);
                },
                Left: error => Left<EncinaError, T>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, T>(PseudonymizationFailed(ex));
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
                    if (!AesGcmEnvelope.IsValidKey(key))
                    {
                        return Left<EncinaError, T>(AnonymizationErrors.KeyNotFound(keyId));
                    }

                    var properties = GetStringProperties(typeof(T));
                    var copy = ShallowCopy(data);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(copy) as string;
                        if (value is null)
                        {
                            continue;
                        }

                        if (!TryDepseudonymize(value, key, out var decrypted))
                        {
                            return Left<EncinaError, T>(AnonymizationErrors.DecryptionFailed());
                        }

                        property.SetValue(copy, decrypted);
                    }

                    return Right<EncinaError, T>(copy);
                },
                Left: error => Left<EncinaError, T>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, T>(
                AnonymizationErrors.DepseudonymizationFailed(ex.GetType().Name));
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
                        PseudonymizationAlgorithm.Aes256Gcm when !AesGcmEnvelope.IsValidKey(key) =>
                            Left<EncinaError, string>(AnonymizationErrors.KeyNotFound(keyId)),

                        PseudonymizationAlgorithm.Aes256Gcm =>
                            Right<EncinaError, string>(
                                Convert.ToBase64String(AesGcmEnvelope.Encrypt(value, key))),

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
            return Left<EncinaError, string>(PseudonymizationFailed(ex));
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
                    if (!AesGcmEnvelope.IsValidKey(key))
                    {
                        return Left<EncinaError, string>(AnonymizationErrors.KeyNotFound(keyId));
                    }

                    return TryDepseudonymize(pseudonym, key, out var decrypted)
                        ? Right<EncinaError, string>(decrypted)
                        : Left<EncinaError, string>(AnonymizationErrors.DecryptionFailed());
                },
                Left: error => Left<EncinaError, string>(error));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, string>(
                AnonymizationErrors.DepseudonymizationFailed(ex.GetType().Name));
        }
    }

    // Base64 decode plus AES-GCM decrypt; false for every stored-value failure, with no cause.
    private static bool TryDepseudonymize(string value, byte[] key, out string plaintext)
    {
        if (AesGcmEnvelope.TryDecodeBase64(value, out var cipherData))
        {
            return AesGcmEnvelope.TryDecrypt(cipherData, key, out plaintext);
        }

        plaintext = string.Empty;
        return false;
    }

    // The message carries only the exception type; the exception is attached redacted (AGENTS.md section 3).
    private static EncinaError PseudonymizationFailed(Exception ex) =>
        AnonymizationErrors.PseudonymizationFailed(ex.GetType().Name, ex.ForLogging());

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
        // S3011: NonPublic reflection required — MemberwiseClone is protected on System.Object
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
