using System.Collections.Immutable;
using Encina.DomainModeling;
using Encina.Marten.Projections;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Marten;

/// <summary>
/// Marten-based implementation of the aggregate repository.
/// </summary>
/// <typeparam name="TAggregate">
/// The aggregate type. It must expose a parameterless constructor so that the repository can
/// instantiate it before replaying its event stream through <see cref="IAggregate.LoadFromHistory"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Event replay is done by Encina, not by Marten: the repository fetches the stream with
/// <c>FetchStreamAsync</c> and lets the aggregate fold the events. This keeps aggregates
/// provider-agnostic (a single <c>Apply(object)</c> switch) instead of requiring the
/// per-event-type methods and <c>partial</c> classes that Marten's own aggregation needs.
/// </para>
/// <para>
/// When an <see cref="IInlineProjectionDispatcher"/> is registered (see
/// <c>AddProjection</c>), every successful <see cref="SaveAsync"/> and <see cref="CreateAsync"/>
/// hands the persisted events to the inline projections so read models stay in step with the
/// stream. See <see cref="InlineProjectionRelay"/> for the failure semantics.
/// </para>
/// </remarks>
public sealed class MartenAggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
    where TAggregate : class, IAggregate, new()
{
    private readonly IDocumentSession _session;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<MartenAggregateRepository<TAggregate>> _logger;
    private readonly EncinaMartenOptions _options;
    private readonly EventMetadataEnrichmentService? _enrichmentService;
    private readonly InlineProjectionRelay _projections;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenAggregateRepository{TAggregate}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="requestContext">The current request context for metadata extraction.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="enrichers">Optional collection of metadata enrichers.</param>
    /// <param name="projectionDispatcher">Optional inline projection dispatcher; when present, saved events update the read models.</param>
    public MartenAggregateRepository(
        IDocumentSession session,
        IRequestContext requestContext,
        ILogger<MartenAggregateRepository<TAggregate>> logger,
        IOptions<EncinaMartenOptions> options,
        IEnumerable<IEventMetadataEnricher>? enrichers = null,
        IInlineProjectionDispatcher? projectionDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _session = session;
        _requestContext = requestContext;
        _logger = logger;
        _options = options.Value;
        _projections = new InlineProjectionRelay(session, projectionDispatcher, _options.Projections, logger);

        // Create enrichment service if metadata tracking is enabled
        if (_options.Metadata.IsAnyMetadataEnabled())
        {
            _enrichmentService = new EventMetadataEnrichmentService(
                _options.Metadata,
                enrichers ?? [],
                logger);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TAggregate>> LoadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log.LoadingAggregate(_logger, typeof(TAggregate).Name, id);

            var events = await _session.Events.FetchStreamAsync(
                id,
                token: cancellationToken).ConfigureAwait(false);

            if (events.Count == 0)
            {
                Log.AggregateNotFound(_logger, typeof(TAggregate).Name, id);

                return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        MartenErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} was not found."));
            }

            var aggregate = Replay(events);

            if (aggregate.Id != id)
            {
                return StreamDoesNotBelongToAggregate(id);
            }

            Log.LoadedAggregate(_logger, typeof(TAggregate).Name, id, aggregate.Version);

            return Right<EncinaError, TAggregate>(aggregate); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            Log.ErrorLoadingAggregate(_logger, ex, typeof(TAggregate).Name, id);

            return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TAggregate>> LoadAsync(
        Guid id,
        int version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log.LoadingAggregateAtVersion(_logger, typeof(TAggregate).Name, id, version);

            // Marten returns the events up to and including the requested version
            var events = await _session.Events.FetchStreamAsync(
                id,
                version: version,
                token: cancellationToken).ConfigureAwait(false);

            if (events.Count == 0)
            {
                return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        MartenErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} at version {version} was not found."));
            }

            var aggregate = Replay(events);

            if (aggregate.Id != id)
            {
                return StreamDoesNotBelongToAggregate(id);
            }

            return Right<EncinaError, TAggregate>(aggregate); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id} at version {version}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        try
        {
            var uncommittedEvents = aggregate.UncommittedEvents;
            if (uncommittedEvents.Count == 0)
            {
                Log.NoUncommittedEvents(_logger, typeof(TAggregate).Name, aggregate.Id);
                return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
            }

            Log.SavingEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            // Snapshot the events: UncommittedEvents is a live view that ClearUncommittedEvents empties.
            var events = uncommittedEvents.ToArray();
            var versionBeforeAppend = aggregate.Version - events.Length;

            // Enrich session with metadata before appending events
            _enrichmentService?.EnrichSession(_session, _requestContext, uncommittedEvents);

            // Append events with optimistic concurrency. Marten's expectedVersion is the version
            // the stream must have AFTER the append; the aggregate's Version already counts the
            // uncommitted events (RaiseEvent increments it), so it is exactly that number. If
            // another process appended to the stream since we loaded it, SaveChangesAsync throws
            // a concurrency exception that we catch below.
            //
            // Marten 9 defaults to EventAppendMode.QuickWithServerTimestamps. The expected-version
            // check is still enforced under that default (a stale append fails with a concurrency
            // exception and the stream keeps only the first writer's events); this is pinned by
            // MartenAggregateRepositoryIntegrationTests.SaveAsync_StreamModifiedByAnotherSession_ReturnsConcurrencyConflict.
            _session.Events.Append(aggregate.Id, aggregate.Version, events);

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.SavedEvents(_logger, events.Length, typeof(TAggregate).Name, aggregate.Id);

            // The events are durable at this point; projections run after the commit
            return await _projections.ProjectAsync(
                typeof(TAggregate).Name,
                aggregate.Id,
                versionBeforeAppend,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            Log.ConcurrencyConflict(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            if (_options.ThrowOnConcurrencyConflict)
            {
                throw;
            }

            // Note: Marten uses event stream versioning, which differs from entity-level versioning
            // used by other providers. When a concurrency conflict occurs:
            // - We have the aggregate the caller attempted to save (with uncommitted events)
            // - We do NOT have the "original" entity state before modifications
            // - We do NOT have the current database state without an additional query
            // The conflict info provides what we can: the aggregate being saved and version context
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count;
            var conflictDetails = new Dictionary<string, object?>
            {
                ["EntityType"] = typeof(TAggregate).Name,
                ["AggregateId"] = aggregate.Id,
                ["ExpectedVersion"] = expectedVersion,
                ["AggregateVersion"] = aggregate.Version,
                ["UncommittedEventCount"] = aggregate.UncommittedEvents.Count,
                ["ConflictType"] = "EventStreamVersionConflict",
                ["Note"] = "Marten uses event stream versioning. The 'ExpectedVersion' is the stream version we expected when appending events."
            }.ToImmutableDictionary();

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.Create(
                    MartenErrorCodes.ConcurrencyConflict,
                    $"Concurrency conflict saving aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}. " +
                    $"Expected stream version {expectedVersion} but the stream has been modified by another process.",
                    ex,
                    conflictDetails));
        }
        catch (Exception ex) when (!IsConcurrencyException(ex))
        {
            Log.ErrorSavingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.SaveFailed,
                    ex,
                    $"Failed to save aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CreateAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        try
        {
            var uncommittedEvents = aggregate.UncommittedEvents;
            if (uncommittedEvents.Count == 0)
            {
                return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        MartenErrorCodes.NoEventsToCreate,
                        "Cannot create aggregate without any events."));
            }

            Log.CreatingAggregate(_logger, typeof(TAggregate).Name, aggregate.Id, uncommittedEvents.Count);

            // Snapshot the events: UncommittedEvents is a live view that ClearUncommittedEvents empties.
            var events = uncommittedEvents.ToArray();

            // Enrich session with metadata before starting stream
            _enrichmentService?.EnrichSession(_session, _requestContext, uncommittedEvents);

            // Start a new stream
            _session.Events.StartStream<TAggregate>(aggregate.Id, events);

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.CreatedAggregate(_logger, typeof(TAggregate).Name, aggregate.Id);

            // The stream is durable at this point; projections run after the commit
            return await _projections.ProjectAsync(
                typeof(TAggregate).Name,
                aggregate.Id,
                versionBeforeAppend: 0,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsStreamCollisionException(ex))
        {
            Log.StreamAlreadyExists(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.StreamAlreadyExists,
                    ex,
                    $"Stream already exists for aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
        catch (Exception ex) when (!IsStreamCollisionException(ex))
        {
            Log.ErrorCreatingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.CreateFailed,
                    ex,
                    $"Failed to create aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <summary>
    /// Instantiates the aggregate and replays the fetched events into it.
    /// </summary>
    private static TAggregate Replay(IReadOnlyList<global::JasperFx.Events.IEvent> events)
    {
        var aggregate = new TAggregate();
        aggregate.LoadFromHistory(events.Select(static e => e.Data));
        return aggregate;
    }

    /// <summary>
    /// The stream exists but its events did not produce an aggregate with the requested identifier:
    /// the stream belongs to another aggregate type (its events are unknown to <typeparamref name="TAggregate"/>
    /// and were folded as no-ops), which Marten does not check on <c>FetchStreamAsync</c>.
    /// </summary>
    private Either<EncinaError, TAggregate> StreamDoesNotBelongToAggregate(Guid id)
    {
        Log.AggregateNotFound(_logger, typeof(TAggregate).Name, id);

        return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
            EncinaErrors.Create(
                MartenErrorCodes.AggregateNotFound,
                $"Stream {id} does not belong to aggregate {typeof(TAggregate).Name}: replaying it did not yield an aggregate with that identifier."));
    }

    /// <summary>
    /// Determines if the exception is a concurrency-related exception.
    /// </summary>
    private static bool IsConcurrencyException(Exception ex)
    {
        // Marten v8 uses different exception types
        var typeName = ex.GetType().Name;
        return typeName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("UnexpectedMaxEventId", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("EventStreamVersion", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the exception is a stream collision exception.
    /// </summary>
    private static bool IsStreamCollisionException(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("StreamIdCollision", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("ExistingStream", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("DuplicateStream", StringComparison.OrdinalIgnoreCase);
    }
}
