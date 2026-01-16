using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Health;

public sealed class EntityFrameworkCoreHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _scopeProvider;
    private readonly DbContext _dbContext;
    private readonly DatabaseFacade _database;

    public EntityFrameworkCoreHealthCheckTests()
    {
        _dbContext = Substitute.For<DbContext>();
        _database = Substitute.For<DatabaseFacade>(_dbContext);
        _dbContext.Database.Returns(_database);

        _scope = Substitute.For<IServiceScope>();
        _scopeProvider = Substitute.For<IServiceProvider>();
        _scope.ServiceProvider.Returns(_scopeProvider);
        _scopeProvider.GetService(typeof(DbContext)).Returns(_dbContext);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_scope);

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        EntityFrameworkCoreHealthCheck.DefaultName.ShouldBe("encina-efcore");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-efcore" };

        // Act
        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-efcore");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(EntityFrameworkCoreHealthCheck.DefaultName);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCanConnect_ReturnsHealthy()
    {
        // Arrange
        _database.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCannotConnect_ReturnsUnhealthy()
    {
        // Arrange
        _database.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(false);

        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _database.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new InvalidOperationException("Connection error"));

        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Connection error");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDbContextNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        _scopeProvider.GetService(typeof(DbContext))
            .Returns(_ => throw new InvalidOperationException("DbContext not registered"));

        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_RespectsTimeout()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Timeout = TimeSpan.FromMilliseconds(100) };

        _database.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), callInfo.Arg<CancellationToken>());
                return true;
            });

        var healthCheck = new EntityFrameworkCoreHealthCheck(_serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }
}
