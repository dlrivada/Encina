using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteSeparationHealthCheck"/>.
/// </summary>
public sealed class ReadWriteSeparationHealthCheckTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteSeparationHealthCheck(null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteSeparationHealthCheck(serviceProvider, null!));
    }

    [Fact]
    public void Name_ReturnsDefaultName()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act & Assert
        healthCheck.Name.ShouldBe(ReadWriteSeparationHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act & Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("read-write-separation");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoConnectionSelector_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        // Deliberately not registering IReadWriteConnectionSelector
        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("IReadWriteConnectionSelector is not registered");
    }

    [Fact]
    public async Task CheckHealthAsync_WithConnectionSelectorButNoReplicas_ChecksPrimaryOnly()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("health-check-test-1"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - InMemory database should be "connectable"
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("no replicas configured");
        result.Data.ShouldContainKey("primary");
        result.Data["primary"].ShouldBe("reachable");
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        ReadWriteSeparationHealthCheck.DefaultName.ShouldBe("encina-read-write-separation");
    }

    [Fact]
    public void ImplementsIEncinaHealthCheck()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Assert
        (healthCheck is IEncinaHealthCheck).ShouldBeTrue();
    }

    [Fact]
    public void InheritsFromEncinaHealthCheck()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Assert
        (healthCheck is EncinaHealthCheck).ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ReturnsUnhealthyOrHandlesCancellation()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("health-check-test-cancel"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>());

        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - InMemory db connects instantly, so may be Healthy despite cancellation
        // This test verifies cancellation doesn't throw exceptions
        result.Status.ShouldBeOneOf(HealthStatus.Unhealthy, HealthStatus.Healthy);
    }

    /// <summary>
    /// Simple test DbContext for testing purposes.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }
    }
}
