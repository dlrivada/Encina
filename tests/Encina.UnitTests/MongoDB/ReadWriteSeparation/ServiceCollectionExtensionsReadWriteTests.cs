using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class ServiceCollectionExtensionsReadWriteTests
{
    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_RegistersCollectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IReadWriteMongoCollectionFactory>();
        factory.ShouldNotBeNull();
        factory.ShouldBeOfType<ReadWriteMongoCollectionFactory>();
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationDisabled_DoesNotRegisterCollectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = false;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IReadWriteMongoCollectionFactory>();
        factory.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<TestQuery, string>>();
        behaviors.ShouldContain(b => b.GetType().IsGenericType &&
            b.GetType().GetGenericTypeDefinition() == typeof(ReadWriteRoutingPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();
        var cluster = Substitute.For<ICluster>();
        mongoClient.Cluster.Returns(cluster);

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldContain(h => h.GetType() == typeof(ReadWriteMongoHealthCheck));
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Secondary;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
            options.ReadWriteSeparationOptions.ValidateOnStartup = true;
            options.ReadWriteSeparationOptions.FallbackToPrimaryOnNoSecondaries = false;
            options.ReadWriteSeparationOptions.MaxStaleness = TimeSpan.FromMinutes(5);
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var rwOptions = provider.GetRequiredService<IOptions<MongoReadWriteSeparationOptions>>().Value;

        rwOptions.ReadPreference.ShouldBe(MongoReadPreference.Secondary);
        rwOptions.ReadConcern.ShouldBe(MongoReadConcern.Local);
        rwOptions.ValidateOnStartup.ShouldBeTrue();
        rwOptions.FallbackToPrimaryOnNoSecondaries.ShouldBeFalse();
        rwOptions.MaxStaleness.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaMongoDB_WithConnectionString_AndReadWriteSeparationEnabled_RegistersCollectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IReadWriteMongoCollectionFactory>();
        factory.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMongoDB_CollectionFactoryIsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IReadWriteMongoCollectionFactory));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMongoDB_HealthCheckIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ImplementationType == typeof(ReadWriteMongoHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMongoDB_PipelineBehaviorIsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var mongoClient = Substitute.For<IMongoClient>();

        // Act
        services.AddEncinaMongoDB(mongoClient, options =>
        {
            options.DatabaseName = "TestDb";
            options.UseReadWriteSeparation = true;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ReadWriteRoutingPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    // Test types
    public sealed record TestQuery : IQuery<string>;
}
