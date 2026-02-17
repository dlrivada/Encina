using Encina.Dapper.Sqlite.Sagas;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating SagaState test data.
/// Provides fluent API for constructing test saga states with sensible defaults.
/// </summary>
public sealed class SagaStateBuilder
{
    private Guid _sagaId = Guid.NewGuid();
    private string _sagaType = "TestSaga";
    private int _currentStep;
    private string _status = "Running";
    private string _data = "{}";
    private DateTime _startedAtUtc = DateTime.UtcNow;
    private DateTime _lastUpdatedAtUtc = DateTime.UtcNow;
    private DateTime? _completedAtUtc;
    private string? _errorMessage;

    /// <summary>
    /// Sets the saga ID.
    /// </summary>
    public SagaStateBuilder WithSagaId(Guid sagaId)
    {
        _sagaId = sagaId;
        return this;
    }

    /// <summary>
    /// Sets the saga type.
    /// </summary>
    public SagaStateBuilder WithSagaType(string sagaType)
    {
        _sagaType = sagaType;
        return this;
    }

    /// <summary>
    /// Sets the current step.
    /// </summary>
    public SagaStateBuilder WithCurrentStep(int currentStep)
    {
        _currentStep = currentStep;
        return this;
    }

    /// <summary>
    /// Sets the saga status.
    /// </summary>
    public SagaStateBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Sets the saga data (JSON).
    /// </summary>
    public SagaStateBuilder WithData(string data)
    {
        _data = data;
        return this;
    }

    /// <summary>
    /// Sets the started timestamp.
    /// </summary>
    public SagaStateBuilder WithStartedAtUtc(DateTime startedAtUtc)
    {
        _startedAtUtc = startedAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the last updated timestamp.
    /// </summary>
    public SagaStateBuilder WithLastUpdatedAtUtc(DateTime lastUpdatedAtUtc)
    {
        _lastUpdatedAtUtc = lastUpdatedAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the saga as completed.
    /// </summary>
    public SagaStateBuilder AsCompleted(DateTime? completedAtUtc = null)
    {
        _status = "Completed";
        _completedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the saga as compensating.
    /// </summary>
    public SagaStateBuilder AsCompensating()
    {
        _status = "Compensating";
        return this;
    }

    /// <summary>
    /// Marks the saga as compensated.
    /// </summary>
    public SagaStateBuilder AsCompensated(DateTime? completedAtUtc = null)
    {
        _status = "Compensated";
        _completedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the saga as failed with an error.
    /// </summary>
    public SagaStateBuilder WithError(string errorMessage)
    {
        _status = "Failed";
        _errorMessage = errorMessage;
        _completedAtUtc = DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Builds the SagaState instance.
    /// </summary>
    public SagaState Build() => new()
    {
        SagaId = _sagaId,
        SagaType = _sagaType,
        CurrentStep = _currentStep,
        Status = _status,
        Data = _data,
        StartedAtUtc = _startedAtUtc,
        LastUpdatedAtUtc = _lastUpdatedAtUtc,
        CompletedAtUtc = _completedAtUtc,
        ErrorMessage = _errorMessage
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static SagaStateBuilder Create() => new();
}
