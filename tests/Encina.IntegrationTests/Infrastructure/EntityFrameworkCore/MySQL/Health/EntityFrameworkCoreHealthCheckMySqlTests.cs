using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Health;

/// <summary>
/// MySQL-specific integration tests for <see cref="EntityFrameworkCoreHealthCheck"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class EntityFrameworkCoreHealthCheckMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public EntityFrameworkCoreHealthCheckMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsRunning_ReturnsHealthy()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        var options = new ProviderHealthCheckOptions { Name = "my-custom-efcore-mysql" };
        using var serviceProvider = CreateServiceProvider(context);
        var healthCheck = new EntityFrameworkCoreHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-efcore-mysql");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [SkippableFact]
    public async Task Tags_ContainsExpectedValues()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

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
