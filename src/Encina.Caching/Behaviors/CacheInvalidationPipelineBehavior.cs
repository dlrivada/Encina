using System.Reflection;
using Encina.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching;

/// <summary>
/// Pipeline behavior that invalidates cache entries after successful command execution.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior intercepts requests marked with <see cref="InvalidatesCacheAttribute"/> and:
/// </para>
/// <list type="number">
/// <item><description>Executes the handler first</description></item>
/// <item><description>If successful, invalidates cache entries matching the patterns</description></item>
/// <item><description>Optionally broadcasts invalidation to other instances via Pub/Sub</description></item>
/// </list>
/// <para>
/// Invalidation only occurs for successful responses (Right side of Either).
/// Failed commands do not trigger cache invalidation.
/// </para>
/// <para>
/// When multi-tenancy is enabled (the <see cref="MultiTenancyMarker"/> is registered) and the request
/// context carries no tenant, the invalidation is skipped and logged (EventId 3514, request type only),
/// so it never removes the entries of every tenant. Without multi-tenancy the behavior is unchanged.
/// </para>
/// </remarks>
public sealed partial class CacheInvalidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IPubSubProvider? _pubSubProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly CachingOptions _options;
    private readonly bool _multiTenant;
    private readonly ILogger<CacheInvalidationPipelineBehavior<TRequest, TResponse>> _logger;
    private static readonly InvalidatesCacheAttribute[] InvalidationAttributes =
        typeof(TRequest).GetCustomAttributes<InvalidatesCacheAttribute>(true).ToArray();

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheInvalidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider.</param>
    /// <param name="keyGenerator">The cache key generator.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="pubSubProvider">The optional pub/sub provider for cross-instance invalidation.</param>
    /// <param name="multiTenancy">
    /// The multi-tenancy marker (registered by <c>AddEncinaTenancy</c>); when present, an invalidation
    /// whose request context carries no tenant is skipped.
    /// </param>
    public CacheInvalidationPipelineBehavior(
        ICacheProvider cacheProvider,
        ICacheKeyGenerator keyGenerator,
        IOptions<CachingOptions> options,
        ILogger<CacheInvalidationPipelineBehavior<TRequest, TResponse>> logger,
        IPubSubProvider? pubSubProvider = null,
        MultiTenancyMarker? multiTenancy = null)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _keyGenerator = keyGenerator;
        _options = options.Value;
        _logger = logger;
        _pubSubProvider = pubSubProvider;
        _multiTenant = multiTenancy is not null;
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

        // Check if invalidation is enabled and request has [InvalidatesCache] attributes
        if (!_options.EnableCacheInvalidation || InvalidationAttributes.Length == 0)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Execute handler first
        var result = await nextStep().ConfigureAwait(false);

        // Only invalidate on success
        if (result.IsLeft)
        {
            return result;
        }

        // Process each invalidation attribute
        foreach (var attr in InvalidationAttributes)
        {
            try
            {
                await InvalidatePatternAsync(request, context, attr, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogInvalidationError(_logger, typeof(TRequest).Name, attr.KeyPattern, ex.ForLogging());

                if (_options.ThrowOnCacheErrors)
                {
                    throw;
                }
            }
        }

        return result;
    }

    private async Task InvalidatePatternAsync(
        TRequest request,
        IRequestContext context,
        InvalidatesCacheAttribute attr,
        CancellationToken cancellationToken)
    {
        // Fail closed for the cache: with multi-tenancy on, a pattern built without a tenant is not
        // scoped to the caller's tenant and would remove entries of every tenant, so nothing is removed.
        if (_multiTenant && string.IsNullOrWhiteSpace(context.TenantId))
        {
            LogTenantMissingSkipped(_logger, typeof(TRequest).Name);
            return;
        }

        // Generate the actual pattern with substituted values
        var pattern = _keyGenerator.GeneratePatternFromTemplate(attr.KeyPattern, request, context);

        // Handle delayed invalidation
        if (attr.DelayMilliseconds > 0)
        {
            _ = InvalidateWithDelayAsync(pattern, attr, context.CorrelationId, cancellationToken);
            return;
        }

        // Immediate invalidation
        await InvalidateCacheAsync(pattern, attr.BroadcastInvalidation, context.CorrelationId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task InvalidateWithDelayAsync(
        string pattern,
        InvalidatesCacheAttribute attr,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(attr.Delay, cancellationToken).ConfigureAwait(false);
        await InvalidateCacheAsync(pattern, attr.BroadcastInvalidation, correlationId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task InvalidateCacheAsync(
        string pattern,
        bool broadcast,
        string correlationId,
        CancellationToken cancellationToken)
    {
        // Local invalidation
        await _cacheProvider.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);

        LogCacheInvalidated(_logger, typeof(TRequest).Name, CacheKeyRedactor.Redact(pattern), correlationId);

        // Cross-instance invalidation via pub/sub
        if (broadcast && _options.EnablePubSubInvalidation && _pubSubProvider is not null)
        {
            await BroadcastInvalidationAsync(_pubSubProvider, pattern, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task BroadcastInvalidationAsync(
        IPubSubProvider pubSubProvider,
        string pattern,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await pubSubProvider.PublishAsync(
                _options.InvalidationChannel,
                pattern,
                cancellationToken).ConfigureAwait(false);

            LogInvalidationBroadcast(_logger, CacheKeyRedactor.Redact(pattern), _options.InvalidationChannel, correlationId);
        }
        catch (Exception ex)
        {
            LogPubSubError(_logger, CacheKeyRedactor.Redact(pattern), _options.InvalidationChannel, ex.ForLogging());

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }
        }
    }

    [LoggerMessage(
        EventId = 3500,
        Level = LogLevel.Debug,
        Message = "Invalidated cache for {RequestType} with pattern {Pattern} (CorrelationId: {CorrelationId})")]
    private static partial void LogCacheInvalidated(
        ILogger logger,
        string requestType,
        string pattern,
        string correlationId);

    [LoggerMessage(
        EventId = 3501,
        Level = LogLevel.Debug,
        Message = "Broadcast invalidation for pattern {Pattern} on channel {Channel} (CorrelationId: {CorrelationId})")]
    private static partial void LogInvalidationBroadcast(
        ILogger logger,
        string pattern,
        string channel,
        string correlationId);

    [LoggerMessage(
        EventId = 3502,
        Level = LogLevel.Warning,
        Message = "Cache invalidation error for {RequestType} with pattern {Pattern}")]
    private static partial void LogInvalidationError(
        ILogger logger,
        string requestType,
        string pattern,
        Exception exception);

    [LoggerMessage(
        EventId = 3503,
        Level = LogLevel.Warning,
        Message = "Pub/Sub error broadcasting invalidation for pattern {Pattern} on channel {Channel}")]
    private static partial void LogPubSubError(
        ILogger logger,
        string pattern,
        string channel,
        Exception exception);

    [LoggerMessage(
        EventId = 3514,
        Level = LogLevel.Warning,
        Message = "Cache invalidation skipped for {RequestType}: multi-tenancy is enabled and the request context has no tenant, so no tenant-scoped pattern exists")]
    private static partial void LogTenantMissingSkipped(
        ILogger logger,
        string requestType);
}
