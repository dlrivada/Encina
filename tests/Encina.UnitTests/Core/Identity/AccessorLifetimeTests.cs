using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for the holder-based <see cref="RequestContextAccessor"/>: ending a holder only
/// invalidates it (the caller comes back through the async frame), an ended holder is never
/// readable again from any flow that captured it, and the setter only preserves the identity and
/// origin it finds.
/// </summary>
public sealed class AccessorLifetimeTests
{
    private readonly RequestContextAccessor _accessor = new();

    private static IRequestContext UserContext(string user) => TestRequestContext.For(TestIdentity.User(user));

    // Anonymous contexts told apart by correlation id (origin Unspecified, what CreateAnonymousAt builds).
    private static IRequestContext Anonymous(string correlationId) => RequestContext.CreateForTest(correlationId: correlationId);

    [Fact]
    public async Task AScope_IsReadableInside_AndTheCallersContextComesBackThroughTheFrame()
    {
        var host = new ScopeTestHost();
        await Task.Yield();
        var outer = Anonymous("outer");
        host.Accessor.RequestContext = outer;

        var inside = await host.InUserScope("inner", _ => Task.FromResult(host.Accessor.RequestContext?.UserId));

        inside.ShouldBe("inner");
        host.Accessor.RequestContext.ShouldBeSameAs(outer);
    }

    [Fact]
    public async Task ATaskStartedInsideAScope_AndRunAfterItEnded_SeesNoContext()
    {
        var host = new ScopeTestHost();
        var gate = new TaskCompletionSource();
        Task<IRequestContext?>? captured = null;

        await host.InUserScope("alice", _ =>
        {
            captured = Task.Run(async () =>
            {
                await gate.Task;
                return host.Accessor.RequestContext;
            });
            return Task.FromResult(0);
        });
        gate.SetResult();

        (await captured!).ShouldBeNull();
    }

    [Fact]
    public async Task AValueSetInsideAScope_IsUnreadableOnceTheScopeEnds()
    {
        var host = new ScopeTestHost();
        var gate = new TaskCompletionSource();
        var setDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<IRequestContext?>? captured = null;

        await host.InUserScope("alice", async context =>
        {
            captured = Task.Run<IRequestContext?>(async () =>
            {
                // Identity- and origin-preserving: accepted, and bound to the live scope.
                host.Accessor.RequestContext = context.WithMetadata("k", "set-inside");
                setDone.SetResult();
                await gate.Task;
                return host.Accessor.RequestContext;
            });
            await setDone.Task;
            return 0;
        });
        gate.SetResult();

        (await captured!).ShouldBeNull();
    }

    [Fact]
    public async Task EndingAnOuterScopeFirst_InvalidatesTheInnerOne_AndTheInnerEndReportsItOutlivedItsParent()
    {
        await Task.Yield();
        var outer = RequestContextAccessor.Push(UserContext("outer"));
        var inner = RequestContextAccessor.Push(UserContext("inner"));

        RequestContextAccessor.End(outer).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeNull();

        RequestContextAccessor.End(inner).ShouldBeFalse();
        _accessor.RequestContext.ShouldBeNull();
        outer.IsDisposed.ShouldBeTrue();
        inner.IsDisposed.ShouldBeTrue();
        inner.EndedAncestorKind().ShouldBe(IdentityKind.User);
    }

    [Fact]
    public async Task AChildForkedInsideAScope_CannotEndTheOwnersScope()
    {
        await Task.Yield();
        var owner = RequestContextAccessor.Push(UserContext("job"));

        var seenInChild = await Task.Run(() =>
        {
            var child = RequestContextAccessor.Push(Anonymous("child"));
            RequestContextAccessor.End(child);

            // End restores nothing: the child's flow reads no context, never the parent.
            return _accessor.RequestContext?.UserId;
        });

        seenInChild.ShouldBeNull();
        _accessor.RequestContext!.UserId.ShouldBe("job");
        owner.IsDisposed.ShouldBeFalse();
        RequestContextAccessor.End(owner).ShouldBeTrue();
    }

    [Fact]
    public async Task End_OnlyInvalidates_AndRestoresNothingInTheSameFlow()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(Anonymous("scope"));
        _accessor.RequestContext = _accessor.RequestContext!.WithMetadata("k", "set-inside");

