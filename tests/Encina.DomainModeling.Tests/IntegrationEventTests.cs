using Encina.DomainModeling;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for Integration Event mapping and publishing classes.
/// </summary>
public sealed class IntegrationEventTests
{
    #region Test Types

    public sealed record TestDomainEventForIntegration(Guid Id, string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed record TestIntegrationEvent(Guid Id, string ProcessedData) : IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
        public int EventVersion => 1;
        public string? CorrelationId { get; init; }
    }

    // Intermediate event that is both domain and integration
    public sealed record IntermediateEvent(Guid Id, string Data) : IDomainEvent, IIntegrationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
        public int EventVersion => 1;
        public string? CorrelationId { get; init; }
    }

    public sealed class TestMapper : IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>
    {
        public TestIntegrationEvent Map(TestDomainEventForIntegration domainEvent)
        {
            return new TestIntegrationEvent(domainEvent.Id, $"Processed: {domainEvent.Data}");
        }
    }

    public sealed class ThrowingMapper : IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>
    {
        public TestIntegrationEvent Map(TestDomainEventForIntegration domainEvent)
        {
            throw new InvalidOperationException("Mapping failed");
        }
    }

    public sealed class AsyncTestMapper : IAsyncDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>
    {
        public Task<TestIntegrationEvent> MapAsync(TestDomainEventForIntegration domainEvent, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TestIntegrationEvent(domainEvent.Id, $"Async: {domainEvent.Data}"));
        }
    }

    public sealed class FirstMapper : IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, IntermediateEvent>
    {
        public IntermediateEvent Map(TestDomainEventForIntegration domainEvent)
        {
            return new IntermediateEvent(domainEvent.Id, $"First: {domainEvent.Data}");
        }
    }

    public sealed class SecondMapper : IDomainEventToIntegrationEventMapper<IntermediateEvent, TestIntegrationEvent>
    {
        public TestIntegrationEvent Map(IntermediateEvent domainEvent)
        {
            return new TestIntegrationEvent(domainEvent.Id, $"Second({domainEvent.Data})");
        }
    }

    #endregion

    #region IntegrationEventMappingError Tests

    [Fact]
    public void IntegrationEventMappingError_MissingField_CreatesCorrectError()
    {
        // Act
        var error = IntegrationEventMappingError.MissingField<TestDomainEventForIntegration, TestIntegrationEvent>("CustomerId");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_MISSING_FIELD");
        error.DomainEventType.ShouldBe(typeof(TestDomainEventForIntegration));
        error.IntegrationEventType.ShouldBe(typeof(TestIntegrationEvent));
        error.Message.ShouldContain("CustomerId");
    }

    [Fact]
    public void IntegrationEventMappingError_ValidationFailed_CreatesCorrectError()
    {
        // Act
        var error = IntegrationEventMappingError.ValidationFailed<TestDomainEventForIntegration, TestIntegrationEvent>("Invalid amount");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_VALIDATION_FAILED");
        error.Message.ShouldContain("Invalid amount");
    }

    [Fact]
    public void IntegrationEventMappingError_LookupFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new InvalidOperationException("Database error");

        // Act
        var error = IntegrationEventMappingError.LookupFailed<TestDomainEventForIntegration, TestIntegrationEvent>("CustomerDetails", exception);

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_LOOKUP_FAILED");
        error.Message.ShouldContain("CustomerDetails");
        error.InnerException.ShouldBe(exception);
    }

    [Fact]
    public void IntegrationEventMappingError_LookupFailed_WithoutException_CreatesCorrectError()
    {
        // Act
        var error = IntegrationEventMappingError.LookupFailed<TestDomainEventForIntegration, TestIntegrationEvent>("CustomerDetails");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_LOOKUP_FAILED");
        error.InnerException.ShouldBeNull();
    }

    #endregion

    #region IntegrationEventMappingExtensions Tests

    [Fact]
    public void MapTo_MapsCorrectly()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");
        var mapper = new TestMapper();

        // Act
        var result = domainEvent.MapTo(mapper);

        // Assert
        result.ProcessedData.ShouldBe("Processed: TestData");
    }

    [Fact]
    public void MapTo_NullDomainEvent_ThrowsArgumentNullException()
    {
        // Arrange
        TestDomainEventForIntegration? domainEvent = null;
        var mapper = new TestMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => domainEvent!.MapTo(mapper));
    }

    [Fact]
    public void MapTo_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            domainEvent.MapTo((IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>)null!));
    }

    [Fact]
    public async Task MapToAsync_MapsCorrectly()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "AsyncData");
        var mapper = new AsyncTestMapper();

        // Act
        var result = await domainEvent.MapToAsync(mapper);

        // Assert
        result.ProcessedData.ShouldBe("Async: AsyncData");
    }

    [Fact]
    public async Task MapToAsync_NullDomainEvent_ThrowsArgumentNullException()
    {
        // Arrange
        TestDomainEventForIntegration? domainEvent = null;
        var mapper = new AsyncTestMapper();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await domainEvent!.MapToAsync(mapper));
    }

    [Fact]
    public async Task MapToAsync_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await domainEvent.MapToAsync((IAsyncDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>)null!));
    }

    [Fact]
    public void MapAll_MapsAllEvents()
    {
        // Arrange
        var events = new[]
        {
            new TestDomainEventForIntegration(Guid.NewGuid(), "Event1"),
            new TestDomainEventForIntegration(Guid.NewGuid(), "Event2"),
            new TestDomainEventForIntegration(Guid.NewGuid(), "Event3")
        };
        var mapper = new TestMapper();

        // Act
        var results = events.MapAll(mapper).ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].ProcessedData.ShouldBe("Processed: Event1");
        results[1].ProcessedData.ShouldBe("Processed: Event2");
        results[2].ProcessedData.ShouldBe("Processed: Event3");
    }

    [Fact]
    public void MapAll_NullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<TestDomainEventForIntegration>? events = null;
        var mapper = new TestMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => events!.MapAll(mapper).ToList());
    }

    [Fact]
    public void MapAll_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var events = new[] { new TestDomainEventForIntegration(Guid.NewGuid(), "Event1") };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            events.MapAll((IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>)null!).ToList());
    }

    [Fact]
    public async Task MapAllAsync_MapsAllEventsAsync()
    {
        // Arrange
        var events = new[]
        {
            new TestDomainEventForIntegration(Guid.NewGuid(), "Event1"),
            new TestDomainEventForIntegration(Guid.NewGuid(), "Event2")
        };
        var mapper = new AsyncTestMapper();

        // Act
        var results = await events.MapAllAsync(mapper);

        // Assert
        results.Count.ShouldBe(2);
        results[0].ProcessedData.ShouldBe("Async: Event1");
    }

    [Fact]
    public async Task MapAllAsync_NullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<TestDomainEventForIntegration>? events = null;
        var mapper = new AsyncTestMapper();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await events!.MapAllAsync(mapper));
    }

    [Fact]
    public async Task MapAllAsync_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var events = new[] { new TestDomainEventForIntegration(Guid.NewGuid(), "Event1") };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await events.MapAllAsync((IAsyncDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>)null!));
    }

    [Fact]
    public void TryMapTo_Success_ReturnsSome()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");
        var mapper = new TestMapper();

        // Act
        var result = domainEvent.TryMapTo(mapper);

        // Assert
        result.IsSome.ShouldBeTrue();
        result.Match(
            Some: e => e.ProcessedData.ShouldBe("Processed: TestData"),
            None: () => throw new InvalidOperationException("Expected Some"));
    }

    [Fact]
    public void TryMapTo_MapperThrows_ReturnsNone()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");
        var mapper = new ThrowingMapper();

        // Act
        var result = domainEvent.TryMapTo(mapper);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void TryMapTo_NullMapper_ReturnsNone()
    {
        // Arrange
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "TestData");

        // Act
        var result = domainEvent.TryMapTo((IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, TestIntegrationEvent>)null!);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Compose_ComposesMappers()
    {
        // Arrange
        var firstMapper = new FirstMapper();
        var secondMapper = new SecondMapper();
        var domainEvent = new TestDomainEventForIntegration(Guid.NewGuid(), "Data");

        // Act
        var compositeMapper = firstMapper.Compose(secondMapper);
        var result = compositeMapper.Map(domainEvent);

        // Assert
        result.ProcessedData.ShouldBe("Second(First: Data)");
    }

    [Fact]
    public void Compose_NullFirstMapper_ThrowsArgumentNullException()
    {
        // Arrange
        IDomainEventToIntegrationEventMapper<TestDomainEventForIntegration, IntermediateEvent>? firstMapper = null;
        var secondMapper = new SecondMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => firstMapper!.Compose(secondMapper));
    }

    [Fact]
    public void Compose_NullSecondMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var firstMapper = new FirstMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            firstMapper.Compose((IDomainEventToIntegrationEventMapper<IntermediateEvent, TestIntegrationEvent>)null!));
    }

    #endregion

    #region IntegrationEventPublishError Tests

    [Fact]
    public void IntegrationEventPublishError_SerializationFailed_CreatesCorrectError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var exception = new InvalidOperationException("Serialization error");

        // Act
        var error = IntegrationEventPublishError.SerializationFailed<TestIntegrationEvent>(eventId, exception);

        // Assert
        error.ErrorCode.ShouldBe("PUBLISH_SERIALIZATION_FAILED");
        error.EventType.ShouldBe(typeof(TestIntegrationEvent));
        error.EventId.ShouldBe(eventId);
        error.InnerException.ShouldBe(exception);
    }

    [Fact]
    public void IntegrationEventPublishError_OutboxStoreFailed_CreatesCorrectError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var exception = new InvalidOperationException("Outbox error");

        // Act
        var error = IntegrationEventPublishError.OutboxStoreFailed<TestIntegrationEvent>(eventId, exception);

        // Assert
        error.ErrorCode.ShouldBe("PUBLISH_OUTBOX_FAILED");
        error.EventId.ShouldBe(eventId);
    }

    [Fact]
    public void IntegrationEventPublishError_BrokerFailed_CreatesCorrectError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var exception = new TimeoutException("Broker timeout");

        // Act
        var error = IntegrationEventPublishError.BrokerFailed<TestIntegrationEvent>(eventId, "RabbitMQ", exception);

        // Assert
        error.ErrorCode.ShouldBe("PUBLISH_BROKER_FAILED");
        error.Message.ShouldContain("RabbitMQ");
        error.InnerException.ShouldBe(exception);
    }

    #endregion
}
