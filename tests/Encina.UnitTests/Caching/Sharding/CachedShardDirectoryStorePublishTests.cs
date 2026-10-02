using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Diagnostics;
using Encina.Sharding.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for the distributed invalidation publishing of <see cref="CachedShardDirectoryStore"/>.
/// The publish runs fire-and-forget, so each test waits on a signal raised by the pub/sub substitute
/// or the logger.
/// </summary>
public sealed class CachedShardDirectoryStorePublishTests
{
    private const string SentinelMessage = "sentinel-secret-message";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);

    private readonly IShardDirectoryStore _inner = Substitute.For<IShardDirectoryStore>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IPubSubProvider _pubSub = Substitute.For<IPubSubProvider>();

    [Fact]
    public async Task AddMapping_WithDistributedInvalidation_PublishesAnUpdateMessage()
    {
        // Arrange
        var published = new TaskCompletionSource<DirectoryCacheInvalidationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<DirectoryCacheInvalidationMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                published.TrySetResult(call.Arg<DirectoryCacheInvalidationMessage>());
                return Task.CompletedTask;
            });
        var store = CreateStore(new FakeLogger<CachedShardDirectoryStore>());

        // Act
        store.AddMapping("key-1", "shard-1");
        var message = await published.Task.WaitAsync(SafetyTimeout);

        // Assert
        message.Key.ShouldBe("key-1");
        message.ShardId.ShouldBe("shard-1");
        message.IsRemoval.ShouldBeFalse();
        await _pubSub.Received(1).PublishAsync("shard:directory:invalidate", Arg.Any<DirectoryCacheInvalidationMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task RemoveMapping_WithDistributedInvalidation_PublishesARemovalMessage()
    {
        // Arrange
        var published = new TaskCompletionSource<DirectoryCacheInvalidationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<DirectoryCacheInvalidationMessage>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                published.TrySetResult(call.Arg<DirectoryCacheInvalidationMessage>());
                return Task.CompletedTask;
            });
        var store = CreateStore(new FakeLogger<CachedShardDirectoryStore>());

        // Act
        store.RemoveMapping("key-1");
        var message = await published.Task.WaitAsync(SafetyTimeout);

        // Assert
        message.Key.ShouldBe("key-1");
        message.ShardId.ShouldBeNull();
        message.IsRemoval.ShouldBeTrue();
    }

    [Fact]
    public async Task AddMapping_WhenPublishingFails_LogsARedactedExceptionAndDoesNotThrow()
    {
        // Arrange
        var logged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = FakeLogCollector.Create(new FakeLogCollectorOptions { OutputSink = _ => logged.TrySetResult() });
        var logger = new FakeLogger<CachedShardDirectoryStore>(collector);
        _pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<DirectoryCacheInvalidationMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException(SentinelMessage));
        var store = CreateStore(logger);

        // Act
        Should.NotThrow(() => store.AddMapping("key-1", "shard-1"));
        await logged.Task.WaitAsync(SafetyTimeout);

        // Assert
        var entry = collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    [Fact]
    public async Task AddMapping_WithDistributedInvalidationDisabled_DoesNotPublish()
    {
        // Arrange
        var store = CreateStore(new FakeLogger<CachedShardDirectoryStore>(), enableDistributedInvalidation: false);

        // Act
        store.AddMapping("key-1", "shard-1");

        // Assert
        await _pubSub.DidNotReceiveWithAnyArgs().PublishAsync(default!, default(DirectoryCacheInvalidationMessage)!, default);
    }

    private CachedShardDirectoryStore CreateStore(ILogger<CachedShardDirectoryStore> logger, bool enableDistributedInvalidation = true)
    {
        var options = new DirectoryCacheOptions
        {
            EnableDistributedInvalidation = enableDistributedInvalidation,
            InvalidationChannel = "shard:directory:invalidate"
        };

        return new CachedShardDirectoryStore(_inner, _cache, Options.Create(options), logger, _pubSub);
    }
}
