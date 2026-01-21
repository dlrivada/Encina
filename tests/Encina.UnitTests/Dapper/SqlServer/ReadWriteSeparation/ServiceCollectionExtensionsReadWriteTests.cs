using System.Data;
using Encina.Dapper.SqlServer;
using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.ReadWriteSeparation;

/// <summary>
/// Unit tests for ServiceCollectionExtensions related to ReadWriteSeparation.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ServiceCollectionExtensionsReadWriteTests
{
    private const string WriteConnectionString = "Server=primary;Database=test;";
    private const string ReadConnectionString1 = "Server=replica1;Database=test;";
    private const string ReadConnectionString2 = "Server=replica2;Database=test;";

    [Fact]
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersReplicaSelector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersConnectionSelector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersConnectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
            {
                config.UseReadWriteSeparation = true;
                config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
            });

        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetService<IReadWriteConnectionFactory>();
        factory.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithReadWriteSeparation_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithoutReadWriteSeparation_DoesNotRegisterComponents()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithRandomStrategy_RegistersRandomSelector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithLeastConnectionsStrategy_RegistersLeastConnectionsSelector()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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
    public void AddEncinaDapper_WithMultipleReplicas_RegistersAllReplicas()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
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

    [Fact]
    public void AddEncinaDapper_WithNoReplicas_RegistersConnectionSelectorWithFallback()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;"),
            config =>
            {
                config.UseReadWriteSeparation = true;
                config.ReadWriteSeparationOptions.WriteConnectionString = WriteConnectionString;
                // No replicas configured
            });

        var provider = services.BuildServiceProvider();

        // Assert
        var connectionSelector = provider.GetService<IReadWriteConnectionSelector>();
        connectionSelector.ShouldNotBeNull();

        // When no replicas are configured, IReplicaSelector should not be registered
        var replicaSelector = provider.GetService<IReplicaSelector>();
        replicaSelector.ShouldBeNull();
    }

    /// <summary>
    /// Test command for pipeline behavior resolution.
    /// </summary>
    public sealed record TestCommand : ICommand<string>;
}
