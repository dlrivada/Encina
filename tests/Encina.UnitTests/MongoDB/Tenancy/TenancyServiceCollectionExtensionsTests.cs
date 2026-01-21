using Encina.DomainModeling;
using Encina.MongoDB;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenancyServiceCollectionExtensions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenancyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMongoDBWithTenancy_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required dependencies
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Substitute.For<IMongoClient>());

        // Act
        services.AddEncinaMongoDBWithTenancy(config =>
        {
            config.ConnectionString = "mongodb://localhost:27017";
            config.DatabaseName = "TestDb";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var tenancyOptions = provider.GetService<IOptions<MongoDbTenancyOptions>>();
        tenancyOptions.ShouldNotBeNull();
        tenancyOptions.Value.AutoFilterTenantQueries.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaMongoDBWithTenancy_WithCustomOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Substitute.For<IMongoClient>());

        // Act
        services.AddEncinaMongoDBWithTenancy(
            config =>
            {
                config.ConnectionString = "mongodb://localhost:27017";
                config.DatabaseName = "TestDb";
            },
            tenancy =>
            {
                tenancy.AutoFilterTenantQueries = false;
                tenancy.TenantFieldName = "OrganizationId";
                tenancy.EnableDatabasePerTenant = true;
            });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MongoDbTenancyOptions>>().Value;
        options.AutoFilterTenantQueries.ShouldBeFalse();
        options.TenantFieldName.ShouldBe("OrganizationId");
        options.EnableDatabasePerTenant.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaMongoDBWithTenancy_RegistersMongoCollectionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Substitute.For<IMongoClient>());

        // Act
        services.AddEncinaMongoDBWithTenancy(config =>
        {
            config.ConnectionString = "mongodb://localhost:27017";
            config.DatabaseName = "TestDb";
        });

        // Assert - Check that IMongoCollectionFactory is registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoCollectionFactory));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddTenantAwareRepository_RegistersRepositoryWithCorrectMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockDatabase = Substitute.For<IMongoDatabase>();
        var mockCollection = Substitute.For<IMongoCollection<MongoTenantTestEntity>>();
        var mockMongoClient = Substitute.For<IMongoClient>();

        mockMongoClient.GetDatabase(Arg.Any<string>()).Returns(mockDatabase);
        mockDatabase.GetCollection<MongoTenantTestEntity>(Arg.Any<string>()).Returns(mockCollection);

        services.AddSingleton(mockMongoClient);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }));
        services.AddSingleton(Options.Create(new MongoDbTenancyOptions()));

        // Act
        services.AddTenantAwareRepository<MongoTenantTestEntity, Guid>(mapping =>
            mapping.ToCollection("entities")
                   .HasId(e => e.Id)
                   .HasTenantId(e => e.TenantId)
                   .MapField(e => e.Name));

        // Assert
        var provider = services.BuildServiceProvider();

        // Check mapping is registered
        var entityMapping = provider.GetService<ITenantEntityMapping<MongoTenantTestEntity, Guid>>();
        entityMapping.ShouldNotBeNull();
        entityMapping.IsTenantEntity.ShouldBeTrue();
        entityMapping.CollectionName.ShouldBe("entities");

        // Check repository is registered
        var repository = provider.GetService<IFunctionalRepository<MongoTenantTestEntity, Guid>>();
        repository.ShouldNotBeNull();
        repository.ShouldBeOfType<TenantAwareFunctionalRepositoryMongoDB<MongoTenantTestEntity, Guid>>();
    }

    [Fact]
    public void AddTenantAwareRepository_RegistersReadRepositoryInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockDatabase = Substitute.For<IMongoDatabase>();
        var mockCollection = Substitute.For<IMongoCollection<MongoTenantTestEntity>>();
        var mockMongoClient = Substitute.For<IMongoClient>();

        mockMongoClient.GetDatabase(Arg.Any<string>()).Returns(mockDatabase);
        mockDatabase.GetCollection<MongoTenantTestEntity>(Arg.Any<string>()).Returns(mockCollection);

        services.AddSingleton(mockMongoClient);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }));
        services.AddSingleton(Options.Create(new MongoDbTenancyOptions()));

        // Act
        services.AddTenantAwareRepository<MongoTenantTestEntity, Guid>(mapping =>
            mapping.ToCollection("entities")
                   .HasId(e => e.Id)
                   .HasTenantId(e => e.TenantId)
                   .MapField(e => e.Name));

        // Assert
        var provider = services.BuildServiceProvider();
        var readRepository = provider.GetService<IFunctionalReadRepository<MongoTenantTestEntity, Guid>>();
        readRepository.ShouldNotBeNull();
    }

    [Fact]
    public void AddTenantAwareReadRepository_RegistersOnlyReadRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockDatabase = Substitute.For<IMongoDatabase>();
        var mockCollection = Substitute.For<IMongoCollection<MongoTenantTestEntity>>();
        var mockMongoClient = Substitute.For<IMongoClient>();

        mockMongoClient.GetDatabase(Arg.Any<string>()).Returns(mockDatabase);
        mockDatabase.GetCollection<MongoTenantTestEntity>(Arg.Any<string>()).Returns(mockCollection);

        services.AddSingleton(mockMongoClient);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }));
        services.AddSingleton(Options.Create(new MongoDbTenancyOptions()));

        // Act
        services.AddTenantAwareReadRepository<MongoTenantTestEntity, Guid>(mapping =>
            mapping.ToCollection("entity_summaries")
                   .HasId(e => e.Id)
                   .HasTenantId(e => e.TenantId)
                   .MapField(e => e.Name));

        // Assert
        var provider = services.BuildServiceProvider();

        // Read repository should be registered
        var readRepository = provider.GetService<IFunctionalReadRepository<MongoTenantTestEntity, Guid>>();
        readRepository.ShouldNotBeNull();

        // Full repository should NOT be registered
        var fullRepository = provider.GetService<IFunctionalRepository<MongoTenantTestEntity, Guid>>();
        fullRepository.ShouldBeNull();
    }

    [Fact]
    public void AddTenantAwareRepository_WithNonTenantEntity_CreatesNonTenantAwareMapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockDatabase = Substitute.For<IMongoDatabase>();
        var mockCollection = Substitute.For<IMongoCollection<MongoTenantTestEntity>>();
        var mockMongoClient = Substitute.For<IMongoClient>();

        mockMongoClient.GetDatabase(Arg.Any<string>()).Returns(mockDatabase);
        mockDatabase.GetCollection<MongoTenantTestEntity>(Arg.Any<string>()).Returns(mockCollection);

        services.AddSingleton(mockMongoClient);
        services.AddSingleton(Substitute.For<ITenantProvider>());
        services.AddSingleton(Substitute.For<ITenantStore>());
        services.AddSingleton(Options.Create(new EncinaMongoDbOptions { DatabaseName = "TestDb" }));
        services.AddSingleton(Options.Create(new MongoDbTenancyOptions()));

        // Act - Register without HasTenantId
        services.AddTenantAwareRepository<MongoTenantTestEntity, Guid>(mapping =>
            mapping.ToCollection("global_entities")
                   .HasId(e => e.Id)
                   .MapField(e => e.Name));

        // Assert
        var provider = services.BuildServiceProvider();
        var entityMapping = provider.GetRequiredService<ITenantEntityMapping<MongoTenantTestEntity, Guid>>();
        entityMapping.IsTenantEntity.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaMongoDBWithTenancy_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMongoDBWithTenancy(config => { }));
    }

    [Fact]
    public void AddEncinaMongoDBWithTenancy_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDBWithTenancy(null!));
    }

    [Fact]
    public void AddTenantAwareRepository_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantAwareRepository<MongoTenantTestEntity, Guid>(
                m => m.ToCollection("entities").HasId(e => e.Id)));
    }

    [Fact]
    public void AddTenantAwareRepository_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantAwareRepository<MongoTenantTestEntity, Guid>(null!));
    }
}

/// <summary>
/// Test entity for MongoDB tenancy service registration tests.
/// </summary>
public sealed class MongoTenantTestEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
