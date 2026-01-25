using Encina.Messaging.Sagas;

namespace Encina.ADO.SqlServer.Sagas;

/// <summary>
/// Factory for creating ADO.NET SQL Server saga state instances.
/// </summary>
public sealed class SagaStateFactory : ISagaStateFactory
{
    /// <inheritdoc />
    public ISagaState Create(
        Guid sagaId,
        string sagaType,
        string data,
        string status,
        int currentStep,
        DateTime startedAtUtc,
        DateTime? timeoutAtUtc = null)
    {
        return new SagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = data,
            Status = status,
            CurrentStep = currentStep,
            StartedAtUtc = startedAtUtc,
            LastUpdatedAtUtc = startedAtUtc,
            TimeoutAtUtc = timeoutAtUtc
        };
    }
}
