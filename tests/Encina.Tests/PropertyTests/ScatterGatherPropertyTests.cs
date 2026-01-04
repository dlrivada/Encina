using Encina.Messaging.ScatterGather;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.PropertyTests;

/// <summary>
/// Property-based tests for Scatter-Gather pattern.
/// Verifies invariants and properties that should hold across various inputs.
/// </summary>
public sealed class ScatterGatherPropertyTests
{
    private readonly ScatterGatherOptions _options = new();

    private ScatterGatherRunner CreateRunner() =>
        new(_options, NullLogger<ScatterGatherRunner>.Instance);

    #region Scatter Count Invariants

    /// <summary>
    /// Property: ScatterCount equals the number of added handlers.
    /// Invariant: The definition accurately reflects all registered handlers.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void ScatterCount_EqualsNumberOfAddedHandlers(int handlerCount)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < handlerCount; i++)
        {
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(i)));
        }

        // Act
        var definition = builder.GatherAll().TakeFirst().Build();

        // Assert
        definition.ScatterCount.ShouldBe(handlerCount);
    }

    #endregion

    #region Priority Ordering Invariants

    /// <summary>
    /// Property: Handlers are sorted by priority.
    /// Invariant: Lower priority values come before higher priority values.
    /// </summary>
    [Theory]
    [InlineData(new[] { 5, 3, 1, 4, 2 })]
    [InlineData(new[] { 100, 1, 50 })]
    [InlineData(new[] { 10, 20, 30 })]
    [InlineData(new[] { 1, 1, 1 })]
    public void HandlersAreSortedByPriority(int[] priorities)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < priorities.Length; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}")
                .WithPriority(priorities[i])
                .Execute(req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        // Act
        var definition = builder.GatherAll().TakeFirst().Build();

        // Assert
        for (int i = 1; i < definition.ScatterHandlers.Count; i++)
        {
            definition.ScatterHandlers[i].Priority
                .ShouldBeGreaterThanOrEqualTo(definition.ScatterHandlers[i - 1].Priority);
        }
    }

    #endregion

    #region Success/Failure Count Invariants

    /// <summary>
    /// Property: SuccessCount + FailureCount equals total scatter count.
    /// Invariant: All handlers are accounted for in the result.
    /// </summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 3)]
    [InlineData(3, 5)]
    public async Task SuccessCountPlusFailureCountEqualsScatterCount(int successCount, int failureCount)
    {
        // Arrange
        var totalCount = successCount + failureCount;
        if (totalCount == 0) return;

        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < successCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Success{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        for (int i = 0; i < failureCount; i++)
        {
            builder.ScatterTo($"Failure{i}", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")));
        }

        var definition = builder.GatherAllAllowingPartialFailures()
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(0)))
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        if (successCount > 0)
        {
            var sgResult = result.ShouldBeSuccess();
            (sgResult.SuccessCount + sgResult.FailureCount).ShouldBe(totalCount);
        }
        else
        {
            // All handlers failed
            result.ShouldBeError();
        }
    }

    #endregion

    #region Quorum Strategy Invariants

    /// <summary>
    /// Property: WaitForQuorum returns success when quorum is reached.
    /// Invariant: If enough handlers succeed, the operation succeeds.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    [InlineData(10, 5)]
    public async Task WaitForQuorum_ReturnsSuccessWhenQuorumReached(int successCount, int quorum)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < successCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        var definition = builder.GatherQuorum(quorum)
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(0)))
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        var sgResult = result.ShouldBeSuccess();
        sgResult.SuccessCount.ShouldBeGreaterThanOrEqualTo(quorum);
    }

    /// <summary>
    /// Property: WaitForQuorum fails when quorum cannot be reached.
    /// Invariant: If not enough handlers can succeed, the operation fails.
    /// </summary>
    [Theory]
    [InlineData(2, 3, 4)]  // 2 successes, 3 failures, need 4 -> fail
    [InlineData(3, 5, 5)]  // 3 successes, 5 failures, need 5 -> fail
    public async Task WaitForQuorum_FailsWhenQuorumNotReached(int successCount, int failureCount, int quorum)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < successCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Success{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        for (int i = 0; i < failureCount; i++)
        {
            builder.ScatterTo($"Failure{i}", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")));
        }

        var definition = builder.GatherQuorum(quorum)
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(0)))
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeError();
    }

    #endregion

    #region WaitForAll Invariants

    /// <summary>
    /// Property: WaitForAll fails if any handler fails.
    /// Invariant: Even one failure causes the entire operation to fail.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(5, 2)]
    public async Task WaitForAll_FailsIfAnyHandlerFails(int successCount, int failureCount)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < successCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Success{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        for (int i = 0; i < failureCount; i++)
        {
            builder.ScatterTo($"Failure{i}", req => Left<EncinaError, TestResponse>(EncinaErrors.Create("test.error", "Failed")));
        }

        var definition = builder.GatherAll()
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(0)))
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        result.ShouldBeError();
    }

    /// <summary>
    /// Property: WaitForAll succeeds if all handlers succeed.
    /// Invariant: All successes means the operation succeeds.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task WaitForAll_SucceedsIfAllHandlersSucceed(int handlerCount)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        var definition = builder.GatherAll()
            .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(results.Count())))
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        var sgResult = result.ShouldBeSuccess();
        sgResult.SuccessCount.ShouldBe(handlerCount);
    }

    #endregion

    #region Effective Quorum Invariants

    /// <summary>
    /// Property: EffectiveQuorumCount defaults to majority (half + 1).
    /// Invariant: Default quorum calculation is consistent.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]   // (1/2) + 1 = 1
    [InlineData(2, 2)]   // (2/2) + 1 = 2
    [InlineData(3, 2)]   // (3/2) + 1 = 2
    [InlineData(4, 3)]   // (4/2) + 1 = 3
    [InlineData(5, 3)]   // (5/2) + 1 = 3
    [InlineData(10, 6)]  // (10/2) + 1 = 6
    [InlineData(20, 11)] // (20/2) + 1 = 11
    public void EffectiveQuorumCount_DefaultsToMajority(int scatterCount, int expectedQuorum)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < scatterCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        // Act
        var definition = builder.GatherWith(GatherStrategy.WaitForQuorum)
            .TakeFirst()
            .Build();

        // Assert
        definition.GetEffectiveQuorumCount().ShouldBe(expectedQuorum);
    }

    #endregion

    #region WaitForFirst Invariants

    /// <summary>
    /// Property: WaitForFirst returns at least one successful result.
    /// Invariant: The operation succeeds if at least one handler succeeds.
    /// Note: For synchronous handlers, all may complete before cancellation takes effect.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task WaitForFirst_ReturnsAtLeastOneSuccessfulResult(int handlerCount)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
        }

        var definition = builder.GatherFirst()
            .TakeFirst()
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        var sgResult = result.ShouldBeSuccess();
        sgResult.SuccessCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Property: WaitForFirst with async handlers can cancel remaining handlers.
    /// Invariant: When handlers are slow, only the first to complete is needed.
    /// </summary>
    [Fact]
    public async Task WaitForFirst_WithSlowHandlers_CancelsRemaining()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");
        var executedCount = 0;
        var lockObj = new object();

        // Fast handler
        builder.ScatterTo("Fast", async (req, ct) =>
        {
            await Task.Delay(10, ct);
            lock (lockObj) executedCount++;
            return Right<EncinaError, TestResponse>(new TestResponse(1));
        });

        // Slow handlers
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            builder.ScatterTo($"Slow{i}", async (req, ct) =>
            {
                await Task.Delay(5000, ct); // Very slow
                lock (lockObj) executedCount++;
                return Right<EncinaError, TestResponse>(new TestResponse(index + 10));
            });
        }

        var definition = builder.GatherFirst()
            .TakeFirst()
            .Build();

        var runner = CreateRunner();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));
        stopwatch.Stop();

        // Assert
        result.ShouldBeSuccess();
        // Should complete much faster than 5 seconds
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        // Only the fast handler should have completed
        executedCount.ShouldBe(1);
    }

    #endregion

    #region Aggregation Invariants

    /// <summary>
    /// Property: TakeMin returns the minimum value.
    /// Invariant: The aggregation correctly identifies the minimum.
    /// </summary>
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, 1)]
    [InlineData(new[] { 100, 50, 75 }, 50)]
    [InlineData(new[] { -5, 0, 5 }, -5)]
    [InlineData(new[] { 42 }, 42)]
    public async Task TakeMin_ReturnsMinimumValue(int[] values, int expectedMin)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(value)));
        }

        var definition = builder.GatherAll()
            .TakeMin(r => r.Value)
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        var sgResult = result.ShouldBeSuccess();
        sgResult.Response.Value.ShouldBe(expectedMin);
    }

    /// <summary>
    /// Property: TakeMax returns the maximum value.
    /// Invariant: The aggregation correctly identifies the maximum.
    /// </summary>
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, 3)]
    [InlineData(new[] { 100, 50, 75 }, 100)]
    [InlineData(new[] { -5, 0, 5 }, 5)]
    [InlineData(new[] { 42 }, 42)]
    public async Task TakeMax_ReturnsMaximumValue(int[] values, int expectedMax)
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

        for (int i = 0; i < values.Length; i++)
        {
            var value = values[i];
            builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(value)));
        }

        var definition = builder.GatherAll()
            .TakeMax(r => r.Value)
            .Build();

        var runner = CreateRunner();

        // Act
        var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

        // Assert
        var sgResult = result.ShouldBeSuccess();
        sgResult.Response.Value.ShouldBe(expectedMax);
    }

    #endregion

    public sealed record TestRequest(string Query);
    public sealed record TestResponse(decimal Value);

    #region FsCheck Property Tests

    /// <summary>
    /// Property: ScatterCount always equals handler count.
    /// Uses FsCheck to verify across random handler counts.
    /// </summary>
    [EncinaProperty]
    public Property ScatterCount_EqualsHandlerCount()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 20)),
            handlerCount =>
            {
                var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

                for (var i = 0; i < handlerCount; i++)
                {
                    var index = i;
                    builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
                }

                var definition = builder.GatherAll().TakeFirst().Build();
                return definition.ScatterCount == handlerCount;
            });
    }

    /// <summary>
    /// Property: All handlers succeed ? SuccessCount equals ScatterCount.
    /// Verified with random handler counts.
    /// </summary>
    [EncinaProperty]
    public Property AllSuccess_SuccessCountEqualsScatterCount()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10)),
            async handlerCount =>
            {
                var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

                for (var i = 0; i < handlerCount; i++)
                {
                    var index = i;
                    builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
                }

                var definition = builder.GatherAll()
                    .AggregateSuccessful(results => Right<EncinaError, TestResponse>(new TestResponse(results.Count())))
                    .Build();

                var runner = CreateRunner();
                var result = await runner.ExecuteAsync(definition, new TestRequest("test"));

                return result.Match(
                    Left: _ => false,
                    Right: r => r.SuccessCount == handlerCount);
            });
    }

    /// <summary>
    /// Property: Default quorum is always (N/2) + 1.
    /// Verified across random scatter counts.
    /// </summary>
    [EncinaProperty]
    public Property DefaultQuorum_IsMajority()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)),
            scatterCount =>
            {
                var builder = ScatterGatherBuilder.Create<TestRequest, TestResponse>("Test");

                for (var i = 0; i < scatterCount; i++)
                {
                    var index = i;
                    builder.ScatterTo($"Handler{i}", req => Right<EncinaError, TestResponse>(new TestResponse(index)));
                }

                var definition = builder.GatherWith(GatherStrategy.WaitForQuorum)
                    .TakeFirst()
                    .Build();

                var expectedQuorum = (scatterCount / 2) + 1;
                return definition.GetEffectiveQuorumCount() == expectedQuorum;
            });
    }

    #endregion
}
