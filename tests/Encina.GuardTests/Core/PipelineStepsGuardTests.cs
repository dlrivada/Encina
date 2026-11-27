using LanguageExt;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for the pipeline ordering and step types (attribute, candidate, description, step lookup), the
/// notification guards and the four processor steps. None of them checks its arguments itself, so these tests pin
/// the accepted inputs and the refusal paths that do exist.
/// </summary>
public class PipelineStepsGuardTests
{
    private sealed record Ping : IRequest<string>;

    private sealed class Handlers
    {
        public static Task<Either<EncinaError, Unit>> Good(object notification, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));

        public static Task<Either<EncinaError, Unit>> OneParameter(object notification)
            => Task.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));

        public static Task WrongReturn(object notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [PipelineStage(PipelineStage.Application, Order = 7)]
    private sealed class StagedProcessor : IRequestPreProcessor<Ping>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Ping request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class PlainPre(Func<CancellationToken, ValueTask<Either<EncinaError, Unit>>> process) : IRequestPreProcessor<Ping>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Ping request, IRequestContext context, CancellationToken cancellationToken)
            => process(cancellationToken);
    }

    private sealed class PlainPost(Func<CancellationToken, ValueTask<Either<EncinaError, Unit>>> process) : IRequestPostProcessor<Ping, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Ping request, IRequestContext context, string response, CancellationToken cancellationToken)
            => process(cancellationToken);
    }

    private sealed class PlainPostCommit(Func<CancellationToken, ValueTask<Either<EncinaError, Unit>>> process) : IRequestPostCommitProcessor<Ping, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Ping request, IRequestContext context, string response, CancellationToken cancellationToken)
            => process(cancellationToken);
    }

    private sealed class PlainError(Func<CancellationToken, ValueTask> process) : IRequestErrorProcessor<Ping, string>
    {
        public ValueTask Process(Ping request, IRequestContext context, EncinaError failure, CancellationToken cancellationToken)
            => process(cancellationToken);
    }

    private static readonly Ping Request = new();

    private static ValueTask<Either<EncinaError, Unit>> Right(CancellationToken _)
        => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));

    private static ValueTask<Either<EncinaError, Unit>> Cancelled(CancellationToken token)
        => throw new OperationCanceledException(token);

    private static RequestHandlerCallback<string> Next(Either<EncinaError, string> result, Action? onCall = null)
        => () =>
        {
            onCall?.Invoke();
            return ValueTask.FromResult(result);
        };

    private static Either<EncinaError, string> Ok => Either<EncinaError, string>.Right("ok");

    private static Either<EncinaError, string> Failed => Either<EncinaError, string>.Left(EncinaErrors.Create("test.failed", ErrorCategory.Internal, "failed"));

    private static CancellationToken CancelledToken()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        return source.Token;
    }

    #region EncinaNotificationGuards.TryValidateHandleMethod

    /// <summary>
    /// A Handle method with the notification, a token and the expected return type is valid.
    /// </summary>
    [Fact]
    public void TryValidateHandleMethod_ValidMethod_ReturnsTrue()
    {
        var method = typeof(Handlers).GetMethod(nameof(Handlers.Good))!;

        EncinaNotificationGuards.TryValidateHandleMethod(method, typeof(Handlers), "Note", out _).ShouldBeTrue();
    }

    /// <summary>
    /// A Handle method with one parameter is refused with the invoke-exception code.
    /// </summary>
    [Fact]
    public void TryValidateHandleMethod_WrongParameterCount_ReturnsFalse()
    {
        var method = typeof(Handlers).GetMethod(nameof(Handlers.OneParameter))!;

        EncinaNotificationGuards.TryValidateHandleMethod(method, typeof(Handlers), "Note", out var failure).ShouldBeFalse();

        failure.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.NotificationInvokeException);
    }

    /// <summary>
    /// A Handle method with another return type is refused with the invalid-return code.
    /// </summary>
    [Fact]
    public void TryValidateHandleMethod_WrongReturnType_ReturnsFalse()
    {
        var method = typeof(Handlers).GetMethod(nameof(Handlers.WrongReturn))!;

        EncinaNotificationGuards.TryValidateHandleMethod(method, typeof(Handlers), "Note", out var failure).ShouldBeFalse();

        failure.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.NotificationInvalidReturn);
    }

    #endregion

    #region PipelineStageLookup.Find(Type)

    /// <summary>
    /// A null type is refused by the attribute lookup.
    /// </summary>
    [Fact]
    public void Find_NullType_ThrowsArgumentNullException()
    {
        var act = () => PipelineStageLookup.Find(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// A type with the attribute returns it; a type without returns null.
    /// </summary>
    [Fact]
    public void Find_TypeWithAndWithoutAttribute_ReturnsAttributeOrNull()
    {
        PipelineStageLookup.Find(typeof(StagedProcessor))!.Order.ShouldBe(7);
        PipelineStageLookup.Find(typeof(Handlers)).ShouldBeNull();
    }

    #endregion

    #region PipelineStageAttribute, PipelineStepDescription, PipelineStepCandidate

    /// <summary>
    /// The attribute accepts any stage value (an enum has no null check) and defaults the order to 0.
    /// </summary>
    [Fact]
    public void PipelineStageAttribute_UndefinedStage_IsStored()
    {
        var attribute = new PipelineStageAttribute((PipelineStage)999);

        attribute.Stage.ShouldBe((PipelineStage)999);
        attribute.Order.ShouldBe(0);
    }

    /// <summary>
    /// The description record stores its members and compares by value.
    /// </summary>
    [Fact]
    public void PipelineStepDescription_SameMembers_AreEqual()
    {
        var first = new PipelineStepDescription(typeof(Ping), PipelineStepKind.Behavior, PipelineStage.Application, 3, PipelineStepSource.Default);
        var second = first with { };

        second.ShouldBe(first);
        first.StepType.ShouldBe(typeof(Ping));
        first.Order.ShouldBe(3);
    }

    /// <summary>
    /// A candidate for a behavior that does not describe itself is a plain behavior of its runtime type.
    /// </summary>
    [Fact]
    public void PipelineStepCandidate_For_PlainObject_IsBehavior()
    {
        var candidate = PipelineStepCandidate.For(new Handlers(), 4);

        candidate.Kind.ShouldBe(PipelineStepKind.Behavior);
        candidate.StepType.ShouldBe(typeof(Handlers));
        candidate.ModuleType.ShouldBeNull();
        candidate.RegistrationIndex.ShouldBe(4);
    }

    /// <summary>
    /// A candidate for a step that describes itself takes the kind and step type from that identity.
    /// </summary>
    [Fact]
    public void PipelineStepCandidate_For_Identity_UsesItsKindAndStepType()
    {
        var step = new PreProcessorStep<Ping, string>(new StagedProcessor());

        var candidate = PipelineStepCandidate.For(step, 0);

        candidate.Kind.ShouldBe(PipelineStepKind.PreProcessor);
        candidate.StepType.ShouldBe(typeof(StagedProcessor));
        candidate.Identity.ShouldBeSameAs(step);
    }

    #endregion

    #region Step identity

    /// <summary>
    /// Every step reports its kind, the processor type, no module and the declared stage and order of the processor.
    /// </summary>
    [Fact]
    public void Steps_Identity_ReportsKindStepTypeAndDeclaredStage()
    {
        var staged = new PreProcessorStep<Ping, string>(new StagedProcessor());
        staged.Kind.ShouldBe(PipelineStepKind.PreProcessor);
        staged.StepType.ShouldBe(typeof(StagedProcessor));
        staged.ModuleType.ShouldBeNull();
        staged.DeclaredStage.ShouldBe(PipelineStage.Application);
        staged.DeclaredOrder.ShouldBe(7);

        var plain = new PostProcessorStep<Ping, string>(new PlainPost(Right));
        plain.Kind.ShouldBe(PipelineStepKind.PostProcessor);
        plain.DeclaredStage.ShouldBeNull();
        plain.DeclaredOrder.ShouldBeNull();

        new PostCommitProcessorStep<Ping, string>(new PlainPostCommit(Right)).Kind.ShouldBe(PipelineStepKind.PostCommitProcessor);
        new ErrorProcessorStep<Ping, string>(new PlainError(_ => ValueTask.CompletedTask)).Kind.ShouldBe(PipelineStepKind.ErrorProcessor);
    }

    #endregion

    #region PreProcessorStep.Handle

    /// <summary>
    /// A pre-processor that succeeds lets the next step run.
    /// </summary>
    [Fact]
    public async Task PreProcessorStep_Success_RunsNextStep()
    {
        var called = false;
        var step = new PreProcessorStep<Ping, string>(new PlainPre(Right));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok, () => called = true), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        called.ShouldBeTrue();
    }

    /// <summary>
    /// A pre-processor that fails short-circuits the pipeline with its error.
    /// </summary>
    [Fact]
    public async Task PreProcessorStep_Left_ShortCircuits()
    {
        var called = false;
        var error = EncinaErrors.Create("test.pre", ErrorCategory.Validation, "pre failed");
        var step = new PreProcessorStep<Ping, string>(new PlainPre(_ => ValueTask.FromResult(Either<EncinaError, Unit>.Left(error))));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok, () => called = true), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.pre");
        called.ShouldBeFalse();
    }

    /// <summary>
    /// A cancelled pre-processor becomes a cancelled-category error.
    /// </summary>
    [Fact]
    public async Task PreProcessorStep_Cancelled_ReturnsCancelledError()
    {
        var step = new PreProcessorStep<Ping, string>(new PlainPre(Cancelled));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancelledToken());

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.PreProcessorCancelled);
    }

    #endregion

    #region PostProcessorStep.Handle

    /// <summary>
    /// A post-processor runs after a successful inner step and keeps its response.
    /// </summary>
    [Fact]
    public async Task PostProcessorStep_Success_ReturnsInnerResponse()
    {
        var step = new PostProcessorStep<Ping, string>(new PlainPost(Right));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancellationToken.None);

        ((string)result).ShouldBe("ok");
    }

    /// <summary>
    /// A failed inner step is returned and the post-processor never runs.
    /// </summary>
    [Fact]
    public async Task PostProcessorStep_InnerLeft_SkipsProcessor()
    {
        var ran = false;
        var step = new PostProcessorStep<Ping, string>(new PlainPost(t => { ran = true; return Right(t); }));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Failed), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    /// <summary>
    /// A post-processor that fails fails the request.
    /// </summary>
    [Fact]
    public async Task PostProcessorStep_ProcessorLeft_FailsRequest()
    {
        var error = EncinaErrors.Create("test.post", ErrorCategory.Internal, "post failed");
        var step = new PostProcessorStep<Ping, string>(new PlainPost(_ => ValueTask.FromResult(Either<EncinaError, Unit>.Left(error))));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.post");
    }

    /// <summary>
    /// A cancelled post-processor becomes a cancelled-category error.
    /// </summary>
    [Fact]
    public async Task PostProcessorStep_Cancelled_ReturnsCancelledError()
    {
        var step = new PostProcessorStep<Ping, string>(new PlainPost(Cancelled));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancelledToken());

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.PostProcessorCancelled);
    }

    #endregion

    #region PostCommitProcessorStep.Handle

    /// <summary>
    /// A post-commit processor runs after a successful inner step and keeps its response.
    /// </summary>
    [Fact]
    public async Task PostCommitProcessorStep_Success_ReturnsInnerResponse()
    {
        var step = new PostCommitProcessorStep<Ping, string>(new PlainPostCommit(Right));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancellationToken.None);

        ((string)result).ShouldBe("ok");
    }

    /// <summary>
    /// A failed inner step is returned and the post-commit processor never runs.
    /// </summary>
    [Fact]
    public async Task PostCommitProcessorStep_InnerLeft_SkipsProcessor()
    {
        var ran = false;
        var step = new PostCommitProcessorStep<Ping, string>(new PlainPostCommit(t => { ran = true; return Right(t); }));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Failed), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    /// <summary>
    /// A post-commit processor that fails fails the request.
    /// </summary>
    [Fact]
    public async Task PostCommitProcessorStep_ProcessorLeft_FailsRequest()
    {
        var error = EncinaErrors.Create("test.commit", ErrorCategory.Internal, "commit failed");
        var step = new PostCommitProcessorStep<Ping, string>(new PlainPostCommit(_ => ValueTask.FromResult(Either<EncinaError, Unit>.Left(error))));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.commit");
    }

    /// <summary>
    /// A cancelled post-commit processor becomes a cancelled-category error.
    /// </summary>
    [Fact]
    public async Task PostCommitProcessorStep_Cancelled_ReturnsCancelledError()
    {
        var step = new PostCommitProcessorStep<Ping, string>(new PlainPostCommit(Cancelled));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancelledToken());

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.PostCommitProcessorCancelled);
    }

    #endregion

    #region ErrorProcessorStep.Handle

    /// <summary>
    /// A success is returned without the error processor observing anything.
    /// </summary>
    [Fact]
    public async Task ErrorProcessorStep_Right_DoesNotObserve()
    {
        var observed = false;
        var step = new ErrorProcessorStep<Ping, string>(new PlainError(_ => { observed = true; return ValueTask.CompletedTask; }));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Ok), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        observed.ShouldBeFalse();
    }

    /// <summary>
    /// A failure is observed and returned unchanged.
    /// </summary>
    [Fact]
    public async Task ErrorProcessorStep_Left_ObservesAndKeepsError()
    {
        var observed = false;
        var step = new ErrorProcessorStep<Ping, string>(new PlainError(_ => { observed = true; return ValueTask.CompletedTask; }));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Failed), CancellationToken.None);

        observed.ShouldBeTrue();
        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.failed");
    }

    /// <summary>
    /// A cancelled observer does not replace the original error.
    /// </summary>
    [Fact]
    public async Task ErrorProcessorStep_CancelledObserver_KeepsOriginalError()
    {
        var step = new ErrorProcessorStep<Ping, string>(new PlainError(t => throw new OperationCanceledException(t)));

        var result = await step.Handle(Request, RequestContext.CreateForTest(), Next(Failed), CancelledToken());

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.failed");
    }

    #endregion
}
