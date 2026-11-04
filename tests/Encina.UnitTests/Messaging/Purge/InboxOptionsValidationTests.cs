using Encina.Messaging.Inbox;

namespace Encina.UnitTests.Messaging.Purge;

/// <summary>
/// The validated purge setters of <see cref="InboxOptions"/> (#3165); defaults are unchanged.
/// </summary>
public sealed class InboxOptionsValidationTests
{
    /// <summary>The longest delay <c>Task.Delay</c> accepts.</summary>
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    [Fact]
    public void Defaults_AreUnchanged()
    {
        var options = new InboxOptions();

        options.MessageRetentionPeriod.ShouldBe(TimeSpan.FromDays(30));
        options.PurgeInterval.ShouldBe(TimeSpan.FromHours(24));
        options.PurgeBatchSize.ShouldBe(100);
        options.EnableAutomaticPurge.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void PurgeBatchSize_NonPositive_Throws(int value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { PurgeBatchSize = value })
            .ParamName.ShouldBe("PurgeBatchSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(int.MaxValue)]
    public void PurgeBatchSize_Positive_IsAcceptedWithNoCeiling(int value)
    {
        new InboxOptions { PurgeBatchSize = value }.PurgeBatchSize.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PurgeInterval_NonPositive_Throws(int seconds)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { PurgeInterval = TimeSpan.FromSeconds(seconds) })
            .ParamName.ShouldBe("PurgeInterval");
    }

    [Fact]
    public void PurgeInterval_Positive_IsAccepted()
    {
        new InboxOptions { PurgeInterval = TimeSpan.FromMilliseconds(1) }
            .PurgeInterval.ShouldBe(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void PurgeInterval_ExactlyTheTaskDelayMaximum_IsAccepted()
    {
        var options = new InboxOptions { PurgeInterval = MaxDelay };

        options.PurgeInterval.ShouldBe(MaxDelay);
    }

    [Fact]
    public async Task PurgeInterval_TheTaskDelayMaximum_IsAcceptedByTaskDelay()
    {
        var options = new InboxOptions { PurgeInterval = MaxDelay };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => Task.Delay(options.PurgeInterval, TimeProvider.System, cts.Token));
    }

    [Fact]
    public void PurgeInterval_OneTickAboveTheTaskDelayMaximum_Throws()
    {
        var options = new InboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.PurgeInterval = MaxDelay + TimeSpan.FromTicks(1))
            .ParamName.ShouldBe("PurgeInterval");
        options.PurgeInterval.ShouldBe(TimeSpan.FromHours(24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void MessageRetentionPeriod_NonPositive_Throws(int days)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InboxOptions { MessageRetentionPeriod = TimeSpan.FromDays(days) })
            .ParamName.ShouldBe("MessageRetentionPeriod");
    }

    [Fact]
    public void MessageRetentionPeriod_Positive_IsAccepted()
    {
        new InboxOptions { MessageRetentionPeriod = TimeSpan.FromDays(7) }
            .MessageRetentionPeriod.ShouldBe(TimeSpan.FromDays(7));
    }
}
