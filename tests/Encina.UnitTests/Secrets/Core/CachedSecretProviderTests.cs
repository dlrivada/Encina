using Encina.Secrets;
using Encina.TestInfrastructure.Extensions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Secrets.Core;

/// <summary>
/// Unit tests for <see cref="CachedSecretProvider"/>.
/// Verifies cache read-through, write-through invalidation, ROP-awareness (only Right values cached),
/// and disabled-cache passthrough behaviour.
/// </summary>
public sealed class CachedSecretProviderTests : IDisposable
{
    private const string SecretName = "my-secret";
    private const string SecretVersion = "v2";

    private readonly ISecretProvider _inner;
    private readonly MemoryCache _cache;
    private readonly ILogger<CachedSecretProvider> _logger;

    // Default: caching enabled with 5-minute TTL
    private readonly SecretCacheOptions _defaultOptions = new() { Enabled = true, DefaultTtl = TimeSpan.FromMinutes(5) };

    public CachedSecretProviderTests()
    {
        _inner = Substitute.For<ISecretProvider>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<CachedSecretProvider>>();
    }

    public void Dispose() => _cache.Dispose();

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private CachedSecretProvider CreateSut(SecretCacheOptions? options = null)
    {
        var opts = Options.Create(options ?? _defaultOptions);
        return new CachedSecretProvider(_inner, _cache, opts, _logger);
    }

    private static Secret MakeSecret(string name = SecretName, string version = "v1") =>
        new(name, "super-secret-value", version, null);

    private static SecretMetadata MakeMetadata(string name = SecretName, string version = "v1") =>
        new(name, version, DateTime.UtcNow, null);

    // ---------------------------------------------------------------------------
    // Constructor null-check tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Arrange
        var opts = Options.Create(_defaultOptions);

        // Act
        var act = () => new CachedSecretProvider(null!, _cache, opts, _logger);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Arrange
        var opts = Options.Create(_defaultOptions);

        // Act
        var act = () => new CachedSecretProvider(_inner, null!, opts, _logger);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CachedSecretProvider(_inner, _cache, null!, _logger);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var opts = Options.Create(_defaultOptions);

