using LanguageExt;
using Marten;
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
/// The relay re-reads the appended envelopes from the stream and builds each
/// <see cref="ProjectionContext"/> with <see cref="ProjectionContextFactory"/>, exactly as a
/// rebuild does, so projections see the persisted version, global sequence, timestamp,
/// correlation data and headers rather than values guessed on the client. This costs one read
/// per save while projections are active.
/// </para>
/// <para>
/// With <see cref="ProjectionOptions.ThrowOnProjectionError"/> <c>false</c> (the default) a
/// failure is logged and the save is still reported as a success. With <c>true</c> the
/// projection error is returned to the caller as the <c>Left</c> of the save result. Exceptions
/// escaping the dispatcher are mapped to <see cref="ProjectionErrorCodes.ApplyFailed"/> here so
/// that a projection problem is never reported as a failed save; cancellation propagates.
/// </para>
/// <para>
/// The relay is a no-op when no <see cref="IInlineProjectionDispatcher"/> is registered or when
/// <see cref="ProjectionOptions.UseInlineProjections"/> is <c>false</c>.
/// </para>
/// </remarks>
internal sealed class InlineProjectionRelay
{
    private readonly IDocumentSession _session;
    private readonly IInlineProjectionDispatcher? _dispatcher;
    private readonly ProjectionOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineProjectionRelay"/> class.
    /// </summary>
    /// <param name="session">The Marten session the events were saved through.</param>
    /// <param name="dispatcher">The inline projection dispatcher, or <c>null</c> when projections are not registered.</param>
    /// <param name="options">The projection options.</param>
    /// <param name="logger">The logger of the owning repository.</param>
    public InlineProjectionRelay(
        IDocumentSession session,
        IInlineProjectionDispatcher? dispatcher,
        ProjectionOptions options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _dispatcher = dispatcher;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the relay will dispatch anything at all.
    /// </summary>
    public bool IsActive => _dispatcher is not null && _options.UseInlineProjections;

    /// <summary>
    /// Dispatches the events appended to one stream after a given version to the inline projections.
    /// </summary>
    /// <param name="aggregateType">The aggregate type name, for logging.</param>
    /// <param name="streamId">The aggregate (stream) identifier.</param>
    /// <param name="versionBeforeAppend">The stream version before the events were appended; every event above it is dispatched.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> when the projections were applied, when there is nothing to dispatch, or when a
    /// failure is tolerated by <see cref="ProjectionOptions.ThrowOnProjectionError"/>; otherwise the
    /// projection error.
    /// </returns>
    public async Task<Either<EncinaError, Unit>> ProjectAsync(
        string aggregateType,
        Guid streamId,
        long versionBeforeAppend,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregateType);

        if (!IsActive)
        {
            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        Either<EncinaError, Unit> result;
        try
        {
            // 'fromVersion' is inclusive; the filter guards against a wider window
            var envelopes = await _session.Events.FetchStreamAsync(
                streamId,
                fromVersion: versionBeforeAppend + 1,
                token: cancellationToken).ConfigureAwait(false);

            var items = envelopes
                .Where(e => e.Version > versionBeforeAppend)
                .OrderBy(static e => e.Version)
                .Select(static e => (e.Data, ProjectionContextFactory.FromEvent(e)))
                .ToArray();

            if (items.Length == 0)
            {
                return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
            }

            result = await _dispatcher!.DispatchManyAsync(items, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result = Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    ProjectionErrorCodes.ApplyFailed,
                    ex,
                    $"Failed to dispatch inline projections for aggregate {aggregateType} with ID {streamId}."));
        }

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
