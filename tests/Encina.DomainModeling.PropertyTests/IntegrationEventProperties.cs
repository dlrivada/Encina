using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for integration event patterns.
/// </summary>
public class IntegrationEventProperties
{
    private sealed record TestDomainEvent(string Data) : DomainEvent;
    private sealed record TestIntegrationEvent(string Data) : IntegrationEvent;

    // === IntegrationEventMappingError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool MappingError_MissingField_HasCorrectCode(NonEmptyString fieldName)
    {
        var error = IntegrationEventMappingError.MissingField<TestDomainEvent, TestIntegrationEvent>(
            fieldName.Get);

        return error.ErrorCode == "MAPPING_MISSING_FIELD"
            && error.DomainEventType == typeof(TestDomainEvent)
            && error.IntegrationEventType == typeof(TestIntegrationEvent)
            && error.Message.Contains(fieldName.Get);
    }

    [Property(MaxTest = 100)]
    public bool MappingError_ValidationFailed_HasCorrectCode(NonEmptyString reason)
    {
        var error = IntegrationEventMappingError.ValidationFailed<TestDomainEvent, TestIntegrationEvent>(
            reason.Get);

        return error.ErrorCode == "MAPPING_VALIDATION_FAILED"
            && error.Message.Contains(reason.Get);
    }

    [Property(MaxTest = 100)]
    public bool MappingError_LookupFailed_HasCorrectCode(NonEmptyString resource)
    {
        var exception = new InvalidOperationException("Lookup failed");
        var error = IntegrationEventMappingError.LookupFailed<TestDomainEvent, TestIntegrationEvent>(
            resource.Get,
            exception);

        return error.ErrorCode == "MAPPING_LOOKUP_FAILED"
            && error.Message.Contains(resource.Get)
            && error.InnerException == exception;
    }

    // === IntegrationEventPublishError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool PublishError_SerializationFailed_HasCorrectCode(Guid eventId)
    {
        var exception = new InvalidOperationException("Serialization failed");
        var error = IntegrationEventPublishError.SerializationFailed<TestIntegrationEvent>(
            eventId,
            exception);

        return error.ErrorCode == "PUBLISH_SERIALIZATION_FAILED"
            && error.EventType == typeof(TestIntegrationEvent)
            && error.EventId == eventId
            && error.InnerException == exception;
    }

    [Property(MaxTest = 100)]
    public bool PublishError_OutboxStoreFailed_HasCorrectCode(Guid eventId)
    {
        var exception = new InvalidOperationException("Outbox store failed");
        var error = IntegrationEventPublishError.OutboxStoreFailed<TestIntegrationEvent>(
            eventId,
            exception);

        return error.ErrorCode == "PUBLISH_OUTBOX_FAILED"
            && error.EventType == typeof(TestIntegrationEvent)
            && error.EventId == eventId
            && error.InnerException == exception;
    }

    [Property(MaxTest = 100)]
    public bool PublishError_BrokerFailed_HasCorrectCode(
        Guid eventId,
        NonEmptyString brokerName)
    {
        var exception = new InvalidOperationException("Broker failed");
        var error = IntegrationEventPublishError.BrokerFailed<TestIntegrationEvent>(
            eventId,
            brokerName.Get,
            exception);

        return error.ErrorCode == "PUBLISH_BROKER_FAILED"
            && error.Message.Contains(brokerName.Get)
            && error.EventType == typeof(TestIntegrationEvent)
            && error.EventId == eventId
            && error.InnerException == exception;
    }

    // === IntegrationEventMappingExtensions ===

    [Property(MaxTest = 100)]
    public bool MapTo_Extension_InvokesMapper(NonEmptyString data)
    {
        var domainEvent = new TestDomainEvent(data.Get);
        var mapper = new TestMapper();

        var result = domainEvent.MapTo(mapper);

        return result.Data == data.Get + "_mapped";
    }

    [Property(MaxTest = 100)]
    public bool MapAll_Extension_MapsAllEvents(PositiveInt count)
    {
        var actualCount = Math.Min(count.Get, 100); // Limit for performance
        var events = Enumerable.Range(1, actualCount)
            .Select(i => new TestDomainEvent($"event_{i}"))
            .ToList();
        var mapper = new TestMapper();

        var results = events.MapAll(mapper).ToList();

        return results.Count == actualCount
            && results.All(r => r.Data.EndsWith("_mapped", StringComparison.Ordinal));
    }

    [Property(MaxTest = 100)]
    public bool TryMapTo_Extension_ReturnsNoneOnNullMapper()
    {
        var domainEvent = new TestDomainEvent("test");
        IDomainEventToIntegrationEventMapper<TestDomainEvent, TestIntegrationEvent>? mapper = null;

        var result = domainEvent.TryMapTo(mapper!);

        return result.IsNone;
    }

    [Property(MaxTest = 100)]
    public bool TryMapTo_Extension_ReturnsSomeOnSuccess(NonEmptyString data)
    {
        var domainEvent = new TestDomainEvent(data.Get);
        var mapper = new TestMapper();

        var result = domainEvent.TryMapTo(mapper);

        return result.IsSome;
    }

    private sealed class TestMapper : IDomainEventToIntegrationEventMapper<TestDomainEvent, TestIntegrationEvent>
    {
        public TestIntegrationEvent Map(TestDomainEvent domainEvent) =>
            new(domainEvent.Data + "_mapped");
    }
}