        // Act
        var act = () => new CachedSecretProvider(_inner, _cache, opts, null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ---------------------------------------------------------------------------
    // GetSecretAsync – cache miss, cache hit, ROP-awareness
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_CacheMiss_DelegatesToInnerAndCachesResult()
    {
        // Arrange
        var expected = MakeSecret();
        var sut = CreateSut();

#pragma warning disable CA2012 // NSubstitute internally manages ValueTask lifecycle
        _inner.GetSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act
        var result = await sut.GetSecretAsync(SecretName);

        // Assert – delegates to inner and returns the secret
        var secret = result.ShouldBeSuccess();
        secret.ShouldBe(expected);
        await _inner.Received(1).GetSecretAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_CacheMiss_StoresValueInCache()
    {
        // Arrange
        var expected = MakeSecret();
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.GetSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act – first call populates the cache
        await sut.GetSecretAsync(SecretName);

        // Assert – second call should be a cache hit (inner not called again)
        await sut.GetSecretAsync(SecretName);
        await _inner.Received(1).GetSecretAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_CacheHit_ReturnsValueWithoutCallingInner()
    {
        // Arrange
        var expected = MakeSecret();
        var sut = CreateSut();

        // Pre-populate the cache using the known key format
        _cache.Set("encina:secrets:" + SecretName, expected, TimeSpan.FromMinutes(5));

        // Act
        var result = await sut.GetSecretAsync(SecretName);

        // Assert – inner is never called
        result.ShouldBeSuccess(expected);
        await _inner.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_InnerReturnsLeft_ResultIsNotCached()
    {
        // Arrange
        var error = SecretsErrorCodes.NotFound(SecretName);
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.GetSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act – two calls with a Left result
        await sut.GetSecretAsync(SecretName);
        await sut.GetSecretAsync(SecretName);

        // Assert – inner called both times (no caching of errors)
        await _inner.Received(2).GetSecretAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_InnerReturnsLeft_PropagatesErrorCode()
    {
        // Arrange
        var error = SecretsErrorCodes.NotFound(SecretName);
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.GetSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        var result = await sut.GetSecretAsync(SecretName);

        // Assert
        result.ShouldBeErrorWithCode(SecretsErrorCodes.NotFoundCode);
    }

    // ---------------------------------------------------------------------------
    // GetSecretVersionAsync – cache miss, cache hit, ROP-awareness
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretVersionAsync_CacheMiss_DelegatesToInnerAndCachesResult()
    {
        // Arrange
        var expected = MakeSecret(version: SecretVersion);
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act
        var result = await sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        var secret = result.ShouldBeSuccess();
        secret.ShouldBe(expected);
        await _inner.Received(1).GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretVersionAsync_CacheHit_ReturnsValueWithoutCallingInner()
    {
        // Arrange
        var expected = MakeSecret(version: SecretVersion);
        var sut = CreateSut();

        // Pre-populate cache with versioned key
        _cache.Set("encina:secrets:v:" + SecretName + ":" + SecretVersion, expected, TimeSpan.FromMinutes(5));

        // Act
        var result = await sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        result.ShouldBeSuccess(expected);
        await _inner.DidNotReceive().GetSecretVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretVersionAsync_InnerReturnsLeft_ResultIsNotCached()
    {
        // Arrange
        var error = SecretsErrorCodes.VersionNotFound(SecretName, SecretVersion);
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act – two calls
        await sut.GetSecretVersionAsync(SecretName, SecretVersion);
        await sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert – inner called both times
        await _inner.Received(2).GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------------
    // ExistsAsync – cache miss, cache hit, ROP-awareness
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExistsAsync_CacheMiss_DelegatesToInnerAndCachesResult()
    {
        // Arrange
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.ExistsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Either<EncinaError, bool>.Right(true)));
#pragma warning restore CA2012

        // Act
        var result = await sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
        await _inner.Received(1).ExistsAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_CacheHit_ReturnsValueWithoutCallingInner()
    {
        // Arrange
        var sut = CreateSut();
        _cache.Set("encina:secrets:exists:" + SecretName, true, TimeSpan.FromMinutes(5));

        // Act
        var result = await sut.ExistsAsync(SecretName);

        // Assert
        result.ShouldBeSuccess(true);
        await _inner.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_CachesSecondCallAfterCacheMiss()
    {
        // Arrange
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.ExistsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Either<EncinaError, bool>.Right(false)));
#pragma warning restore CA2012

        // Act
        await sut.ExistsAsync(SecretName);
        await sut.ExistsAsync(SecretName);

        // Assert – inner called only once (second call served from cache)
        await _inner.Received(1).ExistsAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_InnerReturnsLeft_ResultIsNotCached()
    {
        // Arrange
        var error = SecretsErrorCodes.ProviderUnavailable("test-provider");
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.ExistsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Either<EncinaError, bool>.Left(error)));
#pragma warning restore CA2012

        // Act
        await sut.ExistsAsync(SecretName);
        await sut.ExistsAsync(SecretName);

        // Assert – inner called both times (errors are never cached)
        await _inner.Received(2).ExistsAsync(SecretName, Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------------------
    // SetSecretAsync – delegates to inner, invalidates cache on Right
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SetSecretAsync_AlwaysDelegatesToInner()
    {
        // Arrange
        var metadata = MakeMetadata();
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(Either<EncinaError, SecretMetadata>.Right(metadata)));
#pragma warning restore CA2012

        // Act
        var result = await sut.SetSecretAsync(SecretName, "new-value");

        // Assert
        result.ShouldBeSuccess();
        await _inner.Received(1).SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_OnRight_InvalidatesGetSecretCacheEntry()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var metadata = MakeMetadata();
        var sut = CreateSut();

        // Pre-populate the get-secret cache entry
        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.SetSecretAsync(SecretName, "updated-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(Either<EncinaError, SecretMetadata>.Right(metadata)));
#pragma warning restore CA2012

        // Act
        await sut.SetSecretAsync(SecretName, "updated-value");

        // Assert – cache entry removed; next GetSecretAsync must hit inner
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeFalse();
    }

    [Fact]
    public async Task SetSecretAsync_OnRight_InvalidatesExistsCacheEntry()
    {
        // Arrange
        var metadata = MakeMetadata();
        var sut = CreateSut();

        // Pre-populate the exists cache entry
        _cache.Set("encina:secrets:exists:" + SecretName, true, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(Either<EncinaError, SecretMetadata>.Right(metadata)));
#pragma warning restore CA2012

        // Act
        await sut.SetSecretAsync(SecretName, "new-value");

        // Assert – exists cache entry removed
        var cacheHit = _cache.TryGetValue("encina:secrets:exists:" + SecretName, out bool _);
        cacheHit.ShouldBeFalse();
    }

    [Fact]
    public async Task SetSecretAsync_OnLeft_DoesNotInvalidateCache()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var error = SecretsErrorCodes.OperationFailed("set", "permission denied");
        var sut = CreateSut();

        // Pre-populate cache
        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(Either<EncinaError, SecretMetadata>.Left(error)));
#pragma warning restore CA2012

        // Act
        await sut.SetSecretAsync(SecretName, "new-value");

        // Assert – cache entry still present after failed write
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeTrue();
    }

    // ---------------------------------------------------------------------------
    // DeleteSecretAsync – delegates to inner, invalidates cache on Right
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSecretAsync_AlwaysDelegatesToInner()
    {
        // Arrange
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));
#pragma warning restore CA2012

        // Act
        var result = await sut.DeleteSecretAsync(SecretName);

        // Assert
        result.ShouldBeSuccess();
        await _inner.Received(1).DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSecretAsync_OnRight_InvalidatesGetSecretCacheEntry()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var sut = CreateSut();

        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));
#pragma warning restore CA2012

        // Act
        await sut.DeleteSecretAsync(SecretName);

        // Assert – cache entry removed
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteSecretAsync_OnRight_InvalidatesExistsCacheEntry()
    {
        // Arrange
        var sut = CreateSut();

        _cache.Set("encina:secrets:exists:" + SecretName, true, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));
#pragma warning restore CA2012

        // Act
        await sut.DeleteSecretAsync(SecretName);

        // Assert – exists entry also removed
        var cacheHit = _cache.TryGetValue("encina:secrets:exists:" + SecretName, out bool _);
        cacheHit.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteSecretAsync_OnLeft_DoesNotInvalidateCache()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var error = SecretsErrorCodes.NotFound(SecretName);
        var sut = CreateSut();

        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Left(error)));
#pragma warning restore CA2012

