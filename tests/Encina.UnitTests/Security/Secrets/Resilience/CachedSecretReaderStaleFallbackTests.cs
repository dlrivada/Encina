#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class CachedSecretReaderStaleFallbackTests : IDisposable
{
    private readonly ISecretReader _innerReader;
    private readonly MemoryCache _cache;
    private readonly ILogger<CachedSecretReaderDecorator> _logger;

    public CachedSecretReaderStaleFallbackTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<CachedSecretReaderDecorator>>();
    }

    public void Dispose() => _cache.Dispose();

    #region Successful result stores LKG

    [Fact]
    public async Task GetSecretAsync_SuccessfulResult_StoresLastKnownGoodValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        _innerReader.GetSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("super-secret"));

        // Act - first call populates cache and LKG
        var result = await decorator.GetSecretAsync("db-password");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("super-secret"));

        // Now simulate provider failure - LKG should be available
        _innerReader.GetSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));

        // Invalidate the primary cache so it goes to inner reader
        decorator.Invalidate("db-password");

        var fallbackResult = await decorator.GetSecretAsync("db-password");
        fallbackResult.IsRight.Should().BeTrue();
        fallbackResult.IfRight(v => v.Should().Be("super-secret"));
    }

    #endregion

    #region Resilience errors with LKG serve stale value

    [Fact]
    public async Task GetSecretAsync_CircuitBreakerOpen_WithLKG_ReturnsStaleValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        await PopulateCache(decorator, "api-key", "cached-value");

        _innerReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        decorator.Invalidate("api-key");

        // Act
        var result = await decorator.GetSecretAsync("api-key");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("cached-value"));
    }

    [Fact]
    public async Task GetSecretAsync_ResilienceTimeout_WithLKG_ReturnsStaleValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        await PopulateCache(decorator, "conn-string", "Server=prod;");

        _innerReader.GetSecretAsync("conn-string", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ResilienceTimeout("conn-string", TimeSpan.FromSeconds(30))));
        decorator.Invalidate("conn-string");

        // Act
        var result = await decorator.GetSecretAsync("conn-string");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("Server=prod;"));
    }

    [Fact]
    public async Task GetSecretAsync_ProviderUnavailable_WithLKG_ReturnsStaleValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        await PopulateCache(decorator, "token", "bearer-xyz");

        _innerReader.GetSecretAsync("token", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("test")));
        decorator.Invalidate("token");

        // Act
        var result = await decorator.GetSecretAsync("token");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("bearer-xyz"));
    }

    #endregion

    #region Resilience errors without LKG return error

    [Fact]
    public async Task GetSecretAsync_CircuitBreakerOpen_NoLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        var error = SecretsErrors.CircuitBreakerOpen("secrets");

        _innerReader.GetSecretAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(error));

        // Act
        var result = await decorator.GetSecretAsync("missing-key");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    [Fact]
    public async Task GetSecretAsync_ResilienceTimeout_NoLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        var error = SecretsErrors.ResilienceTimeout("new-secret", TimeSpan.FromSeconds(30));

        _innerReader.GetSecretAsync("new-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(error));

        // Act
        var result = await decorator.GetSecretAsync("new-secret");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.ResilienceTimeoutCode)));
    }

    [Fact]
    public async Task GetSecretAsync_ProviderUnavailable_NoLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        var error = SecretsErrors.ProviderUnavailable("test");

        _innerReader.GetSecretAsync("unknown", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(error));

        // Act
        var result = await decorator.GetSecretAsync("unknown");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.ProviderUnavailableCode)));
    }

    #endregion

    #region Non-resilience errors always return error (even with LKG)

    [Fact]
    public async Task GetSecretAsync_NotFound_WithLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        await PopulateCache(decorator, "secret-x", "old-value");

        _innerReader.GetSecretAsync("secret-x", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("secret-x")));
        decorator.Invalidate("secret-x");

        // Act
        var result = await decorator.GetSecretAsync("secret-x");

        // Assert - should NOT serve stale value for non-resilience error
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.NotFoundCode)));
    }

    [Fact]
    public async Task GetSecretAsync_AccessDenied_WithLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        await PopulateCache(decorator, "restricted", "was-available");

        _innerReader.GetSecretAsync("restricted", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.AccessDenied("restricted")));
        decorator.Invalidate("restricted");

        // Act
        var result = await decorator.GetSecretAsync("restricted");

        // Assert - should NOT serve stale value for access denied
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.AccessDeniedCode)));
    }

    #endregion

    #region MaxStaleDuration = TimeSpan.Zero disables fallback

    [Fact]
    public async Task GetSecretAsync_MaxStaleDurationZero_DoesNotServeStaleFallback()
    {
        // Arrange
        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions
            {
                MaxStaleDuration = TimeSpan.Zero
            }
        });
        var decorator = new CachedSecretReaderDecorator(_innerReader, _cache, options, _logger);

        // Populate a successful value first
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));
        await decorator.GetSecretAsync("key");

        // Now fail with resilience error
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        decorator.Invalidate("key");

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert - no fallback because MaxStaleDuration is zero
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region EnableResilience = false disables fallback

    [Fact]
    public async Task GetSecretAsync_ResilienceDisabled_DoesNotServeStaleFallback()
    {
        // Arrange
        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = false,
            Resilience = new SecretsResilienceOptions
            {
                MaxStaleDuration = TimeSpan.FromHours(1)
            }
        });
        var decorator = new CachedSecretReaderDecorator(_innerReader, _cache, options, _logger);

        // Populate a successful value first
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("value"));
        await decorator.GetSecretAsync("key");

        // Now fail with resilience error
        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        decorator.Invalidate("key");

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert - no fallback because EnableResilience is false
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region Typed GetSecretAsync<T> staleness fallback

    [Fact]
    public async Task GetSecretAsync_Typed_SuccessfulResult_StoresLastKnownGoodValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        var config = new TestConfig { Host = "prod-server", Port = 5432 };
        _innerReader.GetSecretAsync<TestConfig>("db-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(config));

        // Act - populate LKG
        await decorator.GetSecretAsync<TestConfig>("db-config");

        // Simulate provider failure
        _innerReader.GetSecretAsync<TestConfig>("db-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.ProviderUnavailable("test")));

        // Force cache miss by using a new cache (LKG is stored separately)
        // Instead, invalidate won't clear typed cache key the same way, so let's
        // expire the primary cache by using short TTL
        var shortTtlOptions = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMilliseconds(1),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions { MaxStaleDuration = TimeSpan.FromHours(1) }
        });
        var shortCache = new MemoryCache(new MemoryCacheOptions());
        var shortDecorator = new CachedSecretReaderDecorator(_innerReader, shortCache, shortTtlOptions, _logger);

        // First populate with success
        _innerReader.GetSecretAsync<TestConfig>("db-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(config));
        await shortDecorator.GetSecretAsync<TestConfig>("db-config");

        // Wait for primary cache to expire
        await Task.Delay(50);

        // Now fail with resilience error
        _innerReader.GetSecretAsync<TestConfig>("db-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));

        // Act
        var result = await shortDecorator.GetSecretAsync<TestConfig>("db-config");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("prod-server");
            v.Port.Should().Be(5432);
        });

        shortCache.Dispose();
    }

    [Fact]
    public async Task GetSecretAsync_Typed_ResilienceError_NoLKG_ReturnsError()
    {
        // Arrange
        var decorator = CreateDecorator();
        var error = SecretsErrors.CircuitBreakerOpen("secrets");

        _innerReader.GetSecretAsync<TestConfig>("new-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(error));

        // Act
        var result = await decorator.GetSecretAsync<TestConfig>("new-config");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    [Fact]
    public async Task GetSecretAsync_Typed_NonResilienceError_WithLKG_ReturnsError()
    {
        // Arrange - use short TTL so primary cache expires but LKG remains
        var shortCache = new MemoryCache(new MemoryCacheOptions());
        var shortTtlOptions = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMilliseconds(1),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions { MaxStaleDuration = TimeSpan.FromHours(1) }
        });
        var decorator = new CachedSecretReaderDecorator(_innerReader, shortCache, shortTtlOptions, _logger);

        var config = new TestConfig { Host = "old-host", Port = 3306 };
        _innerReader.GetSecretAsync<TestConfig>("typed-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(config));
        await decorator.GetSecretAsync<TestConfig>("typed-secret");

        // Wait for primary cache to expire
        await Task.Delay(50);

        // Now return NotFound (non-resilience error)
        _innerReader.GetSecretAsync<TestConfig>("typed-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.NotFound("typed-secret")));

        // Act
        var result = await decorator.GetSecretAsync<TestConfig>("typed-secret");

        // Assert - should NOT serve stale for non-resilience errors
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.NotFoundCode)));

        shortCache.Dispose();
    }

    #endregion

    #region Helpers

    private CachedSecretReaderDecorator CreateDecorator()
    {
        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions
            {
                MaxStaleDuration = TimeSpan.FromHours(1)
            }
        });
        return new CachedSecretReaderDecorator(_innerReader, _cache, options, _logger);
    }

    private async Task PopulateCache(CachedSecretReaderDecorator decorator, string secretName, string value)
    {
        _innerReader.GetSecretAsync(secretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(value));
        await decorator.GetSecretAsync(secretName);
    }

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
