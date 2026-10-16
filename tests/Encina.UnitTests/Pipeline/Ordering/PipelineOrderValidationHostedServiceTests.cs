using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Startup validation: <see cref="PipelineOrderValidationHostedService"/> and <see cref="PipelineRegistrationSnapshot"/>.
/// </summary>
public sealed class PipelineOrderValidationHostedServiceTests
{
    private readonly PipelineStageRegistry _registry = new();

    private sealed record Harness(
        PipelineOrderValidationHostedService Service,
        PipelineOrderResolver Resolver,
        ServiceProvider Provider) : IDisposable
    {
        public void Dispose() => Provider.Dispose();
    }

    private Harness Create(Action<IServiceCollection> configure, PipelineOrderRuleSet rules = PipelineOrderRuleSet.All)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_registry);
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler());
        configure(services);
        var snapshot = new PipelineRegistrationSnapshot(services);
        var resolver = new PipelineOrderResolver(_registry, NullLogger<PipelineOrderResolver>.Instance, snapshot, rules);
        var provider = services.BuildServiceProvider();
        return new Harness(new PipelineOrderValidationHostedService(provider, snapshot, resolver), resolver, provider);
    }

    #region StartAsync

    [Fact]
    public async Task StartAsync_ValidPipelines_CompletesAndWarmsTheCache()
    {
        using var harness = Create(services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
            services.AddScoped<IRequestPreProcessor<Req>, Pre>();
        });

        await harness.Service.StartAsync(CancellationToken.None);

        harness.Resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_ThenRuntimeBuild_IsACacheHit()
    {
        using var harness = Create(services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
            services.AddScoped<IRequestPreProcessor<Req>, Pre>();
        });
        await harness.Service.StartAsync(CancellationToken.None);
        using var scope = harness.Provider.CreateScope();
        var builder = new PipelineBuilder<Req, string>(
            new Req(), new Handler(), RequestContext.Create(), harness.Resolver, CancellationToken.None);

        builder.Build(scope.ServiceProvider);

        harness.Resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_NoHandlers_Completes()
    {
        var resolver = new PipelineOrderResolver(_registry, NullLogger<PipelineOrderResolver>.Instance);
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var service = new PipelineOrderValidationHostedService(provider, new PipelineRegistrationSnapshot(services), resolver);

        await service.StartAsync(CancellationToken.None);

        resolver.Computations.ShouldBe(0);
    }

    [Fact]
    public async Task StartAsync_StageBreakingR1_FailsNamingTheRuleThePipelineAndTheStep()
    {
        _registry.Set(typeof(Gate), (PipelineStage)7, 0);
        using var harness = Create(services => services.AddScoped<IPipelineBehavior<Req, string>, Gate>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R1");
        exception.Message.ShouldContain("Gate");
        exception.Message.ShouldContain("IPipelineBehavior<Req, String>");
    }

    [Fact]
    public async Task StartAsync_BuiltInOnTheDefaultStage_FailsR3()
    {
        using var harness = Create(services => services.AddScoped(typeof(IPipelineBehavior<Req, string>), typeof(EncinaMetrics)));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R3");
        exception.Message.ShouldContain("EncinaMetrics");
    }

    [Fact]
    public async Task StartAsync_DeadRegistration_FailsR4()
    {
        using var harness = Create(services => services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(DeadBehavior<,>)));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R4");
        exception.Message.ShouldContain("DeadBehavior");
    }

    [Fact]
    public async Task StartAsync_TwoBuiltInsAtOnePosition_FailsR5()
    {
        _registry.Set(typeof(EncinaMetrics), PipelineStage.Transaction, 0);
        _registry.Set(typeof(PipelineOrderResolver), PipelineStage.Transaction, 0);
        using var harness = Create(services =>
        {
            services.AddScoped(typeof(IPipelineBehavior<Req, string>), typeof(EncinaMetrics));
            services.AddScoped(typeof(IPipelineBehavior<Req, string>), typeof(PipelineOrderResolver));
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R5");
    }

    [Fact]
    public async Task StartAsync_ConflictingOverrides_FailsR6BeforeAnyPipeline()
    {
        _registry.Set(typeof(Gate), PipelineStage.Audit, 0);
        _registry.Set(typeof(Gate), PipelineStage.Caching, 0);
        using var harness = Create(_ => { });

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R6");
        harness.Resolver.Computations.ShouldBe(0);
    }

    [Fact]
    public async Task StartAsync_PostCommitProcessorInsideTransaction_FailsR7()
    {
        _registry.Set(typeof(PostCommit), PipelineStage.Application, 0);
        using var harness = Create(services => services.AddScoped<IRequestPostCommitProcessor<Req, string>, PostCommit>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R7");
        exception.Message.ShouldContain("PostCommit");
    }

    [Fact]
    public async Task StartAsync_PostCommitProcessorWithTheDefaultPosition_AndATransactionBehavior_Passes()
    {
        _registry.Set(typeof(Gate), PipelineStage.Transaction, 0);
        using var harness = Create(services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
            services.AddScoped<IRequestPostCommitProcessor<Req, string>, PostCommit>();
        });

        await harness.Service.StartAsync(CancellationToken.None);

        harness.Resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_FactoryThatThrows_IsAResolutionErrorNamingTheServiceType_NotAnOrderRule()
    {
        using var harness = Create(services =>
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => throw new ArgumentException("secret detail")));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("resolution failed");
        exception.Message.ShouldContain("IPipelineBehavior<Req, String>");
        exception.Message.ShouldContain("ArgumentException");
        exception.Message.ShouldNotContain("Pipeline order rule");
        exception.Message.ShouldNotContain("secret detail");
        exception.InnerException.ShouldBeNull("the factory's own message must not reach the host log");
    }

    [Fact]
    public async Task StartAsync_FactoryRegisteredSteps_AreInstantiatedToLearnTheirIdentity()
    {
        _registry.Set(typeof(Gate), PipelineStage.Authorization, 0);
        using var harness = Create(services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Gate());
            services.AddScoped<IRequestPreProcessor<Req>>(_ => new Pre());
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new PreProcessorStep<Req, string>(new Pre()));
        });

        await harness.Service.StartAsync(CancellationToken.None);

        harness.Resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_TransientFactoryResult_IsDisposed_Synchronously()
    {
        var probe = new DisposeProbe();
        using var harness = Create(services =>
            services.AddTransient<IPipelineBehavior<Req, string>>(_ => new DisposableGate(probe)));

        await harness.Service.StartAsync(CancellationToken.None);

        probe.Disposed.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_TransientFactoryResult_IsDisposed_Asynchronously()
    {
        var probe = new DisposeProbe();
        using var harness = Create(services =>
            services.AddTransient<IPipelineBehavior<Req, string>>(_ => new AsyncDisposableGate(probe)));

        await harness.Service.StartAsync(CancellationToken.None);

        probe.AsyncDisposed.ShouldBe(1);
        probe.Disposed.ShouldBe(0);
    }

    // A singleton belongs to the container until the provider is disposed; a scoped instance is disposed exactly once,
    // by the validation scope that owns it, never a second time by the service
    [Theory]
    [InlineData(ServiceLifetime.Singleton, 0)]
    [InlineData(ServiceLifetime.Scoped, 1)]
    public async Task StartAsync_AliasFactoryToAContainerOwnedInstance_IsNotDisposedByTheService(ServiceLifetime lifetime, int expectedDisposals)
    {
        var probe = new DisposeProbe();
        using var harness = Create(services =>
        {
            Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Add(
                services, new ServiceDescriptor(typeof(DisposableGate), _ => new DisposableGate(probe), lifetime));
            Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Add(
                services, new ServiceDescriptor(typeof(IPipelineBehavior<Req, string>), sp => sp.GetRequiredService<DisposableGate>(), lifetime));
        });

        await harness.Service.StartAsync(CancellationToken.None);

        probe.Disposed.ShouldBe(expectedDisposals);
    }

    [Fact]
    public async Task StartAsync_OpenGenericBehavior_IsClosedOverTheHandlerArgumentsAndConstraintViolationsAreSkipped()
    {
        _registry.Set(typeof(Generic<,>), (PipelineStage)7, 0);
        using var closed = Create(services => services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Generic<,>)));
        using var skipped = Create(services => services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Constrained<,>)));

        await Should.ThrowAsync<InvalidOperationException>(() => closed.Service.StartAsync(CancellationToken.None));
        await skipped.Service.StartAsync(CancellationToken.None);
        skipped.Resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_StreamHandler_ValidatesTheStreamPipeline()
    {
        _registry.Set(typeof(StreamGate), (PipelineStage)7, 0);
        using var harness = Create(services =>
        {
            services.AddScoped<IStreamRequestHandler<StreamReq, int>, StreamHandler>();
            services.AddScoped<IStreamPipelineBehavior<StreamReq, int>, StreamGate>();
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => harness.Service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("IStreamPipelineBehavior<StreamReq, Int32>");
    }

    [Fact]
    public async Task StartAsync_ValidStreamPipeline_CompletesAndWarmsTheCache()
    {
        using var harness = Create(services =>
        {
            services.AddScoped<IStreamRequestHandler<StreamReq, int>, StreamHandler>();
            services.AddScoped<IStreamPipelineBehavior<StreamReq, int>, StreamGate>();
        });

        await harness.Service.StartAsync(CancellationToken.None);

        harness.Resolver.Computations.ShouldBe(2);
    }

    [Fact]
    public async Task StartAsync_OpenGenericHandlers_AreSkipped()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddScoped(typeof(IRequestHandler<,>), typeof(OpenHandler<,>));
        var resolver = new PipelineOrderResolver(_registry, NullLogger<PipelineOrderResolver>.Instance);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new PipelineOrderValidationHostedService(provider, new PipelineRegistrationSnapshot(services), resolver);

        await service.StartAsync(CancellationToken.None);

        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task StartAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var harness = Create(_ => { });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => harness.Service.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_Completes()
    {
        using var harness = Create(_ => { });

        await harness.Service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var snapshot = new PipelineRegistrationSnapshot(new ServiceCollection());
        var resolver = new PipelineOrderResolver(_registry, NullLogger<PipelineOrderResolver>.Instance);
        using var provider = new ServiceCollection().BuildServiceProvider();

        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(null!, snapshot, resolver)).ParamName.ShouldBe("serviceProvider");
        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(provider, null!, resolver)).ParamName.ShouldBe("snapshot");
        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(provider, snapshot, null!)).ParamName.ShouldBe("resolver");
    }

    #endregion

    #region Snapshot

    [Fact]
    public void Snapshot_ClosedHandlerServiceTypes_SkipsOpenKeyedAndDuplicates()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddKeyedScoped<IRequestHandler<OtherReq, string>, Handler>("key");
        services.AddScoped(typeof(IRequestHandler<,>), typeof(OpenHandler<,>));
        services.AddScoped<IStreamRequestHandler<StreamReq, int>, StreamHandler>();
        services.AddScoped<IPipelineBehavior<Req, string>, Gate>();

        var result = new PipelineRegistrationSnapshot(services).GetClosedHandlerServiceTypes();

        result.ShouldBe([typeof(IRequestHandler<Req, string>), typeof(IStreamRequestHandler<StreamReq, int>)]);
    }

    [Fact]
    public void Snapshot_Registrations_IncludeExactOpenClosableInstanceAndFactoryDescriptors()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Generic<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Constrained<,>));
        services.AddSingleton<IPipelineBehavior<Req, string>>(new OtherGate());
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Gate());
        services.AddKeyedScoped<IPipelineBehavior<Req, string>, Gate>("key");
        services.AddScoped(typeof(IPipelineBehavior<OtherReq, string>), typeof(Gate));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Gate));

        var result = new PipelineRegistrationSnapshot(services).GetRegistrations(typeof(IPipelineBehavior<Req, string>));

        result.Select(registration => registration.ImplementationType).ShouldBe(
            [typeof(Gate), typeof(Generic<Req, string>), typeof(OtherGate), null]);
    }

    [Fact]
    public void Snapshot_Guards()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineRegistrationSnapshot(null!)).ParamName.ShouldBe("services");
        Should.Throw<ArgumentNullException>(() => new PipelineRegistrationSnapshot(new ServiceCollection()).GetRegistrations(null!))
            .ParamName.ShouldBe("closedServiceType");
    }

    [Fact]
    public void Snapshot_DeadRegistrations_ListsOpenAndClosedCommandAndQueryServiceTypes()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(DeadBehavior<,>));
        services.AddTransient<IQueryPipelineBehavior<Query, string>, DeadQuery>();
        services.AddTransient<IPipelineBehavior<Req, string>, Gate>();
        services.AddTransient<IRequestHandler<Req, string>, Handler>();

        var result = new PipelineRegistrationSnapshot(services).GetDeadPipelineRegistrations();

        result.Select(descriptor => descriptor.ServiceType).ShouldBe(
            [typeof(ICommandPipelineBehavior<,>), typeof(IQueryPipelineBehavior<Query, string>)]);
    }

    #endregion

    #region Types

    private sealed record Req : IRequest<string>;

    private sealed record OtherReq : IRequest<string>;

    private sealed record Query : IQuery<string>;

    private sealed record StreamReq : IStreamRequest<int>;

    private sealed class Handler : IRequestHandler<Req, string>, IRequestHandler<OtherReq, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, string>.Right("handled"));

        public Task<Either<EncinaError, string>> Handle(OtherReq request, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, string>.Right("handled"));
    }

    // Abstract so that assembly scanning of this test assembly never registers it; only descriptors name it
    private abstract class OpenHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public abstract Task<Either<EncinaError, TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
    }

    private sealed class StreamHandler : IStreamRequestHandler<StreamReq, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamReq request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private class Gate : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class OtherGate : Gate;

    private sealed class DisposeProbe
    {
        public int Disposed { get; set; }

        public int AsyncDisposed { get; set; }
    }

    private sealed class DisposableGate(DisposeProbe probe) : Gate, IDisposable
    {
        public void Dispose() => probe.Disposed++;
    }

    private sealed class AsyncDisposableGate(DisposeProbe probe) : Gate, IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            probe.AsyncDisposed++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StreamGate : IStreamPipelineBehavior<StreamReq, int>
    {
        public IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamReq request, IRequestContext context, StreamHandlerCallback<int> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class Generic<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class Constrained<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>, IDisposable
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class Pre : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class PostCommit : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class DeadBehavior<TRequest, TResponse>;

    private sealed class DeadQuery : IQueryPipelineBehavior<Query, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Query request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    #endregion
}