        // Act
        await sut.DeleteSecretAsync(SecretName);

        // Assert – cache untouched after failed delete
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeTrue();
    }

    // ---------------------------------------------------------------------------
    // ListSecretsAsync – always delegates, no caching
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ListSecretsAsync_AlwaysDelegatesToInner()
    {
        // Arrange
        IEnumerable<string> names = ["secret-a", "secret-b"];
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IEnumerable<string>>>(
                Either<EncinaError, IEnumerable<string>>.Right(names)));
#pragma warning restore CA2012

        // Act – two consecutive calls
        await sut.ListSecretsAsync();
        await sut.ListSecretsAsync();

        // Assert – inner called both times (no caching for list)
        await _inner.Received(2).ListSecretsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSecretsAsync_ReturnsInnerResult()
    {
        // Arrange
        IEnumerable<string> names = ["secret-a", "secret-b"];
        var sut = CreateSut();

#pragma warning disable CA2012
        _inner.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IEnumerable<string>>>(
                Either<EncinaError, IEnumerable<string>>.Right(names)));
#pragma warning restore CA2012

        // Act
        var result = await sut.ListSecretsAsync();

        // Assert
        var list = result.ShouldBeSuccess();
        list.ShouldBe(names);
    }

    // ---------------------------------------------------------------------------
    // Disabled caching – all operations pass directly through to inner
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSecretAsync_CachingDisabled_AlwaysDelegatesToInner()
    {
        // Arrange
        var expected = MakeSecret();
        var sut = CreateSut(new SecretCacheOptions { Enabled = false });

#pragma warning disable CA2012
        _inner.GetSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act – two calls
        await sut.GetSecretAsync(SecretName);
        await sut.GetSecretAsync(SecretName);

        // Assert – inner called both times regardless of cache
        await _inner.Received(2).GetSecretAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretVersionAsync_CachingDisabled_AlwaysDelegatesToInner()
    {
        // Arrange
        var expected = MakeSecret(version: SecretVersion);
        var sut = CreateSut(new SecretCacheOptions { Enabled = false });

#pragma warning disable CA2012
        _inner.GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act – two calls
        await sut.GetSecretVersionAsync(SecretName, SecretVersion);
        await sut.GetSecretVersionAsync(SecretName, SecretVersion);

        // Assert
        await _inner.Received(2).GetSecretVersionAsync(SecretName, SecretVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_CachingDisabled_AlwaysDelegatesToInner()
    {
        // Arrange
        var sut = CreateSut(new SecretCacheOptions { Enabled = false });

#pragma warning disable CA2012
        _inner.ExistsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(Either<EncinaError, bool>.Right(true)));
#pragma warning restore CA2012

        // Act – two calls
        await sut.ExistsAsync(SecretName);
        await sut.ExistsAsync(SecretName);

        // Assert
        await _inner.Received(2).ExistsAsync(SecretName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_CachingDisabled_DoesNotInvalidateCacheButStillDelegatesToInner()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var metadata = MakeMetadata();
        var sut = CreateSut(new SecretCacheOptions { Enabled = false });

        // Pre-populate cache directly
        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, SecretMetadata>>(Either<EncinaError, SecretMetadata>.Right(metadata)));
#pragma warning restore CA2012

        // Act
        await sut.SetSecretAsync(SecretName, "new-value");

        // Assert – inner is still called even when caching is disabled
        await _inner.Received(1).SetSecretAsync(SecretName, "new-value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>());

        // Assert – cache NOT invalidated (disabled path skips invalidation)
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSecretAsync_CachingDisabled_DoesNotInvalidateCacheButStillDelegatesToInner()
    {
        // Arrange
        var existingSecret = MakeSecret();
        var sut = CreateSut(new SecretCacheOptions { Enabled = false });

        _cache.Set("encina:secrets:" + SecretName, existingSecret, TimeSpan.FromMinutes(5));

#pragma warning disable CA2012
        _inner.DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));
#pragma warning restore CA2012

        // Act
        await sut.DeleteSecretAsync(SecretName);

        // Assert – inner is still called
        await _inner.Received(1).DeleteSecretAsync(SecretName, Arg.Any<CancellationToken>());

        // Assert – cache NOT invalidated
        var cacheHit = _cache.TryGetValue("encina:secrets:" + SecretName, out Secret? _);
        cacheHit.ShouldBeTrue();
    }
}