        RequestContextAccessor.End(scope).ShouldBeTrue();

        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task AfterAnOutOfOrderEnd_ANewPushInTheOwningFlow_IsReadable()
    {
        await Task.Yield();
        var outer = RequestContextAccessor.Push(UserContext("outer"));
        RequestContextAccessor.Push(UserContext("inner"));
        RequestContextAccessor.End(outer).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeNull();

        var fresh = RequestContextAccessor.Push(Anonymous("fresh"));

        _accessor.RequestContext!.CorrelationId.ShouldBe("fresh");
        RequestContextAccessor.End(fresh).ShouldBeTrue();
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task AFlowWhoseScopeEnded_CanSetAnAnonymousContext_ButKeepsTheFactsOfTheEndedChain()
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
            return (afterEnd, afterSet, facts: RequestContextAccessor.CurrentFacts);
        });

        RequestContextAccessor.End(holder).ShouldBeTrue();
        gate.SetResult();

        var seen = await captured;
        seen.afterEnd.ShouldBeNull();
        seen.afterSet.ShouldBe("set-after-end");
        seen.facts.HasFlag(ChainFacts.User).ShouldBeTrue();
    }

    [Fact]
    public async Task ADispatchEndingAfterItsFlowsScopeEnded_DoesNotReviveTheScopesContext()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(UserContext("alice"));
        var nested = RequestContext.ForNestedDispatch(_accessor.RequestContext!, TimeProvider.System.GetUtcNow());
        var dispatch = AmbientRequestContext.Enter(_accessor, nested);
        _accessor.RequestContext!.UserId.ShouldBe("alice");

        // The owner ends the scope while the dispatch is still running in this flow.
        RequestContextAccessor.End(scope);
        _accessor.RequestContext.ShouldBeNull();
        dispatch.Dispose();

        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task AStreamStepAfterTheConsumersScopeEnded_ReadsNoContext()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(UserContext("alice"));
        var context = _accessor.RequestContext!;
        var seen = new List<string?>();
        var source = Steps();

        await using var enumerator = AmbientRequestContext.Flow(source, _accessor, context).GetAsyncEnumerator();
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        RequestContextAccessor.End(scope);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();

        seen.ShouldBe(["alice", null]);

        async IAsyncEnumerable<int> Steps()
        {
            for (var i = 0; i < 2; i++)
            {
                await Task.Yield();
                seen.Add(_accessor.RequestContext?.UserId);
                yield return i;
            }
        }
    }

    [Fact]
    public async Task EndingTwice_IsHarmless_AndTheSecondEndReportsAnEndedHolder()
    {
        await Task.Yield();
        var holder = RequestContextAccessor.Push(UserContext("alice"));

        RequestContextAccessor.End(holder).ShouldBeTrue();
        RequestContextAccessor.End(holder).ShouldBeFalse();
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task RepeatedSets_DoNotGrowTheHolderChain()
    {
        await Task.Yield();
        var scope = RequestContextAccessor.Push(UserContext("scope"));
        for (var i = 0; i < 1000; i++)
        {
            _accessor.RequestContext = _accessor.RequestContext!.WithMetadata("i", i);
        }

        _accessor.RequestContext!.Metadata["i"].ShouldBe(999);
        RequestContextAccessor.Current!.Parent.ShouldBeSameAs(scope);
        RequestContextAccessor.End(scope);
        _accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task SettingNull_IsRefused_LogsWarning165_AndKeepsTheContext()
    {
        await Task.Yield();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);
        var scope = RequestContextAccessor.Push(UserContext("scope"));

        Should.Throw<InvalidOperationException>(() => accessor.RequestContext = null);

        accessor.RequestContext!.UserId.ShouldBe("scope");
        logger.Collector.GetSnapshot().Single().Id.Id.ShouldBe(165);
        RequestContextAccessor.End(scope);
    }

    [Fact]
    public async Task ParallelFlows_NeverObserveEachOthersContext()
    {
        var host = new ScopeTestHost();
        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(async i =>
        {
            await Task.Yield();
            var seen = await host.InUserScope($"user-{i}", async _ =>
            {
                await Task.Delay(1);
                return host.Accessor.RequestContext!.UserId;
            });
            return (Expected: $"user-{i}", Seen: seen);
        }));

        results.ShouldAllBe(result => result.Expected == result.Seen);
    }

    [Fact]
    public void PushAndEnd_RejectNull()
    {
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.Push(null!));
        Should.Throw<ArgumentNullException>(() => RequestContextAccessor.End(null!));
    }

    [Fact]
    public async Task ReadContext_OfAHolderWhoseIdentityIssuerEnded_IsNull()
    {
        var host = new ScopeTestHost();
        IRequestContext? issued = null;
        await host.InUserScope("alice", context =>
        {
            issued = context;
            return Task.FromResult(0);
        });

        // A holder that still holds the identity (installed elsewhere) reads nothing once its issuer ended.
        await Task.Yield();
        var holder = RequestContextAccessor.Push(issued!);
        _accessor.RequestContext.ShouldBeNull();
        RequestContextAccessor.End(holder);
    }
}
