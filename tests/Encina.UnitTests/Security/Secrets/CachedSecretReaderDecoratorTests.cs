#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets;

public sealed class CachedSecretReaderDecoratorTests : IDisposable
{
    private readonly ISecretReader _innerReader;
    private readonly MemoryCache _cache;
    private readonly ILogger<CachedSecretReaderDecorator> _logger;

    public CachedSecretReaderDecoratorTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<CachedSecretReaderDecorator>>();
    }

    public void Dispose() => _cache.Dispose();

    #region GetSecretAsync (string) - Caching Enabled

    [Fact]
    public async Task GetSecretAsync_CachingEnabled_CachesSuccessfulResult()
    {
        var decorator = CreateDecorator(enableCaching: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        // First call - cache miss
        var result1 = await decorator.GetSecretAsync("key");
        // Second call - cache hit
        var result2 = await decorator.GetSecretAsync("key");

        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();
        await _innerReader.Received(1).GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_CachingEnabled_DoesNotCacheErrors()
    {
        var decorator = CreateDecorator(enableCaching: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("key")));

        // First call
        await decorator.GetSecretAsync("key");
        // Second call - should still go to inner reader
        await decorator.GetSecretAsync("key");

        await _innerReader.Received(2).GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_CachingEnabled_ReturnsCorrectValue()
    {
        var decorator = CreateDecorator(enableCaching: true);
        _innerReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("secret-123"));

        var result = await decorator.GetSecretAsync("api-key");

        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("secret-123"));
    }

    [Fact]
    public async Task GetSecretAsync_CachingEnabled_TTLExpired_DelegatesToInnerAgain()
    {
        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMilliseconds(1) // Very short TTL
        });
        var decorator = new CachedSecretReaderDecorator(_innerReader, _cache, options, _logger);

        _innerReader.GetSecretAsync("ttl-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        // First call - cache miss, populates cache
        await decorator.GetSecretAsync("ttl-key");
        // Wait for TTL to expire
        await Task.Delay(50);
        // Second call - cache should have expired, inner reader called again
        await decorator.GetSecretAsync("ttl-key");

        await _innerReader.Received(2).GetSecretAsync("ttl-key", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Caching Disabled

    [Fact]
    public async Task GetSecretAsync_CachingDisabled_PassesThroughToInner()
    {
        var decorator = CreateDecorator(enableCaching: false);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        await decorator.GetSecretAsync("key");
        await decorator.GetSecretAsync("key");

        await _innerReader.Received(2).GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Caching Enabled

    [Fact]
    public async Task GetSecretAsync_Typed_CachingEnabled_CachesSuccessfulResult()
    {
        var decorator = CreateDecorator(enableCaching: true);
        var expected = new TestConfig { Host = "localhost" };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        var result1 = await decorator.GetSecretAsync<TestConfig>("config");
        var result2 = await decorator.GetSecretAsync<TestConfig>("config");

        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();
        await _innerReader.Received(1).GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_Typed_CachingEnabled_DoesNotCacheErrors()
    {
        var decorator = CreateDecorator(enableCaching: true);
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.NotFound("config")));

        await decorator.GetSecretAsync<TestConfig>("config");
        await decorator.GetSecretAsync<TestConfig>("config");

        await _innerReader.Received(2).GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Caching Disabled

    [Fact]
    public async Task GetSecretAsync_Typed_CachingDisabled_PassesThroughToInner()
    {
        var decorator = CreateDecorator(enableCaching: false);
        var expected = new TestConfig { Host = "localhost" };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        await decorator.GetSecretAsync<TestConfig>("config");
        await decorator.GetSecretAsync<TestConfig>("config");

        await _innerReader.Received(2).GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Invalidate

    [Fact]
    public async Task Invalidate_RemovesCachedEntry()
    {
        var decorator = CreateDecorator(enableCaching: true);
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));

        // Populate cache
        await decorator.GetSecretAsync("key");
        // Invalidate
        decorator.Invalidate("key");
        // Should hit inner reader again
        await decorator.GetSecretAsync("key");

        await _innerReader.Received(2).GetSecretAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Invalidate_NullSecretName_ThrowsArgumentException()
    {
        var decorator = CreateDecorator(enableCaching: true);

        var act = () => decorator.Invalidate(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Invalidate_EmptySecretName_ThrowsArgumentException()
    {
        var decorator = CreateDecorator(enableCaching: true);

        var act = () => decorator.Invalidate("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new CachedSecretReaderDecorator(null!, _cache, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new CachedSecretReaderDecorator(_innerReader, null!, options, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachedSecretReaderDecorator(_innerReader, _cache, null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = CreateOptions(true);

        var act = () => new CachedSecretReaderDecorator(_innerReader, _cache, options, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private CachedSecretReaderDecorator CreateDecorator(bool enableCaching) =>
        new(_innerReader, _cache, CreateOptions(enableCaching), _logger);

    private static IOptions<SecretsOptions> CreateOptions(bool enableCaching) =>
        Options.Create(new SecretsOptions
        {
            EnableCaching = enableCaching,
            DefaultCacheDuration = TimeSpan.FromMinutes(5)
        });

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
    }

    #endregion
}
