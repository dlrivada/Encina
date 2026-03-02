using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataResidency;

public class InMemoryDataLocationStoreTests
{
    private readonly InMemoryDataLocationStore _store;

    public InMemoryDataLocationStoreTests()
    {
        _store = new InMemoryDataLocationStore(NullLogger<InMemoryDataLocationStore>.Instance);
    }

    [Fact]
    public async Task RecordAsync_ValidLocation_ShouldSucceed()
    {
        // Arrange
        var location = DataLocation.Create("entity-1", "personal-data", RegionRegistry.DE);

        // Act
        var result = await _store.RecordAsync(location);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_MultipleLocationsForSameEntity_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(DataLocation.Create("entity-1", "data", RegionRegistry.DE, StorageType.Primary));
        await _store.RecordAsync(DataLocation.Create("entity-1", "data", RegionRegistry.FR, StorageType.Replica));

        // Assert
        _store.Count.Should().Be(2);
        var result = await _store.GetByEntityAsync("entity-1");
        result.Match(
            Right: locations => locations.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByEntityAsync_ExistingEntity_ShouldReturnLocations()
    {
        // Arrange
        await _store.RecordAsync(DataLocation.Create("entity-1", "data", RegionRegistry.DE));

        // Act
        var result = await _store.GetByEntityAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: locations => locations.Should().ContainSingle(),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByEntityAsync_NonExistingEntity_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetByEntityAsync("unknown");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: locations => locations.Should().BeEmpty(),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByRegionAsync_ShouldReturnLocationsInRegion()
    {
        // Arrange
        await _store.RecordAsync(DataLocation.Create("e1", "data", RegionRegistry.DE));
        await _store.RecordAsync(DataLocation.Create("e2", "data", RegionRegistry.FR));
        await _store.RecordAsync(DataLocation.Create("e3", "data", RegionRegistry.DE));

        // Act
        var result = await _store.GetByRegionAsync(RegionRegistry.DE);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: locations => locations.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnLocationsForCategory()
    {
        // Arrange
        await _store.RecordAsync(DataLocation.Create("e1", "personal-data", RegionRegistry.DE));
        await _store.RecordAsync(DataLocation.Create("e2", "financial-data", RegionRegistry.FR));
        await _store.RecordAsync(DataLocation.Create("e3", "personal-data", RegionRegistry.IT));

        // Act
        var result = await _store.GetByCategoryAsync("personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: locations => locations.Should().HaveCount(2),
            Left: _ => { });
    }

    [Fact]
    public async Task DeleteByEntityAsync_ShouldRemoveAllLocationsForEntity()
    {
        // Arrange
        await _store.RecordAsync(DataLocation.Create("entity-1", "data", RegionRegistry.DE));
        await _store.RecordAsync(DataLocation.Create("entity-1", "data", RegionRegistry.FR));
        await _store.RecordAsync(DataLocation.Create("entity-2", "data", RegionRegistry.DE));

        // Act
        var result = await _store.DeleteByEntityAsync("entity-1");

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllLocations()
    {
        // Arrange
        await _store.RecordAsync(DataLocation.Create("e1", "data", RegionRegistry.DE));
        await _store.RecordAsync(DataLocation.Create("e2", "data", RegionRegistry.FR));

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }
}
