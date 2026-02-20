using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.Testing.Shouldly;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public class CollectionFactoryTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ReadWriteMongoCollectionFactoryTests
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public ReadWriteMongoCollectionFactoryTests()
    {
        _mongoClient = Substitute.For<IMongoClient>();
        _database = Substitute.For<IMongoDatabase>();
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = "TestDb"
        });

        _mongoClient.GetDatabase("TestDb").Returns(_database);
    }

    [Fact]
    public void Constructor_ThrowsOnNullMongoClient()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(null!, _options));
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(_mongoClient, null!));
    }

    [Fact]
    public async Task GetWriteCollectionAsync_ThrowsOnNullCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetWriteCollectionAsync<CollectionFactoryTestEntity>(null!));
    }

    [Fact]
    public async Task GetWriteCollectionAsync_ThrowsOnEmptyCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetWriteCollectionAsync<CollectionFactoryTestEntity>(string.Empty));
    }

    [Fact]
    public async Task GetReadCollectionAsync_ThrowsOnNullCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetReadCollectionAsync<CollectionFactoryTestEntity>(null!));
    }

    [Fact]
    public async Task GetReadCollectionAsync_ThrowsOnEmptyCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetReadCollectionAsync<CollectionFactoryTestEntity>(string.Empty));
    }

    [Fact]
    public async Task GetCollectionAsync_ThrowsOnNullCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<CollectionFactoryTestEntity>(null!));
    }

    [Fact]
    public async Task GetCollectionAsync_ThrowsOnEmptyCollectionName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<CollectionFactoryTestEntity>(string.Empty));
    }

    [Fact]
    public async Task GetDatabaseNameAsync_ReturnsConfiguredName()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);

        // Act
        var result = await factory.GetDatabaseNameAsync();

        // Assert
        result.ShouldBeRight().ShouldBe("TestDb");
    }

    [Fact]
    public async Task GetDatabaseNameAsync_ReturnsLeftWhenNoDatabaseNameConfigured()
    {
        // Arrange
        var opts = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = string.Empty
        });
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, opts);

        // Act
        var result = await factory.GetDatabaseNameAsync();

        // Assert
        var error = result.ShouldBeLeft();
        error.Message.ShouldContain("DatabaseName");
    }

    [Fact]
    public async Task GetWriteCollectionAsync_ThrowsOnCancellation()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await factory.GetWriteCollectionAsync<CollectionFactoryTestEntity>("test", cts.Token));
    }

    [Fact]
    public async Task GetReadCollectionAsync_ThrowsOnCancellation()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await factory.GetReadCollectionAsync<CollectionFactoryTestEntity>("test", cts.Token));
    }

    [Fact]
    public async Task GetCollectionAsync_ThrowsOnCancellation()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await factory.GetCollectionAsync<CollectionFactoryTestEntity>("test", cts.Token));
    }

    [Fact]
    public async Task GetDatabaseNameAsync_ThrowsOnCancellation()
    {
        // Arrange
        var factory = new ReadWriteMongoCollectionFactory(_mongoClient, _options);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await factory.GetDatabaseNameAsync(cts.Token));
    }
}
