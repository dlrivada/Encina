using Encina.DomainModeling;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.DomainModeling;

public class FullyAuditedEntityTests
{
    private sealed class TestFullyAuditedEntity : FullyAuditedEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestFullyAuditedEntity(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider) { }
    }

    [Fact]
    public void NewEntity_ShouldNotBeDeleted()
    {
        // Arrange & Act
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());

        // Assert
        entity.IsDeleted.ShouldBeFalse();
        entity.DeletedAtUtc.ShouldBeNull();
        entity.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Delete_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());

        // Act
        entity.Delete();

        // Assert
        entity.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Delete_ShouldSetDeletedAtUtcToCurrentTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        var entity = new TestFullyAuditedEntity(Guid.NewGuid(), fakeTimeProvider);

        // Act
        entity.Delete();

        // Assert
        entity.DeletedAtUtc.ShouldBe(fixedTime.UtcDateTime);
    }

    [Fact]
    public void Delete_WithDeletedBy_ShouldSetDeletedByProperty()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        const string userId = "user-123";

        // Act
        entity.Delete(userId);

        // Assert
        entity.DeletedBy.ShouldBe(userId);
    }

    [Fact]
    public void Delete_WithoutDeletedBy_ShouldLeaveDeletedByNull()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());

        // Act
        entity.Delete();

        // Assert
        entity.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Restore_ShouldSetIsDeletedToFalse()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        entity.Delete("user-123");

        // Act
        entity.Restore();

        // Assert
        entity.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Restore_ShouldClearDeletedAtUtc()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        entity.Delete("user-123");

        // Act
        entity.Restore();

        // Assert
        entity.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Restore_ShouldClearDeletedBy()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        entity.Delete("user-123");

        // Act
        entity.Restore();

        // Assert
        entity.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Delete_AfterRestore_ShouldMarkAsDeletedAgain()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(fixedTime);
        var entity = new TestFullyAuditedEntity(Guid.NewGuid(), fakeTimeProvider);
        entity.Delete("user-1");
        entity.Restore();

        // Act
        fakeTimeProvider.Advance(TimeSpan.FromHours(1));
        entity.Delete("user-2");

        // Assert
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAtUtc.ShouldBe(fixedTime.UtcDateTime.AddHours(1));
        entity.DeletedBy.ShouldBe("user-2");
    }

    [Fact]
    public void SoftDeleteProperties_ShouldHavePublicSetters()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        var deletedAt = DateTime.UtcNow;

        // Act - Using public setters (for interceptor compatibility)
        entity.IsDeleted = true;
        entity.DeletedAtUtc = deletedAt;
        entity.DeletedBy = "interceptor-user";

        // Assert
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAtUtc.ShouldBe(deletedAt);
        entity.DeletedBy.ShouldBe("interceptor-user");
    }

    [Fact]
    public void Entity_ShouldInheritAuditProperties()
    {
        // Arrange
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());
        var now = DateTime.UtcNow;

        // Act
        entity.CreatedAtUtc = now;
        entity.CreatedBy = "creator";
        entity.ModifiedAtUtc = now.AddHours(1);
        entity.ModifiedBy = "modifier";

        // Assert
        entity.CreatedAtUtc.ShouldBe(now);
        entity.CreatedBy.ShouldBe("creator");
        entity.ModifiedAtUtc.ShouldBe(now.AddHours(1));
        entity.ModifiedBy.ShouldBe("modifier");
    }

    [Fact]
    public void Entity_ShouldHaveCorrectId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestFullyAuditedEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
    }

    [Fact]
    public void Entity_Equality_ShouldBeBasedOnId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestFullyAuditedEntity(id) { Name = "Entity1" };
        var entity2 = new TestFullyAuditedEntity(id) { Name = "Entity2" };

        // Act & Assert
        entity1.ShouldBe(entity2);
    }

    [Fact]
    public void Entity_ShouldImplementISoftDeletableEntity()
    {
        // Arrange & Act
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());

        // Assert
        entity.ShouldBeAssignableTo<ISoftDeletableEntity>();
    }

    [Fact]
    public void Entity_ShouldImplementIAuditableEntity()
    {
        // Arrange & Act
        var entity = new TestFullyAuditedEntity(Guid.NewGuid());

        // Assert
        entity.ShouldBeAssignableTo<IAuditableEntity>();
    }
}
