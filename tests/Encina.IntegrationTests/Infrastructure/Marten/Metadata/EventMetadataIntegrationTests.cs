using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Marten;
using Marten.Events;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.Marten.Metadata;

/// <summary>
/// Integration tests for event metadata persistence and querying with a real PostgreSQL database.
/// Tests correlation/causation ID tracking, headers, and query capabilities.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class EventMetadataIntegrationTests
{
    private readonly MartenFixture _fixture;

    public EventMetadataIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Events_WithCorrelationId_ArePersisted()
    {

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var streamId = Guid.NewGuid();
        var correlationId = "test-correlation-" + Guid.NewGuid();

        // Set correlation ID on session
        session.CorrelationId = correlationId;

        // Act
        session.Events.Append(streamId, new TestOrderCreated(streamId, "Test Order"));
        await session.SaveChangesAsync();

        // Assert - Query events back
        var events = await session.Events.FetchStreamAsync(streamId);
        events.ShouldNotBeEmpty();
        events[0].CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public async Task Events_WithCausationId_ArePersisted()
    {

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var streamId = Guid.NewGuid();
        var causationId = "test-causation-" + Guid.NewGuid();

        // Set causation ID on session
        session.CausationId = causationId;

        // Act
        session.Events.Append(streamId, new TestOrderCreated(streamId, "Test Order"));
        await session.SaveChangesAsync();

        // Assert - Query events back
        var events = await session.Events.FetchStreamAsync(streamId);
        events.ShouldNotBeEmpty();
        events[0].CausationId.ShouldBe(causationId);
    }

    [Fact]
    public async Task Events_WithHeaders_ArePersisted()
    {

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var streamId = Guid.NewGuid();

        // Set headers on session
        session.SetHeader("UserId", "user-123");
        session.SetHeader("TenantId", "tenant-abc");
        session.SetHeader("Environment", "Test");

        // Act
        session.Events.Append(streamId, new TestOrderCreated(streamId, "Test Order"));
        await session.SaveChangesAsync();

        // Assert - Query events back
        var events = await session.Events.FetchStreamAsync(streamId);
        events.ShouldNotBeEmpty();
        var eventHeaders = events[0].Headers;
        eventHeaders.ShouldNotBeNull();
        eventHeaders!["UserId"].ShouldBe("user-123");
        eventHeaders!["TenantId"].ShouldBe("tenant-abc");
        eventHeaders!["Environment"].ShouldBe("Test");
    }

    [Fact]
    public async Task Events_WithAllMetadata_ArePersisted()
    {

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var streamId = Guid.NewGuid();
        var correlationId = "correlation-" + Guid.NewGuid();
        var causationId = "causation-" + Guid.NewGuid();

        session.CorrelationId = correlationId;
        session.CausationId = causationId;
        session.SetHeader("UserId", "user-456");
        session.SetHeader("CommitSha", "abc123");

        // Act
        session.Events.Append(streamId, new TestOrderCreated(streamId, "Full Metadata Order"));
        await session.SaveChangesAsync();

        // Assert
        var events = await session.Events.FetchStreamAsync(streamId);
        var ev = events[0];

        ev.CorrelationId.ShouldBe(correlationId);
        ev.CausationId.ShouldBe(causationId);
        ev.Headers!["UserId"].ShouldBe("user-456");
        ev.Headers!["CommitSha"].ShouldBe("abc123");
    }

    [Fact]
    public async Task MultipleEvents_ShareSameMetadata()
    {

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var streamId = Guid.NewGuid();
        var correlationId = "shared-correlation-" + Guid.NewGuid();

        session.CorrelationId = correlationId;

        // Act - Append multiple events
        session.Events.Append(
            streamId,
            new TestOrderCreated(streamId, "Order 1"),
            new TestOrderItemAdded(streamId, "Item 1", 10.00m),
            new TestOrderItemAdded(streamId, "Item 2", 20.00m));
        await session.SaveChangesAsync();

        // Assert - All events should have the same correlation ID
        var events = await session.Events.FetchStreamAsync(streamId);
        events.Count.ShouldBe(3);
        events.ShouldAllBe(e => e.CorrelationId == correlationId);
    }

    [Fact]
    public async Task QueryEvents_ByCorrelationId_ReturnsMatchingEvents()
    {

        // Arrange - Create events with different correlation IDs
        var correlationId = "query-test-" + Guid.NewGuid();

        await using (var session1 = _fixture.Store!.LightweightSession())
        {
            session1.CorrelationId = correlationId;
            session1.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Order A"));
            session1.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Order B"));
            await session1.SaveChangesAsync();
        }

        await using (var session2 = _fixture.Store!.LightweightSession())
        {
            session2.CorrelationId = "other-correlation";
            session2.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Order C"));
            await session2.SaveChangesAsync();
        }

        // Act - Query by correlation ID using raw events
        await using var querySession = _fixture.Store!.LightweightSession();
        var matchingEvents = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CorrelationId == correlationId)
            .ToListAsync();

        // Assert
        matchingEvents.Count.ShouldBe(2);
        matchingEvents.ShouldAllBe(e => e.CorrelationId == correlationId);
    }

    [Fact]
    public async Task QueryEvents_ByCausationId_ReturnsMatchingEvents()
    {

        // Arrange - Create a chain of causally-related events
        var causationId = "causation-query-" + Guid.NewGuid();

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CausationId = causationId;
            session.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Caused Order 1"));
            session.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Caused Order 2"));
            await session.SaveChangesAsync();
        }

        // Act
        await using var querySession = _fixture.Store!.LightweightSession();
        var matchingEvents = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CausationId == causationId)
            .ToListAsync();

        // Assert
        matchingEvents.Count.ShouldBe(2);
        matchingEvents.ShouldAllBe(e => e.CausationId == causationId);
    }

    [Fact]
    public async Task CausalChain_CanBeReconstructed()
    {

        // Arrange - Create a causal chain: Command -> Event A -> Event B -> Event C
        var correlationId = "chain-" + Guid.NewGuid();

        // First event (root cause - from a command)
        Guid eventAId;
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = "command-123"; // Caused by a command
            var streamA = Guid.NewGuid();
            session.Events.Append(streamA, new TestOrderCreated(streamA, "Event A"));
            await session.SaveChangesAsync();

            var events = await session.Events.FetchStreamAsync(streamA);
            eventAId = events[0].Id;
        }

        // Second event (caused by Event A)
        Guid eventBId;
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = eventAId.ToString(); // Caused by Event A
            var streamB = Guid.NewGuid();
            session.Events.Append(streamB, new TestOrderItemAdded(streamB, "Event B", 50.00m));
            await session.SaveChangesAsync();

            var events = await session.Events.FetchStreamAsync(streamB);
            eventBId = events[0].Id;
        }

        // Third event (caused by Event B)
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = eventBId.ToString(); // Caused by Event B
            var streamC = Guid.NewGuid();
            session.Events.Append(streamC, new TestOrderCompleted(streamC));
            await session.SaveChangesAsync();
        }

        // Act - Query all events in the correlation group
        await using var querySession = _fixture.Store!.LightweightSession();
        var allRelatedEvents = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        // Assert - All three events should be in the same correlation group
        allRelatedEvents.Count.ShouldBe(3);

        // Verify causal relationships
        allRelatedEvents[0].CausationId.ShouldBe("command-123");
        allRelatedEvents[1].CausationId.ShouldBe(eventAId.ToString());
        allRelatedEvents[2].CausationId.ShouldBe(eventBId.ToString());
    }

    [Fact]
    public async Task Events_WithPagination_ReturnsCorrectPage()
    {

        // Arrange - Create multiple events with the same correlation ID
        var correlationId = "pagination-" + Guid.NewGuid();

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            var streamId = Guid.NewGuid();

            // Create 10 events
            for (var i = 0; i < 10; i++)
            {
                session.Events.Append(
                    streamId,
                    new TestOrderItemAdded(streamId, $"Item {i}", i * 10.00m));
            }

            await session.SaveChangesAsync();
        }

        // Act - Query with pagination
        await using var querySession = _fixture.Store!.LightweightSession();
        var page1 = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Sequence)
            .Skip(0)
            .Take(5)
            .ToListAsync();

        var page2 = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Sequence)
            .Skip(5)
            .Take(5)
            .ToListAsync();

        // Assert
        page1.Count.ShouldBe(5);
        page2.Count.ShouldBe(5);

        // Ensure no overlap
        var page1Ids = page1.Select(e => e.Id).ToHashSet();
        var page2Ids = page2.Select(e => e.Id).ToHashSet();
        page1Ids.Intersect(page2Ids).ShouldBeEmpty();
    }

    [Fact]
    public async Task Events_CanBeFilteredByTimestamp()
    {

        // Arrange
        var correlationId = "timestamp-filter-" + Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.Events.Append(Guid.NewGuid(), new TestOrderCreated(Guid.NewGuid(), "Timestamped Order"));
            await session.SaveChangesAsync();
        }

        var afterCreation = DateTimeOffset.UtcNow;

        // Act
        await using var querySession = _fixture.Store!.LightweightSession();
        var eventsInRange = await querySession.Events
            .QueryAllRawEvents()
            .Where(e => e.CorrelationId == correlationId)
            .Where(e => e.Timestamp >= beforeCreation && e.Timestamp <= afterCreation)
            .ToListAsync();

        // Assert
        eventsInRange.Count.ShouldBe(1);
    }

    // Test domain events
    public record TestOrderCreated(Guid OrderId, string Name);
    public record TestOrderItemAdded(Guid OrderId, string ItemName, decimal Price);
    public record TestOrderCompleted(Guid OrderId);
}
