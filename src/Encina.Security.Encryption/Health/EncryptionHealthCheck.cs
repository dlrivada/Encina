using System.Text;
using Encina.Security.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.Encryption.Health;

/// <summary>
/// Health check that verifies the encryption subsystem is operational by validating
/// key provider availability and performing a roundtrip encrypt/decrypt probe.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications in order:
/// <list type="number">
/// <item><description>Resolves <see cref="IKeyProvider"/> from the DI container.</description></item>
/// <item><description>Calls <see cref="IKeyProvider.GetCurrentKeyIdAsync"/> to ensure a current key is configured.</description></item>
/// <item><description>Resolves <see cref="IFieldEncryptor"/> from the DI container.</description></item>
/// <item><description>Performs a roundtrip encrypt/decrypt cycle with test data to verify cryptographic operations work end-to-end.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="EncryptionOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaEncryption(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class EncryptionHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-encryption";

    private static readonly string[] DefaultTags = ["encina", "encryption", "ready"];

    private const string TestPlaintext = "encina-health-probe";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve encryption services.</param>
    public EncryptionHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the default tags for the encryption health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // 1. Verify IKeyProvider is resolvable
            var keyProvider = scopedProvider.GetService<IKeyProvider>();

            if (keyProvider is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing encryption service: IKeyProvider is not registered.");
            }

            // 2. Verify current key ID is available
            var keyIdResult = await keyProvider.GetCurrentKeyIdAsync(cancellationToken)
                .ConfigureAwait(false);

            var keyIdError = keyIdResult.MatchUnsafe<string?>(
                Right: _ => null,
                Left: e => e.Message);

            if (keyIdError is not null)
            {
                return HealthCheckResult.Unhealthy(
                    $"Encryption key provider has no current key configured: {keyIdError}");
            }

            // 3. Verify IFieldEncryptor is resolvable
            var fieldEncryptor = scopedProvider.GetService<IFieldEncryptor>();

            if (fieldEncryptor is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing encryption service: IFieldEncryptor is not registered.");
            }

            // 4. Roundtrip encrypt/decrypt probe
            var encryptionContext = new EncryptionContext
            {
                Purpose = "health-check"
            };

            var encryptResult = await fieldEncryptor.EncryptStringAsync(
                TestPlaintext, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var encryptError = encryptResult.MatchUnsafe<string?>(
                Right: _ => null,
                Left: e => e.Message);

            if (encryptError is not null)
            {
                return HealthCheckResult.Unhealthy(
                    $"Encryption roundtrip failed during encrypt phase: {encryptError}");
            }

            var encryptedValue = encryptResult.Match(
                Right: v => v,
                Left: _ => default);

            var decryptResult = await fieldEncryptor.DecryptStringAsync(
                encryptedValue, encryptionContext, cancellationToken)
                .ConfigureAwait(false);

            var decryptError = decryptResult.MatchUnsafe<string?>(
                Right: decrypted =>
                {
                    if (!string.Equals(decrypted, TestPlaintext, StringComparison.Ordinal))
                    {
                        return "Decrypted value does not match original plaintext.";
                    }

                    return null;
                },
                Left: e => e.Message);

            if (decryptError is not null)
            {
                return HealthCheckResult.Unhealthy(
                    $"Encryption roundtrip failed during decrypt phase: {decryptError}");
            }

            // All checks passed
            var currentKeyId = keyIdResult.Match(
                Right: id => id,
                Left: _ => "unknown");

            var data = new Dictionary<string, object>
            {
                ["currentKeyId"] = currentKeyId,
                ["algorithm"] = nameof(EncryptionAlgorithm.Aes256Gcm)
            };

            return HealthCheckResult.Healthy(
                "Encryption subsystem is healthy. Key provider and roundtrip probe passed.",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Encryption health check failed with exception: {ex.Message}",
                exception: ex);
        }
    }
}
