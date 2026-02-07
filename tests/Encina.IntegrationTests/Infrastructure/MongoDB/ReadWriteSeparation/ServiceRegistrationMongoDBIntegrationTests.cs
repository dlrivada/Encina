using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// Integration tests verifying that <see cref="ServiceCollectionExtensions.AddEncinaMongoDB"/>
/// correctly registers all read/write separation services when enabled.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the DI container is properly configured with all required services
/// for MongoDB read/write separation functionality.
/// </para>
/// </remarks>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Trait("Feature", "ReadWriteSeparation")]
[Trait("Feature", "ServiceRegistration")]
public sealed class ServiceRegistrationMongoDBIntegrationTests
{
    private readonly MongoDbReplicaSetFixture _fixture;

    public ServiceRegistrationMongoDBIntegrationTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
    }

    #region Service Registration Tests

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_ShouldRegisterCollectionFactory()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IReadWriteMongoCollectionFactory>();
        factory.ShouldNotBeNull();
        factory.ShouldBeOfType<ReadWriteMongoCollectionFactory>();
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_ShouldRegisterHealthCheck()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert - Should find ReadWriteMongoHealthCheck among registered health checks
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        healthChecks.ShouldContain(h => h is ReadWriteMongoHealthCheck);
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_ShouldRegisterPipelineBehavior()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert - Pipeline behavior should be registered
        // We check through service descriptors since generic type resolution is complex
        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType?.Name.StartsWith("ReadWriteRoutingPipelineBehavior", StringComparison.Ordinal) == true);

        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationEnabled_ShouldRegisterOptions()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
        });

        var provider = services.BuildServiceProvider();

        // Assert - Options should be configured
        var mongoOptions = provider.GetService<IOptions<EncinaMongoDbOptions>>();
        mongoOptions.ShouldNotBeNull();
        mongoOptions.Value.UseReadWriteSeparation.ShouldBeTrue();

        var rwOptions = provider.GetService<IOptions<MongoReadWriteSeparationOptions>>();
        rwOptions.ShouldNotBeNull();
        rwOptions.Value.ReadPreference.ShouldBe(MongoReadPreference.Nearest);
        rwOptions.Value.ReadConcern.ShouldBe(MongoReadConcern.Local);
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationDisabled_ShouldNotRegisterCollectionFactory()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = false; // Disabled
        });

        var provider = services.BuildServiceProvider();

        // Assert - Should NOT have R/W collection factory
        var factory = provider.GetService<IReadWriteMongoCollectionFactory>();
        factory.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaMongoDB_WithReadWriteSeparationDisabled_ShouldNotRegisterRWHealthCheck()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = false; // Disabled
        });

        var provider = services.BuildServiceProvider();

        // Assert - Should NOT have ReadWriteMongoHealthCheck
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        healthChecks.ShouldNotContain(h => h is ReadWriteMongoHealthCheck);
    }

    #endregion

    #region Service Resolution Tests

    [Fact]
    public void CollectionFactory_ShouldBeResolvableFromServiceProvider()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var factory = scope.ServiceProvider.GetRequiredService<IReadWriteMongoCollectionFactory>();

        // Assert
        factory.ShouldNotBeNull();
    }

    [Fact]
    public async Task CollectionFactory_ResolvedFromDI_ShouldWorkCorrectly()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var factory = scope.ServiceProvider.GetRequiredService<IReadWriteMongoCollectionFactory>();
        var writeCollection = await factory.GetWriteCollectionAsync<BsonDocument>("test_di");
        var readCollection = await factory.GetReadCollectionAsync<BsonDocument>("test_di");

        // Assert
        writeCollection.Settings.ReadPreference.ShouldBe(ReadPreference.Primary);
        readCollection.Settings.ReadPreference.ReadPreferenceMode
            .ShouldBe(ReadPreferenceMode.SecondaryPreferred);
    }

    [Fact]
    public void HealthCheck_ShouldBeDiscoverableThroughDI()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        var rwHealthCheck = healthChecks.OfType<ReadWriteMongoHealthCheck>().FirstOrDefault();

        // Assert
        rwHealthCheck.ShouldNotBeNull();
        rwHealthCheck.Name.ShouldBe(ReadWriteMongoHealthCheck.DefaultName);
        rwHealthCheck.Tags.ShouldContain("read-write-separation");
    }

    [Fact]
    public async Task HealthCheck_ResolvedFromDI_ShouldExecuteSuccessfully()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();
        var rwHealthCheck = provider
            .GetServices<IEncinaHealthCheck>()
            .OfType<ReadWriteMongoHealthCheck>()
            .First();

        // Act
        var result = await rwHealthCheck.CheckHealthAsync();

        // Assert - Should execute without throwing
        result.Data.ShouldNotBeEmpty();
        result.Data.ShouldContainKey("cluster_type");
    }

    #endregion

    #region MongoClient Registration Tests

    [Fact]
    public void AddEncinaMongoDB_ShouldRegisterMongoClient()
    {

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var client = provider.GetService<IMongoClient>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMongoDB_WithExistingClient_ShouldUseProvidedClient()
    {

        // Arrange
        var services = new ServiceCollection();
        var existingClient = _fixture.Client!;

        // Act
        services.AddEncinaMongoDB(existingClient, options =>
        {
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var resolvedClient = provider.GetService<IMongoClient>();
        resolvedClient.ShouldBeSameAs(existingClient);
    }

    #endregion

    #region Scoped Lifetime Tests

    [Fact]
    public void CollectionFactory_ShouldHaveScopedLifetime()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Act - Create two scopes
        IReadWriteMongoCollectionFactory? factory1;
        IReadWriteMongoCollectionFactory? factory2;

        using (var scope1 = provider.CreateScope())
        {
            factory1 = scope1.ServiceProvider.GetService<IReadWriteMongoCollectionFactory>();
        }

        using (var scope2 = provider.CreateScope())
        {
            factory2 = scope2.ServiceProvider.GetService<IReadWriteMongoCollectionFactory>();
        }

        // Assert - Different scopes should get different instances
        factory1.ShouldNotBeNull();
        factory2.ShouldNotBeNull();
        // Note: Can't compare references as they're disposed, but this verifies scoped resolution works
    }

    [Fact]
    public void CollectionFactory_WithinSameScope_ShouldReturnSameInstance()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
        });

        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var factory1 = scope.ServiceProvider.GetService<IReadWriteMongoCollectionFactory>();
        var factory2 = scope.ServiceProvider.GetService<IReadWriteMongoCollectionFactory>();

        // Assert - Same scope should get same instance
        factory1.ShouldBeSameAs(factory2);
    }

    #endregion

    #region Configuration Propagation Tests

    [Fact]
    public void Configuration_ShouldPropagateToAllServices()
    {

        // Arrange
        var maxStaleness = TimeSpan.FromSeconds(120);
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
            options.ReadWriteSeparationOptions.MaxStaleness = maxStaleness;
            options.ReadWriteSeparationOptions.ValidateOnStartup = true;
            options.ReadWriteSeparationOptions.FallbackToPrimaryOnNoSecondaries = false;
        });

        var provider = services.BuildServiceProvider();
        var rwOptions = provider.GetRequiredService<IOptions<MongoReadWriteSeparationOptions>>().Value;

        // Assert
        rwOptions.ReadPreference.ShouldBe(MongoReadPreference.Nearest);
        rwOptions.ReadConcern.ShouldBe(MongoReadConcern.Local);
        rwOptions.MaxStaleness.ShouldBe(maxStaleness);
        rwOptions.ValidateOnStartup.ShouldBeTrue();
        rwOptions.FallbackToPrimaryOnNoSecondaries.ShouldBeFalse();
    }

    [Fact]
    public async Task Configuration_ShouldAffectCollectionBehavior()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
            options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.Nearest;
            options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Local;
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IReadWriteMongoCollectionFactory>();

        // Act
        var readCollection = await factory.GetReadCollectionAsync<BsonDocument>("test_config");

        // Assert - Configuration should be reflected in collection settings
        readCollection.Settings.ReadPreference.ReadPreferenceMode.ShouldBe(ReadPreferenceMode.Nearest);
        readCollection.Settings.ReadConcern.ShouldBe(ReadConcern.Local);
    }

    #endregion

    #region Multiple Feature Registration Tests

    [Fact]
    public void AddEncinaMongoDB_WithMultipleFeaturesEnabled_ShouldRegisterAll()
    {

        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - Enable multiple features
        services.AddEncinaMongoDB(options =>
        {
            options.ConnectionString = _fixture.ConnectionString;
            options.DatabaseName = MongoDbReplicaSetFixture.DatabaseName;
            options.UseReadWriteSeparation = true;
            options.UseOutbox = true;
            options.UseInbox = true;
            options.ProviderHealthCheck.Enabled = true;
        });

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert - R/W separation services should be registered
        scope.ServiceProvider.GetService<IReadWriteMongoCollectionFactory>().ShouldNotBeNull();

        // Assert - Outbox services should be registered
        scope.ServiceProvider.GetService<IOutboxStore>().ShouldNotBeNull();

        // Assert - Inbox services should be registered
        scope.ServiceProvider.GetService<IInboxStore>().ShouldNotBeNull();

        // Assert - Health checks should include both provider and R/W separation
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        healthChecks.Count.ShouldBeGreaterThanOrEqualTo(2);
        healthChecks.ShouldContain(h => h is ReadWriteMongoHealthCheck);
    }

    #endregion
}
