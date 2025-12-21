using System.Text.Json;

namespace SimpleMediator.Caching.Redis;

/// <summary>
/// Redis implementation of <see cref="ICacheProvider"/> using StackExchange.Redis.
/// </summary>
/// <remarks>
/// <para>
/// This provider is ideal for:
/// </para>
/// <list type="bullet">
/// <item><description>Multi-instance applications requiring distributed cache</description></item>
/// <item><description>Production deployments with high availability requirements</description></item>
/// <item><description>Scenarios requiring cache sharing across services</description></item>
/// </list>
/// <para>
/// This provider is wire-compatible with Redis, Garnet, Valkey, Dragonfly, and KeyDB.
/// </para>
/// </remarks>
public sealed partial class RedisCacheProvider : ICacheProvider
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheProvider"/> class.
    /// </summary>
    /// <param name="connection">The Redis connection multiplexer.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    public RedisCacheProvider(
        IConnectionMultiplexer connection,
        IOptions<RedisCacheOptions> options,
        ILogger<RedisCacheProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private IDatabase Database => _connection.GetDatabase(_options.Database);

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        var value = await Database.StringGetAsync(prefixedKey).ConfigureAwait(false);

        if (value.IsNullOrEmpty)
        {
            LogCacheMiss(_logger, prefixedKey);
            return default;
        }

        LogCacheHit(_logger, prefixedKey);
        return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var effectiveExpiration = expiration ?? _options.DefaultExpiration;

        await Database.StringSetAsync(prefixedKey, json, effectiveExpiration).ConfigureAwait(false);
        LogCacheSet(_logger, prefixedKey, effectiveExpiration);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        await Database.KeyDeleteAsync(prefixedKey).ConfigureAwait(false);
        LogCacheRemove(_logger, prefixedKey);
    }

    /// <inheritdoc/>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedPattern = GetPrefixedKey(pattern);
        var server = GetServer();
        var count = 0;

        await foreach (var key in server.KeysAsync(pattern: prefixedPattern, pageSize: 1000).ConfigureAwait(false))
        {
            await Database.KeyDeleteAsync(key).ConfigureAwait(false);
            count++;
        }

        LogCacheRemovePattern(_logger, prefixedPattern, count);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        return await Database.KeyExistsAsync(prefixedKey).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        // Use a lock to prevent cache stampede
        var lockKey = $"lock:{key}";
        var lockExpiry = TimeSpan.FromSeconds(30);

        var prefixedLockKey = GetPrefixedKey(lockKey);
        var lockValue = Guid.NewGuid().ToString();

        // Try to acquire lock
        var lockAcquired = await Database.StringSetAsync(
            prefixedLockKey,
            lockValue,
            lockExpiry,
            When.NotExists).ConfigureAwait(false);

        if (!lockAcquired)
        {
            // Another process is creating the value, wait and retry
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            var retry = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (retry is not null)
            {
                return retry;
            }
        }

        try
        {
            var value = await factory(cancellationToken).ConfigureAwait(false);
            await SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
            return value;
        }
        finally
        {
            // Release lock only if we own it
            var script = """
                if redis.call("get", KEYS[1]) == ARGV[1] then
                    return redis.call("del", KEYS[1])
                else
                    return 0
                end
                """;
            await Database.ScriptEvaluateAsync(script, [prefixedLockKey], [lockValue]).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        var metadataKey = $"{prefixedKey}:meta";

        var json = JsonSerializer.Serialize(value, _jsonOptions);

        // Store the value with sliding expiration
        await Database.StringSetAsync(prefixedKey, json, slidingExpiration).ConfigureAwait(false);

        // Store metadata for absolute expiration check
        if (absoluteExpiration.HasValue)
        {
            var metadata = new SlidingMetadata
            {
                SlidingExpiration = slidingExpiration,
                AbsoluteExpirationUtc = DateTime.UtcNow.Add(absoluteExpiration.Value)
            };
            var metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
            await Database.StringSetAsync(metadataKey, metadataJson, absoluteExpiration.Value).ConfigureAwait(false);
        }

        LogCacheSetSliding(_logger, prefixedKey, slidingExpiration, absoluteExpiration);
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        var prefixedKey = GetPrefixedKey(key);
        var metadataKey = $"{prefixedKey}:meta";

        // Check for sliding expiration metadata
        var metadataJson = await Database.StringGetAsync(metadataKey).ConfigureAwait(false);
        if (metadataJson.IsNullOrEmpty)
        {
            // No sliding expiration, check if key exists
            return await Database.KeyExistsAsync(prefixedKey).ConfigureAwait(false);
        }

        var metadata = JsonSerializer.Deserialize<SlidingMetadata>((string)metadataJson!, _jsonOptions);
        if (metadata is null || DateTime.UtcNow >= metadata.AbsoluteExpirationUtc)
        {
            // Absolute expiration reached
            return false;
        }

        // Refresh the sliding expiration
        return await Database.KeyExpireAsync(prefixedKey, metadata.SlidingExpiration).ConfigureAwait(false);
    }

    private string GetPrefixedKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? key
            : $"{_options.KeyPrefix}:{key}";
    }

    private IServer GetServer()
    {
        var endpoints = _connection.GetEndPoints();
        return _connection.GetServer(endpoints[0]);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Cache hit for key: {Key}")]
    private static partial void LogCacheHit(ILogger logger, string key);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Cache miss for key: {Key}")]
    private static partial void LogCacheMiss(ILogger logger, string key);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Cache set for key: {Key} with expiration: {Expiration}")]
    private static partial void LogCacheSet(ILogger logger, string key, TimeSpan expiration);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Cache set for key: {Key} with sliding expiration: {SlidingExpiration}, absolute: {AbsoluteExpiration}")]
    private static partial void LogCacheSetSliding(ILogger logger, string key, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Cache remove for key: {Key}")]
    private static partial void LogCacheRemove(ILogger logger, string key);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Cache remove by pattern: {Pattern}, removed {Count} keys")]
    private static partial void LogCacheRemovePattern(ILogger logger, string pattern, int count);

    private sealed class SlidingMetadata
    {
        public TimeSpan SlidingExpiration { get; init; }
        public DateTime AbsoluteExpirationUtc { get; init; }
    }
}

/// <summary>
/// Options for the Redis cache provider.
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the Redis database number to use.
    /// </summary>
    public int Database { get; set; }

    /// <summary>
    /// Gets or sets the key prefix for all cache keys.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
