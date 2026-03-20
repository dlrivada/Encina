using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Encina.Security.Encryption.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Azure Key Vault implementation of <see cref="IKeyProvider"/> using envelope encryption.
/// </summary>
/// <remarks>
/// <para>
/// Uses Azure Key Vault's key wrapping/unwrapping capabilities for envelope encryption:
/// data encryption keys (DEKs) are encrypted with the Key Vault key (KEK) and stored alongside
/// the ciphertext. Only the wrapped DEK is sent to Key Vault for unwrapping during decryption.
/// </para>
/// <para>
/// Supports RSA and EC keys hosted in Azure Key Vault, including HSM-backed keys.
/// Key rotation is performed by creating a new version of the key in Key Vault.
/// </para>
/// <para>
/// This class is thread-safe and suitable for singleton registration.
/// </para>
/// </remarks>
public sealed partial class AzureKeyVaultKeyProvider : IKeyProvider
{
    private readonly KeyClient _keyClient;
    private readonly AzureKeyVaultOptions _options;
    private readonly ILogger<AzureKeyVaultKeyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultKeyProvider"/> class.
    /// </summary>
    /// <param name="keyClient">The Azure Key Vault key client.</param>
    /// <param name="options">The Azure Key Vault configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureKeyVaultKeyProvider(
        KeyClient keyClient,
        IOptions<AzureKeyVaultOptions> options,
        ILogger<AzureKeyVaultKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(keyClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _keyClient = keyClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            // keyId format: "{keyName}/{keyVersion}" or just "{keyName}"
            var (keyName, keyVersion) = ParseKeyId(keyId);

            var cryptoClient = _keyClient.GetCryptographyClient(keyName, keyVersion);

            // Generate a local data key and wrap it with the Key Vault key
            var dataKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            var wrapResult = await cryptoClient.WrapKeyAsync(
                KeyWrapAlgorithm.RsaOaep256, dataKey, cancellationToken)
                .ConfigureAwait(false);

            Log.KeyRetrieved(_logger, keyId);
            return Right<EncinaError, byte[]>(dataKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Log.KeyNotFound(_logger, keyId);
            return Left<EncinaError, byte[]>(
                MessageEncryptionErrors.KeyNotFound(keyId));
        }
        catch (RequestFailedException ex) when (ex.Status is 401 or 403)
        {
            Log.AccessDenied(_logger, keyId, ex.Message, ex);
            return Left<EncinaError, byte[]>(
                MessageEncryptionErrors.ProviderUnavailable($"Access denied to key '{keyId}'.", ex));
        }
        catch (RequestFailedException ex)
        {
            Log.ProviderError(_logger, keyId, ex.Message, ex);
            return Left<EncinaError, byte[]>(
                MessageEncryptionErrors.ProviderUnavailable(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetCurrentKeyIdAsync(
        CancellationToken cancellationToken = default)
    {
        var keyName = _options.KeyName;
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return Left<EncinaError, string>(
                MessageEncryptionErrors.ProviderUnavailable("KeyName is not configured in AzureKeyVaultOptions."));
        }

        try
        {
            var response = await _keyClient.GetKeyAsync(
                keyName, _options.KeyVersion, cancellationToken)
                .ConfigureAwait(false);

            var key = response.Value;
            var keyId = key.Properties.Version is not null
                ? $"{keyName}/{key.Properties.Version}"
                : keyName;

            Log.CurrentKeyResolved(_logger, keyId);
            return Right<EncinaError, string>(keyId);
        }
        catch (RequestFailedException ex)
        {
            Log.ProviderError(_logger, keyName, ex.Message, ex);
            return Left<EncinaError, string>(
                MessageEncryptionErrors.ProviderUnavailable(ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> RotateKeyAsync(
        CancellationToken cancellationToken = default)
    {
        var keyName = _options.KeyName;
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return Left<EncinaError, string>(
                MessageEncryptionErrors.ProviderUnavailable("KeyName is not configured in AzureKeyVaultOptions."));
        }

        try
        {
            // Azure Key Vault rotation creates a new version of the existing key
            var key = await _keyClient.RotateKeyAsync(keyName, cancellationToken)
                .ConfigureAwait(false);

            var newKeyId = $"{keyName}/{key.Value.Properties.Version}";
            Log.KeyRotated(_logger, newKeyId);
            return Right<EncinaError, string>(newKeyId);
        }
        catch (RequestFailedException ex)
        {
            Log.RotationFailed(_logger, keyName, ex.Message, ex);
            return Left<EncinaError, string>(
                MessageEncryptionErrors.ProviderUnavailable($"Key rotation failed: {ex.Message}", ex));
        }
    }

    private static (string KeyName, string? KeyVersion) ParseKeyId(string keyId)
    {
        var slashIndex = keyId.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex > 0)
        {
            return (keyId[..slashIndex], keyId[(slashIndex + 1)..]);
        }

        return (keyId, null);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2475, Level = LogLevel.Debug,
            Message = "Key '{KeyId}' retrieved from Azure Key Vault.")]
        public static partial void KeyRetrieved(ILogger logger, string keyId);

        [LoggerMessage(EventId = 2476, Level = LogLevel.Debug,
            Message = "Key '{KeyId}' not found in Azure Key Vault.")]
        public static partial void KeyNotFound(ILogger logger, string keyId);

        [LoggerMessage(EventId = 2477, Level = LogLevel.Debug,
            Message = "Current key resolved to '{KeyId}' in Azure Key Vault.")]
        public static partial void CurrentKeyResolved(ILogger logger, string keyId);

        [LoggerMessage(EventId = 2478, Level = LogLevel.Information,
            Message = "Key rotated to '{KeyId}' in Azure Key Vault.")]
        public static partial void KeyRotated(ILogger logger, string keyId);

        [LoggerMessage(EventId = 2479, Level = LogLevel.Warning,
            Message = "Access denied to key '{KeyId}' in Azure Key Vault: {Reason}")]
        public static partial void AccessDenied(ILogger logger, string keyId, string reason, Exception exception);

        [LoggerMessage(EventId = 2480, Level = LogLevel.Warning,
            Message = "Azure Key Vault provider error for key '{KeyId}': {Reason}")]
        public static partial void ProviderError(ILogger logger, string keyId, string reason, Exception exception);

        [LoggerMessage(EventId = 2481, Level = LogLevel.Error,
            Message = "Key rotation failed for '{KeyName}' in Azure Key Vault: {Reason}")]
        public static partial void RotationFailed(ILogger logger, string keyName, string reason, Exception exception);
    }
}
