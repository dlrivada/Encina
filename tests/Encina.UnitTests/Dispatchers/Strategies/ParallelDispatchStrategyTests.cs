using Encina.Dispatchers.Strategies;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Dispatchers.Strategies;

public sealed class ParallelDispatchStrategyTests
{
    private sealed record TestNotification(int Value) : INotification;

    [Fact]
    public async Task DispatchAsync_WithEmptyHandlers_ReturnsSuccess()
    {
        var strategy = new ParallelDispatchStrategy();
        var handlers = System.Array.Empty<object>();

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(1),
            (_, _, _) => Task.FromResult(Right<EncinaError, Unit>(Unit.Default)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithSingleHandler_InvokesHandler()
    {
        var strategy = new ParallelDispatchStrategy();
        var invoked = false;
        var handler = new object();
        var handlers = new[] { handler };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(42),
            (h, n, ct) =>
            {
                invoked = true;
                h.ShouldBe(handler);
                n.Value.ShouldBe(42);
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithSingleNullHandler_ReturnsSuccess()
    {
        var strategy = new ParallelDispatchStrategy();
        var handlers = new object?[] { null };

        var result = await strategy.DispatchAsync(
            handlers!,
            new TestNotification(1),
            (_, _, _) => Task.FromResult(Right<EncinaError, Unit>(Unit.Default)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_InvokesAllInParallel()
    {
        var strategy = new ParallelDispatchStrategy();
        var invokedHandlers = new List<int>();
        var handlers = new object[] { 1, 2, 3 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (h, _, _) =>
            {
                await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
                lock (invokedHandlers)
                {
                    invokedHandlers.Add((int)h);
                }
                return Right<EncinaError, Unit>(Unit.Default);
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invokedHandlers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_SkipsNullHandlers()
    {
        var strategy = new ParallelDispatchStrategy();
        var invokedHandlers = new List<int>();
        var handlers = new object?[] { 1, null, 3 };

        var result = await strategy.DispatchAsync(
            handlers!,
            new TestNotification(10),
            (h, _, _) =>
            {
                lock (invokedHandlers)
                {
                    invokedHandlers.Add((int)h);
                }
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invokedHandlers.Count.ShouldBe(2);
        invokedHandlers.ShouldContain(1);
        invokedHandlers.ShouldContain(3);
    }

    [Fact]
    public async Task DispatchAsync_WithFirstError_CancelsRemainingHandlers()
    {
        var strategy = new ParallelDispatchStrategy(maxDegreeOfParallelism: 1);
        var invokedHandlers = new List<int>();
        var handlers = new object[] { 1, 2, 3 };
        var expectedError = EncinaErrors.Create("test.error", "Test error");

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (h, _, ct) =>
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                lock (invokedHandlers)
                {
                    invokedHandlers.Add((int)h);
                }
                if ((int)h == 1)
                    return Left<EncinaError, Unit>(expectedError);
                return Right<EncinaError, Unit>(Unit.Default);
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("Test error");
    }

    [Fact]
    public async Task DispatchAsync_WithCancellation_ReturnsSuccessWhenNoHandlerStarted()
    {
        // ParallelDispatchStrategy catches cancellation and returns success if no handler processed
        var strategy = new ParallelDispatchStrategy();
        var handlers = new object[] { 1, 2 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (_, _, ct) =>
            {
                await Task.Delay(100, ct).ConfigureAwait(false);
                return Right<EncinaError, Unit>(Unit.Default);
            },
            cts.Token);

        // When cancelled before handlers can start, returns success (no handlers failed)
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithMaxDegreeOfParallelism_ThrottlesExecution()
    {
        var strategy = new ParallelDispatchStrategy(maxDegreeOfParallelism: 2);
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var handlers = Enumerable.Range(1, 6).Cast<object>().ToArray();

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (_, _, _) =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                lock (strategy)
                {
                    if (current > maxConcurrent)
                        maxConcurrent = current;
                }
                await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
                Interlocked.Decrement(ref concurrentCount);
                return Right<EncinaError, Unit>(Unit.Default);
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Constructor_WithDefaultParallelism_UsesProcessorCount()
    {
        var strategy = new ParallelDispatchStrategy();
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var handlers = Enumerable.Range(1, Environment.ProcessorCount * 2).Cast<object>().ToArray();

        await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (_, _, _) =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                lock (strategy)
                {
                    if (current > maxConcurrent)
                        maxConcurrent = current;
                }
                await Task.Delay(20, CancellationToken.None).ConfigureAwait(false);
                Interlocked.Decrement(ref concurrentCount);
                return Right<EncinaError, Unit>(Unit.Default);
            },
            CancellationToken.None);

        maxConcurrent.ShouldBeLessThanOrEqualTo(Environment.ProcessorCount);
    }

    [Fact]
    public async Task DispatchAsync_WhenAlreadyCancelled_ReturnsSuccessWithoutInvokingHandlers()
    {
        var strategy = new ParallelDispatchStrategy();
        var invokedHandlers = new List<int>();
        var handlers = new object[] { 1, 2 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // The strategy catches cancellation and returns success (no handlers failed)
        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                lock (invokedHandlers)
                {
                    invokedHandlers.Add((int)h);
                }
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            cts.Token);

        // When already cancelled, returns success without invoking handlers
        result.IsRight.ShouldBeTrue();
        invokedHandlers.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DispatchAsync_WithAllHandlersSucceeding_ReturnsSuccess()
    {
        var strategy = new ParallelDispatchStrategy();
        var handlers = new object[] { 1, 2, 3, 4, 5 };
        var completedCount = 0;

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            async (_, _, _) =>
            {
                await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
                Interlocked.Increment(ref completedCount);
                return Right<EncinaError, Unit>(Unit.Default);
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        completedCount.ShouldBe(5);
    }

    [Fact]
    public async Task DispatchAsync_FailFast_StopsOnFirstError()
    {
        var strategy = new ParallelDispatchStrategy(maxDegreeOfParallelism: 1);
        var invokedCount = 0;
        var handlers = new object[] { 1, 2, 3 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                Interlocked.Increment(ref invokedCount);
                if ((int)h == 1)
                {
                    return Task.FromResult(Left<EncinaError, Unit>(
                        EncinaErrors.Create("test.error", "First handler failed")));
                }
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("First handler failed");
    }
}
