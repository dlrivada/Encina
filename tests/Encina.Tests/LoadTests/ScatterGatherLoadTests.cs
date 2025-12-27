using Encina.Messaging.ScatterGather;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.LoadTests;

[Trait("Category", "Load")]
public sealed class ScatterGatherLoadTests
{
    private readonly ScatterGatherOptions _options = new();

    private ScatterGatherRunner CreateRunner() =>
        new(_options, NullLogger<ScatterGatherRunner>.Instance);

    [Fact]
    public async Task HighConcurrency_ManyScatterHandlers_AllComplete()
    {
        // Arrange
        const int handlerCount = 50;
        var runner = CreateRunner();

        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("LoadTest");
        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", async (req, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(index));
            });
        }

        var definition = builder
            .ExecuteInParallel()
            .GatherAll()
            .AggregateSuccessful(results =>
            {
                var sum = results.Sum(r => r.Value);
                return Right<EncinaError, TestResponse>(new TestResponse(sum));
            })
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.SuccessCount.ShouldBe(handlerCount);
                r.FailureCount.ShouldBe(0);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HighConcurrency_ParallelOperations_NoDataCorruption()
    {
        // Arrange
        const int operationCount = 100;
        var runner = CreateRunner();

        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("ConcurrencyTest")
            .ScatterTo("H1", req => Right<EncinaError, TestResponse>(new TestResponse(1)))
            .ScatterTo("H2", req => Right<EncinaError, TestResponse>(new TestResponse(2)))
            .ScatterTo("H3", req => Right<EncinaError, TestResponse>(new TestResponse(3)))
            .GatherAll()
            .AggregateSuccessful(results =>
            {
                var sum = results.Sum(r => r.Value);
                return Right<EncinaError, TestResponse>(new TestResponse(sum));
            })
            .Build();

        // Act
        var tasks = Enumerable.Range(0, operationCount)
            .Select(i => runner.ExecuteAsync(definition, new TestRequest($"test-{i}")).AsTask())
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.All(r => r.IsRight).ShouldBeTrue();
        results.All(r => r.Match(
            Right: result => result.Response.Value == 6,
            Left: _ => false)).ShouldBeTrue();
    }

    [Fact]
    public async Task HighLoad_WithQuorumStrategy_PerformsWell()
    {
        // Arrange
        const int handlerCount = 20;
        const int quorum = 10;
        var runner = CreateRunner();

        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("QuorumLoadTest");
        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", async (req, ct) =>
            {
                await Task.Delay(Random.Shared.Next(5, 50), ct);
                return Right<EncinaError, TestResponse>(new TestResponse(index));
            });
        }

        var definition = builder
            .ExecuteInParallel()
            .GatherQuorum(quorum)
            .AggregateSuccessful(results =>
            {
                var sum = results.Sum(r => r.Value);
                return Right<EncinaError, TestResponse>(new TestResponse(sum));
            })
            .Build();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));
        stopwatch.Stop();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.SuccessCount.ShouldBeGreaterThanOrEqualTo(quorum),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        // Quorum should complete faster than waiting for all
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HighLoad_WaitForFirst_ReturnsQuickly()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("FirstLoadTest")
            .ScatterTo("Fast", async (req, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(1));
            })
            .ScatterTo("Slow1", async (req, ct) =>
            {
                await Task.Delay(5000, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(2));
            })
            .ScatterTo("Slow2", async (req, ct) =>
            {
                await Task.Delay(5000, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(3));
            })
            .GatherFirst()
            .TakeFirst()
            .Build();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));
        stopwatch.Stop();

        // Assert
        result.IsRight.ShouldBeTrue();
        // Should complete much faster than 5 seconds
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HighLoad_SequentialExecution_MaintainsOrder()
    {
        // Arrange
        const int handlerCount = 20;
        var executionOrder = new List<int>();
        var lockObj = new object();
        var runner = CreateRunner();

        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("SequentialLoadTest");
        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", req =>
            {
                lock (lockObj)
                {
                    executionOrder.Add(index);
                }
                return Right<EncinaError, TestResponse>(new TestResponse(index));
            });
        }

        var definition = builder
            .ExecuteSequentially()
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.IsRight.ShouldBeTrue();
        executionOrder.ShouldBe(Enumerable.Range(0, handlerCount).ToList());
    }

    [Fact]
    public async Task HighLoad_MaxDegreeOfParallelism_RespectsLimit()
    {
        // Arrange
        const int maxParallelism = 3;
        const int handlerCount = 10;
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var options = new ScatterGatherOptions { MaxDegreeOfParallelism = maxParallelism };
        var runner = new ScatterGatherRunner(options, NullLogger<ScatterGatherRunner>.Instance);

        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("ParallelismTest");
        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", async (req, ct) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }

                await Task.Delay(50, ct);

                lock (lockObj)
                {
                    concurrentCount--;
                }

                return Right<EncinaError, TestResponse>(new TestResponse(index));
            });
        }

        var definition = builder
            .ExecuteInParallel(maxParallelism)
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.IsRight.ShouldBeTrue();
        maxConcurrent.ShouldBeLessThanOrEqualTo(maxParallelism);
    }

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);
}
