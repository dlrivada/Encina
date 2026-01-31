using Encina.DomainModeling;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.DomainModeling.Auditing;

public class AuditedAggregateRootTests
{
    #region Test Aggregates

    private sealed class TestAuditedAggregate : AuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAuditedAggregate(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
        }
    }

    private sealed class TestFullyAuditedAggregate : FullyAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestFullyAuditedAggregate(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
        }
    }

    private sealed class TestAuditableAggregate : AuditableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAuditableAggregate(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
        }

        public void DoSetCreatedBy(string createdBy) => SetCreatedBy(createdBy);
        public void DoSetModifiedBy(string modifiedBy) => SetModifiedBy(modifiedBy);
    }

    private sealed class TestSoftDeletableAggregate : SoftDeletableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestSoftDeletableAggregate(Guid id, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
        }
    }

    #endregion

    #region AuditedAggregateRoot Tests

    [Fact]
    public void AuditedAggregateRoot_Constructor_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAuditedAggregate(id);

        // Assert
        aggregate.Id.ShouldBe(id);
    }

    [Fact]
    public void AuditedAggregateRoot_ImplementsIAuditableEntity()
    {
        // Arrange & Act
        var aggregate = new TestAuditedAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IAuditableEntity>();
        aggregate.ShouldBeAssignableTo<ICreatedAtUtc>();
        aggregate.ShouldBeAssignableTo<ICreatedBy>();
        aggregate.ShouldBeAssignableTo<IModifiedAtUtc>();
        aggregate.ShouldBeAssignableTo<IModifiedBy>();
    }

    [Fact]
    public void AuditedAggregateRoot_ImplementsIConcurrencyAware()
    {
        // Arrange & Act
        var aggregate = new TestAuditedAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IConcurrencyAware>();
    }

    [Fact]
    public void AuditedAggregateRoot_RowVersion_CanBeSetAndGet()
    {
        // Arrange
        var aggregate = new TestAuditedAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        // Act
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    [Fact]
    public void AuditedAggregateRoot_AllAuditProperties_CanBeSetAndGet()
    {
        // Arrange
        var aggregate = new TestAuditedAggregate(Guid.NewGuid());
        var createdAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var modifiedAt = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc);

        // Act
        aggregate.CreatedAtUtc = createdAt;
        aggregate.CreatedBy = "creator";
        aggregate.ModifiedAtUtc = modifiedAt;
        aggregate.ModifiedBy = "modifier";

        // Assert
        aggregate.CreatedAtUtc.ShouldBe(createdAt);
        aggregate.CreatedBy.ShouldBe("creator");
        aggregate.ModifiedAtUtc.ShouldBe(modifiedAt);
        aggregate.ModifiedBy.ShouldBe("modifier");
    }

    #endregion

    #region FullyAuditedAggregateRoot Tests

    [Fact]
    public void FullyAuditedAggregateRoot_ImplementsISoftDeletable()
    {
        // Arrange & Act
        var aggregate = new TestFullyAuditedAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<ISoftDeletable>();
    }

    [Fact]
    public void FullyAuditedAggregateRoot_Delete_SetsDeletedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 17, 9, 0, 0, TimeSpan.Zero));
        var aggregate = new TestFullyAuditedAggregate(Guid.NewGuid(), fakeTime);
        const string deletedBy = "deleter";

        // Act
        aggregate.Delete(deletedBy);

        // Assert
        aggregate.IsDeleted.ShouldBeTrue();
        aggregate.DeletedAtUtc.ShouldBe(new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc));
        aggregate.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void FullyAuditedAggregateRoot_Delete_WithNullUser_SetsDeletedByToNull()
    {
        // Arrange
        var aggregate = new TestFullyAuditedAggregate(Guid.NewGuid());

        // Act
        aggregate.Delete(null);

        // Assert
        aggregate.IsDeleted.ShouldBeTrue();
        aggregate.DeletedAtUtc.ShouldNotBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void FullyAuditedAggregateRoot_Restore_ClearsDeletedFields()
    {
        // Arrange
        var aggregate = new TestFullyAuditedAggregate(Guid.NewGuid());
        aggregate.Delete("deleter");

        // Act
        aggregate.Restore();

        // Assert
        aggregate.IsDeleted.ShouldBeFalse();
        aggregate.DeletedAtUtc.ShouldBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void FullyAuditedAggregateRoot_SoftDeleteProperties_HavePublicSetters()
    {
        // Arrange
        var aggregate = new TestFullyAuditedAggregate(Guid.NewGuid());
        var deletedAt = new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc);

        // Act - Direct property set (for interceptor compatibility)
        aggregate.IsDeleted = true;
        aggregate.DeletedAtUtc = deletedAt;
        aggregate.DeletedBy = "direct-setter";

        // Assert
        aggregate.IsDeleted.ShouldBeTrue();
        aggregate.DeletedAtUtc.ShouldBe(deletedAt);
        aggregate.DeletedBy.ShouldBe("direct-setter");
    }

    #endregion

    #region AuditableAggregateRoot Tests (Immutable Pattern)

    [Fact]
    public void AuditableAggregateRoot_ImplementsIAuditable()
    {
        // Arrange & Act
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void AuditableAggregateRoot_Constructor_SetsCreatedAtUtc()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        var aggregate = new TestAuditableAggregate(Guid.NewGuid(), fakeTime);

        // Assert
        aggregate.CreatedAtUtc.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AuditableAggregateRoot_SetCreatedBy_SetsCreatedBy()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());

        // Act
        aggregate.DoSetCreatedBy("creator");

        // Assert
        aggregate.CreatedBy.ShouldBe("creator");
    }

    [Fact]
    public void AuditableAggregateRoot_SetModifiedBy_SetsModifiedByAndModifiedAtUtc()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var aggregate = new TestAuditableAggregate(Guid.NewGuid(), fakeTime);

        // Advance time before modification
        fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 16, 14, 0, 0, TimeSpan.Zero));

        // Act
        aggregate.DoSetModifiedBy("modifier");

        // Assert
        aggregate.ModifiedBy.ShouldBe("modifier");
        aggregate.ModifiedAtUtc.ShouldBe(new DateTime(2024, 1, 16, 14, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region SoftDeletableAggregateRoot Tests (Immutable Pattern)

    [Fact]
    public void SoftDeletableAggregateRoot_ImplementsISoftDeletable()
    {
        // Arrange & Act
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<ISoftDeletable>();
        aggregate.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Delete_SetsDeletedFields()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 17, 9, 0, 0, TimeSpan.Zero));
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid(), fakeTime);

        // Act
        aggregate.Delete("deleter");

        // Assert
        aggregate.IsDeleted.ShouldBeTrue();
        aggregate.DeletedAtUtc.ShouldBe(new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc));
        aggregate.DeletedBy.ShouldBe("deleter");
    }

    [Fact]
    public void SoftDeletableAggregateRoot_Restore_ClearsDeletedFields()
    {
        // Arrange
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());
        aggregate.Delete("deleter");

        // Act
        aggregate.Restore();

        // Assert
        aggregate.IsDeleted.ShouldBeFalse();
        aggregate.DeletedAtUtc.ShouldBeNull();
        aggregate.DeletedBy.ShouldBeNull();
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void AuditedAggregateRoot_CanRaiseDomainEvents()
    {
        // Arrange
        var aggregate = new TestAggregateWithEvents(Guid.NewGuid());

        // Act
        aggregate.DoSomething();

        // Assert
        aggregate.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void AuditedAggregateRoot_ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateWithEvents(Guid.NewGuid());
        aggregate.DoSomething();
        aggregate.DoSomething();

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    private sealed class TestAggregateWithEvents : AuditedAggregateRoot<Guid>
    {
        public TestAggregateWithEvents(Guid id) : base(id) { }

        public void DoSomething()
        {
            RaiseDomainEvent(new TestDomainEvent());
        }
    }

    private sealed record TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    }

    #endregion
}
