using Encina.Messaging.Inbox;
using Shouldly;

namespace Encina.GuardTests.Messaging.Inbox;

/// <summary>
/// Guard tests for the purge setters of <see cref="InboxOptions"/> (#3165): an out-of-range value is rejected
/// with the property name, and the previous value is kept.
/// </summary>
public class InboxOptionsPurgeGuardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PurgeBatchSize_NonPositive_ThrowsArgumentOutOfRangeException(int value)
    {
        var options = new InboxOptions();

        var act = () => { options.PurgeBatchSize = value; };

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe(nameof(InboxOptions.PurgeBatchSize));
        options.PurgeBatchSize.ShouldBe(100);
    }

    [Fact]
    public void PurgeInterval_Zero_ThrowsArgumentOutOfRangeException()
    {
        var options = new InboxOptions();

        var act = () => { options.PurgeInterval = TimeSpan.Zero; };

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe(nameof(InboxOptions.PurgeInterval));
        options.PurgeInterval.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void PurgeInterval_Negative_ThrowsArgumentOutOfRangeException()
    {
        var options = new InboxOptions();

        var act = () => { options.PurgeInterval = TimeSpan.FromSeconds(-1); };

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void PurgeInterval_AboveTheTaskDelayMaximum_ThrowsArgumentOutOfRangeException()
    {
        var options = new InboxOptions();

        var act = () => { options.PurgeInterval = TimeSpan.FromMilliseconds(uint.MaxValue); };

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe(nameof(InboxOptions.PurgeInterval));
        options.PurgeInterval.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void MessageRetentionPeriod_Zero_ThrowsArgumentOutOfRangeException()
    {
        var options = new InboxOptions();

        var act = () => { options.MessageRetentionPeriod = TimeSpan.Zero; };

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe(nameof(InboxOptions.MessageRetentionPeriod));
        options.MessageRetentionPeriod.ShouldBe(TimeSpan.FromDays(30));
    }

    [Fact]
    public void PurgeSetters_PositiveValues_AreAccepted()
    {
        var options = new InboxOptions
        {
            PurgeBatchSize = 1,
            PurgeInterval = TimeSpan.FromTicks(1),
            MessageRetentionPeriod = TimeSpan.FromTicks(1)
        };

        options.PurgeBatchSize.ShouldBe(1);
        options.PurgeInterval.ShouldBe(TimeSpan.FromTicks(1));
        options.MessageRetentionPeriod.ShouldBe(TimeSpan.FromTicks(1));
    }

    [Fact]
    public void MessageRetentionPeriod_Negative_ThrowsArgumentOutOfRangeException()
    {
        var options = new InboxOptions();

        var act = () => { options.MessageRetentionPeriod = TimeSpan.FromDays(-1); };

        Should.Throw<ArgumentOutOfRangeException>(act);
    }
}
