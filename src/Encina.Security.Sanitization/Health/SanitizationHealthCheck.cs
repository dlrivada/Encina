using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.Sanitization.Health;

/// <summary>
/// Health check that verifies the sanitization subsystem is operational by validating
/// service availability and performing a sanitization probe.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications in order:
/// <list type="number">
/// <item><description>Resolves <see cref="ISanitizer"/> from the DI container.</description></item>
/// <item><description>Resolves <see cref="IOutputEncoder"/> from the DI container.</description></item>
/// <item><description>Performs a roundtrip sanitization probe with test data.</description></item>
/// <item><description>Performs a roundtrip encoding probe with test data.</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="SanitizationOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaSanitization(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class SanitizationHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-sanitization";

    private static readonly string[] DefaultTags = ["encina", "security", "sanitization", "ready"];

    private const string TestInput = "<script>alert('xss')</script><p>safe</p>";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SanitizationHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve sanitization services.</param>
    public SanitizationHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the default tags for the sanitization health check.
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

            // 1. Verify ISanitizer is resolvable
            var sanitizer = scopedProvider.GetService<ISanitizer>();

            if (sanitizer is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Missing sanitization service: ISanitizer is not registered."));
            }

            // 2. Verify IOutputEncoder is resolvable
            var encoder = scopedProvider.GetService<IOutputEncoder>();

            if (encoder is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Missing sanitization service: IOutputEncoder is not registered."));
            }

            // 3. Sanitization probe — verify that sanitization actually strips dangerous content
            var sanitized = sanitizer.SanitizeHtml(TestInput);

            if (sanitized.Contains("<script>", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Sanitization probe failed: script tags were not removed from test input."));
            }

            // 4. Encoding probe — verify that encoding transforms special characters
            var encoded = encoder.EncodeForHtml("<test>");

            if (string.Equals(encoded, "<test>", StringComparison.Ordinal))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Encoding probe failed: HTML special characters were not encoded."));
            }

            // All checks passed
            var data = new Dictionary<string, object>
            {
                ["sanitizer"] = sanitizer.GetType().Name,
                ["encoder"] = encoder.GetType().Name
            };

            return Task.FromResult(HealthCheckResult.Healthy(
                "Sanitization subsystem is healthy. Sanitizer and encoder probes passed.",
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Sanitization health check failed with exception: {ex.Message}",
                exception: ex));
        }
    }
}
