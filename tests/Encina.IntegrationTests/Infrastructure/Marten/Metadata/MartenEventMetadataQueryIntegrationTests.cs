using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.Marten.Metadata;

/// <summary>
/// Integration tests for <see cref="MartenEventMetadataQuery"/> with a real PostgreSQL database.
/// Tests the query capabilities for correlation/causation ID tracking and causal chain reconstruction.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenEventMetadataQueryIntegrationTests
{
    private readonly MartenFixture _fixture;

    public MartenEventMetadataQueryIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task GetEventsByCorrelationIdAsync_ReturnsMatchingEvents()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "query-correlation-" + Guid.NewGuid();
        var query = CreateQuery();

        // Create events with the correlation ID
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.Events.Append(Guid.NewGuid(), new TestEvent("Event 1"));
            session.Events.Append(Guid.NewGuid(), new TestEvent("Event 2"));
            session.Events.Append(Guid.NewGuid(), new TestEvent("Event 3"));
            await session.SaveChangesAsync();
        }

        // Create events with different correlation ID (noise)
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = "other-correlation";
            session.Events.Append(Guid.NewGuid(), new TestEvent("Other Event"));
            await session.SaveChangesAsync();
        }

        // Act
        var result = await query.GetEventsByCorrelationIdAsync(correlationId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        queryResult.Events.Count.ShouldBe(3);
        queryResult.TotalCount.ShouldBe(3);
        queryResult.HasMore.ShouldBeFalse();
        queryResult.Events.ShouldAllBe(e => e.CorrelationId == correlationId);
    }

    [SkippableFact]
    public async Task GetEventsByCorrelationIdAsync_WithPagination_ReturnsCorrectPage()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "paginated-" + Guid.NewGuid();
        var query = CreateQuery();

        // Create 10 events
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            for (var i = 0; i < 10; i++)
            {
                session.Events.Append(Guid.NewGuid(), new TestEvent($"Event {i}"));
            }

            await session.SaveChangesAsync();
        }

        // Act
        var options = new EventQueryOptions { Skip = 3, Take = 4 };
        var result = await query.GetEventsByCorrelationIdAsync(correlationId, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        queryResult.Events.Count.ShouldBe(4);
        queryResult.TotalCount.ShouldBe(10);
        queryResult.HasMore.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task GetEventsByCausationIdAsync_ReturnsMatchingEvents()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var causationId = "query-causation-" + Guid.NewGuid();
        var query = CreateQuery();

        // Create events with the causation ID
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CausationId = causationId;
            session.Events.Append(Guid.NewGuid(), new TestEvent("Caused Event 1"));
            session.Events.Append(Guid.NewGuid(), new TestEvent("Caused Event 2"));
            await session.SaveChangesAsync();
        }

        // Act
        var result = await query.GetEventsByCausationIdAsync(causationId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        queryResult.Events.Count.ShouldBe(2);
        queryResult.Events.ShouldAllBe(e => e.CausationId == causationId);
    }

    [SkippableFact]
    public async Task GetEventByIdAsync_ReturnsEvent()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var query = CreateQuery();
        var streamId = Guid.NewGuid();
        var correlationId = "get-by-id-" + Guid.NewGuid();
        Guid eventId;

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.Events.Append(streamId, new TestEvent("Test Event"));
            await session.SaveChangesAsync();

            var events = await session.Events.FetchStreamAsync(streamId);
            eventId = events[0].Id;
        }

        // Act
        var result = await query.GetEventByIdAsync(eventId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var eventWithMetadata = result.Match(e => e, _ => null!);
        eventWithMetadata.Id.ShouldBe(eventId);
        eventWithMetadata.StreamId.ShouldBe(streamId);
        eventWithMetadata.CorrelationId.ShouldBe(correlationId);
        eventWithMetadata.EventTypeName.ShouldContain("TestEvent");
    }

    [SkippableFact]
    public async Task GetEventByIdAsync_WhenNotFound_ReturnsError()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var query = CreateQuery();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await query.GetEventByIdAsync(nonExistentId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.EventNotFound),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [SkippableFact]
    public async Task GetCausalChainAsync_Ancestors_ReturnsChainInOrder()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "chain-ancestors-" + Guid.NewGuid();
        var query = CreateQuery();

        // Create a causal chain: Event A -> Event B -> Event C
        Guid eventAId, eventBId, eventCId;

        // Event A (root)
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = "root-command";
            var stream = Guid.NewGuid();
            session.Events.Append(stream, new TestEvent("Event A"));
            await session.SaveChangesAsync();
            var events = await session.Events.FetchStreamAsync(stream);
            eventAId = events[0].Id;
        }

        // Event B (caused by A)
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = eventAId.ToString();
            var stream = Guid.NewGuid();
            session.Events.Append(stream, new TestEvent("Event B"));
            await session.SaveChangesAsync();
            var events = await session.Events.FetchStreamAsync(stream);
            eventBId = events[0].Id;
        }

        // Event C (caused by B)
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = eventBId.ToString();
            var stream = Guid.NewGuid();
            session.Events.Append(stream, new TestEvent("Event C"));
            await session.SaveChangesAsync();
            var events = await session.Events.FetchStreamAsync(stream);
            eventCId = events[0].Id;
        }

        // Act - Get ancestors of Event C
        var result = await query.GetCausalChainAsync(eventCId, CausalChainDirection.Ancestors);

        // Assert
        result.IsRight.ShouldBeTrue();
        var chain = result.Match(c => c, _ => null!);

        // Chain should include C, B, A in order (C at position 0 as starting point)
        chain.Count.ShouldBeGreaterThanOrEqualTo(3);
        chain[0].Id.ShouldBe(eventCId);
    }

    [SkippableFact]
    public async Task GetCausalChainAsync_Descendants_ReturnsChildren()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "chain-descendants-" + Guid.NewGuid();
        var query = CreateQuery();

        // Event A (root)
        Guid eventAId;
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            var stream = Guid.NewGuid();
            session.Events.Append(stream, new TestEvent("Root Event"));
            await session.SaveChangesAsync();
            var events = await session.Events.FetchStreamAsync(stream);
            eventAId = events[0].Id;
        }

        // Create descendants
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = eventAId.ToString();
            session.Events.Append(Guid.NewGuid(), new TestEvent("Child 1"));
            session.Events.Append(Guid.NewGuid(), new TestEvent("Child 2"));
            await session.SaveChangesAsync();
        }

        // Act - Get descendants of root event
        var result = await query.GetCausalChainAsync(eventAId, CausalChainDirection.Descendants);

        // Assert
        result.IsRight.ShouldBeTrue();
        var chain = result.Match(c => c, _ => null!);
        chain.Count.ShouldBeGreaterThanOrEqualTo(3); // Root + 2 children
    }

    [SkippableFact]
    public async Task GetCausalChainAsync_WithMaxDepth_LimitsResults()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "chain-depth-" + Guid.NewGuid();
        var query = CreateQuery();

        // Create a 5-level deep chain
        var currentCausationId = "root";
        Guid lastEventId = Guid.Empty;

        for (var i = 0; i < 5; i++)
        {
            await using var session = _fixture.Store!.LightweightSession();
            session.CorrelationId = correlationId;
            session.CausationId = currentCausationId;
            var stream = Guid.NewGuid();
            session.Events.Append(stream, new TestEvent($"Level {i}"));
            await session.SaveChangesAsync();
            var events = await session.Events.FetchStreamAsync(stream);
            lastEventId = events[0].Id;
            currentCausationId = lastEventId.ToString();
        }

        // Act - Get ancestors with maxDepth of 2
        var result = await query.GetCausalChainAsync(lastEventId, CausalChainDirection.Ancestors, maxDepth: 2);

        // Assert
        result.IsRight.ShouldBeTrue();
        var chain = result.Match(c => c, _ => null!);
        chain.Count.ShouldBeLessThanOrEqualTo(3); // Start event + 2 ancestors max
    }

    [SkippableFact]
    public async Task GetEventsByCorrelationIdAsync_WithStreamFilter_ReturnsFilteredResults()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "stream-filter-" + Guid.NewGuid();
        var targetStreamId = Guid.NewGuid();
        var otherStreamId = Guid.NewGuid();
        var query = CreateQuery();

        // Create events in target stream
        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.Events.Append(targetStreamId, new TestEvent("Target 1"), new TestEvent("Target 2"));
            session.Events.Append(otherStreamId, new TestEvent("Other"));
            await session.SaveChangesAsync();
        }

        // Act
        var options = new EventQueryOptions { StreamId = targetStreamId };
        var result = await query.GetEventsByCorrelationIdAsync(correlationId, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        queryResult.Events.Count.ShouldBe(2);
        queryResult.Events.ShouldAllBe(e => e.StreamId == targetStreamId);
    }

    [SkippableFact]
    public async Task GetEventsByCorrelationIdAsync_WithTimeFilter_ReturnsFilteredResults()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "time-filter-" + Guid.NewGuid();
        var query = CreateQuery();

        var beforeCreation = DateTimeOffset.UtcNow.AddSeconds(-1);

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.Events.Append(Guid.NewGuid(), new TestEvent("Timed Event"));
            await session.SaveChangesAsync();
        }

        var afterCreation = DateTimeOffset.UtcNow.AddSeconds(1);

        // Act
        var options = new EventQueryOptions
        {
            FromTimestamp = beforeCreation,
            ToTimestamp = afterCreation,
        };
        var result = await query.GetEventsByCorrelationIdAsync(correlationId, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        queryResult.Events.Count.ShouldBe(1);
    }

    [SkippableFact]
    public async Task EventWithMetadata_ContainsAllExpectedFields()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        var correlationId = "metadata-fields-" + Guid.NewGuid();
        var causationId = "test-causation";
        var streamId = Guid.NewGuid();
        var query = CreateQuery();

        await using (var session = _fixture.Store!.LightweightSession())
        {
            session.CorrelationId = correlationId;
            session.CausationId = causationId;
            session.SetHeader("CustomKey", "CustomValue");
            session.Events.Append(streamId, new TestEvent("Full Metadata Event"));
            await session.SaveChangesAsync();
        }

        // Act
        var result = await query.GetEventsByCorrelationIdAsync(correlationId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var queryResult = result.Match(r => r, _ => null!);
        var ev = queryResult.Events[0];

        ev.Id.ShouldNotBe(Guid.Empty);
        ev.StreamId.ShouldBe(streamId);
        ev.Version.ShouldBeGreaterThan(0);
        ev.Sequence.ShouldBeGreaterThan(0);
        ev.EventTypeName.ShouldNotBeNullOrEmpty();
        ev.Data.ShouldNotBeNull();
        ev.Timestamp.ShouldBeGreaterThan(DateTimeOffset.MinValue);
        ev.CorrelationId.ShouldBe(correlationId);
        ev.CausationId.ShouldBe(causationId);
        ev.Headers.ShouldContainKey("CustomKey");
        ev.Headers["CustomKey"].ShouldBe("CustomValue");
    }

    private MartenEventMetadataQuery CreateQuery()
    {
        return new MartenEventMetadataQuery(
            _fixture.Store!,
            NullLogger<MartenEventMetadataQuery>.Instance);
    }

    // Test event
    public record TestEvent(string Name);
}
