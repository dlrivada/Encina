using Encina.Messaging.ScatterGather;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.Messaging.Tests.ScatterGather;

/// <summary>
/// Unit tests for <see cref="ScatterGatherRunner"/>.
/// </summary>
public sealed class ScatterGatherRunnerTests
{
    private sealed record TestRequest(string Value);

    #region Constructor

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<ScatterGatherRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ScatterGatherRunner(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ScatterGatherRunner(options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var options = new ScatterGatherOptions();
        var logger = NullLogger<ScatterGatherRunner>.Instance;

        // Act
        var runner = new ScatterGatherRunner(options, logger);

        // Assert
        runner.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync - Basic Success

    [Fact]
    public async Task ExecuteAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();
        var request = new TestRequest("test");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => runner.ExecuteAsync<TestRequest, int>(null!, request).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(
            scatters: [("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(42)))]);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => runner.ExecuteAsync(definition, (TestRequest)null!).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_SingleHandler_Success_ReturnsResult()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(
            scatters: [("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(42)))]);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.Response.ShouldBe(42);
        sgResult.ScatterResults.Count.ShouldBe(1);
        sgResult.SuccessCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleHandlers_AllSuccess_ReturnsAggregatedResult()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Right<EncinaError, int>(20))),
                ("Handler3", _ => ValueTask.FromResult(Right<EncinaError, int>(30)))
            ]);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.ScatterResults.Count.ShouldBe(3);
        sgResult.SuccessCount.ShouldBe(3);
        sgResult.Response.ShouldBe(60);
    }

    #endregion

    #region ExecuteAsync - Handler Failures

    [Fact]
    public async Task ExecuteAsync_SingleHandler_ReturnsError_WithWaitForAll_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters: [("Handler1", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))],
            strategy: GatherStrategy.WaitForAll);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(ScatterGatherErrorCodes.ScatterFailed),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task ExecuteAsync_SingleHandler_ReturnsError_WithAllowPartial_AggregatesResult()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters: [("Handler1", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))],
            strategy: GatherStrategy.WaitForAllAllowPartial);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        // WaitForAllAllowPartial allows all to fail, gather will still run
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.SuccessCount.ShouldBe(0);
        sgResult.FailureCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_SingleHandler_ThrowsException_WithWaitForAll_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(
            scatters: [("Handler1", (Func<CancellationToken, ValueTask<Either<EncinaError, int>>>)(_ => throw new InvalidOperationException("Handler exception")))],
            strategy: GatherStrategy.WaitForAll);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_MultipleHandlers_PartialSuccess_ReturnsResult()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Left<EncinaError, int>(error))),
                ("Handler3", _ => ValueTask.FromResult(Right<EncinaError, int>(30)))
            ]);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.SuccessCount.ShouldBe(2);
        sgResult.Response.ShouldBe(40);
    }

    [Fact]
    public async Task ExecuteAsync_AllHandlersFail_WithWaitForFirst_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Left<EncinaError, int>(error))),
                ("Handler2", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))
            ],
            strategy: GatherStrategy.WaitForFirst);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(ScatterGatherErrorCodes.AllScattersFailed),
            () => throw new InvalidOperationException("Expected error code"));
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var definition = CreateDefinition(
            scatters: [("Handler1", async ct =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, int>(42);
            })]);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(ScatterGatherErrorCodes.Cancelled),
            () => throw new InvalidOperationException("Expected error code"));
    }

    #endregion

    #region ExecuteAsync - Timeout

    [Fact]
    public async Task ExecuteAsync_WhenTimedOut_WithWaitForAll_ReturnsError()
    {
        // Arrange
        var options = new ScatterGatherOptions { DefaultTimeout = TimeSpan.FromMilliseconds(50) };
        var runner = CreateRunner(options);

        var definition = CreateDefinition(
            scatters: [("Handler1", async ct =>
            {
                await Task.Delay(5000, ct);
                return Right<EncinaError, int>(42);
            })],
            strategy: GatherStrategy.WaitForAll,
            timeout: TimeSpan.FromMilliseconds(50));

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        // Either timeout error or scatter failed error (handler was cancelled)
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimedOut_WithAllowPartial_ReturnsResult()
    {
        // Arrange
        var options = new ScatterGatherOptions { DefaultTimeout = TimeSpan.FromMilliseconds(50) };
        var runner = CreateRunner(options);

        var definition = CreateDefinition(
            scatters: [("Handler1", async ct =>
            {
                await Task.Delay(5000, ct);
                return Right<EncinaError, int>(42);
            })],
            strategy: GatherStrategy.WaitForAllAllowPartial,
            timeout: TimeSpan.FromMilliseconds(50));

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        // WaitForAllAllowPartial allows failed handlers, so gather still runs
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.SuccessCount.ShouldBe(0);
    }

    #endregion

    #region ExecuteAsync - WaitForAll Strategy

    [Fact]
    public async Task ExecuteAsync_WaitForAll_AllSucceed_ReturnsResult()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Right<EncinaError, int>(20)))
            ],
            strategy: GatherStrategy.WaitForAll);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.Strategy.ShouldBe(GatherStrategy.WaitForAll);
        sgResult.SuccessCount.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WaitForAll_OneFails_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))
            ],
            strategy: GatherStrategy.WaitForAll);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(ScatterGatherErrorCodes.ScatterFailed),
            () => throw new InvalidOperationException("Expected error code"));
    }

    #endregion

    #region ExecuteAsync - WaitForFirst Strategy

    [Fact]
    public async Task ExecuteAsync_WaitForFirst_ReturnsFirstSuccess()
    {
        // Arrange
        var options = new ScatterGatherOptions { CancelRemainingOnStrategyComplete = true };
        var runner = CreateRunner(options);
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", async ct =>
                {
                    await Task.Delay(500, ct);
                    return Right<EncinaError, int>(20);
                })
            ],
            strategy: GatherStrategy.WaitForFirst);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.Strategy.ShouldBe(GatherStrategy.WaitForFirst);
        // First result should be 10 (from Handler1)
        sgResult.Response.ShouldBe(10);
    }

    [Fact]
    public async Task ExecuteAsync_WaitForFirst_AllFail_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Left<EncinaError, int>(error))),
                ("Handler2", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))
            ],
            strategy: GatherStrategy.WaitForFirst);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ExecuteAsync - WaitForQuorum Strategy

    [Fact]
    public async Task ExecuteAsync_WaitForQuorum_QuorumReached_ReturnsResult()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Right<EncinaError, int>(20))),
                ("Handler3", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))
            ],
            strategy: GatherStrategy.WaitForQuorum,
            quorum: 2);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sgResult = result.RightAsEnumerable().First();
        sgResult.Strategy.ShouldBe(GatherStrategy.WaitForQuorum);
        sgResult.SuccessCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_WaitForQuorum_QuorumNotReached_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ => ValueTask.FromResult(Right<EncinaError, int>(10))),
                ("Handler2", _ => ValueTask.FromResult(Left<EncinaError, int>(error))),
                ("Handler3", _ => ValueTask.FromResult(Left<EncinaError, int>(error)))
            ],
            strategy: GatherStrategy.WaitForQuorum,
            quorum: 2);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(ScatterGatherErrorCodes.QuorumNotReached),
            () => throw new InvalidOperationException("Expected error code"));
    }

    #endregion

    #region ExecuteAsync - Sequential Execution

    [Fact]
    public async Task ExecuteAsync_Sequential_ExecutesInOrder()
    {
        // Arrange
        var runner = CreateRunner();
        var executionOrder = new List<string>();
        var definition = CreateDefinition(
            scatters:
            [
                ("Handler1", _ =>
                {
                    executionOrder.Add("Handler1");
                    return ValueTask.FromResult(Right<EncinaError, int>(10));
                }),
                ("Handler2", _ =>
                {
                    executionOrder.Add("Handler2");
                    return ValueTask.FromResult(Right<EncinaError, int>(20));
                }),
                ("Handler3", _ =>
                {
                    executionOrder.Add("Handler3");
                    return ValueTask.FromResult(Right<EncinaError, int>(30));
                })
            ],
            parallel: false);

        var request = new TestRequest("test");

        // Act
        var result = await runner.ExecuteAsync(definition, request);

        // Assert
        result.IsRight.ShouldBeTrue();
        executionOrder.Count.ShouldBe(3);
        executionOrder[0].ShouldBe("Handler1");
        executionOrder[1].ShouldBe("Handler2");
        executionOrder[2].ShouldBe("Handler3");
    }

    #endregion

    #region Helper Methods

    private static ScatterGatherRunner CreateRunner(ScatterGatherOptions? options = null)
    {
        return new ScatterGatherRunner(
            options ?? new ScatterGatherOptions(),
            NullLogger<ScatterGatherRunner>.Instance);
    }

    private static BuiltScatterGatherDefinition<TestRequest, int> CreateDefinition(
        IReadOnlyList<(string Name, Func<CancellationToken, ValueTask<Either<EncinaError, int>>> Handler)> scatters,
        GatherStrategy strategy = GatherStrategy.WaitForAllAllowPartial,
        int? quorum = null,
        bool parallel = true,
        TimeSpan? timeout = null)
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("TestScatterGather");

        if (timeout.HasValue)
        {
            builder.WithTimeout(timeout.Value);
        }

        if (!parallel)
        {
            builder.ExecuteSequentially();
        }

        // Add scatter handlers
        foreach (var (name, handler) in scatters)
        {
            builder.ScatterTo(name, (_, ct) => handler(ct));
        }

        // Configure gather based on strategy
        GatherBuilder<TestRequest, int> gatherBuilder;
        if (quorum.HasValue)
        {
            gatherBuilder = builder.GatherQuorum(quorum.Value);
        }
        else
        {
            gatherBuilder = strategy switch
            {
                GatherStrategy.WaitForAll => builder.GatherAll(),
                GatherStrategy.WaitForFirst => builder.GatherFirst(),
                GatherStrategy.WaitForQuorum => builder.GatherQuorum(quorum ?? 1),
                GatherStrategy.WaitForAllAllowPartial => builder.GatherAllAllowingPartialFailures(),
                _ => builder.GatherAllAllowingPartialFailures()
            };
        }

        // Default gather: sum all successful results
        gatherBuilder.Aggregate((results, _) =>
        {
            var sum = results
                .Where(r => r.IsSuccess)
                .Sum(r => r.Result.Match(Right: v => v, Left: _ => 0));
            return ValueTask.FromResult(Right<EncinaError, int>(sum));
        });

        return builder.Build();
    }

    #endregion
}
