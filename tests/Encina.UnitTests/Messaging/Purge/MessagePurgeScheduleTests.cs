using Encina.Messaging.Inbox;
using Encina.Messaging.Purge;

namespace Encina.UnitTests.Messaging.Purge;

/// <summary>
/// <see cref="MessagePurgeSchedule"/> (#3013): built from the inbox options, an off pattern has no entry.
/// </summary>
public sealed class MessagePurgeScheduleTests
{
    [Fact]
    public void Create_WithAutomaticPurgeOn_TakesTheIntervalAndBatchSizeOfTheOptions()
    {
        var options = new InboxOptions { PurgeInterval = TimeSpan.FromHours(3), PurgeBatchSize = 25 };

        var schedule = MessagePurgeSchedule.Create(options);

        schedule.InboxPurge.ShouldBe(new PurgeEntry(TimeSpan.FromHours(3), 25));
    }

    [Fact]
    public void Create_WithAutomaticPurgeOff_HasNoEntry()
    {
        var schedule = MessagePurgeSchedule.Create(new InboxOptions { EnableAutomaticPurge = false });

        schedule.InboxPurge.ShouldBeNull();
    }

    [Fact]
    public void Create_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => MessagePurgeSchedule.Create(null!))
            .ParamName.ShouldBe("inboxOptions");
    }
}
