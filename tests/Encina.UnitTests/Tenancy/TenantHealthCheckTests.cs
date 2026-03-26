using Encina.Tenancy;
using Encina.Tenancy.Health;
using NSubstitute;
using HealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Encina.UnitTests.Tenancy;

public class TenantHealthCheckTests
{
    [Fact]
    public void DefaultName_ShouldBeEncinaTenancy()
    {
        TenantHealthCheck.DefaultName.ShouldBe("encina-tenancy");
    }

    [Fact]
    public void Constructor_NullProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new TenantHealthCheck(null!));
    }

    [Fact]
    public void Name_ShouldMatchDefaultName()
    {
        var provider = Substitute.For<ITenantProvider>();
        var hc = new TenantHealthCheck(provider);
        hc.Name.ShouldBe(TenantHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ShouldContainTenancy()
    {
        var provider = Substitute.For<ITenantProvider>();
        var hc = new TenantHealthCheck(provider);
        hc.Tags.ShouldContain("tenancy");
    }

    [Fact]
    public async Task CheckHealthAsync_NoTenantContext_ReturnsHealthy()
    {
        var provider = Substitute.For<ITenantProvider>();
        provider.GetCurrentTenantId().Returns((string?)null);

        var hc = new TenantHealthCheck(provider);
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
        result.Data.ShouldContainKey("has_tenant_context");
        result.Data["has_tenant_context"].ShouldBe(false);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTenantContext_ReturnsHealthyWithTenantId()
    {
        var provider = Substitute.For<ITenantProvider>();
        provider.GetCurrentTenantId().Returns("tenant-42");

        var hc = new TenantHealthCheck(provider);
        var result = await hc.CheckHealthAsync();

        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
        result.Data["has_tenant_context"].ShouldBe(true);
        result.Data["tenant_id"].ShouldBe("tenant-42");
    }
}
