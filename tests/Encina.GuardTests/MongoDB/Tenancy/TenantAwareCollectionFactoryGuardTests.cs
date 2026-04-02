using Encina.MongoDB;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Tenancy;

public class TenantAwareCollectionFactoryGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly ITenantProvider TenantProvider = Substitute.For<ITenantProvider>();
    private static readonly ITenantStore TenantStore = Substitute.For<ITenantStore>();
    private static readonly IOptions<EncinaMongoDbOptions> MongoOpts =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });
    private static readonly IOptions<MongoDbTenancyOptions> TenancyOpts =
        Options.Create(new MongoDbTenancyOptions());

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(null!, TenantProvider, TenantStore, MongoOpts, TenancyOpts));

    [Fact]
    public void Ctor_NullTenantProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(Client, null!, TenantStore, MongoOpts, TenancyOpts));

    [Fact]
    public void Ctor_NullTenantStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(Client, TenantProvider, null!, MongoOpts, TenancyOpts));

    [Fact]
    public void Ctor_NullMongoOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(Client, TenantProvider, TenantStore, null!, TenancyOpts));

    [Fact]
    public void Ctor_NullTenancyOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TenantAwareMongoCollectionFactory(Client, TenantProvider, TenantStore, MongoOpts, null!));

    #endregion

    #region GetCollectionAsync

    [Fact]
    public async Task GetCollectionAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<TestEntity>(null!));
    }

    [Fact]
    public async Task GetCollectionAsync_EmptyCollectionName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<TestEntity>(""));
    }

    #endregion

    #region GetCollectionForTenantAsync

    [Fact]
    public async Task GetCollectionForTenantAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForTenantAsync<TestEntity>(null!, "tenant1"));
    }

    [Fact]
    public async Task GetCollectionForTenantAsync_NullTenantId_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForTenantAsync<TestEntity>("coll", null!));
    }

    [Fact]
    public async Task GetCollectionForTenantAsync_EmptyTenantId_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForTenantAsync<TestEntity>("coll", ""));
    }

    #endregion

    private static TenantAwareMongoCollectionFactory CreateFactory()
        => new(Client, TenantProvider, TenantStore, MongoOpts, TenancyOpts);

    private static void SetupMockClient()
    {
        var db = Substitute.For<IMongoDatabase>();
        Client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<TestEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<TestEntity>>());
    }

    public class TestEntity { public Guid Id { get; set; } }
}
