using JasperFx.Events;

namespace Encina.Marten.Projections;

/// <summary>
/// Builds <see cref="ProjectionContext"/> instances from persisted Marten event envelopes, so that
/// inline dispatch (after a save) and rebuilds (from the stream) hand projections the same values.
/// </summary>
internal static class ProjectionContextFactory
{
    /// <summary>
    /// Creates the projection context for a persisted event.
    /// </summary>
    /// <param name="eventData">The event envelope as read from the event store.</param>
    /// <returns>The context carrying stream id, version, global sequence, timestamp, correlation data and headers.</returns>
    public static ProjectionContext FromEvent(IEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var metadata = new Dictionary<string, object>();
        if (eventData.Headers is not null)
        {
            foreach (var header in eventData.Headers)
            {
                if (header.Value is not null)
                {
                    metadata[header.Key] = header.Value;
                }
            }
        }

        return new ProjectionContext
        {
            StreamId = eventData.StreamId,
            SequenceNumber = eventData.Version,
            GlobalPosition = eventData.Sequence,
            Timestamp = eventData.Timestamp.UtcDateTime,
            EventType = eventData.EventTypeName,
            CorrelationId = eventData.CorrelationId,
            CausationId = eventData.CausationId,
            Metadata = metadata,
        };
    }
}
