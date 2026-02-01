using Encina.DomainModeling.Auditing;
using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;

namespace Encina.UnitTests.MongoDB.Auditing;

/// <summary>
/// Unit tests for <see cref="AuditLogStoreMongoDB"/>.
/// </summary>
public sealed class AuditLogStoreMongoDBTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullMongoClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(null!, options, logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(mongoClient, null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var options = Options.Create(new EncinaMongoDbOptions());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AuditLogStoreMongoDB(mongoClient, options, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        // Act
        var store = new AuditLogStoreMongoDB(mongoClient, options, logger);

        // Assert
        store.ShouldNotBeNull();
    }

    #endregion

    #region LogAsync Validation Tests

    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        var store = new AuditLogStoreMongoDB(mongoClient, options, logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.LogAsync(null!));

        exception.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetHistoryAsync Validation Tests

    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        var store = new AuditLogStoreMongoDB(mongoClient, options, logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.GetHistoryAsync(null!, "123"));

        exception.ParamName.ShouldBe("entityType");
    }

    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        var store = new AuditLogStoreMongoDB(mongoClient, options, logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.GetHistoryAsync("Order", null!));

        exception.ParamName.ShouldBe("entityId");
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        var store = new AuditLogStoreMongoDB(mongoClient, options, logger);

        // Act & Assert - should not throw and complete immediately
        await store.SaveChangesAsync();
    }

    #endregion
}
