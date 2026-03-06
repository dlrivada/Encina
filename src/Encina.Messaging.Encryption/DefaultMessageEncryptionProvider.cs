using System.Collections.Immutable;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Model;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Encryption;

/// <summary>
/// Default implementation of <see cref="IMessageEncryptionProvider"/> that delegates
/// cryptographic operations to <see cref="IFieldEncryptor"/> and <see cref="IKeyProvider"/>
/// from <c>Encina.Security.Encryption</c>.
/// </summary>
/// <remarks>
/// <para>
/// This provider reuses the existing AES-256-GCM encryption infrastructure. It converts
/// between <see cref="MessageEncryptionContext"/> and <see cref="EncryptionContext"/>,
/// delegating the actual encrypt/decrypt operations to <see cref="IFieldEncryptor.EncryptBytesAsync"/>
/// and <see cref="IFieldEncryptor.DecryptBytesAsync"/>.
/// </para>
/// <para>
/// If <see cref="MessageEncryptionContext.KeyId"/> is <c>null</c>, this provider resolves
/// the current active key via <see cref="IKeyProvider.GetCurrentKeyIdAsync"/>.
/// </para>
/// <para>
/// This class is thread-safe and suitable for singleton registration.
/// </para>
/// </remarks>
public sealed class DefaultMessageEncryptionProvider : IMessageEncryptionProvider
{
    private const string DefaultAlgorithm = "AES-256-GCM";

    private readonly IFieldEncryptor _fieldEncryptor;
    private readonly IKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessageEncryptionProvider"/> class.
    /// </summary>
    /// <param name="fieldEncryptor">The field encryptor for cryptographic operations.</param>
    /// <param name="keyProvider">The key provider for key resolution and retrieval.</param>
    public DefaultMessageEncryptionProvider(
        IFieldEncryptor fieldEncryptor,
        IKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(fieldEncryptor);
        ArgumentNullException.ThrowIfNull(keyProvider);

        _fieldEncryptor = fieldEncryptor;
        _keyProvider = keyProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Resolve key ID: use explicit key from context, or fall back to current active key
        var keyId = context.KeyId;
        if (keyId is null)
        {
            var keyIdResult = await _keyProvider.GetCurrentKeyIdAsync(cancellationToken).ConfigureAwait(false);
            if (keyIdResult.IsLeft)
            {
                return Left<EncinaError, EncryptedPayload>(
                    keyIdResult.Match(Right: _ => default!, Left: e => e));
            }

            keyId = keyIdResult.Match(Right: id => id, Left: _ => string.Empty);
        }

        // Build EncryptionContext from MessageEncryptionContext
        var encryptionContext = new EncryptionContext
        {
            KeyId = keyId,
            AssociatedData = context.AssociatedData
        };

        // Delegate to IFieldEncryptor for the actual cryptographic operation
        var result = await _fieldEncryptor.EncryptBytesAsync(plaintext, encryptionContext, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(encrypted => new EncryptedPayload
        {
            Ciphertext = encrypted.Ciphertext,
            KeyId = encrypted.KeyId,
            Algorithm = DefaultAlgorithm,
            Nonce = encrypted.Nonce,
            Tag = encrypted.Tag,
            Version = 1
        });
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ImmutableArray<byte>>> DecryptAsync(
        EncryptedPayload payload,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(context);

        // Build EncryptionContext for decryption
        var encryptionContext = new EncryptionContext
        {
            KeyId = payload.KeyId,
            AssociatedData = context.AssociatedData
        };

        // Reconstruct EncryptedValue from EncryptedPayload
        var encryptedValue = new EncryptedValue
        {
            Ciphertext = payload.Ciphertext,
            KeyId = payload.KeyId,
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            Nonce = payload.Nonce,
            Tag = payload.Tag
        };

        var result = await _fieldEncryptor.DecryptBytesAsync(encryptedValue, encryptionContext, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(bytes => bytes.ToImmutableArray());
    }
}
