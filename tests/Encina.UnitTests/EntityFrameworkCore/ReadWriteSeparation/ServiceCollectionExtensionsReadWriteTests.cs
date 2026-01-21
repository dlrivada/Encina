using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Health;
using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.ReadWriteSeparation;

/// <summary>
/// Unit tests for ServiceCollectionExtensions related to ReadWriteSeparation.
/// </summary>
public sealed class ServiceCollectionExtensionsReadWriteTests
{
    private const string WriteConnectionString = "Server=primary;Database=test;";
    private const string ReadConnectionString1 = "Server=replica1;Database=test;";
    private const string ReadConnectionString2 = "Server=replica2;Database=test;";

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-options"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<ReadWriteSeparationOptions>();
        options.ShouldNotBeNull();
        options.WriteConnectionString.ShouldBe(WriteConnectionString);
        options.ReadConnectionStrings.ShouldContain(ReadConnectionString1);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersReplicaSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-selector"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
            config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.RoundRobin;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var selector = provider.GetService<IReplicaSelector>();
        selector.ShouldNotBeNull();
        (selector is RoundRobinReplicaSelector).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersConnectionSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-connection-selector"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var connectionSelector = provider.GetService<IReadWriteConnectionSelector>();
        connectionSelector.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersDbContextFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-factory"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetService<IReadWriteDbContextFactory<TestDbContext>>();
        factory.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-behavior"));
        services.AddLogging();

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<TestCommand, string>>();
        behaviors.ShouldContain(b => b is ReadWriteRoutingPipelineBehavior<TestCommand, string>);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithReadWriteSeparation_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-health"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldContain(h => h is ReadWriteSeparationHealthCheck);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithoutReadWriteSeparation_DoesNotRegisterComponents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-no-readwrite"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseTransactions = true;
            // UseReadWriteSeparation = false (default)
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<ReadWriteSeparationOptions>();
        options.ShouldBeNull();

        var selector = provider.GetService<IReplicaSelector>();
        selector.ShouldBeNull();

        var connectionSelector = provider.GetService<IReadWriteConnectionSelector>();
        connectionSelector.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithRandomStrategy_RegistersRandomSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-random"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
            config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.Random;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var selector = provider.GetService<IReplicaSelector>();
        selector.ShouldNotBeNull();
        (selector is RandomReplicaSelector).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithLeastConnectionsStrategy_RegistersLeastConnectionsSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-least-connections"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
            config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.LeastConnections;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var selector = provider.GetService<IReplicaSelector>();
        selector.ShouldNotBeNull();
        (selector is LeastConnectionsReplicaSelector).ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_WithMultipleReplicas_RegistersAllReplicas()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase("test-multi-replicas"));

        // Act
        services.AddEncinaEntityFrameworkCore<TestDbContext>(config =>
        {
            config.UseReadWriteSeparation = true;
            config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString1);
            config.ReadWriteSeparationOptions.ReadConnectionStrings.Add(ReadConnectionString2);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<ReadWriteSeparationOptions>();
        options.ShouldNotBeNull();
        options.ReadConnectionStrings.Count.ShouldBe(2);
        options.ReadConnectionStrings.ShouldContain(ReadConnectionString1);
        options.ReadConnectionStrings.ShouldContain(ReadConnectionString2);
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

    /// <summary>
    /// Test command for pipeline behavior resolution.
    /// </summary>
    public sealed record TestCommand : ICommand<string>;
}
