using Encina.Testing.Identity;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for the holder-based <see cref="RequestContextAccessor"/>: an ended holder is never
/// readable again, from any flow that captured it, and out-of-order ends never resurrect it.
/// </summary>
public sealed class AccessorLifetimeTests
{
    private readonly RequestContextAccessor _accessor = new();

    private static IRequestContext UserContext(string user) => TestRequestContext.For(TestIdentity.User(user));

    // Anonymous contexts told apart by correlation id: the setter refuses to swap one user for another.
    private static IRequestContext Anonymous(string correlationId) => RequestContext.CreateForTest(correlationId: correlationId);

    [Fact]
    public async Task Push_ThenPop_RestoresThePreviousContext()
    {
        await Task.Yield();
        var outer = UserContext("outer");
        _accessor.RequestContext = outer;

        var holder = RequestContextAccessor.Push(UserContext("inner"));
        _accessor.RequestContext!.UserId.ShouldBe("inner");

        RequestContextAccessor.Pop(holder).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeSameAs(outer);
    }

    [Fact]
    public async Task ATaskStartedInsideAScope_AndRunAfterItEnded_SeesNoContext()
    {
        await Task.Yield();
        var gate = new TaskCompletionSource();
        var holder = RequestContextAccessor.Push(UserContext("alice"));
        var captured = Task.Run(async () =>
        {
            await gate.Task;
            return _accessor.RequestContext;
        });

        RequestContextAccessor.Pop(holder);
        gate.SetResult();

        (await captured).ShouldBeNull();
    }

    [Fact]
    public async Task AValueSetInsideAScope_IsUnreadableOnceTheScopeEnds()
    {
        await Task.Yield();
        var gate = new TaskCompletionSource();
        var setDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holder = RequestContextAccessor.Push(UserContext("alice"));
        var captured = Task.Run(async () =>
        {
            _accessor.RequestContext = Anonymous("set-inside");
            setDone.SetResult();
            await gate.Task;
            return _accessor.RequestContext;
        });

        // The value is set while the scope is live, so it is bound to it.
        await setDone.Task;
        RequestContextAccessor.Pop(holder);
        gate.SetResult();

        (await captured).ShouldBeNull();
    }

    [Fact]
    public async Task EndingAnOuterScopeFirst_InvalidatesTheInnerOne_AndTheLaterInnerEndStaysAnonymous()
    {
        await Task.Yield();
        var outer = RequestContextAccessor.Push(UserContext("outer"));
        var inner = RequestContextAccessor.Push(UserContext("inner"));

        RequestContextAccessor.Pop(outer).ShouldBeFalse();
        _accessor.RequestContext.ShouldBeNull();

        // The inner holder is still current in this flow: it restores its parent, which has ended.
        RequestContextAccessor.Pop(inner).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeNull();
        outer.IsDisposed.ShouldBeTrue();
        inner.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task EndingAScopeFromAnotherFlow_AfterThatFlowSetItsOwnValue_NeverInstallsItsParentThere()
    {
        await Task.Yield();
        _accessor.RequestContext = UserContext("alice");
        var holder = RequestContextAccessor.Push(UserContext("job"));

        var seenInOtherFlow = await Task.Run(() =>
        {
            RequestContextAccessor.Push(Anonymous("other-flow"));
            var inOrder = RequestContextAccessor.Pop(holder);
            return (inOrder, _accessor.RequestContext?.UserId);
        });

        // The other flow's current scope is its own: ending the job scope there is out of order.
        seenInOtherFlow.inOrder.ShouldBeFalse();
        seenInOtherFlow.UserId.ShouldBeNull();
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task SettingAValueInsideAScope_ThenEndingItInOrder_RestoresTheOuterContext()
    {
        await Task.Yield();
        var outer = UserContext("alice");
        _accessor.RequestContext = outer;
        var scope = RequestContextAccessor.Push(Anonymous("scope"));
        _accessor.RequestContext = Anonymous("set-inside");

        RequestContextAccessor.Pop(scope).ShouldBeTrue();

        _accessor.RequestContext.ShouldBeSameAs(outer);
    }

    [Fact]
    public async Task AfterAnOutOfOrderEnd_ANewPushInTheOwningFlow_IsReadable()
    {
        await Task.Yield();
        var outer = RequestContextAccessor.Push(UserContext("outer"));
        RequestContextAccessor.Push(UserContext("inner"));
        RequestContextAccessor.Pop(outer).ShouldBeFalse();
        _accessor.RequestContext.ShouldBeNull();

        var fresh = RequestContextAccessor.Push(Anonymous("fresh"));

        _accessor.RequestContext!.CorrelationId.ShouldBe("fresh");
        RequestContextAccessor.Pop(fresh).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task AFlowWhoseScopeEnded_CanSetAndPushANewReadableContext()
    {
        await Task.Yield();
        var gate = new TaskCompletionSource();
        var holder = RequestContextAccessor.Push(UserContext("alice"));
        var captured = Task.Run(async () =>
        {
            await gate.Task;
            var afterEnd = _accessor.RequestContext;
            _accessor.RequestContext = Anonymous("set-after-end");
            var afterSet = _accessor.RequestContext?.CorrelationId;
            RequestContextAccessor.Push(UserContext("bob"));
            return (afterEnd, afterSet, afterPush: _accessor.RequestContext?.UserId);
        });

        RequestContextAccessor.Pop(holder).ShouldBeTrue();
        gate.SetResult();

        var seen = await captured;
        seen.afterEnd.ShouldBeNull();
        seen.afterSet.ShouldBe("set-after-end");
        seen.afterPush.ShouldBe("bob");
    }

    [Fact]
    public async Task PoppingTwice_IsHarmless_AndReportsOutOfOrder()
    {
        await Task.Yield();
        var holder = RequestContextAccessor.Push(UserContext("alice"));

        RequestContextAccessor.Pop(holder).ShouldBeTrue();
        RequestContextAccessor.Pop(holder).ShouldBeFalse();
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task RepeatedSets_DoNotGrowTheHolderChain()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(UserContext("scope"));
        for (var i = 0; i < 1000; i++)
        {
            _accessor.RequestContext = Anonymous($"ctx-{i}");
        }

        _accessor.RequestContext!.CorrelationId.ShouldBe("ctx-999");
        RequestContextAccessor.Pop(scope);
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task SettingNullInsideAScope_StaysBoundToTheScope()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(UserContext("scope"));

        _accessor.RequestContext = null;
        _accessor.RequestContext.ShouldBeNull();
        _accessor.RequestContext = Anonymous("again");
        RequestContextAccessor.Pop(scope).ShouldBeTrue();

        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task ParallelFlows_NeverObserveEachOthersContext()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(async i =>
        {
            await Task.Yield();
            var holder = RequestContextAccessor.Push(UserContext($"user-{i}"));
            await Task.Delay(1);
            var seen = _accessor.RequestContext!.UserId;
            RequestContextAccessor.Pop(holder);
            return (Expected: $"user-{i}", Seen: seen);
        }));

        results.ShouldAllBe(result => result.Expected == result.Seen);
    }

    [Fact]
    public void PushAndPop_RejectNull()
    {
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.Push(null!));
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.Pop(null!));
    }
}
