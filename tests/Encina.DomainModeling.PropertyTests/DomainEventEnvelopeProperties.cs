using System.Globalization;
using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for domain event envelope patterns.
/// </summary>
public class DomainEventEnvelopeProperties
{
    private sealed record TestDomainEvent(string Data) : DomainEvent;

    // === DomainEventMetadata Properties ===

    [Property(MaxTest = 100)]
    public bool Empty_AlwaysReturnsEmptyMetadata()
    {
        var metadata = DomainEventMetadata.Empty;

        return metadata.CorrelationId is null
            && metadata.CausationId is null
            && metadata.UserId is null
            && metadata.TenantId is null
            && metadata.AdditionalMetadata.Count == 0;
    }

    [Property(MaxTest = 100)]
    public bool WithCorrelation_PreservesCorrelationId(NonEmptyString correlationId)
    {
        var metadata = DomainEventMetadata.WithCorrelation(correlationId.Get);

        return metadata.CorrelationId == correlationId.Get
            && metadata.CausationId is null
            && metadata.UserId is null
            && metadata.TenantId is null;
    }

    [Property(MaxTest = 100)]
    public bool WithCausation_PreservesCorrelationAndCausation(
        NonEmptyString correlationId,
        NonEmptyString causationId)
    {
        var metadata = DomainEventMetadata.WithCausation(correlationId.Get, causationId.Get);

        return metadata.CorrelationId == correlationId.Get
            && metadata.CausationId == causationId.Get;
    }

    [Property(MaxTest = 100)]
    public bool AdditionalMetadata_DefaultsToEmpty()
    {
        var metadata = new DomainEventMetadata();

        return metadata.AdditionalMetadata.Count == 0;
    }

    // === DomainEventEnvelope Properties ===

    [Property(MaxTest = 100)]
    public bool Create_WithMetadata_PreservesEventAndMetadata(
        NonEmptyString eventData,
        NonEmptyString correlationId)
    {
        var @event = new TestDomainEvent(eventData.Get);
        var metadata = DomainEventMetadata.WithCorrelation(correlationId.Get);

        var envelope = DomainEventEnvelope.Create(@event, metadata);

        return envelope.Event.Data == eventData.Get
            && envelope.Metadata.CorrelationId == correlationId.Get;
    }

    [Property(MaxTest = 100)]
    public bool Create_WithoutMetadata_UsesEmptyMetadata(NonEmptyString eventData)
    {
        var @event = new TestDomainEvent(eventData.Get);

        var envelope = DomainEventEnvelope.Create(@event);

        return envelope.Event.Data == eventData.Get
            && envelope.Metadata.CorrelationId is null;
    }

    [Property(MaxTest = 100)]
    public bool Envelope_HasUniqueIds(NonEmptyString eventData)
    {
        var @event = new TestDomainEvent(eventData.Get);

        var envelope1 = DomainEventEnvelope.Create(@event);
        var envelope2 = DomainEventEnvelope.Create(@event);

        return envelope1.EnvelopeId != envelope2.EnvelopeId;
    }

    [Property(MaxTest = 100)]
    public bool Envelope_EnvelopeCreatedAtUtc_IsRecent(NonEmptyString eventData)
    {
        var before = DateTime.UtcNow;
        var @event = new TestDomainEvent(eventData.Get);
        var envelope = DomainEventEnvelope.Create(@event);
        var after = DateTime.UtcNow;

        return envelope.EnvelopeCreatedAtUtc >= before
            && envelope.EnvelopeCreatedAtUtc <= after;
    }

    // === Extension Methods Properties ===

    [Property(MaxTest = 100)]
    public bool ToEnvelope_Extension_CreatesValidEnvelope(NonEmptyString eventData)
    {
        var @event = new TestDomainEvent(eventData.Get);

        var envelope = @event.ToEnvelope();

        // Check that metadata has empty/default values
        return envelope.Event == @event
            && envelope.Metadata.CorrelationId is null
            && envelope.Metadata.CausationId is null
            && envelope.Metadata.UserId is null
            && envelope.Metadata.TenantId is null;
    }

    [Property(MaxTest = 100)]
    public bool WithMetadata_Extension_AttachesMetadata(
        NonEmptyString eventData,
        NonEmptyString userId)
    {
        var @event = new TestDomainEvent(eventData.Get);
        var metadata = new DomainEventMetadata(UserId: userId.Get);

        var envelope = @event.WithMetadata(metadata);

        return envelope.Event == @event
            && envelope.Metadata.UserId == userId.Get;
    }

    [Property(MaxTest = 100)]
    public bool WithCorrelation_Extension_SetsCorrelationId(
        NonEmptyString eventData,
        NonEmptyString correlationId)
    {
        var @event = new TestDomainEvent(eventData.Get);

        var envelope = @event.WithCorrelation(correlationId.Get);

        return envelope.Event == @event
            && envelope.Metadata.CorrelationId == correlationId.Get;
    }

    [Property(MaxTest = 100)]
    public bool Map_Extension_TransformsEvent(NonEmptyString eventData)
    {
        var @event = new TestDomainEvent(eventData.Get);
        var metadata = DomainEventMetadata.WithCorrelation("test-correlation");
        var envelope = DomainEventEnvelope.Create(@event, metadata);

        var mapped = envelope.Map(e => new TestDomainEvent(e.Data.ToUpperInvariant()));

        return string.Equals(mapped.Event.Data, eventData.Get.ToUpperInvariant(), StringComparison.Ordinal)
            && mapped.Metadata.CorrelationId == "test-correlation"
            && mapped.EnvelopeId == envelope.EnvelopeId;
    }
}
