using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Caching.Tests;

/// <summary>
/// Unit tests for <see cref="CacheInvalidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class CacheInvalidationPipelineBehaviorTests : IDisposable
{
    private readonly FakeCacheProvider _cacheProvider;
    private readonly FakePubSubProvider _pubSubProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly CachingOptions _options;
    private readonly ILogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, string>> _logger;
    private readonly Faker _faker;
    private readonly CacheKeyFaker _keyFaker;

    public CacheInvalidationPipelineBehaviorTests()
    {
        _cacheProvider = new FakeCacheProvider();
        _pubSubProvider = new FakePubSubProvider();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _options = new CachingOptions
        {
            EnableCacheInvalidation = true,
            EnablePubSubInvalidation = true,
            InvalidationChannel = "cache:invalidate"
        };
        _logger = NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, string>>.Instance;
        _faker = new Faker();
        _keyFaker = new CacheKeyFaker();
    }

    public void Dispose()
    {
        _cacheProvider.Dispose();
        GC.SuppressFinalize(this);
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
        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();

        // Pre-populate cache with some data
        var cacheKey = $"product:{productId}:details";
        await _cacheProvider.SetAsync(cacheKey, "cached-data", null, CancellationToken.None);
        _cacheProvider.ClearTracking();

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert - cache should not have been cleared
        _cacheProvider.RemovedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_OnSuccess_InvalidatesCache()
    {
        // Arrange
        var sut = CreateBehavior();
        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();
        var expectedPattern = $"product:{productId}:*";

        // Pre-populate cache with matching keys
        await _cacheProvider.SetAsync($"product:{productId}:details", "data1", null, CancellationToken.None);
        await _cacheProvider.SetAsync($"product:{productId}:inventory", "data2", null, CancellationToken.None);
        _cacheProvider.ClearTracking();

        _keyGenerator.GeneratePatternFromTemplate("product:{ProductId}:*", request, context)
            .Returns(expectedPattern);

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert - both keys should have been removed
        _cacheProvider.RemovedKeys.ShouldContain($"product:{productId}:details");
        _cacheProvider.RemovedKeys.ShouldContain($"product:{productId}:inventory");
    }

    [Fact]
    public async Task Handle_OnFailure_DoesNotInvalidateCache()
    {
        // Arrange
        var sut = CreateBehavior();
        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();
        var errorMessage = _faker.Lorem.Sentence();
        var error = EncinaError.New(errorMessage);

        // Pre-populate cache
        var cacheKey = $"product:{productId}:details";
        await _cacheProvider.SetAsync(cacheKey, "cached-data", null, CancellationToken.None);
        _cacheProvider.ClearTracking();

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Left<EncinaError, string>(error)),
            CancellationToken.None);

        // Assert - cache should not have been cleared on error
        _cacheProvider.RemovedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithBroadcast_PublishesInvalidation()
    {
        // Arrange
        _options.EnablePubSubInvalidation = true;
        var productId = _faker.Random.Guid();
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

        // Assert - verify message was published to invalidation channel
        _pubSubProvider.WasMessagePublished(_options.InvalidationChannel).ShouldBeTrue();
        _pubSubProvider.WasMessagePublished(_options.InvalidationChannel, expectedPattern).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenCacheThrows_ContinuesWithoutThrowing()
    {
        // Arrange - use mock for error simulation
        var errorCacheProvider = Substitute.For<ICacheProvider>();
        _options.ThrowOnCacheErrors = false;
        var sut = new CacheInvalidationPipelineBehavior<InvalidatingCommand, string>(
            errorCacheProvider,
            _keyGenerator,
            Options.Create(_options),
            _logger);

        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();
        var expectedResult = _faker.Lorem.Sentence();

        _keyGenerator.GeneratePatternFromTemplate(Arg.Any<string>(), Arg.Any<InvalidatingCommand>(), Arg.Any<IRequestContext>())
            .Returns("pattern:*");
        errorCacheProvider.RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Cache error"));

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenCacheThrowsAndThrowEnabled_ThrowsException()
    {
        // Arrange - use FakeCacheProvider with SimulateErrors
        _cacheProvider.SimulateErrors = true;
        _options.ThrowOnCacheErrors = true;
        var sut = CreateBehavior();
        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommand(productId);
        var context = CreateContext();

        _keyGenerator.GeneratePatternFromTemplate(Arg.Any<string>(), Arg.Any<InvalidatingCommand>(), Arg.Any<IRequestContext>())
            .Returns("pattern:*");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(
                request,
                context,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithDelayedInvalidation_InvalidatesAfterDelay()
    {
        // Arrange
        using var cacheProvider = new FakeCacheProvider();
        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        var options = new CachingOptions { EnableCacheInvalidation = true };
        var logger = NullLogger<CacheInvalidationPipelineBehavior<InvalidatingCommandWithDelay, string>>.Instance;

        var sut = new CacheInvalidationPipelineBehavior<InvalidatingCommandWithDelay, string>(
            cacheProvider,
            keyGenerator,
            Options.Create(options),
            logger);

        var productId = _faker.Random.Guid();
        var request = new InvalidatingCommandWithDelay(productId);
        var context = CreateContext();
        var expectedPattern = $"product:{productId}:*";

        // Pre-populate cache
        await cacheProvider.SetAsync($"product:{productId}:details", "data", null, CancellationToken.None);
        cacheProvider.ClearTracking();

        keyGenerator.GeneratePatternFromTemplate("product:{ProductId}:*", request, context)
            .Returns(expectedPattern);

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert - immediately after the call, cache may not be invalidated yet (delay is 50ms)
        // Wait a bit more than the delay to ensure invalidation occurs
        await Task.Delay(100);

        // Now the cache should have been invalidated
        cacheProvider.RemovedKeys.ShouldContain($"product:{productId}:details");
    }

    [Fact]
    public async Task Handle_ForNonInvalidatingCommand_DoesNotInvalidate()
    {
        // Arrange
        using var cacheProvider = new FakeCacheProvider();
        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        var options = new CachingOptions { EnableCacheInvalidation = true };
        var logger = NullLogger<CacheInvalidationPipelineBehavior<NonInvalidatingCommand, string>>.Instance;

        var sut = new CacheInvalidationPipelineBehavior<NonInvalidatingCommand, string>(
            cacheProvider,
            keyGenerator,
            Options.Create(options),
            logger);

        var requestValue = _faker.Lorem.Word();
        var request = new NonInvalidatingCommand(requestValue);
        var context = CreateContext();

        // Pre-populate cache
        await cacheProvider.SetAsync("some:key", "data", null, CancellationToken.None);
        cacheProvider.ClearTracking();

        // Act
        await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("result")),
            CancellationToken.None);

        // Assert - cache should not have been cleared (no [InvalidatesCache] attribute)
        cacheProvider.RemovedKeys.ShouldBeEmpty();
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

    private IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(_faker.Random.TenantId());
        context.UserId.Returns(_faker.Random.UserId());
        context.CorrelationId.Returns(_faker.Random.CorrelationId().ToString());
        return context;
    }

    // Command with [InvalidatesCache] attribute
    [InvalidatesCache("product:{ProductId}:*")]
    private sealed record InvalidatingCommand(Guid ProductId) : IRequest<string>;

    // Command with broadcast
    [InvalidatesCache("product:{ProductId}:*", BroadcastInvalidation = true)]
    private sealed record InvalidatingCommandWithBroadcast(Guid ProductId) : IRequest<string>;

    // Command with delayed invalidation
    [InvalidatesCache("product:{ProductId}:*", DelayMilliseconds = 50)]
    private sealed record InvalidatingCommandWithDelay(Guid ProductId) : IRequest<string>;

    // Command without [InvalidatesCache] attribute
    private sealed record NonInvalidatingCommand(string Value) : IRequest<string>;
}
