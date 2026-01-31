using Encina.DomainModeling;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class AuditFieldPopulatorTests
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

    private sealed class SoftDeletableEntity : ISoftDeletable
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class FullySoftDeletableEntity : IAuditableEntity, ISoftDeletable
    {
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
    }

    private sealed class NonAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region PopulateForCreate Tests

    [Fact]
    public void PopulateForCreate_WithICreatedAtUtc_SetsCreatedAtUtc()
    {
        // Arrange
        var entity = new CreatedAtOnlyEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        var result = AuditFieldPopulator.PopulateForCreate(entity, "user-123", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void PopulateForCreate_WithICreatedBy_SetsCreatedBy()
    {
        // Arrange
        var entity = new CreatedByOnlyEntity();
        var fakeTime = new FakeTimeProvider();

        // Act
        var result = AuditFieldPopulator.PopulateForCreate(entity, "user-123", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.CreatedBy.ShouldBe("user-123");
    }

    [Fact]
    public void PopulateForCreate_WithNullUserId_DoesNotSetCreatedBy()
    {
        // Arrange
        var entity = new CreatedByOnlyEntity { CreatedBy = null };
        var fakeTime = new FakeTimeProvider();

        // Act
        AuditFieldPopulator.PopulateForCreate(entity, null, fakeTime);

        // Assert
        entity.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public void PopulateForCreate_WithIAuditableEntity_SetsBothCreatedFields()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        AuditFieldPopulator.PopulateForCreate(entity, "user-123", fakeTime);

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe("user-123");
    }

    [Fact]
    public void PopulateForCreate_WithNonAuditableEntity_DoesNotThrow()
    {
        // Arrange
        var entity = new NonAuditableEntity { Name = "Test" };
        var fakeTime = new FakeTimeProvider();

        // Act
        var result = AuditFieldPopulator.PopulateForCreate(entity, "user-123", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.Name.ShouldBe("Test");
    }

    [Fact]
    public void PopulateForCreate_ReturnsEntityForChaining()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        var fakeTime = new FakeTimeProvider();

        // Act & Assert - fluent chaining should work
        var result = AuditFieldPopulator.PopulateForCreate(entity, "user", fakeTime);
        result.ShouldBe(entity);
    }

    #endregion

    #region PopulateForUpdate Tests

    [Fact]
    public void PopulateForUpdate_WithIModifiedAtUtc_SetsModifiedAtUtc()
    {
        // Arrange
        var entity = new ModifiedAtOnlyEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 16, 14, 45, 0, TimeSpan.Zero));

        // Act
        var result = AuditFieldPopulator.PopulateForUpdate(entity, "user-456", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 45, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void PopulateForUpdate_WithIModifiedBy_SetsModifiedBy()
    {
        // Arrange
        var entity = new ModifiedByOnlyEntity();
        var fakeTime = new FakeTimeProvider();

        // Act
        var result = AuditFieldPopulator.PopulateForUpdate(entity, "user-456", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.ModifiedBy.ShouldBe("user-456");
    }

    [Fact]
    public void PopulateForUpdate_WithNullUserId_DoesNotSetModifiedBy()
    {
        // Arrange
        var entity = new ModifiedByOnlyEntity { ModifiedBy = null };
        var fakeTime = new FakeTimeProvider();

        // Act
        AuditFieldPopulator.PopulateForUpdate(entity, null, fakeTime);

        // Assert
        entity.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    public void PopulateForUpdate_WithIAuditableEntity_SetsBothModifiedFields()
    {
        // Arrange
        var entity = new FullAuditableEntity
        {
            CreatedAtUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "creator"
        };
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 16, 14, 45, 0, TimeSpan.Zero));

        // Act
        AuditFieldPopulator.PopulateForUpdate(entity, "user-456", fakeTime);

        // Assert
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 45, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe("user-456");
        // Creation fields should remain unchanged
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe("creator");
    }

    [Fact]
    public void PopulateForUpdate_WithNonAuditableEntity_DoesNotThrow()
    {
        // Arrange
        var entity = new NonAuditableEntity { Name = "Test" };
        var fakeTime = new FakeTimeProvider();

        // Act
        var result = AuditFieldPopulator.PopulateForUpdate(entity, "user-456", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
    }

    #endregion

    #region PopulateForDelete Tests

    [Fact]
    public void PopulateForDelete_WithISoftDeletable_SetsDeletedFields()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 17, 9, 0, 0, TimeSpan.Zero));

        // Act
        var result = AuditFieldPopulator.PopulateForDelete(entity, "deleter", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
        result.IsDeleted.ShouldBeTrue();
        result.DeletedAtUtc.ShouldBe(new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc));
        result.DeletedBy.ShouldBe("deleter");
    }

    [Fact]
    public void PopulateForDelete_WithNullUserId_SetsIsDeletedAndDeletedAtButNotDeletedBy()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 17, 9, 0, 0, TimeSpan.Zero));

        // Act
        AuditFieldPopulator.PopulateForDelete(entity, null, fakeTime);

        // Assert
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAtUtc.ShouldNotBeNull();
        entity.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void PopulateForDelete_WithNonSoftDeletableEntity_DoesNotThrow()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        var fakeTime = new FakeTimeProvider();

        // Act
        var result = AuditFieldPopulator.PopulateForDelete(entity, "deleter", fakeTime);

        // Assert
        result.ShouldBeSameAs(entity);
    }

    [Fact]
    public void PopulateForDelete_WithFullySoftDeletableEntity_PreservesAuditFields()
    {
        // Arrange
        var entity = new FullySoftDeletableEntity
        {
            CreatedAtUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "creator",
            ModifiedAtUtc = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc),
            ModifiedBy = "modifier"
        };
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 17, 9, 0, 0, TimeSpan.Zero));

        // Act
        AuditFieldPopulator.PopulateForDelete(entity, "deleter", fakeTime);

        // Assert - soft delete fields are set
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAtUtc.ShouldBe(new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc));
        entity.DeletedBy.ShouldBe("deleter");
        // Audit fields remain unchanged
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe("creator");
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe("modifier");
    }

    #endregion

    #region RestoreFromDelete Tests

    [Fact]
    public void RestoreFromDelete_WithISoftDeletable_ClearsDeletedFields()
    {
        // Arrange
        var entity = new SoftDeletableEntity
        {
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc),
            DeletedBy = "deleter"
        };

        // Act
        var result = AuditFieldPopulator.RestoreFromDelete(entity);

        // Assert
        result.ShouldBeSameAs(entity);
        result.IsDeleted.ShouldBeFalse();
        result.DeletedAtUtc.ShouldBeNull();
        result.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void RestoreFromDelete_WithNonSoftDeletableEntity_DoesNotThrow()
    {
        // Arrange
        var entity = new FullAuditableEntity
        {
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "creator"
        };

        // Act
        var result = AuditFieldPopulator.RestoreFromDelete(entity);

        // Assert
        result.ShouldBeSameAs(entity);
        result.CreatedBy.ShouldBe("creator");
    }

    [Fact]
    public void RestoreFromDelete_WithFullySoftDeletableEntity_PreservesAuditFields()
    {
        // Arrange
        var entity = new FullySoftDeletableEntity
        {
            CreatedAtUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "creator",
            ModifiedAtUtc = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc),
            ModifiedBy = "modifier",
            IsDeleted = true,
            DeletedAtUtc = new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc),
            DeletedBy = "deleter"
        };

        // Act
        AuditFieldPopulator.RestoreFromDelete(entity);

        // Assert - soft delete fields are cleared
        entity.IsDeleted.ShouldBeFalse();
        entity.DeletedAtUtc.ShouldBeNull();
        entity.DeletedBy.ShouldBeNull();
        // Audit fields remain unchanged
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe("creator");
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe("modifier");
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public void PopulateForCreate_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        FullAuditableEntity entity = null!;
        var fakeTime = new FakeTimeProvider();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForCreate(entity, "user", fakeTime));
    }

    [Fact]
    public void PopulateForCreate_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        TimeProvider timeProvider = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForCreate(entity, "user", timeProvider));
    }

    [Fact]
    public void PopulateForUpdate_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        FullAuditableEntity entity = null!;
        var fakeTime = new FakeTimeProvider();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForUpdate(entity, "user", fakeTime));
    }

    [Fact]
    public void PopulateForUpdate_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        TimeProvider timeProvider = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForUpdate(entity, "user", timeProvider));
    }

    [Fact]
    public void PopulateForDelete_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        SoftDeletableEntity entity = null!;
        var fakeTime = new FakeTimeProvider();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForDelete(entity, "user", fakeTime));
    }

    [Fact]
    public void PopulateForDelete_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        TimeProvider timeProvider = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForDelete(entity, "user", timeProvider));
    }

    [Fact]
    public void RestoreFromDelete_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        SoftDeletableEntity entity = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.RestoreFromDelete(entity));
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void PopulateForCreate_And_PopulateForUpdate_CanBeChained()
    {
        // Arrange
        var entity = new FullAuditableEntity();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero));

        // Act - Simulate create then update
        AuditFieldPopulator.PopulateForCreate(entity, "creator", fakeTime);

        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 16, 14, 0, 0, TimeSpan.Zero));
        AuditFieldPopulator.PopulateForUpdate(entity, "modifier", fakeTime);

        // Assert
        entity.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        entity.CreatedBy.ShouldBe("creator");
        entity.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 0, 0, DateTimeKind.Utc));
        entity.ModifiedBy.ShouldBe("modifier");
    }

    #endregion
}
