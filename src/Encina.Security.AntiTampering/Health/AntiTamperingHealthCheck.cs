using Encina.Security.AntiTampering.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.Security.AntiTampering.Health;

/// <summary>
/// Health check that verifies the anti-tampering subsystem is operational by validating
/// key provider availability, nonce store functionality, and signer resolution.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications:
/// <list type="number">
/// <item><description>Resolves <see cref="IKeyProvider"/> from the DI container.</description></item>
/// <item><description>If test keys are configured, verifies at least one key can be retrieved.</description></item>
/// <item><description>Resolves <see cref="IRequestSigner"/> from the DI container.</description></item>
/// <item><description>Resolves <see cref="INonceStore"/> from the DI container.</description></item>
/// <item><description>Performs a roundtrip nonce write/read probe to verify store operation.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="AntiTamperingOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaAntiTampering(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class AntiTamperingHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-antitampering";

    private static readonly string[] DefaultTags = ["encina", "security", "antitampering"];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntiTamperingHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve anti-tampering services.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is null.
    /// </exception>
    public AntiTamperingHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the default tags for the anti-tampering health check.
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
                    "Missing anti-tampering service: IKeyProvider is not registered.");
            }

            // 2. If test keys are configured, verify at least one key resolves
            var options = scopedProvider.GetService<IOptions<AntiTamperingOptions>>();
            string? verifiedKeyId = null;

            if (options?.Value.TestKeys.Count > 0)
            {
                var firstKeyId = options.Value.TestKeys.Keys.First();
                var keyResult = await keyProvider.GetKeyAsync(firstKeyId, cancellationToken)
                    .ConfigureAwait(false);

                var keyError = keyResult.MatchUnsafe<string?>(
                    Right: _ => null,
                    Left: e => e.Message);

                if (keyError is not null)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Key provider failed to retrieve test key '{firstKeyId}': {keyError}");
                }

                verifiedKeyId = firstKeyId;
            }

            // 3. Verify IRequestSigner is resolvable
            var requestSigner = scopedProvider.GetService<IRequestSigner>();

            if (requestSigner is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing anti-tampering service: IRequestSigner is not registered.");
            }

            // 4. Verify INonceStore is resolvable
            var nonceStore = scopedProvider.GetService<INonceStore>();

            if (nonceStore is null)
            {
                return HealthCheckResult.Unhealthy(
                    "Missing anti-tampering service: INonceStore is not registered.");
            }

            // 5. Roundtrip nonce write/read probe
            var probeNonce = $"health-probe-{Guid.NewGuid():N}";
            var probeExpiry = TimeSpan.FromSeconds(30);

            var addResult = await nonceStore.TryAddAsync(probeNonce, probeExpiry, cancellationToken)
                .ConfigureAwait(false);

            if (!addResult)
            {
                return HealthCheckResult.Unhealthy(
                    "Nonce store probe failed: could not add a test nonce.");
            }

            var existsResult = await nonceStore.ExistsAsync(probeNonce, cancellationToken)
                .ConfigureAwait(false);

            if (!existsResult)
            {
                return HealthCheckResult.Unhealthy(
                    "Nonce store probe failed: added nonce was not found on read.");
            }

            // All checks passed
            var data = new Dictionary<string, object>
            {
                ["keyProvider"] = keyProvider.GetType().Name,
                ["requestSigner"] = requestSigner.GetType().Name,
                ["nonceStore"] = nonceStore.GetType().Name,
                ["algorithm"] = (options?.Value.Algorithm ?? HMACAlgorithm.SHA256).ToString()
            };

            if (verifiedKeyId is not null)
            {
                data["verifiedKeyId"] = verifiedKeyId;
            }

            return HealthCheckResult.Healthy(
                "Anti-tampering subsystem is healthy. All services registered and nonce store operational.",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Anti-tampering health check failed with exception: {ex.Message}",
                exception: ex);
        }
    }
}
