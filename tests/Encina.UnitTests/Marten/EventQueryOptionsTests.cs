using Encina.Marten;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="EventQueryOptions"/>.
/// </summary>
public sealed class EventQueryOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EventQueryOptions();

        options.Skip.ShouldBe(0);
        options.Take.ShouldBe(100);
        options.StreamId.ShouldBeNull();
        options.EventTypes.ShouldBeNull();
        options.FromTimestamp.ShouldBeNull();
        options.ToTimestamp.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var streamId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var eventTypes = new[] { "OrderCreated", "OrderShipped" };

        var options = new EventQueryOptions
        {
            Skip = 10,
            Take = 50,
            StreamId = streamId,
            EventTypes = eventTypes,
            FromTimestamp = from,
            ToTimestamp = to,
        };

        options.Skip.ShouldBe(10);
        options.Take.ShouldBe(50);
        options.StreamId.ShouldBe(streamId);
        options.EventTypes.ShouldBe(eventTypes);
        options.FromTimestamp.ShouldBe(from);
        options.ToTimestamp.ShouldBe(to);
    }
}
