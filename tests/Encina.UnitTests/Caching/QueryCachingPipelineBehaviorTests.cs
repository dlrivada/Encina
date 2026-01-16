using Bogus;
using Encina.Caching;
using Encina.Testing;
using Encina.Testing.Bogus;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class QueryCachingPipelineBehaviorTests : IDisposable
{
    private readonly FakeCacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<QueryCachingPipelineBehavior<CachedQuery, string>> _logger;
    private readonly Faker _faker;
    private readonly CacheKeyFaker _keyFaker;

    public QueryCachingPipelineBehaviorTests()
    {
        _cacheProvider = new FakeCacheProvider();
        _keyGenerator = Substitute.For<ICacheKeyGenerator>();
        _logger = NullLogger<QueryCachingPipelineBehavior<CachedQuery, string>>.Instance;
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
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };

        // Act & Assert
        var act = () => new QueryCachingPipelineBehavior<CachedQuery, string>(
            null!,
            _keyGenerator,
            Options.Create(options),
            _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cacheProvider");
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

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("keyGenerator");
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

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
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
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("context", () =>
            sut.Handle(
                request,
                null!,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
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
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var expectedResult = _faker.Lorem.Sentence();

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(expectedResult);

        // Verify cache was not accessed using FakeCacheProvider
        _cacheProvider.GetOperations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValue()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var cacheKey = _keyFaker.WithPrefix("cache").Generate();
        var cachedValue = _faker.Lorem.Sentence();

        // Pre-populate cache
        await _cacheProvider.SetAsync(cacheKey, new global::Encina.Caching.CacheEntry<string> { Value = cachedValue, CachedAtUtc = DateTime.UtcNow }, null, CancellationToken.None);
        _cacheProvider.ClearTracking(); // Clear tracking so we can verify behavior

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);

        var nextStepCalled = false;

        // Act
        var result = await sut.Handle(
            request,
            context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("fresh-result"));
            },
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(cachedValue);
        nextStepCalled.ShouldBeFalse();

        // Verify cache was accessed
        _cacheProvider.WasKeyRequested(cacheKey).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ExecutesHandlerAndCachesResult()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var cacheKey = _keyFaker.WithPrefix("cache").Generate();
        var freshResult = _faker.Lorem.Sentence();

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>(freshResult)),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(freshResult);

        // Verify the key was cached using FakeCacheProvider verification
        _cacheProvider.WasKeyCached(cacheKey).ShouldBeTrue();

        // Verify the cached value
        var cachedEntry = _cacheProvider.GetValue<global::Encina.Caching.CacheEntry<string>>(cacheKey);
        cachedEntry.ShouldNotBeNull();
        cachedEntry.Value.ShouldBe(freshResult);
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsError_DoesNotCacheResult()
    {
        // Arrange
        var options = new CachingOptions { EnableQueryCaching = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var cacheKey = _keyFaker.WithPrefix("cache").Generate();
        var errorMessage = _faker.Lorem.Sentence();
        var error = EncinaError.New(errorMessage);

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Left<EncinaError, string>(error)),
            CancellationToken.None);

        // Assert
        result.ShouldBeError();

        // Verify nothing was cached
        _cacheProvider.CachedKeys.ShouldNotContain(cacheKey);
    }

    [Fact]
    public async Task Handle_WhenCacheThrows_ContinuesWithoutCache()
    {
        // Arrange - using a mock for this specific error simulation test
        var errorCacheProvider = Substitute.For<ICacheProvider>();
        var options = new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = false };
        var sut = new QueryCachingPipelineBehavior<CachedQuery, string>(
            errorCacheProvider,
            _keyGenerator,
            Options.Create(options),
            _logger);

        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var cacheKey = _keyFaker.WithPrefix("cache").Generate();
        var freshResult = _faker.Lorem.Sentence();

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);
        errorCacheProvider.GetAsync<global::Encina.Caching.CacheEntry<string>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns<global::Encina.Caching.CacheEntry<string>?>(x => throw new InvalidOperationException("Cache error"));

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>(freshResult)),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(freshResult);
    }

    [Fact]
    public async Task Handle_WhenCacheThrowsAndThrowOnCacheErrorsEnabled_ThrowsException()
    {
        // Arrange - use FakeCacheProvider with SimulateErrors
        _cacheProvider.SimulateErrors = true;
        var options = new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = true };
        var sut = CreateBehavior(options);
        var queryValue = _faker.Lorem.Word();
        var request = new CachedQuery(queryValue);
        var context = CreateContext();
        var cacheKey = _keyFaker.WithPrefix("cache").Generate();

        _keyGenerator.GenerateKey<CachedQuery, string>(request, context).Returns(cacheKey);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(
                request,
                context,
                () => ValueTask.FromResult(Right<EncinaError, string>("result")),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ForNonCachedQuery_BypassesCaching()
    {
        // Arrange
        using var cacheProvider = new FakeCacheProvider();
        var keyGenerator = Substitute.For<ICacheKeyGenerator>();
        var options = new CachingOptions { EnableQueryCaching = true };
        var logger = NullLogger<QueryCachingPipelineBehavior<NonCachedQuery, string>>.Instance;

        var sut = new QueryCachingPipelineBehavior<NonCachedQuery, string>(
            cacheProvider,
            keyGenerator,
            Options.Create(options),
            logger);

        var queryValue = _faker.Lorem.Word();
        var request = new NonCachedQuery(queryValue);
        var context = CreateContext();
        var expectedResult = _faker.Lorem.Sentence();

        // Act
        var result = await sut.Handle(
            request,
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.ShouldBeSuccess().ShouldBe(expectedResult);

        // Verify cache was not accessed (no [Cache] attribute)
        cacheProvider.GetOperations.ShouldBeEmpty();
        cacheProvider.CachedKeys.ShouldBeEmpty();
    }

    private QueryCachingPipelineBehavior<CachedQuery, string> CreateBehavior(CachingOptions options)
    {
        return new QueryCachingPipelineBehavior<CachedQuery, string>(
            _cacheProvider,
            _keyGenerator,
            Options.Create(options),
            _logger);
    }

    private IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(_faker.Random.TenantId());
        context.UserId.Returns(_faker.Random.UserId());
        context.CorrelationId.Returns(_faker.Random.CorrelationId().ToString());
        return context;
    }

    // Request with [Cache] attribute
    [Cache(DurationSeconds = 300)]
    private sealed record CachedQuery(string Value) : IRequest<string>;

    // Request without [Cache] attribute
    private sealed record NonCachedQuery(string Value) : IRequest<string>;
}
