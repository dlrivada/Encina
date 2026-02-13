using NBomber.Contracts;

namespace Encina.NBomber.Scenarios.IdGeneration;

/// <summary>
/// Runner that creates and manages ID generation load test scenarios.
/// </summary>
public sealed class IdGenerationScenarioRunner : IAsyncDisposable
{
    private readonly IdGenerationFeature _feature;
    private readonly int _rate;
    private readonly TimeSpan _duration;
    private IdGenerationScenarioContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerationScenarioRunner"/> class.
    /// </summary>
    /// <param name="feature">The ID generation feature to test.</param>
    /// <param name="rate">Injection rate per second (default: 10,000).</param>
    /// <param name="duration">Duration for each scenario (default: 1 minute).</param>
    public IdGenerationScenarioRunner(
        IdGenerationFeature feature = IdGenerationFeature.All,
        int rate = 10_000,
        TimeSpan? duration = null)
    {
        _feature = feature;
        _rate = rate;
        _duration = duration ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Creates all configured ID generation scenarios.
    /// </summary>
    /// <returns>An array of NBomber scenario configurations.</returns>
    public Task<ScenarioProps[]> CreateScenariosAsync()
    {
        _context = new IdGenerationScenarioContext();
        var factory = new IdGenerationScenarioFactory(_context, _rate, _duration);
        var scenarios = factory.CreateScenarios(_feature).ToArray();
        return Task.FromResult(scenarios);
    }

    /// <summary>
    /// Gets the scenario context for collision summary access.
    /// </summary>
    public IdGenerationScenarioContext? Context => _context;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _context?.PrintCollisionSummary();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Feature categories for ID generation load testing.
/// </summary>
public enum IdGenerationFeature
{
    /// <summary>Snowflake ID generation scenarios.</summary>
    Snowflake,

    /// <summary>ULID generation scenarios.</summary>
    Ulid,

    /// <summary>UUIDv7 generation scenarios.</summary>
    UuidV7,

    /// <summary>ShardPrefixed ID generation scenarios.</summary>
    ShardPrefixed,

    /// <summary>All ID generation scenarios.</summary>
    All
}
