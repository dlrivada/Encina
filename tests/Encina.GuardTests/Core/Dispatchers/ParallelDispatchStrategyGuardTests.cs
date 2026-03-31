using Encina.Dispatchers.Strategies;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Dispatchers;

/// <summary>
/// Guard tests for <see cref="ParallelDispatchStrategy"/>
/// to verify constructor validation and DispatchAsync error paths.
/// </summary>
public class ParallelDispatchStrategyGuardTests
{
    /// <summary>
    /// Verifies that the default constructor (unlimited parallelism) does not throw.
    /// </summary>
    [Fact]
    public void Constructor_DefaultParallelism_DoesNotThrow()
    {
        var act = () => new ParallelDispatchStrategy();
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that the constructor with -1 (unlimited) does not throw.
    /// </summary>
    [Fact]
    public void Constructor_UnlimitedParallelism_DoesNotThrow()
    {
        var act = () => new ParallelDispatchStrategy(-1);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that the constructor with a specific positive value does not throw.
    /// </summary>
    [Fact]
    public void Constructor_SpecificParallelism_DoesNotThrow()
    {
        var act = () => new ParallelDispatchStrategy(4);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that DispatchAsync returns Right(Unit) when handlers list is empty.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_EmptyHandlers_ReturnsRight()
    {
        var strategy = new ParallelDispatchStrategy();
        var handlers = System.Array.Empty<object>().ToList().AsReadOnly();

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(),
            (_, _, _) => Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DispatchAsync with a single handler delegates directly.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_SingleHandler_InvokesThatHandler()
    {
        var strategy = new ParallelDispatchStrategy();
        var handler = new object();
        var invoked = false;

        var result = await strategy.DispatchAsync<TestNotification>(
            new List<object> { handler }.AsReadOnly(),
            new TestNotification(),
            (h, _, _) =>
            {
                invoked = true;
                return Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invoked.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DispatchAsync with a single null handler returns Right (skips null).
    /// </summary>
    [Fact]
    public async Task DispatchAsync_SingleNullHandler_ReturnsRight()
    {
        var strategy = new ParallelDispatchStrategy();

        var result = await strategy.DispatchAsync<TestNotification>(
            new List<object> { null! }.AsReadOnly(),
            new TestNotification(),
            (_, _, _) => Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DispatchAsync with multiple handlers invokes all of them.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_MultipleHandlers_InvokesAll()
    {
        var strategy = new ParallelDispatchStrategy(2);
        var count = 0;
        var handlers = new List<object> { new(), new(), new() }.AsReadOnly();

        var result = await strategy.DispatchAsync<TestNotification>(
            handlers,
            new TestNotification(),
            (_, _, _) =>
            {
                Interlocked.Increment(ref count);
                return Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        count.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that DispatchAsync returns Left when one handler fails, demonstrating fail-fast behavior.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_HandlerReturnsLeft_ReturnsFirstError()
    {
        var strategy = new ParallelDispatchStrategy();
        var errorMessage = "handler failed";
        var handlers = new List<object> { new(), new() }.AsReadOnly();

        var result = await strategy.DispatchAsync<TestNotification>(
            handlers,
            new TestNotification(),
            (_, _, _) => Task.FromResult(Left<EncinaError, LanguageExt.Unit>(EncinaError.New(errorMessage))),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that DispatchAsync skips null handlers in a multi-handler list.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_MultipleHandlersWithNulls_SkipsNullsAndInvokesRest()
    {
        var strategy = new ParallelDispatchStrategy();
        var count = 0;
        var handlers = new List<object> { new(), null!, new() }.AsReadOnly();

        var result = await strategy.DispatchAsync<TestNotification>(
            handlers,
            new TestNotification(),
            (_, _, _) =>
            {
                Interlocked.Increment(ref count);
                return Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        count.ShouldBe(2);
    }

    /// <summary>
    /// Verifies that DispatchAsync respects cancellation and does not invoke handlers.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_CancelledToken_HandlersMayNotComplete()
    {
        var strategy = new ParallelDispatchStrategy();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handlers = new List<object> { new(), new() }.AsReadOnly();

        // With already-cancelled token, the semaphore.WaitAsync should throw OperationCanceledException
        // which is caught and returns Right (no error holder set)
        var result = await strategy.DispatchAsync<TestNotification>(
            handlers,
            new TestNotification(),
            (_, _, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));
            },
            cts.Token);

        // The strategy catches OperationCanceledException internally; it returns Right if no error holder is set
        result.IsRight.ShouldBeTrue();
    }

    private sealed record TestNotification : INotification;
}
