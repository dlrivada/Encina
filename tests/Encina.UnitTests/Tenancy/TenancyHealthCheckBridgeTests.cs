using Encina.Tenancy.AspNetCore.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Tests for the bridge that exposes the tenancy <c>IEncinaHealthCheck</c> as an ASP.NET Core health check.
/// </summary>
public sealed class TenancyHealthCheckBridgeTests
{
    [Fact]
    public async Task AddEncinaTenancy_WhenProviderWorks_ReportsHealthy()
    {
        var provider = Substitute.For<ITenantProvider>();
        provider.GetCurrentTenantId().Returns((string?)null);

        var report = await RunAsync(provider);

        var entry = report.Entries["encina-tenancy"];
        entry.Status.ShouldBe(HealthStatus.Healthy);
        entry.Data["has_tenant_context"].ShouldBe(false);
    }

    [Fact]
    public async Task AddEncinaTenancy_WhenProviderThrows_ReportsOnlyTheExceptionType()
    {
        var provider = Substitute.For<ITenantProvider>();
        provider.GetCurrentTenantId().Returns(_ => throw new InvalidOperationException("tenant store at db-secret-host"));

        var report = await RunAsync(provider);

        var entry = report.Entries["encina-tenancy"];
        entry.Status.ShouldBe(HealthStatus.Unhealthy);
        entry.Description!.ShouldContain(nameof(InvalidOperationException));
        entry.Description!.ShouldNotContain("db-secret-host");
        entry.Exception.ShouldBeNull();
    }

    private static async Task<HealthReport> RunAsync(ITenantProvider provider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(provider);
        services.AddLogging();
        services.AddHealthChecks().AddEncinaTenancy();

        await using var serviceProvider = services.BuildServiceProvider();
        return await serviceProvider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
    }
}
