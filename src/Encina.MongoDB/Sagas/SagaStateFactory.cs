using Encina.Messaging.Sagas;

namespace Encina.MongoDB.Sagas;

/// <summary>
/// Factory for creating MongoDB saga state instances.
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
        DateTime startedAtUtc)
    {
        return new SagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = data,
            Status = status,
            CurrentStep = currentStep,
            StartedAtUtc = startedAtUtc,
            LastUpdatedAtUtc = startedAtUtc
        };
    }
}
