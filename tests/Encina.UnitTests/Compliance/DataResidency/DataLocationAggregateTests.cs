using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;

namespace Encina.UnitTests.Compliance.DataResidency;

public class DataLocationAggregateTests
{
    private static readonly DateTimeOffset DefaultStoredAt = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, string> DefaultMetadata =
        new Dictionary<string, string> { ["provider"] = "azure", ["cluster"] = "westeu-01" };

    #region Register

    [Fact]
    public void Register_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var location = CreateActiveLocation(
            id, "cust-123", "personal-data", "EU", StorageType.Primary,
            tenantId: "tenant-1", moduleId: "module-1");

        // Assert
        location.Id.ShouldBe(id);
        location.EntityId.ShouldBe("cust-123");
        location.DataCategory.ShouldBe("personal-data");
        location.RegionCode.ShouldBe("EU");
        location.StorageType.ShouldBe(StorageType.Primary);
        location.StoredAtUtc.ShouldBe(DefaultStoredAt);
        location.Metadata.ShouldBe(DefaultMetadata);
        location.IsRemoved.ShouldBeFalse();
        location.HasViolation.ShouldBeFalse();
        location.ViolationDetails.ShouldBeNull();
        location.LastVerifiedAtUtc.ShouldBeNull();
        location.TenantId.ShouldBe("tenant-1");
        location.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Register_RaisesDataLocationRegisteredEvent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var location = CreateActiveLocation(id, "cust-456", "healthcare-data", "DE", StorageType.Backup);

