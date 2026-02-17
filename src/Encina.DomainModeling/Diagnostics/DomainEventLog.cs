using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for domain event operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2500-2599 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class DomainEventLog
{
    /// <summary>Logs when a domain event is being raised.</summary>
    [LoggerMessage(
        EventId = 2500,
        Level = LogLevel.Debug,
        Message = "Raising domain event {EventType}")]
    public static partial void RaisingEvent(
        ILogger logger,
        string eventType);

    /// <summary>Logs when a domain event has been handled.</summary>
    [LoggerMessage(
        EventId = 2501,
        Level = LogLevel.Debug,
        Message = "Domain event {EventType} handled")]
    public static partial void EventHandled(
        ILogger logger,
        string eventType);

    /// <summary>Logs when dispatching multiple domain events.</summary>
    [LoggerMessage(
        EventId = 2502,
        Level = LogLevel.Debug,
        Message = "Dispatching {Count} domain events")]
    public static partial void DispatchingEvents(
        ILogger logger,
        int count);

    /// <summary>Logs when a domain event handler fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 2503,
        Level = LogLevel.Warning,
        Message = "Domain event {EventType} handler failed: {ErrorMessage}")]
    public static partial void HandlerFailed(
        ILogger logger,
        string eventType,
        string errorMessage);

    /// <summary>Logs when domain event dispatch throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 2504,
        Level = LogLevel.Error,
        Message = "Domain event dispatch failed")]
    public static partial void DispatchException(
        ILogger logger,
        Exception exception);
}
