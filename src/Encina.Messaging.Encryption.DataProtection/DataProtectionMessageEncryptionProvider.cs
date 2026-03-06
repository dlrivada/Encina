using System.Collections.Immutable;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Model;
using LanguageExt;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Encryption.DataProtection;

/// <summary>
/// ASP.NET Core Data Protection implementation of <see cref="IMessageEncryptionProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the Azure Key Vault and AWS KMS providers (which implement <c>IKeyProvider</c>),
/// this provider implements <see cref="IMessageEncryptionProvider"/> directly because
/// Data Protection handles both key management AND encryption as a unified framework.
/// </para>
/// <para>
/// Key management is automatic: Data Protection generates, rotates, and retires keys
/// based on its configured key management policy. No explicit key IDs are needed.
/// </para>
/// <para>
/// The <see cref="EncryptedPayload.KeyId"/> is set to <c>"data-protection"</c> for
/// identification in logs and diagnostics.
/// </para>
/// <para>
/// This class is thread-safe and suitable for singleton registration.
/// </para>
/// </remarks>
public sealed partial class DataProtectionMessageEncryptionProvider : IMessageEncryptionProvider
{
    private const string ProviderKeyId = "data-protection";
    private const string Algorithm = "DataProtection";

    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionMessageEncryptionProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionMessageEncryptionProvider"/> class.
    /// </summary>
    /// <param name="dataProtectionProvider">The Data Protection provider to create protectors from.</param>
    /// <param name="options">The Data Protection encryption options.</param>
    /// <param name="logger">The logger instance.</param>
    public DataProtectionMessageEncryptionProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<DataProtectionEncryptionOptions> options,
        ILogger<DataProtectionMessageEncryptionProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _protector = dataProtectionProvider.CreateProtector(options.Value.Purpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var protectedBytes = _protector.Protect(plaintext.ToArray());

            var payload = new EncryptedPayload
            {
                Ciphertext = protectedBytes.ToImmutableArray(),
                KeyId = ProviderKeyId,
                Algorithm = Algorithm,
                Nonce = ImmutableArray<byte>.Empty,
                Tag = ImmutableArray<byte>.Empty,
                Version = 1
            };

            Log.MessageEncrypted(_logger, context.MessageType ?? "unknown");
            return ValueTask.FromResult<Either<EncinaError, EncryptedPayload>>(Right(payload));
        }
        catch (Exception ex)
        {
            Log.EncryptionFailed(_logger, context.MessageType ?? "unknown", ex.Message, ex);
            return ValueTask.FromResult<Either<EncinaError, EncryptedPayload>>(
                Left<EncinaError, EncryptedPayload>(
                    MessageEncryptionErrors.EncryptionFailed(context.MessageType, ex)));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, ImmutableArray<byte>>> DecryptAsync(
        EncryptedPayload payload,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var unprotectedBytes = _protector.Unprotect(payload.Ciphertext.AsSpan().ToArray());

            Log.MessageDecrypted(_logger, payload.KeyId);
            return ValueTask.FromResult<Either<EncinaError, ImmutableArray<byte>>>(
                Right(unprotectedBytes.ToImmutableArray()));
        }
        catch (Exception ex)
        {
            Log.DecryptionFailed(_logger, payload.KeyId, ex.Message, ex);
            return ValueTask.FromResult<Either<EncinaError, ImmutableArray<byte>>>(
                Left<EncinaError, ImmutableArray<byte>>(
                    MessageEncryptionErrors.DecryptionFailed(payload.KeyId, context.MessageType, ex)));
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2520, Level = LogLevel.Debug,
            Message = "Message payload encrypted via Data Protection for type '{MessageType}'.")]
        public static partial void MessageEncrypted(ILogger logger, string messageType);

        [LoggerMessage(EventId = 2521, Level = LogLevel.Error,
            Message = "Data Protection encryption failed for type '{MessageType}': {Reason}")]
        public static partial void EncryptionFailed(ILogger logger, string messageType, string reason, Exception exception);

        [LoggerMessage(EventId = 2522, Level = LogLevel.Debug,
            Message = "Message payload decrypted via Data Protection with key '{KeyId}'.")]
        public static partial void MessageDecrypted(ILogger logger, string keyId);

        [LoggerMessage(EventId = 2523, Level = LogLevel.Error,
            Message = "Data Protection decryption failed with key '{KeyId}': {Reason}")]
        public static partial void DecryptionFailed(ILogger logger, string keyId, string reason, Exception exception);
    }
}
