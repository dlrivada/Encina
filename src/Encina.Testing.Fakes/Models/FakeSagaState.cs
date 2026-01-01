using Encina.Messaging.Sagas;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="ISagaState"/> for testing.
/// </summary>
public sealed class FakeSagaState : ISagaState
{
    /// <summary>
    /// Sentinel value indicating a completed saga (no more steps to execute).
    /// </summary>
    public const int CompletedStep = 99;

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
    /// Creates a shallow copy of this saga state with the same property values.
    /// </summary>
    /// <returns>A new <see cref="FakeSagaState"/> instance with identical property values.</returns>
    /// <remarks>
    /// <para>
    /// This performs a shallow copy. The string properties (<see cref="SagaType"/>, <see cref="Data"/>,
    /// <see cref="Status"/>, and <see cref="ErrorMessage"/>) are reference types, but since strings
    /// are immutable in .NET, it is safe to share their references between the original and cloned instances.
    /// </para>
    /// <para>
    /// The remaining properties (<see cref="SagaId"/>, <see cref="CurrentStep"/>, <see cref="StartedAtUtc"/>,
    /// <see cref="CompletedAtUtc"/>, <see cref="LastUpdatedAtUtc"/>, and <see cref="TimeoutAtUtc"/>)
    /// are value types and are copied by value.
    /// </para>
    /// </remarks>
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
