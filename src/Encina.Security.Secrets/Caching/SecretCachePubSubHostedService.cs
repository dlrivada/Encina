using Encina.Caching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Caching;

/// <summary>
/// Hosted service that subscribes to <see cref="SecretCacheInvalidationMessage"/> events
/// on the configured PubSub channel, enabling cross-instance cache eviction for secrets.
/// </summary>
/// <remarks>
/// <para>
/// This service is only registered when <see cref="SecretCachingOptions.EnablePubSubInvalidation"/>
/// is <c>true</c> and both <see cref="ICacheProvider"/> and <see cref="IPubSubProvider"/> are
/// available in the DI container.
/// </para>
/// <para>
/// On <see cref="StartAsync"/>, it subscribes to the
/// <see cref="SecretCachingOptions.InvalidationChannel"/> using
/// <see cref="IPubSubProvider.SubscribeAsync{T}"/>. When a
/// <see cref="SecretCacheInvalidationMessage"/> is received from another instance,
/// all local secret cache entries are evicted via
/// <see cref="ICacheProvider.RemoveByPatternAsync"/>.
/// </para>
/// <para>
/// <b>Resilience</b>: subscription errors during <see cref="StartAsync"/> are logged at
/// <c>Warning</c> level and swallowed — the application starts without PubSub invalidation
/// rather than failing. Message processing errors are also logged and swallowed to prevent
/// the subscription from terminating.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatically registered by AddEncinaSecrets when:
/// //   options.EnableCaching = true
/// //   options.Caching.EnablePubSubInvalidation = true
/// //   IPubSubProvider is registered in DI
/// </code>
/// </example>
internal sealed class SecretCachePubSubHostedService : IHostedService
{
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider? _pubSub;
    private readonly SecretCachingOptions _options;
    private readonly ILogger<SecretCachePubSubHostedService> _logger;
    private IAsyncDisposable? _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretCachePubSubHostedService"/> class.
    /// </summary>
    /// <param name="cache">The cache provider for local cache eviction.</param>
    /// <param name="pubSub">
    /// The PubSub provider for subscribing to invalidation messages.
    /// When <c>null</c>, the hosted service starts as a no-op (logs a warning on startup).
    /// </param>
    /// <param name="options">The secrets caching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public SecretCachePubSubHostedService(
        ICacheProvider cache,
        IPubSubProvider? pubSub,
        SecretCachingOptions options,
        ILogger<SecretCachePubSubHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _pubSub = pubSub;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_pubSub is null)
        {
            Log.PubSubSubscriptionFailed(_logger, _options.InvalidationChannel,
                new InvalidOperationException("IPubSubProvider is not registered — cross-instance cache invalidation disabled."));
            return;
        }

        try
        {
            _subscription = await _pubSub.SubscribeAsync<SecretCacheInvalidationMessage>(
                _options.InvalidationChannel,
                HandleInvalidationMessageAsync,
                cancellationToken).ConfigureAwait(false);

            Log.PubSubSubscriptionStarted(_logger, _options.InvalidationChannel);
        }
        catch (Exception ex)
        {
            // Don't fail application startup — PubSub is a best-effort enhancement
            Log.PubSubSubscriptionFailed(_logger, _options.InvalidationChannel, ex);
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscription is not null)
        {
            try
            {
                await _subscription.DisposeAsync().ConfigureAwait(false);
                _subscription = null;

                Log.PubSubSubscriptionStopped(_logger, _options.InvalidationChannel);
            }
            catch (Exception ex)
            {
                Log.PubSubSubscriptionStopError(_logger, _options.InvalidationChannel, ex);
            }
        }
    }

    /// <summary>
    /// Handles an incoming invalidation message by evicting the affected secret cache entries.
    /// </summary>
    private async Task HandleInvalidationMessageAsync(SecretCacheInvalidationMessage message)
    {
        try
        {
            Log.PubSubInvalidationReceived(
                _logger, message.SecretName, message.Operation, _options.InvalidationChannel);

            // Evict cache entries for the affected secret using pattern match
            var pattern = message.SecretName == "*"
                ? $"{_options.CacheKeyPrefix}:*"
                : $"{_options.CacheKeyPrefix}:*:{message.SecretName}*";

            await _cache.RemoveByPatternAsync(pattern, CancellationToken.None).ConfigureAwait(false);

            Log.CacheBulkInvalidated(_logger, pattern);
        }
        catch (Exception ex)
        {
            Log.CacheEvictionError(_logger, message.SecretName, message.Operation, ex);
        }
    }

}
