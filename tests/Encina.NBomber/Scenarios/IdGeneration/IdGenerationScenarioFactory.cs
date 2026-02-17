using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.IdGeneration;

/// <summary>
/// Factory for creating ID generation load test scenarios.
/// Tests throughput, collision detection, and shard extraction across all four ID types.
/// </summary>
public sealed class IdGenerationScenarioFactory
{
    private readonly IdGenerationScenarioContext _context;
    private readonly int _rate;
    private readonly TimeSpan _duration;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerationScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The shared scenario context for collision tracking.</param>
    /// <param name="rate">Injection rate per second.</param>
    /// <param name="duration">Duration for each scenario.</param>
    public IdGenerationScenarioFactory(
        IdGenerationScenarioContext context,
        int rate = 10_000,
        TimeSpan? duration = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _rate = rate;
        _duration = duration ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Creates ID generation scenarios for the specified feature.
    /// </summary>
    /// <returns>A collection of NBomber scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios(IdGenerationFeature feature)
    {
        return feature switch
        {
            IdGenerationFeature.Snowflake => [.. CreateSnowflakeScenarios()],
            IdGenerationFeature.Ulid => [.. CreateUlidScenarios()],
            IdGenerationFeature.UuidV7 => [.. CreateUuidV7Scenarios()],
            IdGenerationFeature.ShardPrefixed => [.. CreateShardPrefixedScenarios()],
            IdGenerationFeature.All => [.. CreateAllScenarios()],
            _ => [.. CreateAllScenarios()]
        };
    }

    private IEnumerable<ScenarioProps> CreateAllScenarios()
    {
        foreach (var scenario in CreateSnowflakeScenarios()) yield return scenario;
        foreach (var scenario in CreateUlidScenarios()) yield return scenario;
        foreach (var scenario in CreateUuidV7Scenarios()) yield return scenario;
        foreach (var scenario in CreateShardPrefixedScenarios()) yield return scenario;
    }

    // ────────────────────────────────────────────────────────────
    //  Snowflake scenarios
    // ────────────────────────────────────────────────────────────

    private IEnumerable<ScenarioProps> CreateSnowflakeScenarios()
    {
        yield return CreateSnowflakeThroughputScenario();
        yield return CreateSnowflakeShardedScenario();
    }

    private ScenarioProps CreateSnowflakeThroughputScenario()
    {
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions());

        return Scenario.Create(
            name: "idgen-snowflake-throughput",
            run: async context =>
            {
                try
                {
                    var result = generator.Generate();
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);
                    if (!_context.SnowflakeIds.TryAdd(id.Value, 0))
                    {
                        _context.RecordSnowflakeCollision();
                        return Response.Fail("Snowflake collision detected", statusCode: "collision");
                    }

                    return Response.Ok(sizeBytes: sizeof(long));
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }

    private ScenarioProps CreateSnowflakeShardedScenario()
    {
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions());

        return Scenario.Create(
            name: "idgen-snowflake-sharded",
            run: async context =>
            {
                try
                {
                    var shardId = _context.GetNextNumericShardId();
                    var result = generator.Generate(shardId);
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);

                    // Verify shard extraction roundtrip
                    var extractResult = generator.ExtractShardId(id);
                    if (extractResult.IsLeft)
                    {
                        return Response.Fail(extractResult.LeftToList().First().Message, statusCode: "extract_error");
                    }

                    var extracted = extractResult.Match(s => s, _ => string.Empty);
                    if (extracted != shardId)
                    {
                        return Response.Fail(
                            $"Shard mismatch: expected {shardId}, got {extracted}",
                            statusCode: "shard_mismatch");
                    }

                    if (!_context.SnowflakeIds.TryAdd(id.Value, 0))
                    {
                        _context.RecordSnowflakeCollision();
                        return Response.Fail("Snowflake collision detected", statusCode: "collision");
                    }

                    return Response.Ok(sizeBytes: sizeof(long));
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate / 2,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }

    // ────────────────────────────────────────────────────────────
    //  ULID scenarios
    // ────────────────────────────────────────────────────────────

    private IEnumerable<ScenarioProps> CreateUlidScenarios()
    {
        yield return CreateUlidThroughputScenario();
    }

    private ScenarioProps CreateUlidThroughputScenario()
    {
        var generator = new UlidIdGenerator();

        return Scenario.Create(
            name: "idgen-ulid-throughput",
            run: async context =>
            {
                try
                {
                    var result = generator.Generate();
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);
                    var str = id.ToString();
                    if (!_context.UlidIds.TryAdd(str, 0))
                    {
                        _context.RecordUlidCollision();
                        return Response.Fail("ULID collision detected", statusCode: "collision");
                    }

                    return Response.Ok(sizeBytes: 26);
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }

    // ────────────────────────────────────────────────────────────
    //  UUIDv7 scenarios
    // ────────────────────────────────────────────────────────────

    private IEnumerable<ScenarioProps> CreateUuidV7Scenarios()
    {
        yield return CreateUuidV7ThroughputScenario();
    }

    private ScenarioProps CreateUuidV7ThroughputScenario()
    {
        var generator = new UuidV7IdGenerator();

        return Scenario.Create(
            name: "idgen-uuidv7-throughput",
            run: async context =>
            {
                try
                {
                    var result = generator.Generate();
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);
                    if (!_context.UuidV7Ids.TryAdd(id.Value, 0))
                    {
                        _context.RecordUuidV7Collision();
                        return Response.Fail("UUIDv7 collision detected", statusCode: "collision");
                    }

                    return Response.Ok(sizeBytes: 16);
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }

    // ────────────────────────────────────────────────────────────
    //  ShardPrefixed scenarios
    // ────────────────────────────────────────────────────────────

    private IEnumerable<ScenarioProps> CreateShardPrefixedScenarios()
    {
        yield return CreateShardPrefixedThroughputScenario();
        yield return CreateShardPrefixedExtractionScenario();
    }

    private ScenarioProps CreateShardPrefixedThroughputScenario()
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions());

        return Scenario.Create(
            name: "idgen-shardprefixed-throughput",
            run: async context =>
            {
                try
                {
                    var shardId = _context.GetNextShardId();
                    var result = generator.Generate(shardId);
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);
                    var str = id.ToString();
                    if (!_context.ShardPrefixedIds.TryAdd(str, 0))
                    {
                        _context.RecordShardPrefixedCollision();
                        return Response.Fail("ShardPrefixed collision detected", statusCode: "collision");
                    }

                    return Response.Ok(sizeBytes: str.Length);
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }

    private ScenarioProps CreateShardPrefixedExtractionScenario()
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions());

        return Scenario.Create(
            name: "idgen-shardprefixed-extraction",
            run: async context =>
            {
                try
                {
                    var shardId = _context.GetNextShardId();
                    var result = generator.Generate(shardId);
                    if (result.IsLeft)
                    {
                        return Response.Fail(result.LeftToList().First().Message, statusCode: "generation_error");
                    }

                    var id = result.Match(id => id, _ => default);
                    var extractResult = generator.ExtractShardId(id);
                    if (extractResult.IsLeft)
                    {
                        return Response.Fail(extractResult.LeftToList().First().Message, statusCode: "extract_error");
                    }

                    var extracted = extractResult.Match(s => s, _ => string.Empty);
                    if (extracted != shardId)
                    {
                        return Response.Fail(
                            $"Shard mismatch: expected {shardId}, got {extracted}",
                            statusCode: "shard_mismatch");
                    }

                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: _rate / 2,
                    interval: TimeSpan.FromSeconds(1),
                    during: _duration));
    }
}
