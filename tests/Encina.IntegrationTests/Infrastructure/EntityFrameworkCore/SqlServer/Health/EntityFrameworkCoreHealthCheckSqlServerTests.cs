using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Health;

/// <summary>
/// SQL Server-specific integration tests for <see cref="EntityFrameworkCoreHealthCheck"/>.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class EntityFrameworkCoreHealthCheckSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public EntityFrameworkCoreHealthCheckSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
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

        var options = new ProviderHealthCheckOptions { Name = "my-custom-efcore-sqlserver" };
        using var serviceProvider = CreateServiceProvider(context);
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-efcore-sqlserver");
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
