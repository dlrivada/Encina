using Encina.MongoDB.Inbox;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.PropertyTests.MongoDB;

/// <summary>
/// Property-based tests for <see cref="InboxMessage"/>.
/// </summary>
[Trait("Category", "Property")]
public sealed class InboxMessagePropertyTests
{
    [Property(MaxTest = 50)]
    public bool IsProcessed_RequiresBothProcessedAtAndResponse(DateTime? processedAt, bool hasResponse)
    {
        var msg = new InboxMessage
        {
            ProcessedAtUtc = processedAt,
            Response = hasResponse ? "{}" : null
        };

        var expected = processedAt.HasValue && hasResponse;
        return msg.IsProcessed == expected;
    }

    [Property(MaxTest = 30)]
    public bool IsExpired_PastDate_AlwaysTrue(PositiveInt daysAgo)
    {
        var msg = new InboxMessage
        {
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-daysAgo.Get)
        };
        return msg.IsExpired();
    }

    [Property(MaxTest = 30)]
    public bool IsExpired_FutureDate_AlwaysFalse(PositiveInt daysAhead)
    {
        var msg = new InboxMessage
        {
            ExpiresAtUtc = DateTime.UtcNow.AddDays(daysAhead.Get)
        };
        return !msg.IsExpired();
    }
}
