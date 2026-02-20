using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.Encryption.Algorithms;

/// <summary>
/// Field-level encryptor using AES-256 with Galois/Counter Mode (GCM).
/// </summary>
/// <remarks>
/// <para>
/// Provides authenticated encryption with associated data (AEAD), ensuring both
/// confidentiality and integrity of encrypted values. Uses NIST-approved AES-256-GCM
/// (SP 800-38D) with:
/// <list type="bullet">
/// <item><description>256-bit (32-byte) key size</description></item>
/// <item><description>96-bit (12-byte) nonce, generated per operation via <see cref="RandomNumberGenerator"/></description></item>
/// <item><description>128-bit (16-byte) authentication tag</description></item>
/// </list>
/// </para>
/// <para>
/// Keys are obtained from the injected <see cref="IKeyProvider"/>. If the
/// <see cref="EncryptionContext.KeyId"/> is <c>null</c>, the current active key is used.
/// </para>
/// <para>
/// Thread-safe: all operations are stateless and use new <see cref="AesGcm"/> instances
/// per operation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var encryptor = new AesGcmFieldEncryptor(keyProvider);
/// var context = new EncryptionContext { Purpose = "User.Email" };
///
/// var encrypted = await encryptor.EncryptStringAsync("user@example.com", context);
/// var decrypted = await encryptor.DecryptStringAsync(encrypted.Match(r => r, l => default), context);
/// </code>
/// </example>
internal sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    /// <summary>
    /// AES-GCM nonce size in bytes (96 bits).
    /// </summary>
    private const int NonceSizeInBytes = 12;

    /// <summary>
    /// AES-GCM authentication tag size in bytes (128 bits).
    /// </summary>
    private const int TagSizeInBytes = 16;

    /// <summary>
    /// Required key size in bytes for AES-256 (256 bits).
    /// </summary>
    private const int KeySizeInBytes = 32;

    private readonly IKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesGcmFieldEncryptor"/> class.
    /// </summary>
    /// <param name="keyProvider">The key provider used to retrieve encryption keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyProvider"/> is null.</exception>
    public AesGcmFieldEncryptor(IKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, EncryptedValue>> EncryptStringAsync(
        string plaintext,
        EncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(context);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        return await EncryptCoreAsync(plaintextBytes, context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> DecryptStringAsync(
        EncryptedValue encryptedValue,
        EncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var bytesResult = await DecryptCoreAsync(encryptedValue, context, cancellationToken).ConfigureAwait(false);

        return bytesResult.Map(bytes => Encoding.UTF8.GetString(bytes));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, EncryptedValue>> EncryptBytesAsync(
        ReadOnlyMemory<byte> plaintext,
        EncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await EncryptCoreAsync(plaintext.Span.ToArray(), context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, byte[]>> DecryptBytesAsync(
        EncryptedValue encryptedValue,
        EncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await DecryptCoreAsync(encryptedValue, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Core encryption logic using AES-256-GCM.
    /// </summary>
    private async ValueTask<Either<EncinaError, EncryptedValue>> EncryptCoreAsync(
        byte[] plaintext,
        EncryptionContext context,
        CancellationToken cancellationToken)
    {
        // Resolve key ID
        var keyIdResult = await ResolveKeyIdAsync(context, cancellationToken).ConfigureAwait(false);
        if (keyIdResult.IsLeft)
        {
            return Left<EncinaError, EncryptedValue>(
                keyIdResult.Match(Right: _ => default!, Left: e => e));
        }

        var keyId = keyIdResult.Match(Right: id => id, Left: _ => string.Empty);

        // Retrieve key material
        var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken).ConfigureAwait(false);
        if (keyResult.IsLeft)
        {
            return Left<EncinaError, EncryptedValue>(
                keyResult.Match(Right: _ => default!, Left: e => e));
        }

        var key = keyResult.Match(Right: k => k, Left: _ => []);

        if (key.Length != KeySizeInBytes)
        {
            return Left<EncinaError, EncryptedValue>(
                EncryptionErrors.KeyNotFound(keyId));
        }

        try
        {
            // Generate cryptographic nonce
            var nonce = new byte[NonceSizeInBytes];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSizeInBytes];

            // Resolve associated data
            var associatedData = context.AssociatedData.IsDefaultOrEmpty
                ? null
                : context.AssociatedData.AsSpan();

            using var aesGcm = new AesGcm(key, TagSizeInBytes);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            var result = new EncryptedValue
            {
                Ciphertext = [.. ciphertext],
                Algorithm = EncryptionAlgorithm.Aes256Gcm,
                KeyId = keyId,
                Nonce = [.. nonce],
                Tag = [.. tag]
            };

            return Right<EncinaError, EncryptedValue>(result);
        }
        catch (CryptographicException ex)
        {
            return Left<EncinaError, EncryptedValue>(
                EncryptionErrors.DecryptionFailed(keyId, exception: ex));
        }
    }

    /// <summary>
    /// Core decryption logic using AES-256-GCM.
    /// </summary>
    private async ValueTask<Either<EncinaError, byte[]>> DecryptCoreAsync(
        EncryptedValue encryptedValue,
        EncryptionContext context,
        CancellationToken cancellationToken)
    {
        if (encryptedValue.Algorithm != EncryptionAlgorithm.Aes256Gcm)
        {
            return Left<EncinaError, byte[]>(
                EncryptionErrors.AlgorithmNotSupported(encryptedValue.Algorithm));
        }

        if (encryptedValue.Ciphertext.IsDefaultOrEmpty ||
            encryptedValue.Nonce.IsDefaultOrEmpty ||
            encryptedValue.Tag.IsDefaultOrEmpty)
        {
            return Left<EncinaError, byte[]>(
                EncryptionErrors.InvalidCiphertext());
        }

        var keyId = encryptedValue.KeyId;

        // Retrieve key material
        var keyResult = await _keyProvider.GetKeyAsync(keyId, cancellationToken).ConfigureAwait(false);
        if (keyResult.IsLeft)
        {
            return Left<EncinaError, byte[]>(
                keyResult.Match(Right: _ => default!, Left: e => e));
        }

        var key = keyResult.Match(Right: k => k, Left: _ => []);

        if (key.Length != KeySizeInBytes)
        {
            return Left<EncinaError, byte[]>(
                EncryptionErrors.KeyNotFound(keyId));
        }

        try
        {
            var plaintext = new byte[encryptedValue.Ciphertext.Length];

            // Resolve associated data
            var associatedData = context.AssociatedData.IsDefaultOrEmpty
                ? null
                : context.AssociatedData.AsSpan();

            using var aesGcm = new AesGcm(key, TagSizeInBytes);
            aesGcm.Decrypt(
                encryptedValue.Nonce.AsSpan(),
                encryptedValue.Ciphertext.AsSpan(),
                encryptedValue.Tag.AsSpan(),
                plaintext,
                associatedData);

            return Right<EncinaError, byte[]>(plaintext);
        }
        catch (CryptographicException ex)
        {
            return Left<EncinaError, byte[]>(
                EncryptionErrors.DecryptionFailed(keyId, exception: ex));
        }
    }

    /// <summary>
    /// Resolves the key ID to use for encryption, preferring the context's explicit key ID
    /// over the current active key from the provider.
    /// </summary>
    private async ValueTask<Either<EncinaError, string>> ResolveKeyIdAsync(
        EncryptionContext context,
        CancellationToken cancellationToken)
    {
        if (context.KeyId is not null)
        {
            return Right<EncinaError, string>(context.KeyId);
        }

        return await _keyProvider.GetCurrentKeyIdAsync(cancellationToken).ConfigureAwait(false);
    }
}
