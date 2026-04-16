#pragma warning disable CA2012

using Encina.Caching;
using Encina.Security.Secrets.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

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
        await Should.NotThrowAsync(act);
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
        await Should.NotThrowAsync(act);
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
        await Should.NotThrowAsync(act);
    }

    #region Handler Callback Invocation

    [Fact]
    public async Task HandleMessage_PerSecretInvalidation_RemovesCorrectKeys()
    {
        // Arrange
        Func<SecretCacheInvalidationMessage, Task>? capturedHandler = null;
        _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                Arg.Any<string>(), Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedHandler = callInfo.Arg<Func<SecretCacheInvalidationMessage, Task>>();
                return Task.FromResult<IAsyncDisposable>(Substitute.For<IAsyncDisposable>());
            });
        var sut = CreateService();
        await sut.StartAsync(CancellationToken.None);

        // Act
        capturedHandler.ShouldNotBeNull();
        var message = new SecretCacheInvalidationMessage("my-secret", "Set", DateTime.UtcNow);
        await capturedHandler!(message);

        // Assert — explicit key removal
        await _cache.Received().RemoveAsync(
            $"{_options.CacheKeyPrefix}:v:my-secret", Arg.Any<CancellationToken>());
        await _cache.Received().RemoveAsync(
            $"{_options.CacheKeyPrefix}:lkg:my-secret", Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            $"{_options.CacheKeyPrefix}:t:my-secret:*", Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            $"{_options.CacheKeyPrefix}:lkg:t:my-secret:*", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessage_BulkInvalidation_RemovesAllSecrets()
    {
        // Arrange
        Func<SecretCacheInvalidationMessage, Task>? capturedHandler = null;
        _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                Arg.Any<string>(), Arg.Any<Func<SecretCacheInvalidationMessage, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedHandler = callInfo.Arg<Func<SecretCacheInvalidationMessage, Task>>();
                return Task.FromResult<IAsyncDisposable>(Substitute.For<IAsyncDisposable>());
            });
        var sut = CreateService();
        await sut.StartAsync(CancellationToken.None);

        // Act
        var message = new SecretCacheInvalidationMessage("any", "BulkInvalidate", DateTime.UtcNow);
        await capturedHandler!(message);

        // Assert — bulk pattern removal
        await _cache.Received().RemoveByPatternAsync(
            $"{_options.CacheKeyPrefix}:*", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(null!, _pubSub, _options, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        var act = () => new SecretCachePubSubHostedService(_cache, null, _options, _logger);
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_EmptyInvalidationChannel_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { InvalidationChannel = "" };
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, options, _logger);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyCacheKeyPrefix_ThrowsArgumentException()
    {
        var options = new SecretCachingOptions { CacheKeyPrefix = "" };
        var act = () => new SecretCachePubSubHostedService(_cache, _pubSub, options, _logger);
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    private SecretCachePubSubHostedService CreateService() =>
        new(_cache, _pubSub, _options, _logger);
}
