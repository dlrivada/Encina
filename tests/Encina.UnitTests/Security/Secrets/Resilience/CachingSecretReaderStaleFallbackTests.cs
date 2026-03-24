#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class CachingSecretReaderStaleFallbackTests
{
    private readonly ISecretReader _innerReader;
    private readonly ICacheProvider _cache;
    private readonly ILogger<CachingSecretReaderDecorator> _logger;

    public CachingSecretReaderStaleFallbackTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _cache = Substitute.For<ICacheProvider>();
        _logger = Substitute.For<ILogger<CachingSecretReaderDecorator>>();

        // Default: cache miss on primary keys
        _cache.GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
    }

    #region Successful result stores LKG

    [Fact]
    public async Task GetSecretAsync_SuccessfulResult_StoresLastKnownGoodValue()
    {
        // Arrange
        var decorator = CreateDecorator();
        _innerReader.GetSecretAsync("db-password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("super-secret"));

        // Act — first call populates cache and LKG
        var result = await decorator.GetSecretAsync("db-password");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v => v.Should().Be("super-secret"));

        // Verify LKG was stored with extended TTL
        await _cache.Received().SetAsync(
            Arg.Is<string>(k => k.Contains(":lkg:")),
            "super-secret",
            TimeSpan.FromHours(1),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Resilience errors with LKG serve stale value

    [Fact]
    public async Task GetSecretAsync_CircuitBreakerOpen_WithLKG_ReturnsStaleValue()
    {
        // Arrange — LKG available
        _cache.GetAsync<string>(Arg.Is<string>(k => k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("cached-value"));
        _innerReader.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        var decorator = CreateDecorator();

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
        _cache.GetAsync<string>(Arg.Is<string>(k => k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("Server=prod;"));
        _innerReader.GetSecretAsync("conn-string", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ResilienceTimeout("conn-string", TimeSpan.FromSeconds(30))));
        var decorator = CreateDecorator();

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
        _cache.GetAsync<string>(Arg.Is<string>(k => k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("bearer-xyz"));
        _innerReader.GetSecretAsync("token", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ProviderUnavailable("test")));
        var decorator = CreateDecorator();

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
        // Arrange — no LKG
        _innerReader.GetSecretAsync("missing-key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        var decorator = CreateDecorator();

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
        _innerReader.GetSecretAsync("new-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.ResilienceTimeout("new-secret", TimeSpan.FromSeconds(30))));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("new-secret");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.ResilienceTimeoutCode)));
    }

    #endregion

    #region Non-resilience errors always return error (even with LKG)

    [Fact]
    public async Task GetSecretAsync_NotFound_WithLKG_ReturnsError()
    {
        // Arrange — LKG available, but non-resilience error should NOT serve stale
        _cache.GetAsync<string>(Arg.Is<string>(k => k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("old-value"));
        _innerReader.GetSecretAsync("secret-x", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.NotFound("secret-x")));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("secret-x");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.NotFoundCode)));
    }

    [Fact]
    public async Task GetSecretAsync_AccessDenied_WithLKG_ReturnsError()
    {
        // Arrange
        _cache.GetAsync<string>(Arg.Is<string>(k => k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("was-available"));
        _innerReader.GetSecretAsync("restricted", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.AccessDenied("restricted")));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync("restricted");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.AccessDeniedCode)));
    }

    #endregion

    #region MaxStaleDuration = Zero disables fallback

    [Fact]
    public async Task GetSecretAsync_MaxStaleDurationZero_DoesNotServeStaleFallback()
    {
        // Arrange
        var options = new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions { MaxStaleDuration = TimeSpan.Zero }
        };
        var decorator = new CachingSecretReaderDecorator(
            _innerReader, _cache, null, new SecretCachingOptions(), options, _logger);

        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert — no fallback because MaxStaleDuration is zero
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region EnableResilience = false disables fallback

    [Fact]
    public async Task GetSecretAsync_ResilienceDisabled_DoesNotServeStaleFallback()
    {
        // Arrange
        var options = new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = false,
            Resilience = new SecretsResilienceOptions { MaxStaleDuration = TimeSpan.FromHours(1) }
        };
        var decorator = new CachingSecretReaderDecorator(
            _innerReader, _cache, null, new SecretCachingOptions(), options, _logger);

        _innerReader.GetSecretAsync("key", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));

        // Act
        var result = await decorator.GetSecretAsync("key");

        // Assert — no fallback because EnableResilience is false
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Typed GetSecretAsync<T> staleness fallback

    [Fact]
    public async Task GetSecretAsync_Typed_ResilienceError_WithLKG_ReturnsStaleValue()
    {
        // Arrange
        var staleConfig = new TestConfig { Host = "prod-server", Port = 5432 };
        _cache.GetAsync<TestConfig>(Arg.Is<string>(k => k.Contains(":lkg:t:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestConfig?>(staleConfig));
        _cache.GetAsync<TestConfig>(Arg.Is<string>(k => !k.Contains(":lkg:")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestConfig?>(null));
        _innerReader.GetSecretAsync<TestConfig>("db-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync<TestConfig>("db-config");

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(v =>
        {
            v.Host.Should().Be("prod-server");
            v.Port.Should().Be(5432);
        });
    }

    [Fact]
    public async Task GetSecretAsync_Typed_ResilienceError_NoLKG_ReturnsError()
    {
        // Arrange
        _cache.GetAsync<TestConfig>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestConfig?>(null));
        _innerReader.GetSecretAsync<TestConfig>("new-config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(
                SecretsErrors.CircuitBreakerOpen("secrets")));
        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetSecretAsync<TestConfig>("new-config");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private CachingSecretReaderDecorator CreateDecorator()
    {
        var options = new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableResilience = true,
            Resilience = new SecretsResilienceOptions
            {
                MaxStaleDuration = TimeSpan.FromHours(1)
            }
        };
        return new CachingSecretReaderDecorator(
            _innerReader, _cache, null, new SecretCachingOptions(), options, _logger);
    }

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
