using Encina.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Order key, source precedence, duplicate collapsing and caching of <see cref="PipelineOrderResolver"/>.
/// </summary>
public sealed class PipelineOrderResolverTests
{
    private static readonly Type Key = typeof(IPipelineBehavior<Req, string>);

    private readonly PipelineStageRegistry _registry = new();

    private PipelineOrderResolver CreateResolver()
        => new(_registry, NullLogger<PipelineOrderResolver>.Instance);

    private static IEnumerable<Type> Ordered(PipelineOrderResolver resolver, params object[] steps)
        => OrderedOf(resolver, Candidates(steps), steps);

    private static IEnumerable<Type> OrderedOf(PipelineOrderResolver resolver, PipelineStepCandidate[] candidates, object[] steps)
        => resolver.Resolve(Key, candidates).Select(index => PipelineStepCandidate.For(steps[index], index).StepType);

    private static PipelineStepCandidate[] Candidates(object[] steps)
        => [.. steps.Select(PipelineStepCandidate.For)];

    #region Order key

    [Fact]
    public void Resolve_SortsByStage()
    {
        var result = Ordered(CreateResolver(), new HandlerPrep(), new Validation(), new Authn(), new Observe());

        result.ShouldBe([typeof(Observe), typeof(Authn), typeof(Validation), typeof(HandlerPrep)]);
    }

    [Fact]
    public void Resolve_SortsByOrderInsideAStage()
    {
        var result = Ordered(CreateResolver(), new AuthnLate(), new AuthnEarly(), new Authn());

        result.ShouldBe([typeof(AuthnEarly), typeof(Authn), typeof(AuthnLate)]);
    }

    [Fact]
    public void Resolve_NegativeOrderRunsBeforeTheDefaultOrder()
    {
        var result = Ordered(CreateResolver(), new Authn(), new AuthnEarly());

        result.First().ShouldBe(typeof(AuthnEarly));
    }

    [Fact]
    public void Resolve_BehaviorsWithSameStageAndOrder_KeepRegistrationOrder()
    {
        var result = Ordered(CreateResolver(), new TwinB(), new TwinA());

        result.ShouldBe([typeof(TwinB), typeof(TwinA)]);
    }

    [Fact]
    public void Resolve_ProcessorBeforeBehaviorAtEqualStageAndOrder()
    {
        var behavior = new AppAtMinus100();
        var processor = new PreProcessorStep<Req, string>(new PreA());

        var result = Ordered(CreateResolver(), behavior, processor);

        result.ShouldBe([typeof(PreA), typeof(AppAtMinus100)]);
    }

    [Fact]
    public void Resolve_ProcessorsAtEqualPosition_AreOrderedByFullNameNotRegistration()
    {
        var first = new PreProcessorStep<Req, string>(new PreB());
        var second = new PreProcessorStep<Req, string>(new PreA());

        var result = Ordered(CreateResolver(), first, second);

        result.ShouldBe([typeof(PreA), typeof(PreB)]);
    }

    [Fact]
    public void Resolve_UndeclaredBehaviorRunsInApplicationInsideTheGates()
    {
        var result = Ordered(CreateResolver(), new Undeclared(), new Authn(), new HandlerPrep());

        result.ShouldBe([typeof(Authn), typeof(Undeclared), typeof(HandlerPrep)]);
    }

    [Fact]
    public void Resolve_DefaultPositions_PreAndPostAtApplicationMinus100_PostCommitInTransaction()
    {
        var resolver = CreateResolver();

        var pre = resolver.ResolvePosition(PipelineStepCandidate.For(new PreProcessorStep<Req, string>(new PreA()), 0));
        var post = resolver.ResolvePosition(PipelineStepCandidate.For(new PostProcessorStep<Req, string>(new PostA()), 0));
        var error = resolver.ResolvePosition(PipelineStepCandidate.For(new ErrorProcessorStep<Req, string>(new ErrorA()), 0));
        var postCommit = resolver.ResolvePosition(PipelineStepCandidate.For(new PostCommitProcessorStep<Req, string>(new PostCommitA()), 0));
        var behavior = resolver.ResolvePosition(PipelineStepCandidate.For(new Undeclared(), 0));

        pre.ShouldBe(new ResolvedPosition(PipelineStage.Application, -100, PipelineStepSource.Default));
        post.ShouldBe(new ResolvedPosition(PipelineStage.Application, -100, PipelineStepSource.Default));
        error.ShouldBe(new ResolvedPosition(PipelineStage.Application, -100, PipelineStepSource.Default));
        postCommit.ShouldBe(new ResolvedPosition(PipelineStage.Transaction, -100, PipelineStepSource.Default));
        behavior.ShouldBe(new ResolvedPosition(PipelineStage.Application, 0, PipelineStepSource.Default));
    }

