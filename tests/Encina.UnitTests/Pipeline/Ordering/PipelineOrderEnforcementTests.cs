using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// The lazy fail-closed path: the resolver validates every new closed pipeline before caching it, so an application
/// without a host fails on the first request, before any step or handler runs.
/// </summary>
public sealed class PipelineOrderEnforcementTests
{
    private static readonly Type Key = typeof(IPipelineBehavior<Req, string>);

    private readonly PipelineStageRegistry _registry = new();

    private PipelineOrderResolver CreateResolver(
        PipelineOrderRuleSet rules = PipelineOrderRuleSet.Staged,
        PipelineRegistrationSnapshot? snapshot = null)
        => new(_registry, NullLogger<PipelineOrderResolver>.Instance, snapshot, rules);

    #region Resolver

    [Fact]
    public void Resolve_PipelineBreakingARule_ThrowsOnEveryBuildAndIsNeverCached()
    {
        _registry.Set(typeof(Custom), (PipelineStage)7, 0);
        var resolver = CreateResolver();

        Should.Throw<InvalidOperationException>(() => resolver.Resolve(Key, [new Custom()])).Message.ShouldContain("R1");
        Should.Throw<InvalidOperationException>(() => resolver.Resolve(Key, [new Custom()])).Message.ShouldContain("R1");

        resolver.Computations.ShouldBe(0);
    }

