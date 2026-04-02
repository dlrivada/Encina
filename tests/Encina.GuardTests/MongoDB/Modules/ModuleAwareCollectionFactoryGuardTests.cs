using Encina.Modules.Isolation;
using Encina.MongoDB;
using Encina.MongoDB.Modules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Modules;

public class ModuleAwareCollectionFactoryGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly IModuleExecutionContext ModuleCtx = Substitute.For<IModuleExecutionContext>();
    private static readonly IOptions<EncinaMongoDbOptions> MongoOpts =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });
    private static readonly IOptions<MongoDbModuleIsolationOptions> IsolationOpts =
        Options.Create(new MongoDbModuleIsolationOptions());
    private static readonly ILogger<ModuleAwareMongoCollectionFactory> Logger =
        NullLogger<ModuleAwareMongoCollectionFactory>.Instance;

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ModuleAwareMongoCollectionFactory(null!, ModuleCtx, MongoOpts, IsolationOpts, Logger));

    [Fact]
    public void Ctor_NullModuleContext_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ModuleAwareMongoCollectionFactory(Client, null!, MongoOpts, IsolationOpts, Logger));

    [Fact]
    public void Ctor_NullMongoOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ModuleAwareMongoCollectionFactory(Client, ModuleCtx, null!, IsolationOpts, Logger));

    [Fact]
    public void Ctor_NullIsolationOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ModuleAwareMongoCollectionFactory(Client, ModuleCtx, MongoOpts, null!, Logger));

    [Fact]
    public void Ctor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ModuleAwareMongoCollectionFactory(Client, ModuleCtx, MongoOpts, IsolationOpts, null!));

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

    #region GetCollectionForModuleAsync

    [Fact]
    public async Task GetCollectionForModuleAsync_NullCollectionName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<TestEntity>(null!, "mod"));
    }

    [Fact]
    public async Task GetCollectionForModuleAsync_NullModuleName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<TestEntity>("coll", null!));
    }

    [Fact]
    public async Task GetCollectionForModuleAsync_EmptyModuleName_Throws()
    {
        SetupMockClient();
        var factory = CreateFactory();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<TestEntity>("coll", ""));
    }

    #endregion

    #region GetDatabaseNameForModule

    [Fact]
    public void GetDatabaseNameForModule_NullModuleName_Throws()
    {
        var factory = CreateFactory();
        Should.Throw<ArgumentException>(() =>
            factory.GetDatabaseNameForModule(null!));
    }

    [Fact]
    public void GetDatabaseNameForModule_EmptyModuleName_Throws()
    {
        var factory = CreateFactory();
        Should.Throw<ArgumentException>(() =>
            factory.GetDatabaseNameForModule(""));
    }

    #endregion

    private static ModuleAwareMongoCollectionFactory CreateFactory()
        => new(Client, ModuleCtx, MongoOpts, IsolationOpts, Logger);

    private static void SetupMockClient()
    {
        var db = Substitute.For<IMongoDatabase>();
        Client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<TestEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<TestEntity>>());
    }

    public class TestEntity { public Guid Id { get; set; } }
}
