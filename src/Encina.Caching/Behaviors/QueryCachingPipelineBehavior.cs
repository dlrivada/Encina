using System.Reflection;
using Encina.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching;

/// <summary>
/// Pipeline behavior that implements query caching using the <see cref="CacheAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts requests marked with <see cref="CacheAttribute"/> and:
/// </para>
/// <list type="number">
/// <item><description>Generates a cache key based on the request and context</description></item>
/// <item><description>Checks if a cached response exists</description></item>
/// <item><description>If cached, returns the cached response</description></item>
/// <item><description>If not cached, executes the handler and caches successful responses</description></item>
/// </list>
/// <para>
/// Only successful responses (Right side of Either) are cached. Error responses are not cached.
/// </para>
/// <para>
/// A request marked <see cref="CacheAttribute.VaryByUser"/> is cached only for an authenticated
/// <see cref="IdentityKind.User"/>. Any other identity (anonymous, a service) bypasses the cache with
/// no read and no write, and logs a Debug message (EventId 3512) with the identity kind only.
/// </para>
/// <para>
/// The identity is read once per request and the key is built from that snapshot. Because an
/// identity reads as anonymous once its scope ends, a <c>VaryByUser</c> entry is read, served or
/// written only while the context still reports that same identity; a request whose scope ended
/// meanwhile runs its handler without the cache, so no response is ever shared across callers.
/// </para>
/// </remarks>
public sealed partial class QueryCachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly CachingOptions _options;
    private readonly ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly TimeProvider _timeProvider;
    private static readonly CacheAttribute? CacheAttribute = typeof(TRequest).GetCustomAttribute<CacheAttribute>();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider.</param>
    /// <param name="keyGenerator">The cache key generator.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public QueryCachingPipelineBehavior(
        ICacheProvider cacheProvider,
        ICacheKeyGenerator keyGenerator,
        IOptions<CachingOptions> options,
        ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _keyGenerator = keyGenerator;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // The identity is read once: it reads Anonymous as soon as its scope ends (#1892), so the
        // bypass decision and the key are taken from this snapshot, never from a second read.
        var identity = context.Identity;
        if (!ShouldUseCache(identity))
        {
            return await nextStep().ConfigureAwait(false);
        }

        var cacheKey = _keyGenerator.GenerateKey<TRequest, TResponse>(request, KeyContext(context, identity));

        var (found, cachedValue) = await TryGetForCallerAsync(cacheKey, context, identity, cancellationToken).ConfigureAwait(false);
        if (found)
        {
            return cachedValue!;
        }

        var result = await nextStep().ConfigureAwait(false);

        // Cache successful responses only, and never a response produced after the user's scope ended.
        if (result.IsRight && IsSameCaller(context, identity))
        {
            await TryCacheResultAsync(result, cacheKey, context.CorrelationId, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    // Caching is enabled, the request has [Cache], and a per-user entry has a user: any other
    // identity bypasses the cache (no read, no write).
    private bool ShouldUseCache(RequestIdentity? identity)
    {
        if (!_options.EnableQueryCaching || CacheAttribute is null)
        {
            return false;
        }

        if (!CacheAttribute.VaryByUser || CacheUserIdentity.IsUser(identity))
        {
            return true;
        }

        LogVaryByUserBypassed(_logger, typeof(TRequest).Name, identity?.Kind ?? IdentityKind.Anonymous);
        return false;
    }

    // Reads the entry only while the caller is still the identity the key was built from, and does
    // not serve a hit read after the scope ended: the caller is no longer that user.
    private async ValueTask<(bool Found, TResponse? Value)> TryGetForCallerAsync(
        string cacheKey,
        IRequestContext context,
        RequestIdentity? identity,
        CancellationToken cancellationToken)
    {
        if (!IsSameCaller(context, identity))
        {
            return (false, default);
        }

        var cached = await TryGetFromCacheAsync(cacheKey, context.CorrelationId, cancellationToken).ConfigureAwait(false);
        if (!cached.Found || !IsSameCaller(context, identity))
        {
            return (false, default);
        }

        // Only a hit that is served is logged and extends a sliding entry.
        LogCacheHit(_logger, typeof(TRequest).Name, cacheKey, context.CorrelationId);
        if (CacheAttribute!.SlidingExpiration)
        {
            _ = _cacheProvider.RefreshAsync(cacheKey, cancellationToken);
        }

        return cached;
    }

    // A VaryByUser key is built from the pinned snapshot, so a custom generator reads the same identity.
    private static IRequestContext KeyContext(IRequestContext context, RequestIdentity? identity) =>
        CacheAttribute!.VaryByUser ? new IdentityPinnedRequestContext(context, identity!) : context;

    // A VaryByUser entry is read or written only while the live context still reports the very
    // identity the key was built from (the same instance: an ended scope reads Anonymous).
    private static bool IsSameCaller(IRequestContext context, RequestIdentity? identity) =>
        !CacheAttribute!.VaryByUser || ReferenceEquals(context.Identity, identity);

    private async ValueTask<(bool Found, TResponse? Value)> TryGetFromCacheAsync(
        string cacheKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cacheProvider.GetAsync<CacheEntry<TResponse>>(cacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached is null)
            {
                LogCacheMiss(_logger, typeof(TRequest).Name, cacheKey, correlationId);
                return (false, default);
            }

            return (true, cached.Value);
        }
        catch (Exception ex)
        {
            LogCacheError(_logger, typeof(TRequest).Name, cacheKey, ex.ForLogging());

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }

            return (false, default);
        }
    }

    private async Task TryCacheResultAsync(
        Either<EncinaError, TResponse> result,
        string cacheKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = new CacheEntry<TResponse>
            {
                Value = result.Match(Right: v => v, Left: _ => default!),
                CachedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            };

            await StoreCacheEntryAsync(entry, cacheKey, cancellationToken).ConfigureAwait(false);

            LogCacheSet(_logger, typeof(TRequest).Name, cacheKey, CacheAttribute!.DurationSeconds, correlationId);
        }
        catch (Exception ex)
        {
            LogCacheError(_logger, typeof(TRequest).Name, cacheKey, ex.ForLogging());

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }
        }
    }

    private async Task StoreCacheEntryAsync(
        CacheEntry<TResponse> entry,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        if (CacheAttribute!.SlidingExpiration)
        {
            await _cacheProvider.SetWithSlidingExpirationAsync(
                cacheKey,
                entry,
                CacheAttribute.Duration,
                CacheAttribute.MaxAbsoluteExpiration,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _cacheProvider.SetAsync(
                cacheKey,
                entry,
                CacheAttribute.Duration,
                cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(
        EventId = 3508,
        Level = LogLevel.Debug,
        Message = "Cache hit for {RequestType} with key {CacheKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheHit(
        ILogger logger,
        string requestType,
        string cacheKey,
        string correlationId);

    [LoggerMessage(
        EventId = 3509,
        Level = LogLevel.Debug,
        Message = "Cache miss for {RequestType} with key {CacheKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheMiss(
        ILogger logger,
        string requestType,
        string cacheKey,
        string correlationId);

    [LoggerMessage(
        EventId = 3510,
        Level = LogLevel.Debug,
        Message = "Cached {RequestType} with key {CacheKey} for {Duration}s (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheSet(
        ILogger logger,
        string requestType,
        string cacheKey,
        int duration,
        string correlationId);

    [LoggerMessage(
        EventId = 3511,
        Level = LogLevel.Warning,
        Message = "Cache error for {RequestType} with key {CacheKey}")]
    private static partial void LogCacheError(
        ILogger logger,
        string requestType,
        string cacheKey,
        Exception exception);

    [LoggerMessage(
        EventId = 3512,
        Level = LogLevel.Debug,
        Message = "{RequestType} varies by user but the request identity is {IdentityKind}, not an authenticated user; the cache is bypassed (no read, no write)")]
    private static partial void LogVaryByUserBypassed(
        ILogger logger,
        string requestType,
        IdentityKind identityKind);
}

/// <summary>
/// Wrapper for cached values with metadata.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
internal sealed class CacheEntry<T>
{
    /// <summary>
    /// Gets or sets the cached value.
    /// </summary>
    public required T Value { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the value was cached.
    /// </summary>
    public required DateTime CachedAtUtc { get; init; }
}