    [Fact]
    public void Resolve_ValidPipeline_IsValidatedAndComputedOnce()
    {
        var resolver = CreateResolver(PipelineOrderRuleSet.All);

        resolver.Resolve(Key, [new Custom()]);
        resolver.Resolve(Key, [new Custom()]);

        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task Resolve_ConcurrentFirstBuilds_ValidateOnce()
    {
        var resolver = CreateResolver(PipelineOrderRuleSet.All);

        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() => resolver.Resolve(Key, [new Custom(), new OtherCustom()]))));

        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public async Task Resolve_ConcurrentFirstBuilds_OfAnInvalidPipeline_AllFail()
    {
        _registry.Set(typeof(Custom), (PipelineStage)7, 0);
        var resolver = CreateResolver();

        var attempts = Enumerable.Range(0, 32).Select(_ => Task.Run(() => resolver.Resolve(Key, [new Custom()])));

        foreach (var attempt in attempts)
        {
            await Should.ThrowAsync<InvalidOperationException>(attempt);
        }

        resolver.Computations.ShouldBe(0);
    }

    [Fact]
    public void Warm_BuiltInOnTheDefault_FailsR3_WhenEnforced()
    {
        var resolver = CreateResolver(PipelineOrderRuleSet.All);

        var exception = Should.Throw<InvalidOperationException>(() => resolver.Warm(Key, [BuiltIn(typeof(EncinaMetrics), 0)]));

        exception.Message.ShouldContain("R3");
        exception.Message.ShouldContain("EncinaMetrics");
    }

    [Fact]
    public void Warm_BuiltInOnTheDefault_Passes_WhenR3IsNotEnforced()
    {
        var resolver = CreateResolver();

        Should.NotThrow(() => resolver.Warm(Key, [BuiltIn(typeof(EncinaMetrics), 0)]));
        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public void Warm_BuiltInWithARegistryStage_PassesR3()
    {
        _registry.Set(typeof(EncinaMetrics), PipelineStage.Observability, 0);
        var resolver = CreateResolver(PipelineOrderRuleSet.All);

        Should.NotThrow(() => resolver.Warm(Key, [BuiltIn(typeof(EncinaMetrics), 0)]));
    }

    [Fact]
    public void Warm_TwoBuiltInsAtOnePosition_FailsR5()
    {
        _registry.Set(typeof(EncinaMetrics), PipelineStage.Transaction, 0);
        _registry.Set(typeof(PipelineOrderResolver), PipelineStage.Transaction, 0);
        var resolver = CreateResolver();

        var exception = Should.Throw<InvalidOperationException>(
            () => resolver.Warm(Key, [BuiltIn(typeof(EncinaMetrics), 0), BuiltIn(typeof(PipelineOrderResolver), 1)]));

        exception.Message.ShouldContain("R5");
    }

    [Fact]
    public void Warm_PostCommitProcessorInsideTransaction_FailsR7()
    {
        _registry.Set(typeof(Custom), PipelineStage.Application, 0);
        var resolver = CreateResolver();

        var exception = Should.Throw<InvalidOperationException>(() => resolver.Warm(
            Key, [new PipelineStepCandidate(typeof(Custom), null, PipelineStepKind.PostCommitProcessor, 0)]));

        exception.Message.ShouldContain("R7");
    }

    [Fact]
    public void Warm_SameSequenceTwice_IsComputedOnce()
    {
        var resolver = CreateResolver();
        var candidates = new[] { new PipelineStepCandidate(typeof(Custom), null, PipelineStepKind.Behavior, 0) };

        resolver.Warm(Key, candidates);
        resolver.Warm(Key, candidates);

        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public void Warm_DifferentSequence_Recomputes()
    {
        var resolver = CreateResolver();

        resolver.Warm(Key, [new PipelineStepCandidate(typeof(Custom), null, PipelineStepKind.Behavior, 0)]);
        resolver.Warm(Key, [new PipelineStepCandidate(typeof(OtherCustom), null, PipelineStepKind.Behavior, 0)]);
        resolver.Warm(Key, []);

        resolver.Computations.ShouldBe(3);
    }

    [Fact]
    public void Resolve_SameLengthDifferentStepTypes_ValidatesAndComputesAgain()
    {
        var resolver = CreateResolver();

        resolver.Resolve(Key, [new Custom()]);
        resolver.Resolve(Key, [new OtherCustom()]);

        resolver.Computations.ShouldBe(2);
    }

    [Fact]
    public void Resolve_AfterWarmWithTheSameStepTypes_IsACacheHit()
    {
        var resolver = CreateResolver();
        resolver.Warm(Key, [new PipelineStepCandidate(typeof(Custom), null, PipelineStepKind.Behavior, 0)]);

        resolver.Resolve(Key, [new Custom()]);

        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public void ValidateRegistrations_ConflictingOverrides_FailsR6OnEveryCallUntilNothingChanges()
    {
        _registry.Set(typeof(Custom), PipelineStage.Audit, 0);
        _registry.Set(typeof(Custom), PipelineStage.Caching, 0);
        var resolver = CreateResolver();

        Should.Throw<InvalidOperationException>(resolver.ValidateRegistrations).Message.ShouldContain("R6");
        Should.Throw<InvalidOperationException>(resolver.ValidateRegistrations).Message.ShouldContain("R6");
    }

    [Fact]
    public void ValidateRegistrations_DeadRegistrationWithSnapshot_FailsR4_OnlyWhenEnforced()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(DeadBehavior<,>));
        var snapshot = new PipelineRegistrationSnapshot(services);

        Should.Throw<InvalidOperationException>(CreateResolver(PipelineOrderRuleSet.All, snapshot).ValidateRegistrations)
            .Message.ShouldContain("R4");
        Should.NotThrow(CreateResolver(PipelineOrderRuleSet.Staged, snapshot).ValidateRegistrations);
        Should.NotThrow(CreateResolver(PipelineOrderRuleSet.All, snapshot: null).ValidateRegistrations);
    }

    [Fact]
    public void Resolve_FirstUseWithoutAHost_ChecksR4FromTheSnapshot()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IQueryPipelineBehavior<,>), typeof(DeadBehavior<,>));
        var resolver = CreateResolver(PipelineOrderRuleSet.All, new PipelineRegistrationSnapshot(services));

        Should.Throw<InvalidOperationException>(() => resolver.Resolve(Key, [new Custom()])).Message.ShouldContain("R4");
    }

    private static PipelineStepCandidate BuiltIn(Type type, int index)
        => new(type, null, PipelineStepKind.Behavior, index);

    #endregion

    [Fact]
    public void AddApplicationMessaging_RegistersTheResolverAndTheSnapshotLikeAddEncina()
    {
        var services = new ServiceCollection();

        services.AddApplicationMessaging((Action<EncinaConfiguration>?)null, typeof(IEncina).Assembly);

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(PipelineRegistrationSnapshot));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(PipelineOrderResolver));
    }

    #region Send (no host)

    [Fact]
    public async Task Send_PipelineBreakingR1_FailsBeforeAnyStepOrHandlerRuns()
    {
        var calls = new List<string>();
        await using var provider = BuildProvider(calls, registry => registry.Set(typeof(Recording), (PipelineStage)7, 0));

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None));

        exception.Message.ShouldContain("R1");
        exception.Message.ShouldContain("Recording");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_PostCommitProcessorInsideTransaction_FailsR7WithTheHandlerNeverCalled()
    {
        var calls = new List<string>();
        await using var provider = BuildProvider(calls, registry => registry.Set(typeof(PostCommit), PipelineStage.Application, 0));

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None));

        exception.Message.ShouldContain("R7");
        exception.Message.ShouldContain("PostCommit");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_ConflictingOverrides_FailsR6WithTheHandlerNeverCalled()
    {
        var calls = new List<string>();
        await using var provider = BuildProvider(calls, registry =>
        {
            registry.Set(typeof(Recording), PipelineStage.Audit, 0);
            registry.Set(typeof(Recording), PipelineStage.Caching, 0);
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None));

        exception.Message.ShouldContain("R6");
        calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_ValidPipeline_StillRuns()
    {
        var calls = new List<string>();
        await using var provider = BuildProvider(calls, registry => registry.Set(typeof(Recording), PipelineStage.Audit, 0));

        var result = await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["recording", "handler"]);
    }

    private static ServiceProvider BuildProvider(List<string> calls, Action<PipelineStageRegistry> configureRegistry)
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls));
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Recording(calls));
        services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new PostCommit());
        var provider = services.BuildServiceProvider();
        configureRegistry(provider.GetRequiredService<PipelineStageRegistry>());
        return provider;
    }

    #endregion

    private sealed record Req : IRequest<string>;

    private sealed class Custom;

    private sealed class OtherCustom;

    private sealed class DeadBehavior<TRequest, TResponse>;

    private sealed class Handler(List<string> calls) : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult(Either<EncinaError, string>.Right("handled"));
        }
    }

    private sealed class Recording(List<string> calls) : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            calls.Add("recording");
            return nextStep();
        }
    }

    private sealed class PostCommit : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }
}
