using Encina.Messaging.Health;
using Encina.Tenancy;
using Encina.Tenancy.Health;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantHealthCheck"/>.
/// </summary>
public sealed class TenantHealthCheckTests
{
    [Fact]
    public void DefaultName_ShouldBeExpected()
    {
        TenantHealthCheck.DefaultName.ShouldBe("encina-tenancy");
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TenantHealthCheck(null!));
    }

    [Fact]
    public void Name_ShouldReturnDefaultName()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        var healthCheck = new TenantHealthCheck(tenantProvider);

        healthCheck.Name.ShouldBe("encina-tenancy");
    }

    [Fact]
    public void Tags_ShouldContainExpectedTags()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        var healthCheck = new TenantHealthCheck(tenantProvider);

        healthCheck.Tags.ShouldContain("tenancy");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WithTenantContext_ReturnsHealthyWithTenantData()
    {
        // Arrange
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        var healthCheck = new TenantHealthCheck(tenantProvider);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("operational");
        result.Data.ShouldContainKey("has_tenant_context");
        result.Data["has_tenant_context"].ShouldBe(true);
        result.Data.ShouldContainKey("tenant_id");
        result.Data["tenant_id"].ShouldBe("tenant-123");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutTenantContext_ReturnsHealthyWithNoTenantData()
    {
        // Arrange
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns((string?)null);
        var healthCheck = new TenantHealthCheck(tenantProvider);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("has_tenant_context");
        result.Data["has_tenant_context"].ShouldBe(false);
        result.Data.ShouldNotContainKey("tenant_id");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProviderThrows_ReturnsUnhealthy()
    {
        // Arrange
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId()
            .Returns<string?>(_ => throw new InvalidOperationException("Provider failure"));
        var healthCheck = new TenantHealthCheck(tenantProvider);

        // Act - base class EncinaHealthCheck catches exceptions
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
