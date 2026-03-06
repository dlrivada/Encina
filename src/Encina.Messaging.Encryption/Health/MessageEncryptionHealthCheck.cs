using System.Text;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Diagnostics;
using Encina.Messaging.Encryption.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Encryption.Health;

/// <summary>
/// Health check that verifies the message encryption subsystem is operational
/// by performing a roundtrip encrypt/decrypt probe.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications:
/// <list type="number">
///   <item><description>Resolves <see cref="IMessageEncryptionProvider"/> from the DI container.</description></item>
///   <item><description>Encrypts a test payload using <see cref="IMessageEncryptionProvider.EncryptAsync"/>.</description></item>
///   <item><description>Decrypts the result using <see cref="IMessageEncryptionProvider.DecryptAsync"/>.</description></item>
///   <item><description>Verifies the decrypted content matches the original test payload.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="MessageEncryptionOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaMessageEncryption(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class MessageEncryptionHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-message-encryption";

    private static readonly string[] DefaultTags = ["encina", "messaging", "encryption", "ready"];

    private const string TestPlaintext = "encina-message-health-probe";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageEncryptionHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageEncryptionHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve encryption services.</param>
    /// <param name="logger">The logger instance.</param>
    public MessageEncryptionHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<MessageEncryptionHealthCheck> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the default tags for the message encryption health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.HealthCheckStarted();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // 1. Verify IMessageEncryptionProvider is resolvable
            var encryptionProvider = scopedProvider.GetService<IMessageEncryptionProvider>();

            if (encryptionProvider is null)
            {
                _logger.HealthCheckFailed("Missing service: IMessageEncryptionProvider is not registered.");
                return HealthCheckResult.Unhealthy(
                    "Missing service: IMessageEncryptionProvider is not registered.");
            }

            // 2. Encrypt test payload
            var plaintextBytes = Encoding.UTF8.GetBytes(TestPlaintext);
            var encryptionContext = new MessageEncryptionContext
            {
                MessageType = "health-check"
            };

            var encryptResult = await encryptionProvider.EncryptAsync(
                plaintextBytes, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var encryptError = encryptResult.MatchUnsafe<string?>(
                Right: _ => null,
                Left: e => e.Message);

            if (encryptError is not null)
            {
                _logger.HealthCheckFailed($"Encrypt phase: {encryptError}");
                return HealthCheckResult.Unhealthy(
                    $"Encryption roundtrip failed during encrypt phase: {encryptError}");
            }

            var encryptedPayload = encryptResult.Match(
                Right: p => p,
                Left: _ => default!);

            // 3. Decrypt and verify
            var decryptResult = await encryptionProvider.DecryptAsync(
                encryptedPayload, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var decryptError = decryptResult.MatchUnsafe<string?>(
                Right: decryptedBytes =>
                {
                    var decrypted = Encoding.UTF8.GetString(decryptedBytes.AsSpan());
                    if (!string.Equals(decrypted, TestPlaintext, StringComparison.Ordinal))
                    {
                        return "Decrypted value does not match original plaintext.";
                    }

                    return null;
                },
                Left: e => e.Message);

            if (decryptError is not null)
            {
                _logger.HealthCheckFailed($"Decrypt phase: {decryptError}");
                return HealthCheckResult.Unhealthy(
                    $"Encryption roundtrip failed during decrypt phase: {decryptError}");
            }

            // All checks passed
            _logger.HealthCheckPassed(encryptedPayload.KeyId, encryptedPayload.Algorithm);

            var data = new Dictionary<string, object>
            {
                ["keyId"] = encryptedPayload.KeyId,
                ["algorithm"] = encryptedPayload.Algorithm
            };

            return HealthCheckResult.Healthy(
                "Message encryption subsystem is healthy. Roundtrip probe passed.",
                data);
        }
        catch (Exception ex)
        {
            _logger.HealthCheckException(ex);
            return HealthCheckResult.Unhealthy(
                $"Message encryption health check failed with exception: {ex.Message}",
                exception: ex);
        }
    }
}
