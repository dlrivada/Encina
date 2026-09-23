using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Encina.Caching;
using Encina.DomainModeling;
using Encina.Messaging.Inbox;
using Encina.Messaging.Serialization;
using Encina.Testing;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core;

/// <summary>
/// The context of a dispatch nested inside another one (#1147 review): it inherits the identity of
/// the outer dispatch but never its idempotency key, and gets its own timestamp.
/// </summary>
public sealed class NestedDispatchContextTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    // ── Shared probes ──────────────────────────────────────────────────

    /// <summary>Every context a behavior or handler saw, in order, tagged with who saw it.</summary>
    private sealed class Seen
    {
        public ConcurrentQueue<(string Who, IRequestContext Context)> Items { get; } = new();

        public void Add(string who, IRequestContext? context) => Items.Enqueue((who, context!));

        public IRequestContext[] Of(string who) => [.. Items.Where(i => i.Who == who).Select(i => i.Context)];
    }

    private sealed class RecordingBehavior<TRequest, TResponse>(Seen seen) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request,
            IRequestContext context,
            RequestHandlerCallback<TResponse> nextStep,
            CancellationToken cancellationToken)
        {
            seen.Add($"behavior:{typeof(TRequest).Name}", context);
            return nextStep();
        }
    }

    private sealed record Leaf : IRequest<Unit>;

    private sealed class LeafHandler(IRequestContextAccessor accessor, Seen seen) : IRequestHandler<Leaf, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(Leaf request, CancellationToken cancellationToken)
        {
            seen.Add("leaf", accessor.RequestContext);
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    /// <summary>Sends <see cref="Children"/> leaves one after another, advancing the clock before each.</summary>
    private sealed record Parent(int Children, IRequestContext? ExplicitChildContext = null) : IRequest<Unit>;

    private sealed class ParentHandler(IEncina encina, IRequestContextAccessor accessor, FakeTimeProvider time, Seen seen)
        : IRequestHandler<Parent, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(Parent request, CancellationToken cancellationToken)
        {
            seen.Add("parent", accessor.RequestContext);
            for (var i = 0; i < request.Children; i++)
            {
                time.Advance(TimeSpan.FromSeconds(1));
                var result = request.ExplicitChildContext is null
                    ? await encina.Send(new Leaf(), cancellationToken)
                    : await encina.Send(new Leaf(), request.ExplicitChildContext, cancellationToken);
                if (result.IsLeft)
                {
                    return result;
                }
            }

            return Unit.Default;
        }
    }

    /// <summary>Sends a <see cref="Parent"/> with one child: three levels of dispatch.</summary>
    private sealed record GrandParent : IRequest<Unit>;

    private sealed class GrandParentHandler(IEncina encina) : IRequestHandler<GrandParent, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(GrandParent request, CancellationToken cancellationToken)
            => await encina.Send(new Parent(1), cancellationToken);
    }

    private sealed record Happened : INotification;

    private sealed class HappenedHandler(IEncina encina, IRequestContextAccessor accessor, Seen seen) : INotificationHandler<Happened>
    {
        public async Task<Either<EncinaError, Unit>> Handle(Happened notification, CancellationToken cancellationToken)
        {
            seen.Add("notification", accessor.RequestContext);
            return await encina.Send(new Leaf(), cancellationToken);
        }
    }

    private sealed record Announce : IRequest<Unit>;

    private sealed class AnnounceHandler(IEncina encina) : IRequestHandler<Announce, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(Announce request, CancellationToken cancellationToken)
            => await encina.Publish(new Happened(), cancellationToken);
    }

    private sealed record OrderShipped : DomainEvent, INotification;

    private sealed class Order(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Ship() => RaiseDomainEvent(new OrderShipped());
    }

    private sealed class OrderShippedHandler(IRequestContextAccessor accessor, Seen seen) : INotificationHandler<OrderShipped>
    {
        public Task<Either<EncinaError, Unit>> Handle(OrderShipped notification, CancellationToken cancellationToken)
        {
            seen.Add("domain-event", accessor.RequestContext);
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    /// <summary>Raises a domain event and dispatches it the way a unit of work does on commit.</summary>
    private sealed record ShipOrder : IRequest<Unit>;

    private sealed class ShipOrderHandler(IEncina encina) : IRequestHandler<ShipOrder, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(ShipOrder request, CancellationToken cancellationToken)
        {
            var order = new Order(Guid.NewGuid());
            order.Ship();
            var collector = new DomainEventCollector();
            collector.TrackAggregate(order);
            return await new DomainEventDispatchHelper(encina, collector).DispatchCollectedEventsAsync(cancellationToken);
        }
    }

    // ── Idempotency probes ─────────────────────────────────────────────

    private sealed class Counters
    {
        private readonly ConcurrentDictionary<string, int> _counts = new();

        public int Increment(string name) => _counts.AddOrUpdate(name, 1, static (_, c) => c + 1);

        public int this[string name] => _counts.GetValueOrDefault(name);
    }

    private sealed record InboxInner(string Name) : ICommand<int>, IIdempotentRequest;

    private sealed class InboxInnerHandler(Counters counters) : IRequestHandler<InboxInner, int>
    {
        public Task<Either<EncinaError, int>> Handle(InboxInner request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(counters.Increment(request.Name)));
    }

    /// <summary>Sends one nested idempotent command per name; a non-null key sends it with its own key.</summary>
    private sealed record InboxOuter(string[] Children, string? ChildKey = null) : ICommand<int>, IIdempotentRequest;

    private sealed class InboxOuterHandler(IEncina encina, IRequestContextAccessor accessor, Counters counters)
        : IRequestHandler<InboxOuter, int>
    {
        public async Task<Either<EncinaError, int>> Handle(InboxOuter request, CancellationToken cancellationToken)
        {
            foreach (var child in request.Children)
            {
                var result = request.ChildKey is null
                    ? await encina.Send(new InboxInner(child), cancellationToken)
                    : await encina.Send(new InboxInner(child), accessor.RequestContext!.WithIdempotencyKey(request.ChildKey), cancellationToken);
                if (result.IsLeft)
                {
                    return result;
                }
            }

            return counters.Increment("outer");
        }
    }

    private sealed record CacheInner(string Name) : ICommand<int>, IDistributedIdempotentRequest;

    private sealed class CacheInnerHandler(Counters counters) : IRequestHandler<CacheInner, int>
    {
        public Task<Either<EncinaError, int>> Handle(CacheInner request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(counters.Increment(request.Name)));
    }

    private sealed record CacheOuter(string[] Children) : ICommand<int>, IDistributedIdempotentRequest;

    private sealed class CacheOuterHandler(IEncina encina, Counters counters) : IRequestHandler<CacheOuter, int>
    {
        public async Task<Either<EncinaError, int>> Handle(CacheOuter request, CancellationToken cancellationToken)
        {
            foreach (var child in request.Children)
            {
                var result = await encina.Send(new CacheInner(child), cancellationToken);
                if (result.IsLeft)
                {
                    return result;
                }
            }

            return counters.Increment("outer");
        }
    }

    private sealed class FakeInboxMessageFactory : IInboxMessageFactory
    {
        public IInboxMessage Create(string messageId, string requestType, DateTime receivedAtUtc, DateTime expiresAtUtc, InboxMetadata? metadata)
            => new FakeInboxMessage
            {
                MessageId = messageId,
                RequestType = requestType,
                ReceivedAtUtc = receivedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            };
    }

    // ── Restore-path probes ────────────────────────────────────────────

    private sealed record Explode : IRequest<Unit>;

    private sealed class ExplodeHandler : IRequestHandler<Explode, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(Explode request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("handler bug");
        }
    }

    private sealed record ExplodeInBehavior : IRequest<Unit>;

    private sealed class ExplodeInBehaviorHandler : IRequestHandler<ExplodeInBehavior, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(ExplodeInBehavior request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
    }

    private sealed class ThrowingBehavior : IPipelineBehavior<ExplodeInBehavior, Unit>
    {
        public async ValueTask<Either<EncinaError, Unit>> Handle(
            ExplodeInBehavior request,
            IRequestContext context,
            RequestHandlerCallback<Unit> nextStep,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("behavior bug");
        }
    }

    private sealed record WaitForCancel : IRequest<Unit>;

    private sealed class WaitForCancelHandler : IRequestHandler<WaitForCancel, Unit>
    {
        public async Task<Either<EncinaError, Unit>> Handle(WaitForCancel request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Unit.Default;
        }
    }

    /// <summary>Yields <see cref="Count"/> items and throws before item <see cref="ThrowAt"/> when set.</summary>
    private sealed record Ticks(int Count, int? ThrowAt = null) : IStreamRequest<int>;

    private sealed class TicksHandler(IRequestContextAccessor accessor, Seen seen) : IStreamRequestHandler<Ticks, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            Ticks request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < request.Count; i++)
                {
                    await Task.Yield();
                    if (i == request.ThrowAt)
                    {
                        throw new InvalidOperationException("stream bug");
                    }

                    seen.Add("tick", accessor.RequestContext);
                    yield return i;
                }
            }
            finally
            {
                seen.Add("stream-finally", accessor.RequestContext);
            }
        }
    }

    // ── Setup ──────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(FakeTimeProvider? time = null, FakeInboxStore? inboxStore = null, FakeCacheProvider? cache = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var clock = time ?? new FakeTimeProvider(Start);
        services.AddSingleton(clock);
        services.AddSingleton<TimeProvider>(clock);
        services.AddEncina();
        services.AddSingleton<Seen>();
        services.AddSingleton<Counters>();

        services.AddScoped<IRequestHandler<Leaf, Unit>, LeafHandler>();
        services.AddScoped<IRequestHandler<Parent, Unit>, ParentHandler>();
        services.AddScoped<IRequestHandler<GrandParent, Unit>, GrandParentHandler>();
        services.AddScoped<IRequestHandler<Announce, Unit>, AnnounceHandler>();
        services.AddScoped<IRequestHandler<ShipOrder, Unit>, ShipOrderHandler>();
        services.AddScoped<INotificationHandler<Happened>, HappenedHandler>();
        services.AddScoped<INotificationHandler<OrderShipped>, OrderShippedHandler>();
        services.AddScoped<IPipelineBehavior<Leaf, Unit>, RecordingBehavior<Leaf, Unit>>();
        services.AddScoped<IPipelineBehavior<Parent, Unit>, RecordingBehavior<Parent, Unit>>();

        services.AddScoped<IRequestHandler<Explode, Unit>, ExplodeHandler>();
        services.AddScoped<IRequestHandler<ExplodeInBehavior, Unit>, ExplodeInBehaviorHandler>();
        services.AddScoped<IPipelineBehavior<ExplodeInBehavior, Unit>, ThrowingBehavior>();
        services.AddScoped<IRequestHandler<WaitForCancel, Unit>, WaitForCancelHandler>();
        services.AddScoped<IStreamRequestHandler<Ticks, int>, TicksHandler>();

        // Inbox (database-style idempotency) over an in-memory store.
        services.AddSingleton<IInboxStore>(inboxStore ?? new FakeInboxStore(clock));
        services.AddSingleton(new InboxOptions());
        services.AddSingleton<IInboxMessageFactory, FakeInboxMessageFactory>();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddScoped<InboxOrchestrator>();
        services.AddScoped<IRequestHandler<InboxOuter, int>, InboxOuterHandler>();
        services.AddScoped<IRequestHandler<InboxInner, int>, InboxInnerHandler>();
        services.AddScoped<IPipelineBehavior<InboxOuter, int>, InboxPipelineBehavior<InboxOuter, int>>();
        services.AddScoped<IPipelineBehavior<InboxInner, int>, InboxPipelineBehavior<InboxInner, int>>();

        // Distributed (cache) idempotency over an in-memory cache.
        services.AddSingleton<ICacheProvider>(cache ?? new FakeCacheProvider(timeProvider: clock));
        services.AddSingleton(Options.Create(new CachingOptions { EnableDistributedIdempotency = true }));
        services.AddScoped<IRequestHandler<CacheOuter, int>, CacheOuterHandler>();
        services.AddScoped<IRequestHandler<CacheInner, int>, CacheInnerHandler>();
        services.AddScoped<IPipelineBehavior<CacheOuter, int>, DistributedIdempotencyPipelineBehavior<CacheOuter, int>>();
        services.AddScoped<IPipelineBehavior<CacheInner, int>, DistributedIdempotencyPipelineBehavior<CacheInner, int>>();

        return services.BuildServiceProvider();
    }

    /// <summary>The context an entry point (e.g. EncinaContextMiddleware) puts on the accessor.</summary>
    private static IRequestContext EntryContext(string idempotencyKey = "entry-key")
        => RequestContext.CreateForTest(userId: "user-1", tenantId: "tenant-1", idempotencyKey: idempotencyKey, correlationId: "corr-1")
            .WithMetadata("custom", "value");

    private static void ShouldBeDerivedFrom(IRequestContext nested, IRequestContext parent)
    {
        nested.ShouldNotBeSameAs(parent);
        nested.CorrelationId.ShouldBe(parent.CorrelationId);
        nested.UserId.ShouldBe(parent.UserId);
        nested.TenantId.ShouldBe(parent.TenantId);
        nested.IdempotencyKey.ShouldBeNull();
        nested.IsNestedDispatch().ShouldBeTrue();
        foreach (var (key, value) in parent.Metadata)
        {
            nested.Metadata[key].ShouldBe(value);
        }
    }

    // ── Context derivation ─────────────────────────────────────────────

    [Fact]
    public async Task EntryPointDispatch_UsesTheAmbientContextAsIs_IdempotencyKeyIncluded()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new Leaf())).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        seen.Of("behavior:Leaf").ShouldHaveSingleItem().ShouldBeSameAs(entry);
        entry.IsNestedDispatch().ShouldBeFalse();
    }

    [Fact]
    public async Task NestedSend_GetsADerivedContext_WithTheParentIdentity_ItsOwnTimestamp_AndNoIdempotencyKey()
    {
        var time = new FakeTimeProvider(Start);
        await using var provider = BuildProvider(time);
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new Parent(1))).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        seen.Of("parent").ShouldHaveSingleItem().ShouldBeSameAs(entry);
        var nested = seen.Of("behavior:Leaf").ShouldHaveSingleItem();
        ShouldBeDerivedFrom(nested, entry);
        nested.Timestamp.ShouldBe(Start.AddSeconds(1));

        // The handler of the nested request sees the same derived context through the accessor.
        seen.Of("leaf").ShouldHaveSingleItem().ShouldBeSameAs(nested);
    }

    [Fact]
    public async Task SiblingNestedSends_EachGetTheirOwnDerivedContext_AndTimestamp()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new Parent(2))).ShouldBeSuccess();

        var nested = provider.GetRequiredService<Seen>().Of("behavior:Leaf");
        nested.Length.ShouldBe(2);
        nested[0].ShouldNotBeSameAs(nested[1]);
        nested[0].Timestamp.ShouldBe(Start.AddSeconds(1));
        nested[1].Timestamp.ShouldBe(Start.AddSeconds(2));
        nested.ShouldAllBe(c => c.IdempotencyKey == null && c.CorrelationId == entry.CorrelationId);
    }

    [Fact]
    public async Task DeeplyNestedSend_StaysDerived_WithoutTheKey()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new GrandParent())).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        var parent = seen.Of("behavior:Parent").ShouldHaveSingleItem();
        var leaf = seen.Of("behavior:Leaf").ShouldHaveSingleItem();
        ShouldBeDerivedFrom(parent, entry);
        ShouldBeDerivedFrom(leaf, entry);
        leaf.ShouldNotBeSameAs(parent);
    }

    [Fact]
    public async Task NestedSend_WithAnExplicitContext_UsesItAsIs()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = EntryContext();
        var childContext = RequestContext.CreateForTest(userId: "child-user", idempotencyKey: "child-key");

        (await provider.GetRequiredService<IEncina>().Send(new Parent(1, childContext))).ShouldBeSuccess();

        provider.GetRequiredService<Seen>().Of("behavior:Leaf").ShouldHaveSingleItem().ShouldBeSameAs(childContext);
    }

    [Fact]
    public async Task EntryPointDispatch_WithoutAnyContext_IsStampedByTheTimeProvider_AndItsNestedSendIsDerived()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;

        (await provider.GetRequiredService<IEncina>().Send(new Parent(1))).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        var outer = seen.Of("parent").ShouldHaveSingleItem();
        outer.Timestamp.ShouldBe(Start);
        outer.IsNestedDispatch().ShouldBeFalse();
        ShouldBeDerivedFrom(seen.Of("behavior:Leaf").ShouldHaveSingleItem(), outer);
    }

    [Fact]
    public async Task PublishInsideAHandler_NotificationHandlersAndTheirSends_DoNotInheritTheKey()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new Announce())).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        var notificationContext = seen.Of("notification").ShouldHaveSingleItem();
        ShouldBeDerivedFrom(notificationContext, entry);
        var leaf = seen.Of("behavior:Leaf").ShouldHaveSingleItem();
        ShouldBeDerivedFrom(leaf, entry);
        leaf.ShouldNotBeSameAs(notificationContext);
    }

    [Fact]
    public async Task EntryPointPublish_NotificationHandlerSends_DoNotInheritTheKey()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Publish(new Happened())).ShouldBeSuccess();

        var seen = provider.GetRequiredService<Seen>();
        seen.Of("notification").ShouldHaveSingleItem().ShouldBeSameAs(entry);
        ShouldBeDerivedFrom(seen.Of("behavior:Leaf").ShouldHaveSingleItem(), entry);
    }

    [Fact]
    public async Task DomainEventsPublishedFromAHandler_DoNotInheritTheKey()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = entry;

        (await provider.GetRequiredService<IEncina>().Send(new ShipOrder())).ShouldBeSuccess();

        ShouldBeDerivedFrom(provider.GetRequiredService<Seen>().Of("domain-event").ShouldHaveSingleItem(), entry);
    }

    [Fact]
    public async Task EntryPointStream_UsesTheAmbientContextAsIs()
    {
        await using var provider = BuildProvider();
        var entry = EntryContext();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var encina = provider.GetRequiredService<IEncina>();

        accessor.RequestContext = entry;
        await foreach (var item in encina.Stream(new Ticks(1)))
        {
            item.ShouldBeSuccess();
        }

        provider.GetRequiredService<Seen>().Of("tick").ShouldHaveSingleItem().ShouldBeSameAs(entry);
    }

    // ── Idempotency: inbox ─────────────────────────────────────────────

    [Fact]
    public async Task Inbox_NestedIdempotentSend_OuterAndInnerBothRun_AndARepeatedOuterKeyRunsNeither()
    {
        var inbox = new FakeInboxStore();
        await using var provider = BuildProvider(inboxStore: inbox);
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var counters = provider.GetRequiredService<Counters>();

        accessor.RequestContext = EntryContext("order-1");
        var first = await encina.Send(new InboxOuter(["a"]));
        accessor.RequestContext = EntryContext("order-1");
        var repeated = await encina.Send(new InboxOuter(["a"]));

        first.ShouldBeSuccess().ShouldBe(1);
        repeated.ShouldBeSuccess().ShouldBe(1);
        counters["outer"].ShouldBe(1);
        counters["a"].ShouldBe(1);
        inbox.IsMessageProcessed("order-1").ShouldBeTrue();
        inbox.GetAddedMessages().ShouldHaveSingleItem().MessageId.ShouldBe("order-1");
    }

    [Fact]
    public async Task Inbox_SiblingNestedIdempotentSends_EachRun()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = EntryContext("order-2");

        var result = await provider.GetRequiredService<IEncina>().Send(new InboxOuter(["a", "b", "a"]));

        result.ShouldBeSuccess().ShouldBe(1);
        var counters = provider.GetRequiredService<Counters>();
        counters["a"].ShouldBe(2);
        counters["b"].ShouldBe(1);
    }

    [Fact]
    public async Task Inbox_NestedSendWithItsOwnKey_IsDeduplicatedByThatKey()
    {
        var inbox = new FakeInboxStore();
        await using var provider = BuildProvider(inboxStore: inbox);
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();

        accessor.RequestContext = EntryContext("order-3");
        (await encina.Send(new InboxOuter(["a"], ChildKey: "child-1"))).ShouldBeSuccess();
        accessor.RequestContext = EntryContext("order-4");
        (await encina.Send(new InboxOuter(["a"], ChildKey: "child-1"))).ShouldBeSuccess();

        var counters = provider.GetRequiredService<Counters>();
        counters["outer"].ShouldBe(2);
        counters["a"].ShouldBe(1);
        inbox.IsMessageProcessed("child-1").ShouldBeTrue();
    }

    [Fact]
    public async Task Inbox_EntryPointIdempotentSendWithoutKey_IsStillRejected()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;

        var result = await provider.GetRequiredService<IEncina>().Send(new InboxInner("a"));

        result.ShouldBeError();
        provider.GetRequiredService<Counters>()["a"].ShouldBe(0);
    }

    // ── Idempotency: distributed cache ─────────────────────────────────

    [Fact]
    public async Task DistributedIdempotency_NestedIdempotentSend_OuterAndInnerBothRun_AndARepeatedOuterKeyRunsNeither()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var counters = provider.GetRequiredService<Counters>();

        accessor.RequestContext = EntryContext("pay-1");
        var first = await encina.Send(new CacheOuter(["a"]));
        accessor.RequestContext = EntryContext("pay-1");
        var repeated = await encina.Send(new CacheOuter(["a"]));

        first.ShouldBeSuccess().ShouldBe(1);
        repeated.ShouldBeSuccess().ShouldBe(1);
        counters["outer"].ShouldBe(1);
        counters["a"].ShouldBe(1);
    }

    [Fact]
    public async Task DistributedIdempotency_SiblingNestedIdempotentSends_EachRun()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = EntryContext("pay-2");

        var result = await provider.GetRequiredService<IEncina>().Send(new CacheOuter(["a", "a"]));

        result.ShouldBeSuccess().ShouldBe(1);
        provider.GetRequiredService<Counters>()["a"].ShouldBe(2);
    }

    [Fact]
    public async Task DistributedIdempotency_EntryPointSendWithoutKey_IsStillRejected()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;

        var result = await provider.GetRequiredService<IEncina>().Send(new CacheInner("a"));

        result.ShouldBeError();
    }

    // ── Restore paths ──────────────────────────────────────────────────

    /// <summary>
    /// After a dispatch, the ambient context is the caller's again and no dispatch is in flight: a
    /// follow-up send is an entry point that keeps the ambient context as-is.
    /// </summary>
    private static async Task ShouldBeRestoredAsync(ServiceProvider provider, IRequestContext callerContext)
    {
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        accessor.RequestContext.ShouldBeSameAs(callerContext);

        var seen = provider.GetRequiredService<Seen>();
        seen.Items.Clear();
        (await provider.GetRequiredService<IEncina>().Send(new Leaf())).ShouldBeSuccess();
        seen.Of("behavior:Leaf").ShouldHaveSingleItem().ShouldBeSameAs(callerContext);
    }

    [Fact]
    public async Task AfterAHandlerThrows_TheAmbientContextAndDispatchStateAreRestored()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = caller;
        var explicitContext = RequestContext.CreateForTest(userId: "job");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await provider.GetRequiredService<IEncina>().Send(new Explode(), explicitContext));

        await ShouldBeRestoredAsync(provider, caller);
    }

    [Fact]
    public async Task AfterABehaviorThrows_TheAmbientContextAndDispatchStateAreRestored()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = caller;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await provider.GetRequiredService<IEncina>().Send(new ExplodeInBehavior(), RequestContext.CreateForTest(userId: "job")));

        await ShouldBeRestoredAsync(provider, caller);
    }

    [Fact]
    public async Task AfterCancellation_TheAmbientContextAndDispatchStateAreRestored()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = caller;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        var result = await provider.GetRequiredService<IEncina>().Send(new WaitForCancel(), RequestContext.CreateForTest(userId: "job"), cts.Token);

        result.ShouldBeError();
        await ShouldBeRestoredAsync(provider, caller);
    }

    [Fact]
    public async Task StreamEarlyBreak_DisposesUnderTheStreamContext_AndRestoresTheCallerContext()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = caller;
        var streamContext = RequestContext.CreateForTest(userId: "export");

        await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new Ticks(5), streamContext))
        {
            item.ShouldBeSuccess();
            break;
        }

        var seen = provider.GetRequiredService<Seen>();
        seen.Of("tick").ShouldHaveSingleItem().ShouldBeSameAs(streamContext);
        seen.Of("stream-finally").ShouldHaveSingleItem().ShouldBeSameAs(streamContext);
        await ShouldBeRestoredAsync(provider, caller);
    }

    [Fact]
    public async Task StreamExceptionMidStream_PropagatesToTheConsumer_AndRestoresTheCallerContext()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = caller;
        var streamContext = RequestContext.CreateForTest(userId: "export");
        var received = 0;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new Ticks(5, ThrowAt: 2), streamContext))
            {
                item.ShouldBeSuccess();
                received++;
            }
        });

        received.ShouldBe(2);
        provider.GetRequiredService<Seen>().Of("stream-finally").ShouldHaveSingleItem().ShouldBeSameAs(streamContext);
        await ShouldBeRestoredAsync(provider, caller);
    }

    [Fact]
    public async Task StreamDisposeAsync_WithoutFullEnumeration_RunsTheHandlerCleanupUnderTheStreamContext()
    {
        await using var provider = BuildProvider();
        var caller = EntryContext();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        accessor.RequestContext = caller;
        var streamContext = RequestContext.CreateForTest(userId: "export");

        var enumerator = provider.GetRequiredService<IEncina>().Stream(new Ticks(5), streamContext).GetAsyncEnumerator();
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        accessor.RequestContext.ShouldBeSameAs(caller);
        await enumerator.DisposeAsync();

        provider.GetRequiredService<Seen>().Of("stream-finally").ShouldHaveSingleItem().ShouldBeSameAs(streamContext);
        await ShouldBeRestoredAsync(provider, caller);
    }
}
