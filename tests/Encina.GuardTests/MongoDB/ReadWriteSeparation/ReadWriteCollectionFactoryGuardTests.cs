using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.ReadWriteSeparation;

public class ReadWriteCollectionFactoryGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly IOptions<EncinaMongoDbOptions> Opts =
        Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = "test",
            UseReadWriteSeparation = true
        });

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(null!, Opts));

    [Fact]
    public void Ctor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(Client, null!));

    #endregion

    #region GetWriteCollectionAsync

    [Fact]
    public async Task GetWriteCollectionAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetWriteCollectionAsync<TestEntity>(null!));
    }

    [Fact]
    public async Task GetWriteCollectionAsync_EmptyCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetWriteCollectionAsync<TestEntity>(""));
    }

    #endregion

    #region GetReadCollectionAsync

    [Fact]
    public async Task GetReadCollectionAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetReadCollectionAsync<TestEntity>(null!));
    }

    [Fact]
    public async Task GetReadCollectionAsync_EmptyCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetReadCollectionAsync<TestEntity>(""));
    }

    #endregion

    #region GetCollectionAsync

    [Fact]
    public async Task GetCollectionAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<TestEntity>(null!));
    }

    [Fact]
    public async Task GetCollectionAsync_EmptyCollectionName_Throws()
    {
        SetupMockClient();
        var factory = new ReadWriteMongoCollectionFactory(Client, Opts);
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<TestEntity>(""));
    }

    #endregion

    private static void SetupMockClient()
    {
        var db = Substitute.For<IMongoDatabase>();
        Client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        var mockCollection = Substitute.For<IMongoCollection<TestEntity>>();
        mockCollection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(mockCollection);
        mockCollection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(mockCollection);
        db.GetCollection<TestEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(mockCollection);
    }

    public class TestEntity { public Guid Id { get; set; } }
}
