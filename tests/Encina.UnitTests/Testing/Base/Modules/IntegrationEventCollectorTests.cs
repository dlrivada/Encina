using Encina.Testing;
using Encina.Testing.Modules;
using Shouldly;

namespace Encina.UnitTests.Testing.Base.Modules;

public sealed class IntegrationEventCollectorTests
{
    #region Test Infrastructure

    public sealed record OrderPlacedEvent(Guid OrderId, decimal Amount) : INotification;
    public sealed record OrderCancelledEvent(Guid OrderId, string Reason) : INotification;
    public sealed record PaymentProcessedEvent(Guid PaymentId) : INotification;

    #endregion

    #region Add Tests

    [Fact]
    public void Add_NullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => collector.Add(null!));
    }

    [Fact]
    public void Add_ValidNotification_IncrementsCount()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Assert
        collector.Count.ShouldBe(1);
    }

    [Fact]
    public void Add_MultipleNotifications_TracksAll()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderCancelledEvent(Guid.NewGuid(), "Out of stock"));
        collector.Add(new PaymentProcessedEvent(Guid.NewGuid()));

        // Assert
        collector.Count.ShouldBe(3);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_EmptiesCollection()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderCancelledEvent(Guid.NewGuid(), "Reason"));

        // Act
        collector.Clear();

        // Assert
        collector.Count.ShouldBe(0);
        collector.Events.ShouldBeEmpty();
    }

    #endregion

    #region Query Methods Tests

    [Fact]
    public void GetEvents_ReturnsEventsOfType()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        collector.Add(new OrderPlacedEvent(orderId1, 100m));
        collector.Add(new OrderCancelledEvent(Guid.NewGuid(), "Reason"));
        collector.Add(new OrderPlacedEvent(orderId2, 200m));

        // Act
        var orderEvents = collector.GetEvents<OrderPlacedEvent>();

        // Assert
        orderEvents.Count.ShouldBe(2);
        orderEvents.ShouldContain(e => e.OrderId == orderId1);
        orderEvents.ShouldContain(e => e.OrderId == orderId2);
    }

    [Fact]
    public void GetEvents_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act
        var payments = collector.GetEvents<PaymentProcessedEvent>();

        // Assert
        payments.ShouldBeEmpty();
    }

    [Fact]
    public void GetFirst_ReturnsMatchingEvent()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();

        collector.Add(new OrderPlacedEvent(firstOrderId, 100m));
        collector.Add(new OrderPlacedEvent(secondOrderId, 200m));

        // Act
        var first = collector.GetFirst<OrderPlacedEvent>();

        // Assert - ConcurrentBag doesn't guarantee order, so check it's one of the added events
        new[] { firstOrderId, secondOrderId }.ShouldContain(first.OrderId);
    }

    [Fact]
    public void GetFirst_NoMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => collector.GetFirst<OrderPlacedEvent>());
    }

    [Fact]
    public void GetFirstOrDefault_NoMatch_ReturnsNull()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act
        var result = collector.GetFirstOrDefault<OrderPlacedEvent>();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetSingle_SingleMatch_ReturnsEvent()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(orderId, 100m));

        // Act
        var result = collector.GetSingle<OrderPlacedEvent>();

        // Assert
        result.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void GetSingle_NoMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => collector.GetSingle<OrderPlacedEvent>());
    }

    [Fact]
    public void GetSingle_MultipleMatches_ThrowsInvalidOperationException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 200m));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => collector.GetSingle<OrderPlacedEvent>());
    }

    [Fact]
    public void Contains_EventExists_ReturnsTrue()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        collector.Contains<OrderPlacedEvent>().ShouldBeTrue();
    }

    [Fact]
    public void Contains_EventNotExists_ReturnsFalse()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        collector.Contains<OrderPlacedEvent>().ShouldBeFalse();
    }

    [Fact]
    public void Contains_WithPredicate_MatchesCorrectEvent()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var targetOrderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderPlacedEvent(targetOrderId, 200m));

        // Act & Assert
        collector.Contains<OrderPlacedEvent>(e => e.OrderId == targetOrderId).ShouldBeTrue();
        collector.Contains<OrderPlacedEvent>(e => e.Amount == 500m).ShouldBeFalse();
    }

    [Fact]
    public void Contains_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => collector.Contains<OrderPlacedEvent>(null!));
    }

    #endregion

    #region Assertion Tests

    [Fact]
    public void ShouldContain_EventExists_ReturnsCollector()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act
        var result = collector.ShouldContain<OrderPlacedEvent>();

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldContain_EventNotExists_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldContain<OrderPlacedEvent>());
    }

    [Fact]
    public void ShouldContain_WithPredicate_MatchesEvent()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(orderId, 100m));

        // Act
        var result = collector.ShouldContain<OrderPlacedEvent>(e => e.OrderId == orderId);

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldContain_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => collector.ShouldContain<OrderPlacedEvent>(null!));
    }

    [Fact]
    public void ShouldContain_WithPredicate_NoMatch_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            collector.ShouldContain<OrderPlacedEvent>(e => e.Amount == 999m));
    }

    [Fact]
    public void ShouldContainAnd_ReturnsAndConstraint()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(orderId, 100m));

        // Act
        var result = collector.ShouldContainAnd<OrderPlacedEvent>();

        // Assert
        result.Value.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void ShouldContainSingle_SingleEvent_Passes()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act
        var result = collector.ShouldContainSingle<OrderPlacedEvent>();

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldContainSingle_MultipleEvents_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 200m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldContainSingle<OrderPlacedEvent>());
    }

    [Fact]
    public void ShouldContainSingleAnd_ReturnsAndConstraint()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(orderId, 100m));

        // Act
        var result = collector.ShouldContainSingleAnd<OrderPlacedEvent>();

        // Assert
        result.Value.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void ShouldNotContain_EventNotExists_Passes()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act
        var result = collector.ShouldNotContain<OrderPlacedEvent>();

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldNotContain_EventExists_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldNotContain<OrderPlacedEvent>());
    }

    [Fact]
    public void ShouldBeEmpty_EmptyCollection_Passes()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act
        var result = collector.ShouldBeEmpty();

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldBeEmpty_NotEmpty_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldBeEmpty());
    }

    [Fact]
    public void ShouldHaveCount_CorrectCount_Passes()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderCancelledEvent(Guid.NewGuid(), "Reason"));

        // Act
        var result = collector.ShouldHaveCount(2);

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldHaveCount_WrongCount_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldHaveCount(5));
    }

    [Fact]
    public void ShouldHaveCount_ByType_CorrectCount_Passes()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 200m));
        collector.Add(new OrderCancelledEvent(Guid.NewGuid(), "Reason"));

        // Act
        var result = collector.ShouldHaveCount<OrderPlacedEvent>(2);

        // Assert
        result.ShouldBe(collector);
    }

    [Fact]
    public void ShouldHaveCount_ByType_WrongCount_Throws()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        collector.Add(new OrderPlacedEvent(Guid.NewGuid(), 100m));

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => collector.ShouldHaveCount<OrderPlacedEvent>(3));
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Assertions_CanBeChained()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var orderId = Guid.NewGuid();
        collector.Add(new OrderPlacedEvent(orderId, 100m));
        collector.Add(new PaymentProcessedEvent(Guid.NewGuid()));

        // Act & Assert - all assertions should pass and chain correctly
        collector
            .ShouldContain<OrderPlacedEvent>()
            .ShouldContain<PaymentProcessedEvent>()
            .ShouldNotContain<OrderCancelledEvent>()
            .ShouldHaveCount(2)
            .ShouldHaveCount<OrderPlacedEvent>(1);
    }

    #endregion
}
