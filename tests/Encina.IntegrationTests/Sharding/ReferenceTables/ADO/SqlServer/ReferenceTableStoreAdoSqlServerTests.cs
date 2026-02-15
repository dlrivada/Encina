using Encina.ADO.SqlServer.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.ReferenceTables.ADO.SqlServer;

/// <summary>
/// Integration tests for <see cref="ReferenceTableStoreADO"/> using SQL Server.
/// Verifies upsert, read, hash, and cross-shard replication end-to-end.
/// </summary>
[Collection("Sharding-ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class ReferenceTableStoreAdoSqlServerTests : IAsyncLifetime
{
    private readonly ShardedSqlServerFixture _fixture;

    public ReferenceTableStoreAdoSqlServerTests(ShardedSqlServerFixture fixture)
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
            hash.Length.ShouldBe(16); // XxHash64 = 16 hex chars
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

        // Act — read from primary, then upsert to other shards (simulating replicator)
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

        shard2Data.IsRight.ShouldBeTrue();
        shard3Data.IsRight.ShouldBeTrue();

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

        var readResult = await primaryStore.GetAllAsync<CountryRef>();
        var data = new List<CountryRef>();
        _ = readResult.IfRight(d => data.AddRange(d));

        using var shard2Store = CreateStore("shard-2");
        using var shard3Store = CreateStore("shard-3");

        await shard2Store.UpsertAsync(data);
        await shard3Store.UpsertAsync(data);

        // Act — compute hashes on all shards
        var hash1 = await primaryStore.GetHashAsync<CountryRef>();
        var hash2 = await shard2Store.GetHashAsync<CountryRef>();
        var hash3 = await shard3Store.GetHashAsync<CountryRef>();

        // Assert — all hashes should match
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
    public async Task EndToEnd_UpdatePrimary_ReReplicateUpdatesAllShards()
    {
        // Arrange — initial replication
        using var primaryStore = CreateStore("shard-1");
        using var shard2Store = CreateStore("shard-2");

        var initial = new[] { new CountryRef { Id = "GB", Code = "GB", Name = "United Kingdom" } };
        await primaryStore.UpsertAsync(initial);

        var data = new List<CountryRef>();
        _ = (await primaryStore.GetAllAsync<CountryRef>()).IfRight(d => data.AddRange(d));
        await shard2Store.UpsertAsync(data);

        // Act — update primary and re-replicate
        var updated = new[] { new CountryRef { Id = "GB", Code = "GB", Name = "Great Britain" } };
        await primaryStore.UpsertAsync(updated);

        var newData = new List<CountryRef>();
        _ = (await primaryStore.GetAllAsync<CountryRef>()).IfRight(d => newData.AddRange(d));
        await shard2Store.UpsertAsync(newData);

        // Assert — shard-2 should have updated data
        var shard2Data = await shard2Store.GetAllAsync<CountryRef>();
        shard2Data.IsRight.ShouldBeTrue();
        _ = shard2Data.IfRight(d =>
        {
            d.Count.ShouldBe(1);
            d[0].Name.ShouldBe("Great Britain");
        });
    }

    #endregion

    #region Factory Tests

    [Fact]
    public void Factory_CreateForShard_ShouldReturnWorkingStore()
    {
        // Arrange
        var factory = new ReferenceTableStoreFactoryADO();

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
        var factory = new ReferenceTableStoreFactoryADO();
        using var store1 = (ReferenceTableStoreADO)factory.CreateForShard(_fixture.Shard1ConnectionString);
        using var store2 = (ReferenceTableStoreADO)factory.CreateForShard(_fixture.Shard2ConnectionString);

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

    private ReferenceTableStoreADO CreateStore(string shardId)
    {
        var connection = _fixture.CreateConnection(shardId);
        return new ReferenceTableStoreADO(connection);
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
