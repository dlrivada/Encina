using JasperFx.Events;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Marten;

/// <summary>
/// Marten implementation of <see cref="IEventMetadataQuery"/> for querying events by metadata.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses Marten's event store query capabilities to search for events
/// by their correlation and causation IDs. It supports pagination and filtering options.
/// </para>
/// </remarks>
internal sealed partial class MartenEventMetadataQuery : IEventMetadataQuery
{
    private readonly IDocumentStore _store;
    private readonly ILogger<MartenEventMetadataQuery> _logger;

    private const int MaxTakeLimit = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenEventMetadataQuery"/> class.
    /// </summary>
    /// <param name="store">The Marten document store.</param>
    /// <param name="logger">The logger instance.</param>
    public MartenEventMetadataQuery(
        IDocumentStore store,
        ILogger<MartenEventMetadataQuery> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, EventQueryResult>> GetEventsByCorrelationIdAsync(
        string correlationId,
        EventQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Left<EncinaError, EventQueryResult>(
                EncinaErrors.Create(
                    MartenErrorCodes.InvalidQuery,
                    "Correlation ID cannot be null or empty."));
        }

        try
        {
            Log.QueryingByCorrelationId(_logger, correlationId);
            return await QueryEventsByMetadataAsync(
                e => e.CorrelationId == correlationId,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.QueryFailed(_logger, ex, "CorrelationId", correlationId);
            return Left<EncinaError, EventQueryResult>(
                EncinaErrors.FromException(
                    MartenErrorCodes.QueryFailed,
                    ex,
                    $"Failed to query events by correlation ID: {correlationId}"));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, EventQueryResult>> GetEventsByCausationIdAsync(
        string causationId,
        EventQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(causationId))
        {
            return Left<EncinaError, EventQueryResult>(
                EncinaErrors.Create(
                    MartenErrorCodes.InvalidQuery,
                    "Causation ID cannot be null or empty."));
        }

        try
        {
            Log.QueryingByCausationId(_logger, causationId);
            return await QueryEventsByMetadataAsync(
                e => e.CausationId == causationId,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.QueryFailed(_logger, ex, "CausationId", causationId);
            return Left<EncinaError, EventQueryResult>(
                EncinaErrors.FromException(
                    MartenErrorCodes.QueryFailed,
                    ex,
                    $"Failed to query events by causation ID: {causationId}"));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<EventWithMetadata>>> GetCausalChainAsync(
        Guid eventId,
        CausalChainDirection direction = CausalChainDirection.Ancestors,
        int maxDepth = 100,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            return Left<EncinaError, IReadOnlyList<EventWithMetadata>>(
                EncinaErrors.Create(
                    MartenErrorCodes.InvalidQuery,
                    "Event ID cannot be empty."));
        }

        if (maxDepth <= 0 || maxDepth > 1000)
        {
            return Left<EncinaError, IReadOnlyList<EventWithMetadata>>(
                EncinaErrors.Create(
                    MartenErrorCodes.InvalidQuery,
                    "Max depth must be between 1 and 1000."));
        }

        try
        {
            Log.QueryingCausalChain(_logger, eventId, direction.ToString(), maxDepth);

            return direction == CausalChainDirection.Ancestors
                ? await GetAncestorChainAsync(eventId, maxDepth, cancellationToken).ConfigureAwait(false)
                : await GetDescendantChainAsync(eventId, maxDepth, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.CausalChainQueryFailed(_logger, ex, eventId);
            return Left<EncinaError, IReadOnlyList<EventWithMetadata>>(
                EncinaErrors.FromException(
                    MartenErrorCodes.QueryFailed,
                    ex,
                    $"Failed to get causal chain for event: {eventId}"));
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, EventWithMetadata>> GetEventByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            return Left<EncinaError, EventWithMetadata>(
                EncinaErrors.Create(
                    MartenErrorCodes.InvalidQuery,
                    "Event ID cannot be empty."));
        }

        try
        {
            await using var session = _store.QuerySession();
            var eventData = await session.Events
                .QueryAllRawEvents()
                .Where(e => e.Id == eventId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (eventData is null)
            {
                return Left<EncinaError, EventWithMetadata>(
                    EncinaErrors.Create(
                        MartenErrorCodes.EventNotFound,
                        $"Event with ID {eventId} was not found."));
            }

            return Right<EncinaError, EventWithMetadata>(MapToEventWithMetadata(eventData));
        }
        catch (Exception ex)
        {
            Log.QueryFailed(_logger, ex, "EventId", eventId.ToString());
            return Left<EncinaError, EventWithMetadata>(
                EncinaErrors.FromException(
                    MartenErrorCodes.QueryFailed,
                    ex,
                    $"Failed to get event by ID: {eventId}"));
        }
    }

    private async Task<Either<EncinaError, EventQueryResult>> QueryEventsByMetadataAsync(
        System.Linq.Expressions.Expression<Func<IEvent, bool>> filter,
        EventQueryOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new EventQueryOptions();
        var take = Math.Min(options.Take, MaxTakeLimit);

        await using var session = _store.QuerySession();

        var query = session.Events.QueryAllRawEvents().Where(filter);

        // Apply additional filters
        if (options.StreamId.HasValue)
        {
            var streamId = options.StreamId.Value;
            query = query.Where(e => e.StreamId == streamId);
        }

        if (options.EventTypes?.Count > 0)
        {
            var eventTypes = options.EventTypes;
            query = query.Where(e => eventTypes.Contains(e.EventTypeName));
        }

        if (options.FromTimestamp.HasValue)
        {
            var from = options.FromTimestamp.Value;
            query = query.Where(e => e.Timestamp >= from);
        }

        if (options.ToTimestamp.HasValue)
        {
            var to = options.ToTimestamp.Value;
            query = query.Where(e => e.Timestamp <= to);
        }

        // Get total count first
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply pagination and ordering
        var events = await query
            .OrderBy(e => e.Sequence)
            .Skip(options.Skip)
            .Take(take + 1) // Take one extra to determine HasMore
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMore = events.Count > take;
        if (hasMore)
        {
            events = events.Take(take).ToList();
        }

        var mappedEvents = events.Select(MapToEventWithMetadata).ToList();

        return Right<EncinaError, EventQueryResult>(new EventQueryResult
        {
            Events = mappedEvents,
            TotalCount = totalCount,
            HasMore = hasMore,
        });
    }

    private async Task<Either<EncinaError, IReadOnlyList<EventWithMetadata>>> GetAncestorChainAsync(
        Guid eventId,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        var result = new List<EventWithMetadata>();
        var visited = new System.Collections.Generic.HashSet<Guid>();
        var currentEventId = eventId;
        var depth = 0;

        await using var session = _store.QuerySession();

        while (depth < maxDepth && !cancellationToken.IsCancellationRequested)
        {
            var currentEvent = await session.Events
                .QueryAllRawEvents()
                .Where(e => e.Id == currentEventId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (currentEvent is null)
            {
                break;
            }

            if (!visited.Add(currentEvent.Id))
            {
                // Cycle detected
                break;
            }

            result.Add(MapToEventWithMetadata(currentEvent));

            // Follow the causation chain
            var causationId = currentEvent.CausationId;
            if (string.IsNullOrWhiteSpace(causationId))
            {
                break;
            }

            // Try to find the causing event by its ID (if CausationId is a GUID)
            if (!Guid.TryParse(causationId, out var causationGuid))
            {
                // CausationId might be a correlation ID - stop here
                break;
            }

            currentEventId = causationGuid;
            depth++;
        }

        // Reverse to get oldest first (root cause at the beginning)
        result.Reverse();
        return Right<EncinaError, IReadOnlyList<EventWithMetadata>>(result);
    }

    private async Task<Either<EncinaError, IReadOnlyList<EventWithMetadata>>> GetDescendantChainAsync(
        Guid eventId,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        var result = new List<EventWithMetadata>();
        var toProcess = new Queue<Guid>();
        var visited = new System.Collections.Generic.HashSet<Guid>();
        var depth = 0;

        toProcess.Enqueue(eventId);

        await using var session = _store.QuerySession();

        // First, get the root event
        var rootEvent = await session.Events
            .QueryAllRawEvents()
            .Where(e => e.Id == eventId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rootEvent is not null)
        {
            result.Add(MapToEventWithMetadata(rootEvent));
            visited.Add(rootEvent.Id);
        }

        // BFS to find descendants
        while (toProcess.Count > 0 && depth < maxDepth && !cancellationToken.IsCancellationRequested)
        {
            var currentId = toProcess.Dequeue();
            var currentIdString = currentId.ToString();

            // Find events caused by this event
            var descendants = await session.Events
                .QueryAllRawEvents()
                .Where(e => e.CausationId == currentIdString)
                .OrderBy(e => e.Sequence)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var descendant in descendants)
            {
                if (visited.Add(descendant.Id))
                {
                    result.Add(MapToEventWithMetadata(descendant));
                    toProcess.Enqueue(descendant.Id);
                }
            }

            depth++;
        }

        return Right<EncinaError, IReadOnlyList<EventWithMetadata>>(result);
    }

    private static EventWithMetadata MapToEventWithMetadata(IEvent eventData)
    {
        var headers = new Dictionary<string, object>();

        // Extract headers from event if available
        if (eventData.Headers is not null)
        {
            foreach (var header in eventData.Headers)
            {
                if (header.Value is not null)
                {
                    headers[header.Key] = header.Value;
                }
            }
        }

        return new EventWithMetadata
        {
            Id = eventData.Id,
            StreamId = eventData.StreamId,
            Version = eventData.Version,
            Sequence = eventData.Sequence,
            EventTypeName = eventData.EventTypeName,
            Data = eventData.Data,
            Timestamp = eventData.Timestamp,
            CorrelationId = eventData.CorrelationId,
            CausationId = eventData.CausationId,
            Headers = headers,
        };
    }

    /// <summary>
    /// High-performance logging methods using LoggerMessage source generators.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3230,
            Level = LogLevel.Debug,
            Message = "Querying events by correlation ID: {CorrelationId}")]
        public static partial void QueryingByCorrelationId(ILogger logger, string correlationId);

        [LoggerMessage(
            EventId = 3231,
            Level = LogLevel.Debug,
            Message = "Querying events by causation ID: {CausationId}")]
        public static partial void QueryingByCausationId(ILogger logger, string causationId);

        [LoggerMessage(
            EventId = 3232,
            Level = LogLevel.Debug,
            Message = "Querying causal chain for event {EventId}, direction: {Direction}, max depth: {MaxDepth}")]
        public static partial void QueryingCausalChain(ILogger logger, Guid eventId, string direction, int maxDepth);

        [LoggerMessage(
            EventId = 3233,
            Level = LogLevel.Error,
            Message = "Query by {QueryType}={QueryValue} failed")]
        public static partial void QueryFailed(ILogger logger, Exception ex, string queryType, string queryValue);

        [LoggerMessage(
            EventId = 3234,
            Level = LogLevel.Error,
            Message = "Causal chain query for event {EventId} failed")]
        public static partial void CausalChainQueryFailed(ILogger logger, Exception ex, Guid eventId);
    }
}
