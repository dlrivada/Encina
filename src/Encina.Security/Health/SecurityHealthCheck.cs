using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Security.Health;

/// <summary>
/// Health check that verifies all required security services are registered and resolvable.
/// </summary>
/// <remarks>
/// <para>
/// This health check resolves the core security services from the DI container to verify
/// they are properly configured:
/// <list type="bullet">
/// <item><description><see cref="ISecurityContextAccessor"/> — context management</description></item>
/// <item><description><see cref="IPermissionEvaluator"/> — permission evaluation</description></item>
/// <item><description><see cref="IResourceOwnershipEvaluator"/> — ownership verification</description></item>
/// </list>
/// </para>
/// <para>
/// Enable via <see cref="SecurityOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaSecurity(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class SecurityHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-security";

    private static readonly string[] DefaultTags = ["encina", "security", "ready"];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve security services.</param>
    public SecurityHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the default tags for the security health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var missingServices = new List<string>();

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        if (scopedProvider.GetService<ISecurityContextAccessor>() is null)
        {
            missingServices.Add(nameof(ISecurityContextAccessor));
        }

        if (scopedProvider.GetService<IPermissionEvaluator>() is null)
        {
            missingServices.Add(nameof(IPermissionEvaluator));
        }

        if (scopedProvider.GetService<IResourceOwnershipEvaluator>() is null)
        {
            missingServices.Add(nameof(IResourceOwnershipEvaluator));
        }

        if (missingServices.Count > 0)
        {
            var description = $"Missing security services: {string.Join(", ", missingServices)}";
            return Task.FromResult(HealthCheckResult.Unhealthy(description));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All security services are registered and resolvable."));
    }
}
