using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for DomainEventMetadata, DomainEventEnvelope, and DomainEventExtensions.
/// </summary>
public sealed class DomainEventEnvelopeTests
{
    private static readonly DateTime TestTimestampUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region Test Domain Events

    private sealed record TestDomainEvent(Guid Id, string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = TestTimestampUtc;
    }

    private sealed record AnotherDomainEvent(Guid Id, int Value) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = TestTimestampUtc;
    }

    #endregion

    #region DomainEventMetadata Tests

    [Fact]
    public void DomainEventMetadata_Empty_ReturnsEmptyMetadata()
    {
        // Act
        var metadata = DomainEventMetadata.Empty;

        // Assert
        metadata.ShouldNotBeNull();
        metadata.CorrelationId.ShouldBeNull();
        metadata.CausationId.ShouldBeNull();
        metadata.UserId.ShouldBeNull();
        metadata.TenantId.ShouldBeNull();
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(0);
    }

    [Fact]
    public void DomainEventMetadata_WithCorrelation_ReturnsMetadataWithCorrelationId()
    {
        // Arrange
        var correlationId = "test-correlation-123";

        // Act
        var metadata = DomainEventMetadata.WithCorrelation(correlationId);

        // Assert
        metadata.CorrelationId.ShouldBe(correlationId);
        metadata.CausationId.ShouldBeNull();
    }

    [Fact]
    public void DomainEventMetadata_WithCausation_ReturnsMetadataWithBothIds()
    {
        // Arrange
        var correlationId = "test-correlation-123";
        var causationId = "test-causation-456";

        // Act
        var metadata = DomainEventMetadata.WithCausation(correlationId, causationId);

        // Assert
        metadata.CorrelationId.ShouldBe(correlationId);
        metadata.CausationId.ShouldBe(causationId);
    }

    [Fact]
    public void DomainEventMetadata_Constructor_SetsAllProperties()
    {
        // Arrange
        var correlationId = "corr-1";
        var causationId = "cause-1";
        var userId = "user-1";
        var tenantId = "tenant-1";
        var additionalMetadata = new Dictionary<string, string> { ["key1"] = "value1" };

        // Act
        var metadata = new DomainEventMetadata(
            correlationId,
            causationId,
            userId,
            tenantId,
            additionalMetadata);

        // Assert
        metadata.CorrelationId.ShouldBe(correlationId);
        metadata.CausationId.ShouldBe(causationId);
        metadata.UserId.ShouldBe(userId);
        metadata.TenantId.ShouldBe(tenantId);
        metadata.AdditionalMetadata["key1"].ShouldBe("value1");
    }

    [Fact]
    public void DomainEventMetadata_NullAdditionalMetadata_DefaultsToEmptyDictionary()
    {
        // Act
        var metadata = new DomainEventMetadata(AdditionalMetadata: null);

        // Assert
        metadata.AdditionalMetadata.ShouldNotBeNull();
        metadata.AdditionalMetadata.Count.ShouldBe(0);
    }

    #endregion

    #region DomainEventEnvelope Factory Tests

    [Fact]
    public void DomainEventEnvelope_Create_WithMetadata_CreatesEnvelope()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");
        var metadata = DomainEventMetadata.WithCorrelation("corr-123");

        // Act
        var envelope = DomainEventEnvelope.Create(domainEvent, metadata);

        // Assert
        envelope.ShouldNotBeNull();
        envelope.Event.ShouldBe(domainEvent);
        envelope.Metadata.ShouldBe(metadata);
        envelope.EnvelopeId.ShouldNotBe(Guid.Empty);
        envelope.EnvelopeCreatedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void DomainEventEnvelope_Create_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var metadata = DomainEventMetadata.Empty;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            DomainEventEnvelope.Create<TestDomainEvent>(null!, metadata));
    }

    [Fact]
    public void DomainEventEnvelope_Create_NullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            DomainEventEnvelope.Create(domainEvent, null!));
    }

    [Fact]
    public void DomainEventEnvelope_Create_WithoutMetadata_CreatesEnvelopeWithEmptyMetadata()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");

        // Act
        var envelope = DomainEventEnvelope.Create(domainEvent);

        // Assert
        envelope.ShouldNotBeNull();
        envelope.Event.ShouldBe(domainEvent);
        envelope.Metadata.ShouldNotBeNull();
        envelope.Metadata.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void DomainEventEnvelope_Create_NullEventWithoutMetadata_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            DomainEventEnvelope.Create<TestDomainEvent>(null!));
    }

    [Fact]
    public void DomainEventEnvelope_WithCorrelation_CreatesEnvelopeWithCorrelationMetadata()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");
        var correlationId = "corr-456";

        // Act
        var envelope = DomainEventEnvelope.WithCorrelation(domainEvent, correlationId);

        // Assert
        envelope.ShouldNotBeNull();
        envelope.Event.ShouldBe(domainEvent);
        envelope.Metadata.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void DomainEventEnvelope_WithCorrelation_NullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            DomainEventEnvelope.WithCorrelation<TestDomainEvent>(null!, "corr-123"));
    }

    #endregion

    #region DomainEventEnvelope<T> Record Tests

    [Fact]
    public void DomainEventEnvelope_Record_GeneratesUniqueIds()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");

        // Act
        var envelope1 = DomainEventEnvelope.Create(domainEvent);
        var envelope2 = DomainEventEnvelope.Create(domainEvent);

        // Assert
        envelope1.EnvelopeId.ShouldNotBe(envelope2.EnvelopeId);
    }

    [Fact]
    public void DomainEventEnvelope_Record_PreservesEventData()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var domainEvent = new TestDomainEvent(eventId, "important-data");

        // Act
        var envelope = DomainEventEnvelope.Create(domainEvent);

        // Assert
        envelope.Event.Id.ShouldBe(eventId);
        envelope.Event.Data.ShouldBe("important-data");
    }

    #endregion

    #region DomainEventExtensions Tests

    [Fact]
    public void WithMetadata_Extension_WrapsEventInEnvelope()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");
        var metadata = DomainEventMetadata.WithCorrelation("corr-ext");

        // Act
        var envelope = domainEvent.WithMetadata(metadata);

        // Assert
        envelope.ShouldNotBeNull();
        envelope.Event.ShouldBe(domainEvent);
        envelope.Metadata.CorrelationId.ShouldBe("corr-ext");
    }

    [Fact]
    public void ToEnvelope_Extension_WrapsEventWithEmptyMetadata()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");

        // Act
        var envelope = domainEvent.ToEnvelope();

        // Assert
        envelope.ShouldNotBeNull();
        envelope.Event.ShouldBe(domainEvent);
        envelope.Metadata.ShouldNotBeNull();
    }

    [Fact]
    public void WithCorrelation_Extension_WrapsEventWithCorrelationId()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), "test-data");

        // Act
        var envelope = domainEvent.WithCorrelation("corr-789");

        // Assert
        envelope.Metadata.CorrelationId.ShouldBe("corr-789");
    }

    [Fact]
    public void Map_Extension_TransformsEventPreservesMetadata()
    {
        // Arrange
        var originalEvent = new TestDomainEvent(Guid.NewGuid(), "original-data");
        var metadata = DomainEventMetadata.WithCorrelation("map-corr");
        var envelope = DomainEventEnvelope.Create(originalEvent, metadata);

        // Act
        var mappedEnvelope = envelope.Map(e => new AnotherDomainEvent(e.Id, 42));

        // Assert
        mappedEnvelope.Event.Id.ShouldBe(originalEvent.Id);
        mappedEnvelope.Event.Value.ShouldBe(42);
        mappedEnvelope.Metadata.CorrelationId.ShouldBe("map-corr");
        mappedEnvelope.EnvelopeId.ShouldBe(envelope.EnvelopeId);
        mappedEnvelope.EnvelopeCreatedAtUtc.ShouldBe(envelope.EnvelopeCreatedAtUtc);
    }

    [Fact]
    public void Map_Extension_NullEnvelope_ThrowsArgumentNullException()
    {
        // Arrange
        DomainEventEnvelope<TestDomainEvent>? envelope = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            envelope!.Map(e => new AnotherDomainEvent(e.Id, 0)));
    }

    [Fact]
    public void Map_Extension_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var envelope = new TestDomainEvent(Guid.NewGuid(), "data").ToEnvelope();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            envelope.Map<TestDomainEvent, AnotherDomainEvent>(null!));
    }

    #endregion
}
