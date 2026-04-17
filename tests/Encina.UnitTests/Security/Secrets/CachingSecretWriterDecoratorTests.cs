#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class CachingSecretWriterDecoratorTests
{
    private readonly ISecretWriter _innerWriter;
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider _pubSub;
    private readonly SecretCachingOptions _options;
    private readonly ILogger<CachingSecretWriterDecorator> _logger;

    public CachingSecretWriterDecoratorTests()
    {
        _innerWriter = Substitute.For<ISecretWriter>();
        _cache = Substitute.For<ICacheProvider>();
        _pubSub = Substitute.For<IPubSubProvider>();
        _options = new SecretCachingOptions();
        _logger = Substitute.For<ILogger<CachingSecretWriterDecorator>>();
    }

    #region SetSecretAsync - Success Path

    [Fact]
    public async Task SetSecretAsync_Success_InvalidatesCacheAndPublishes()
    {
        // Arrange
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var decorator = new CachingSecretWriterDecorator(
            _innerWriter, _cache, _pubSub, _options, _logger);

        // Act
        var result = await decorator.SetSecretAsync("key", "value");

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify all cache key variants are invalidated
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":v:key")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":lkg:key")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            Arg.Is<string>(p => p.Contains(":t:key:")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            Arg.Is<string>(p => p.Contains(":lkg:t:key:")),
            Arg.Any<CancellationToken>());

        // Verify PubSub message has correct fields
        await _pubSub.Received(1).PublishAsync(
            _options.InvalidationChannel,
            Arg.Is<SecretCacheInvalidationMessage>(m =>
                m.SecretName == "key" &&
                m.Operation == "Set" &&
                m.InvalidatedAtUtc > DateTime.UtcNow.AddMinutes(-1)),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - Inner Failure

    [Fact]
    public async Task SetSecretAsync_InnerFails_DoesNotInvalidateOrPublish()
    {
        // Arrange
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(
                SecretsErrors.ProviderUnavailable("test")));
        var decorator = new CachingSecretWriterDecorator(
            _innerWriter, _cache, _pubSub, _options, _logger);

        // Act
        var result = await decorator.SetSecretAsync("key", "value");

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _cache.DidNotReceive().RemoveAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _pubSub.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<SecretCacheInvalidationMessage>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - No PubSub

    [Fact]
    public async Task SetSecretAsync_NoPubSub_InvalidatesLocalCacheOnly()
    {
        // Arrange
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var decorator = new CachingSecretWriterDecorator(
            _innerWriter, _cache, null, _options, _logger);

        // Act
        var result = await decorator.SetSecretAsync("key", "value");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":v:key")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - PubSub Failure

    [Fact]
    public async Task SetSecretAsync_PubSubFails_StillReturnsSuccess()
    {
        // Arrange
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        _pubSub.PublishAsync(
            Arg.Any<string>(), Arg.Any<SecretCacheInvalidationMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("PubSub down"));
        var decorator = new CachingSecretWriterDecorator(
            _innerWriter, _cache, _pubSub, _options, _logger);

        // Act
        var result = await decorator.SetSecretAsync("key", "value");

        // Assert — inner write succeeded, pubsub failure is swallowed
        result.IsRight.ShouldBeTrue();

        // Verify local cache invalidation still happened despite PubSub failure
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":v:key")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":lkg:key")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - PubSub Disabled

    [Fact]
    public async Task SetSecretAsync_PubSubDisabled_DoesNotPublish()
    {
        // Arrange
        _options.EnablePubSubInvalidation = false;
        _innerWriter.SetSecretAsync("key", "value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var decorator = new CachingSecretWriterDecorator(
            _innerWriter, _cache, _pubSub, _options, _logger);

        // Act
        var result = await decorator.SetSecretAsync("key", "value");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _pubSub.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<SecretCacheInvalidationMessage>(), Arg.Any<CancellationToken>());

        // Verify local cache invalidation still happened even though PubSub is disabled
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":v:key")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":lkg:key")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(null!, _cache, _pubSub, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(_innerWriter, null!, _pubSub, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(_innerWriter, _cache, _pubSub, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretWriterDecorator(_innerWriter, _cache, _pubSub, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        var act = () => new CachingSecretWriterDecorator(_innerWriter, _cache, null, _options, _logger);

        Should.NotThrow(act);
    }

    #endregion
}
