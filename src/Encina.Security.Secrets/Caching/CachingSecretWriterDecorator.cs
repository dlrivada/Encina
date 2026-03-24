using Encina.Caching;
using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Caching;

/// <summary>
/// Decorator that wraps an <see cref="ISecretWriter"/> with write-through cache invalidation
/// and optional cross-instance PubSub notification.
/// </summary>
/// <remarks>
/// <para>
/// <b>Write-through pattern:</b> On <see cref="SetSecretAsync"/>, the inner writer is called
/// first. On success, all cached variants of the secret are removed from the cache, and
/// an invalidation message is published to PubSub (when available).
/// </para>
/// <para>
/// <b>Resilience:</b> All cache and PubSub operations are wrapped in try/catch. Failures are
/// logged at <c>Warning</c> level — the inner write result is always returned unchanged.
/// Cache/PubSub errors never affect the write outcome.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically by AddEncinaSecrets when EnableCaching is true
/// // and ISecretWriter is available:
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableCaching = true;
///     options.Caching.EnablePubSubInvalidation = true;
/// });
/// </code>
/// </example>
public sealed class CachingSecretWriterDecorator : ISecretWriter
{
    private readonly ISecretWriter _inner;
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider? _pubSub;
    private readonly SecretCachingOptions _options;
    private readonly ILogger<CachingSecretWriterDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingSecretWriterDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner secret writer to wrap.</param>
    /// <param name="cache">The cache provider for invalidation operations.</param>
    /// <param name="pubSub">
    /// Optional pub/sub provider for cross-instance invalidation.
    /// When <c>null</c>, PubSub invalidation is silently skipped.
    /// </param>
    /// <param name="options">The secrets caching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public CachingSecretWriterDecorator(
        ISecretWriter inner,
        ICacheProvider cache,
        IPubSubProvider? pubSub,
        SecretCachingOptions options,
        ILogger<CachingSecretWriterDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _pubSub = pubSub;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SetSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken = default)
    {
        // 1. Persist via inner writer first
        var result = await _inner.SetSecretAsync(secretName, value, cancellationToken).ConfigureAwait(false);

        // 2. On success: invalidate cache + broadcast.
        // Post-write invalidation uses separate bounded timeouts per operation
        // so a slow cache backend doesn't starve the PubSub broadcast.
        if (result.IsRight)
        {
            using var invalidateCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await InvalidateCacheAsync(secretName, invalidateCts.Token).ConfigureAwait(false);

            using var publishCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await PublishInvalidationAsync(secretName, "Set", publishCts.Token).ConfigureAwait(false);
        }

        return result;
    }

    // ── Invalidation Helpers ───────────────────────────────────────

    private async Task InvalidateCacheAsync(string secretName, CancellationToken cancellationToken)
    {
        // Best-effort invalidation: each key/pattern is independent.
        // Failure on one doesn't prevent attempting the others.
        await TryRemoveAsync($"{_options.CacheKeyPrefix}:v:{secretName}", cancellationToken).ConfigureAwait(false);
        await TryRemoveAsync($"{_options.CacheKeyPrefix}:lkg:{secretName}", cancellationToken).ConfigureAwait(false);
        await TryRemoveByPatternAsync($"{_options.CacheKeyPrefix}:t:{secretName}:*", cancellationToken).ConfigureAwait(false);
        await TryRemoveByPatternAsync($"{_options.CacheKeyPrefix}:lkg:t:{secretName}:*", cancellationToken).ConfigureAwait(false);

        Log.WriterCacheInvalidation(_logger, secretName);
    }

    private async Task TryRemoveAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.CacheInvalidationError(_logger, key, ex);
        }
    }

    private async Task TryRemoveByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.CacheInvalidationError(_logger, pattern, ex);
        }
    }

    private async Task PublishInvalidationAsync(string secretName, string operation, CancellationToken cancellationToken)
    {
        if (!_options.EnablePubSubInvalidation || _pubSub is null)
        {
            return;
        }

        try
        {
            var message = new SecretCacheInvalidationMessage(
                secretName, operation, DateTime.UtcNow);

            await _pubSub.PublishAsync(
                _options.InvalidationChannel,
                message,
                cancellationToken).ConfigureAwait(false);

            Log.PubSubInvalidationPublished(_logger, secretName, _options.InvalidationChannel);
        }
        catch (Exception ex)
        {
            Log.PubSubPublishError(_logger, secretName, _options.InvalidationChannel, ex);
        }
    }

}
