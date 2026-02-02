using global::Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Marten;

/// <summary>
/// Configures Marten's event store to enable metadata columns based on <see cref="EventMetadataOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="IConfigureOptions{StoreOptions}"/> to configure
/// Marten's metadata storage capabilities for causation/correlation tracking.
/// </para>
/// <para>
/// When metadata tracking is enabled, Marten will create additional columns in the
/// event store for correlation ID, causation ID, and custom headers.
/// </para>
/// </remarks>
internal sealed partial class ConfigureMartenEventMetadata : IConfigureOptions<StoreOptions>
{
    private readonly IOptions<EncinaMartenOptions> _options;
    private readonly ILogger<ConfigureMartenEventMetadata> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMartenEventMetadata"/> class.
    /// </summary>
    /// <param name="options">The Encina Marten options containing metadata configuration.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public ConfigureMartenEventMetadata(
        IOptions<EncinaMartenOptions> options,
        ILogger<ConfigureMartenEventMetadata> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Configures Marten's store options with metadata columns based on configuration.
    /// </summary>
    /// <param name="options">The Marten store options to configure.</param>
    public void Configure(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var metadata = _options.Value.Metadata;

        // Skip configuration if no metadata features are enabled
        if (!metadata.IsAnyMetadataEnabled())
        {
            Log.MetadataTrackingDisabled(_logger);
            return;
        }

        // Configure correlation ID column
        if (metadata.CorrelationIdEnabled)
        {
            options.Events.MetadataConfig.CorrelationIdEnabled = true;
            Log.CorrelationIdEnabled(_logger);
        }

        // Configure causation ID column
        if (metadata.CausationIdEnabled)
        {
            options.Events.MetadataConfig.CausationIdEnabled = true;
            Log.CausationIdEnabled(_logger);
        }

        // Enable headers storage if needed for user metadata
        if (metadata.RequiresHeaderStorage())
        {
            options.Events.MetadataConfig.HeadersEnabled = true;
            Log.HeadersEnabled(_logger);

            var headerCount = CountConfiguredHeaders(metadata);
            if (headerCount > 0)
            {
                Log.CustomHeadersConfigured(_logger, headerCount);
            }
        }

        Log.MetadataTrackingConfigured(
            _logger,
            metadata.CorrelationIdEnabled,
            metadata.CausationIdEnabled,
            metadata.RequiresHeaderStorage());
    }

    private static int CountConfiguredHeaders(EventMetadataOptions metadata)
    {
        var count = metadata.CustomHeaders.Count;

        if (metadata.CaptureUserId)
        {
            count++;
        }

        if (metadata.CaptureTenantId)
        {
            count++;
        }

        if (metadata.CaptureCommitSha && !string.IsNullOrWhiteSpace(metadata.CommitSha))
        {
            count++;
        }

        if (metadata.CaptureSemanticVersion && !string.IsNullOrWhiteSpace(metadata.SemanticVersion))
        {
            count++;
        }

        if (metadata.CaptureTimestamp)
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// High-performance logging methods using LoggerMessage source generators.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3210,
            Level = LogLevel.Debug,
            Message = "Event metadata tracking is disabled")]
        public static partial void MetadataTrackingDisabled(ILogger logger);

        [LoggerMessage(
            EventId = 3211,
            Level = LogLevel.Debug,
            Message = "Correlation ID tracking enabled for event metadata")]
        public static partial void CorrelationIdEnabled(ILogger logger);

        [LoggerMessage(
            EventId = 3212,
            Level = LogLevel.Debug,
            Message = "Causation ID tracking enabled for event metadata")]
        public static partial void CausationIdEnabled(ILogger logger);

        [LoggerMessage(
            EventId = 3213,
            Level = LogLevel.Debug,
            Message = "Event headers storage enabled for custom metadata")]
        public static partial void HeadersEnabled(ILogger logger);

        [LoggerMessage(
            EventId = 3214,
            Level = LogLevel.Debug,
            Message = "Configured {HeaderCount} custom headers for event metadata")]
        public static partial void CustomHeadersConfigured(ILogger logger, int headerCount);

        [LoggerMessage(
            EventId = 3215,
            Level = LogLevel.Information,
            Message = "Event metadata tracking configured: CorrelationId={CorrelationIdEnabled}, CausationId={CausationIdEnabled}, Headers={HeadersEnabled}")]
        public static partial void MetadataTrackingConfigured(
            ILogger logger,
            bool correlationIdEnabled,
            bool causationIdEnabled,
            bool headersEnabled);
    }
}
