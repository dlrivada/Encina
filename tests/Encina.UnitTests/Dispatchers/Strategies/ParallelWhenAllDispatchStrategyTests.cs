using Encina.Dispatchers.Strategies;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Dispatchers.Strategies;

public sealed class ParallelWhenAllDispatchStrategyTests
{
    private sealed record TestNotification(int Value) : INotification;

    [Fact]
    public async Task DispatchAsync_WithEmptyHandlers_ReturnsSuccess()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
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
        var strategy = new ParallelWhenAllDispatchStrategy();
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
        var strategy = new ParallelWhenAllDispatchStrategy();
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
        var strategy = new ParallelWhenAllDispatchStrategy();
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
        invokedHandlers.ShouldContain(1);
        invokedHandlers.ShouldContain(2);
        invokedHandlers.ShouldContain(3);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_SkipsNullHandlers()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
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
    public async Task DispatchAsync_WithSingleError_ReturnsError()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
        var handlers = new object[] { 1, 2 };
        var expectedError = EncinaErrors.Create("test.error", "Test error");

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                if ((int)h == 1)
                    return Task.FromResult(Left<EncinaError, Unit>(expectedError));
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("Test error");
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleErrors_ReturnsAggregateError()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
        var handlers = new object[] { 1, 2, 3 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                var handlerId = (int)h;
                if (handlerId != 2)
                {
                    return Task.FromResult(Left<EncinaError, Unit>(
                        EncinaErrors.Create($"test.error.{handlerId}", $"Error {handlerId}")));
                }
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationMultipleFailures);
        error.Message.ShouldContain("Multiple notification handlers failed");
        error.Message.ShouldContain("2 errors");
    }

    [Fact]
    public async Task DispatchAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // When token is already cancelled before semaphore.WaitAsync, it throws TaskCanceledException
        var strategy = new ParallelWhenAllDispatchStrategy();
        var handlers = new object[] { 1, 2 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<TaskCanceledException>(async () =>
        {
            await strategy.DispatchAsync(
                handlers,
                new TestNotification(10),
                (_, _, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
                },
                cts.Token);
        });
    }

    [Fact]
    public async Task DispatchAsync_WithException_ReturnsExceptionError()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
        var handlers = new object[] { 1, 2 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                if ((int)h == 1)
                    throw new InvalidOperationException("Test exception");
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationException);
        error.Message.ShouldContain("threw an unexpected exception");
    }

    [Fact]
    public async Task DispatchAsync_WithMaxDegreeOfParallelism_ThrottlesExecution()
    {
        var strategy = new ParallelWhenAllDispatchStrategy(maxDegreeOfParallelism: 2);
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
        var strategy = new ParallelWhenAllDispatchStrategy();
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
    public async Task DispatchAsync_AggregateError_ContainsHandlerDetails()
    {
        var strategy = new ParallelWhenAllDispatchStrategy();
        var handler1 = new TestHandler("Handler1");
        var handler2 = new TestHandler("Handler2");
        var handlers = new object[] { handler1, handler2 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) => Task.FromResult(Left<EncinaError, Unit>(
                EncinaErrors.Create("test.error", $"Error from {((TestHandler)h).Name}"))),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        var details = error.GetEncinaDetails();
        details.ShouldContainKey("error_count");
        details["error_count"].ShouldBe(2);
        details.ShouldContainKey("notification");
        details["notification"].ShouldBe(nameof(TestNotification));
    }

    private sealed record TestHandler(string Name);
}
