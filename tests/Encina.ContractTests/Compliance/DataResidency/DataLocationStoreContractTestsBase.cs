#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

namespace Encina.ContractTests.Compliance.DataResidency;

public abstract class DataLocationStoreContractTestsBase
{
    protected abstract IDataLocationStore CreateStore();

    protected static DataLocation CreateLocation(
        string? entityId = null,
        string? dataCategory = null,
        Region? region = null,
        StorageType storageType = StorageType.Primary)
    {
        return DataLocation.Create(
            entityId: entityId ?? $"entity-{Guid.NewGuid():N}",
            dataCategory: dataCategory ?? "personal-data",
            region: region ?? RegionRegistry.DE,
            storageType: storageType);
    }

    [Fact]
    public async Task Contract_RecordAsync_ValidLocation_ShouldSucceed()
    {
        var store = CreateStore();
        var location = CreateLocation();

        var result = await store.RecordAsync(location);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetByEntityAsync_ShouldReturnMatchingLocations()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        await store.RecordAsync(CreateLocation(entityId: entityId));
        await store.RecordAsync(CreateLocation(entityId: entityId));
        await store.RecordAsync(CreateLocation()); // different entity

        var result = await store.GetByEntityAsync(entityId);

        var locations = result.Match(Right: l => l, Left: _ => []);
        locations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetByRegionAsync_ShouldReturnMatchingLocations()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateLocation(region: RegionRegistry.DE));
        await store.RecordAsync(CreateLocation(region: RegionRegistry.FR));
        await store.RecordAsync(CreateLocation(region: RegionRegistry.DE));

        var result = await store.GetByRegionAsync(RegionRegistry.DE);

        var locations = result.Match(Right: l => l, Left: _ => []);
        locations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_GetByCategoryAsync_ShouldReturnMatchingLocations()
    {
        var store = CreateStore();
        await store.RecordAsync(CreateLocation(dataCategory: "health"));
        await store.RecordAsync(CreateLocation(dataCategory: "finance"));
        await store.RecordAsync(CreateLocation(dataCategory: "health"));

        var result = await store.GetByCategoryAsync("health");

        var locations = result.Match(Right: l => l, Left: _ => []);
        locations.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_DeleteByEntityAsync_ShouldRemoveAllForEntity()
    {
        var store = CreateStore();
        var entityId = $"entity-{Guid.NewGuid():N}";
        await store.RecordAsync(CreateLocation(entityId: entityId));
        await store.RecordAsync(CreateLocation(entityId: entityId));

        var result = await store.DeleteByEntityAsync(entityId);

        result.IsRight.ShouldBeTrue();
        var retrieved = await store.GetByEntityAsync(entityId);
        var locations = retrieved.Match(Right: l => l, Left: _ => []);
        locations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Contract_GetByEntityAsync_NonExisting_ShouldReturnEmptyList()
    {
        var store = CreateStore();

        var result = await store.GetByEntityAsync("non-existing");

        result.IsRight.ShouldBeTrue();
        var locations = result.Match(Right: l => l, Left: _ => []);
        locations.ShouldBeEmpty();
    }
}
