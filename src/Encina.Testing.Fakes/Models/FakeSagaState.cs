using Encina.Messaging.Sagas;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="ISagaState"/> for testing.
/// </summary>
public sealed class FakeSagaState : ISagaState
{
    /// <inheritdoc />
    public Guid SagaId { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string SagaType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Data { get; set; } = "{}";

    /// <inheritdoc />
    public string Status { get; set; } = "Running";

    /// <inheritdoc />
    public int CurrentStep { get; set; }

    /// <inheritdoc />
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? CompletedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? TimeoutAtUtc { get; set; }

    /// <summary>
    /// Creates a deep copy of this saga state.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeSagaState Clone() => new()
    {
        SagaId = SagaId,
        SagaType = SagaType,
        Data = Data,
        Status = Status,
        CurrentStep = CurrentStep,
        StartedAtUtc = StartedAtUtc,
        CompletedAtUtc = CompletedAtUtc,
        ErrorMessage = ErrorMessage,
        LastUpdatedAtUtc = LastUpdatedAtUtc,
        TimeoutAtUtc = TimeoutAtUtc
    };
}
