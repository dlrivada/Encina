using Encina.Caching.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using RedisDistributedLockProvider = Encina.Caching.Redis.RedisDistributedLockProvider;
using RedisLockOptions = Encina.Caching.Redis.RedisLockOptions;

namespace Encina.GuardTests.Caching.Redis;

/// <summary>
/// Guard tests for the argument and cancellation guards of the public methods of the Redis lock provider.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RedisDistributedLockProviderGuardTests
{
    private static RedisDistributedLockProvider CreateProvider()
        => new(
            Substitute.For<IConnectionMultiplexer>(),
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);

    [Fact]
    public async Task AcquireAsync_NullResource_Throws()
        => await Should.ThrowAsync<ArgumentNullException>(() =>
            CreateProvider().AcquireAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));

    [Fact]
    public async Task IsLockedAsync_NullResource_Throws()
        => await Should.ThrowAsync<ArgumentNullException>(() =>
            CreateProvider().IsLockedAsync(null!, CancellationToken.None));

    [Fact]
    public async Task ExtendAsync_NullResource_Throws()
        => await Should.ThrowAsync<ArgumentNullException>(() =>
            CreateProvider().ExtendAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));

    [Fact]
    public async Task TryAcquireAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => CreateProvider().TryAcquireAsync(
            "resource", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            CreateProvider().AcquireAsync("resource", TimeSpan.FromSeconds(10), cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            CreateProvider().IsLockedAsync("resource", cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            CreateProvider().ExtendAsync("resource", TimeSpan.FromSeconds(10), cts.Token));
    }
}
