using Encina.DomainModeling;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class AuditedEntityTests
{
    #region Test Entities

    private sealed class TestAuditedEntity : AuditedEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAuditedEntity(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void AuditedEntity_Constructor_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestAuditedEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
    }

    [Fact]
    public void AuditedEntity_Constructor_UsesSystemTimeProviderByDefault()
    {
        // Arrange & Act
        var entity = new TestAuditedEntity(Guid.NewGuid());

        // Assert
        // Entity should not throw when TimeProvider is null (uses System)
        entity.ShouldNotBeNull();
    }

    [Fact]
    public void AuditedEntity_Constructor_AcceptsCustomTimeProvider()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        var entity = new TestAuditedEntity(Guid.NewGuid(), fakeTime);

        // Assert - entity is created successfully
        entity.ShouldNotBeNull();
    }

    #endregion

    #region IAuditableEntity Implementation Tests

    [Fact]
    public void AuditedEntity_ImplementsIAuditableEntity()
    {
        // Arrange & Act
        var entity = new TestAuditedEntity(Guid.NewGuid());

        // Assert
        entity.ShouldBeAssignableTo<IAuditableEntity>();
        entity.ShouldBeAssignableTo<ICreatedAtUtc>();
        entity.ShouldBeAssignableTo<ICreatedBy>();
        entity.ShouldBeAssignableTo<IModifiedAtUtc>();
        entity.ShouldBeAssignableTo<IModifiedBy>();
    }

    [Fact]
    public void AuditedEntity_CreatedAtUtc_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        entity.CreatedAtUtc = expectedTime;

        // Assert
        entity.CreatedAtUtc.ShouldBe(expectedTime);
    }

    [Fact]
    public void AuditedEntity_CreatedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        const string expectedUser = "user-123";

        // Act
        entity.CreatedBy = expectedUser;

        // Assert
        entity.CreatedBy.ShouldBe(expectedUser);
    }

    [Fact]
    public void AuditedEntity_ModifiedAtUtc_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        var expectedTime = new DateTime(2024, 1, 16, 14, 45, 0, DateTimeKind.Utc);

        // Act
        entity.ModifiedAtUtc = expectedTime;

        // Assert
        entity.ModifiedAtUtc.ShouldBe(expectedTime);
    }

    [Fact]
    public void AuditedEntity_ModifiedBy_CanBeSetAndGet()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        const string expectedUser = "user-456";

        // Act
        entity.ModifiedBy = expectedUser;

        // Assert
        entity.ModifiedBy.ShouldBe(expectedUser);
    }

    [Fact]
    public void AuditedEntity_NullableProperties_CanBeNull()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid())
        {
            CreatedBy = "user",
            ModifiedAtUtc = DateTime.UtcNow,
            ModifiedBy = "modifier"
        };

        // Act
        entity.CreatedBy = null;
        entity.ModifiedAtUtc = null;
        entity.ModifiedBy = null;

        // Assert
        entity.CreatedBy.ShouldBeNull();
        entity.ModifiedAtUtc.ShouldBeNull();
        entity.ModifiedBy.ShouldBeNull();
    }

    #endregion

    #region Entity Equality Tests

    [Fact]
    public void AuditedEntity_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditedEntity(id);
        var entity2 = new TestAuditedEntity(id);

        // Act & Assert
        entity1.ShouldBe(entity2);
        entity1.Equals(entity2).ShouldBeTrue();
    }

    [Fact]
    public void AuditedEntity_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestAuditedEntity(Guid.NewGuid());
        var entity2 = new TestAuditedEntity(Guid.NewGuid());

        // Act & Assert
        entity1.ShouldNotBe(entity2);
        entity1.Equals(entity2).ShouldBeFalse();
    }

    [Fact]
    public void AuditedEntity_EqualityIgnoresAuditFields()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestAuditedEntity(id)
        {
            CreatedAtUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            CreatedBy = "user1"
        };
        var entity2 = new TestAuditedEntity(id)
        {
            CreatedAtUtc = new DateTime(2024, 2, 20, 15, 30, 0, DateTimeKind.Utc),
            CreatedBy = "user2"
        };

        // Act & Assert - Entities with same ID should be equal regardless of audit fields
        entity1.Equals(entity2).ShouldBeTrue();
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void AuditedEntity_CanRaiseDomainEvents()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        // Use reflection to access protected method
        var method = typeof(Entity<Guid>).GetMethod("AddDomainEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        method!.Invoke(entity, [domainEvent]);

        // Assert
        entity.DomainEvents.ShouldContain(domainEvent);
        entity.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void AuditedEntity_ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var entity = new TestAuditedEntity(Guid.NewGuid());
        var method = typeof(Entity<Guid>).GetMethod("AddDomainEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(entity, [new TestDomainEvent()]);
        method!.Invoke(entity, [new TestDomainEvent()]);

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.ShouldBeEmpty();
    }

    private sealed record TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    }

    #endregion
}
