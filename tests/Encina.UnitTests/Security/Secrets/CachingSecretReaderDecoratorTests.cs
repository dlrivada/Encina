#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class CachingSecretReaderDecoratorTests
{
    private readonly ISecretReader _innerReader;
    private readonly ICacheProvider _cache;
    private readonly SecretCachingOptions _cachingOptions;
    private readonly SecretsOptions _secretsOptions;
    private readonly ILogger<CachingSecretReaderDecorator> _logger;

    public CachingSecretReaderDecoratorTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _cache = Substitute.For<ICacheProvider>();
        _cachingOptions = new SecretCachingOptions();
        _secretsOptions = new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5)
        };
        _logger = Substitute.For<ILogger<CachingSecretReaderDecorator>>();
    }

    #region GetSecretAsync (string) - Cache Hit

    [Fact]
    public async Task GetSecretAsync_CacheHit_ReturnsFromCache_DoesNotCallInner()
    {
        // Arrange — GetAsync returns cached value (hit detection)
        _cache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("cached-value"));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("cached-value"));
        await _innerReader.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        // GetOrSetAsync should NOT be called on cache hit
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Cache Miss

    [Fact]
    public async Task GetSecretAsync_CacheMiss_CallsInner_StoresInCache()
    {
        // Arrange — GetAsync returns null (miss), GetOrSetAsync invokes factory
        _cache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("inner-value"));
        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<string>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                // Invoke the factory to simulate cache miss
                var factory = callInfo.ArgAt<Func<CancellationToken, Task<string>>>(1);
                return await factory(CancellationToken.None).ConfigureAwait(false);
            });
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("inner-value"));
        await _cache.Received(1).GetOrSetAsync(
            Arg.Is<string>(k => k.Contains(":v:key")),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            _secretsOptions.DefaultCacheDuration,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Error Not Cached

    [Fact]
    public async Task GetSecretAsync_InnerReturnsError_DoesNotCache()
    {
        // Arrange — GetAsync returns null (miss), factory throws StoreResultException
        _cache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("key")));
        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<string>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var factory = callInfo.ArgAt<Func<CancellationToken, Task<string>>>(1);
                return await factory(CancellationToken.None).ConfigureAwait(false);
            });
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        // SetAsync should not be called for error results (LKG is also off since resilience is disabled)
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Cache Provider Failure

    [Fact]
    public async Task GetSecretAsync_CacheProviderFails_FallsToInner()
    {
        // Arrange — GetAsync returns null (miss), GetOrSetAsync throws (cache infrastructure failure)
        _cache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<string>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Cache down"));
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("fallback-value"));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("fallback-value"));
    }

    #endregion

    #region GetSecretAsync (string) - Caching Disabled

    [Fact]
    public async Task GetSecretAsync_CachingDisabled_PassesThroughToInner()
    {
        // Arrange
        _secretsOptions.EnableCaching = false;
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));
        var decorator = CreateDecorator();

        // Act
        await decorator.GetSecretAsync("key");

        // Assert
        await _cache.DidNotReceive().GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
        await _innerReader.Received(1).GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Separate Cache Key

    [Fact]
    public async Task GetSecretAsync_TypedSecret_UsesSeparateCacheKey()
    {
        // Arrange
        _cache.GetAsync<TestConfig>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestConfig?>(null));
        var expected = new TestConfig { Host = "localhost" };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));
        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<TestConfig>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var factory = callInfo.ArgAt<Func<CancellationToken, Task<TestConfig>>>(1);
                return await factory(CancellationToken.None).ConfigureAwait(false);
            });
        var decorator = CreateDecorator();

        // Act
        await decorator.GetSecretAsync<TestConfig>("config");

        // Assert — typed key contains type name
        await _cache.Received(1).GetOrSetAsync(
            Arg.Is<string>(k => k.Contains(":t:config:") && k.Contains(nameof(TestConfig))),
            Arg.Any<Func<CancellationToken, Task<TestConfig>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region InvalidateAsync

    [Fact]
    public async Task InvalidateAsync_RemovesAllCacheVariantsByPattern()
    {
        // Arrange
        var decorator = CreateDecorator();

        // Act
        await decorator.InvalidateAsync("my-secret");

        // Assert — explicit keys removed + typed variants via pattern
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":v:my-secret")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains(":lkg:my-secret")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            Arg.Is<string>(p => p.Contains(":t:my-secret:")),
            Arg.Any<CancellationToken>());
        await _cache.Received().RemoveByPatternAsync(
            Arg.Is<string>(p => p.Contains(":lkg:t:my-secret:")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAsync_NullSecretName_ThrowsArgumentNullException()
    {
        // Arrange
        var decorator = CreateDecorator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => decorator.InvalidateAsync(null!));
    }

    [Fact]
    public async Task InvalidateAsync_EmptySecretName_ThrowsArgumentException()
    {
        // Arrange
        var decorator = CreateDecorator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => decorator.InvalidateAsync(""));
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            null!, _cache, _cachingOptions, _secretsOptions, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            _innerReader, null!, _cachingOptions, _secretsOptions, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullCachingOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            _innerReader, _cache, null!, _secretsOptions, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cachingOptions");
    }

    [Fact]
    public void Constructor_NullSecretsOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            _innerReader, _cache, _cachingOptions, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("secretsOptions");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachingSecretReaderDecorator(
            _innerReader, _cache, _cachingOptions, _secretsOptions, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Helpers

    private CachingSecretReaderDecorator CreateDecorator() =>
        new(_innerReader, _cache, _cachingOptions, _secretsOptions, _logger);

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
    }

    #endregion
}
