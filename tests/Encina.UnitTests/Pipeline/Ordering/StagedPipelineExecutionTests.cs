using System.Runtime.CompilerServices;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// End-to-end behavior of the request and stream builders: steps run in stage order whatever the registration
/// order, and the four processor kinds run with their documented semantics.
/// </summary>
public sealed class StagedPipelineExecutionTests
{
    #region Stage order

    [Fact]
    public async Task Send_BehaviorsRegisteredInReverse_RunInStageOrder()
    {
        var calls = new List<string>();

        await SendAsync(calls, services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new HandlerPrep(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Validation(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Authz(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Observe(calls));
        });

        calls.ShouldBe(["observe", "authz", "validation", "handler-prep", "handler"]);
    }

    [Fact]
    public async Task Send_PreProcessorRunsInsideTheGates_AfterAuthorizationAndBeforeTheHandler()
    {
        var calls = new List<string>();

        await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestPreProcessor<Req>>(_ => new Pre(calls, null));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Authz(calls));
        });

        calls.ShouldBe(["authz", "pre", "handler"]);
    }

    [Fact]
    public async Task Send_AuthorizationDenial_PreventsTheApplicationPreProcessor()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestPreProcessor<Req>>(_ => new Pre(calls, null));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Authz(calls, deny: true));
        });

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["authz"]);
    }

    [Fact]
    public async Task Send_RegistrationOverrideMovesATypeWithoutAttribute()
    {
        var calls = new List<string>();
        var registry = new PipelineStageRegistry();
        registry.Set(typeof(Undeclared), PipelineStage.Observability, -1);

        await SendAsync(calls, services =>
        {
            services.AddSingleton(registry);
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Observe(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Undeclared(calls));
        });

        calls.ShouldBe(["undeclared", "observe", "handler"]);
    }

    [Fact]
    public async Task Send_SameBehaviorTypeRegisteredTwice_RunsOnce()
    {
        var calls = new List<string>();

        await SendAsync(calls, services =>
        {
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Observe(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Observe(calls));
        });

        calls.ShouldBe(["observe", "handler"]);
    }

    [Fact]
    public async Task Send_SecondRequest_ReusesTheCachedPermutation()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, false));
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Validation(calls));
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Observe(calls));
        await using var provider = services.BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();

        await encina.Send(new Req(), CancellationToken.None);
        await encina.Send(new Req(), CancellationToken.None);

        provider.GetRequiredService<PipelineOrderResolver>().Computations.ShouldBe(1);
        calls.ShouldBe(["observe", "validation", "handler", "observe", "validation", "handler"]);
    }

    [Fact]
    public async Task Send_NoBehaviorsAndNoProcessors_CallsTheHandlerDirectly()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, _ => { });

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["handler"]);
    }

    #endregion

    [Fact]
    public void AddEncina_DoesNotRegisterTheProcessorStepsAsServices_AndRegistersTheResolver()
    {
        var services = new ServiceCollection();
        services.AddEncina();

        var stepTypes = new[] { typeof(PreProcessorStep<,>), typeof(PostProcessorStep<,>), typeof(ErrorProcessorStep<,>), typeof(PostCommitProcessorStep<,>) };
        services.Where(descriptor => descriptor.ImplementationType is { IsGenericTypeDefinition: true } type && stepTypes.Contains(type)).ShouldBeEmpty();
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(PipelineOrderResolver) && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(PipelineStageRegistry) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    #region Error processor

    [Fact]
    public async Task ErrorProcessor_RunsOnLeft_AndTheOriginalErrorIsReturned()
    {
        var calls = new List<string>();
        var observed = new ErrorObserver(calls);

        var result = await SendAsync(calls, services => services.AddScoped<IRequestErrorProcessor<Req, string>>(_ => observed), handlerFails: true);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("handler failed"));
        observed.Observed.ShouldNotBeNull();
        observed.Observed!.Value.Message.ShouldBe("handler failed");
        calls.ShouldBe(["handler", "error"]);
    }

    [Fact]
    public async Task ErrorProcessor_DoesNotRunOnRight()
    {
        var calls = new List<string>();
        var observed = new ErrorObserver(calls);

        var result = await SendAsync(calls, services => services.AddScoped<IRequestErrorProcessor<Req, string>>(_ => observed));

        result.IsRight.ShouldBeTrue();
        observed.Observed.ShouldBeNull();
        calls.ShouldBe(["handler"]);
    }

    [Fact]
    public async Task ErrorProcessor_SeesAnErrorProducedByAnInnerGate()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestErrorProcessor<Req, string>>(_ => new ErrorObserver(calls));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new HandlerPrep(calls, deny: true));
        });

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["handler-prep", "error"]);
    }

    [Fact]
    public async Task ErrorProcessor_CancelledObserver_StillReturnsTheOriginalError()
    {
        var calls = new List<string>();
        using var cts = new CancellationTokenSource();
        var processor = new CancellingErrorObserver(cts);

        var result = await SendAsync(calls, services => services.AddScoped<IRequestErrorProcessor<Req, string>>(_ => processor), handlerFails: true, token: cts.Token);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("handler failed"));
    }

    #endregion

    #region Post-commit processor

    [Fact]
    public async Task PostCommitProcessor_RunsAfterTheCommit()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new PostCommit(calls, null));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Tx(calls));
        });

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["tx-begin", "handler", "tx-commit", "post-commit"]);
    }

    [Fact]
    public async Task PostCommitProcessor_DoesNotRunAfterARollback()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new PostCommit(calls, null));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Tx(calls));
        }, handlerFails: true);

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["tx-begin", "handler", "tx-rollback"]);
    }

    [Fact]
    public async Task PostCommitProcessor_Left_FailsTheRequestAfterTheCommit()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, services =>
        {
            services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new PostCommit(calls, EncinaError.New("post-commit failed")));
            services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Tx(calls));
        });

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("post-commit failed"));
        calls.ShouldBe(["tx-begin", "handler", "tx-commit", "post-commit"]);
    }

    [Fact]
    public async Task PostCommitProcessor_WithoutATransactionBehavior_RunsAfterASuccessfulPipeline()
    {
        var calls = new List<string>();

        await SendAsync(calls, services => services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new PostCommit(calls, null)));

        calls.ShouldBe(["handler", "post-commit"]);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task PreProcessor_Cancelled_MapsToThePreProcessorCancelledCode()
    {
        var calls = new List<string>();
        using var cts = new CancellationTokenSource();

        var result = await SendAsync(calls, services => services.AddScoped<IRequestPreProcessor<Req>>(_ => new CancellingPre(cts)), token: cts.Token);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.PreProcessorCancelled));
    }

    [Fact]
    public async Task PostProcessor_Cancelled_MapsToThePostProcessorCancelledCode()
    {
        var calls = new List<string>();
        using var cts = new CancellationTokenSource();

        var result = await SendAsync(calls, services => services.AddScoped<IRequestPostProcessor<Req, string>>(_ => new CancellingPost(cts)), token: cts.Token);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.PostProcessorCancelled));
    }

    [Fact]
    public async Task PostCommitProcessor_Cancelled_MapsToThePostProcessorCancelledCode()
    {
        var calls = new List<string>();
        using var cts = new CancellationTokenSource();

        var result = await SendAsync(calls, services => services.AddScoped<IRequestPostCommitProcessor<Req, string>>(_ => new CancellingPostCommit(cts)), token: cts.Token);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.PostProcessorCancelled));
    }

    #endregion

    #region Streams

    [Fact]
    public async Task Stream_BehaviorsRegisteredInReverse_RunInStageOrder()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IStreamRequestHandler<StreamReq, int>>(_ => new StreamHandler(calls));
        services.AddScoped<IStreamPipelineBehavior<StreamReq, int>>(_ => new StreamValidation(calls));
        services.AddScoped<IStreamPipelineBehavior<StreamReq, int>>(_ => new StreamObserve(calls));
        await using var provider = services.BuildServiceProvider();

        var items = new List<int>();
        await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new StreamReq(), CancellationToken.None))
        {
            item.IfRight(items.Add);
        }

        items.ShouldBe([1]);
        calls.ShouldBe(["stream-observe", "stream-validation", "stream-handler"]);
    }

    [Fact]
    public async Task Stream_NoHandlerRegistered_YieldsAHandlerMissingError()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        await using var provider = services.BuildServiceProvider();

        var errors = new List<string>();
        await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new StreamReq(), CancellationToken.None))
        {
            item.IfLeft(error => errors.Add(error.GetEncinaCode()));
        }

        errors.ShouldBe([EncinaErrorCodes.HandlerMissing]);
    }

    [Fact]
    public async Task Stream_NoBehaviors_CallsTheHandlerDirectly()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IStreamRequestHandler<StreamReq, int>>(_ => new StreamHandler(calls));
        await using var provider = services.BuildServiceProvider();

        await foreach (var _ in provider.GetRequiredService<IEncina>().Stream(new StreamReq(), CancellationToken.None))
        {
        }

        calls.ShouldBe(["stream-handler"]);
    }

    #endregion

    private static async Task<Either<EncinaError, string>> SendAsync(
        List<string> calls,
        Action<IServiceCollection> configure,
        bool handlerFails = false,
        CancellationToken token = default)
    {
        var services = new ServiceCollection();
        configure(services);
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, handlerFails));

        await using var provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<IEncina>().Send(new Req(), token);
    }

    #region Test types

    private sealed record Req : IRequest<string>;

    private sealed record StreamReq : IStreamRequest<int>;

    private sealed class Handler(List<string> calls, bool fails) : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult(
                fails
                    ? Either<EncinaError, string>.Left(EncinaError.New("handler failed"))
                    : Either<EncinaError, string>.Right("handled"));
        }
    }

    private abstract class Recording(List<string> calls, string name, bool deny = false) : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            calls.Add(name);
            return deny ? ValueTask.FromResult<Either<EncinaError, string>>(EncinaError.New(name + " denied")) : nextStep();
        }
    }

    [PipelineStage(PipelineStage.Observability)]
    private sealed class Observe(List<string> calls) : Recording(calls, "observe");

    [PipelineStage(PipelineStage.Authorization)]
    private sealed class Authz(List<string> calls, bool deny = false) : Recording(calls, "authz", deny);

    [PipelineStage(PipelineStage.Validation)]
    private sealed class Validation(List<string> calls) : Recording(calls, "validation");

    [PipelineStage(PipelineStage.HandlerPreparation)]
    private sealed class HandlerPrep(List<string> calls, bool deny = false) : Recording(calls, "handler-prep", deny);

    private sealed class Undeclared(List<string> calls) : Recording(calls, "undeclared");

    [PipelineStage(PipelineStage.Transaction)]
    private sealed class Tx(List<string> calls) : IPipelineBehavior<Req, string>
    {
        public async ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            calls.Add("tx-begin");
            var result = await nextStep();
            calls.Add(result.IsRight ? "tx-commit" : "tx-rollback");
            return result;
        }
    }

    private sealed class Pre(List<string> calls, EncinaError? failure) : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
        {
            calls.Add("pre");
            return ValueTask.FromResult<Either<EncinaError, Unit>>(failure is { } error ? error : Unit.Default);
        }
    }

    private sealed class PostCommit(List<string> calls, EncinaError? failure) : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            calls.Add("post-commit");
            return ValueTask.FromResult<Either<EncinaError, Unit>>(failure is { } error ? error : Unit.Default);
        }
    }

    private sealed class ErrorObserver(List<string> calls) : IRequestErrorProcessor<Req, string>
    {
        public EncinaError? Observed { get; private set; }

        public ValueTask Process(Req request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
        {
            calls.Add("error");
            Observed = error;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancellingErrorObserver(CancellationTokenSource cts) : IRequestErrorProcessor<Req, string>
    {
        public ValueTask Process(Req request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
        {
            cts.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class CancellingPre(CancellationTokenSource cts) : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
        {
            cts.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class CancellingPost(CancellationTokenSource cts) : IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            cts.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class CancellingPostCommit(CancellationTokenSource cts) : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            cts.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class StreamHandler(List<string> calls) : IStreamRequestHandler<StreamReq, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(StreamReq request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            calls.Add("stream-handler");
            await Task.CompletedTask;
            yield return 1;
        }
    }

    private abstract class StreamRecording(List<string> calls, string name) : IStreamPipelineBehavior<StreamReq, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamReq request, IRequestContext context, StreamHandlerCallback<int> nextStep, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            calls.Add(name);
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    [PipelineStage(PipelineStage.Observability)]
    private sealed class StreamObserve(List<string> calls) : StreamRecording(calls, "stream-observe");

    [PipelineStage(PipelineStage.Validation)]
    private sealed class StreamValidation(List<string> calls) : StreamRecording(calls, "stream-validation");

    #endregion
}
