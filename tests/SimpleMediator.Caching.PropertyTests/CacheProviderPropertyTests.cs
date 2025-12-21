using FsCheck;
using FsCheck.Xunit;
using MemoryProviderOptions = SimpleMediator.Caching.Memory.MemoryCacheOptions;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace SimpleMediator.Caching.PropertyTests;

/// <summary>
/// Property-based tests for ICacheProvider that verify invariants hold for all inputs.
/// </summary>
public sealed class CacheProviderPropertyTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly MemoryCacheProvider _provider;
    private bool _disposed;

    public CacheProviderPropertyTests()
    {
        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MemoryCache(memoryCacheOptions);

        var providerOptions = Options.Create(new MemoryProviderOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        _provider = new MemoryCacheProvider(_memoryCache, providerOptions, NullLogger<MemoryCacheProvider>.Instance);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            _disposed = true;
        }
    }

    #region Get/Set Invariants

    [Property(MaxTest = 100)]
    public bool SetThenGet_AlwaysReturnsWhatWasSet(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"setget-{keySeed.Get}");
        var valueStr = value.Get;

        _provider.SetAsync(keyStr, valueStr, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        var retrieved = _provider.GetAsync<string>(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return retrieved == valueStr;
    }

    [Property(MaxTest = 100)]
    public bool SetThenRemove_GetReturnsNull(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"setremove-{keySeed.Get}");
        var valueStr = value.Get;

        _provider.SetAsync(keyStr, valueStr, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        _provider.RemoveAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        var retrieved = _provider.GetAsync<string>(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return retrieved == null;
    }

    [Property(MaxTest = 100)]
    public bool SetOverwritesPreviousValue(PositiveInt keySeed, NonEmptyString value1, NonEmptyString value2)
    {
        var keyStr = SanitizeKey($"overwrite-{keySeed.Get}");

        _provider.SetAsync(keyStr, value1.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        _provider.SetAsync(keyStr, value2.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        var retrieved = _provider.GetAsync<string>(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return retrieved == value2.Get;
    }

    #endregion

    #region Exists Invariants

    [Property(MaxTest = 100)]
    public bool Exists_AfterSet_ReturnsTrue(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"existsset-{keySeed.Get}");

        _provider.SetAsync(keyStr, value.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        var exists = _provider.ExistsAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return exists;
    }

    [Property(MaxTest = 100)]
    public bool Exists_AfterRemove_ReturnsFalse(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"existsremove-{keySeed.Get}");

        _provider.SetAsync(keyStr, value.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        _provider.RemoveAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        var exists = _provider.ExistsAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return !exists;
    }

    [Property(MaxTest = 100)]
    public bool Exists_ForNonExistentKey_ReturnsFalse(PositiveInt keySeed)
    {
        var keyStr = SanitizeKey($"nonexistent-{keySeed.Get}");

        var exists = _provider.ExistsAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return !exists;
    }

    #endregion

    #region GetOrSet Invariants

    [Property(MaxTest = 100)]
    public bool GetOrSet_WhenKeyExists_DoesNotCallFactory(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"getorsethit-{keySeed.Get}");
        var factoryCalled = false;

        _provider.SetAsync(keyStr, value.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        _provider.GetOrSetAsync(
            keyStr,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        return !factoryCalled;
    }

    [Property(MaxTest = 100)]
    public bool GetOrSet_WhenKeyDoesNotExist_CallsFactory(PositiveInt keySeed)
    {
        var keyStr = SanitizeKey($"getorsetmiss-{keySeed.Get}");
        var factoryCalled = false;

        _provider.GetOrSetAsync(
            keyStr,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        return factoryCalled;
    }

    [Property(MaxTest = 100)]
    public bool GetOrSet_ReturnsExistingValueWhenPresent(PositiveInt keySeed, NonEmptyString existingValue)
    {
        var keyStr = SanitizeKey($"getorsetexisting-{keySeed.Get}");

        _provider.SetAsync(keyStr, existingValue.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        var result = _provider.GetOrSetAsync(
            keyStr,
            _ => Task.FromResult("factory-value"),
            TimeSpan.FromMinutes(5),
            CancellationToken.None).GetAwaiter().GetResult();

        return result == existingValue.Get;
    }

    #endregion

    #region Refresh Invariants

    [Property(MaxTest = 100)]
    public bool Refresh_WhenKeyExists_ReturnsTrue(PositiveInt keySeed, NonEmptyString value)
    {
        var keyStr = SanitizeKey($"refreshexists-{keySeed.Get}");

        _provider.SetAsync(keyStr, value.Get, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();

        var refreshed = _provider.RefreshAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return refreshed;
    }

    [Property(MaxTest = 100)]
    public bool Refresh_WhenKeyDoesNotExist_ReturnsFalse(PositiveInt keySeed)
    {
        var keyStr = SanitizeKey($"refreshmissing-{keySeed.Get}");

        var refreshed = _provider.RefreshAsync(keyStr, CancellationToken.None)
            .GetAwaiter().GetResult();

        return !refreshed;
    }

    #endregion

    private static string SanitizeKey(string key)
    {
        return $"test-{Guid.NewGuid():N}-{key}";
    }
}
