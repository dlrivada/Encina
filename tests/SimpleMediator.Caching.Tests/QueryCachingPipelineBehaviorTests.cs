using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleMediator.Caching;
using static LanguageExt.Prelude;

namespace SimpleMediator.Caching.Tests;

/// <summary>
/// Unit tests for <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class QueryCachingPipelineBehaviorTests
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<QueryCachingPipelineBehavior<CachedQuery, string>> _logger;

    public QueryCachingPipelineBehaviorTests()
    {
        _cacheProvider = Substitute.For<ICacheProvider>();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _logger = NullLogger<QueryCachingPipelineBehavior<CachedQuery, string>>.Instance;
    }

    [Fact]
    public void Constructor_WithNullCacheProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };

        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<CachedQuery, string>(
            null!,
            _keyGenerator,
            Options.Create(options),
            _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheProvider");
    }

    [Fact]
    public void Constructor_WithNullKeyGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };

        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<CachedQuery, string>(
            _cacheProvider,
            null!,
            Options.Create(options),
            _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("keyGenerator");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<CachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            null!,
            _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };

        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<CachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(options),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var context = CreateContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("request", () =>
            sut.Handle(
                null!,
                context,
                () => ValueTask.FromResult(Right<MediatorError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("context", () =>
            sut.Handle(
                request,
                null!,
                () => ValueTask.FromResult(Right<MediatorError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("nextStep", () =>
            sut.Handle(request, context, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WhenCachingDisabled_CallsNextStep()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = false };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var expectedResult = "result";

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<MediatorError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => "").Should().Be(expectedResult);

        // Verify cache was not accessed
        await _cacheProvider.DidNotReceive().GetAsync<CacheEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValue()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var cacheKey = "cache:test";
        var cachedValue = "cached-result";

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        _cacheProvider.GetAsync<CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(new CacheEntry<string> { Value = cachedValue, CachedAtUtc = DateTime.UtcNow });

        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            request,
            context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<MediatorError, string>("fresh-result"));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => "").Should().Be(cachedValue);
        nextStepCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ExecutesHandlerAndCachesResult()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var cacheKey = "cache:test";
        var freshResult = "fresh-result";

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        _cacheProvider.GetAsync<CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns((CacheEntry<string>?)null);

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<MediatorError, string>(freshResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => "").Should().Be(freshResult);

        await _cacheProvider.Received(1).SetAsync(
            cacheKey,
            Arg.Any<CacheEntry<string>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsError_DoesNotCacheResult()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var cacheKey = "cache:test";
        var error = MediatorError.New("Test error");

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        _cacheProvider.GetAsync<CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns((CacheEntry<string>?)null);

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Left<MediatorError, string>(error)),
            CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();

        await _cacheProvider.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<CacheEntry<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheThrows_ContinuesWithoutCache()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = false };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var cacheKey = "cache:test";
        var freshResult = "fresh-result";

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        _cacheProvider.GetAsync<CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns<CacheEntry<string>?>(x => throw new InvalidOperationException("Cache error"));

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<MediatorError, string>(freshResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => "").Should().Be(freshResult);
    }

    [Fact]
    public async Task Handle_WhenCacheThrowsAndThrowOnCacheErrorsEnabled_ThrowsException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = true };
        var sut = CreateBehavior(options);
        var request = new CachedQuery("test");
        var context = CreateContext();
        var cacheKey = "cache:test";

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        _cacheProvider.GetAsync<CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns<CacheEntry<string>?>(x => throw new InvalidOperationException("Cache error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(
                request,
                context,
                () => ValueTask.FromResult(Right<MediatorError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ForNonCachedQuery_BypassesCaching()
    {
        // Arrange
        var cacheProvider = Substitute.For<ICacheProvider>();
        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        var options = new CachingOptions { EnableQueryCaching = true };
        var logger = NullLogger<QueryCachingPipelineBehavior<NonCachedQuery, string>>.Instance;

        var sut = new QueryCachingPipelineBehavior<NonCachedQuery, string>(
            cacheProvider,
            keyGenerator,
            Options.Create(options),
            logger);

        var request = new NonCachedQuery("test");
        var context = CreateContext();
        var expectedResult = "result";

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<MediatorError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(Right: v => v, Left: _ => "").Should().Be(expectedResult);

        // Verify cache was not accessed (no [Cache] attribute)
        await cacheProvider.DidNotReceive().GetAsync<CacheEntry<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private QueryCachingPipelineBehavior<CachedQuery, string> CreateBehavior(CachingOptions options)
    {
        return new QueryCachingPipelineBehavior<CachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(options),
            _logger);
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns("tenant1");
        context.UserId.Returns("user1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    // Request with [Cache] attribute
    [Cache(DurationSeconds = 300)]
    private sealed record CachedQuery(string Value) : IRequest<string>;

    // Request without [Cache] attribute
    private sealed record NonCachedQuery(string Value) : IRequest<string>;
}
