using Encina.Compliance.Anonymization.Model;
using Encina.MongoDB;
using Encina.MongoDB.Anonymization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Anonymization;

public class TokenMappingStoreGuardTests
{
    private static readonly IOptions<EncinaMongoDbOptions> Opts =
        Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });
    private static readonly ILogger<TokenMappingStoreMongoDB> Logger =
        NullLogger<TokenMappingStoreMongoDB>.Instance;

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(null!, Opts, Logger));

    [Fact]
    public void Ctor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(CreateMockClient(), null!, Logger));

    [Fact]
    public void Ctor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreMongoDB(CreateMockClient(), Opts, null!));

    #endregion

    #region StoreAsync

    [Fact]
    public async Task StoreAsync_NullMapping_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.StoreAsync(null!));
    }

    #endregion

    #region GetByTokenAsync

    [Fact]
    public async Task GetByTokenAsync_NullToken_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByTokenAsync(null!));
    }

    [Fact]
    public async Task GetByTokenAsync_EmptyToken_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByTokenAsync(""));
    }

    [Fact]
    public async Task GetByTokenAsync_WhitespaceToken_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByTokenAsync("   "));
    }

    #endregion

    #region GetByOriginalValueHashAsync

    [Fact]
    public async Task GetByOriginalValueHashAsync_NullHash_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByOriginalValueHashAsync(null!));
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_EmptyHash_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByOriginalValueHashAsync(""));
    }

    #endregion

    #region DeleteByKeyIdAsync

    [Fact]
    public async Task DeleteByKeyIdAsync_NullKeyId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeleteByKeyIdAsync(null!));
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_EmptyKeyId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeleteByKeyIdAsync(""));
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_WhitespaceKeyId_Throws()
    {
        var store = CreateStore();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeleteByKeyIdAsync("   "));
    }

    #endregion

    private static TokenMappingStoreMongoDB CreateStore()
        => new(CreateMockClient(), Opts, Logger);

    private static IMongoClient CreateMockClient()
    {
        var client = Substitute.For<IMongoClient>();
        var db = Substitute.For<IMongoDatabase>();
        client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Returns(db);
        db.GetCollection<TokenMappingDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(Substitute.For<IMongoCollection<TokenMappingDocument>>());
        return client;
    }
}
