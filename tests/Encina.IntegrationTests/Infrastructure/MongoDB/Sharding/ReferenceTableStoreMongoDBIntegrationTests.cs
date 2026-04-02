using System.ComponentModel.DataAnnotations;
using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Sharding;

/// <summary>
/// Integration tests for <see cref="ReferenceTableStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ReferenceTableStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private ReferenceTableStoreMongoDB? _store;

    public ReferenceTableStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return ValueTask.CompletedTask;
        }

        _store = new ReferenceTableStoreMongoDB(_fixture.Database!);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture.IsAvailable)
        {
            // Clean up the collection used by EntityMetadataCache (based on entity type name)
            var collection = _fixture.Database!.GetCollection<ReferenceCountry>(
                nameof(ReferenceCountry));
            await collection.DeleteManyAsync(Builders<ReferenceCountry>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<ReferenceCountry>(
                nameof(ReferenceCountry));
            await collection.DeleteManyAsync(Builders<ReferenceCountry>.Filter.Empty);
        }
    }

    private void SkipIfNotAvailable()
    {
        if (!_fixture.IsAvailable || _store is null)
        {
            Assert.Skip("MongoDB container is not available");
        }
    }

    #region UpsertAsync Tests

    [Fact]
    public async Task UpsertAsync_NewEntities_InsertsAll()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var countries = new[]
        {
            new ReferenceCountry { Id = "US", Name = "United States", IsoCode = "USA" },
            new ReferenceCountry { Id = "GB", Name = "United Kingdom", IsoCode = "GBR" },
            new ReferenceCountry { Id = "DE", Name = "Germany", IsoCode = "DEU" }
        };

        // Act
        var result = await _store!.UpsertAsync(countries);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task UpsertAsync_ExistingEntities_UpdatesThem()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - insert initial data
        var initial = new[]
        {
            new ReferenceCountry { Id = "FR", Name = "France", IsoCode = "FRA" }
        };
        await _store!.UpsertAsync(initial);

        // Update name
        var updated = new[]
        {
            new ReferenceCountry { Id = "FR", Name = "French Republic", IsoCode = "FRA" }
        };

        // Act
        var result = await _store.UpsertAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();

        var allResult = await _store.GetAllAsync<ReferenceCountry>();
        allResult.IsRight.ShouldBeTrue();
        allResult.IfRight(entities =>
        {
            entities.ShouldHaveSingleItem();
            entities[0].Name.ShouldBe("French Republic");
        });
    }

    [Fact]
    public async Task UpsertAsync_EmptyCollection_ReturnsZero()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _store!.UpsertAsync(Array.Empty<ReferenceCountry>());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task UpsertAsync_MixOfNewAndExisting_HandlesCorrectly()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - insert initial data
        var initial = new[]
        {
            new ReferenceCountry { Id = "JP", Name = "Japan", IsoCode = "JPN" }
        };
        await _store!.UpsertAsync(initial);

        // Mix of update and insert
        var mixed = new[]
        {
            new ReferenceCountry { Id = "JP", Name = "Japan (Updated)", IsoCode = "JPN" },
            new ReferenceCountry { Id = "KR", Name = "South Korea", IsoCode = "KOR" }
        };

        // Act
        var result = await _store.UpsertAsync(mixed);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBeGreaterThanOrEqualTo(2));

        var allResult = await _store.GetAllAsync<ReferenceCountry>();
        allResult.IsRight.ShouldBeTrue();
        allResult.IfRight(entities =>
        {
            entities.Count.ShouldBe(2);
            entities.ShouldContain(e => e.Id == "JP" && e.Name == "Japan (Updated)");
            entities.ShouldContain(e => e.Id == "KR" && e.Name == "South Korea");
        });
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyCollection_ReturnsEmptyList()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _store!.GetAllAsync<ReferenceCountry>();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities => entities.ShouldBeEmpty());
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllEntities()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var countries = new[]
        {
            new ReferenceCountry { Id = "IT", Name = "Italy", IsoCode = "ITA" },
            new ReferenceCountry { Id = "ES", Name = "Spain", IsoCode = "ESP" }
        };
        await _store!.UpsertAsync(countries);

        // Act
        var result = await _store.GetAllAsync<ReferenceCountry>();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(entities =>
        {
            entities.Count.ShouldBe(2);
            entities.ShouldContain(e => e.Id == "IT");
            entities.ShouldContain(e => e.Id == "ES");
        });
    }

    #endregion

    #region GetHashAsync Tests

    [Fact]
    public async Task GetHashAsync_EmptyCollection_ReturnsHash()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _store!.GetHashAsync<ReferenceCountry>();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(hash =>
        {
            hash.ShouldNotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task GetHashAsync_SameData_ReturnsSameHash()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var countries = new[]
        {
            new ReferenceCountry { Id = "AU", Name = "Australia", IsoCode = "AUS" }
        };
        await _store!.UpsertAsync(countries);

        // Act
        var hash1 = await _store.GetHashAsync<ReferenceCountry>();
        var hash2 = await _store.GetHashAsync<ReferenceCountry>();

        // Assert
        var hashValue1 = hash1.ShouldBeRight();
        var hashValue2 = hash2.ShouldBeRight();

        hashValue1.ShouldBe(hashValue2);
    }

    [Fact]
    public async Task GetHashAsync_DifferentData_ReturnsDifferentHash()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - first dataset
        var countries1 = new[]
        {
            new ReferenceCountry { Id = "CA", Name = "Canada", IsoCode = "CAN" }
        };
        await _store!.UpsertAsync(countries1);

        var hash1 = (await _store.GetHashAsync<ReferenceCountry>()).ShouldBeRight();

        // Modify data
        var countries2 = new[]
        {
            new ReferenceCountry { Id = "CA", Name = "Canada (Modified)", IsoCode = "CAN" }
        };
        await _store.UpsertAsync(countries2);

        // Act
        var hash2 = (await _store.GetHashAsync<ReferenceCountry>()).ShouldBeRight();

        // Assert
        hash1.ShouldNotBeNull();
        hash2.ShouldNotBeNull();
        hash1.ShouldNotBe(hash2);
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Reference table entity for integration tests.
/// Uses string Id convention detected by <see cref="EntityMetadataCache"/>.
/// </summary>
public class ReferenceCountry
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "GB").
    /// Detected as primary key by convention (property named "Id").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string IsoCode { get; set; } = string.Empty;
}

#endregion
