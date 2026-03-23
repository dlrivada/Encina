using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Diagnostics;
using Encina.Compliance.Attestation.Model;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Attestation.Health;

/// <summary>
/// Health check that verifies the configured attestation provider is operational
/// by performing a lightweight attest/verify round-trip.
/// </summary>
internal sealed class AttestationHealthCheck : IHealthCheck
{
    /// <summary>Default health check name.</summary>
    public const string DefaultName = "encina-attestation";

    /// <summary>Tags for the health check.</summary>
    public static readonly string[] Tags = ["compliance", "attestation"];

    private readonly IAuditAttestationProvider _provider;
    private readonly ILogger<AttestationHealthCheck> _logger;

    public AttestationHealthCheck(
        IAuditAttestationProvider provider,
        ILogger<AttestationHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(logger);
        _provider = provider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var probe = new AuditRecord
            {
                RecordId = Guid.NewGuid(),
                RecordType = "HealthCheck",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                SerializedContent = "{\"probe\":true}"
            };

            var attestResult = await _provider.AttestAsync(probe, cancellationToken);

            return attestResult.Match(
                Right: receipt =>
                {
                    AttestationLogMessages.HealthCheckCompleted(_logger, "Healthy", _provider.ProviderName);
                    return HealthCheckResult.Healthy(
                        $"Provider '{_provider.ProviderName}' operational.",
                        new Dictionary<string, object> { ["provider"] = _provider.ProviderName });
                },
                Left: error =>
                {
                    AttestationLogMessages.HealthCheckCompleted(_logger, "Unhealthy", _provider.ProviderName);
                    return HealthCheckResult.Unhealthy(
                        $"Provider '{_provider.ProviderName}' failed: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            AttestationLogMessages.HealthCheckCompleted(_logger, "Unhealthy", _provider.ProviderName);
            return HealthCheckResult.Unhealthy(
                $"Provider '{_provider.ProviderName}' threw an exception.", ex);
        }
    }
}
