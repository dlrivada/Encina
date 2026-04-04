using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten.Projections;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DataLocationProjection"/> covering all event handlers
/// and projection creation.
/// </summary>
public class DataLocationProjectionTests
{
    private readonly DataLocationProjection _sut = new();

    private static ProjectionContext CreateContext(DateTime? timestamp = null) =>
        new() { Timestamp = timestamp ?? new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc) };

    [Fact]
    public void ProjectionName_ShouldBeDataLocationProjection()
    {
        _sut.ProjectionName.ShouldBe("DataLocationProjection");
    }

    [Fact]
    public void Create_DataLocationRegistered_ShouldCreateReadModel()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var storedAt = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, string> { ["cloud"] = "azure" };
        var @event = new DataLocationRegistered(
            locationId, "entity-1", "healthcare-data", "DE", StorageType.Primary,
            storedAt, metadata, "tenant-1", "mod-1");

        // Act
        var result = _sut.Create(@event, CreateContext());

        // Assert
        result.Id.ShouldBe(locationId);
        result.EntityId.ShouldBe("entity-1");
        result.DataCategory.ShouldBe("healthcare-data");
        result.RegionCode.ShouldBe("DE");
        result.StorageType.ShouldBe(StorageType.Primary);
        result.StoredAtUtc.ShouldBe(storedAt);
        result.Metadata.ShouldBe(metadata);
        result.TenantId.ShouldBe("tenant-1");
        result.ModuleId.ShouldBe("mod-1");
        result.IsRemoved.ShouldBeFalse();
        result.HasViolation.ShouldBeFalse();
        result.Version.ShouldBe(1);
    }

    [Fact]
    public void Apply_DataLocationMigrated_ShouldUpdateRegionCode()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var @event = new DataLocationMigrated(current.Id, "entity-1", "DE", "FR", "GDPR compliance");

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.RegionCode.ShouldBe("FR");
        result.Version.ShouldBe(2);
    }

    [Fact]
    public void Apply_DataLocationVerified_ShouldUpdateLastVerifiedAtUtc()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var verifiedAt = DateTimeOffset.UtcNow;
        var @event = new DataLocationVerified(current.Id, verifiedAt);

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.LastVerifiedAtUtc.ShouldBe(verifiedAt);
        result.Version.ShouldBe(2);
    }

    [Fact]
    public void Apply_DataLocationRemoved_ShouldMarkAsRemoved()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var @event = new DataLocationRemoved(current.Id, "entity-1", "GDPR erasure");

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.IsRemoved.ShouldBeTrue();
        result.Version.ShouldBe(2);
    }

    [Fact]
    public void Apply_SovereigntyViolationDetected_ShouldSetViolation()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        var @event = new SovereigntyViolationDetected(
            current.Id, "entity-1", "personal-data", "CN", "Data found in China");

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.HasViolation.ShouldBeTrue();
        result.ViolationDetails.ShouldBe("Data found in China");
        result.Version.ShouldBe(2);
    }

    [Fact]
    public void Apply_SovereigntyViolationResolved_ShouldClearViolation()
    {
        // Arrange
        var current = CreateDefaultReadModel();
        current.HasViolation = true;
        current.ViolationDetails = "Data found in China";
        var @event = new SovereigntyViolationResolved(current.Id, "entity-1", "Migrated to DE");

        // Act
        var result = _sut.Apply(@event, current, CreateContext());

        // Assert
        result.HasViolation.ShouldBeFalse();
        result.ViolationDetails.ShouldBeNull();
        result.Version.ShouldBe(2);
    }

    private static DataLocationReadModel CreateDefaultReadModel() => new()
    {
        Id = Guid.NewGuid(),
        EntityId = "entity-1",
        DataCategory = "personal-data",
        RegionCode = "DE",
        StorageType = StorageType.Primary,
        StoredAtUtc = DateTimeOffset.UtcNow,
        Version = 1
    };
}
