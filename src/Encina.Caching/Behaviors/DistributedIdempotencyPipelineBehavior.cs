using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching;

/// <summary>
/// Pipeline behavior that implements distributed idempotency using the cache provider.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior ensures exactly-once processing of requests that implement
/// <see cref="IDistributedIdempotentRequest"/>. It uses the cache provider for
/// idempotency state, making it work across multiple application instances.
/// </para>
/// <para>
/// <b>Differences from EF Core InboxPipelineBehavior</b>:
/// </para>
/// <list type="bullet">
/// <item><description>Uses cache instead of database - faster but less durable</description></item>
/// <item><description>No automatic cleanup - relies on TTL expiration</description></item>
/// <item><description>Works with any cache provider (Redis, Garnet, Memory, etc.)</description></item>
/// </list>
/// <para>
/// For durable idempotency with full audit trail, use the database-based Inbox pattern.
/// For high-throughput scenarios where some idempotency loss is acceptable, use this.
/// </para>
/// </remarks>
public sealed partial class DistributedIdempotencyPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheProvider _cacheProvider;
    private readonly CachingOptions _options;
    private readonly ILogger<DistributedIdempotencyPipelineBehavior<TRequest, TResponse>> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedIdempotencyPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider.</param>
    /// <param name="options">The caching options.</param>
    /// <param name="logger">The logger.</param>
    public DistributedIdempotencyPipelineBehavior(
        ICacheProvider cacheProvider,
        IOptions<CachingOptions> options,
        ILogger<DistributedIdempotencyPipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _options = options.Value;
        _logger = logger;
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

        // Check if idempotency is enabled and request implements the interface
        if (!_options.EnableDistributedIdempotency || request is not IDistributedIdempotentRequest)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Get idempotency key from context
        var idempotencyKey = context.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            LogMissingIdempotencyKey(_logger, typeof(TRequest).Name, context.CorrelationId);

            return EncinaErrors.Create(
                "idempotency.missing_key",
                "Distributed idempotent requests require an IdempotencyKey in the request context");
        }

        var cacheKey = $"{_options.IdempotencyKeyPrefix}:{context.TenantId}:{idempotencyKey}";

        try
        {
            // Check for existing entry
            var existing = await _cacheProvider.GetAsync<IdempotencyEntry<TResponse>>(cacheKey, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                LogIdempotentDuplicate(_logger, typeof(TRequest).Name, idempotencyKey, context.CorrelationId);

                if (existing.IsSuccess && existing.Response is not null)
                {
                    return existing.Response;
                }

                if (existing.HasError)
                {
                    return existing.Error;
                }

                // Entry exists but no response yet - request is in progress
                return EncinaErrors.Create(
                    "idempotency.in_progress",
                    "Request is already being processed");
            }

            // Mark as in progress
            var inProgressEntry = new IdempotencyEntry<TResponse>
            {
                StartedAtUtc = DateTime.UtcNow,
                IsSuccess = false
            };

            await _cacheProvider.SetAsync(cacheKey, inProgressEntry, _options.IdempotencyTtl, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogIdempotencyError(_logger, typeof(TRequest).Name, idempotencyKey, ex);

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }

            // Continue without idempotency on cache error
        }

        // Execute the handler
        var result = await nextStep().ConfigureAwait(false);

        // Store the result
        try
        {
            var (hasError, errorValue) = result.Match(
                Right: _ => (false, default(EncinaError)),
                Left: e => (true, e));

            var entry = new IdempotencyEntry<TResponse>
            {
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                IsSuccess = result.IsRight,
                Response = result.Match(Right: v => v, Left: _ => default),
                HasError = hasError,
                Error = errorValue
            };

            await _cacheProvider.SetAsync(cacheKey, entry, _options.IdempotencyTtl, cancellationToken)
                .ConfigureAwait(false);

            LogIdempotencyStored(_logger, typeof(TRequest).Name, idempotencyKey, context.CorrelationId);
        }
        catch (Exception ex)
        {
            LogIdempotencyError(_logger, typeof(TRequest).Name, idempotencyKey, ex);

            if (_options.ThrowOnCacheErrors)
            {
                throw;
            }
        }

        return result;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Idempotent request {RequestType} received without IdempotencyKey (CorrelationId: {CorrelationId})")]
    private static partial void LogMissingIdempotencyKey(
        ILogger logger,
        string requestType,
        string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Returning cached response for duplicate idempotent request {RequestType} with key {IdempotencyKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogIdempotentDuplicate(
        ILogger logger,
        string requestType,
        string idempotencyKey,
        string correlationId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Stored idempotency result for {RequestType} with key {IdempotencyKey} (CorrelationId: {CorrelationId})")]
    private static partial void LogIdempotencyStored(
        ILogger logger,
        string requestType,
        string idempotencyKey,
        string correlationId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Idempotency cache error for {RequestType} with key {IdempotencyKey}")]
    private static partial void LogIdempotencyError(
        ILogger logger,
        string requestType,
        string idempotencyKey,
        Exception exception);
}

/// <summary>
/// Marker interface for requests that should be processed with distributed idempotency.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on commands that should only be processed once across all
/// application instances. The idempotency key is provided via <see cref="IRequestContext.IdempotencyKey"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record ProcessPaymentCommand(decimal Amount, string Reference)
///     : ICommand&lt;PaymentResult&gt;, IDistributedIdempotentRequest;
///
/// // In your API controller
/// [HttpPost]
/// public async Task&lt;IActionResult&gt; ProcessPayment(
///     ProcessPaymentCommand command,
///     [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
/// {
///     // IdempotencyKey flows through via IRequestContext
///     var result = await _Encina.Send(command);
///     return result.ToActionResult();
/// }
/// </code>
/// </example>
public interface IDistributedIdempotentRequest
{
}

/// <summary>
/// Internal entry for storing idempotency state.
/// </summary>
internal sealed class IdempotencyEntry<TResponse>
{
    /// <summary>
    /// Gets or sets when processing started.
    /// </summary>
    public DateTime StartedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets when processing completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets whether the request succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets or sets the cached response (if successful).
    /// </summary>
    public TResponse? Response { get; init; }

    /// <summary>
    /// Gets or sets whether there is an error.
    /// </summary>
    public bool HasError { get; init; }

    /// <summary>
    /// Gets or sets the error (if failed).
    /// </summary>
    public EncinaError Error { get; init; }
}
