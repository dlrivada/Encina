using Microsoft.Extensions.Logging;

namespace Encina.Marten.Versioning;

/// <summary>
/// High-performance logging methods for event versioning using LoggerMessage source generators.
/// </summary>
internal static partial class VersioningLog
{
    // Event Versioning - Registration (100-109)
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Debug,
        Message = "Registering event upcaster {UpcasterType} for source event type '{SourceEventTypeName}'")]
    public static partial void RegisteringUpcaster(ILogger logger, string upcasterType, string sourceEventTypeName);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Registered {Count} event upcasters")]
    public static partial void RegisteredUpcasters(ILogger logger, int count);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Duplicate upcaster registration for event type '{SourceEventTypeName}'. Existing upcaster will be used.")]
    public static partial void DuplicateUpcasterRegistration(ILogger logger, string sourceEventTypeName);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Debug,
        Message = "Scanning assembly '{AssemblyName}' for event upcasters")]
    public static partial void ScanningAssemblyForUpcasters(ILogger logger, string assemblyName);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Information,
        Message = "Found {Count} event upcasters in assembly '{AssemblyName}'")]
    public static partial void FoundUpcastersInAssembly(ILogger logger, int count, string assemblyName);

    // Event Versioning - Upcasting (110-119)
    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Debug,
        Message = "Upcasting event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void UpcastingEvent(ILogger logger, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 111,
        Level = LogLevel.Debug,
        Message = "Successfully upcasted event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void UpcastedEvent(ILogger logger, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 112,
        Level = LogLevel.Error,
        Message = "Failed to upcast event '{SourceEventTypeName}' to '{TargetEventTypeName}'")]
    public static partial void FailedToUpcastEvent(ILogger logger, Exception exception, string sourceEventTypeName, string targetEventTypeName);

    [LoggerMessage(
        EventId = 113,
        Level = LogLevel.Warning,
        Message = "No upcaster found for event type '{EventTypeName}'")]
    public static partial void NoUpcasterFound(ILogger logger, string eventTypeName);

    // Event Versioning - Configuration (120-129)
    [LoggerMessage(
        EventId = 120,
        Level = LogLevel.Information,
        Message = "Event versioning enabled with {Count} upcasters configured")]
    public static partial void EventVersioningEnabled(ILogger logger, int count);

    [LoggerMessage(
        EventId = 121,
        Level = LogLevel.Debug,
        Message = "Configuring Marten with {Count} event upcasters")]
    public static partial void ConfiguringMartenUpcasters(ILogger logger, int count);

    [LoggerMessage(
        EventId = 122,
        Level = LogLevel.Debug,
        Message = "Adding upcaster '{UpcasterType}' to Marten event store")]
    public static partial void AddingUpcasterToMarten(ILogger logger, string upcasterType);
}
