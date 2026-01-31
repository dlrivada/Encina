using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class AuditInterfaceTests
{
    #region Test Entities

    private sealed class CreatedAtOnlyEntity : ICreatedAtUtc
    {
        public DateTime CreatedAtUtc { get; set; }
    }

    private sealed class CreatedByOnlyEntity : ICreatedBy
    {
        public string? CreatedBy { get; set; }
    }

    private sealed class ModifiedAtOnlyEntity : IModifiedAtUtc
    {
        public DateTime? ModifiedAtUtc { get; set; }
    }

    private sealed class ModifiedByOnlyEntity : IModifiedBy
    {
        public string? ModifiedBy { get; set; }
    }

    private sealed class FullAuditableEntity : IAuditableEntity
    {
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
    }

    private sealed class ImmutableAuditEntity : IAuditable
    {
        public DateTime CreatedAtUtc { get; init; }
        public string? CreatedBy { get; init; }
        public DateTime? ModifiedAtUtc { get; init; }
        public string? ModifiedBy { get; init; }
    }

    private sealed class SoftDeletableEntity : ISoftDeletable
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    #endregion

    #region ICreatedAtUtc Tests

    [Fact]
    public void ICreatedAtUtc_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new CreatedAtOnlyEntity();

        // Assert
        entity.ShouldBeAssignableTo<ICreatedAtUtc>();
    }

    [Fact]
    public void ICreatedAtUtc_CreatedAtUtc_CanBeSetAndGet()
    {
        // Arrange
        var entity = new CreatedAtOnlyEntity();
        var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        entity.CreatedAtUtc = expectedTime;

        // Assert
        entity.CreatedAtUtc.ShouldBe(expectedTime);
    }

    #endregion

    #region ICreatedBy Tests

    [Fact]
    public void ICreatedBy_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new CreatedByOnlyEntity();

        // Assert
        entity.ShouldBeAssignableTo<ICreatedBy>();
    }

    [Fact]
    public void ICreatedBy_CreatedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new CreatedByOnlyEntity();
        const string expectedUser = "user-123";

        // Act
        entity.CreatedBy = expectedUser;

        // Assert
        entity.CreatedBy.ShouldBe(expectedUser);
    }

    [Fact]
    public void ICreatedBy_CreatedBy_CanBeNull()
    {
        // Arrange
        var entity = new CreatedByOnlyEntity { CreatedBy = "user-123" };

        // Act
        entity.CreatedBy = null;

        // Assert
        entity.CreatedBy.ShouldBeNull();
    }

    #endregion

    #region IModifiedAtUtc Tests

    [Fact]
    public void IModifiedAtUtc_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new ModifiedAtOnlyEntity();

        // Assert
        entity.ShouldBeAssignableTo<IModifiedAtUtc>();
    }

    [Fact]
    public void IModifiedAtUtc_ModifiedAtUtc_CanBeSetAndGet()
    {
        // Arrange
        var entity = new ModifiedAtOnlyEntity();
        var expectedTime = new DateTime(2024, 1, 15, 14, 45, 0, DateTimeKind.Utc);

        // Act
        entity.ModifiedAtUtc = expectedTime;

        // Assert
        entity.ModifiedAtUtc.ShouldBe(expectedTime);
    }

    [Fact]
    public void IModifiedAtUtc_ModifiedAtUtc_CanBeNull()
    {
        // Arrange
        var entity = new ModifiedAtOnlyEntity
        {
            ModifiedAtUtc = new DateTime(2024, 1, 15, 14, 45, 0, DateTimeKind.Utc)
        };

        // Act
        entity.ModifiedAtUtc = null;

        // Assert
        entity.ModifiedAtUtc.ShouldBeNull();
    }

    #endregion

    #region IModifiedBy Tests

    [Fact]
    public void IModifiedBy_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new ModifiedByOnlyEntity();

        // Assert
        entity.ShouldBeAssignableTo<IModifiedBy>();
    }

    [Fact]
    public void IModifiedBy_ModifiedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new ModifiedByOnlyEntity();
        const string expectedUser = "user-456";

        // Act
        entity.ModifiedBy = expectedUser;

        // Assert
        entity.ModifiedBy.ShouldBe(expectedUser);
    }

    [Fact]
    public void IModifiedBy_ModifiedBy_CanBeNull()
    {
        // Arrange
        var entity = new ModifiedByOnlyEntity { ModifiedBy = "user-456" };

        // Act
        entity.ModifiedBy = null;

        // Assert
        entity.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void IAuditableEntity_ImplementsAllGranularInterfaces()
    {
        // Arrange & Act
        var entity = new FullAuditableEntity();

        // Assert
        entity.ShouldBeAssignableTo<ICreatedAtUtc>();
        entity.ShouldBeAssignableTo<ICreatedBy>();
        entity.ShouldBeAssignableTo<IModifiedAtUtc>();
        entity.ShouldBeAssignableTo<IModifiedBy>();
        entity.ShouldBeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void IAuditableEntity_AllProperties_CanBeSetAndGet()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        var createdAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var modifiedAt = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc);
        const string createdBy = "creator";
        const string modifiedBy = "modifier";

        // Act
        entity.CreatedAtUtc = createdAt;
        entity.CreatedBy = createdBy;
        entity.ModifiedAtUtc = modifiedAt;
        entity.ModifiedBy = modifiedBy;

        // Assert
        entity.CreatedAtUtc.ShouldBe(createdAt);
        entity.CreatedBy.ShouldBe(createdBy);
        entity.ModifiedAtUtc.ShouldBe(modifiedAt);
        entity.ModifiedBy.ShouldBe(modifiedBy);
    }

    #endregion

    #region IAuditable Tests

    [Fact]
    public void IAuditable_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new ImmutableAuditEntity();

        // Assert
        entity.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void IAuditable_AllProperties_CanBeRead()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var modifiedAt = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc);
        var entity = new ImmutableAuditEntity
        {
            CreatedAtUtc = createdAt,
            CreatedBy = "creator",
            ModifiedAtUtc = modifiedAt,
            ModifiedBy = "modifier"
        };

        // Act & Assert
        entity.CreatedAtUtc.ShouldBe(createdAt);
        entity.CreatedBy.ShouldBe("creator");
        entity.ModifiedAtUtc.ShouldBe(modifiedAt);
        entity.ModifiedBy.ShouldBe("modifier");
    }

    #endregion

    #region ISoftDeletable Tests

    [Fact]
    public void ISoftDeletable_CanBeImplemented()
    {
        // Arrange & Act
        var entity = new SoftDeletableEntity();

        // Assert
        entity.ShouldBeAssignableTo<ISoftDeletable>();
    }

    [Fact]
    public void ISoftDeletable_AllProperties_CanBeSetAndGet()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        var deletedAt = new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc);
        const string deletedBy = "deleter";

        // Act
        entity.IsDeleted = true;
        entity.DeletedAtUtc = deletedAt;
        entity.DeletedBy = deletedBy;

        // Assert
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAtUtc.ShouldBe(deletedAt);
        entity.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void ISoftDeletable_DefaultValues_AreNotDeleted()
    {
        // Arrange & Act
        var entity = new SoftDeletableEntity();

        // Assert
        entity.IsDeleted.ShouldBeFalse();
        entity.DeletedAtUtc.ShouldBeNull();
        entity.DeletedBy.ShouldBeNull();
    }

    #endregion

    #region Interface Composition Tests

    [Fact]
    public void GranularInterfaces_CanBeCombinedSelectively()
    {
        // This test verifies that you can implement only the interfaces you need
        var createdAtOnly = new CreatedAtOnlyEntity();
        var createdByOnly = new CreatedByOnlyEntity();
        var modifiedAtOnly = new ModifiedAtOnlyEntity();
        var modifiedByOnly = new ModifiedByOnlyEntity();

        createdAtOnly.ShouldBeAssignableTo<ICreatedAtUtc>();
        createdAtOnly.ShouldNotBeAssignableTo<ICreatedBy>();

        createdByOnly.ShouldBeAssignableTo<ICreatedBy>();
        createdByOnly.ShouldNotBeAssignableTo<ICreatedAtUtc>();

        modifiedAtOnly.ShouldBeAssignableTo<IModifiedAtUtc>();
        modifiedAtOnly.ShouldNotBeAssignableTo<IModifiedBy>();

        modifiedByOnly.ShouldBeAssignableTo<IModifiedBy>();
        modifiedByOnly.ShouldNotBeAssignableTo<IModifiedAtUtc>();
    }

    #endregion
}
