using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Security.Encryption;

/// <summary>
/// Orchestrates field-level encryption and decryption for objects with properties
/// decorated with <see cref="EncryptAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator discovers encrypted properties via <see cref="EncryptedPropertyCache"/>
/// and delegates actual cryptographic operations to <see cref="IFieldEncryptor"/>.
/// </para>
/// <para>
/// For each property marked with <see cref="EncryptAttribute"/>:
/// <list type="bullet">
/// <item><description><b>Encryption</b>: reads the plaintext string value, encrypts it, and replaces
/// the property value with the Base64-encoded <see cref="EncryptedValue"/> JSON representation.</description></item>
/// <item><description><b>Decryption</b>: reads the encrypted string value, decrypts it, and replaces
/// the property value with the original plaintext.</description></item>
/// </list>
/// </para>
/// <para>
/// Error handling respects the <see cref="EncryptionAttribute.FailOnError"/> flag:
/// when <c>true</c>, the first failure short-circuits and returns the error;
/// when <c>false</c>, the property value is left unchanged and processing continues.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var orchestrator = new EncryptionOrchestrator(fieldEncryptor, logger);
///
/// // Encrypt all [Encrypt]-decorated properties
/// var result = await orchestrator.EncryptAsync(command, requestContext);
///
/// // Decrypt all [Encrypt]-decorated properties
/// var result = await orchestrator.DecryptAsync(command, requestContext);
/// </code>
/// </example>
internal sealed class EncryptionOrchestrator : IEncryptionOrchestrator
{
    private readonly IFieldEncryptor _fieldEncryptor;
    private readonly ILogger<EncryptionOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionOrchestrator"/> class.
    /// </summary>
    /// <param name="fieldEncryptor">The field encryptor for cryptographic operations.</param>
    /// <param name="logger">The logger for structured logging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fieldEncryptor"/> or <paramref name="logger"/> is null.
    /// </exception>
    public EncryptionOrchestrator(
        IFieldEncryptor fieldEncryptor,
        ILogger<EncryptionOrchestrator> logger)
    {
        _fieldEncryptor = fieldEncryptor ?? throw new ArgumentNullException(nameof(fieldEncryptor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> EncryptAsync<T>(
        T instance,
        IRequestContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, T>(
                EncinaError.New("Operation was cancelled before encryption."));
        }

        var properties = EncryptedPropertyCache.GetProperties(typeof(T));

        if (properties.Length == 0)
        {
            return Right<EncinaError, T>(instance);
        }

        _logger.LogDebug(
            "Encrypting {PropertyCount} properties on {TypeName}",
            properties.Length,
            typeof(T).Name);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(instance);

            if (value is not string plaintext)
            {
                continue;
            }

            var encryptionContext = BuildEncryptionContext(prop.Attribute, context);
            var result = await _fieldEncryptor.EncryptStringAsync(plaintext, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var error = result.MatchUnsafe<EncinaError?>(
                Right: encrypted =>
                {
                    // Store as Base64-serialized representation for string properties
                    var serialized = SerializeEncryptedValue(encrypted);
                    prop.SetValue(instance, serialized);
                    return null;
                },
                Left: e =>
                {
                    if (prop.Attribute.FailOnError)
                    {
                        return e;
                    }

                    _logger.LogWarning(
                        "Encryption failed for property {PropertyName} on {TypeName}, leaving value unchanged: {ErrorMessage}",
                        prop.Property.Name,
                        typeof(T).Name,
                        e.Message);

                    return null;
                });

            if (error is not null)
            {
                return Left<EncinaError, T>(error.Value);
            }
        }

        return Right<EncinaError, T>(instance);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> DecryptAsync<T>(
        T instance,
        IRequestContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, T>(
                EncinaError.New("Operation was cancelled before decryption."));
        }

        var properties = EncryptedPropertyCache.GetProperties(typeof(T));

        if (properties.Length == 0)
        {
            return Right<EncinaError, T>(instance);
        }

        _logger.LogDebug(
            "Decrypting {PropertyCount} properties on {TypeName}",
            properties.Length,
            typeof(T).Name);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(instance);

            if (value is not string serialized)
            {
                continue;
            }

            var encryptedValue = DeserializeEncryptedValue(serialized);

            if (encryptedValue is null)
            {
                if (prop.Attribute.FailOnError)
                {
                    return Left<EncinaError, T>(
                        EncryptionErrors.InvalidCiphertext(prop.Property.Name));
                }

                _logger.LogWarning(
                    "Could not deserialize encrypted value for property {PropertyName} on {TypeName}, leaving value unchanged",
                    prop.Property.Name,
                    typeof(T).Name);

                continue;
            }

            var encryptionContext = BuildEncryptionContext(prop.Attribute, context);
            var result = await _fieldEncryptor.DecryptStringAsync(encryptedValue.Value, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var error = result.MatchUnsafe<EncinaError?>(
                Right: plaintext =>
                {
                    prop.SetValue(instance, plaintext);
                    return null;
                },
                Left: e =>
                {
                    if (prop.Attribute.FailOnError)
                    {
                        return e;
                    }

                    _logger.LogWarning(
                        "Decryption failed for property {PropertyName} on {TypeName}, leaving value unchanged: {ErrorMessage}",
                        prop.Property.Name,
                        typeof(T).Name,
                        e.Message);

                    return null;
                });

            if (error is not null)
            {
                return Left<EncinaError, T>(error.Value);
            }
        }

        return Right<EncinaError, T>(instance);
    }

    /// <summary>
    /// Builds an <see cref="EncryptionContext"/> from the attribute metadata and request context.
    /// </summary>
    private static EncryptionContext BuildEncryptionContext(
        EncryptAttribute attribute,
        IRequestContext requestContext) =>
        new()
        {
            KeyId = attribute.KeyId,
            Purpose = attribute.Purpose,
            TenantId = requestContext.TenantId
        };

    /// <summary>
    /// Serializes an <see cref="EncryptedValue"/> into a compact Base64 string representation.
    /// </summary>
    /// <remarks>
    /// Format: <c>ENC:v1:{Algorithm}:{KeyId}:{Base64Nonce}:{Base64Tag}:{Base64Ciphertext}</c>
    /// </remarks>
    private static string SerializeEncryptedValue(EncryptedValue value)
    {
        var nonce = Convert.ToBase64String(value.Nonce.AsSpan());
        var tag = Convert.ToBase64String(value.Tag.AsSpan());
        var ciphertext = Convert.ToBase64String(value.Ciphertext.AsSpan());

        return $"ENC:v1:{(int)value.Algorithm}:{value.KeyId}:{nonce}:{tag}:{ciphertext}";
    }

    /// <summary>
    /// Deserializes an <see cref="EncryptedValue"/> from its string representation.
    /// </summary>
    /// <returns>
    /// The deserialized <see cref="EncryptedValue"/>, or <c>null</c> if the string is not a valid
    /// encrypted value representation.
    /// </returns>
    private static EncryptedValue? DeserializeEncryptedValue(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized) || !serialized.StartsWith("ENC:v1:", StringComparison.Ordinal))
        {
            return null;
        }

        var parts = serialized.Split(':');

        // Expected: ENC, v1, algorithm, keyId, nonce, tag, ciphertext
        if (parts.Length != 7)
        {
            return null;
        }

        try
        {
            if (!int.TryParse(parts[2], out var algorithmInt) || !Enum.IsDefined(typeof(EncryptionAlgorithm), algorithmInt))
            {
                return null;
            }

            var algorithm = (EncryptionAlgorithm)algorithmInt;
            var keyId = parts[3];
            var nonce = Convert.FromBase64String(parts[4]);
            var tag = Convert.FromBase64String(parts[5]);
            var ciphertext = Convert.FromBase64String(parts[6]);

            return new EncryptedValue
            {
                Algorithm = algorithm,
                KeyId = keyId,
                Nonce = [.. nonce],
                Tag = [.. tag],
                Ciphertext = [.. ciphertext]
            };
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