        // Assert
        location.UncommittedEvents.Count.ShouldBe(1);
        var evt = location.UncommittedEvents[0].ShouldBeOfType<DataLocationRegistered>();
        evt.LocationId.ShouldBe(id);
        evt.EntityId.ShouldBe("cust-456");
        evt.DataCategory.ShouldBe("healthcare-data");
        evt.RegionCode.ShouldBe("DE");
        evt.StorageType.ShouldBe(StorageType.Backup);
        evt.StoredAtUtc.ShouldBe(DefaultStoredAt);
        evt.Metadata.ShouldBe(DefaultMetadata);
    }

    [Fact]
    public void Register_WithNullEntityId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), null!, "personal-data", "EU", StorageType.Primary, DefaultStoredAt);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Register_WithNullDataCategory_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "cust-1", null!, "EU", StorageType.Primary, DefaultStoredAt);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Register_WithNullRegionCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "cust-1", "personal-data", null!, StorageType.Primary, DefaultStoredAt);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("regionCode");
    }

    [Fact]
    public void Register_WithWhitespaceRegionCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "cust-1", "personal-data", "  ", StorageType.Primary, DefaultStoredAt);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("regionCode");
    }

    #endregion

    #region Migrate

    [Fact]
    public void Migrate_UpdatesRegionCode()
    {
        // Arrange
        var location = CreateActiveLocation(regionCode: "EU");

        // Act
        location.Migrate("US", "Business expansion");

        // Assert
        location.RegionCode.ShouldBe("US");
    }

    [Fact]
    public void Migrate_RaisesDataLocationMigratedEvent()
    {
        // Arrange
        var location = CreateActiveLocation(regionCode: "EU");

        // Act
        location.Migrate("JP", "APAC compliance");

        // Assert
        location.UncommittedEvents.Count.ShouldBe(2); // Registered + Migrated
        var evt = location.UncommittedEvents[1].ShouldBeOfType<DataLocationMigrated>();
        evt.LocationId.ShouldBe(location.Id);
        evt.EntityId.ShouldBe(location.EntityId);
        evt.PreviousRegionCode.ShouldBe("EU");
        evt.NewRegionCode.ShouldBe("JP");
        evt.Reason.ShouldBe("APAC compliance");
    }

    [Fact]
    public void Migrate_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateRemovedLocation();

        // Act
        var act = () => location.Migrate("US", "Relocation");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Migrate_WithNullRegionCode_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.Migrate(null!, "Some reason");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("newRegionCode");
    }

    [Fact]
    public void Migrate_WithNullReason_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.Migrate("US", null!);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("reason");
    }

    #endregion

    #region Verify

    [Fact]
    public void Verify_UpdatesLastVerifiedAtUtc()
    {
        // Arrange
        var location = CreateActiveLocation();
        var verifiedAt = DateTimeOffset.UtcNow;

        // Act
        location.Verify(verifiedAt);

        // Assert
        location.LastVerifiedAtUtc.ShouldBe(verifiedAt);
    }

    [Fact]
    public void Verify_RaisesDataLocationVerifiedEvent()
    {
        // Arrange
        var location = CreateActiveLocation();
        var verifiedAt = new DateTimeOffset(2026, 3, 15, 14, 0, 0, TimeSpan.Zero);

        // Act
        location.Verify(verifiedAt);

        // Assert
        location.UncommittedEvents.Count.ShouldBe(2); // Registered + Verified
        var evt = location.UncommittedEvents[1].ShouldBeOfType<DataLocationVerified>();
        evt.LocationId.ShouldBe(location.Id);
        evt.VerifiedAtUtc.ShouldBe(verifiedAt);
    }

    [Fact]
    public void Verify_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateRemovedLocation();

        // Act
        var act = () => location.Verify(DateTimeOffset.UtcNow);

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    #endregion

    #region Remove

    [Fact]
    public void Remove_SetsIsRemovedTrue()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        location.Remove("Data deleted per GDPR Art. 17");

        // Assert
        location.IsRemoved.ShouldBeTrue();
    }

    [Fact]
    public void Remove_RaisesDataLocationRemovedEvent()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        location.Remove("Cache expired");

        // Assert
        location.UncommittedEvents.Count.ShouldBe(2); // Registered + Removed
        var evt = location.UncommittedEvents[1].ShouldBeOfType<DataLocationRemoved>();
        evt.LocationId.ShouldBe(location.Id);
        evt.EntityId.ShouldBe(location.EntityId);
        evt.Reason.ShouldBe("Cache expired");
    }

    [Fact]
    public void Remove_WhenAlreadyRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateRemovedLocation();

        // Act
        var act = () => location.Remove("Double remove");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Remove_WithNullReason_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.Remove(null!);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("reason");
    }

    #endregion

    #region DetectViolation

    [Fact]
    public void DetectViolation_SetsHasViolationTrue()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        location.DetectViolation("personal-data", "CN", "Data found in non-allowed region");

        // Assert
        location.HasViolation.ShouldBeTrue();
        location.ViolationDetails.ShouldBe("Data found in non-allowed region");
    }

    [Fact]
    public void DetectViolation_RaisesSovereigntyViolationDetectedEvent()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        location.DetectViolation("healthcare-data", "RU", "Unauthorized replication");

        // Assert
        location.UncommittedEvents.Count.ShouldBe(2); // Registered + ViolationDetected
        var evt = location.UncommittedEvents[1].ShouldBeOfType<SovereigntyViolationDetected>();
        evt.LocationId.ShouldBe(location.Id);
        evt.EntityId.ShouldBe(location.EntityId);
        evt.DataCategory.ShouldBe("healthcare-data");
        evt.ViolatingRegionCode.ShouldBe("RU");
        evt.Details.ShouldBe("Unauthorized replication");
    }

    [Fact]
    public void DetectViolation_WhenRemoved_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateRemovedLocation();

        // Act
        var act = () => location.DetectViolation("personal-data", "CN", "Violation details");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void DetectViolation_WhenAlreadyViolated_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateViolatedLocation();

        // Act
        var act = () => location.DetectViolation("personal-data", "BR", "Second violation");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void DetectViolation_WithNullDataCategory_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.DetectViolation(null!, "CN", "Details");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void DetectViolation_WithNullViolatingRegionCode_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.DetectViolation("personal-data", null!, "Details");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("violatingRegionCode");
    }

    [Fact]
    public void DetectViolation_WithNullDetails_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.DetectViolation("personal-data", "CN", null!);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("details");
    }

    #endregion

    #region ResolveViolation

    [Fact]
    public void ResolveViolation_ClearsViolation()
    {
        // Arrange
        var location = CreateViolatedLocation();

        // Act
        location.ResolveViolation("Migrated data to EU region");

        // Assert
        location.HasViolation.ShouldBeFalse();
        location.ViolationDetails.ShouldBeNull();
    }

    [Fact]
    public void ResolveViolation_RaisesSovereigntyViolationResolvedEvent()
    {
        // Arrange
        var location = CreateViolatedLocation();

        // Act
        location.ResolveViolation("Data removed from violating region");

        // Assert
        var evt = location.UncommittedEvents[^1].ShouldBeOfType<SovereigntyViolationResolved>();
        evt.LocationId.ShouldBe(location.Id);
        evt.EntityId.ShouldBe(location.EntityId);
        evt.Resolution.ShouldBe("Data removed from violating region");
    }

    [Fact]
    public void ResolveViolation_WhenNoViolation_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = CreateActiveLocation();

        // Act
        var act = () => location.ResolveViolation("Nothing to resolve");

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void ResolveViolation_WithNullResolution_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateViolatedLocation();

        // Act
        var act = () => location.ResolveViolation(null!);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("resolution");
    }

    [Fact]
    public void ResolveViolation_WithWhitespaceResolution_ThrowsArgumentException()
    {
        // Arrange
        var location = CreateViolatedLocation();

        // Act
        var act = () => location.ResolveViolation("   ");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .ParamName.ShouldBe("resolution");
    }

    #endregion

    #region Helpers

    private static DataLocationAggregate CreateActiveLocation(
        Guid? id = null,
        string entityId = "entity-001",
        string dataCategory = "personal-data",
        string regionCode = "EU",
        StorageType storageType = StorageType.Primary,
        string? tenantId = null,
        string? moduleId = null)
    {
        return DataLocationAggregate.Register(
            id ?? Guid.NewGuid(),
            entityId,
            dataCategory,
            regionCode,
            storageType,
            DefaultStoredAt,
            DefaultMetadata,
            tenantId,
            moduleId);
    }

    private static DataLocationAggregate CreateRemovedLocation()
    {
        var location = CreateActiveLocation();
        location.Remove("Test removal");
        return location;
    }

    private static DataLocationAggregate CreateViolatedLocation()
    {
        var location = CreateActiveLocation();
        location.DetectViolation("personal-data", "CN", "Data in non-compliant region");
        return location;
    }

    #endregion
}
