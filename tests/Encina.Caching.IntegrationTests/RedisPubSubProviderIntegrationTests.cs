using Encina.Caching.IntegrationTests.Fixtures;

namespace Encina.Caching.IntegrationTests;

/// <summary>
/// Integration tests for RedisPubSubProvider using a real Redis container.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public class RedisPubSubProviderIntegrationTests : IAsyncLifetime
{
    private readonly RedisFixture _fixture;
    private RedisPubSubProvider? _provider;

    public RedisPubSubProviderIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _provider = new RedisPubSubProvider(
            _fixture.Connection!,
            NullLogger<RedisPubSubProvider>.Instance);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task SubscribeAndPublish_MessageReceived()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var channel = $"test-channel-{Guid.NewGuid():N}";
        var expectedMessage = "test-message";
        var receivedMessage = string.Empty;
        var messageReceived = new TaskCompletionSource<bool>();

        // Act
        await _provider!.SubscribeAsync(channel, msg =>
        {
            receivedMessage = msg;
            messageReceived.SetResult(true);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Small delay to ensure subscription is active
        await Task.Delay(100);

        await _provider.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Wait for message with timeout
        var received = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == messageReceived.Task;

        // Cleanup
        await _provider.UnsubscribeAsync(channel, CancellationToken.None);

        // Assert
        received.ShouldBeTrue("Message should be received within timeout");
        receivedMessage.ShouldBe(expectedMessage);
    }

    [SkippableFact]
    public async Task MultipleSubscribers_AllReceiveMessage()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var channel = $"multi-sub-{Guid.NewGuid():N}";
        var expectedMessage = "broadcast-message";
        var receivedCount = 0;
        var allReceived = new TaskCompletionSource<bool>();
        var expectedSubscribers = 3;

        // Create multiple providers (each with its own subscriber)
        var providers = new List<RedisPubSubProvider>();
        for (var i = 0; i < expectedSubscribers; i++)
        {
            var provider = new RedisPubSubProvider(
                _fixture.Connection!,
                NullLogger<RedisPubSubProvider>.Instance);
            providers.Add(provider);

            await provider.SubscribeAsync(channel, _ =>
            {
                if (Interlocked.Increment(ref receivedCount) == expectedSubscribers)
                {
                    allReceived.TrySetResult(true);
                }
                return Task.CompletedTask;
            }, CancellationToken.None);
        }

        // Small delay to ensure all subscriptions are active
        await Task.Delay(200);

        // Act
        await _provider!.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Wait for all messages with timeout
        var received = await Task.WhenAny(
            allReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))) == allReceived.Task;

        // Cleanup
        foreach (var provider in providers)
        {
            await provider.UnsubscribeAsync(channel, CancellationToken.None);
        }

        // Assert
        received.ShouldBeTrue("All subscribers should receive the message");
        receivedCount.ShouldBe(expectedSubscribers);
    }

    [SkippableFact]
    public async Task Unsubscribe_NoMoreMessagesReceived()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var channel = $"unsub-test-{Guid.NewGuid():N}";
        var receiveCount = 0;
        var firstReceived = new TaskCompletionSource<bool>();

        await _provider!.SubscribeAsync(channel, _ =>
        {
            Interlocked.Increment(ref receiveCount);
            firstReceived.TrySetResult(true);
            return Task.CompletedTask;
        }, CancellationToken.None);

        await Task.Delay(100);

        // Act - Send first message
        await _provider.PublishAsync(channel, "first-message", CancellationToken.None);
        await Task.WhenAny(firstReceived.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        var countAfterFirst = receiveCount;

        // Unsubscribe
        await _provider.UnsubscribeAsync(channel, CancellationToken.None);

        await Task.Delay(100);

        // Send second message (should not be received)
        await _provider.PublishAsync(channel, "second-message", CancellationToken.None);

        // Wait a bit to see if any message arrives
        await Task.Delay(500);

        // Assert
        countAfterFirst.ShouldBe(1);
        receiveCount.ShouldBe(1, "Should not receive messages after unsubscribe");
    }

    [SkippableFact]
    public async Task DifferentChannels_Independent()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var channel1 = $"channel-1-{Guid.NewGuid():N}";
        var channel2 = $"channel-2-{Guid.NewGuid():N}";
        var channel1Received = false;
        var channel2Received = false;
        var channel1Task = new TaskCompletionSource<bool>();

        await _provider!.SubscribeAsync(channel1, _ =>
        {
            channel1Received = true;
            channel1Task.TrySetResult(true);
            return Task.CompletedTask;
        }, CancellationToken.None);

        await _provider.SubscribeAsync(channel2, _ =>
        {
            channel2Received = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await Task.Delay(100);

        // Act - Only publish to channel1
        await _provider.PublishAsync(channel1, "message", CancellationToken.None);

        await Task.WhenAny(channel1Task.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        // Wait a bit to see if channel2 gets anything
        await Task.Delay(300);

        // Cleanup
        await _provider.UnsubscribeAsync(channel1, CancellationToken.None);
        await _provider.UnsubscribeAsync(channel2, CancellationToken.None);

        // Assert
        channel1Received.ShouldBeTrue();
        channel2Received.ShouldBeFalse();
    }
}
