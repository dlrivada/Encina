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
    private readonly Action<byte[]> _fillNonce;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesGcmFieldEncryptor"/> class.
    /// </summary>
    /// <param name="keyProvider">The key provider used to retrieve encryption keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyProvider"/> is null.</exception>
    public AesGcmFieldEncryptor(IKeyProvider keyProvider)
        : this(keyProvider, fillNonce: null)
    {
    }

    /// <summary>
    /// Initializes a new instance with a replaceable nonce source, so tests can make the
    /// encryption primitive fail (no real input makes <c>AesGcm.Encrypt</c> throw).
    /// </summary>
    /// <param name="keyProvider">The key provider used to retrieve encryption keys.</param>
    /// <param name="fillNonce">Fills the nonce buffer; defaults to <see cref="RandomNumberGenerator.Fill(Span{byte})"/>.</param>
    internal AesGcmFieldEncryptor(IKeyProvider keyProvider, Action<byte[]>? fillNonce)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _fillNonce = fillNonce ?? (static nonce => RandomNumberGenerator.Fill(nonce));
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
            _fillNonce(nonce);

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
                EncryptionErrors.EncryptionFailed(keyId, ex));
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
        var validationError = ValidateEncryptedValue(encryptedValue);
        if (validationError is not null)
        {
            return Left<EncinaError, byte[]>(validationError.Value);
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

        return DecryptWithKey(key, encryptedValue, context);
    }

    private static EncinaError? ValidateEncryptedValue(EncryptedValue encryptedValue)
    {
        if (encryptedValue.Algorithm != EncryptionAlgorithm.Aes256Gcm)
        {
            return EncryptionErrors.AlgorithmNotSupported(encryptedValue.Algorithm);
        }

        // A zero-length ciphertext is valid: AES-GCM authenticates an empty plaintext through the tag.
        if (encryptedValue.Ciphertext.IsDefault ||
            !HasValidNonceAndTagLength(encryptedValue))
        {
            return EncryptionErrors.InvalidCiphertext();
        }

        return null;
    }

    // AesGcm.Decrypt throws ArgumentException (not CryptographicException) for a nonce that is
    // not 12 bytes or a tag that is not 16 bytes; reject them here so the Either API never throws.
    // Default and empty arrays have length 0 and are rejected too.
    private static bool HasValidNonceAndTagLength(EncryptedValue encryptedValue) =>
        !encryptedValue.Nonce.IsDefault &&
        !encryptedValue.Tag.IsDefault &&
        encryptedValue.Nonce.Length == NonceSizeInBytes &&
        encryptedValue.Tag.Length == TagSizeInBytes;

    private static Either<EncinaError, byte[]> DecryptWithKey(
        byte[] key,
        EncryptedValue encryptedValue,
        EncryptionContext context)
    {
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
        catch (CryptographicException)
        {
            // Tag verification failures, wrong keys and tampered ciphertext are indistinguishable
            // by design: one generic error with no key id and no exception, so callers get no oracle.
            return Left<EncinaError, byte[]>(EncryptionErrors.InvalidCiphertext());
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
