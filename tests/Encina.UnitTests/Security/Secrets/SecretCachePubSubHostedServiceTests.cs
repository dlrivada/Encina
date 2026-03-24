#pragma warning disable CA2012

using Encina.Caching;
using Encina.Security.Secrets.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretCachePubSubHostedServiceTests
{
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider _pubSub;
    private readonly SecretCachingOptions _options;
    private readonly ILogger<SecretCachePubSubHostedService> _logger;

    public SecretCachePubSubHostedServiceTests()
    {
        _cache = Substitute.For<ICacheProvider>();
        _pubSub = Substitute.For<IPubSubProvider>();
        _options = new SecretCachingOptions();
        _logger = NullLogger<SecretCachePubSubHostedService>.Instance;
    }

    [Fact]
    public async Task StartAsync_SubscribesToConfiguredChannel()
    {
        // Arrange
        _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                Arg.Any<string>(), Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IAsyncDisposable>(Substitute.For<IAsyncDisposable>()));
        var sut = CreateService();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        await _pubSub.Received(1).SubscribeAsync<SecretCacheInvalidationMessage>(
            _options.InvalidationChannel,
            Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_NullPubSub_DoesNotThrow()
    {
        // Arrange — no IPubSubProvider
        var sut = new SecretCachePubSubHostedService(_cache, null, _options, _logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert — should not throw, just log warning
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_SubscriptionFails_DoesNotThrow()
    {
        // Arrange
        _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                Arg.Any<string>(), Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("PubSub unavailable"));
        var sut = CreateService();

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_DisposesSubscription()
    {
        // Arrange
        var subscription = Substitute.For<IAsyncDisposable>();
        _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                Arg.Any<string>(), Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IAsyncDisposable>(subscription));
        var sut = CreateService();
        await sut.StartAsync(CancellationToken.None);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        await subscription.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_NoSubscription_DoesNotThrow()
    {
        // Arrange — never started
        var sut = CreateService();

        // Act
        var act = () => sut.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(null!, _pubSub, _options, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, null, _options, _logger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_EmptyInvalidationChannel_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { InvalidationChannel = "" };
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, options, _logger);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyCacheKeyPrefix_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { CacheKeyPrefix = "" };
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, options, _logger);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    private SecretCachePubSubHostedService CreateService() =>
        new(_cache, _pubSub, _options, _logger);
}
