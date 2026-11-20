#pragma warning disable CA1859 // the steps are exercised through the contract interfaces on purpose
#pragma warning disable CA2012 // NSubstitute stubbing of ValueTask members

using Encina.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ContractTests.Core;

/// <summary>
/// Contract tests: the internal steps that adapt a processor or a module behavior into the pipeline honour
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/> (rail-oriented outcome, no thrown business failures)
/// and <see cref="IPipelineStepIdentity"/> (what the pipeline inspector reads).
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Pipeline")]
public sealed class AdaptedPipelineStepContractTests
{
    private static readonly IRequestContext Context = RequestContext.Create();

    private static ValueTask<Either<EncinaError, string>> Next(string value = "next")
        => ValueTask.FromResult<Either<EncinaError, string>>(value);

    private static ValueTask<Either<EncinaError, string>> NextFails()
        => ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("test.next_failed", "next failed"));

    private static string CodeOf(Either<EncinaError, string> result)
        => result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    private static string ValueOf(Either<EncinaError, string> result)
        => result.Match(Right: v => v, Left: _ => string.Empty);

    private static CancellationToken Cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    // ── PreProcessorStep ────────────────────────────────────────────────

    [Fact]
    public async Task PreProcessor_Right_ContinuesToTheNextStep()
    {
        var processor = Substitute.For<IRequestPreProcessor<Req>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        IPipelineBehavior<Req, string> step = new PreProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), CancellationToken.None);

        ValueOf(result).ShouldBe("next");
    }

    [Fact]
    public async Task PreProcessor_Left_AbortsWithoutRunningTheNextStep()
    {
        var processor = Substitute.For<IRequestPreProcessor<Req>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.pre_failed", "pre failed")));
        IPipelineBehavior<Req, string> step = new PreProcessorStep<Req, string>(processor);
        var nextCalls = 0;

        var result = await step.Handle(new Req(), Context, () => { nextCalls++; return Next(); }, CancellationToken.None);

        CodeOf(result).ShouldBe("test.pre_failed");
        nextCalls.ShouldBe(0);
    }

    [Fact]
    public async Task PreProcessor_CancelledProcessor_MapsToTheCancellationCodeWithoutThrowing()
    {
        var processor = Substitute.For<IRequestPreProcessor<Req>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException());
        IPipelineBehavior<Req, string> step = new PreProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), Cancelled());

        CodeOf(result).ShouldBe(EncinaErrorCodes.PreProcessorCancelled);
    }

    [Fact]
    public void PreProcessor_Identity_DescribesAPreProcessorOfTheProcessorType()
    {
        var processor = Substitute.For<IRequestPreProcessor<Req>>();

        AssertIdentity(new PreProcessorStep<Req, string>(processor), processor.GetType(), PipelineStepKind.PreProcessor);
    }

    // ── PostProcessorStep ───────────────────────────────────────────────

    [Fact]
    public async Task PostProcessor_Right_ReturnsTheOriginalResponse()
    {
        var processor = Substitute.For<IRequestPostProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        IPipelineBehavior<Req, string> step = new PostProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next("response"), CancellationToken.None);

        ValueOf(result).ShouldBe("response");
    }

    [Fact]
    public async Task PostProcessor_LeftFromTheProcessor_FailsTheRequest()
    {
        var processor = Substitute.For<IRequestPostProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.post_failed", "post failed")));
        IPipelineBehavior<Req, string> step = new PostProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), CancellationToken.None);

        CodeOf(result).ShouldBe("test.post_failed");
    }

    [Fact]
    public async Task PostProcessor_LeftFromTheNextStep_SkipsTheProcessor()
    {
        var processor = Substitute.For<IRequestPostProcessor<Req, string>>();
        IPipelineBehavior<Req, string> step = new PostProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => NextFails(), CancellationToken.None);

        CodeOf(result).ShouldBe("test.next_failed");
        processor.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task PostProcessor_CancelledProcessor_MapsToTheCancellationCodeWithoutThrowing()
    {
        var processor = Substitute.For<IRequestPostProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException());
        IPipelineBehavior<Req, string> step = new PostProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), Cancelled());

        CodeOf(result).ShouldBe(EncinaErrorCodes.PostProcessorCancelled);
    }

    [Fact]
    public void PostProcessor_Identity_DescribesAPostProcessorOfTheProcessorType()
    {
        var processor = Substitute.For<IRequestPostProcessor<Req, string>>();

        AssertIdentity(new PostProcessorStep<Req, string>(processor), processor.GetType(), PipelineStepKind.PostProcessor);
    }

    // ── PostCommitProcessorStep ─────────────────────────────────────────

    [Fact]
    public async Task PostCommitProcessor_Right_ReturnsTheOriginalResponse()
    {
        var processor = Substitute.For<IRequestPostCommitProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        IPipelineBehavior<Req, string> step = new PostCommitProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next("response"), CancellationToken.None);

        ValueOf(result).ShouldBe("response");
    }

    [Fact]
    public async Task PostCommitProcessor_LeftFromTheProcessor_FailsTheRequest()
    {
        var processor = Substitute.For<IRequestPostCommitProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(EncinaErrors.Create("test.commit_failed", "commit failed")));
        IPipelineBehavior<Req, string> step = new PostCommitProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), CancellationToken.None);

        CodeOf(result).ShouldBe("test.commit_failed");
    }

    [Fact]
    public async Task PostCommitProcessor_LeftFromTheNextStep_SkipsTheProcessor()
    {
        var processor = Substitute.For<IRequestPostCommitProcessor<Req, string>>();
        IPipelineBehavior<Req, string> step = new PostCommitProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => NextFails(), CancellationToken.None);

        CodeOf(result).ShouldBe("test.next_failed");
        processor.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task PostCommitProcessor_CancelledProcessor_MapsToTheCancellationCodeWithoutThrowing()
    {
        var processor = Substitute.For<IRequestPostCommitProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new OperationCanceledException());
        IPipelineBehavior<Req, string> step = new PostCommitProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next(), Cancelled());

        CodeOf(result).ShouldBe(EncinaErrorCodes.PostCommitProcessorCancelled);
    }

    [Fact]
    public void PostCommitProcessor_Identity_DescribesAPostCommitProcessorOfTheProcessorType()
    {
        var processor = Substitute.For<IRequestPostCommitProcessor<Req, string>>();

        AssertIdentity(new PostCommitProcessorStep<Req, string>(processor), processor.GetType(), PipelineStepKind.PostCommitProcessor);
    }

    // ── ErrorProcessorStep ──────────────────────────────────────────────

    [Fact]
    public async Task ErrorProcessor_Right_NeverObserves()
    {
        var processor = Substitute.For<IRequestErrorProcessor<Req, string>>();
        IPipelineBehavior<Req, string> step = new ErrorProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => Next("response"), CancellationToken.None);

        ValueOf(result).ShouldBe("response");
        processor.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ErrorProcessor_Left_ObservesTheFailureAndReturnsTheOriginalError()
    {
        var processor = Substitute.For<IRequestErrorProcessor<Req, string>>();
        IPipelineBehavior<Req, string> step = new ErrorProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => NextFails(), CancellationToken.None);

        CodeOf(result).ShouldBe("test.next_failed");
        await processor.Received(1).Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<EncinaError>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ErrorProcessor_CancelledObserver_DoesNotReplaceTheOriginalError()
    {
        var processor = Substitute.For<IRequestErrorProcessor<Req, string>>();
        processor.Process(Arg.Any<Req>(), Arg.Any<IRequestContext>(), Arg.Any<EncinaError>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new OperationCanceledException());
        IPipelineBehavior<Req, string> step = new ErrorProcessorStep<Req, string>(processor);

        var result = await step.Handle(new Req(), Context, () => NextFails(), Cancelled());

        CodeOf(result).ShouldBe("test.next_failed");
    }

    [Fact]
    public void ErrorProcessor_Identity_DescribesAnErrorProcessorOfTheProcessorType()
    {
        var processor = Substitute.For<IRequestErrorProcessor<Req, string>>();

        AssertIdentity(new ErrorProcessorStep<Req, string>(processor), processor.GetType(), PipelineStepKind.ErrorProcessor);
    }

    // ── ModuleBehaviorAdapter ───────────────────────────────────────────

    [Fact]
    public async Task ModuleAdapter_HandlerOfTheTargetModule_RunsTheWrappedBehavior()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        registry.BelongsToModule(typeof(IRequestHandler<Req, string>), "Orders").Returns(true);
        IPipelineBehavior<Req, string> adapter = new ModuleBehaviorAdapter<OrdersModule, Req, string>(new InnerBehavior(), new OrdersModule(), registry);

        var result = await adapter.Handle(new Req(), Context, () => Next(), CancellationToken.None);

        ValueOf(result).ShouldBe("inner");
    }

    [Fact]
    public async Task ModuleAdapter_HandlerOfAnotherModule_SkipsTheWrappedBehavior()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        registry.BelongsToModule(typeof(IRequestHandler<Req, string>), "Orders").Returns(false);
        IPipelineBehavior<Req, string> adapter = new ModuleBehaviorAdapter<OrdersModule, Req, string>(new InnerBehavior(), new OrdersModule(), registry);

        var result = await adapter.Handle(new Req(), Context, () => Next(), CancellationToken.None);

        ValueOf(result).ShouldBe("next");
    }

    [Fact]
    public void ModuleAdapter_Identity_DescribesAModuleBehaviorOfTheModuleType()
    {
        var registry = Substitute.For<IModuleHandlerRegistry>();
        var adapter = new ModuleBehaviorAdapter<OrdersModule, Req, string>(new InnerBehavior(), new OrdersModule(), registry);

        AssertIdentity(adapter, typeof(InnerBehavior), PipelineStepKind.ModuleBehavior, typeof(OrdersModule));
    }

    private static void AssertIdentity(IPipelineStepIdentity identity, Type stepType, PipelineStepKind kind, Type? moduleType = null)
    {
        identity.StepType.ShouldBe(stepType);
        identity.Kind.ShouldBe(kind);
        identity.ModuleType.ShouldBe(moduleType);
        identity.DeclaredStage.ShouldBeNull();
        identity.DeclaredOrder.ShouldBeNull();
    }

    // Public so the NSubstitute proxies of the processor interfaces can name it.
    public sealed record Req : IRequest<string>;

    private sealed class OrdersModule : IModule
    {
        public string Name => "Orders";

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    private sealed class InnerBehavior : IModulePipelineBehavior<OrdersModule, Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, string>>("inner");
    }
}
