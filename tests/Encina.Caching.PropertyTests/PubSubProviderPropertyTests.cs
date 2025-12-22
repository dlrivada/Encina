using FsCheck;
using FsCheck.Xunit;

#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests - Required for timing-based assertions in pub/sub tests

namespace Encina.Caching.PropertyTests;

/// <summary>
/// Property-based tests for IPubSubProvider that verify invariants hold for all inputs.
/// </summary>
public sealed class PubSubProviderPropertyTests : IAsyncLifetime
{
    private readonly MemoryPubSubProvider _provider;

    public PubSubProviderPropertyTests()
    {
        _provider = new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region Subscribe/Publish Invariants

    [Property(MaxTest = 50)]
    public bool Subscriber_ReceivesPublishedMessage(PositiveInt channelSeed, NonEmptyString message)
    {
        var channel = $"pubsub-test-{Guid.NewGuid():N}-{channelSeed.Get}";
        var receivedMessage = string.Empty;
        var messageReceived = new ManualResetEventSlim(false);

        _provider.SubscribeAsync(channel, msg =>
        {
            receivedMessage = msg;
            messageReceived.Set();
            return Task.CompletedTask;
        }, CancellationToken.None).GetAwaiter().GetResult();

        _provider.PublishAsync(channel, message.Get, CancellationToken.None).GetAwaiter().GetResult();

        var received = messageReceived.Wait(TimeSpan.FromSeconds(1));

        _provider.UnsubscribeAsync(channel, CancellationToken.None).GetAwaiter().GetResult();

        return received && receivedMessage == message.Get;
    }

    [Property(MaxTest = 50)]
    public bool MultipleSubscribers_AllReceiveMessage(PositiveInt channelSeed, PositiveInt subscriberCount, NonEmptyString message)
    {
        var channel = $"multi-sub-{Guid.NewGuid():N}-{channelSeed.Get}";
        var actualSubscriberCount = Math.Min(subscriberCount.Get % 5 + 1, 5);
        var receiveCounts = new int[actualSubscriberCount];
        var countdowns = new CountdownEvent(actualSubscriberCount);

        for (var i = 0; i < actualSubscriberCount; i++)
        {
            var index = i;
            _provider.SubscribeAsync(channel, msg =>
            {
                Interlocked.Increment(ref receiveCounts[index]);
                countdowns.Signal();
                return Task.CompletedTask;
            }, CancellationToken.None).GetAwaiter().GetResult();
        }

        _provider.PublishAsync(channel, message.Get, CancellationToken.None).GetAwaiter().GetResult();

        var allReceived = countdowns.Wait(TimeSpan.FromSeconds(2));

        _provider.UnsubscribeAsync(channel, CancellationToken.None).GetAwaiter().GetResult();

        return allReceived && receiveCounts.All(c => c == 1);
    }

    [Property(MaxTest = 50)]
    public bool DifferentChannels_AreIndependent(PositiveInt _, PositiveInt __, NonEmptyString message)
    {
        var channel1 = $"channel-a-{Guid.NewGuid():N}";
        var channel2 = $"channel-b-{Guid.NewGuid():N}";
        var channel1Received = false;
        var channel2Received = false;
        var event1 = new ManualResetEventSlim(false);

        _provider.SubscribeAsync(channel1, msg =>
        {
            channel1Received = true;
            event1.Set();
            return Task.CompletedTask;
        }, CancellationToken.None).GetAwaiter().GetResult();

        _provider.SubscribeAsync(channel2, msg =>
        {
            channel2Received = true;
            return Task.CompletedTask;
        }, CancellationToken.None).GetAwaiter().GetResult();

        // Only publish to channel1
        _provider.PublishAsync(channel1, message.Get, CancellationToken.None).GetAwaiter().GetResult();

        event1.Wait(TimeSpan.FromSeconds(1));

        // Allow some time for any erroneous delivery
        Thread.Sleep(50);

        _provider.UnsubscribeAsync(channel1, CancellationToken.None).GetAwaiter().GetResult();
        _provider.UnsubscribeAsync(channel2, CancellationToken.None).GetAwaiter().GetResult();

        return channel1Received && !channel2Received;
    }

    #endregion

    #region Unsubscribe Invariants

    [Property(MaxTest = 50)]
    public bool AfterUnsubscribe_NoMoreMessagesReceived(PositiveInt channelSeed, NonEmptyString message1, NonEmptyString message2)
    {
        var channel = $"unsub-test-{Guid.NewGuid():N}-{channelSeed.Get}";
        var receiveCount = 0;
        var firstReceived = new ManualResetEventSlim(false);

        _provider.SubscribeAsync(channel, msg =>
        {
            Interlocked.Increment(ref receiveCount);
            firstReceived.Set();
            return Task.CompletedTask;
        }, CancellationToken.None).GetAwaiter().GetResult();

        // First message should be received
        _provider.PublishAsync(channel, message1.Get, CancellationToken.None).GetAwaiter().GetResult();
        firstReceived.Wait(TimeSpan.FromSeconds(1));

        // Unsubscribe
        _provider.UnsubscribeAsync(channel, CancellationToken.None).GetAwaiter().GetResult();

        var countAfterUnsubscribe = receiveCount;

        // Second message should not be received
        _provider.PublishAsync(channel, message2.Get, CancellationToken.None).GetAwaiter().GetResult();

        // Allow time for erroneous delivery
        Thread.Sleep(50);

        return receiveCount == countAfterUnsubscribe && receiveCount == 1;
    }

    #endregion

    #region Publish Without Subscribers

    [Property(MaxTest = 50)]
    public bool Publish_WithoutSubscribers_DoesNotThrow(PositiveInt channelSeed, NonEmptyString message)
    {
        var channel = $"no-sub-{Guid.NewGuid():N}-{channelSeed.Get}";

        try
        {
            _provider.PublishAsync(channel, message.Get, CancellationToken.None).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
