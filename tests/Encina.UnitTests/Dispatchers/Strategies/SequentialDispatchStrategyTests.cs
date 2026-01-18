using Encina.Dispatchers.Strategies;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Dispatchers.Strategies;

public sealed class SequentialDispatchStrategyTests
{
    private sealed record TestNotification(int Value) : INotification;

    [Fact]
    public void Instance_ReturnsSingletonInstance()
    {
        var instance1 = SequentialDispatchStrategy.Instance;
        var instance2 = SequentialDispatchStrategy.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public async Task DispatchAsync_WithEmptyHandlers_ReturnsSuccess()
    {
        var strategy = SequentialDispatchStrategy.Instance;
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
        var strategy = SequentialDispatchStrategy.Instance;
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
    public async Task DispatchAsync_WithMultipleHandlers_InvokesInOrder()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var invokedOrder = new List<int>();
        var handlers = new object[] { 1, 2, 3 };

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                invokedOrder.Add((int)h);
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invokedOrder.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task DispatchAsync_WithNullHandler_SkipsNull()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var invokedOrder = new List<int>();
        var handlers = new object?[] { 1, null, 3 };

        var result = await strategy.DispatchAsync(
            handlers!,
            new TestNotification(10),
            (h, _, _) =>
            {
                invokedOrder.Add((int)h);
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        invokedOrder.ShouldBe([1, 3]);
    }

    [Fact]
    public async Task DispatchAsync_FailFast_StopsOnFirstError()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var invokedOrder = new List<int>();
        var handlers = new object[] { 1, 2, 3 };
        var expectedError = EncinaErrors.Create("test.error", "Second handler failed");

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                invokedOrder.Add((int)h);
                if ((int)h == 2)
                    return Task.FromResult(Left<EncinaError, Unit>(expectedError));
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("Second handler failed");
        invokedOrder.ShouldBe([1, 2]); // Third handler should not be invoked
    }

    [Fact]
    public async Task DispatchAsync_WithFirstHandlerError_StopsImmediately()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var invokedOrder = new List<int>();
        var handlers = new object[] { 1, 2, 3 };
        var expectedError = EncinaErrors.Create("test.error", "First handler failed");

        var result = await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (h, _, _) =>
            {
                invokedOrder.Add((int)h);
                if ((int)h == 1)
                    return Task.FromResult(Left<EncinaError, Unit>(expectedError));
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("First handler failed");
        invokedOrder.ShouldBe([1]); // Only first handler should be invoked
    }

    [Fact]
    public async Task DispatchAsync_WithAllNullHandlers_ReturnsSuccess()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var handlers = new object?[] { null, null, null };

        var result = await strategy.DispatchAsync(
            handlers!,
            new TestNotification(10),
            (_, _, _) =>
            {
                // Should never be called
                throw new InvalidOperationException("Should not be called");
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_PassesCancellationToken()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var handlers = new object[] { 1 };
        using var cts = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;

        await strategy.DispatchAsync(
            handlers,
            new TestNotification(10),
            (_, _, ct) =>
            {
                receivedToken = ct;
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            cts.Token);

        receivedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task DispatchAsync_PassesNotification()
    {
        var strategy = SequentialDispatchStrategy.Instance;
        var handlers = new object[] { 1 };
        var notification = new TestNotification(42);
        TestNotification? receivedNotification = null;

        await strategy.DispatchAsync(
            handlers,
            notification,
            (_, n, _) =>
            {
                receivedNotification = n;
                return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        receivedNotification.ShouldBe(notification);
    }
}
