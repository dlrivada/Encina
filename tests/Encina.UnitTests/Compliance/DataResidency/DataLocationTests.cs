using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataLocationTests
{
    [Fact]
    public void Create_WithRequiredParameters_ShouldSetAllProperties()
    {
        // Arrange
        var region = RegionRegistry.DE;

        // Act
        var location = DataLocation.Create(
            entityId: "entity-1",
            dataCategory: "personal-data",
            region: region,
            storageType: StorageType.Primary);

        // Assert
        location.Id.Should().NotBeNullOrEmpty();
        location.Id.Should().HaveLength(32);
        location.EntityId.Should().Be("entity-1");
        location.DataCategory.Should().Be("personal-data");
        location.Region.Should().Be(region);
        location.StorageType.Should().Be(StorageType.Primary);
        location.StoredAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        location.LastVerifiedAtUtc.Should().BeNull();
        location.Metadata.Should().BeNull();
    }

    [Fact]
    public void Create_WithMetadata_ShouldPreserveMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["provider"] = "Azure",
            ["datacenter"] = "westeurope"
        };

        // Act
        var location = DataLocation.Create(
            entityId: "entity-2",
            dataCategory: "financial-records",
            region: RegionRegistry.FR,
            storageType: StorageType.Replica,
            metadata: metadata);

        // Assert
        location.Metadata.Should().NotBeNull();
        location.Metadata.Should().HaveCount(2);
        location.Metadata!["provider"].Should().Be("Azure");
        location.Metadata!["datacenter"].Should().Be("westeurope");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var location1 = DataLocation.Create("e1", "cat1", RegionRegistry.DE);
        var location2 = DataLocation.Create("e2", "cat2", RegionRegistry.FR);

        // Assert
        location1.Id.Should().NotBe(location2.Id);
    }

    [Theory]
    [InlineData(StorageType.Primary)]
    [InlineData(StorageType.Replica)]
    [InlineData(StorageType.Cache)]
    [InlineData(StorageType.Backup)]
    [InlineData(StorageType.Archive)]
    public void Create_WithStorageType_ShouldPreserveValue(StorageType storageType)
    {
        // Act
        var location = DataLocation.Create(
            entityId: "entity-1",
            dataCategory: "data",
            region: RegionRegistry.DE,
            storageType: storageType);

        // Assert
        location.StorageType.Should().Be(storageType);
    }

    [Fact]
    public void Create_DefaultStorageType_ShouldBePrimary()
    {
        // Act
        var location = DataLocation.Create(
            entityId: "entity-1",
            dataCategory: "data",
            region: RegionRegistry.DE);

        // Assert
        location.StorageType.Should().Be(StorageType.Primary);
    }
}
