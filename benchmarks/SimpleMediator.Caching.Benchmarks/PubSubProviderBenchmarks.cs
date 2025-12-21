using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleMediator.Caching.Memory;

namespace SimpleMediator.Caching.Benchmarks;

/// <summary>
/// Benchmarks for MemoryPubSubProvider operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PubSubProviderBenchmarks
{
    private MemoryPubSubProvider _provider = null!;
    private string _subscribedChannel = null!;
    private int _messageCounter;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
        _subscribedChannel = "benchmark-channel";
        _messageCounter = 0;

        // Set up a subscriber
        _provider.SubscribeAsync(_subscribedChannel, _ =>
        {
            Interlocked.Increment(ref _messageCounter);
            return Task.CompletedTask;
        }, CancellationToken.None).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _provider.UnsubscribeAsync(_subscribedChannel, CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public async Task PublishAsync_SingleSubscriber()
    {
        await _provider.PublishAsync(_subscribedChannel, "test-message", CancellationToken.None);
    }

    [Benchmark]
    public async Task PublishAsync_NoSubscriber()
    {
        await _provider.PublishAsync($"no-sub-{Guid.NewGuid():N}", "test-message", CancellationToken.None);
    }

    [Benchmark]
    public async Task SubscribeAndUnsubscribe()
    {
        var channel = $"temp-{Guid.NewGuid():N}";
        await _provider.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);
        await _provider.UnsubscribeAsync(channel, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task PublishBurst(int messageCount)
    {
        var tasks = new Task[messageCount];
        for (var i = 0; i < messageCount; i++)
        {
            tasks[i] = _provider.PublishAsync(_subscribedChannel, $"message-{i}", CancellationToken.None);
        }

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public async Task MultipleSubscribers(int subscriberCount)
    {
        var channel = $"multi-{Guid.NewGuid():N}";
        var counter = 0;

        // Add subscribers
        for (var i = 0; i < subscriberCount; i++)
        {
            await _provider.SubscribeAsync(channel, _ =>
            {
                Interlocked.Increment(ref counter);
                return Task.CompletedTask;
            }, CancellationToken.None);
        }

        // Publish message
        await _provider.PublishAsync(channel, "broadcast", CancellationToken.None);

        // Allow delivery
        await Task.Delay(10);

        // Cleanup
        await _provider.UnsubscribeAsync(channel, CancellationToken.None);
    }
}
