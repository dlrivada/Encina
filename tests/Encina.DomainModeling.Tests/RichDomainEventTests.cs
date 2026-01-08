using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for RichDomainEvent class.
/// </summary>
public sealed class RichDomainEventTests
{
    #region Test Types

    private sealed record OrderPlacedRich : RichDomainEvent
    {
        public Guid OrderId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public decimal Total { get; init; }
    }

    private sealed record ProductAddedRich : RichDomainEvent
    {
        public string ProductName { get; init; } = string.Empty;
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void RichDomainEvent_DefaultValues_AreCorrect()
    {
        // Act
        var @event = new OrderPlacedRich
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.EventId.ShouldNotBe(Guid.Empty);
        @event.OccurredAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        @event.CorrelationId.ShouldBeNull();
        @event.CausationId.ShouldBeNull();
        @event.AggregateId.ShouldBe(Guid.Empty);
        @event.AggregateVersion.ShouldBe(0);
        @event.EventVersion.ShouldBe(1);
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void RichDomainEvent_CorrelationId_CanBeSet()
    {
        // Act
        var @event = new OrderPlacedRich
        {
            CorrelationId = "corr-123",
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.CorrelationId.ShouldBe("corr-123");
    }

    [Fact]
    public void RichDomainEvent_CausationId_CanBeSet()
    {
        // Act
        var @event = new OrderPlacedRich
        {
            CausationId = "cause-456",
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.CausationId.ShouldBe("cause-456");
    }

    [Fact]
    public void RichDomainEvent_AggregateId_CanBeSet()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var @event = new OrderPlacedRich
        {
            AggregateId = aggregateId,
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.AggregateId.ShouldBe(aggregateId);
    }

    [Fact]
    public void RichDomainEvent_AggregateVersion_CanBeSet()
    {
        // Act
        var @event = new OrderPlacedRich
        {
            AggregateVersion = 5,
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.AggregateVersion.ShouldBe(5);
    }

    [Fact]
    public void RichDomainEvent_EventVersion_CanBeSet()
    {
        // Act
        var @event = new OrderPlacedRich
        {
            EventVersion = 2,
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        @event.EventVersion.ShouldBe(2);
    }

    #endregion

    #region Complete Event Construction Tests

    [Fact]
    public void RichDomainEvent_WithAllMetadata_InitializesCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var @event = new OrderPlacedRich
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            CorrelationId = "corr-full",
            CausationId = "cause-full",
            AggregateId = aggregateId,
            AggregateVersion = 10,
            EventVersion = 3,
            OrderId = Guid.NewGuid(),
            CustomerName = "Full Test",
            Total = 999.99m
        };

        // Assert
        @event.EventId.ShouldBe(eventId);
        @event.OccurredAtUtc.ShouldBe(occurredAt);
        @event.CorrelationId.ShouldBe("corr-full");
        @event.CausationId.ShouldBe("cause-full");
        @event.AggregateId.ShouldBe(aggregateId);
        @event.AggregateVersion.ShouldBe(10);
        @event.EventVersion.ShouldBe(3);
        @event.CustomerName.ShouldBe("Full Test");
        @event.Total.ShouldBe(999.99m);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void RichDomainEvent_SameValues_AreEqual()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var event1 = new OrderPlacedRich
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            OrderId = orderId,
            CustomerName = "John",
            Total = 100m
        };

        var event2 = new OrderPlacedRich
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            OrderId = orderId,
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        event1.ShouldBe(event2);
        event1.GetHashCode().ShouldBe(event2.GetHashCode());
    }

    [Fact]
    public void RichDomainEvent_DifferentEventId_AreNotEqual()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var event1 = new OrderPlacedRich
        {
            EventId = Guid.NewGuid(),
            OrderId = orderId,
            CustomerName = "John",
            Total = 100m
        };

        var event2 = new OrderPlacedRich
        {
            EventId = Guid.NewGuid(),
            OrderId = orderId,
            CustomerName = "John",
            Total = 100m
        };

        // Assert
        event1.ShouldNotBe(event2);
    }

    #endregion

    #region With Expression Tests (Record Mutation)

    [Fact]
    public void RichDomainEvent_With_CreatesNewEventWithModifiedValue()
    {
        // Arrange
        var original = new OrderPlacedRich
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Original",
            Total = 100m,
            AggregateVersion = 1
        };

        // Act
        var modified = original with { AggregateVersion = 2 };

        // Assert
        modified.AggregateVersion.ShouldBe(2);
        modified.CustomerName.ShouldBe("Original"); // unchanged
        modified.EventId.ShouldBe(original.EventId); // unchanged
        original.AggregateVersion.ShouldBe(1); // original unchanged
    }

    [Fact]
    public void RichDomainEvent_With_CorrelationId_CreatesNewEvent()
    {
        // Arrange
        var original = new OrderPlacedRich
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Test",
            Total = 100m
        };

        // Act
        var modified = original with { CorrelationId = "new-correlation" };

        // Assert
        modified.CorrelationId.ShouldBe("new-correlation");
        original.CorrelationId.ShouldBeNull();
    }

    #endregion

    #region Different Event Types Tests

    [Fact]
    public void RichDomainEvent_DifferentTypes_AreNotEqual()
    {
        // Arrange
        var event1 = new OrderPlacedRich
        {
            EventId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerName = "John",
            Total = 100m
        };

        var event2 = new ProductAddedRich
        {
            EventId = event1.EventId,
            ProductName = "Test Product"
        };

        // Assert
        // Different types should not be equal even with same EventId
        object obj1 = event1;
        object obj2 = event2;
        obj1.Equals(obj2).ShouldBeFalse();
    }

    #endregion

    #region IDomainEvent Interface Tests

    [Fact]
    public void RichDomainEvent_ImplementsIDomainEvent()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var @event = new OrderPlacedRich
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Test",
            Total = 100m
        };

        // Act & Assert
        var after = DateTime.UtcNow;
        IDomainEvent domainEvent = @event;
        domainEvent.EventId.ShouldNotBe(Guid.Empty);
        // Timestamp should fall within the window captured around construction
        domainEvent.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        domainEvent.OccurredAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion
}
