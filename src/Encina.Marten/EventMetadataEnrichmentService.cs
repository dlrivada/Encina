using Marten;
using Microsoft.Extensions.Logging;

namespace Encina.Marten;

/// <summary>
/// Service responsible for enriching Marten sessions with event metadata.
/// </summary>
/// <remarks>
/// <para>
/// This service centralizes the logic for setting metadata on Marten sessions
/// before events are appended. It reads configuration from <see cref="EventMetadataOptions"/>
/// and applies headers from <see cref="IRequestContext"/> and registered enrichers.
/// </para>
/// </remarks>
internal sealed partial class EventMetadataEnrichmentService
{
    private readonly EventMetadataOptions _options;
    private readonly IEnumerable<IEventMetadataEnricher> _enrichers;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventMetadataEnrichmentService"/> class.
    /// </summary>
    /// <param name="options">The metadata configuration options.</param>
    /// <param name="enrichers">The collection of registered enrichers.</param>
    /// <param name="logger">The logger instance.</param>
    public EventMetadataEnrichmentService(
        EventMetadataOptions options,
        IEnumerable<IEventMetadataEnricher> enrichers,
        ILogger logger)
    {
        _options = options;
        _enrichers = enrichers;
        _logger = logger;
    }

    /// <summary>
    /// Enriches the session with metadata from the request context and enrichers.
    /// </summary>
    /// <param name="session">The Marten document session to enrich.</param>
    /// <param name="context">The current request context.</param>
    /// <param name="events">The events being persisted (for enricher invocation).</param>
    public void EnrichSession(
        IDocumentSession session,
        IRequestContext context,
        IReadOnlyCollection<object> events)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(events);

        // Set correlation ID if enabled
        if (_options.CorrelationIdEnabled && !string.IsNullOrWhiteSpace(context.CorrelationId))
        {
            session.CorrelationId = context.CorrelationId;
            Log.CorrelationIdSet(_logger, context.CorrelationId);
        }

        // Set causation ID if enabled
        // For command-triggered events, CausationId is the CorrelationId (the command's identifier)
        // This can be overridden via IRequestContext.Metadata["CausationId"]
        if (_options.CausationIdEnabled)
        {
            var causationId = GetCausationId(context);
            if (!string.IsNullOrWhiteSpace(causationId))
            {
                session.CausationId = causationId;
                Log.CausationIdSet(_logger, causationId);
            }
        }

        // Set headers if any are configured
        if (_options.HeadersEnabled)
        {
            SetHeaders(session, context, events);
        }
    }

    private static string? GetCausationId(IRequestContext context)
    {
        // Check if a specific CausationId was provided in metadata
        if (context.Metadata.TryGetValue("CausationId", out var causationIdObj)
            && causationIdObj is string causationId
            && !string.IsNullOrWhiteSpace(causationId))
        {
            return causationId;
        }

        // Default: use CorrelationId as CausationId for command-triggered events
        return context.CorrelationId;
    }

    private void SetHeaders(
        IDocumentSession session,
        IRequestContext context,
        IReadOnlyCollection<object> events)
    {
        // User ID
        if (_options.CaptureUserId && !string.IsNullOrWhiteSpace(context.UserId))
        {
            session.SetHeader("UserId", context.UserId);
        }

        // Tenant ID
        if (_options.CaptureTenantId && !string.IsNullOrWhiteSpace(context.TenantId))
        {
            session.SetHeader("TenantId", context.TenantId);
        }

        // Timestamp
        if (_options.CaptureTimestamp)
        {
            session.SetHeader("Timestamp", context.Timestamp.ToString("O"));
        }

        // Commit SHA
        if (_options.CaptureCommitSha && !string.IsNullOrWhiteSpace(_options.CommitSha))
        {
            session.SetHeader("CommitSha", _options.CommitSha);
        }

        // Semantic Version
        if (_options.CaptureSemanticVersion && !string.IsNullOrWhiteSpace(_options.SemanticVersion))
        {
            session.SetHeader("SemanticVersion", _options.SemanticVersion);
        }

        // Custom headers from configuration
        foreach (var header in _options.CustomHeaders)
        {
            session.SetHeader(header.Key, header.Value);
        }

        // Headers from registered enrichers
        ApplyEnricherHeaders(session, context, events);
    }

    private void ApplyEnricherHeaders(
        IDocumentSession session,
        IRequestContext context,
        IReadOnlyCollection<object> events)
    {
        foreach (var enricher in _enrichers)
        {
            try
            {
                // Invoke enricher for each event and collect all unique headers
                foreach (var @event in events)
                {
                    var headers = enricher.EnrichMetadata(@event, context);
                    foreach (var header in headers)
                    {
                        session.SetHeader(header.Key, header.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail event persistence
                Log.EnricherFailed(_logger, ex, enricher.GetType().Name);
            }
        }
    }

    /// <summary>
    /// High-performance logging methods using LoggerMessage source generators.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3220,
            Level = LogLevel.Debug,
            Message = "Set correlation ID on session: {CorrelationId}")]
        public static partial void CorrelationIdSet(ILogger logger, string correlationId);

        [LoggerMessage(
            EventId = 3221,
            Level = LogLevel.Debug,
            Message = "Set causation ID on session: {CausationId}")]
        public static partial void CausationIdSet(ILogger logger, string causationId);

        [LoggerMessage(
            EventId = 3222,
            Level = LogLevel.Warning,
            Message = "Event metadata enricher {EnricherType} failed")]
        public static partial void EnricherFailed(ILogger logger, Exception ex, string enricherType);
    }
}
