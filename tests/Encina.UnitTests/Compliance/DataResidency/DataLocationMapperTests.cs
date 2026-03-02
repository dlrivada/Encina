using System.Text.Json;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataLocationMapperTests
{
    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["key"] = "value" };
        var location = DataLocation.Create("entity-1", "personal-data", RegionRegistry.DE, StorageType.Primary, metadata);

        // Act
        var entity = DataLocationMapper.ToEntity(location);

        // Assert
        entity.Id.Should().Be(location.Id);
        entity.EntityId.Should().Be("entity-1");
        entity.DataCategory.Should().Be("personal-data");
        entity.RegionCode.Should().Be("DE");
        entity.StorageTypeValue.Should().Be((int)StorageType.Primary);
        entity.StoredAtUtc.Should().Be(location.StoredAtUtc);
        entity.LastVerifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithMetadata_ShouldSerializeToJson()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["provider"] = "Azure", ["dc"] = "west" };
        var location = DataLocation.Create("e1", "data", RegionRegistry.DE, metadata: metadata);

        // Act
        var entity = DataLocationMapper.ToEntity(location);

        // Assert
        entity.Metadata.Should().NotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata!);
        deserialized.Should().ContainKey("provider").WhoseValue.Should().Be("Azure");
    }

    [Fact]
    public void ToEntity_WithNullMetadata_ShouldSetNull()
    {
        // Arrange
        var location = DataLocation.Create("e1", "data", RegionRegistry.DE);

        // Act
        var entity = DataLocationMapper.ToEntity(location);

        // Assert
        entity.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullLocation_ShouldThrow()
    {
        // Act
        var act = () => DataLocationMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new DataLocationEntity
        {
            Id = "abc123",
            EntityId = "entity-1",
            DataCategory = "personal-data",
            RegionCode = "DE",
            StorageTypeValue = (int)StorageType.Replica,
            StoredAtUtc = DateTimeOffset.UtcNow,
            LastVerifiedAtUtc = null,
            Metadata = null
        };

        // Act
        var result = DataLocationMapper.ToDomain(entity);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("abc123");
        result.EntityId.Should().Be("entity-1");
        result.Region.Code.Should().Be("DE");
        result.StorageType.Should().Be(StorageType.Replica);
    }

    [Fact]
    public void ToDomain_WithJsonMetadata_ShouldDeserialize()
    {
        // Arrange
        var entity = new DataLocationEntity
        {
            Id = "id1",
            EntityId = "e1",
            DataCategory = "data",
            RegionCode = "DE",
            StorageTypeValue = 0,
            StoredAtUtc = DateTimeOffset.UtcNow,
            Metadata = "{\"key\":\"value\"}"
        };

        // Act
        var result = DataLocationMapper.ToDomain(entity);

        // Assert
        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
        result.Metadata!["key"].Should().Be("value");
    }

    [Fact]
    public void ToDomain_InvalidRegionCode_ShouldReturnNull()
    {
        // Arrange
        var entity = new DataLocationEntity
        {
            Id = "id1",
            EntityId = "e1",
            DataCategory = "data",
            RegionCode = "INVALID_REGION",
            StorageTypeValue = 0,
            StoredAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = DataLocationMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_InvalidStorageType_ShouldReturnNull()
    {
        // Arrange
        var entity = new DataLocationEntity
        {
            Id = "id1",
            EntityId = "e1",
            DataCategory = "data",
            RegionCode = "DE",
            StorageTypeValue = 999,
            StoredAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = DataLocationMapper.ToDomain(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrow()
    {
        // Act
        var act = () => DataLocationMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(StorageType.Primary)]
    [InlineData(StorageType.Replica)]
    [InlineData(StorageType.Cache)]
    [InlineData(StorageType.Backup)]
    [InlineData(StorageType.Archive)]
    public void RoundTrip_AllStorageTypes_ShouldPreserve(StorageType storageType)
    {
        // Arrange
        var location = DataLocation.Create("e1", "data", RegionRegistry.DE, storageType);

        // Act
        var entity = DataLocationMapper.ToEntity(location);
        var roundTripped = DataLocationMapper.ToDomain(entity);

        // Assert
        roundTripped.Should().NotBeNull();
        roundTripped!.StorageType.Should().Be(storageType);
    }
}
