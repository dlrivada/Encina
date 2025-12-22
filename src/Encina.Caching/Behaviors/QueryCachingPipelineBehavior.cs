using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

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
/// </remarks>
public sealed partial class QueryCachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly CachingOptions _options;
    private readonly ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> _logger;
    private static readonly CacheAttribute? CacheAttribute = typeof(TRequest).GetCustomAttribute<CacheAttribute>();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider.</param>
    /// <param name="keyGenerator">The cache key generator.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    public QueryCachingPipelineBehavior(
        ICacheProvider cacheProvider,
        ICacheKeyGenerator keyGenerator,
        IOptions<CachingOptions> options,
        ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _keyGenerator = keyGenerator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Check if caching is enabled and request has [Cache] attribute
        if (!_options.EnableQueryCaching || CacheAttribute is null)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var cacheKey = _keyGenerator.GenerateKey<TRequest, TResponse>(request, context);

        try
        {
            // Try to get from cache
            var cached = await _cacheProvider.GetAsync<CacheEntry<TResponse>>(cacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (cached is not null)
            {
                LogCacheHit(_logger, typeof(TRequest).Name, cacheKey, context.CorrelationId);

                // Handle sliding expiration refresh
                if (CacheAttribute.SlidingExpiration)
                {
                    _ = _cacheProvider.RefreshAsync(cacheKey, cancellationToken);
                }

                return cached.Value;
            }

            LogCacheMiss(_logger, typeof(TRequest).Name, cacheKey, context.CorrelationId);
        }
        catch (Exception ex)
        {
            LogCacheError(_logger, typeof(TRequest).Name, cacheKey, ex);

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }

            // Continue without cache on error
        }

        // Execute handler
        var result = await nextStep().ConfigureAwait(false);

        // Cache successful responses only
        if (result.IsRight)
        {
            try
            {
                var entry = new CacheEntry<TResponse>
                {
                    Value = result.Match(
                        Right: v => v,
                        Left: _ => default!),
                    CachedAtUtc = DateTime.UtcNow
                };

                if (CacheAttribute.SlidingExpiration)
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

                LogCacheSet(_logger, typeof(TRequest).Name, cacheKey, CacheAttribute.DurationSeconds, context.CorrelationId);
            }
            catch (Exception ex)
            {
                LogCacheError(_logger, typeof(TRequest).Name, cacheKey, ex);

                if (_options.ThrowOnCacheErrors)
                {
                    throw;
                }
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Cache hit for {RequestType} with key {CacheKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheHit(
        ILogger logger,
        string requestType,
        string cacheKey,
        string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Cache miss for {RequestType} with key {CacheKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheMiss(
        ILogger logger,
        string requestType,
        string cacheKey,
        string correlationId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Cached {RequestType} with key {CacheKey} for {Duration}s (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheSet(
        ILogger logger,
        string requestType,
        string cacheKey,
        int duration,
        string correlationId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Cache error for {RequestType} with key {CacheKey}")]
    private static partial void LogCacheError(
        ILogger logger,
        string requestType,
        string cacheKey,
        Exception exception);
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
