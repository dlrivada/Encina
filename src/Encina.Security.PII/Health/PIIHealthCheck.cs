using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.PII.Health;

/// <summary>
/// Health check that verifies the PII masking subsystem is operational by validating
/// service availability and performing a masking probe.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications in order:
/// <list type="number">
/// <item><description>Resolves <see cref="IPIIMasker"/> from the DI container.</description></item>
/// <item><description>Verifies built-in masking strategies are resolvable.</description></item>
/// <item><description>Performs a masking probe with a dummy object to confirm end-to-end functionality.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="PIIOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaPII(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class PIIHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina_pii";

    private static readonly string[] DefaultTags = ["pii", "security", "ready"];

    private static readonly Type[] RequiredStrategies =
    [
        typeof(EmailMaskingStrategy),
        typeof(PhoneMaskingStrategy),
        typeof(CreditCardMaskingStrategy),
        typeof(SSNMaskingStrategy),
        typeof(NameMaskingStrategy),
        typeof(AddressMaskingStrategy),
        typeof(DateOfBirthMaskingStrategy),
        typeof(IPAddressMaskingStrategy),
        typeof(FullMaskingStrategy)
    ];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIIHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve PII services.</param>
    public PIIHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the default tags for the PII health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // 1. Verify IPIIMasker is resolvable
            var masker = scopedProvider.GetService<IPIIMasker>();

            if (masker is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Missing PII service: IPIIMasker is not registered."));
            }

            // 2. Verify built-in strategies are resolvable
            var resolvedStrategies = new List<string>();
            var missingStrategies = new List<string>();

            foreach (var strategyType in RequiredStrategies)
            {
                var strategy = scopedProvider.GetService(strategyType);
                if (strategy is not null)
                {
                    resolvedStrategies.Add(strategyType.Name);
                }
                else
                {
                    missingStrategies.Add(strategyType.Name);
                }
            }

            if (missingStrategies.Count == RequiredStrategies.Length)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"No masking strategies could be resolved. Missing: {string.Join(", ", missingStrategies)}"));
            }

            // 3. Masking probe — verify that masking actually transforms PII
            var probeResult = RunMaskingProbe(masker);

            if (!probeResult.Success)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Masking probe failed: {probeResult.ErrorMessage}"));
            }

            // Build result metadata
            var data = new Dictionary<string, object>
            {
                ["masker"] = masker.GetType().Name,
                ["strategies_resolved"] = resolvedStrategies.Count,
                ["strategies_total"] = RequiredStrategies.Length,
                ["version"] = "1.0"
            };

            // Degraded if some optional strategies are missing
            if (missingStrategies.Count > 0)
            {
                data["missing_strategies"] = string.Join(", ", missingStrategies);

                return Task.FromResult(HealthCheckResult.Degraded(
                    $"PII subsystem is operational but {missingStrategies.Count} strategies could not be resolved.",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "PII masking subsystem is healthy. All strategies and masking probes passed.",
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"PII health check failed with exception: {ex.Message}",
                exception: ex));
        }
    }

    private static (bool Success, string? ErrorMessage) RunMaskingProbe(IPIIMasker masker)
    {
        try
        {
            var probe = new HealthProbeDto { Email = "test@example.com" };
            var masked = masker.MaskObject(probe);

            if (string.Equals(masked.Email, probe.Email, StringComparison.Ordinal))
            {
                return (false, "Email was not masked in probe object.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Masking probe threw exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal DTO used exclusively for health check probes.
    /// </summary>
    private sealed class HealthProbeDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;
    }
}
