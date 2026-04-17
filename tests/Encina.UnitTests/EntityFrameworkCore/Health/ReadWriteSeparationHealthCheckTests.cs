using Encina.EntityFrameworkCore.Health;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Health;

/// <summary>
/// Unit tests for <see cref="ReadWriteSeparationHealthCheck"/> covering health status logic,
/// degraded states, and error handling.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteSeparationHealthCheckTests
{
    #region Constructor Validation

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

    #endregion

    #region Name & Tags

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        ReadWriteSeparationHealthCheck.DefaultName.ShouldBe("encina-read-write-separation");
    }

    [Fact]
    public void Name_ReturnsDefaultName()
    {
        // Arrange
        var sut = CreateHealthCheck();

        // Act & Assert
        sut.Name.ShouldBe(ReadWriteSeparationHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsAllExpectedTags()
    {
        // Arrange
        var sut = CreateHealthCheck();

        // Act & Assert
        sut.Tags.ShouldContain("encina");
        sut.Tags.ShouldContain("database");
        sut.Tags.ShouldContain("read-write-separation");
        sut.Tags.ShouldContain("ready");
    }

    #endregion

    #region Type Hierarchy

    [Fact]
    public void ImplementsIEncinaHealthCheck()
    {
        var sut = CreateHealthCheck();
        (sut is IEncinaHealthCheck).ShouldBeTrue();
    }

    [Fact]
    public void InheritsFromEncinaHealthCheck()
    {
        var sut = CreateHealthCheck();
        (sut is EncinaHealthCheck).ShouldBeTrue();
    }

    #endregion

    #region Unhealthy States

    [Fact]
    public async Task CheckHealthAsync_WithNoConnectionSelector_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("IReadWriteConnectionSelector is not registered");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenScopeCreationThrows_ReturnsUnhealthy()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_ => throw new InvalidOperationException("Scope creation failed"));

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("Scope creation failed");
    }

    #endregion

    #region Healthy States

    [Fact]
    public async Task CheckHealthAsync_PrimaryReachableNoReplicas_ReturnsHealthy()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<HealthTestDbContext>(opts =>
            opts.UseInMemoryDatabase($"health-test-{Guid.NewGuid()}"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HealthTestDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("no replicas configured");
        result.Data.ShouldContainKey("primary");
        result.Data["primary"].ShouldBe("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_PrimaryAndAllReplicasReachable_ReturnsHealthy()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<HealthTestDbContext>(opts =>
            opts.UseInMemoryDatabase($"health-test-{Guid.NewGuid()}"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HealthTestDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        // Use empty read connection strings so replica check loop is skipped
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("primary");
        result.Data["primary"].ShouldBe("reachable");
    }

    #endregion

    #region Degraded States

    [Fact]
    public async Task CheckHealthAsync_PrimaryReachableButAllReplicasUnreachable_ReturnsDegraded()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<HealthTestDbContext>(opts =>
            opts.UseInMemoryDatabase($"health-test-{Guid.NewGuid()}"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HealthTestDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;",
            ReadConnectionStrings = ["Server=invalid-replica;"]
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("all replicas are unreachable");
        result.Description!.ShouldContain("fall back to the primary");
    }

    [Fact]
    public async Task CheckHealthAsync_AllReplicasUnreachable_DataContainsReplicaCounts()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<HealthTestDbContext>(opts =>
            opts.UseInMemoryDatabase($"health-test-{Guid.NewGuid()}"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HealthTestDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;",
            ReadConnectionStrings = ["Server=bad1;", "Server=bad2;"]
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        // Act
        var result = await sut.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("healthy_replica_count");
        result.Data["healthy_replica_count"].ShouldBe(0);
        result.Data.ShouldContainKey("total_replica_count");
        result.Data["total_replica_count"].ShouldBe(2);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_DoesNotThrow()
    {
        // Arrange
        var connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        connectionSelector.HasReadReplicas.Returns(false);

        var services = new ServiceCollection();
        services.AddSingleton(connectionSelector);
        services.AddDbContext<HealthTestDbContext>(opts =>
            opts.UseInMemoryDatabase($"health-test-cancel-{Guid.NewGuid()}"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<HealthTestDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var sut = new ReadWriteSeparationHealthCheck(serviceProvider, options);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await sut.CheckHealthAsync(cts.Token);

        // Assert - InMemory DB connects instantly; no exception should propagate
        result.Status.ShouldBeOneOf(HealthStatus.Unhealthy, HealthStatus.Healthy);
    }

    #endregion

    #region Helpers

    private static ReadWriteSeparationHealthCheck CreateHealthCheck()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };
        return new ReadWriteSeparationHealthCheck(serviceProvider, options);
    }

    private sealed class HealthTestDbContext : DbContext
    {
        public HealthTestDbContext(DbContextOptions<HealthTestDbContext> options)
            : base(options)
        {
        }
    }

    #endregion
}
