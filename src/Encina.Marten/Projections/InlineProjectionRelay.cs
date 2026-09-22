using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten.Projections;

/// <summary>
/// Hands the events an aggregate repository has just persisted to the inline projection
/// dispatcher so that read models are updated in the same request (issue #1095).
/// </summary>
/// <remarks>
/// <para>
/// The relay runs <b>after</b> the event stream has been committed: the optimistic-concurrency
/// check has already passed and the events are durable. A projection failure therefore never
/// loses events; it only leaves the read model behind, which is what
/// <see cref="ProjectionOptions.ThrowOnProjectionError"/> controls.
/// </para>
/// <para>
/// With <see cref="ProjectionOptions.ThrowOnProjectionError"/> <c>false</c> (the default) a
/// failure is logged and the save is still reported as a success. With <c>true</c> the
/// projection error is returned to the caller as the <c>Left</c> of the save result.
/// </para>
/// <para>
/// The relay is a no-op when no <see cref="IInlineProjectionDispatcher"/> is registered or when
/// <see cref="ProjectionOptions.UseInlineProjections"/> is <c>false</c>.
/// </para>
/// </remarks>
internal sealed class InlineProjectionRelay
{
    private readonly IInlineProjectionDispatcher? _dispatcher;
    private readonly ProjectionOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineProjectionRelay"/> class.
    /// </summary>
    /// <param name="dispatcher">The inline projection dispatcher, or <c>null</c> when projections are not registered.</param>
    /// <param name="options">The projection options.</param>
    /// <param name="timeProvider">The time provider used to stamp projection contexts.</param>
    /// <param name="logger">The logger of the owning repository.</param>
    public InlineProjectionRelay(
        IInlineProjectionDispatcher? dispatcher,
        ProjectionOptions options,
        TimeProvider timeProvider,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _dispatcher = dispatcher;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the relay will dispatch anything at all.
    /// </summary>
    public bool IsActive => _dispatcher is not null && _options.UseInlineProjections;

    /// <summary>
    /// Dispatches the persisted events of one aggregate to the inline projections.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name, for logging.</param>
    /// <param name="streamId">The aggregate (stream) identifier.</param>
    /// <param name="versionBeforeAppend">The stream version before the events were appended.</param>
    /// <param name="events">The events, in the order they were appended.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> when the projections were applied, when there is nothing to dispatch, or when a
    /// failure is tolerated by <see cref="ProjectionOptions.ThrowOnProjectionError"/>; otherwise the
    /// projection error.
    /// </returns>
    public async Task<Either<EncinaError, Unit>> ProjectAsync(
        string aggregateType,
        Guid streamId,
        int versionBeforeAppend,
        IReadOnlyList<object> events,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregateType);
        ArgumentNullException.ThrowIfNull(events);

        if (!IsActive || events.Count == 0)
        {
            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
        var items = new (object Event, ProjectionContext Context)[events.Count];
        for (var i = 0; i < events.Count; i++)
        {
            var domainEvent = events[i];
            items[i] = (domainEvent, new ProjectionContext(streamId, versionBeforeAppend + i + 1, 0, timestamp)
            {
                EventType = domainEvent.GetType().Name
            });
        }

        var result = await _dispatcher!.DispatchManyAsync(items, cancellationToken).ConfigureAwait(false);

        if (result.IsRight || _options.ThrowOnProjectionError)
        {
            return result;
        }

        var errorMessage = result.Match(
            Right: static _ => string.Empty,
            Left: static error => error.Message);
        ProjectionLog.InlineProjectionFailedAfterSave(_logger, aggregateType, streamId, errorMessage);

        return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
    }
}
