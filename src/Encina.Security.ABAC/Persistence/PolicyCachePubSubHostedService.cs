using Encina.Caching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Hosted service that subscribes to <see cref="PolicyCacheInvalidationMessage"/> events
/// on the configured PubSub channel, enabling cross-instance cache eviction for ABAC policies.
/// </summary>
/// <remarks>
/// <para>
/// This service is only registered when <see cref="PolicyCachingOptions.EnablePubSubInvalidation"/>
/// is <c>true</c> and both <see cref="ICacheProvider"/> and <see cref="IPubSubProvider"/> are
/// available in the DI container.
/// </para>
/// <para>
/// On <see cref="StartAsync"/>, it subscribes to the
/// <see cref="PolicyCachingOptions.InvalidationChannel"/> using
/// <see cref="IPubSubProvider.SubscribeAsync{T}"/>. When a
/// <see cref="PolicyCacheInvalidationMessage"/> is received from another instance,
/// all local ABAC policy cache entries are evicted via
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
/// // Automatically registered by AddEncinaABAC when:
/// //   options.UsePersistentPAP = true
/// //   options.PolicyCaching.Enabled = true
/// //   options.PolicyCaching.EnablePubSubInvalidation = true
/// //   IPubSubProvider is registered in DI
/// </code>
/// </example>
internal sealed partial class PolicyCachePubSubHostedService : IHostedService
{
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider _pubSub;
    private readonly PolicyCachingOptions _options;
    private readonly ILogger<PolicyCachePubSubHostedService> _logger;
    private IAsyncDisposable? _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyCachePubSubHostedService"/> class.
    /// </summary>
    /// <param name="cache">The cache provider for local cache eviction.</param>
    /// <param name="pubSub">The PubSub provider for subscribing to invalidation messages.</param>
    /// <param name="options">The policy caching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public PolicyCachePubSubHostedService(
        ICacheProvider cache,
        IPubSubProvider pubSub,
        PolicyCachingOptions options,
        ILogger<PolicyCachePubSubHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(pubSub);
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
        try
        {
            _subscription = await _pubSub.SubscribeAsync<PolicyCacheInvalidationMessage>(
                _options.InvalidationChannel,
                HandleInvalidationMessageAsync,
                cancellationToken).ConfigureAwait(false);

            LogSubscriptionStarted(_logger, _options.InvalidationChannel);
        }
        catch (Exception ex)
        {
            // Don't fail application startup — PubSub is a best-effort enhancement
            LogSubscriptionError(_logger, _options.InvalidationChannel, ex);
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

                LogSubscriptionStopped(_logger, _options.InvalidationChannel);
            }
            catch (Exception ex)
            {
                LogSubscriptionStopError(_logger, _options.InvalidationChannel, ex);
            }
        }
    }

    /// <summary>
    /// Handles an incoming invalidation message by evicting all ABAC policy cache entries.
    /// </summary>
    private async Task HandleInvalidationMessageAsync(PolicyCacheInvalidationMessage message)
    {
        try
        {
            LogInvalidationReceived(
                _logger, message.EntityType, message.EntityId ?? "*",
                message.Operation, _options.InvalidationChannel);

            // Evict all ABAC policy cache entries using pattern match
            var pattern = $"{_options.CacheKeyPrefix}:*";
            await _cache.RemoveByPatternAsync(pattern, CancellationToken.None).ConfigureAwait(false);

            LogCacheEvicted(_logger, pattern);
        }
        catch (Exception ex)
        {
            LogCacheEvictionError(_logger, message.EntityType, message.EntityId ?? "*", ex);
        }
    }

    // ── LoggerMessage Source Generators ─────────────────────────────

    [LoggerMessage(
        EventId = 1, Level = LogLevel.Information,
        Message = "ABAC policy PubSub subscription started on channel {Channel}")]
    private static partial void LogSubscriptionStarted(
        ILogger logger, string channel);

    [LoggerMessage(
        EventId = 2, Level = LogLevel.Warning,
        Message = "ABAC policy PubSub subscription failed to start on channel {Channel}")]
    private static partial void LogSubscriptionError(
        ILogger logger, string channel, Exception exception);

    [LoggerMessage(
        EventId = 3, Level = LogLevel.Information,
        Message = "ABAC policy PubSub subscription stopped on channel {Channel}")]
    private static partial void LogSubscriptionStopped(
        ILogger logger, string channel);

    [LoggerMessage(
        EventId = 4, Level = LogLevel.Warning,
        Message = "ABAC policy PubSub subscription stop error on channel {Channel}")]
    private static partial void LogSubscriptionStopError(
        ILogger logger, string channel, Exception exception);

    [LoggerMessage(
        EventId = 5, Level = LogLevel.Debug,
        Message = "ABAC policy cache invalidation received for {EntityType}:{EntityId} (operation: {Operation}) on channel {Channel}")]
    private static partial void LogInvalidationReceived(
        ILogger logger, string entityType, string entityId, string operation, string channel);

    [LoggerMessage(
        EventId = 6, Level = LogLevel.Debug,
        Message = "ABAC policy cache evicted by pattern {Pattern}")]
    private static partial void LogCacheEvicted(
        ILogger logger, string pattern);

    [LoggerMessage(
        EventId = 7, Level = LogLevel.Warning,
        Message = "ABAC policy cache eviction error for {EntityType}:{EntityId}")]
    private static partial void LogCacheEvictionError(
        ILogger logger, string entityType, string entityId, Exception exception);
}
