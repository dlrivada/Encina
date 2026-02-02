using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="EventMetadataEnrichmentService"/>.
/// </summary>
public sealed class EventMetadataEnrichmentServiceTests
{
    private readonly IDocumentSession _mockSession;
    private readonly ILogger _mockLogger;
    private readonly EventMetadataOptions _options;
    private readonly List<IEventMetadataEnricher> _enrichers;

    public EventMetadataEnrichmentServiceTests()
    {
        _mockSession = Substitute.For<IDocumentSession>();
        _mockLogger = Substitute.For<ILogger>();
        _options = new EventMetadataOptions();
        _enrichers = [];
    }

    [Fact]
    public void EnrichSession_WithCorrelationId_SetsOnSession()
    {
        // Arrange
        _options.CorrelationIdEnabled = true;
        var service = CreateService();
        var context = CreateContext(correlationId: "test-correlation-id");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.CorrelationId.ShouldBe("test-correlation-id");
    }

    [Fact]
    public void EnrichSession_WithCorrelationIdDisabled_DoesNotSet()
    {
        // Arrange
        _options.CorrelationIdEnabled = false;
        var service = CreateService();
        var context = CreateContext(correlationId: "test-correlation-id");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.CorrelationId.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void EnrichSession_WithCausationId_SetsOnSession()
    {
        // Arrange
        _options.CausationIdEnabled = true;
        var service = CreateService();
        var context = CreateContext(correlationId: "test-correlation-id");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        // Causation ID defaults to correlation ID when not explicitly set
        _mockSession.CausationId.ShouldBe("test-correlation-id");
    }

    [Fact]
    public void EnrichSession_WithExplicitCausationId_UsesExplicitValue()
    {
        // Arrange
        _options.CausationIdEnabled = true;
        var service = CreateService();
        var context = CreateContext(
            correlationId: "test-correlation-id",
            metadata: new Dictionary<string, object?> { ["CausationId"] = "explicit-causation-id" });

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.CausationId.ShouldBe("explicit-causation-id");
    }

    [Fact]
    public void EnrichSession_WithCausationIdDisabled_DoesNotSet()
    {
        // Arrange
        _options.CausationIdEnabled = false;
        var service = CreateService();
        var context = CreateContext(correlationId: "test-correlation-id");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.CausationId.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void EnrichSession_WithUserIdEnabled_SetsHeader()
    {
        // Arrange
        _options.CaptureUserId = true;
        _options.HeadersEnabled = true;
        var service = CreateService();
        var context = CreateContext(userId: "user-123");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("UserId", "user-123");
    }

    [Fact]
    public void EnrichSession_WithTenantIdEnabled_SetsHeader()
    {
        // Arrange
        _options.CaptureTenantId = true;
        _options.HeadersEnabled = true;
        var service = CreateService();
        var context = CreateContext(tenantId: "tenant-abc");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("TenantId", "tenant-abc");
    }

    [Fact]
    public void EnrichSession_WithTimestampEnabled_SetsHeader()
    {
        // Arrange
        _options.CaptureTimestamp = true;
        _options.HeadersEnabled = true;
        var service = CreateService();
        var timestamp = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var context = CreateContext(timestamp: timestamp);

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("Timestamp", timestamp.ToString("O"));
    }

    [Fact]
    public void EnrichSession_WithCommitShaEnabled_SetsHeader()
    {
        // Arrange
        _options.CaptureCommitSha = true;
        _options.CommitSha = "abc123def";
        _options.HeadersEnabled = true;
        var service = CreateService();
        var context = CreateContext();

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("CommitSha", "abc123def");
    }

    [Fact]
    public void EnrichSession_WithSemanticVersionEnabled_SetsHeader()
    {
        // Arrange
        _options.CaptureSemanticVersion = true;
        _options.SemanticVersion = "1.2.3-beta";
        _options.HeadersEnabled = true;
        var service = CreateService();
        var context = CreateContext();

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("SemanticVersion", "1.2.3-beta");
    }

    [Fact]
    public void EnrichSession_WithCustomHeaders_SetsHeaders()
    {
        // Arrange
        _options.HeadersEnabled = true;
        _options.CustomHeaders["Environment"] = "Production";
        _options.CustomHeaders["Region"] = "eu-west-1";
        var service = CreateService();
        var context = CreateContext();

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("Environment", "Production");
        _mockSession.Received(1).SetHeader("Region", "eu-west-1");
    }

    [Fact]
    public void EnrichSession_WithHeadersDisabled_DoesNotSetHeaders()
    {
        // Arrange
        _options.CaptureUserId = true;
        _options.CaptureTenantId = true;
        _options.HeadersEnabled = false;
        var service = CreateService();
        var context = CreateContext(userId: "user-123", tenantId: "tenant-abc");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.DidNotReceive().SetHeader(Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public void EnrichSession_WithEnricher_InvokesEnricher()
    {
        // Arrange
        _options.HeadersEnabled = true;
        var mockEnricher = Substitute.For<IEventMetadataEnricher>();
        mockEnricher.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(new Dictionary<string, object> { ["CustomKey"] = "CustomValue" });
        _enrichers.Add(mockEnricher);

        var service = CreateService();
        var context = CreateContext();
        var testEvent = new TestEvent();

        // Act
        service.EnrichSession(_mockSession, context, [testEvent]);

        // Assert
        mockEnricher.Received(1).EnrichMetadata(testEvent, context);
        _mockSession.Received(1).SetHeader("CustomKey", "CustomValue");
    }

    [Fact]
    public void EnrichSession_WithMultipleEnrichers_InvokesAll()
    {
        // Arrange
        _options.HeadersEnabled = true;
        var enricher1 = Substitute.For<IEventMetadataEnricher>();
        enricher1.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(new Dictionary<string, object> { ["Key1"] = "Value1" });

        var enricher2 = Substitute.For<IEventMetadataEnricher>();
        enricher2.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(new Dictionary<string, object> { ["Key2"] = "Value2" });

        _enrichers.Add(enricher1);
        _enrichers.Add(enricher2);

        var service = CreateService();
        var context = CreateContext();

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.Received(1).SetHeader("Key1", "Value1");
        _mockSession.Received(1).SetHeader("Key2", "Value2");
    }

    [Fact]
    public void EnrichSession_WhenEnricherThrows_ContinuesWithOtherEnrichers()
    {
        // Arrange
        _options.HeadersEnabled = true;
        var failingEnricher = Substitute.For<IEventMetadataEnricher>();
        failingEnricher.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(_ => throw new InvalidOperationException("Enricher failed"));

        var workingEnricher = Substitute.For<IEventMetadataEnricher>();
        workingEnricher.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(new Dictionary<string, object> { ["Key"] = "Value" });

        _enrichers.Add(failingEnricher);
        _enrichers.Add(workingEnricher);

        var service = CreateService();
        var context = CreateContext();

        // Act - should not throw
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert - working enricher should still execute
        _mockSession.Received(1).SetHeader("Key", "Value");
    }

    [Fact]
    public void EnrichSession_WithMultipleEvents_InvokesEnricherForEach()
    {
        // Arrange
        _options.HeadersEnabled = true;
        var enricher = Substitute.For<IEventMetadataEnricher>();
        enricher.EnrichMetadata(Arg.Any<object>(), Arg.Any<IRequestContext>())
            .Returns(new Dictionary<string, object>());
        _enrichers.Add(enricher);

        var service = CreateService();
        var context = CreateContext();
        var events = new object[] { new TestEvent(), new TestEvent(), new TestEvent() };

        // Act
        service.EnrichSession(_mockSession, context, events);

        // Assert
        enricher.Received(3).EnrichMetadata(Arg.Any<object>(), context);
    }

    [Fact]
    public void EnrichSession_WithNullSession_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var context = CreateContext();

        // Act & Assert
        var act = () => service.EnrichSession(null!, context, [new TestEvent()]);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void EnrichSession_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.EnrichSession(_mockSession, null!, [new TestEvent()]);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
    }

    [Fact]
    public void EnrichSession_WithNullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();
        var context = CreateContext();

        // Act & Assert
        var act = () => service.EnrichSession(_mockSession, context, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("events");
    }

    [Fact]
    public void EnrichSession_WithEmptyCorrelationId_DoesNotSet()
    {
        // Arrange
        _options.CorrelationIdEnabled = true;
        var service = CreateService();
        var context = CreateContext(correlationId: "");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.CorrelationId.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void EnrichSession_WithWhitespaceUserId_DoesNotSetHeader()
    {
        // Arrange
        _options.CaptureUserId = true;
        _options.HeadersEnabled = true;
        var service = CreateService();
        var context = CreateContext(userId: "   ");

        // Act
        service.EnrichSession(_mockSession, context, [new TestEvent()]);

        // Assert
        _mockSession.DidNotReceive().SetHeader("UserId", Arg.Any<object>());
    }

    private EventMetadataEnrichmentService CreateService()
    {
        return new EventMetadataEnrichmentService(_options, _enrichers, _mockLogger);
    }

    private static TestRequestContext CreateContext(
        string? correlationId = "default-correlation",
        string? userId = null,
        string? tenantId = null,
        DateTimeOffset? timestamp = null,
        IDictionary<string, object?>? metadata = null)
    {
        return new TestRequestContext(
            correlationId ?? "default-correlation",
            userId,
            tenantId,
            timestamp ?? DateTimeOffset.UtcNow,
            metadata ?? new Dictionary<string, object?>());
    }

    private sealed class TestEvent;

    private sealed class TestRequestContext : IRequestContext
    {
        private readonly Dictionary<string, object?> _metadata;

        public TestRequestContext(
            string correlationId,
            string? userId,
            string? tenantId,
            DateTimeOffset timestamp,
            IDictionary<string, object?> metadata)
        {
            CorrelationId = correlationId;
            UserId = userId;
            TenantId = tenantId;
            Timestamp = timestamp;
            _metadata = new Dictionary<string, object?>(metadata);
        }

        public string CorrelationId { get; }
        public string? UserId { get; }
        public string? IdempotencyKey => null;
        public string? TenantId { get; }
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyDictionary<string, object?> Metadata => _metadata;

        public IRequestContext WithMetadata(string key, object? value)
        {
            var newMetadata = new Dictionary<string, object?>(_metadata) { [key] = value };
            return new TestRequestContext(CorrelationId, UserId, TenantId, Timestamp, newMetadata);
        }

        public IRequestContext WithUserId(string? userId)
            => new TestRequestContext(CorrelationId, userId, TenantId, Timestamp, _metadata);

        public IRequestContext WithIdempotencyKey(string? idempotencyKey)
            => this; // Not used in tests

        public IRequestContext WithTenantId(string? tenantId)
            => new TestRequestContext(CorrelationId, UserId, tenantId, Timestamp, _metadata);
    }
}
