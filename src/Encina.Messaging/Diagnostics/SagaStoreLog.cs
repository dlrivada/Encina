using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for saga store operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2200-2299 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class SagaStoreLog
{
    /// <summary>Logs when loading a saga from the store.</summary>
    [LoggerMessage(
        EventId = 2200,
        Level = LogLevel.Debug,
        Message = "Loading saga {SagaId} ({SagaType})")]
    public static partial void LoadingSaga(
        ILogger logger,
        string sagaId,
        string sagaType);

    /// <summary>Logs when a saga has been loaded from the store.</summary>
    [LoggerMessage(
        EventId = 2201,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} loaded (CurrentStep: {CurrentStep})")]
    public static partial void SagaLoaded(
        ILogger logger,
        string sagaId,
        string currentStep);

    /// <summary>Logs when saving a saga step transition.</summary>
    [LoggerMessage(
        EventId = 2202,
        Level = LogLevel.Debug,
        Message = "Saving saga {SagaId} transition from {FromStep} to {ToStep}")]
    public static partial void SavingTransition(
        ILogger logger,
        string sagaId,
        string fromStep,
        string toStep);

    /// <summary>Logs when a saga has completed.</summary>
    [LoggerMessage(
        EventId = 2203,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} completed")]
    public static partial void SagaCompleted(
        ILogger logger,
        string sagaId);

    /// <summary>Logs when a saga store operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 2204,
        Level = LogLevel.Warning,
        Message = "Saga store operation failed: {ErrorMessage}")]
    public static partial void OperationFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Logs when a saga store operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 2205,
        Level = LogLevel.Error,
        Message = "Saga store operation threw an unexpected exception")]
    public static partial void OperationException(
        ILogger logger,
        Exception exception);
}
