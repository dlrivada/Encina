using System.Security.Cryptography;
using System.Text;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Default implementation of <see cref="ITokenizer"/> that generates tokens based on
/// <see cref="TokenFormat"/> and stores mappings via <see cref="ITokenMappingStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tokens are generated using cryptographically secure random values. The original value
/// is encrypted with AES-256-GCM before storage, and an HMAC-SHA256 hash is computed
/// for deduplication (same value → same token).
/// </para>
/// <para>
/// Three token formats are supported:
/// <list type="bullet">
/// <item><see cref="TokenFormat.Uuid"/>: Standard UUID v4</item>
/// <item><see cref="TokenFormat.Prefixed"/>: UUID with configurable prefix (e.g., <c>"tok_abc123"</c>)</item>
/// <item><see cref="TokenFormat.FormatPreserving"/>: Preserves the length and character class of the original</item>
/// </list>
/// </para>
/// </remarks>
public sealed class DefaultTokenizer : ITokenizer
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly ITokenMappingStore _mappingStore;
    private readonly IKeyProvider _keyProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultTokenizer"/>.
    /// </summary>
    /// <param name="mappingStore">Store for persisting token-to-value mappings.</param>
    /// <param name="keyProvider">Provider for cryptographic key material.</param>
    public DefaultTokenizer(ITokenMappingStore mappingStore, IKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(mappingStore);
        ArgumentNullException.ThrowIfNull(keyProvider);

        _mappingStore = mappingStore;
        _keyProvider = keyProvider;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> TokenizeAsync(
        string value,
        TokenizationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // Get active key
            var activeKeyIdResult = await _keyProvider.GetActiveKeyIdAsync(cancellationToken)
                .ConfigureAwait(false);

            return await activeKeyIdResult.MatchAsync(
                RightAsync: async activeKeyId =>
                {
                    var keyResult = await _keyProvider.GetKeyAsync(activeKeyId, cancellationToken)
                        .ConfigureAwait(false);

                    return await keyResult.MatchAsync(
                        RightAsync: async key =>
                        {
                            // Check for existing mapping (deduplication via HMAC hash)
                            var hash = ComputeHmac(value, key);
                            var existingResult = await _mappingStore.GetByOriginalValueHashAsync(hash, cancellationToken)
                                .ConfigureAwait(false);

                            return await existingResult.MatchAsync(
                                RightAsync: async existing =>
                                {
                                    if (existing.IsSome)
                                    {
                                        var existingMapping = existing.Match(m => m, () => default!);
                                        return Right<EncinaError, string>(existingMapping.Token);
                                    }

                                    // Generate new token
                                    var token = GenerateToken(options, value);

                                    // Encrypt original value
                                    var encrypted = EncryptAesGcm(value, key);

                                    // Store mapping
                                    var mapping = TokenMapping.Create(
                                        token: token,
                                        originalValueHash: hash,
                                        encryptedOriginalValue: encrypted,
                                        keyId: activeKeyId);

                                    var storeResult = await _mappingStore.StoreAsync(mapping, cancellationToken)
                                        .ConfigureAwait(false);

                                    return storeResult.Match(
                                        Right: _ => Right<EncinaError, string>(token),
                                        Left: error => Left<EncinaError, string>(error));
                                },
                                Left: error => Left<EncinaError, string>(error)).ConfigureAwait(false);
                        },
                        Left: error => Left<EncinaError, string>(error)).ConfigureAwait(false);
                },
                Left: error => Left<EncinaError, string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, string>(
                AnonymizationErrors.TokenizationFailed(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, string>> DetokenizeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var mappingResult = await _mappingStore.GetByTokenAsync(token, cancellationToken)
                .ConfigureAwait(false);

            return await mappingResult.MatchAsync(
                RightAsync: async optionMapping =>
                {
                    if (optionMapping.IsNone)
                    {
                        return Left<EncinaError, string>(
                            AnonymizationErrors.TokenNotFound(token));
                    }

                    var mapping = optionMapping.Match(m => m, () => default!);

                    var keyResult = await _keyProvider.GetKeyAsync(mapping.KeyId, cancellationToken)
                        .ConfigureAwait(false);

                    return keyResult.Match(
                        Right: key =>
                        {
                            try
                            {
                                var decrypted = DecryptAesGcm(mapping.EncryptedOriginalValue, key);
                                return Right<EncinaError, string>(decrypted);
                            }
                            catch (CryptographicException ex)
                            {
                                return Left<EncinaError, string>(
                                    AnonymizationErrors.DecryptionFailed(mapping.KeyId, ex));
                            }
                        },
                        Left: error => Left<EncinaError, string>(error));
                },
                Left: error => Left<EncinaError, string>(error)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, string>(
                AnonymizationErrors.TokenizationFailed(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, bool>> IsTokenAsync(
        string value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var result = await _mappingStore.GetByTokenAsync(value, cancellationToken)
                .ConfigureAwait(false);

            return result.Match(
                Right: option => Right<EncinaError, bool>(option.IsSome),
                Left: error => Left<EncinaError, bool>(error));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                AnonymizationErrors.TokenizationFailed(ex.Message, ex));
        }
    }

    private static string GenerateToken(TokenizationOptions options, string originalValue) =>
        options.Format switch
        {
            TokenFormat.Uuid => Guid.NewGuid().ToString("D"),
            TokenFormat.Prefixed => GeneratePrefixedToken(options.Prefix),
            TokenFormat.FormatPreserving => GenerateFormatPreservingToken(originalValue, options.PreserveLength),
            _ => Guid.NewGuid().ToString("D")
        };

    private static string GeneratePrefixedToken(string? prefix)
    {
        var uuid = Guid.NewGuid().ToString("N");
        return string.IsNullOrEmpty(prefix)
            ? $"tok_{uuid}"
            : $"{prefix}_{uuid}";
    }

    private static string GenerateFormatPreservingToken(string originalValue, bool preserveLength)
    {
        var length = preserveLength ? originalValue.Length : Math.Max(originalValue.Length, 8);
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            var originalChar = i < originalValue.Length ? originalValue[i] : '0';

            if (char.IsDigit(originalChar))
            {
                chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }
            else if (char.IsUpper(originalChar))
            {
                chars[i] = (char)('A' + RandomNumberGenerator.GetInt32(26));
            }
            else if (char.IsLower(originalChar))
            {
                chars[i] = (char)('a' + RandomNumberGenerator.GetInt32(26));
            }
            else
            {
                // Preserve non-alphanumeric characters (separators, etc.)
                chars[i] = originalChar;
            }
        }

        return new string(chars);
    }

    private static string ComputeHmac(string value, byte[] key)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var hash = HMACSHA256.HashData(key, valueBytes);
        return Convert.ToBase64String(hash);
    }

    private static byte[] EncryptAesGcm(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertext.CopyTo(result, NonceSizeBytes + TagSizeBytes);

        return result;
    }

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
}
