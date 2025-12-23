using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Sagas;

/// <summary>
/// Orchestrates the Saga Pattern for distributed transactions with compensation.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator contains all domain logic for the Saga Pattern (orchestration-based),
/// delegating persistence operations to <see cref="ISagaStore"/>. It manages the lifecycle
/// of sagas through their steps and handles compensation on failure.
/// </para>
/// <para>
/// <b>Saga Lifecycle</b>:
/// <list type="number">
/// <item><description>Start saga with initial data</description></item>
/// <item><description>Execute steps sequentially</description></item>
/// <item><description>On success, mark as completed</description></item>
/// <item><description>On failure, execute compensation steps in reverse order</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SagaOrchestrator
{
    private readonly ISagaStore _store;
    private readonly SagaOptions _options;
    private readonly ILogger<SagaOrchestrator> _logger;
    private readonly ISagaStateFactory _stateFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The saga store for persistence.</param>
    /// <param name="options">The saga options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="stateFactory">Factory to create saga state.</param>
    public SagaOrchestrator(
        ISagaStore store,
        SagaOptions options,
        ILogger<SagaOrchestrator> logger,
        ISagaStateFactory stateFactory)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(stateFactory);

        _store = store;
        _options = options;
        _logger = logger;
        _stateFactory = stateFactory;
    }

    /// <summary>
    /// Starts a new saga.
    /// </summary>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="data">The initial saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga ID.</returns>
    public async Task<Guid> StartAsync<TSagaData>(
        string sagaType,
        TSagaData data,
        CancellationToken cancellationToken = default)
        where TSagaData : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaType);
        ArgumentNullException.ThrowIfNull(data);

        var sagaId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var serializedData = JsonSerializer.Serialize(data, JsonOptions);

        var state = _stateFactory.Create(
            sagaId,
            sagaType,
            serializedData,
            SagaStatus.Running,
            currentStep: 0,
            now);

        await _store.AddAsync(state, cancellationToken).ConfigureAwait(false);

        Log.SagaStarted(_logger, sagaId, sagaType);

        return sagaId;
    }

    /// <summary>
    /// Advances the saga to the next step.
    /// </summary>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="updateData">Function to update the saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of advancing the saga.</returns>
    public async Task<Either<EncinaError, SagaAdvanceResult<TSagaData>>> AdvanceAsync<TSagaData>(
        Guid sagaId,
        Func<TSagaData, TSagaData>? updateData = null,
        CancellationToken cancellationToken = default)
        where TSagaData : class
    {
        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            Log.SagaNotFound(_logger, sagaId);
            return EncinaErrors.Create(SagaErrorCodes.NotFound, $"Saga {sagaId} not found");
        }

        if (state.Status != SagaStatus.Running)
        {
            Log.SagaNotRunning(_logger, sagaId, state.Status);
            return EncinaErrors.Create(SagaErrorCodes.InvalidStatus, $"Saga is not running (status: {state.Status})");
        }

        var data = JsonSerializer.Deserialize<TSagaData>(state.Data, JsonOptions);
        if (data == null)
        {
            return EncinaErrors.Create(SagaErrorCodes.DeserializationFailed, "Failed to deserialize saga data");
        }

        if (updateData != null)
        {
            data = updateData(data);
            state.Data = JsonSerializer.Serialize(data, JsonOptions);
        }

        state.CurrentStep++;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        await _store.UpdateAsync(state, cancellationToken).ConfigureAwait(false);

        Log.SagaAdvanced(_logger, sagaId, state.CurrentStep);

        return new SagaAdvanceResult<TSagaData>(sagaId, state.CurrentStep, data);
    }

    /// <summary>
    /// Completes the saga successfully.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of completing the saga.</returns>
    public async Task<Either<EncinaError, Unit>> CompleteAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            return EncinaErrors.Create(SagaErrorCodes.NotFound, $"Saga {sagaId} not found");
        }

        if (state.Status != SagaStatus.Running)
        {
            return EncinaErrors.Create(SagaErrorCodes.InvalidStatus, $"Saga is not running (status: {state.Status})");
        }

        state.Status = SagaStatus.Completed;
        state.CompletedAtUtc = DateTime.UtcNow;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        await _store.UpdateAsync(state, cancellationToken).ConfigureAwait(false);

        Log.SagaCompleted(_logger, sagaId);

        return Unit.Default;
    }

    /// <summary>
    /// Starts compensation for a failed saga.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="errorMessage">The error that caused compensation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current step to start compensation from.</returns>
    public async Task<Either<EncinaError, int>> StartCompensationAsync(
        Guid sagaId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            return EncinaErrors.Create(SagaErrorCodes.NotFound, $"Saga {sagaId} not found");
        }

        if (state.Status is not (SagaStatus.Running or SagaStatus.Compensating))
        {
            return EncinaErrors.Create(SagaErrorCodes.InvalidStatus, $"Cannot compensate saga with status: {state.Status}");
        }

        state.Status = SagaStatus.Compensating;
        state.ErrorMessage = errorMessage;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        await _store.UpdateAsync(state, cancellationToken).ConfigureAwait(false);

        Log.SagaCompensating(_logger, sagaId, state.CurrentStep, errorMessage);

        return state.CurrentStep;
    }

    /// <summary>
    /// Records a compensation step completion.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The remaining step to compensate, or 0 if compensation is complete.</returns>
    public async Task<Either<EncinaError, int>> CompensateStepAsync(
        Guid sagaId,
        CancellationToken cancellationToken = default)
    {
        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            return EncinaErrors.Create(SagaErrorCodes.NotFound, $"Saga {sagaId} not found");
        }

        if (state.Status != SagaStatus.Compensating)
        {
            return EncinaErrors.Create(SagaErrorCodes.InvalidStatus, "Saga is not in compensating state");
        }

        state.CurrentStep--;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        if (state.CurrentStep <= 0)
        {
            state.Status = SagaStatus.Compensated;
            state.CompletedAtUtc = DateTime.UtcNow;
            Log.SagaCompensated(_logger, sagaId);
        }
        else
        {
            Log.SagaCompensationStep(_logger, sagaId, state.CurrentStep);
        }

        await _store.UpdateAsync(state, cancellationToken).ConfigureAwait(false);

        return state.CurrentStep;
    }

    /// <summary>
    /// Marks a saga as failed (compensation also failed).
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="errorMessage">The final error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of failing the saga.</returns>
    public async Task<Either<EncinaError, Unit>> FailAsync(
        Guid sagaId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            return EncinaErrors.Create(SagaErrorCodes.NotFound, $"Saga {sagaId} not found");
        }

        state.Status = SagaStatus.Failed;
        state.ErrorMessage = errorMessage;
        state.CompletedAtUtc = DateTime.UtcNow;
        state.LastUpdatedAtUtc = DateTime.UtcNow;

        await _store.UpdateAsync(state, cancellationToken).ConfigureAwait(false);

        Log.SagaFailed(_logger, sagaId, errorMessage);

        return Unit.Default;
    }

    /// <summary>
    /// Gets the current state of a saga.
    /// </summary>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found.</returns>
    public async Task<Option<SagaStateSnapshot<TSagaData>>> GetAsync<TSagaData>(
        Guid sagaId,
        CancellationToken cancellationToken = default)
        where TSagaData : class
    {
        var state = await _store.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);

        if (state == null)
        {
            return None;
        }

        var data = JsonSerializer.Deserialize<TSagaData>(state.Data, JsonOptions);
        if (data == null)
        {
            return None;
        }

        return Some(new SagaStateSnapshot<TSagaData>(
            state.SagaId,
            state.SagaType,
            data,
            state.Status,
            state.CurrentStep,
            state.StartedAtUtc,
            state.CompletedAtUtc,
            state.ErrorMessage));
    }

    /// <summary>
    /// Gets stuck sagas that may need intervention.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of stuck sagas.</returns>
    public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetStuckSagasAsync(
            _options.StuckSagaThreshold,
            _options.StuckSagaBatchSize,
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Result of advancing a saga.
/// </summary>
/// <typeparam name="TSagaData">The saga data type.</typeparam>
public sealed record SagaAdvanceResult<TSagaData>(
    Guid SagaId,
    int CurrentStep,
    TSagaData Data);

/// <summary>
/// Snapshot of a saga's state.
/// </summary>
/// <typeparam name="TSagaData">The saga data type.</typeparam>
public sealed record SagaStateSnapshot<TSagaData>(
    Guid SagaId,
    string SagaType,
    TSagaData Data,
    string Status,
    int CurrentStep,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage);

/// <summary>
/// Factory interface for creating saga state.
/// </summary>
/// <remarks>
/// Each provider (EF Core, Dapper, ADO.NET) implements this to create their specific state type.
/// </remarks>
public interface ISagaStateFactory
{
    /// <summary>
    /// Creates a new saga state.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="sagaType">The saga type.</param>
    /// <param name="data">The serialized data.</param>
    /// <param name="status">The initial status.</param>
    /// <param name="currentStep">The current step.</param>
    /// <param name="startedAtUtc">The start time.</param>
    /// <returns>A new saga state instance.</returns>
    ISagaState Create(
        Guid sagaId,
        string sagaType,
        string data,
        string status,
        int currentStep,
        DateTime startedAtUtc);
}

/// <summary>
/// Standard saga statuses.
/// </summary>
public static class SagaStatus
{
    /// <summary>
    /// Saga is currently executing steps.
    /// </summary>
    public const string Running = "Running";

    /// <summary>
    /// All steps completed successfully.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// Rolling back completed steps.
    /// </summary>
    public const string Compensating = "Compensating";

    /// <summary>
    /// Compensation completed successfully.
    /// </summary>
    public const string Compensated = "Compensated";

    /// <summary>
    /// Saga or compensation failed, manual intervention needed.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Error codes for saga operations.
/// </summary>
public static class SagaErrorCodes
{
    /// <summary>
    /// Saga not found.
    /// </summary>
    public const string NotFound = "saga.not_found";

    /// <summary>
    /// Invalid saga status for operation.
    /// </summary>
    public const string InvalidStatus = "saga.invalid_status";

    /// <summary>
    /// Failed to deserialize saga data.
    /// </summary>
    public const string DeserializationFailed = "saga.deserialization_failed";

    /// <summary>
    /// Saga step execution failed.
    /// </summary>
    public const string StepFailed = "saga.step_failed";

    /// <summary>
    /// Saga compensation failed.
    /// </summary>
    public const string CompensationFailed = "saga.compensation_failed";
}

/// <summary>
/// Configuration options for the Saga Pattern.
/// </summary>
public sealed class SagaOptions
{
    /// <summary>
    /// Gets or sets the threshold for detecting stuck sagas.
    /// </summary>
    /// <value>Default: 5 minutes</value>
    public TimeSpan StuckSagaThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the batch size for retrieving stuck sagas.
    /// </summary>
    /// <value>Default: 100</value>
    public int StuckSagaBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval for checking stuck sagas.
    /// </summary>
    /// <value>Default: 1 minute</value>
    public TimeSpan StuckSagaCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} started (type: {SagaType})")]
    public static partial void SagaStarted(ILogger logger, Guid sagaId, string sagaType);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} advanced to step {CurrentStep}")]
    public static partial void SagaAdvanced(ILogger logger, Guid sagaId, int currentStep);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} completed successfully")]
    public static partial void SagaCompleted(ILogger logger, Guid sagaId);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} starting compensation from step {CurrentStep}: {ErrorMessage}")]
    public static partial void SagaCompensating(ILogger logger, Guid sagaId, int currentStep, string errorMessage);

    [LoggerMessage(
        EventId = 205,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} compensation step {CurrentStep}")]
    public static partial void SagaCompensationStep(ILogger logger, Guid sagaId, int currentStep);

    [LoggerMessage(
        EventId = 206,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} compensation completed")]
    public static partial void SagaCompensated(ILogger logger, Guid sagaId);

    [LoggerMessage(
        EventId = 207,
        Level = LogLevel.Error,
        Message = "Saga {SagaId} failed: {ErrorMessage}")]
    public static partial void SagaFailed(ILogger logger, Guid sagaId, string errorMessage);

    [LoggerMessage(
        EventId = 208,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} not found")]
    public static partial void SagaNotFound(ILogger logger, Guid sagaId);

    [LoggerMessage(
        EventId = 209,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} is not running (status: {Status})")]
    public static partial void SagaNotRunning(ILogger logger, Guid sagaId, string status);
}
