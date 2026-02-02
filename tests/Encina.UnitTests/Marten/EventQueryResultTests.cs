using Encina.Marten;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="EventQueryResult"/>.
/// </summary>
public sealed class EventQueryResultTests
{
    [Fact]
    public void Properties_CanBeInitialized()
    {
        var events = new List<EventWithMetadata>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StreamId = Guid.NewGuid(),
                Version = 1,
                Sequence = 100,
                EventTypeName = "TestEvent",
                Data = new object(),
                Timestamp = DateTimeOffset.UtcNow,
            },
        };

        var result = new EventQueryResult
        {
            Events = events,
            TotalCount = 50,
            HasMore = true,
        };

        result.Events.ShouldBe(events);
        result.TotalCount.ShouldBe(50);
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public void EmptyResult_CanBeCreated()
    {
        var result = new EventQueryResult
        {
            Events = [],
            TotalCount = 0,
            HasMore = false,
        };

        result.Events.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.HasMore.ShouldBeFalse();
    }
}
