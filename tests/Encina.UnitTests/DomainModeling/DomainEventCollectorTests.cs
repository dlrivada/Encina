using System.Diagnostics.CodeAnalysis;
using Encina.DomainModeling;
using LanguageExt;
using NSubstitute;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for IDomainEventCollector and DomainEventCollector.
/// </summary>
public class DomainEventCollectorTests
{
    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregate(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed record NonNotificationEvent(string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    #region TrackAggregate Tests

    [Fact]
    public void TrackAggregate_SingleAggregate_ShouldTrack()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        collector.TrackAggregate(aggregate);

        // Assert
        collector.TrackedAggregateCount.ShouldBe(1);
    }

    [Fact]
    public void TrackAggregate_MultipleAggregates_ShouldTrackAll()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate1 = new TestAggregate(Guid.NewGuid());
        var aggregate2 = new TestAggregate(Guid.NewGuid());
        var aggregate3 = new TestAggregate(Guid.NewGuid());

        // Act
        collector.TrackAggregate(aggregate1);
        collector.TrackAggregate(aggregate2);
        collector.TrackAggregate(aggregate3);

        // Assert
        collector.TrackedAggregateCount.ShouldBe(3);
    }

    [Fact]
    public void TrackAggregate_SameAggregateTwice_ShouldOnlyTrackOnce()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Act
        collector.TrackAggregate(aggregate);
        collector.TrackAggregate(aggregate);

        // Assert
        collector.TrackedAggregateCount.ShouldBe(1);
    }

    [Fact]
    public void TrackAggregate_NullAggregate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collector = new DomainEventCollector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => collector.TrackAggregate(null!));
    }

    #endregion

    #region CollectEvents Tests

    [Fact]
    public void CollectEvents_NoAggregates_ShouldReturnEmpty()
    {
        // Arrange
        var collector = new DomainEventCollector();

        // Act
        var events = collector.CollectEvents();

        // Assert
        events.ShouldBeEmpty();
    }

    [Fact]
    public void CollectEvents_AggregateWithNoEvents_ShouldReturnEmpty()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        collector.TrackAggregate(aggregate);

        // Act
        var events = collector.CollectEvents();

        // Assert
        events.ShouldBeEmpty();
    }

    [Fact]
    public void CollectEvents_SingleAggregateWithEvents_ShouldReturnAllEvents()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestDomainEvent(aggregate.Id);
        var event2 = new TestDomainEvent(aggregate.Id);
        aggregate.RaiseEvent(event1);
        aggregate.RaiseEvent(event2);
        collector.TrackAggregate(aggregate);

        // Act
        var events = collector.CollectEvents();

        // Assert
        events.Count.ShouldBe(2);
        events.ShouldContain(event1);
        events.ShouldContain(event2);
    }

    [Fact]
    public void CollectEvents_MultipleAggregatesWithEvents_ShouldReturnAllEvents()
    {
        // Arrange
        var collector = new DomainEventCollector();

        var aggregate1 = new TestAggregate(Guid.NewGuid());
        var aggregate2 = new TestAggregate(Guid.NewGuid());

        var event1 = new TestDomainEvent(aggregate1.Id);
        var event2 = new TestDomainEvent(aggregate1.Id);
        var event3 = new TestDomainEvent(aggregate2.Id);

        aggregate1.RaiseEvent(event1);
        aggregate1.RaiseEvent(event2);
        aggregate2.RaiseEvent(event3);

        collector.TrackAggregate(aggregate1);
        collector.TrackAggregate(aggregate2);

        // Act
        var events = collector.CollectEvents();

        // Assert
        events.Count.ShouldBe(3);
        events.ShouldContain(event1);
        events.ShouldContain(event2);
        events.ShouldContain(event3);
    }

    [Fact]
    public void CollectEvents_ShouldReturnReadOnlyList()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        var events = collector.CollectEvents();

        // Assert
        events.ShouldBeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }

    #endregion

    #region TotalEventCount Tests

    [Fact]
    public void TotalEventCount_NoEvents_ShouldBeZero()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        collector.TrackAggregate(aggregate);

        // Act & Assert
        collector.TotalEventCount.ShouldBe(0);
    }

    [Fact]
    public void TotalEventCount_WithEvents_ShouldReflectTotal()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate1 = new TestAggregate(Guid.NewGuid());
        var aggregate2 = new TestAggregate(Guid.NewGuid());

        aggregate1.RaiseEvent(new TestDomainEvent(aggregate1.Id));
        aggregate1.RaiseEvent(new TestDomainEvent(aggregate1.Id));
        aggregate2.RaiseEvent(new TestDomainEvent(aggregate2.Id));

        collector.TrackAggregate(aggregate1);
        collector.TrackAggregate(aggregate2);

        // Act & Assert
        collector.TotalEventCount.ShouldBe(3);
    }

    #endregion

    #region ClearCollectedEvents Tests

    [Fact]
    public void ClearCollectedEvents_ShouldClearEventsFromAggregates()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestDomainEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        collector.ClearCollectedEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
        collector.TotalEventCount.ShouldBe(0);
    }

    [Fact]
    public void ClearCollectedEvents_ShouldClearTrackedAggregates()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var aggregate = new TestAggregate(Guid.NewGuid());
        collector.TrackAggregate(aggregate);

        // Act
        collector.ClearCollectedEvents();

        // Assert
        collector.TrackedAggregateCount.ShouldBe(0);
    }

    [Fact]
    public void ClearCollectedEvents_OnEmptyCollector_ShouldNotThrow()
    {
        // Arrange
        var collector = new DomainEventCollector();

        // Act & Assert
        Should.NotThrow(() => collector.ClearCollectedEvents());
    }

    [Fact]
    public void ClearCollectedEvents_ThenTrackNewAggregate_ShouldWork()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var oldAggregate = new TestAggregate(Guid.NewGuid());
        oldAggregate.RaiseEvent(new TestDomainEvent(oldAggregate.Id));
        collector.TrackAggregate(oldAggregate);
        collector.ClearCollectedEvents();

        var newAggregate = new TestAggregate(Guid.NewGuid());
        var newEvent = new TestDomainEvent(newAggregate.Id);
        newAggregate.RaiseEvent(newEvent);

        // Act
        collector.TrackAggregate(newAggregate);

        // Assert
        collector.TrackedAggregateCount.ShouldBe(1);
        collector.TotalEventCount.ShouldBe(1);
        collector.CollectEvents().ShouldContain(newEvent);
    }

    #endregion
}

