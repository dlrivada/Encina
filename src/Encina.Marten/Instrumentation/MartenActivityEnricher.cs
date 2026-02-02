using System.Diagnostics;

namespace Encina.Marten.Instrumentation;

/// <summary>
/// Enriches OpenTelemetry activities with Marten event metadata for distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// This enricher provides convenient methods to add Marten-specific event metadata to
/// OpenTelemetry activities. It follows semantic conventions for event sourcing systems
/// and integrates with Encina's correlation/causation tracking.
/// </para>
/// <para><b>Tag Naming Conventions:</b></para>
/// <list type="bullet">
/// <item><description><c>event.message_id</c> - Unique event identifier</description></item>
/// <item><description><c>event.correlation_id</c> - Links related events across a workflow</description></item>
/// <item><description><c>event.causation_id</c> - Links cause-effect event relationships</description></item>
/// <item><description><c>event.stream_id</c> - Aggregate/stream identifier</description></item>
/// <item><description><c>event.type_name</c> - Event type name</description></item>
/// <item><description><c>event.version</c> - Version within the stream</description></item>
/// <item><description><c>event.sequence</c> - Global sequence number</description></item>
/// <item><description><c>event.timestamp</c> - Event timestamp (ISO 8601)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Enrich with a single event
/// using var activity = activitySource.StartActivity("QueryEvent");
/// MartenActivityEnricher.EnrichWithEvent(activity, eventWithMetadata);
///
/// // Enrich with query results
/// var result = await query.GetEventsByCorrelationIdAsync(correlationId);
/// result.Match(
///     Right: r => MartenActivityEnricher.EnrichWithQueryResult(activity, r),
///     Left: _ => { });
/// </code>
/// </example>
public static class MartenActivityEnricher
{
    /// <summary>Tag name for the event identifier.</summary>
    private const string EventMessageId = "event.message_id";

    /// <summary>Tag name for the correlation identifier.</summary>
    private const string EventCorrelationId = "event.correlation_id";

    /// <summary>Tag name for the causation identifier.</summary>
    private const string EventCausationId = "event.causation_id";

    /// <summary>Tag name for the stream identifier.</summary>
    private const string EventStreamId = "event.stream_id";

    /// <summary>Tag name for the event type name.</summary>
    private const string EventTypeName = "event.type_name";

    /// <summary>Tag name for the event version.</summary>
    private const string EventVersion = "event.version";

    /// <summary>Tag name for the event sequence.</summary>
    private const string EventSequence = "event.sequence";

    /// <summary>Tag name for the event timestamp.</summary>
    private const string EventTimestamp = "event.timestamp";

    /// <summary>
    /// Enriches an activity with event metadata from an <see cref="EventWithMetadata"/> instance.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="eventWithMetadata">The event with metadata to extract information from.</param>
    /// <remarks>
    /// This method adds all available event metadata as tags to the activity,
    /// enabling correlation of events with distributed traces.
    /// </remarks>
    public static void EnrichWithEvent(Activity? activity, EventWithMetadata? eventWithMetadata)
    {
        if (activity is null || eventWithMetadata is null)
        {
            return;
        }

        activity.SetTag(EventMessageId, eventWithMetadata.Id.ToString());
        activity.SetTag(EventStreamId, eventWithMetadata.StreamId.ToString());
        activity.SetTag(EventTypeName, eventWithMetadata.EventTypeName);
        activity.SetTag(EventVersion, eventWithMetadata.Version);
        activity.SetTag(EventSequence, eventWithMetadata.Sequence);
        activity.SetTag(EventTimestamp, eventWithMetadata.Timestamp.ToString("O"));

        if (!string.IsNullOrWhiteSpace(eventWithMetadata.CorrelationId))
        {
            activity.SetTag(EventCorrelationId, eventWithMetadata.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(eventWithMetadata.CausationId))
        {
            activity.SetTag(EventCausationId, eventWithMetadata.CausationId);
        }
    }

    /// <summary>
    /// Enriches an activity with correlation and causation IDs.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="correlationId">The correlation ID to add.</param>
    /// <param name="causationId">The causation ID to add (optional).</param>
    /// <remarks>
    /// Use this method when you have correlation/causation IDs but not a full event.
    /// This is useful for enriching activities at the start of request processing.
    /// </remarks>
    public static void EnrichWithCorrelationIds(
        Activity? activity,
        string? correlationId,
        string? causationId = null)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag(EventCorrelationId, correlationId);
        }

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            activity.SetTag(EventCausationId, causationId);
        }
    }

    /// <summary>
    /// Enriches an activity with event query result summary information.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="result">The query result containing events.</param>
    /// <remarks>
    /// <para>
    /// When enriching with query results, this method adds summary tags to avoid tag explosion:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>event.query.total_count</c> - Total matching events</description></item>
    /// <item><description><c>event.query.returned_count</c> - Events in this page</description></item>
    /// <item><description><c>event.query.has_more</c> - Whether more pages exist</description></item>
    /// </list>
    /// <para>
    /// If all events share the same correlation ID, it is also added.
    /// </para>
    /// </remarks>
    public static void EnrichWithQueryResult(Activity? activity, EventQueryResult? result)
    {
        if (activity is null || result is null)
        {
            return;
        }

        activity.SetTag("event.query.total_count", result.TotalCount);
        activity.SetTag("event.query.returned_count", result.Events.Count);
        activity.SetTag("event.query.has_more", result.HasMore);

        // If all events share the same correlation ID, add it
        if (result.Events.Count > 0)
        {
            var firstCorrelationId = result.Events[0].CorrelationId;
            if (!string.IsNullOrWhiteSpace(firstCorrelationId) &&
                result.Events.All(e => e.CorrelationId == firstCorrelationId))
            {
                activity.SetTag(EventCorrelationId, firstCorrelationId);
            }
        }
    }

    /// <summary>
    /// Enriches an activity with causal chain traversal information.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="events">The events in the causal chain.</param>
    /// <param name="direction">The direction of chain traversal.</param>
    /// <remarks>
    /// Adds summary information about the causal chain including:
    /// <list type="bullet">
    /// <item><description><c>event.causal_chain.depth</c> - Number of events in the chain</description></item>
    /// <item><description><c>event.causal_chain.direction</c> - Traversal direction (Ancestors/Descendants)</description></item>
    /// <item><description><c>event.correlation_id</c> - Shared correlation ID (if consistent)</description></item>
    /// </list>
    /// </remarks>
    public static void EnrichWithCausalChain(
        Activity? activity,
        IReadOnlyList<EventWithMetadata>? events,
        CausalChainDirection direction)
    {
        if (activity is null || events is null)
        {
            return;
        }

        activity.SetTag("event.causal_chain.depth", events.Count);
        activity.SetTag("event.causal_chain.direction", direction.ToString());

        // If all events share the same correlation ID, add it
        if (events.Count > 0)
        {
            var firstCorrelationId = events[0].CorrelationId;
            if (!string.IsNullOrWhiteSpace(firstCorrelationId) &&
                events.All(e => e.CorrelationId == firstCorrelationId))
            {
                activity.SetTag(EventCorrelationId, firstCorrelationId);
            }
        }
    }
}
