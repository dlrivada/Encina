using Encina.Messaging.ScatterGather;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.ScatterGather;

public sealed class ScatterGatherRunnerTests
{
    private readonly ScatterGatherOptions _options = new();

    private ScatterGatherRunner CreateRunner() =>
        new(_options, NullLogger<ScatterGatherRunner>.Instance);

    [Fact]
    public async Task ExecuteAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            runner.ExecuteAsync<TestRequest, TestResponse>(null!, new TestRequest("test")).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            runner.ExecuteAsync(definition, null!).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_WaitForAll_AllSucceed_ReturnsSuccess()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
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
        result.ShouldBeSuccess(r =>
        {
            r.Response.Value.ShouldBe(300);
            r.SuccessCount.ShouldBe(2);
            r.FailureCount.ShouldBe(0);
            r.AllSucceeded.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_WaitForAll_OneFails_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeError(error => error.Message.ShouldContain("succeeded"));
    }

    [Fact]
    public async Task ExecuteAsync_WaitForFirst_FirstSucceeds_ReturnsImmediately()
    {
        // Arrange
        var runner = CreateRunner();
        var tcs1 = new TaskCompletionSource<Either<EncinaError, TestResponse>>();
        var tcs2 = new TaskCompletionSource<Either<EncinaError, TestResponse>>();

        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("FastHandler", async (req, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(100));
            })
            .ScatterTo("SlowHandler", async (req, ct) =>
            {
                await Task.Delay(5000, ct);
                return Right<EncinaError, TestResponse>(new TestResponse(200));
            })
            .GatherFirst()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r =>
        {
            r.Response.Value.ShouldBe(100);
            r.Strategy.ShouldBe(GatherStrategy.WaitForFirst);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WaitForQuorum_QuorumReached_ReturnsSuccess()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
            .ScatterTo("Handler3", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")))
            .GatherQuorum(2)
            .AggregateSuccessful(results =>
            {
                var sum = results.Sum(r => r.Value);
                return Right<EncinaError, TestResponse>(new TestResponse(sum));
            })
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r =>
        {
            r.Response.Value.ShouldBe(300);
            r.SuccessCount.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WaitForQuorum_QuorumNotReached_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")))
            .ScatterTo("Handler3", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")))
            .GatherQuorum(2)
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeError(error => error.Message.ShouldContain("Quorum not reached"));
    }

    [Fact]
    public async Task ExecuteAsync_WaitForAllAllowPartial_PartialFailures_ReturnsSuccess()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")))
            .GatherAllAllowingPartialFailures()
            .AggregateSuccessful(results =>
            {
                var sum = results.Sum(r => r.Value);
                return Right<EncinaError, TestResponse>(new TestResponse(sum));
            })
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r =>
        {
            r.Response.Value.ShouldBe(100);
            r.HasPartialFailures.ShouldBeTrue();
            r.FailureCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_TimesOut_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("SlowHandler", async (req, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return Right<EncinaError, TestResponse>(new TestResponse(100));
            })
            .WithTimeout(TimeSpan.FromMilliseconds(50))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert - should fail due to timeout (either "timed out" or scatter failure due to cancellation)
        result.ShouldBeError();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ReturnsCancelledError()
    {
        // Arrange
        var runner = CreateRunner();
        using var cts = new CancellationTokenSource();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler", async (req, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return Right<EncinaError, TestResponse>(new TestResponse(100));
            })
            .GatherAll()
            .TakeFirst()
            .Build();

        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await cts.CancelAsync();
        });

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"), cts.Token);

        // Assert - should fail due to cancellation (either "cancelled" or scatter failure)
        result.ShouldBeError();
    }

    [Fact]
    public async Task ExecuteAsync_SequentialExecution_ExecutesInOrder()
    {
        // Arrange
        var runner = CreateRunner();
        var executionOrder = new List<int>();

        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req =>
            {
                executionOrder.Add(1);
                return Right<EncinaError, TestResponse>(new TestResponse(100));
            })
            .ScatterTo("Handler2", req =>
            {
                executionOrder.Add(2);
                return Right<EncinaError, TestResponse>(new TestResponse(200));
            })
            .ScatterTo("Handler3", req =>
            {
                executionOrder.Add(3);
                return Right<EncinaError, TestResponse>(new TestResponse(300));
            })
            .ExecuteSequentially()
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess();
        executionOrder.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteAsync_ParallelExecution_ExecutesConcurrently()
    {
        // Arrange
        var runner = CreateRunner();
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", async (req, ct) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }
                await Task.Delay(50, ct);
                lock (lockObj) { concurrentCount--; }
                return Right<EncinaError, TestResponse>(new TestResponse(100));
            })
            .ScatterTo("Handler2", async (req, ct) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }
                await Task.Delay(50, ct);
                lock (lockObj) { concurrentCount--; }
                return Right<EncinaError, TestResponse>(new TestResponse(200));
            })
            .ScatterTo("Handler3", async (req, ct) =>
            {
                lock (lockObj)
                {
                    concurrentCount++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrentCount);
                }
                await Task.Delay(50, ct);
                lock (lockObj) { concurrentCount--; }
                return Right<EncinaError, TestResponse>(new TestResponse(300));
            })
            .ExecuteInParallel()
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess();
        maxConcurrent.ShouldBeGreaterThan(1); // Should have run concurrently
    }

    [Fact]
    public async Task ExecuteAsync_GatherTakeFirst_ReturnsFirstSuccessfulResult()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r => r.Response.Value.ShouldBe(100));
    }

    [Fact]
    public async Task ExecuteAsync_GatherTakeMin_ReturnsMinimumValue()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(300)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler3", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
            .GatherAll()
            .TakeMin(r => r.Value)
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r => r.Response.Value.ShouldBe(100));
    }

    [Fact]
    public async Task ExecuteAsync_GatherTakeMax_ReturnsMaximumValue()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(300)))
            .ScatterTo("Handler3", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
            .GatherAll()
            .TakeMax(r => r.Value)
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r => r.Response.Value.ShouldBe(300));
    }

    [Fact]
    public async Task ExecuteAsync_HandlerThrowsException_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("ThrowingHandler", (req, ct) =>
                throw new InvalidOperationException("Handler failed"))
            .GatherAllAllowingPartialFailures()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert - Strategy allows partial failures, but all failed so returns error
        result.ShouldBeError();
    }

    [Fact]
    public async Task ExecuteAsync_ResultContainsScatterResults()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .ScatterTo("Handler2", req => Right<EncinaError, TestResponse>(new TestResponse(200)))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r =>
        {
            r.ScatterResults.Count.ShouldBe(2);
            r.ScatterResults.All(sr => sr.IsSuccess).ShouldBeTrue();
            r.ScatterResults.All(sr => sr.Duration > TimeSpan.Zero).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ExecuteAsync_ResultContainsOperationId()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test")
            .ScatterTo(req => Right<EncinaError, TestResponse>(new TestResponse(100)))
            .GatherAll()
            .TakeFirst()
            .Build();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeSuccess(r => r.OperationId.ShouldNotBe(Guid.Empty));
    }

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);
}