/// <summary>
/// Tests for DomainEventDispatchHelper.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public class DomainEventDispatchHelperTests
{
    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate(Guid id) : base(id) { }
        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestNotificationEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed record NonNotificationEvent(string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    #region DispatchCollectedEventsAsync Tests

    [Fact]
    public async Task DispatchCollectedEventsAsync_NoEvents_ShouldReturnSuccess()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        // Act
        var result = await helper.DispatchCollectedEventsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_WithNotificationEvents_ShouldPublishAll()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        var event1 = new TestNotificationEvent(aggregate.Id);
        var event2 = new TestNotificationEvent(aggregate.Id);
        aggregate.RaiseEvent(event1);
        aggregate.RaiseEvent(event2);
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_WithNonNotificationEvents_ShouldSkipThem()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new NonNotificationEvent("data1"));
        aggregate.RaiseEvent(new NonNotificationEvent("data2"));
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        await encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_MixedEvents_ShouldOnlyPublishNotifications()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Should be published
        aggregate.RaiseEvent(new NonNotificationEvent("skip me"));      // Should be skipped
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Should be published
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        await encina.Received(2).Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_OnSuccess_ShouldClearEvents()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        await helper.DispatchCollectedEventsAsync();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
        collector.TrackedAggregateCount.ShouldBe(0);
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_OnFailure_ShouldReturnError()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error message");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(error));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchCollectedEventsAsync_OnFailure_ShouldNotClearEvents()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error message");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(error));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        await helper.DispatchCollectedEventsAsync();

        // Assert - Events should NOT be cleared on failure (for retry)
        aggregate.DomainEvents.Count.ShouldBe(1);
    }

    #endregion

    #region DispatchCollectedEventsWithContinuationAsync Tests

    [Fact]
    public async Task DispatchWithContinuation_NoEvents_ShouldReturnZeroCounts()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        // Act
        var result = await helper.DispatchCollectedEventsWithContinuationAsync();

        // Assert
        result.SuccessCount.ShouldBe(0);
        result.SkippedCount.ShouldBe(0);
        result.Errors.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchWithContinuation_AllSuccess_ShouldReturnCorrectCounts()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsWithContinuationAsync();

        // Assert
        result.SuccessCount.ShouldBe(3);
        result.SkippedCount.ShouldBe(0);
        result.Errors.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
        result.TotalProcessed.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchWithContinuation_SomeFailures_ShouldContinueAndReportErrors()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var encina = Substitute.For<IEncina>();

        // First call succeeds, second fails, third succeeds
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<Either<EncinaError, Unit>>(Unit.Default),
                new ValueTask<Either<EncinaError, Unit>>(error),
                new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsWithContinuationAsync();

        // Assert
        result.SuccessCount.ShouldBe(2);
        result.Errors.Count.ShouldBe(1);
        result.IsSuccess.ShouldBeFalse();
        result.TotalProcessed.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchWithContinuation_NonNotificationSkipped_ShouldCountAsSkipped()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Published
        aggregate.RaiseEvent(new NonNotificationEvent("skip"));         // Skipped
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id)); // Published
        collector.TrackAggregate(aggregate);

        // Act
        var result = await helper.DispatchCollectedEventsWithContinuationAsync();

        // Assert
        result.SuccessCount.ShouldBe(2);
        result.SkippedCount.ShouldBe(1);
        result.Errors.ShouldBeEmpty();
        result.IsSuccess.ShouldBeTrue();
        // TotalAttempted = SuccessCount + Errors.Count (skipped events don't count as attempts)
        result.TotalAttempted.ShouldBe(2);
        // TotalProcessed = SuccessCount + SkippedCount + Errors.Count
        result.TotalProcessed.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchWithContinuation_AlwaysClearsEvents()
    {
        // Arrange
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(error));

        var collector = new DomainEventCollector();
        var helper = new DomainEventDispatchHelper(encina, collector);

        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestNotificationEvent(aggregate.Id));
        collector.TrackAggregate(aggregate);

        // Act
        await helper.DispatchCollectedEventsWithContinuationAsync();

        // Assert - Events should be cleared even on failure in continuation mode
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullEncina_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collector = new DomainEventCollector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DomainEventDispatchHelper(null!, collector));
    }

    [Fact]
    public void Constructor_NullCollector_ShouldThrowArgumentNullException()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DomainEventDispatchHelper(encina, null!));
    }

    #endregion
}

