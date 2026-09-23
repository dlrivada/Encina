using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Encina.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core;

/// <summary>
/// How <see cref="IEncina"/> seeds the pipeline's <see cref="IRequestContext"/> from the ambient
/// <see cref="IRequestContextAccessor"/> or from an explicit context, and keeps it scoped to the
/// dispatch (issue #1147).
/// </summary>
public sealed class AmbientRequestContextTests
{
    // ── Requests, handlers and behaviors ───────────────────────────────

    private sealed record Probe(string Label) : IRequest<Observation>;

    private sealed record Observation(IRequestContext PipelineContext, IRequestContext? AmbientInHandler);

    private sealed class ProbeHandler(IRequestContextAccessor accessor) : IRequestHandler<Probe, Observation>
    {
        public async Task<Either<EncinaError, Observation>> Handle(Probe request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new Observation(ContextSink.Last.Value!, accessor.RequestContext);
        }
    }

    /// <summary>Records the context the pipeline passes to behaviors, per async flow.</summary>
    private static class ContextSink
    {
        public static readonly AsyncLocal<IRequestContext?> Last = new();
    }

    private sealed class RecordingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request,
            IRequestContext context,
            RequestHandlerCallback<TResponse> nextStep,
            CancellationToken cancellationToken)
        {
            ContextSink.Last.Value = context;
            return nextStep();
        }
    }

    private sealed record Outer : IRequest<(IRequestContext Outer, IRequestContext Inner)>;

    private sealed record Inner : IRequest<IRequestContext>;

    private sealed class InnerHandler : IRequestHandler<Inner, IRequestContext>
    {
        public Task<Either<EncinaError, IRequestContext>> Handle(Inner request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, IRequestContext>(ContextSink.Last.Value!));
    }

    private sealed class OuterHandler(IEncina encina) : IRequestHandler<Outer, (IRequestContext Outer, IRequestContext Inner)>
    {
        public async Task<Either<EncinaError, (IRequestContext Outer, IRequestContext Inner)>> Handle(Outer request, CancellationToken cancellationToken)
        {
            var outerContext = ContextSink.Last.Value!;
            var inner = await encina.Send(new Inner(), cancellationToken);
            return inner.Map(innerContext => (outerContext, innerContext));
        }
    }

    private sealed record Ping(string Label) : INotification;

    private sealed class NotificationObservations
    {
        public ConcurrentQueue<IRequestContext?> Seen { get; } = new();
    }

    private sealed class PingHandler(IRequestContextAccessor accessor, NotificationObservations observations) : INotificationHandler<Ping>
    {
        public async Task<Either<EncinaError, Unit>> Handle(Ping notification, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Seen.Enqueue(accessor.RequestContext);
            return Unit.Default;
        }
    }

    private sealed record Numbers(int Count) : IStreamRequest<(int Value, IRequestContext? Ambient)>;

    private sealed class NumbersHandler(IRequestContextAccessor accessor) : IStreamRequestHandler<Numbers, (int Value, IRequestContext? Ambient)>
    {
        public async IAsyncEnumerable<Either<EncinaError, (int Value, IRequestContext? Ambient)>> Handle(
            Numbers request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < request.Count; i++)
            {
                await Task.Delay(1, cancellationToken);
                yield return (i, accessor.RequestContext);
            }
        }
    }

    private sealed class StreamContexts
    {
        public ConcurrentQueue<IRequestContext> Seen { get; } = new();
    }

    private sealed class RecordingStreamBehavior(StreamContexts contexts) : IStreamPipelineBehavior<Numbers, (int Value, IRequestContext? Ambient)>
    {
        public IAsyncEnumerable<Either<EncinaError, (int Value, IRequestContext? Ambient)>> Handle(
            Numbers request,
            IRequestContext context,
            StreamHandlerCallback<(int Value, IRequestContext? Ambient)> nextStep,
            CancellationToken cancellationToken)
        {
            contexts.Seen.Enqueue(context);
            return nextStep();
        }
    }

    // ── Setup ──────────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddSingleton<NotificationObservations>();
        services.AddSingleton<StreamContexts>();
        services.AddScoped<IRequestHandler<Probe, Observation>, ProbeHandler>();
        services.AddScoped<IRequestHandler<Outer, (IRequestContext Outer, IRequestContext Inner)>, OuterHandler>();
        services.AddScoped<IRequestHandler<Inner, IRequestContext>, InnerHandler>();
        services.AddScoped<IPipelineBehavior<Probe, Observation>, RecordingBehavior<Probe, Observation>>();
        services.AddScoped<IPipelineBehavior<Outer, (IRequestContext Outer, IRequestContext Inner)>, RecordingBehavior<Outer, (IRequestContext Outer, IRequestContext Inner)>>();
        services.AddScoped<IPipelineBehavior<Inner, IRequestContext>, RecordingBehavior<Inner, IRequestContext>>();
        services.AddScoped<INotificationHandler<Ping>, PingHandler>();
        services.AddScoped<IStreamRequestHandler<Numbers, (int Value, IRequestContext? Ambient)>, NumbersHandler>();
        services.AddScoped<IStreamPipelineBehavior<Numbers, (int Value, IRequestContext? Ambient)>, RecordingStreamBehavior>();
        return services.BuildServiceProvider();
    }

    private static IRequestContext UserContext(string user, string? tenant = null)
        => RequestContext.CreateForTest(userId: user, tenantId: tenant, correlationId: $"corr-{user}");

    // ── Registration ───────────────────────────────────────────────────

    [Fact]
    public void AddEncina_RegistersTheCoreAccessorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncina();

        var descriptor = services.Single(d => d.ServiceType == typeof(IRequestContextAccessor));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(RequestContextAccessor));
    }

    // ── Send ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_WithAmbientContext_SeedsThePipelineFromTheAccessor()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = UserContext("ambient-user", "ambient-tenant");
        accessor.RequestContext = ambient;

        var result = await encina.Send(new Probe("ambient"));

        var observation = result.ShouldBeSuccess();
        observation.PipelineContext.ShouldBeSameAs(ambient);
        observation.AmbientInHandler.ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Send_WithoutAmbientContext_CreatesAFreshContext_WithTheActivityCorrelationId()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        accessor.RequestContext = null;
        using var activity = new Activity("request-context-test").Start();

        var result = await encina.Send(new Probe("fresh"));

        var observation = result.ShouldBeSuccess();
        observation.PipelineContext.UserId.ShouldBeNull();
        observation.PipelineContext.TenantId.ShouldBeNull();
        observation.PipelineContext.CorrelationId.ShouldBe(activity.Id);
        observation.AmbientInHandler.ShouldBeSameAs(observation.PipelineContext);
        accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task Send_WithExplicitContext_WinsOverTheAmbientContext_AndRestoresItAfterwards()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = UserContext("ambient-user");
        var explicitContext = UserContext("job-user", "job-tenant");
        accessor.RequestContext = ambient;

        var result = await encina.Send(new Probe("explicit"), explicitContext);

        var observation = result.ShouldBeSuccess();
        observation.PipelineContext.ShouldBeSameAs(explicitContext);
        observation.AmbientInHandler.ShouldBeSameAs(explicitContext);
        accessor.RequestContext.ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Send_ConcurrentCallsWithDifferentContexts_NeverSeeEachOthersContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(async i =>
        {
            await Task.Yield();
            var context = UserContext($"user-{i}", $"tenant-{i}");
            var result = await encina.Send(new Probe($"p{i}"), context);
            return (Expected: context, Observation: result.ShouldBeSuccess());
        }));

        foreach (var (expected, observation) in results)
        {
            observation.PipelineContext.ShouldBeSameAs(expected);
            observation.AmbientInHandler.ShouldBeSameAs(expected);
        }
    }

    [Fact]
    public async Task Send_ConcurrentAmbientFlows_EachPipelineSeesItsOwnAmbientContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();

        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(async () =>
        {
            var context = UserContext($"ambient-{i}");
            accessor.RequestContext = context;
            await Task.Yield();
            var result = await encina.Send(new Probe($"a{i}"));
            return (Expected: context, Observation: result.ShouldBeSuccess());
        })));

        foreach (var (expected, observation) in results)
        {
            observation.PipelineContext.ShouldBeSameAs(expected);
        }
    }

    [Fact]
    public async Task Send_NestedSendInsideAHandler_InheritsTheParentIdentity_ButNotItsIdempotencyKey()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var parent = RequestContext.CreateForTest(userId: "parent-user", tenantId: "parent-tenant", idempotencyKey: "parent-key", correlationId: "corr-parent");

        var result = await encina.Send(new Outer(), parent);

        var (outer, inner) = result.ShouldBeSuccess();
        outer.ShouldBeSameAs(parent);
        inner.ShouldNotBeSameAs(parent);
        inner.UserId.ShouldBe("parent-user");
        inner.TenantId.ShouldBe("parent-tenant");
        inner.CorrelationId.ShouldBe("corr-parent");
        inner.IdempotencyKey.ShouldBeNull();
        inner.IsNestedDispatch().ShouldBeTrue();
    }

    [Fact]
    public async Task Send_NestedSendWithoutAnyContext_DerivesFromTheFreshParentContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;

        var result = await encina.Send(new Outer());

        var (outer, inner) = result.ShouldBeSuccess();
        outer.IsNestedDispatch().ShouldBeFalse();
        inner.CorrelationId.ShouldBe(outer.CorrelationId);
        inner.IsNestedDispatch().ShouldBeTrue();
    }

    [Fact]
    public async Task Send_ExplicitNullContext_Throws()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        (await Should.ThrowAsync<ArgumentNullException>(async () => await encina.Send(new Probe("null"), null!)))
            .ParamName.ShouldBe("context");
    }

    // ── Publish ────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_WithAmbientContext_HandlersObserveIt()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var observations = provider.GetRequiredService<NotificationObservations>();
        var ambient = UserContext("ambient-user");
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = ambient;

        var result = await encina.Publish(new Ping("ambient"));

        result.ShouldBeSuccess();
        observations.Seen.ShouldHaveSingleItem().ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Publish_WithExplicitContext_WinsOverTheAmbientContext_AndRestoresItAfterwards()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var observations = provider.GetRequiredService<NotificationObservations>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = UserContext("ambient-user");
        var explicitContext = UserContext("webhook-user");
        accessor.RequestContext = ambient;

        var result = await encina.Publish(new Ping("explicit"), explicitContext);

        result.ShouldBeSuccess();
        observations.Seen.ShouldHaveSingleItem().ShouldBeSameAs(explicitContext);
        accessor.RequestContext.ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Publish_WithoutAnyContext_HandlersObserveAFreshContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var observations = provider.GetRequiredService<NotificationObservations>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        accessor.RequestContext = null;

        var result = await encina.Publish(new Ping("fresh"));

        result.ShouldBeSuccess();
        var seen = observations.Seen.ShouldHaveSingleItem();
        seen.ShouldNotBeNull();
        seen.UserId.ShouldBeNull();
        accessor.RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task Publish_ExplicitNullContext_Throws()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        (await Should.ThrowAsync<ArgumentNullException>(async () => await encina.Publish(new Ping("null"), null!)))
            .ParamName.ShouldBe("context");
    }

    // ── Stream ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Stream_WithAmbientContext_SeedsThePipeline_AndTheHandlerSeesItForEveryItem()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var contexts = provider.GetRequiredService<StreamContexts>();
        var ambient = UserContext("ambient-user");
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = ambient;

        var items = await CollectAsync(encina.Stream(new Numbers(3)));

        contexts.Seen.ShouldHaveSingleItem().ShouldBeSameAs(ambient);
        items.Count.ShouldBe(3);
        items.ShouldAllBe(item => ReferenceEquals(item.Ambient, ambient));
    }

    [Fact]
    public async Task Stream_WithExplicitContext_WinsOverTheAmbientContext_ForEveryItem_AndRestoresItAfterwards()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();
        var contexts = provider.GetRequiredService<StreamContexts>();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = UserContext("ambient-user");
        var explicitContext = UserContext("export-job");
        accessor.RequestContext = ambient;

        var items = new List<(int Value, IRequestContext? Ambient)>();
        await foreach (var item in encina.Stream(new Numbers(4), explicitContext))
        {
            items.Add(item.ShouldBeSuccess());

            // The consumer's own ambient context is untouched between items.
            accessor.RequestContext.ShouldBeSameAs(ambient);
        }

        contexts.Seen.ShouldHaveSingleItem().ShouldBeSameAs(explicitContext);
        items.Select(i => i.Value).ShouldBe([0, 1, 2, 3]);
        items.ShouldAllBe(item => ReferenceEquals(item.Ambient, explicitContext));
        accessor.RequestContext.ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Stream_ConcurrentStreamsWithDifferentContexts_NeverSeeEachOthersContext()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(async i =>
        {
            var context = UserContext($"stream-{i}");
            var items = await CollectAsync(encina.Stream(new Numbers(3), context));
            return (Expected: context, Items: items);
        }));

        foreach (var (expected, items) in results)
        {
            items.ShouldAllBe(item => ReferenceEquals(item.Ambient, expected));
        }
    }

    [Fact]
    public async Task Stream_ExplicitNullContext_Throws()
    {
        await using var provider = BuildProvider();
        var encina = provider.GetRequiredService<IEncina>();

        Should.Throw<ArgumentNullException>(() => encina.Stream(new Numbers(1), null!))
            .ParamName.ShouldBe("context");
    }

    private static async Task<List<(int Value, IRequestContext? Ambient)>> CollectAsync(
        IAsyncEnumerable<Either<EncinaError, (int Value, IRequestContext? Ambient)>> stream)
    {
        var items = new List<(int Value, IRequestContext? Ambient)>();
        await foreach (var item in stream)
        {
            items.Add(item.ShouldBeSuccess());
        }

        return items;
    }
}
