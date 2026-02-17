using Encina.Messaging.Health;

namespace Encina.Tenancy.Health;

/// <summary>
/// Health check that verifies tenant resolution infrastructure is operational.
/// </summary>
/// <remarks>
/// <para>
/// Validates that the tenant provider is resolvable and can execute
/// without errors. Since health checks run outside a tenant context,
/// a <c>null</c> tenant ID is expected and considered healthy.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via ASP.NET Core health checks:
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaTenancyHealthCheck();
/// </code>
/// </example>
public sealed class TenantHealthCheck : EncinaHealthCheck
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-tenancy";

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantHealthCheck"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to check.</param>
    public TenantHealthCheck(ITenantProvider tenantProvider)
        : base(DefaultName, ["tenancy", "ready"])
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Health checks run outside tenant context, so null tenant ID is expected.
        // We verify the provider is resolvable and doesn't throw.
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var data = new Dictionary<string, object>
        {
            ["has_tenant_context"] = tenantId is not null
        };

        if (tenantId is not null)
        {
            data["tenant_id"] = tenantId;
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Tenant resolution infrastructure is operational.", data: data));
    }
}
