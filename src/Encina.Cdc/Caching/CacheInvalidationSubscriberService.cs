using Encina.Caching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Cdc.Caching;

/// <summary>
/// Hosted service that subscribes to the cache invalidation pub/sub channel and
/// invalidates local cache entries when messages are received from other instances.
/// </summary>
/// <remarks>
/// <para>
/// This service is the receiving side of the CDC-driven cache invalidation broadcast.
/// When <see cref="QueryCacheInvalidationCdcHandler"/> detects a database change and
/// publishes an invalidation pattern to the pub/sub channel, this subscriber receives
/// the pattern and calls <see cref="ICacheProvider.RemoveByPatternAsync"/> to invalidate
/// matching cache entries on this instance.
/// </para>
/// <para>
/// The service is only registered when <see cref="QueryCacheInvalidationOptions.UsePubSubBroadcast"/>
/// is enabled and an <see cref="IPubSubProvider"/> is available.
/// </para>
/// </remarks>
internal sealed class CacheInvalidationSubscriberService : IHostedService
{
    private readonly IPubSubProvider _pubSubProvider;
    private readonly ICacheProvider _cacheProvider;
    private readonly QueryCacheInvalidationOptions _options;
    private readonly ILogger<CacheInvalidationSubscriberService> _logger;
    private IAsyncDisposable? _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheInvalidationSubscriberService"/> class.
    /// </summary>
    /// <param name="pubSubProvider">The pub/sub provider for receiving invalidation messages.</param>
    /// <param name="cacheProvider">The cache provider for local cache invalidation.</param>
    /// <param name="options">Configuration options for cache invalidation behavior.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CacheInvalidationSubscriberService(
        IPubSubProvider pubSubProvider,
        ICacheProvider cacheProvider,
        IOptions<QueryCacheInvalidationOptions> options,
        ILogger<CacheInvalidationSubscriberService> logger)
    {
        ArgumentNullException.ThrowIfNull(pubSubProvider);
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _pubSubProvider = pubSubProvider;
        _cacheProvider = cacheProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CdcCacheInvalidationLog.SubscriberStarting(_logger, _options.PubSubChannel);

        _subscription = await _pubSubProvider.SubscribeAsync(
            _options.PubSubChannel,
            async message =>
            {
                CdcCacheInvalidationLog.SubscriberReceivedInvalidation(_logger, message, _options.PubSubChannel);

                try
                {
                    await _cacheProvider.RemoveByPatternAsync(message, CancellationToken.None)
                        .ConfigureAwait(false);

                    CdcCacheInvalidationLog.SubscriberInvalidatedCache(_logger, message);
                }
                catch (Exception ex)
                {
                    CdcCacheInvalidationLog.SubscriberInvalidationFailed(_logger, ex, message);
                }
            },
            cancellationToken).ConfigureAwait(false);

        CdcCacheInvalidationLog.SubscriberStarted(_logger, _options.PubSubChannel);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscription is not null)
        {
            await _subscription.DisposeAsync().ConfigureAwait(false);
            _subscription = null;
        }

        CdcCacheInvalidationLog.SubscriberStopped(_logger, _options.PubSubChannel);
    }
}