/// <summary>
/// Tests for DomainEventDispatchResult record.
/// </summary>
public class DomainEventDispatchResultTests
{
    [Fact]
    public void IsSuccess_WithNoErrors_ShouldBeTrue()
    {
        // Arrange
        var result = new DomainEventDispatchResult(5, 2, []);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void IsSuccess_WithErrors_ShouldBeFalse()
    {
        // Arrange
        var error = new DomainEventDispatchError(
            Substitute.For<IDomainEvent>(),
            EncinaErrors.Create("ERROR", "message"));
        var result = new DomainEventDispatchResult(4, 1, [error]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void TotalAttempted_ShouldBeSuccessPlusErrors()
    {
        // Arrange - TotalAttempted = SuccessCount + Errors.Count (skipped don't count)
        var error = new DomainEventDispatchError(
            Substitute.For<IDomainEvent>(),
            EncinaErrors.Create("ERROR", "message"));
        var result = new DomainEventDispatchResult(5, 3, [error, error]);

        // Assert
        result.TotalAttempted.ShouldBe(7); // 5 success + 2 errors = 7 attempted
    }

    [Fact]
    public void TotalProcessed_ShouldBeSuccessPlusSkippedPlusErrors()
    {
        // Arrange
        var error = new DomainEventDispatchError(
            Substitute.For<IDomainEvent>(),
            EncinaErrors.Create("ERROR", "message"));
        var result = new DomainEventDispatchResult(5, 3, [error, error]);

        // Assert
        result.TotalProcessed.ShouldBe(10); // 5 + 3 + 2
    }
}

/// <summary>
/// Tests for DomainEventDispatchErrors factory.
/// </summary>
public class DomainEventDispatchErrorsTests
{
    [Fact]
    public void CreateAggregateError_EmptyList_ShouldReturnNoErrorsMessage()
    {
        // Arrange
        var errors = new List<EncinaError>();

        // Act
        var result = DomainEventDispatchErrors.CreateAggregateError(errors);

        // Assert
        result.Message.ShouldContain("No errors");
    }

    [Fact]
    public void CreateAggregateError_SingleError_ShouldReturnThatError()
    {
        // Arrange
        var originalError = EncinaErrors.Create("ORIGINAL", "Original message");
        var errors = new List<EncinaError> { originalError };

        // Act
        var result = DomainEventDispatchErrors.CreateAggregateError(errors);

        // Assert
        result.ShouldBe(originalError);
    }

    [Fact]
    public void CreateAggregateError_MultipleErrors_ShouldAggregateWithCount()
    {
        // Arrange
        var error1 = EncinaErrors.Create("ERROR1", "First error");
        var error2 = EncinaErrors.Create("ERROR2", "Second error");
        var error3 = EncinaErrors.Create("ERROR3", "Third error");
        var errors = new List<EncinaError> { error1, error2, error3 };

        // Act
        var result = DomainEventDispatchErrors.CreateAggregateError(errors);

        // Assert
        result.Message.ShouldContain("3");
        result.Message.ShouldContain("domain event");

        var code = result.GetCode();
        code.IsSome.ShouldBeTrue();
        code.IfSome(c => c.ShouldBe(DomainEventDispatchErrors.AggregateDispatchFailedCode));
    }

    [Fact]
    public void CreateAggregateError_NullList_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DomainEventDispatchErrors.CreateAggregateError(null!));
    }
}
