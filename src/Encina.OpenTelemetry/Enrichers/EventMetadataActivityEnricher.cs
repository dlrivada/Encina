using System.Diagnostics;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with event sourcing metadata for correlation and causation tracking.
/// </summary>
/// <remarks>
/// <para>
/// This enricher adds event metadata tags to OpenTelemetry activities, enabling end-to-end
/// distributed tracing across event-sourced systems. When events are stored or queried,
/// these tags allow correlating events with their originating traces.
/// </para>
/// <para><b>Use Cases:</b></para>
/// <list type="bullet">
/// <item><description>Correlating events with HTTP request traces</description></item>
/// <item><description>Debugging event chains across distributed services</description></item>
/// <item><description>Analyzing event flow patterns in observability tools</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Enrich with correlation IDs
/// using var activity = activitySource.StartActivity("ProcessOrder");
/// EventMetadataActivityEnricher.EnrichWithCorrelationIds(activity, correlationId, causationId);
///
/// // Enrich with full event details
/// EventMetadataActivityEnricher.EnrichWithEvent(
///     activity,
///     eventId: Guid.NewGuid(),
///     streamId: orderId,
///     eventTypeName: "OrderCreated",
///     version: 1,
///     sequence: 100,
///     timestamp: DateTimeOffset.UtcNow,
///     correlationId: requestContext.CorrelationId,
///     causationId: commandId);
/// </code>
/// </example>
public static class EventMetadataActivityEnricher
{
    /// <summary>
    /// Enriches an activity with full event metadata for distributed tracing.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="eventId">The unique event identifier.</param>
    /// <param name="streamId">The aggregate/stream identifier.</param>
    /// <param name="eventTypeName">The event type name.</param>
    /// <param name="version">The version within the stream.</param>
    /// <param name="sequence">The global sequence number.</param>
    /// <param name="timestamp">The event timestamp.</param>
    /// <param name="correlationId">The correlation ID (optional).</param>
    /// <param name="causationId">The causation ID (optional).</param>
    /// <remarks>
    /// This method adds the following tags to the activity:
    /// <list type="bullet">
    /// <item><description><c>event.message_id</c> - The unique event identifier</description></item>
    /// <item><description><c>event.stream_id</c> - The aggregate/stream identifier</description></item>
    /// <item><description><c>event.type_name</c> - The event type name</description></item>
    /// <item><description><c>event.version</c> - The version within the stream</description></item>
    /// <item><description><c>event.sequence</c> - The global sequence number</description></item>
    /// <item><description><c>event.timestamp</c> - The event timestamp (ISO 8601)</description></item>
    /// <item><description><c>event.correlation_id</c> - The correlation ID (if present)</description></item>
    /// <item><description><c>event.causation_id</c> - The causation ID (if present)</description></item>
    /// </list>
    /// </remarks>
    public static void EnrichWithEvent(
        Activity? activity,
        Guid eventId,
        Guid streamId,
        string eventTypeName,
        long version,
        long sequence,
        DateTimeOffset timestamp,
        string? correlationId = null,
        string? causationId = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.EventMetadata.MessageId, eventId.ToString());
        activity.SetTag(ActivityTagNames.EventMetadata.StreamId, streamId.ToString());
        activity.SetTag(ActivityTagNames.EventMetadata.TypeName, eventTypeName);
        activity.SetTag(ActivityTagNames.EventMetadata.Version, version);
        activity.SetTag(ActivityTagNames.EventMetadata.Sequence, sequence);
        activity.SetTag(ActivityTagNames.EventMetadata.Timestamp, timestamp.ToString("O"));

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag(ActivityTagNames.EventMetadata.CorrelationId, correlationId);
        }

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            activity.SetTag(ActivityTagNames.EventMetadata.CausationId, causationId);
        }
    }

    /// <summary>
    /// Enriches an activity with correlation and causation IDs only.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="correlationId">The correlation ID to add.</param>
    /// <param name="causationId">The causation ID to add.</param>
    /// <remarks>
    /// Use this method when you have correlation/causation IDs but not a full event object.
    /// This is useful for enriching activities at the start of request processing, before events are created.
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
            activity.SetTag(ActivityTagNames.EventMetadata.CorrelationId, correlationId);
        }

        if (!string.IsNullOrWhiteSpace(causationId))
        {
            activity.SetTag(ActivityTagNames.EventMetadata.CausationId, causationId);
        }
    }

    /// <summary>
    /// Enriches an activity with event query result summary information.
    /// </summary>
    /// <param name="activity">The activity to enrich. If null, no action is taken.</param>
    /// <param name="totalCount">The total number of matching events.</param>
    /// <param name="returnedCount">The number of events returned in this page.</param>
    /// <param name="hasMore">Whether more results are available.</param>
    /// <param name="correlationId">The shared correlation ID (if all events share it).</param>
    /// <remarks>
    /// <para>
    /// When enriching with query results, this method adds summary tags:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>event.query.total_count</c> - Total matching events</description></item>
    /// <item><description><c>event.query.returned_count</c> - Events in this page</description></item>
    /// <item><description><c>event.query.has_more</c> - Whether more pages exist</description></item>
    /// <item><description><c>event.correlation_id</c> - Shared correlation ID (if provided)</description></item>
    /// </list>
    /// </remarks>
    public static void EnrichWithQueryResult(
        Activity? activity,
        int totalCount,
        int returnedCount,
        bool hasMore,
        string? correlationId = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("event.query.total_count", totalCount);
        activity.SetTag("event.query.returned_count", returnedCount);
        activity.SetTag("event.query.has_more", hasMore);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag(ActivityTagNames.EventMetadata.CorrelationId, correlationId);
        }
    }
}
