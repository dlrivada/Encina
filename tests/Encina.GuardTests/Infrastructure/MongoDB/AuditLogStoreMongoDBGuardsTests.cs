using Encina.DomainModeling.Auditing;
using Encina.MongoDB;
using Encina.MongoDB.Auditing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;

namespace Encina.GuardTests.Infrastructure.MongoDB;

/// <summary>
/// Guard tests for <see cref="AuditLogStoreMongoDB"/> to verify null parameter handling.
/// </summary>
public class AuditLogStoreMongoDBGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when mongoClient is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMongoClient_ThrowsArgumentNullException()
    {
        // Arrange
        IMongoClient mongoClient = null!;
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        // Act & Assert
        var act = () => new AuditLogStoreMongoDB(mongoClient, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mongoClient");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        IOptions<EncinaMongoDbOptions> options = null!;
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        // Act & Assert
        var act = () => new AuditLogStoreMongoDB(mongoClient, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoClient = Substitute.For<IMongoClient>();
        var options = Options.Create(new EncinaMongoDbOptions());
        ILogger<AuditLogStoreMongoDB> logger = null!;

        // Act & Assert
        var act = () => new AuditLogStoreMongoDB(mongoClient, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that LogAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task LogAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        AuditLogEntry entry = null!;

        // Act & Assert
        Func<Task> act = () => store.LogAsync(entry);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityType is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityType_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string entityType = null!;
        const string entityId = "123";

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityType");
    }

    /// <summary>
    /// Verifies that GetHistoryAsync throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_NullEntityId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        const string entityType = "Order";
        string entityId = null!;

        // Act & Assert
        Func<Task> act = () => store.GetHistoryAsync(entityType, entityId);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entityId");
    }

    private static AuditLogStoreMongoDB CreateStore()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoDatabase = Substitute.For<IMongoDatabase>();
        var mongoCollection = Substitute.For<IMongoCollection<AuditLogDocument>>();
        var options = Options.Create(new EncinaMongoDbOptions());
        var logger = Substitute.For<ILogger<AuditLogStoreMongoDB>>();

        mongoClient.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(mongoDatabase);
        mongoDatabase.GetCollection<AuditLogDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mongoCollection);

        return new AuditLogStoreMongoDB(mongoClient, options, logger);
    }
}