    [Fact]
    public void Resolve_DeclaredProcessorStage_PlacesTheProcessorAmongTheGates()
    {
        var result = Ordered(CreateResolver(), new Validation(), new PreProcessorStep<Req, string>(new GatePre()), new Authn());

        result.ShouldBe([typeof(Authn), typeof(GatePre), typeof(Validation)]);
    }

    #endregion

    #region Permutation invariance

    [Fact]
    public void Resolve_EveryRegistrationPermutation_GivesTheSamePipeline()
    {
        object[] steps = [new Observe(), new Authn(), new AuthnLate(), new Validation(), new Undeclared(), new HandlerPrep()];
        Type[] expected = [typeof(Observe), typeof(Authn), typeof(AuthnLate), typeof(Validation), typeof(Undeclared), typeof(HandlerPrep)];

        var count = 0;
        foreach (var permutation in Permutations(steps))
        {
            Ordered(CreateResolver(), [.. permutation]).ShouldBe(expected);
            count++;
        }

        count.ShouldBe(720);
    }

    private static IEnumerable<object[]> Permutations(object[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }

        for (var i = 0; i < items.Length; i++)
        {
            var rest = items.Where((_, index) => index != i).ToArray();
            foreach (var tail in Permutations(rest))
            {
                yield return [items[i], .. tail];
            }
        }
    }

    #endregion

    #region Source precedence

    [Fact]
    public void ResolvePosition_RegistryBeatsAttribute()
    {
        _registry.Set(typeof(Authn), PipelineStage.Validation, 5);

        var position = CreateResolver().ResolvePosition(PipelineStepCandidate.For(new Authn(), 0));

        position.ShouldBe(new ResolvedPosition(PipelineStage.Validation, 5, PipelineStepSource.Registration));
    }

    [Fact]
    public void ResolvePosition_AttributeBeatsDefault()
    {
        var position = CreateResolver().ResolvePosition(PipelineStepCandidate.For(new AuthnLate(), 0));

        position.ShouldBe(new ResolvedPosition(PipelineStage.Authentication, 100, PipelineStepSource.Attribute));
    }

    [Fact]
    public void ResolvePosition_RegistryAppliesToAProcessorType()
    {
        _registry.Set(typeof(PreA), PipelineStage.Authorization, -1);

        var position = CreateResolver().ResolvePosition(PipelineStepCandidate.For(new PreProcessorStep<Req, string>(new PreA()), 0));

        position.ShouldBe(new ResolvedPosition(PipelineStage.Authorization, -1, PipelineStepSource.Registration));
    }

    [Fact]
    public void ResolvePosition_RegistryOfAnOpenGenericCoversItsClosedTypes()
    {
        _registry.Set(typeof(GenericBehavior<,>), PipelineStage.Caching, 3);

        var position = CreateResolver().ResolvePosition(PipelineStepCandidate.For(new GenericBehavior<Req, string>(), 0));

        position.ShouldBe(new ResolvedPosition(PipelineStage.Caching, 3, PipelineStepSource.Registration));
    }

    [Fact]
    public void ResolvePosition_ModuleBehaviorWithoutStage_IsApplication_AndWithStage_UsesTheWrappedAttribute()
    {
        var resolver = CreateResolver();
        var plain = PipelineStepCandidate.For(Adapter(new PlainModuleBehavior()), 0);
        var staged = PipelineStepCandidate.For(Adapter(new StagedModuleBehavior()), 0);

        plain.Kind.ShouldBe(PipelineStepKind.ModuleBehavior);
        plain.StepType.ShouldBe(typeof(PlainModuleBehavior));
        resolver.ResolvePosition(plain).ShouldBe(new ResolvedPosition(PipelineStage.Application, 0, PipelineStepSource.Default));
        resolver.ResolvePosition(staged).ShouldBe(new ResolvedPosition(PipelineStage.Authorization, 7, PipelineStepSource.Attribute));
    }

    #endregion

    #region Duplicates

    [Fact]
    public void Resolve_SameBehaviorTypeTwice_KeepsTheFirstOccurrence()
    {
        var resolver = CreateResolver();
        object[] steps = [new Authn(), new Observe(), new Authn()];

        var indexes = resolver.Resolve(Key, Candidates(steps));

        indexes.ShouldBe([1, 0]);
    }

    [Fact]
    public void Resolve_TwoModuleBehaviorsAdaptedForTheSameModule_AreNotCollapsed()
    {
        object[] steps = [Adapter(new PlainModuleBehavior()), Adapter(new StagedModuleBehavior())];

        CreateResolver().Resolve(Key, Candidates(steps)).Length.ShouldBe(2);
    }

    [Fact]
    public void Resolve_OneProcessorTypeImplementingTwoKinds_IsNotCollapsed()
    {
        var both = new PreAndPost();
        object[] steps = [new PreProcessorStep<Req, string>(both), new PostProcessorStep<Req, string>(both)];

        CreateResolver().Resolve(Key, Candidates(steps)).Length.ShouldBe(2);
    }

    #endregion

    #region Cache

