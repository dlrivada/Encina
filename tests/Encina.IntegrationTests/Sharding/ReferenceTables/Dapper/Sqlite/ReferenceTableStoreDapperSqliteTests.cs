using Encina.Dapper.Sqlite.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables;
using Encina.TestInfrastructure.Fixtures.Sharding;
using Shouldly;
using Xunit;
using AdoSqliteStore = Encina.ADO.Sqlite.Sharding.ReferenceTables.ReferenceTableStoreADO;

namespace Encina.IntegrationTests.Sharding.ReferenceTables.Dapper.Sqlite;

/// <summary>
/// Integration tests for <see cref="ReferenceTableStoreDapper"/> using SQLite.
/// Verifies upsert, read, hash, and cross-shard replication end-to-end.
/// </summary>
[Collection("Sharding-Dapper-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Database", "SQLite")]
public sealed class ReferenceTableStoreDapperSqliteTests : IAsyncLifetime
{
    private readonly ShardedSqliteFixture _fixture;

    public ReferenceTableStoreDapperSqliteTests(ShardedSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region UpsertAsync

    [Fact]
    public async Task UpsertAsync_EmptyCollection_ShouldReturnZero()
    {
        // Arrange
        using var store = CreateStore("shard-1");

        // Act
        var result = await store.UpsertAsync(Array.Empty<CountryRef>());

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task UpsertAsync_SingleEntity_ShouldInsertAndReturnAffectedRows()
    {
        // Arrange
        using var store = CreateStore("shard-1");
        var entities = new[]
        {
            new CountryRef { Id = "US", Code = "US", Name = "United States" }
        };

        // Act
        var result = await store.UpsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(count => count.ShouldBeGreaterThan(0));
    }

    [Fact]
    public async Task UpsertAsync_MultipleEntities_ShouldInsertAll()
    {
        // Arrange
        using var store = CreateStore("shard-1");
        var entities = CreateTestCountries(10);

        // Act
        var result = await store.UpsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await store.GetAllAsync<CountryRef>();
        getResult.IsRight.ShouldBeTrue();
        _ = getResult.IfRight(all => all.Count.ShouldBe(10));
    }

    [Fact]
    public async Task UpsertAsync_DuplicateKeys_ShouldUpdateExistingRows()
    {
        // Arrange
        using var store = CreateStore("shard-1");

        var initial = new[] { new CountryRef { Id = "FR", Code = "FR", Name = "France" } };
        await store.UpsertAsync(initial);

        var updated = new[] { new CountryRef { Id = "FR", Code = "FR", Name = "French Republic" } };

        // Act
        var result = await store.UpsertAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await store.GetAllAsync<CountryRef>();
        getResult.IsRight.ShouldBeTrue();
        _ = getResult.IfRight(all =>
        {
            all.Count.ShouldBe(1);
            all[0].Name.ShouldBe("French Republic");
        });
    }

    [Fact]
    public async Task UpsertAsync_LargeBatch_ShouldHandleSqliteParameterLimit()
    {
        // Arrange — 400 entities × 3 columns = 1200 params, exceeds 999 limit
        using var store = CreateStore("shard-1");
        var entities = CreateTestCountries(400);

        // Act
        var result = await store.UpsertAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        var getResult = await store.GetAllAsync<CountryRef>();
        getResult.IsRight.ShouldBeTrue();
        _ = getResult.IfRight(all => all.Count.ShouldBe(400));
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_EmptyTable_ShouldReturnEmptyList()
    {
        // Arrange
        using var store = CreateStore("shard-1");

        // Act
        var result = await store.GetAllAsync<CountryRef>();

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(all => all.Count.ShouldBe(0));
    }

    [Fact]
    public async Task GetAllAsync_AfterUpsert_ShouldReturnInsertedEntities()
    {
        // Arrange
        using var store = CreateStore("shard-1");
        var entities = new[]
        {
            new CountryRef { Id = "DE", Code = "DE", Name = "Germany" },
            new CountryRef { Id = "JP", Code = "JP", Name = "Japan" }
        };
        await store.UpsertAsync(entities);

        // Act
        var result = await store.GetAllAsync<CountryRef>();

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(all =>
        {
            all.Count.ShouldBe(2);
            all.ShouldContain(c => c.Id == "DE" && c.Name == "Germany");
            all.ShouldContain(c => c.Id == "JP" && c.Name == "Japan");
        });
    }

    #endregion

    #region GetHashAsync

    [Fact]
    public async Task GetHashAsync_EmptyTable_ShouldReturnZeroHash()
    {
        // Arrange
        using var store = CreateStore("shard-1");

        // Act
        var result = await store.GetHashAsync<CountryRef>();

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(hash =>
        {
            hash.ShouldNotBeNullOrWhiteSpace();
            hash.Length.ShouldBe(16);
        });
    }

    [Fact]
    public async Task GetHashAsync_SameData_ShouldReturnSameHash()
    {
        // Arrange
        using var store = CreateStore("shard-1");
        var entities = new[]
        {
            new CountryRef { Id = "BR", Code = "BR", Name = "Brazil" },
            new CountryRef { Id = "AR", Code = "AR", Name = "Argentina" }
        };
        await store.UpsertAsync(entities);

        // Act
        var hash1 = await store.GetHashAsync<CountryRef>();
        var hash2 = await store.GetHashAsync<CountryRef>();

        // Assert
        hash1.IsRight.ShouldBeTrue();
        hash2.IsRight.ShouldBeTrue();

        string h1 = "", h2 = "";
        _ = hash1.IfRight(h => h1 = h);
        _ = hash2.IfRight(h => h2 = h);
        h1.ShouldBe(h2);
    }

    [Fact]
    public async Task GetHashAsync_DifferentData_ShouldReturnDifferentHash()
    {
        // Arrange
        using var store1 = CreateStore("shard-1");
        using var store2 = CreateStore("shard-2");

        await store1.UpsertAsync(new[] { new CountryRef { Id = "IT", Code = "IT", Name = "Italy" } });
        await store2.UpsertAsync(new[] { new CountryRef { Id = "ES", Code = "ES", Name = "Spain" } });

        // Act
        var hash1 = await store1.GetHashAsync<CountryRef>();
        var hash2 = await store2.GetHashAsync<CountryRef>();

        // Assert
        hash1.IsRight.ShouldBeTrue();
        hash2.IsRight.ShouldBeTrue();

        string h1 = "", h2 = "";
        _ = hash1.IfRight(h => h1 = h);
        _ = hash2.IfRight(h => h2 = h);
        h1.ShouldNotBe(h2);
    }

    #endregion

    #region Cross-Shard Replication (End-to-End)

    [Fact]
    public async Task EndToEnd_WriteToPrimary_ReplicateToAllShards()
    {
        // Arrange — write data to shard-1 (primary)
        using var primaryStore = CreateStore("shard-1");
        var entities = new[]
        {
            new CountryRef { Id = "US", Code = "US", Name = "United States" },
            new CountryRef { Id = "CA", Code = "CA", Name = "Canada" },
            new CountryRef { Id = "MX", Code = "MX", Name = "Mexico" }
        };
        await primaryStore.UpsertAsync(entities);

        // Act — read from primary, then upsert to other shards
        var readResult = await primaryStore.GetAllAsync<CountryRef>();
        readResult.IsRight.ShouldBeTrue();

        var data = new List<CountryRef>();
        _ = readResult.IfRight(d => data.AddRange(d));

        using var shard2Store = CreateStore("shard-2");
        using var shard3Store = CreateStore("shard-3");

        var upsert2 = await shard2Store.UpsertAsync(data);
        var upsert3 = await shard3Store.UpsertAsync(data);

        // Assert — all shards should have the same data
        upsert2.IsRight.ShouldBeTrue();
        upsert3.IsRight.ShouldBeTrue();

        var shard2Data = await shard2Store.GetAllAsync<CountryRef>();
        var shard3Data = await shard3Store.GetAllAsync<CountryRef>();

        _ = shard2Data.IfRight(d => d.Count.ShouldBe(3));
        _ = shard3Data.IfRight(d => d.Count.ShouldBe(3));
    }

    [Fact]
    public async Task EndToEnd_AllShards_ShouldHaveSameHash_AfterReplication()
    {
        // Arrange — write and replicate
        using var primaryStore = CreateStore("shard-1");
        var entities = CreateTestCountries(5);
        await primaryStore.UpsertAsync(entities);

        var data = new List<CountryRef>();
        _ = (await primaryStore.GetAllAsync<CountryRef>()).IfRight(d => data.AddRange(d));

        using var shard2Store = CreateStore("shard-2");
        using var shard3Store = CreateStore("shard-3");

        await shard2Store.UpsertAsync(data);
        await shard3Store.UpsertAsync(data);

        // Act
        var hash1 = await primaryStore.GetHashAsync<CountryRef>();
        var hash2 = await shard2Store.GetHashAsync<CountryRef>();
        var hash3 = await shard3Store.GetHashAsync<CountryRef>();

        // Assert
        hash1.IsRight.ShouldBeTrue();
        hash2.IsRight.ShouldBeTrue();
        hash3.IsRight.ShouldBeTrue();

        string h1 = "", h2 = "", h3 = "";
        _ = hash1.IfRight(h => h1 = h);
        _ = hash2.IfRight(h => h2 = h);
        _ = hash3.IfRight(h => h3 = h);

        h1.ShouldBe(h2);
        h2.ShouldBe(h3);
    }

    [Fact]
    public async Task EndToEnd_HashConsistency_BetweenAdoAndDapper()
    {
        // Arrange — upsert same data via Dapper
        using var dapperStore = CreateStore("shard-1");
        var entities = CreateTestCountries(5);
        await dapperStore.UpsertAsync(entities);

        // Also insert same data using ADO to shard-2
        var adoConnection = _fixture.CreateConnection("shard-2");
        using var adoStore = new AdoSqliteStore(adoConnection);
        await adoStore.UpsertAsync(entities);

        // Act
        var dapperHash = await dapperStore.GetHashAsync<CountryRef>();
        var adoHash = await adoStore.GetHashAsync<CountryRef>();

        // Assert — hash should be identical regardless of provider
        dapperHash.IsRight.ShouldBeTrue();
        adoHash.IsRight.ShouldBeTrue();

        string h1 = "", h2 = "";
        _ = dapperHash.IfRight(h => h1 = h);
        _ = adoHash.IfRight(h => h2 = h);
        h1.ShouldBe(h2);
    }

    #endregion

    #region Factory Tests

    [Fact]
    public void Factory_CreateForShard_ShouldReturnWorkingStore()
    {
        // Arrange
        var factory = new ReferenceTableStoreFactoryDapper();

        // Act
        using var store = (IDisposable)factory.CreateForShard(_fixture.Shard1ConnectionString);

        // Assert
        store.ShouldNotBeNull();
        store.ShouldBeAssignableTo<IReferenceTableStore>();
    }

    [Fact]
    public async Task Factory_CreatedStores_ShouldOperateIndependently()
    {
        // Arrange
        var factory = new ReferenceTableStoreFactoryDapper();
        using var store1 = (ReferenceTableStoreDapper)factory.CreateForShard(_fixture.Shard1ConnectionString);
        using var store2 = (ReferenceTableStoreDapper)factory.CreateForShard(_fixture.Shard2ConnectionString);

        // Act
        await store1.UpsertAsync(new[] { new CountryRef { Id = "A1", Code = "A1", Name = "Alpha" } });
        await store2.UpsertAsync(new[] { new CountryRef { Id = "B1", Code = "B1", Name = "Beta" } });

        // Assert — each shard has its own data
        var data1 = await store1.GetAllAsync<CountryRef>();
        var data2 = await store2.GetAllAsync<CountryRef>();

        _ = data1.IfRight(d => d.ShouldContain(c => c.Id == "A1"));
        _ = data2.IfRight(d => d.ShouldContain(c => c.Id == "B1"));

        _ = data1.IfRight(d => d.ShouldNotContain(c => c.Id == "B1"));
        _ = data2.IfRight(d => d.ShouldNotContain(c => c.Id == "A1"));
    }

    #endregion

    #region Helpers

    private ReferenceTableStoreDapper CreateStore(string shardId)
    {
        var connection = _fixture.CreateConnection(shardId);
        return new ReferenceTableStoreDapper(connection);
    }

    private static CountryRef[] CreateTestCountries(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new CountryRef
            {
                Id = $"C{i:D4}",
                Code = $"C{i:D4}",
                Name = $"Country {i}"
            })
            .ToArray();

    #endregion
}
