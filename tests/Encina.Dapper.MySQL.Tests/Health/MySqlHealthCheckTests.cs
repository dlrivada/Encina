using System.Data;
using Encina.Dapper.MySQL.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.Dapper.MySQL.Tests.Health;

public sealed class MySqlHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _scopeProvider;
    private readonly IDbConnection _connection;

    public MySqlHealthCheckTests()
    {
        _connection = Substitute.For<IDbConnection>();

        _scope = Substitute.For<IServiceScope>();
        _scopeProvider = Substitute.For<IServiceProvider>();
        _scope.ServiceProvider.Returns(_scopeProvider);
        _scopeProvider.GetService(typeof(IDbConnection)).Returns(_connection);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_scope);

        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        MySqlHealthCheck.DefaultName.ShouldBe("encina-mysql");
    }

    [Fact]
    public void Constructor_SetsDefaultName()
    {
        // Act
        var healthCheck = new MySqlHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(MySqlHealthCheck.DefaultName);
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-mysql" };

        // Act
        var healthCheck = new MySqlHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-mysql");
    }

    [Fact]
    public void Tags_ContainsDatabaseTags()
    {
        // Arrange
        var healthCheck = new MySqlHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        _scopeProvider.GetService(typeof(IDbConnection))
            .Returns(_ => throw new InvalidOperationException("IDbConnection not registered"));

        var healthCheck = new MySqlHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