    [Fact]
    public void Resolve_SameSequence_ReturnsTheCachedPermutation()
    {
        var resolver = CreateResolver();

        var first = resolver.Resolve(Key, Candidates([new Authn(), new Observe()]));
        var second = resolver.Resolve(Key, Candidates([new Authn(), new Observe()]));

        second.ShouldBeSameAs(first);
        resolver.Computations.ShouldBe(1);
    }

    [Fact]
    public void Resolve_DifferentSequence_RecomputesThePermutation()
    {
        var resolver = CreateResolver();

        var first = resolver.Resolve(Key, Candidates([new Authn(), new Observe()]));
        var second = resolver.Resolve(Key, Candidates([new Authn(), new Observe(), new Validation()]));

        second.ShouldNotBeSameAs(first);
        second.Length.ShouldBe(3);
        resolver.Computations.ShouldBe(2);
    }

    [Fact]
    public void Resolve_DifferentPipelineKeys_AreCachedIndependently()
    {
        var resolver = CreateResolver();
        var candidates = Candidates([new Authn(), new Observe()]);

        var a = resolver.Resolve(typeof(IPipelineBehavior<Req, string>), candidates);
        var b = resolver.Resolve(typeof(IPipelineBehavior<OtherReq, string>), candidates);

        resolver.Computations.ShouldBe(2);
        b.ShouldBe(a);
    }

    [Fact]
    public async Task Resolve_ConcurrentFirstBuild_ComputesOnePermutation()
    {
        var resolver = CreateResolver();
        object[] steps = [new Authn(), new Observe(), new Validation()];
        using var gate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            var candidates = Candidates(steps);
            gate.Wait();
            return resolver.Resolve(Key, candidates);
        })).ToArray();
        gate.Set();
        var results = await Task.WhenAll(tasks);

        resolver.Computations.ShouldBe(1);
        results.ShouldAllBe(result => ReferenceEquals(result, results[0]));
    }

    #endregion

    #region Guards

    [Fact]
    public void Resolve_NullArguments_Throw()
    {
        var resolver = CreateResolver();

        Should.Throw<ArgumentNullException>(() => resolver.Resolve(null!, [])).ParamName.ShouldBe("pipelineKey");
        Should.Throw<ArgumentNullException>(() => resolver.Resolve(Key, null!)).ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineOrderResolver(null!, NullLogger<PipelineOrderResolver>.Instance));
        Should.Throw<ArgumentNullException>(() => new PipelineOrderResolver(_registry, null!));
        CreateResolver().Logger.ShouldBeOfType<NullLogger<PipelineOrderResolver>>();
    }

    #endregion

    #region Test types

    private sealed record Req : IRequest<string>;

    private sealed record OtherReq : IRequest<string>;

    private sealed class FakeModule : IModule
    {
        public string Name => "Fake";

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private static ModuleBehaviorAdapter<FakeModule, Req, string> Adapter(IModulePipelineBehavior<FakeModule, Req, string> inner)
        => new(inner, new FakeModule(), NullModuleHandlerRegistry.Instance);

    private abstract class BehaviorBase : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    [PipelineStage(PipelineStage.Observability)]
    private sealed class Observe : BehaviorBase;

    [PipelineStage(PipelineStage.Authentication)]
    private sealed class Authn : BehaviorBase;

    [PipelineStage(PipelineStage.Authentication, Order = -10)]
    private sealed class AuthnEarly : BehaviorBase;

    [PipelineStage(PipelineStage.Authentication, Order = 100)]
    private sealed class AuthnLate : BehaviorBase;

    [PipelineStage(PipelineStage.Validation)]
    private sealed class Validation : BehaviorBase;

    [PipelineStage(PipelineStage.HandlerPreparation)]
    private sealed class HandlerPrep : BehaviorBase;

    [PipelineStage(PipelineStage.Application, Order = -100)]
    private sealed class AppAtMinus100 : BehaviorBase;

    [PipelineStage(PipelineStage.Caching)]
    private sealed class TwinA : BehaviorBase;

    [PipelineStage(PipelineStage.Caching)]
    private sealed class TwinB : BehaviorBase;

    private sealed class Undeclared : BehaviorBase;

    private sealed class GenericBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private abstract class ModuleBehaviorBase : IModulePipelineBehavior<FakeModule, Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class PlainModuleBehavior : ModuleBehaviorBase;

    [PipelineStage(PipelineStage.Authorization, Order = 7)]
    private sealed class StagedModuleBehavior : ModuleBehaviorBase;

    private class PreA : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class PreB : PreA;

    [PipelineStage(PipelineStage.Authentication, Order = 5)]
    private sealed class GatePre : PreA;

    private sealed class PostA : IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class ErrorA : IRequestErrorProcessor<Req, string>
    {
        public ValueTask Process(Req request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class PostCommitA : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class PreAndPost : IRequestPreProcessor<Req>, IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);

        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    #endregion
}
