using Bogus;
using Encina.Messaging.Sagas;

namespace Encina.Testing.Bogus;

/// <summary>
/// Faker for generating realistic <see cref="ISagaState"/> test data.
/// </summary>
/// <remarks>
/// <para>
/// Generates saga states with realistic saga types, data, and lifecycle states.
/// The generated sagas can be configured to represent different lifecycle phases:
/// running, completed, compensating, or failed.
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// var faker = new SagaStateFaker();
/// var runningSaga = faker.Generate();
///
/// var completedSaga = new SagaStateFaker()
///     .AsCompleted()
///     .Generate();
///
/// var failedSaga = new SagaStateFaker()
///     .AsFailed("Compensation failed")
///     .Generate();
/// </code>
/// </para>
/// </remarks>
public sealed class SagaStateFaker : Faker<FakeSagaState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStateFaker"/> class.
    /// </summary>
    /// <param name="locale">The locale for generating localized data (default: "en").</param>
    public SagaStateFaker(string locale = "en")
        : base(locale)
    {
        UseSeed(EncinaFaker<object>.DefaultSeed);
        CustomInstantiator(_ => new FakeSagaState());

        RuleFor(s => s.SagaId, f => f.Random.Guid());
        RuleFor(s => s.SagaType, f => f.SagaType());
        RuleFor(s => s.Data, f => f.JsonContent(5));
        RuleFor(s => s.Status, _ => SagaStatus.Running);
        RuleFor(s => s.CurrentStep, f => f.Random.Int(0, 3));
        RuleFor(s => s.StartedAtUtc, f => f.Date.RecentUtc(7));
        RuleFor(s => s.CompletedAtUtc, _ => null);
        RuleFor(s => s.ErrorMessage, _ => null);
        RuleFor(s => s.LastUpdatedAtUtc, f => f.Date.RecentUtc(1));
        RuleFor(s => s.TimeoutAtUtc, f => f.Date.SoonUtc(1));
    }

    /// <summary>
    /// Configures the faker to generate completed sagas.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker AsCompleted()
    {
        RuleFor(s => s.Status, _ => SagaStatus.Completed);
        RuleFor(s => s.CompletedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate compensating sagas.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker AsCompensating()
    {
        RuleFor(s => s.Status, _ => SagaStatus.Compensating);
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failed sagas.
    /// </summary>
    /// <param name="errorMessage">Optional error message.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker AsFailed(string? errorMessage = null)
    {
        RuleFor(s => s.Status, _ => SagaStatus.Failed);
        RuleFor(s => s.ErrorMessage, f => errorMessage ?? f.Lorem.Sentence());
        RuleFor(s => s.CompletedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate timed out sagas.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker AsTimedOut()
    {
        RuleFor(s => s.Status, _ => SagaStatus.TimedOut);
        RuleFor(s => s.TimeoutAtUtc, f => f.Date.RecentUtc(1));
        RuleFor(s => s.CompletedAtUtc, f => f.Date.RecentUtc(1));
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific saga type.
    /// </summary>
    /// <param name="sagaType">The saga type name.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker WithSagaType(string sagaType)
    {
        RuleFor(s => s.SagaType, _ => sagaType);
        return this;
    }

    /// <summary>
    /// Configures the faker to use a specific saga ID.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker WithSagaId(Guid sagaId)
    {
        RuleFor(s => s.SagaId, _ => sagaId);
        return this;
    }

    /// <summary>
    /// Configures the faker to use specific data.
    /// </summary>
    /// <param name="data">The saga data as JSON.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public SagaStateFaker WithData(string data)
    {
        RuleFor(s => s.Data, _ => data);
        return this;
    }

    /// <summary>
    /// Configures the faker with a specific current step.
    /// </summary>
    /// <param name="step">The step index (must be non-negative).</param>
    /// <returns>This faker instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="step"/> is negative.</exception>
    public SagaStateFaker AtStep(int step)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(step);

        RuleFor(s => s.CurrentStep, _ => step);
        return this;
    }

    /// <summary>
    /// Generates a saga state as <see cref="ISagaState"/>.
    /// </summary>
    /// <returns>A generated saga state.</returns>
    public ISagaState GenerateState() => Generate();
}

/// <summary>
/// Concrete implementation of <see cref="ISagaState"/> for testing.
/// </summary>
public sealed class FakeSagaState : ISagaState
{
    /// <inheritdoc/>
    public Guid SagaId { get; set; }

    /// <inheritdoc/>
    public string SagaType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Data { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Status { get; set; } = string.Empty;

    /// <inheritdoc/>
    public int CurrentStep { get; set; }

    /// <inheritdoc/>
    public DateTime StartedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? CompletedAtUtc { get; set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <inheritdoc/>
    public DateTime LastUpdatedAtUtc { get; set; }

    /// <inheritdoc/>
    public DateTime? TimeoutAtUtc { get; set; }
}
