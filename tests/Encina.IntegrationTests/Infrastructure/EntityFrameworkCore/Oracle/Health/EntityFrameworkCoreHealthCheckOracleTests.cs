using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Oracle.Health;

/// <summary>
/// Oracle-specific integration tests for <see cref="EntityFrameworkCoreHealthCheck"/>.
/// Uses real Oracle database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
[Collection("EFCore-Oracle")]
public sealed class EntityFrameworkCoreHealthCheckOracleTests : IAsyncLifetime
{
    private readonly EFCoreOracleFixture _fixture;

    public EntityFrameworkCoreHealthCheckOracleTests(EFCoreOracleFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        using var serviceProvider = CreateServiceProvider(context);
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        var options = new ProviderHealthCheckOptions { Name = "my-custom-efcore-oracle" };
        using var serviceProvider = CreateServiceProvider(context);
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-efcore-oracle");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Tags_ContainsExpectedValues()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        using var serviceProvider = CreateServiceProvider(context);
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("efcore");
        healthCheck.Tags.ShouldContain("ready");
    }

    private static ServiceProvider CreateServiceProvider(DbContext context)
    {
        var services = new ServiceCollection();
        services.AddScoped<DbContext>(_ => context);
        return services.BuildServiceProvider();
    }
}
