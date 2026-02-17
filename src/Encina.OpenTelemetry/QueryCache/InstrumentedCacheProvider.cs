using System.Diagnostics;
using Encina.Caching;

namespace Encina.OpenTelemetry.QueryCache;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="ICacheProvider"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner cache provider and creates <see cref="Activity"/> spans for all cache operations
/// (get, set, remove, exists, get-or-set, sliding expiration, refresh). All activity creation
/// is guarded by <see cref="ActivitySource.HasListeners()"/> for zero-cost when no trace
/// collector is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.QueryCache"</c> must be registered with the OpenTelemetry
/// tracer, which is done automatically by <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedCacheProvider : ICacheProvider
{
    private static readonly ActivitySource Source = new("Encina.QueryCache", "1.0");

    private readonly ICacheProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedCacheProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner cache provider to decorate.</param>
    public InstrumentedCacheProvider(ICacheProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        using var activity = StartGet(key);

        try
        {
            var result = await _inner.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);

            if (result is not null)
            {
                CompleteWithOutcome(activity, "hit");
            }
            else
            {
                CompleteWithOutcome(activity, "miss");
            }

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        using var activity = StartSet(key);

        try
        {
            await _inner.SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        using var activity = StartRemove(key);

        try
        {
            await _inner.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        using var activity = StartRemoveByPattern(pattern);

        try
        {
            await _inner.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        using var activity = StartExists(key);

        try
        {
            var exists = await _inner.ExistsAsync(key, cancellationToken).ConfigureAwait(false);
            CompleteWithOutcome(activity, exists ? "hit" : "miss");
            return exists;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        using var activity = StartGetOrSet(key);

        try
        {
            var result = await _inner.GetOrSetAsync(key, factory, expiration, cancellationToken)
                .ConfigureAwait(false);
            Complete(activity);
            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken)
    {
        using var activity = StartSetSliding(key);

        try
        {
            await _inner.SetWithSlidingExpirationAsync(key, value, slidingExpiration, absoluteExpiration, cancellationToken)
                .ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
    {
        using var activity = StartRefresh(key);

        try
        {
            var refreshed = await _inner.RefreshAsync(key, cancellationToken).ConfigureAwait(false);
            CompleteWithOutcome(activity, refreshed ? "hit" : "miss");
            return refreshed;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    #region Activity Helpers

    private static Activity? StartGet(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.get", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartSet(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.set", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartRemove(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.remove", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartRemoveByPattern(string pattern)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.remove_by_pattern", ActivityKind.Internal);
        activity?.SetTag("querycache.pattern", pattern);
        return activity;
    }

    private static Activity? StartExists(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.exists", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartGetOrSet(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.get_or_set", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartSetSliding(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.set_sliding", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static Activity? StartRefresh(string key)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.querycache.refresh", ActivityKind.Internal);
        activity?.SetTag("querycache.key", key);
        return activity;
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteWithOutcome(Activity? activity, string outcome)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("querycache.outcome", outcome);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    #endregion
}
