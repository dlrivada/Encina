using Encina.DistributedLock.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

public class RedisDistributedLockProviderExtendedTests
{
    private readonly IConnectionMultiplexer _connection = Substitute.For<IConnectionMultiplexer>();
    private readonly IOptions<RedisLockOptions> _options = Options.Create(new RedisLockOptions());

    [Fact]
    public void Constructor_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(null!, _options, NullLogger<RedisDistributedLockProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connection, null!, NullLogger<RedisDistributedLockProvider>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connection, _options, null!));
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystem()
    {
        // Should not throw — null timeProvider uses TimeProvider.System
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance, null);
        provider.ShouldNotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_NullResource_Throws()
    {
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.TryAcquireAsync(null!, TimeSpan.FromSeconds(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_NullResource_Throws()
    {
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.AcquireAsync(null!, TimeSpan.FromSeconds(5), CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_NullResource_Throws()
    {
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_NullResource_Throws()
    {
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.ExtendAsync(null!, TimeSpan.FromSeconds(5), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_CancelledToken_ThrowsOperationCancelled()
    {
        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.TryAcquireAsync("res", TimeSpan.FromSeconds(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_Success_ReturnsHandle()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        var handle = await provider.TryAcquireAsync("my-resource", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);

        handle.ShouldNotBeNull();
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_Failure_ReturnsNull()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(false);
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        // Zero wait time so it fails immediately
        var handle = await provider.TryAcquireAsync("my-resource", TimeSpan.FromSeconds(30), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        handle.ShouldBeNull();
    }

    [Fact]
    public async Task IsLockedAsync_KeyExists_ReturnsTrue()
    {
        var db = Substitute.For<IDatabase>();
        db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        var locked = await provider.IsLockedAsync("my-resource", CancellationToken.None);
        locked.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_NoKey_ReturnsFalse()
    {
        var db = Substitute.For<IDatabase>();
        db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        var provider = new RedisDistributedLockProvider(
            _connection, _options, NullLogger<RedisDistributedLockProvider>.Instance);

        var locked = await provider.IsLockedAsync("my-resource", CancellationToken.None);
        locked.ShouldBeFalse();
    }
}
