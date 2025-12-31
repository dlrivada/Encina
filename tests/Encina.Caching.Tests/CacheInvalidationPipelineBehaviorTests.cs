using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Caching.Tests;

/// <summary>
/// Unit tests for <see cref="CacheInvalidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class CacheInvalidationPipelineBehaviorTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IPubSubProvider _pubSubProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly CachingOptions _options;
    private readonly ILogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, string>> _logger;

    public CacheInvalidationPipelineBehaviorTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _pubSubProvider = Substitute.For<IPubSubProvider>();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _options = new CachingOptions
        {
            EnableCacheInvalidation = true,
            EnablePubSubInvalidation = true,
            InvalidationChannel = "cache:invalidate"
        };
        _logger = NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, string>>.Instance;
    }

    [Fact]
    public void Constructor_WithNullCacheProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            null!,
            _keyGenerator,
            Options.Create(_options),
            _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cacheProvider");
    }

    [Fact]
    public void Constructor_WithNullKeyGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            _cacheProvider,
            null!,
            Options.Create(_options),
            _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyGenerator");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            null!,
            _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(_options),
            null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateBehavior();
        var context = CreateContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("request", () =>
            sut.Handle(
                null!,
                context,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenInvalidationDisabled_DoesNotInvalidate()
    {
        // Arrange
        _options.EnableCacheInvalidation = false;
        var sut = CreateBehavior();
        var request = new InvalidatingCommand(Guid.NewGuid());
        var context = CreateContext();

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert
        await _cacheProvider.DidNotReceive().RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_InvalidatesCache()
    {
        // Arrange
        var sut = CreateBehavior();
        var productId = Guid.NewGuid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();
        var expectedPattern = $"product:{productId}:*";

        _keyGenerator.GeneratePatternFromTemplate("product:{ProductId}:*", request, context)
            .Returns(expectedPattern);

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert
        await _cacheProvider.Received(1).RemoveByPatternAsync(expectedPattern, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnFailure_DoesNotInvalidateCache()
    {
        // Arrange
        var sut = CreateBehavior();
        var request = new InvalidatingCommand(Guid.NewGuid());
        var context = CreateContext();
        var error = EncinaError.New("Test error");

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Left<EncinaError, string>(error)),
            CancellationToken.None);

        // Assert
        await _cacheProvider.DidNotReceive().RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBroadcast_PublishesInvalidation()
    {
        // Arrange
        _options.EnablePubSubInvalidation = true;
        var sut = CreateBehaviorWithPubSub();
        var productId = Guid.NewGuid();
        var request = new InvalidatingCommandWithBroadcast(productId);
        var context = CreateContext();
        var expectedPattern = $"product:{productId}:*";

        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        keyGenerator.GeneratePatternFromTemplate("product:{ProductId}:*", request, context)
            .Returns(expectedPattern);

        var behavior = new CacheInvalidationPipelineBehavior<InvalidatingCommandWithBroadcast, string>(
            _cacheProvider,
            keyGenerator,
            Options.Create(_options),
            NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommandWithBroadcast, string>>.Instance,
            _pubSubProvider);

        // Act
        await behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert
        await _pubSubProvider.Received(1).PublishAsync(
            _options.InvalidationChannel,
            expectedPattern,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheThrows_ContinuesWithoutThrowing()
    {
        // Arrange
        _options.ThrowOnCacheErrors = false;
        var sut = CreateBehavior();
        var request = new InvalidatingCommand(Guid.NewGuid());
        var context = CreateContext();

        _keyGenerator.GeneratePatternFromTemplate(Arg.Any<string>(), Arg.Any<InvalidatingCommand>(), Arg.Any<IRequestContext>())
            .Returns("pattern:*");
        _cacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Cache error"));

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenCacheThrowsAndThrowEnabled_ThrowsException()
    {
        // Arrange
        _options.ThrowOnCacheErrors = true;
        var sut = CreateBehavior();
        var request = new InvalidatingCommand(Guid.NewGuid());
        var context = CreateContext();

        _keyGenerator.GeneratePatternFromTemplate(Arg.Any<string>(), Arg.Any<InvalidatingCommand>(), Arg.Any<IRequestContext>())
            .Returns("pattern:*");
        _cacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Cache error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(
                request,
                context,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ForNonInvalidatingCommand_DoesNotInvalidate()
    {
        // Arrange
        var cacheProvider = Substitute.For<ICacheProvider>();
        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        var options = new CachingOptions { EnableCacheInvalidation = true };
        var logger = NullLogger<CacheInvalidationPipelineBehavior<NonInvalidatingCommand, string>>.Instance;

        var sut = new CacheInvalidationPipelineBehavior<NonInvalidatingCommand, string>(
            cacheProvider,
            keyGenerator,
            Options.Create(options),
            logger);

        var request = new NonInvalidatingCommand("test");
        var context = CreateContext();

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert
        await cacheProvider.DidNotReceive().RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private CacheInvalidationPipelineBehavior<InvalidatingCommand, string> CreateBehavior()
    {
        return new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(_options),
            _logger);
    }

    private CacheInvalidationPipelineBehavior<InvalidatingCommand, string> CreateBehaviorWithPubSub()
    {
        return new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(_options),
            _logger,
            _pubSubProvider);
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant1");
        context.UserId.Returns("user1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    // Command with [InvalidatesCache] attribute
    [InvalidatesCache("product:{ProductId}:*")]
    private sealed record InvalidatingCommand(Guid ProductId) : IRequest<string>;

    // Command with broadcast
    [InvalidatesCache("product:{ProductId}:*", BroadcastInvalidation = true)]
    private sealed record InvalidatingCommandWithBroadcast(Guid ProductId) : IRequest<string>;

    // Command without [InvalidatesCache] attribute
    private sealed record NonInvalidatingCommand(string Value) : IRequest<string>;
}
