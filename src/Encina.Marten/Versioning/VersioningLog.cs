using Microsoft.Extensions.Logging;

namespace Encina.Marten.Versioning;

/// <summary>
/// High-performance logging methods for event versioning using LoggerMessage source generators.
/// <para>Event IDs: 2617-2628 (see <see cref="Encina.Diagnostics.EventIdRanges.Marten"/>).</para>
/// </summary>
internal static partial class VersioningLog
{
    // Event Versioning - Registration
    [LoggerMessage(
        EventId = 2617,
        Level = LogLevel.Debug,
        Message = "Registering event upcaster {UpcasterType} for source event type '{SourceEventTypeName}'")]
    public static partial void RegisteringUpcaster(ILogger logger, string upcasterType, string sourceEventTypeName);

    [LoggerMessage(
        EventId = 2618,
        Level = LogLevel.Information,
        Message = "Registered {Count} event upcasters")]
    public static partial void RegisteredUpcasters(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2619,
        Level = LogLevel.Warning,
        Message = "Duplicate upcaster registration for event type '{SourceEventTypeName}'. Existing upcaster will be used.")]
    public static partial void DuplicateUpcasterRegistration(ILogger logger, string sourceEventTypeName);

    [LoggerMessage(
        EventId = 2620,
        Level = LogLevel.Debug,
        Message = "Scanning assembly '{AssemblyName}' for event upcasters")]
    public static partial void ScanningAssemblyForUpcasters(ILogger logger, string assemblyName);

    [LoggerMessage(
        EventId = 2621,
        Level = LogLevel.Information,
        Message = "Found {Count} event upcasters in assembly '{AssemblyName}'")]
    public static partial void FoundUpcastersInAssembly(ILogger logger, int count, string assemblyName);

    // Event Versioning - Upcasting
    [LoggerMessage(
        EventId = 2622,
        Level = LogLevel.Debug,
        Message = "Upcasting event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void UpcastingEvent(ILogger logger, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 2623,
        Level = LogLevel.Debug,
        Message = "Successfully upcasted event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void UpcastedEvent(ILogger logger, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 2624,
        Level = LogLevel.Error,
        Message = "Failed to upcast event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void FailedToUpcastEvent(ILogger logger, Exception exception, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 2625,
        Level = LogLevel.Warning,
        Message = "No upcaster found for event type '{EventTypeName}'")]
    public static partial void NoUpcasterFound(ILogger logger, string eventTypeName);

    // Event Versioning - Configuration
    [LoggerMessage(
        EventId = 2626,
        Level = LogLevel.Information,
        Message = "Event versioning enabled with {Count} upcasters configured")]
    public static partial void EventVersioningEnabled(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2627,
        Level = LogLevel.Debug,
        Message = "Configuring Marten with {Count} event upcasters")]
    public static partial void ConfiguringMartenUpcasters(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2628,
        Level = LogLevel.Debug,
        Message = "Adding upcaster '{UpcasterType}' to Marten event store")]
    public static partial void AddingUpcasterToMarten(ILogger logger, string upcasterType);
}
