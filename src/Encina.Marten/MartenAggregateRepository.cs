using Encina.DomainModeling;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Marten;

/// <summary>
/// Marten-based implementation of the aggregate repository.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public sealed class MartenAggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
    where TAggregate : class, IAggregate
{
    private readonly IDocumentSession _session;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<MartenAggregateRepository<TAggregate>> _logger;
    private readonly EncinaMartenOptions _options;
    private readonly EventMetadataEnrichmentService? _enrichmentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenAggregateRepository{TAggregate}"/> class.
    /// </summary>
    /// <param name="session">The Marten document session.</param>
    /// <param name="requestContext">The current request context for metadata extraction.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="enrichers">Optional collection of metadata enrichers.</param>
    public MartenAggregateRepository(
        IDocumentSession session,
        IRequestContext requestContext,
        ILogger<MartenAggregateRepository<TAggregate>> logger,
        IOptions<EncinaMartenOptions> options,
        IEnumerable<IEventMetadataEnricher>? enrichers = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _session = session;
        _requestContext = requestContext;
        _logger = logger;
        _options = options.Value;

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

            var aggregate = await _session.Events.AggregateStreamAsync<TAggregate>(
                id,
                token: cancellationToken).ConfigureAwait(false);

            if (aggregate is null)
            {
                Log.AggregateNotFound(_logger, typeof(TAggregate).Name, id);

                return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        MartenErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} was not found."));
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

            var aggregate = await _session.Events.AggregateStreamAsync<TAggregate>(
                id,
                version: version,
                token: cancellationToken).ConfigureAwait(false);

            if (aggregate is null)
            {
                return Left<EncinaError, TAggregate>( // NOSONAR S6966: LanguageExt Left is a pure function
                    EncinaErrors.Create(
                        MartenErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} at version {version} was not found."));
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

            // Enrich session with metadata before appending events
            _enrichmentService?.EnrichSession(_session, _requestContext, uncommittedEvents);

            // Append events to the stream
            var expectedVersion = aggregate.Version - uncommittedEvents.Count;
            _session.Events.Append(aggregate.Id, expectedVersion, uncommittedEvents.ToArray());

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.SavedEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            Log.ConcurrencyConflict(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            if (_options.ThrowOnConcurrencyConflict)
            {
                throw;
            }

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    MartenErrorCodes.ConcurrencyConflict,
                    ex,
                    $"Concurrency conflict saving aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
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

            // Enrich session with metadata before starting stream
            _enrichmentService?.EnrichSession(_session, _requestContext, uncommittedEvents);

            // Start a new stream
            _session.Events.StartStream<TAggregate>(aggregate.Id, uncommittedEvents.ToArray());

            await _session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.CreatedAggregate(_logger, typeof(TAggregate).Name, aggregate.Id);

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
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
