using Encina.DistributedLock.Redis;
using Encina.DistributedLock.Redis.Health;
using ProviderHealthCheckOptions = Encina.Messaging.Health.ProviderHealthCheckOptions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

namespace Encina.GuardTests.Infrastructure.DistributedLock;

/// <summary>
/// Additional guard tests for Encina.DistributedLock.Redis covering
/// ServiceCollectionExtensions, HealthCheck, and provider method guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RedisDistributedLockAdditionalGuardTests
{
    private static readonly IConnectionMultiplexer Mux = Substitute.For<IConnectionMultiplexer>();

    // ─── ServiceCollectionExtensions overload 1: (services, connectionString) ───

    [Fact]
    public void AddEncinaDistributedLockRedis_String_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis("localhost:6379"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddEncinaDistributedLockRedis_String_InvalidConnectionString_Throws(string? cs)
    {
        Should.Throw<ArgumentException>(() =>
            new ServiceCollection().AddEncinaDistributedLockRedis(cs!));
    }

    // ─── ServiceCollectionExtensions overload 2: (services, connectionString, configure) ───

    [Fact]
    public void AddEncinaDistributedLockRedis_StringWithConfigure_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis("localhost:6379", _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_StringWithConfigure_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDistributedLockRedis("localhost:6379", null!));
    }

    // ─── ServiceCollectionExtensions overload 3: (services, multiplexer) ───

    [Fact]
    public void AddEncinaDistributedLockRedis_Multiplexer_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis(Mux));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_Multiplexer_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDistributedLockRedis((IConnectionMultiplexer)null!));
    }

    // ─── ServiceCollectionExtensions overload 4: (services, multiplexer, configure) ───

    [Fact]
    public void AddEncinaDistributedLockRedis_MultiplexerWithConfigure_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis(Mux, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_MultiplexerWithConfigure_NullMultiplexer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDistributedLockRedis((IConnectionMultiplexer)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_MultiplexerWithConfigure_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ServiceCollection().AddEncinaDistributedLockRedis(Mux, null!));
    }

    // ─── RedisDistributedLockHealthCheck constructor guards ───

    [Fact]
    public void HealthCheck_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(null!, new ProviderHealthCheckOptions()));
    }

    [Fact]
    public void HealthCheck_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(Mux, (ProviderHealthCheckOptions)null!));
    }

    [Fact]
    public void HealthCheck_ValidArgs_Constructs()
    {
        var sut = new RedisDistributedLockHealthCheck(Mux, new ProviderHealthCheckOptions());
        sut.ShouldNotBeNull();
    }

    // ─── RedisDistributedLockProvider method guards ───

    [Fact]
    public async Task TryAcquireAsync_NullResource_Throws()
    {
        var sut = new RedisDistributedLockProvider(Mux,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.TryAcquireAsync(null!, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_NullResource_Throws()
    {
        var sut = new RedisDistributedLockProvider(Mux,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.AcquireAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_NullResource_Throws()
    {
        var sut = new RedisDistributedLockProvider(Mux,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_NullResource_Throws()
    {
        var sut = new RedisDistributedLockProvider(Mux,
            Options.Create(new RedisLockOptions()),
            NullLogger<RedisDistributedLockProvider>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ExtendAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));
    }

    // ─── RedisLockOptions defaults ───

    [Fact]
    public void RedisLockOptions_Defaults()
    {
        var options = new RedisLockOptions();
        options.ShouldNotBeNull();
    }
}
