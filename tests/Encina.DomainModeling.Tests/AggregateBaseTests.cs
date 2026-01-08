using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for AggregateBase and AggregateBase{T} classes.
/// </summary>
public sealed class AggregateBaseTests
{
    #region Test Aggregates

    private sealed class TestAggregate : AggregateBase
    {
        public string Name { get; private set; } = string.Empty;
        public int Value { get; private set; }

        public void Create(Guid id, string name)
        {
            RaiseEvent(new AggregateCreated(id, name));
        }

        public void UpdateValue(int newValue)
        {
            RaiseEvent(new ValueUpdated(newValue));
        }

        protected override void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case AggregateCreated created:
                    Id = created.Id;
                    Name = created.Name;
                    break;
                case ValueUpdated updated:
                    Value = updated.NewValue;
                    break;
            }
        }

        public sealed record AggregateCreated(Guid Id, string Name);
        public sealed record ValueUpdated(int NewValue);
    }

    private sealed class TypedTestAggregate : AggregateBase<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        public void Create(Guid id, string name)
        {
            RaiseEvent(new AggregateCreated(id, name));
        }

        protected override void Apply(object domainEvent)
        {
            switch (domainEvent)
            {
                case AggregateCreated created:
                    Id = created.Id;
                    Name = created.Name;
                    break;
            }
        }

        public sealed record AggregateCreated(Guid Id, string Name);
    }

    private sealed class StringIdAggregate : AggregateBase<string>
    {
        public string Data { get; private set; } = string.Empty;

        public void Create(string id, string data)
        {
            RaiseEvent(new Created(id, data));
        }

        protected override void Apply(object domainEvent)
        {
            if (domainEvent is Created created)
            {
                Id = created.Id;
                Data = created.Data;
            }
        }

        public sealed record Created(string Id, string Data);
    }

    #endregion

    #region AggregateBase Tests

    [Fact]
    public void AggregateBase_NewAggregate_HasNoUncommittedEvents()
    {
        // Arrange & Act
        var aggregate = new TestAggregate();

        // Assert
        aggregate.UncommittedEvents.ShouldBeEmpty();
        aggregate.Version.ShouldBe(0);
    }

    [Fact]
    public void AggregateBase_RaiseEvent_AppliesEventAndAddsToUncommitted()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Test Aggregate");

        // Assert
        aggregate.Id.ShouldBe(id);
        aggregate.Name.ShouldBe("Test Aggregate");
        aggregate.UncommittedEvents.Count.ShouldBe(1);
        aggregate.Version.ShouldBe(1);
    }

    [Fact]
    public void AggregateBase_MultipleEvents_AccumulatesEventsAndIncrementsVersion()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Test");
        aggregate.UpdateValue(100);
        aggregate.UpdateValue(200);

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(3);
        aggregate.Version.ShouldBe(3);
        aggregate.Value.ShouldBe(200);
    }

    [Fact]
    public void AggregateBase_ClearUncommittedEvents_ClearsEventList()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Create(Guid.NewGuid(), "Test");
        aggregate.UpdateValue(100);

        // Act
        aggregate.ClearUncommittedEvents();

        // Assert
        aggregate.UncommittedEvents.ShouldBeEmpty();
        aggregate.Version.ShouldBe(2); // Version is not reset
    }

    [Fact]
    public void AggregateBase_UncommittedEvents_ReturnsReadOnlyList()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.Create(Guid.NewGuid(), "Test");

        // Act
        var events = aggregate.UncommittedEvents;

        // Assert
        events.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<object>>();
    }

    [Fact]
    public void AggregateBase_EventOrder_PreservedInUncommittedEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Test");
        aggregate.UpdateValue(10);
        aggregate.UpdateValue(20);

        // Assert
        aggregate.UncommittedEvents[0].ShouldBeOfType<TestAggregate.AggregateCreated>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<TestAggregate.ValueUpdated>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<TestAggregate.ValueUpdated>();

        ((TestAggregate.ValueUpdated)aggregate.UncommittedEvents[1]).NewValue.ShouldBe(10);
        ((TestAggregate.ValueUpdated)aggregate.UncommittedEvents[2]).NewValue.ShouldBe(20);
    }

    #endregion

    #region AggregateBase<T> Tests

    [Fact]
    public void AggregateBaseT_GuidId_SyncsWithBaseId()
    {
        // Arrange
        var aggregate = new TypedTestAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Typed Aggregate");

        // Assert
        aggregate.Id.ShouldBe(id);
        ((AggregateBase)aggregate).Id.ShouldBe(id); // Base Id should also be set
    }

    [Fact]
    public void AggregateBaseT_StringId_SetsTypedId()
    {
        // Arrange
        var aggregate = new StringIdAggregate();
        var id = "custom-id-123";

        // Act
        aggregate.Create(id, "Some Data");

        // Assert
        aggregate.Id.ShouldBe(id);
        aggregate.Data.ShouldBe("Some Data");
    }

    [Fact]
    public void AggregateBaseT_StringId_BaseIdRemainsDefault()
    {
        // Arrange
        var aggregate = new StringIdAggregate();

        // Act
        aggregate.Create("string-id", "Data");

        // Assert
        // When Id is string, base.Id (Guid) doesn't get synced
        ((AggregateBase)aggregate).Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void AggregateBaseT_AppliesEventsCorrectly()
    {
        // Arrange
        var aggregate = new TypedTestAggregate();
        var id = Guid.NewGuid();

        // Act
        aggregate.Create(id, "Test Name");

        // Assert
        aggregate.Name.ShouldBe("Test Name");
        aggregate.UncommittedEvents.Count.ShouldBe(1);
        aggregate.Version.ShouldBe(1);
    }

    #endregion

    #region Null Event Tests

    [Fact]
    public void AggregateBase_RaiseEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregate = new NullEventAggregate();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => aggregate.RaiseNullEvent());
    }

    private sealed class NullEventAggregate : AggregateBase
    {
        public void RaiseNullEvent()
        {
            RaiseEvent<object>(null!);
        }

        protected override void Apply(object domainEvent)
        {
            // No-op
        }
    }

    #endregion
}
