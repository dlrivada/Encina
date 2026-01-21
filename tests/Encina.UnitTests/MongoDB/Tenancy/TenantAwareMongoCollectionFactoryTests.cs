using Encina.MongoDB;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantAwareMongoCollectionFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareMongoCollectionFactoryTests
{
    private readonly IMongoClient _mongoClient;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly IMongoDatabase _database;

    public TenantAwareMongoCollectionFactoryTests()
    {
        _mongoClient = Substitute.For<IMongoClient>();
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantStore = Substitute.For<ITenantStore>();
        _database = Substitute.For<IMongoDatabase>();

        _mongoClient.GetDatabase(Arg.Any<string>()).Returns(_database);
    }

    [Fact]
    public void Constructor_NullMongoClient_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions());
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(null!, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions());
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(_mongoClient, null!, _tenantStore, mongoOptions, tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenantStore_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions());
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(_mongoClient, _tenantProvider, null!, mongoOptions, tenancyOptions));
    }

    [Fact]
    public void Constructor_NullMongoOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(_mongoClient, _tenantProvider, _tenantStore, null!, tenancyOptions));
    }

    [Fact]
    public void Constructor_NullTenancyOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(_mongoClient, _tenantProvider, _tenantStore, mongoOptions, null!));
    }

    [Fact]
    public async Task GetCollectionAsync_NoTenantContext_ReturnsDefaultDatabase()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionAsync<MongoCollectionFactoryTestDocument>("orders");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb");
    }

    [Fact]
    public async Task GetCollectionAsync_DatabasePerTenantDisabled_ReturnsDefaultDatabase()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions { EnableDatabasePerTenant = false });
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionAsync<MongoCollectionFactoryTestDocument>("orders");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb");
    }

    [Fact]
    public async Task GetCollectionAsync_DatabasePerTenantEnabled_ReturnsTenantDatabase()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions
        {
            EnableDatabasePerTenant = true,
            DatabaseNamePattern = "{baseName}_{tenantId}"
        });
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        _tenantStore.GetTenantAsync("tenant-123", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-123",
                Name: "Test Tenant",
                Strategy: TenantIsolationStrategy.DatabasePerTenant));

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionAsync<MongoCollectionFactoryTestDocument>("orders");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb_tenant-123");
    }

    [Fact]
    public async Task GetCollectionAsync_SharedSchemaTenant_ReturnsDefaultDatabase()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions { EnableDatabasePerTenant = true });
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantProvider.GetCurrentTenantId().Returns("tenant-123");
        _tenantStore.GetTenantAsync("tenant-123", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "tenant-123",
                Name: "Test Tenant",
                Strategy: TenantIsolationStrategy.SharedSchema));

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionAsync<MongoCollectionFactoryTestDocument>("orders");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb");
    }

    [Fact]
    public async Task GetCollectionAsync_TenantNotFound_ReturnsDefaultDatabase()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions { EnableDatabasePerTenant = true });
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantProvider.GetCurrentTenantId().Returns("unknown-tenant");
        _tenantStore.GetTenantAsync("unknown-tenant", Arg.Any<CancellationToken>())
            .Returns((TenantInfo?)null);

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionAsync<MongoCollectionFactoryTestDocument>("orders");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb");
    }

    [Fact]
    public async Task GetCollectionForTenantAsync_ReturnsCollectionForSpecificTenant()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions
        {
            EnableDatabasePerTenant = true,
            DatabaseNamePattern = "{baseName}_{tenantId}"
        });
        var mockCollection = Substitute.For<IMongoCollection<MongoCollectionFactoryTestDocument>>();
        _database.GetCollection<MongoCollectionFactoryTestDocument>("orders").Returns(mockCollection);
        _tenantStore.GetTenantAsync("specific-tenant", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "specific-tenant",
                Name: "Specific Tenant",
                Strategy: TenantIsolationStrategy.DatabasePerTenant));

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var collection = await factory.GetCollectionForTenantAsync<MongoCollectionFactoryTestDocument>("orders", "specific-tenant");

        // Assert
        collection.ShouldBe(mockCollection);
        _mongoClient.Received(1).GetDatabase("DefaultDb_specific-tenant");
    }

    [Fact]
    public async Task GetCollectionForTenantAsync_NullCollectionName_ThrowsArgumentException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            factory.GetCollectionForTenantAsync<MongoCollectionFactoryTestDocument>(null!, "tenant-123").AsTask());
    }

    [Fact]
    public async Task GetCollectionForTenantAsync_EmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "DefaultDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            factory.GetCollectionForTenantAsync<MongoCollectionFactoryTestDocument>("orders", string.Empty).AsTask());
    }

    [Fact]
    public async Task GetDatabaseNameAsync_NoTenantContext_ReturnsDefaultDatabaseName()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "MyAppDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var databaseName = await factory.GetDatabaseNameAsync();

        // Assert
        databaseName.ShouldBe("MyAppDb");
    }

    [Fact]
    public async Task GetDatabaseNameAsync_WithTenantAndDatabasePerTenant_ReturnsTenantDatabaseName()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = "MyAppDb" });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions
        {
            EnableDatabasePerTenant = true,
            DatabaseNamePattern = "tenant_{tenantId}"
        });
        _tenantProvider.GetCurrentTenantId().Returns("acme");
        _tenantStore.GetTenantAsync("acme", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(
                TenantId: "acme",
                Name: "Acme Corp",
                Strategy: TenantIsolationStrategy.DatabasePerTenant));

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act
        var databaseName = await factory.GetDatabaseNameAsync();

        // Assert
        databaseName.ShouldBe("tenant_acme");
    }

    [Fact]
    public void GetDatabaseNameAsync_NoDatabaseNameConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = string.Empty });
        var tenancyOptions = Options.Create(new MongoDbTenancyOptions());
        _tenantProvider.GetCurrentTenantId().Returns((string?)null);

        var factory = new TenantAwareMongoCollectionFactory(
            _mongoClient, _tenantProvider, _tenantStore, mongoOptions, tenancyOptions);

        // Act & Assert
        Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetDatabaseNameAsync());
    }

}

/// <summary>
/// Simple test document class for factory tests.
/// </summary>
public sealed class MongoCollectionFactoryTestDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
