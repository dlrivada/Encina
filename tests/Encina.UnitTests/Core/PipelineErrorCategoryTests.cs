using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core;

/// <summary>
/// Pins the <see cref="ErrorCategory"/> of every error the core pipeline (<c>src/Encina/Pipeline</c>) creates.
/// Each case runs the real producer and reads the category back with <c>GetCategory()</c> (#3691, ADR-062).
/// </summary>
public sealed class PipelineErrorCategoryTests
{
    private sealed record Req : IRequest<string>;

    private sealed record Cmd : ICommand<string>;

    private sealed record Qry : IQuery<string>;

    private sealed class Handler : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>("ok"));
    }

    private sealed class CancellingHandler(CancellationToken token) : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
            => throw new OperationCanceledException(token);
    }

    private sealed class CancellingBehavior(CancellationToken token) : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => throw new OperationCanceledException(token);
    }

    private sealed class CancellingPre(CancellationToken token) : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => throw new OperationCanceledException(token);
    }

    private sealed class CancellingPost(CancellationToken token) : IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => throw new OperationCanceledException(token);
    }

    private sealed class CancellingPostCommit(CancellationToken token) : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => throw new OperationCanceledException(token);
    }

    private sealed class NotificationHandlers
    {
        public static Task<Either<EncinaError, Unit>> WrongParameters(object notification)
            => Task.FromResult(Right<EncinaError, Unit>(unit));

        public static Task<Unit> WrongReturn(object notification, CancellationToken cancellationToken)
            => Task.FromResult(unit);
    }

    private sealed class NoopMetrics : IEncinaMetrics
    {
        public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
        {
        }

        public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
        {
        }
    }

    private static EncinaError LeftOf<T>(Either<EncinaError, T> either)
    {
        either.IsLeft.ShouldBeTrue();
        return either.LeftAsEnumerable().First();
    }

    private static CancellationToken Cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    private static void ShouldBe(EncinaError error, string code, ErrorCategory category)
    {
        error.GetEncinaCode().ShouldBe(code);
        error.GetCategory().ShouldBe(category);
    }

    private static async Task<EncinaError> RunPipeline(Action<IServiceCollection> configure, IRequestHandler<Req, string> handler, CancellationToken token)
    {
        var services = new ServiceCollection();
        configure(services);
        await using var provider = services.BuildServiceProvider();
        var resolver = new PipelineOrderResolver(new PipelineStageRegistry(), NullLogger<PipelineOrderResolver>.Instance);
        var builder = new PipelineBuilder<Req, string>(new Req(), handler, RequestContext.Create(), resolver, token);

        var result = await builder.Build(provider)();

        return LeftOf(result);
    }

    #region Request guards

    [Fact]
    public void TryValidateRequest_Null_IsInternal()
    {
        EncinaRequestGuards.TryValidateRequest<string>(null, out var error).ShouldBeFalse();

        ShouldBe(LeftOf(error), EncinaErrorCodes.RequestNull, ErrorCategory.Internal);
    }

    [Fact]
    public void TryValidateStreamRequest_Null_IsInternal()
    {
        EncinaRequestGuards.TryValidateStreamRequest<string>(null, out var error).ShouldBeFalse();

        ShouldBe(LeftOf(error), EncinaErrorCodes.RequestNull, ErrorCategory.Internal);
    }

    [Fact]
    public void TryValidateNotification_Null_IsInternal()
    {
        EncinaRequestGuards.TryValidateNotification(null, out var error).ShouldBeFalse();

        ShouldBe(LeftOf(error), EncinaErrorCodes.NotificationNull, ErrorCategory.Internal);
    }

    [Fact]
    public void TryValidateHandler_Missing_IsInternal()
    {
        EncinaRequestGuards.TryValidateHandler<string>(null, typeof(Req), typeof(string), out var error).ShouldBeFalse();

        ShouldBe(LeftOf(error), EncinaErrorCodes.RequestHandlerMissing, ErrorCategory.Internal);
    }

    [Fact]
    public void TryValidateHandlerType_Mismatch_IsInternal()
    {
        EncinaRequestGuards.TryValidateHandlerType<string>(new object(), typeof(IRequestHandler<Req, string>), typeof(Req), out var error)
            .ShouldBeFalse();

        ShouldBe(LeftOf(error), EncinaErrorCodes.RequestHandlerTypeMismatch, ErrorCategory.Internal);
    }

    #endregion

    #region Notification and behavior guards

    [Fact]
    public void TryValidateHandleMethod_WrongParameters_IsInternal()
    {
        var method = typeof(NotificationHandlers).GetMethod(nameof(NotificationHandlers.WrongParameters))!;

        EncinaNotificationGuards.TryValidateHandleMethod(method, typeof(NotificationHandlers), "N", out var failure).ShouldBeFalse();

        ShouldBe(failure, EncinaErrorCodes.NotificationInvokeException, ErrorCategory.Internal);
    }

    [Fact]
    public void TryValidateHandleMethod_WrongReturnType_IsInternal()
    {
        MethodInfo method = typeof(NotificationHandlers).GetMethod(nameof(NotificationHandlers.WrongReturn))!;

        EncinaNotificationGuards.TryValidateHandleMethod(method, typeof(NotificationHandlers), "N", out var failure).ShouldBeFalse();

        ShouldBe(failure, EncinaErrorCodes.NotificationInvalidReturn, ErrorCategory.Internal);
    }

    [Fact]
    public void BehaviorGuard_NullRequest_IsInternal()
    {
        EncinaBehaviorGuards.TryValidateRequest(typeof(CancellingBehavior), null, out var failure).ShouldBeFalse();

        ShouldBe(failure, EncinaErrorCodes.BehaviorNullRequest, ErrorCategory.Internal);
    }

    [Fact]
    public void BehaviorGuard_NullNextStep_IsInternal()
    {
        EncinaBehaviorGuards.TryValidateNextStep(typeof(CancellingBehavior), null, out var failure).ShouldBeFalse();

        ShouldBe(failure, EncinaErrorCodes.BehaviorNullNext, ErrorCategory.Internal);
    }

    #endregion

    #region PipelineBuilder and processor steps

    [Fact]
    public async Task PipelineBuilder_CancelledHandler_IsCancelled()
    {
        var token = Cancelled();

        var error = await RunPipeline(_ => { }, new CancellingHandler(token), token);

        ShouldBe(error, EncinaErrorCodes.HandlerCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task PipelineBuilder_CancelledBehavior_IsCancelled()
    {
        var token = Cancelled();

        var error = await RunPipeline(
            services => services.AddSingleton<IPipelineBehavior<Req, string>>(new CancellingBehavior(token)),
            new Handler(),
            token);

        ShouldBe(error, EncinaErrorCodes.BehaviorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task PreProcessorStep_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var step = new PreProcessorStep<Req, string>(new CancellingPre(token));

        var result = await step.Handle(new Req(), RequestContext.Create(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.PreProcessorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task PostProcessorStep_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var step = new PostProcessorStep<Req, string>(new CancellingPost(token));

        var result = await step.Handle(new Req(), RequestContext.Create(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.PostProcessorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task PostCommitProcessorStep_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var step = new PostCommitProcessorStep<Req, string>(new CancellingPostCommit(token));

        var result = await step.Handle(new Req(), RequestContext.Create(), () => ValueTask.FromResult(Right<EncinaError, string>("ok")), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.PostCommitProcessorCancelled, ErrorCategory.Cancelled);
    }

    #endregion

    #region Command and query behaviors

    private static RequestHandlerCallback<string> Throwing(Exception exception) => () => throw exception;

    [Fact]
    public async Task CommandActivityBehavior_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var behavior = new CommandActivityPipelineBehavior<Cmd, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(new Cmd(), RequestContext.Create(), Throwing(new OperationCanceledException(token)), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task CommandActivityBehavior_Exception_IsInternal()
    {
        var behavior = new CommandActivityPipelineBehavior<Cmd, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new Cmd(), RequestContext.Create(), Throwing(new InvalidOperationException("boom")), CancellationToken.None);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorException, ErrorCategory.Internal);
    }

    [Fact]
    public async Task CommandMetricsBehavior_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var behavior = new CommandMetricsPipelineBehavior<Cmd, string>(new NoopMetrics(), NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(new Cmd(), RequestContext.Create(), Throwing(new OperationCanceledException(token)), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task CommandMetricsBehavior_Exception_IsInternal()
    {
        var behavior = new CommandMetricsPipelineBehavior<Cmd, string>(new NoopMetrics(), NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new Cmd(), RequestContext.Create(), Throwing(new InvalidOperationException("boom")), CancellationToken.None);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorException, ErrorCategory.Internal);
    }

    [Fact]
    public async Task QueryActivityBehavior_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var behavior = new QueryActivityPipelineBehavior<Qry, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(new Qry(), RequestContext.Create(), Throwing(new OperationCanceledException(token)), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task QueryActivityBehavior_Exception_IsInternal()
    {
        var behavior = new QueryActivityPipelineBehavior<Qry, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new Qry(), RequestContext.Create(), Throwing(new InvalidOperationException("boom")), CancellationToken.None);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorException, ErrorCategory.Internal);
    }

    [Fact]
    public async Task QueryMetricsBehavior_Cancelled_IsCancelled()
    {
        var token = Cancelled();
        var behavior = new QueryMetricsPipelineBehavior<Qry, string>(new NoopMetrics(), NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(new Qry(), RequestContext.Create(), Throwing(new OperationCanceledException(token)), token);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorCancelled, ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task QueryMetricsBehavior_Exception_IsInternal()
    {
        var behavior = new QueryMetricsPipelineBehavior<Qry, string>(new NoopMetrics(), NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new Qry(), RequestContext.Create(), Throwing(new InvalidOperationException("boom")), CancellationToken.None);

        ShouldBe(LeftOf(result), EncinaErrorCodes.BehaviorException, ErrorCategory.Internal);
    }

    #endregion
}
