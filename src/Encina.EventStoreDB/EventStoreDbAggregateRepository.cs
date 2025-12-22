using EventStore.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.EventStoreDB;

/// <summary>
/// EventStoreDB-based implementation of the aggregate repository.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type.</typeparam>
public sealed class EventStoreDbAggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
    where TAggregate : AggregateBase, new()
{
    private readonly EventStoreClient _client;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<EventStoreDbAggregateRepository<TAggregate>> _logger;
    private readonly EventStoreDbOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDbAggregateRepository{TAggregate}"/> class.
    /// </summary>
    /// <param name="client">The EventStoreDB client.</param>
    /// <param name="serializer">The event serializer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public EventStoreDbAggregateRepository(
        EventStoreClient client,
        IEventSerializer serializer,
        ILogger<EventStoreDbAggregateRepository<TAggregate>> logger,
        IOptions<EventStoreDbOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _serializer = serializer;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Either<MediatorError, TAggregate>> LoadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(id);

        try
        {
            Log.LoadingAggregate(_logger, typeof(TAggregate).Name, id, streamName);

            var aggregate = new TAggregate { Id = id };
            var eventCount = 0L;

            await foreach (var resolvedEvent in _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.Start,
                cancellationToken: cancellationToken))
            {
                var domainEvent = _serializer.Deserialize(resolvedEvent);
                if (domainEvent is not null)
                {
                    aggregate.ApplyFromHistory(domainEvent, resolvedEvent.Event.EventNumber.ToInt64());
                    eventCount++;
                }
            }

            if (eventCount == 0)
            {
                Log.AggregateNotFoundEmpty(_logger, typeof(TAggregate).Name, id);

                return Left<MediatorError, TAggregate>(
                    MediatorErrors.Create(
                        EventStoreDbErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} was not found."));
            }

            Log.LoadedAggregate(_logger, typeof(TAggregate).Name, id, aggregate.Version, eventCount);

            return Right<MediatorError, TAggregate>(aggregate);
        }
        catch (StreamNotFoundException)
        {
            Log.StreamNotFound(_logger, streamName, typeof(TAggregate).Name, id);

            return Left<MediatorError, TAggregate>(
                MediatorErrors.Create(
                    EventStoreDbErrorCodes.AggregateNotFound,
                    $"Aggregate {typeof(TAggregate).Name} with ID {id} was not found."));
        }
        catch (Exception ex)
        {
            Log.ErrorLoadingAggregate(_logger, ex, typeof(TAggregate).Name, id);

            return Left<MediatorError, TAggregate>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<MediatorError, TAggregate>> LoadAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken = default)
    {
        var streamName = GetStreamName(id);

        try
        {
            Log.LoadingAggregateAtVersion(_logger, typeof(TAggregate).Name, id, version);

            var aggregate = new TAggregate { Id = id };
            var eventCount = 0L;

            await foreach (var resolvedEvent in _client.ReadStreamAsync(
                Direction.Forwards,
                streamName,
                StreamPosition.Start,
                maxCount: version + 1,
                cancellationToken: cancellationToken))
            {
                var domainEvent = _serializer.Deserialize(resolvedEvent);
                if (domainEvent is not null)
                {
                    aggregate.ApplyFromHistory(domainEvent, resolvedEvent.Event.EventNumber.ToInt64());
                    eventCount++;
                }

                if (resolvedEvent.Event.EventNumber.ToInt64() >= version)
                {
                    break;
                }
            }

            if (eventCount == 0)
            {
                return Left<MediatorError, TAggregate>(
                    MediatorErrors.Create(
                        EventStoreDbErrorCodes.AggregateNotFound,
                        $"Aggregate {typeof(TAggregate).Name} with ID {id} at version {version} was not found."));
            }

            return Right<MediatorError, TAggregate>(aggregate);
        }
        catch (StreamNotFoundException)
        {
            return Left<MediatorError, TAggregate>(
                MediatorErrors.Create(
                    EventStoreDbErrorCodes.AggregateNotFound,
                    $"Aggregate {typeof(TAggregate).Name} with ID {id} at version {version} was not found."));
        }
        catch (Exception ex)
        {
            return Left<MediatorError, TAggregate>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.LoadFailed,
                    ex,
                    $"Failed to load aggregate {typeof(TAggregate).Name} with ID {id} at version {version}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<MediatorError, Unit>> SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var streamName = GetStreamName(aggregate.Id);
        var uncommittedEvents = aggregate.UncommittedEvents;

        if (uncommittedEvents.Count == 0)
        {
            Log.NoUncommittedEvents(_logger, typeof(TAggregate).Name, aggregate.Id);
            return Right<MediatorError, Unit>(Unit.Default);
        }

        try
        {
            Log.SavingEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            // Calculate expected version
            var expectedVersion = aggregate.Version - uncommittedEvents.Count;
            var expectedRevision = expectedVersion < 0
                ? StreamRevision.None
                : StreamRevision.FromInt64(expectedVersion);

            // Serialize events
            var eventData = uncommittedEvents
                .Select(e => _serializer.Serialize(e))
                .ToArray();

            // Append to stream with optimistic concurrency
            await _client.AppendToStreamAsync(
                streamName,
                expectedRevision,
                eventData,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.SavedEvents(_logger, uncommittedEvents.Count, typeof(TAggregate).Name, aggregate.Id);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (WrongExpectedVersionException ex)
        {
            Log.ConcurrencyConflict(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            if (_options.ThrowOnConcurrencyConflict)
            {
                throw;
            }

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.ConcurrencyConflict,
                    ex,
                    $"Concurrency conflict saving aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
        catch (Exception ex)
        {
            Log.ErrorSavingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.SaveFailed,
                    ex,
                    $"Failed to save aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <inheritdoc />
    public async Task<Either<MediatorError, Unit>> CreateAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var uncommittedEvents = aggregate.UncommittedEvents;

        if (uncommittedEvents.Count == 0)
        {
            return Left<MediatorError, Unit>(
                MediatorErrors.Create(
                    EventStoreDbErrorCodes.NoEventsToCreate,
                    "Cannot create aggregate without any events."));
        }

        var streamName = GetStreamName(aggregate.Id);

        try
        {
            Log.CreatingAggregate(_logger, typeof(TAggregate).Name, aggregate.Id, uncommittedEvents.Count);

            // Serialize events
            var eventData = uncommittedEvents
                .Select(e => _serializer.Serialize(e))
                .ToArray();

            // Create stream (expect no stream exists)
            await _client.AppendToStreamAsync(
                streamName,
                StreamState.NoStream,
                eventData,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Clear uncommitted events after successful save
            aggregate.ClearUncommittedEvents();

            Log.CreatedAggregate(_logger, typeof(TAggregate).Name, aggregate.Id);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (WrongExpectedVersionException ex)
        {
            Log.StreamAlreadyExists(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.StreamAlreadyExists,
                    ex,
                    $"Stream already exists for aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
        catch (Exception ex)
        {
            Log.ErrorCreatingAggregate(_logger, ex, typeof(TAggregate).Name, aggregate.Id);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    EventStoreDbErrorCodes.CreateFailed,
                    ex,
                    $"Failed to create aggregate {typeof(TAggregate).Name} with ID {aggregate.Id}."));
        }
    }

    /// <summary>
    /// Gets the stream name for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <returns>The stream name.</returns>
    private string GetStreamName(Guid aggregateId)
    {
        return $"{_options.StreamNamePrefix}{typeof(TAggregate).Name}-{aggregateId}";
    }
}
