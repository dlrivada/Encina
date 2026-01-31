using Encina.DomainModeling;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.DomainModeling.Auditing;

/// <summary>
/// Guard tests for AuditFieldPopulator to verify null parameter handling.
/// </summary>
public class AuditFieldPopulatorGuardTests
{
    #region Test Entities

    private sealed class AuditableEntity : IAuditableEntity
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

    #endregion

    #region PopulateForCreate Guard Tests

    [Fact]
    public void PopulateForCreate_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        AuditableEntity entity = null!;
        var timeProvider = new FakeTimeProvider();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForCreate(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void PopulateForCreate_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new AuditableEntity();
        TimeProvider timeProvider = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForCreate(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void PopulateForCreate_NullUserId_DoesNotThrow()
    {
        // Arrange
        var entity = new AuditableEntity();
        var timeProvider = new FakeTimeProvider();

        // Act & Assert - null userId is valid (anonymous user)
        Should.NotThrow(() =>
            AuditFieldPopulator.PopulateForCreate(entity, null, timeProvider));
    }

    #endregion

    #region PopulateForUpdate Guard Tests

    [Fact]
    public void PopulateForUpdate_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        AuditableEntity entity = null!;
        var timeProvider = new FakeTimeProvider();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForUpdate(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void PopulateForUpdate_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new AuditableEntity();
        TimeProvider timeProvider = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForUpdate(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void PopulateForUpdate_NullUserId_DoesNotThrow()
    {
        // Arrange
        var entity = new AuditableEntity();
        var timeProvider = new FakeTimeProvider();

        // Act & Assert - null userId is valid
        Should.NotThrow(() =>
            AuditFieldPopulator.PopulateForUpdate(entity, null, timeProvider));
    }

    #endregion

    #region PopulateForDelete Guard Tests

    [Fact]
    public void PopulateForDelete_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        SoftDeletableEntity entity = null!;
        var timeProvider = new FakeTimeProvider();

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForDelete(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("entity");
    }

    [Fact]
    public void PopulateForDelete_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        TimeProvider timeProvider = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.PopulateForDelete(entity, "user", timeProvider));

        // Assert
        exception.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void PopulateForDelete_NullUserId_DoesNotThrow()
    {
        // Arrange
        var entity = new SoftDeletableEntity();
        var timeProvider = new FakeTimeProvider();

        // Act & Assert - null userId is valid
        Should.NotThrow(() =>
            AuditFieldPopulator.PopulateForDelete(entity, null, timeProvider));
    }

    #endregion

    #region RestoreFromDelete Guard Tests

    [Fact]
    public void RestoreFromDelete_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        SoftDeletableEntity entity = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            AuditFieldPopulator.RestoreFromDelete(entity));

        // Assert
        exception.ParamName.ShouldBe("entity");
    }

    #endregion
}
