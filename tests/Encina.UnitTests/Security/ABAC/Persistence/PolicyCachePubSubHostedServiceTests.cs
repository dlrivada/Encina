using Encina.Caching;
using Encina.Diagnostics;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests for the invalidation handler of <see cref="PolicyCachePubSubHostedService"/>.
/// </summary>
public class PolicyCachePubSubHostedServiceTests
{
    private const string Sentinel = "sentinel-secret-message-1557";

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IPubSubProvider _pubSub = Substitute.For<IPubSubProvider>();
    private readonly FakeLogger<PolicyCachePubSubHostedService> _logger = new();
    private readonly PolicyCachingOptions _options = new();
    private readonly PolicyCachePubSubHostedService _sut;
    private Func<PolicyCacheInvalidationMessage, Task>? _handler;

    public PolicyCachePubSubHostedServiceTests()
    {
        _pubSub.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<Func<PolicyCacheInvalidationMessage, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _handler = call.ArgAt<Func<PolicyCacheInvalidationMessage, Task>>(1);
                return Task.FromResult(Substitute.For<IAsyncDisposable>());
            });
        _sut = new PolicyCachePubSubHostedService(_cache, _pubSub, _options, _logger);
    }

    [Fact]
    public async Task InvalidationMessage_EvictsAllPolicyCacheEntriesByPrefix()
    {
        await _sut.StartAsync(CancellationToken.None);
        _handler.ShouldNotBeNull();

        await _handler!(new PolicyCacheInvalidationMessage("Policy", null, "Save", DateTime.UnixEpoch));

        await _cache.Received(1).RemoveByPatternAsync(
            $"{_options.CacheKeyPrefix}:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidationMessage_EvictionFails_DoesNotThrowAndLogsRedactedException()
    {
        _cache.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException(Sentinel)));
        await _sut.StartAsync(CancellationToken.None);

        await _handler!(new PolicyCacheInvalidationMessage("PolicySet", "ps-1", "Delete", DateTime.UnixEpoch));

        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(Sentinel);
        entry.Message.ShouldNotContain(Sentinel);
    }
}
