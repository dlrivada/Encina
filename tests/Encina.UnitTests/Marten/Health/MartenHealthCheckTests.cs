using Encina.Marten;
using Encina.Marten.Health;
using Encina.Messaging.Health;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.Health;

public sealed class MartenHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDocumentStore _store;

    public MartenHealthCheckTests()
    {
        _store = Substitute.For<IDocumentStore>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IDocumentStore)).Returns(_store);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        MartenHealthCheck.DefaultName.ShouldBe("encina-marten");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-marten" };

        // Act
        var healthCheck = new MartenHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-marten");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new MartenHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(MartenHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new MartenHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("eventsourcing");
        healthCheck.Tags.ShouldContain("marten");
        healthCheck.Tags.ShouldContain("postgresql");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenQueryFails_ReturnsUnhealthy()
    {
        // Arrange
        // We can't easily mock Marten's QueryAsync<T>() chain due to complex LINQ internals
        // Instead, we test by making QuerySession throw an exception
        // Integration tests with a real PostgreSQL database will verify healthy status
        var session = Substitute.For<IQuerySession>();
        session.QueryAsync<int>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<int>>(_ => throw new InvalidOperationException("Query failed"));
        _store.QuerySession().Returns(session);

        var healthCheck = new MartenHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - The base class catches the exception and returns Unhealthy
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreNotAvailable_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IDocumentStore))
            .Returns(_ => throw new InvalidOperationException("Store not configured"));
        var healthCheck = new MartenHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
        result.Description!.ShouldContain("Store not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSessionCreationFails_ReturnsUnhealthy()
    {
        // Arrange
        _store.QuerySession()
            .Returns(_ => throw new InvalidOperationException("Database connection failed"));
        var healthCheck = new MartenHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
        result.Description!.ShouldContain("Database connection failed");
    }
}
