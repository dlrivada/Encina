using BenchmarkDotNet.Attributes;
using Encina.Messaging.ScatterGather;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks for Scatter-Gather pattern performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ScatterGatherBenchmarks
{
    private ScatterGatherRunner _runner = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _simpleDefinition = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _parallelDefinition = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _sequentialDefinition = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _quorumDefinition = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _waitForFirstDefinition = null!;
    private BuiltScatterGatherDefinition<TestRequest, TestResponse> _manyHandlersDefinition = null!;
    private TestRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new ScatterGatherOptions();
        var logger = NullLogger<ScatterGatherRunner>.Instance;
        _runner = new ScatterGatherRunner(options, logger);

        _request = new TestRequest("benchmark");

        // Simple definition with one handler
        _simpleDefinition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Simple")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Parallel execution with multiple handlers
        _parallelDefinition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Parallel")
            .ScatterTo("H1", req => Right<EncinaError, TestResponse>(new TestResponse(1)))
            .ScatterTo("H2", req => Right<EncinaError, TestResponse>(new TestResponse(2)))
            .ScatterTo("H3", req => Right<EncinaError, TestResponse>(new TestResponse(3)))
            .ScatterTo("H4", req => Right<EncinaError, TestResponse>(new TestResponse(4)))
            .ScatterTo("H5", req => Right<EncinaError, TestResponse>(new TestResponse(5)))
            .ExecuteInParallel()
            .GatherAll()
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(results.Sum(r => r.Value))))
            .Build();

        // Sequential execution with multiple handlers
        _sequentialDefinition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Sequential")
            .ScatterTo("H1", req => Right<EncinaError, TestResponse>(new TestResponse(1)))
            .ScatterTo("H2", req => Right<EncinaError, TestResponse>(new TestResponse(2)))
            .ScatterTo("H3", req => Right<EncinaError, TestResponse>(new TestResponse(3)))
            .ScatterTo("H4", req => Right<EncinaError, TestResponse>(new TestResponse(4)))
            .ScatterTo("H5", req => Right<EncinaError, TestResponse>(new TestResponse(5)))
            .ExecuteSequentially()
            .GatherAll()
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(results.Sum(r => r.Value))))
            .Build();

        // Quorum strategy
        var quorumBuilder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Quorum");
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            quorumBuilder.ScatterTo($"H{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }
        _quorumDefinition = quorumBuilder
            .GatherQuorum(5)
            .TakeFirst()
            .Build();

        // WaitForFirst strategy
        _waitForFirstDefinition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("WaitForFirst")
            .ScatterTo("H1", req => Right<EncinaError, TestResponse>(new TestResponse(1)))
            .ScatterTo("H2", req => Right<EncinaError, TestResponse>(new TestResponse(2)))
            .ScatterTo("H3", req => Right<EncinaError, TestResponse>(new TestResponse(3)))
            .GatherFirst()
            .TakeFirst()
            .Build();

        // Many handlers
        var manyBuilder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("ManyHandlers");
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            manyBuilder.ScatterTo($"H{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }
        _manyHandlersDefinition = manyBuilder
            .ExecuteInParallel()
            .GatherAll()
            .TakeFirst()
            .Build();
    }

    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> SingleHandler()
    {
        return await _runner.ExecuteAsync(_simpleDefinition, _request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> FiveHandlers_Parallel()
    {
        return await _runner.ExecuteAsync(_parallelDefinition, _request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> FiveHandlers_Sequential()
    {
        return await _runner.ExecuteAsync(_sequentialDefinition, _request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> TenHandlers_Quorum()
    {
        return await _runner.ExecuteAsync(_quorumDefinition, _request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> ThreeHandlers_WaitForFirst()
    {
        return await _runner.ExecuteAsync(_waitForFirstDefinition, _request);
    }

    [Benchmark]
    public async Task<Either<EncinaError, ScatterGatherResult<TestResponse>>> FiftyHandlers_Parallel()
    {
        return await _runner.ExecuteAsync(_manyHandlersDefinition, _request);
    }

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);
}
