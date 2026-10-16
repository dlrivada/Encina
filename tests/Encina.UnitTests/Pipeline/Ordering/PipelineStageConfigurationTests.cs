using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Phase 4 of #2184: the stage-aware overloads of <see cref="EncinaConfiguration"/>, the registration of the ordering
/// infrastructure by <c>AddEncina</c>, the scanner support for the two new processor kinds and the inspector.
/// </summary>
public sealed class PipelineStageConfigurationTests
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    #region Overloads

    [Fact]
    public void AddPipelineBehavior_WithStage_RegistersTheTypeAndRecordsTheDeclaration()
    {
        var configuration = new EncinaConfiguration();

        configuration.AddPipelineBehavior(typeof(AuthBehavior), PipelineStage.Authorization, 7);

        configuration.PipelineBehaviorTypes.ShouldContain(typeof(AuthBehavior));
        configuration.StageDeclarations.ShouldBe([new PipelineStageDeclaration(typeof(AuthBehavior), PipelineStage.Authorization, 7)]);
    }

    [Fact]
    public void StageAwareOverloads_RecordEveryKindWithItsInterfaceCheck()
    {
        var configuration = new EncinaConfiguration()
            .AddPipelineBehavior<AuthBehavior>(PipelineStage.Authorization)
            .AddRequestPreProcessor<PreProcessor>(PipelineStage.InputPreparation, 1)
            .AddRequestPostProcessor<PostProcessor>(PipelineStage.Audit, 2)
            .AddRequestErrorProcessor<ErrorProcessor>(PipelineStage.Observability, 3)
            .AddRequestPostCommitProcessor<PostCommitProcessor>(PipelineStage.Transaction, -200)
            .AddStreamPipelineBehavior<StreamBehavior>(PipelineStage.Caching, 4);

        configuration.RequestPreProcessorTypes.ShouldBe([typeof(PreProcessor)]);
        configuration.RequestPostProcessorTypes.ShouldBe([typeof(PostProcessor)]);
        configuration.RequestErrorProcessorTypes.ShouldBe([typeof(ErrorProcessor)]);
        configuration.RequestPostCommitProcessorTypes.ShouldBe([typeof(PostCommitProcessor)]);
        configuration.StreamPipelineBehaviorTypes.ShouldBe([typeof(StreamBehavior)]);
        configuration.StageDeclarations.Count.ShouldBe(6);
        configuration.StageDeclarations.ShouldContain(new PipelineStageDeclaration(typeof(PostCommitProcessor), PipelineStage.Transaction, -200));
    }

    [Fact]
    public void StageLessOverloads_RegisterTheNewKindsWithoutDeclaringAStage()
    {
        var configuration = new EncinaConfiguration()
            .AddRequestErrorProcessor<ErrorProcessor>()
            .AddRequestErrorProcessor(typeof(ErrorProcessor))
            .AddRequestPostCommitProcessor<PostCommitProcessor>()
            .AddStreamPipelineBehavior(typeof(StreamBehavior));

        configuration.RequestErrorProcessorTypes.Count.ShouldBe(1);
        configuration.RequestPostCommitProcessorTypes.ShouldBe([typeof(PostCommitProcessor)]);
        configuration.StreamPipelineBehaviorTypes.ShouldBe([typeof(StreamBehavior)]);
        configuration.StageDeclarations.ShouldBeEmpty();
    }

    [Fact]
    public void StageAwareOverload_WithAWrongInterface_ThrowsAndRecordsNothing()
    {
        var configuration = new EncinaConfiguration();

        var exception = Should.Throw<ArgumentException>(
            () => configuration.AddRequestErrorProcessor(typeof(AuthBehavior), PipelineStage.Audit));

        exception.ParamName.ShouldBe("processorType");
        exception.Message.ShouldContain("IRequestErrorProcessor<,>");
        configuration.StageDeclarations.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyStageDeclarations_CopiesEveryDeclarationIntoTheRegistry()
    {
        var registry = new PipelineStageRegistry();
        var configuration = new EncinaConfiguration().AddPipelineBehavior(typeof(OpenBehavior<,>), PipelineStage.Caching, 9);

        configuration.ApplyStageDeclarations(registry);

        registry.TryGet(typeof(OpenBehavior<Req, string>), out var position).ShouldBeTrue();
        position.ShouldBe((PipelineStage.Caching, 9));
    }

    #endregion

    #region Registration

    [Fact]
    public void AddEncina_ConfiguredBehaviorImplementingCommandBehavior_RegistersOnlyThePipelineServiceType()
    {
        var services = new ServiceCollection();

        services.AddEncina(configuration => configuration.AddPipelineBehavior(typeof(CommandBehavior)));

        services.ShouldNotContain(descriptor => descriptor.ServiceType.IsGenericType
            && (descriptor.ServiceType.GetGenericTypeDefinition() == typeof(ICommandPipelineBehavior<,>)
                || descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IQueryPipelineBehavior<,>)));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<Cmd, string>)
            && descriptor.ImplementationType == typeof(CommandBehavior));
    }

    [Fact]
    public void AddEncina_ConfiguredErrorPostCommitAndStreamTypes_AreRegisteredUnderTheirServiceTypes()
    {
        var services = new ServiceCollection();

        services.AddEncina(configuration => configuration
            .AddRequestErrorProcessor<ErrorProcessor>()
            .AddRequestPostCommitProcessor<PostCommitProcessor>()
            .AddStreamPipelineBehavior<StreamBehavior>());

        services.ShouldContain(d => d.ServiceType == typeof(IRequestErrorProcessor<Req, string>) && d.ImplementationType == typeof(ErrorProcessor));
        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostCommitProcessor<Req, string>) && d.ImplementationType == typeof(PostCommitProcessor));
        services.ShouldContain(d => d.ServiceType == typeof(IStreamPipelineBehavior<StreamReq, int>) && d.ImplementationType == typeof(StreamBehavior));
    }

    [Fact]
    public void AddEncina_CalledTwice_RegistersTheOrderingInfrastructureOnce()
    {
        var services = new ServiceCollection();

        services.AddEncina();
        services.AddEncina();

        foreach (var serviceType in new[]
        {
            typeof(PipelineStageRegistry), typeof(PipelineOrderResolver), typeof(PipelineRegistrationSnapshot), typeof(IEncinaPipelineInspector)
        })
        {
            services.Count(descriptor => descriptor.ServiceType == serviceType).ShouldBe(1, serviceType.Name);
        }
    }

    [Fact]
    public void AddEncina_StageRegistryRegisteredWithoutAnInstance_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineStageRegistry>();

        Should.Throw<InvalidOperationException>(() => services.AddEncina())
            .Message.ShouldContain("PipelineStageRegistry");
    }

    [Fact]
    public async Task AddEncina_WithEveryNewRegistration_BuildsWithValidateOnBuildAndValidateScopes()
    {
        var services = new ServiceCollection();
        services.AddEncina(configuration => configuration
            .AddPipelineBehavior<AuthBehavior>(PipelineStage.Authorization)
            .AddRequestPreProcessor<PreProcessor>(PipelineStage.InputPreparation)
            .AddRequestPostProcessor<PostProcessor>()
            .AddRequestErrorProcessor<ErrorProcessor>()
            .AddRequestPostCommitProcessor<PostCommitProcessor>()
            .AddStreamPipelineBehavior<StreamBehavior>(PipelineStage.Caching));
        services.AddScoped<IRequestHandler<Req, string>, Handler>();

        await using var provider = services.BuildServiceProvider(StrictOptions);
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IEncinaPipelineInspector>().ShouldBeOfType<EncinaPipelineInspector>();
        provider.GetRequiredService<PipelineStageRegistry>().ShouldNotBeNull();
        provider.GetRequiredService<PipelineOrderResolver>().ShouldNotBeNull();
        provider.GetRequiredService<PipelineRegistrationSnapshot>().ShouldNotBeNull();
        var result = await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void AddEncina_DoesNotRegisterThePipelineOrderValidationHostedServiceYet()
    {
        var services = new ServiceCollection();

        services.AddEncina();

        services.ShouldNotContain(descriptor => descriptor.ImplementationType == typeof(PipelineOrderValidationHostedService));
    }

    #endregion

    #region Scanner

    [Fact]
    public void Scanner_FindsErrorAndPostCommitProcessors()
    {
        var result = EncinaAssemblyScanner.GetRegistrations(typeof(PipelineStageConfigurationTests).Assembly);

        result.RequestErrorProcessorRegistrations.ShouldContain(
            new TypeRegistration(typeof(IRequestErrorProcessor<Req, string>), typeof(ErrorProcessor)));
        result.RequestPostCommitProcessorRegistrations.ShouldContain(
            new TypeRegistration(typeof(IRequestPostCommitProcessor<Req, string>), typeof(PostCommitProcessor)));
        result.RequestErrorProcessorRegistrations.ShouldContain(
            new TypeRegistration(typeof(IRequestErrorProcessor<,>), typeof(OpenErrorProcessor<,>)));
        result.RequestPostCommitProcessorRegistrations.ShouldContain(
            new TypeRegistration(typeof(IRequestPostCommitProcessor<,>), typeof(OpenPostCommitProcessor<,>)));
    }

    [Fact]
    public void AddEncina_ScanningAnAssembly_RegistersTheScannedErrorAndPostCommitProcessors()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(PipelineStageConfigurationTests).Assembly);

        services.ShouldContain(d => d.ServiceType == typeof(IRequestErrorProcessor<Req, string>) && d.ImplementationType == typeof(ErrorProcessor));
        services.ShouldContain(d => d.ServiceType == typeof(IRequestPostCommitProcessor<,>) && d.ImplementationType == typeof(OpenPostCommitProcessor<,>));
    }

    #endregion

    #region Override and inspector

    [Fact]
    public async Task Inspector_ReportsTheResolvedOrderWithStageOrderAndSource()
    {
        var services = new ServiceCollection();
        services.AddEncina(configuration => configuration
            .AddPipelineBehavior<AuthBehavior>(PipelineStage.Authorization)
            .AddPipelineBehavior<AuditBehavior>()
            .AddPipelineBehavior<OverriddenBehavior>(PipelineStage.Caching, 3)
            .AddRequestPreProcessor<PreProcessor>()
            .AddRequestPostCommitProcessor<PostCommitProcessor>());
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var steps = await scope.ServiceProvider.GetRequiredService<IEncinaPipelineInspector>().DescribeAsync<Req, string>();

        var mine = steps.Where(step => step.StepType.DeclaringType == typeof(PipelineStageConfigurationTests)).ToArray();
        mine.ShouldBe(
        [
            new PipelineStepDescription(typeof(AuditBehavior), PipelineStepKind.Behavior, PipelineStage.Audit, 0, PipelineStepSource.Attribute),
            new PipelineStepDescription(typeof(AuthBehavior), PipelineStepKind.Behavior, PipelineStage.Authorization, 0, PipelineStepSource.Registration),
            new PipelineStepDescription(typeof(OverriddenBehavior), PipelineStepKind.Behavior, PipelineStage.Caching, 3, PipelineStepSource.Registration),
            new PipelineStepDescription(typeof(PostCommitProcessor), PipelineStepKind.PostCommitProcessor, PipelineStage.Transaction, -100, PipelineStepSource.Default),
            new PipelineStepDescription(typeof(PreProcessor), PipelineStepKind.PreProcessor, PipelineStage.Application, -100, PipelineStepSource.Default)
        ]);
    }

    [Fact]
    public async Task RegistrationOverride_BeatsTheAttribute_AndTheResolverRunsTheStepAtTheOverriddenPosition()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(calls);
        services.AddEncina(configuration => configuration
            .AddPipelineBehavior<RecordedAudit>()
            .AddPipelineBehavior<RecordedOverridden>(PipelineStage.Observability));
        services.AddScoped<IRequestHandler<Req, string>>(_ => new RecordingHandler(calls));
        await using var provider = services.BuildServiceProvider(StrictOptions);
        await using var scope = provider.CreateAsyncScope();

        var steps = await scope.ServiceProvider.GetRequiredService<IEncinaPipelineInspector>().DescribeAsync<Req, string>();
        var result = await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);

        // Both declare Audit by attribute; the registration moves one to Observability, which is further out
        var overridden = steps.Single(step => step.StepType == typeof(RecordedOverridden));
        overridden.Stage.ShouldBe(PipelineStage.Observability);
        overridden.Source.ShouldBe(PipelineStepSource.Registration);
        steps.Single(step => step.StepType == typeof(RecordedAudit)).Source.ShouldBe(PipelineStepSource.Attribute);
        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["overridden", "audit", "handler"]);
    }

    [Fact]
    public async Task ProcessorStageOverloads_DefaultToTheOrderOfTheKind_SoPostCommitStaysOutsideTransactionBehaviors()
    {
        var configuration = new EncinaConfiguration()
            .AddRequestPreProcessor<PreProcessor>(PipelineStage.InputPreparation)
            .AddRequestPostProcessor(typeof(PostProcessor), PipelineStage.Audit)
            .AddRequestErrorProcessor<ErrorProcessor>(PipelineStage.Observability)
            .AddRequestPostCommitProcessor<PostCommitProcessor>(PipelineStage.Transaction)
            .AddPipelineBehavior<AuthBehavior>(PipelineStage.Transaction);

        configuration.StageDeclarations.Select(declaration => declaration.Order).ShouldBe([-100, -100, -100, -100, 0]);

        var services = new ServiceCollection();
        services.AddEncina(c => c
            .AddRequestPostCommitProcessor<PostCommitProcessor>(PipelineStage.Transaction)
            .AddPipelineBehavior<AuthBehavior>(PipelineStage.Transaction));
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        // R7: the post-commit processor sits below (outside) every transaction behavior, so the pipeline is valid
        var described = await scope.ServiceProvider.GetRequiredService<IEncinaPipelineInspector>().DescribeAsync<Req, string>();
        var steps = described.Where(step => step.StepType.DeclaringType == typeof(PipelineStageConfigurationTests)).ToArray();
        steps.Select(step => step.StepType).ShouldBe([typeof(PostCommitProcessor), typeof(AuthBehavior)]);
    }

    [Fact]
    public async Task Inspector_StepThatImplementsOnlyIAsyncDisposable_IsDisposedWithoutThrowing()
    {
        var services = new ServiceCollection();
        services.AddEncina(configuration => configuration.AddPipelineBehavior<AsyncOnlyBehavior>(PipelineStage.Audit));
        await using var provider = services.BuildServiceProvider(StrictOptions);

        var steps = await provider.GetRequiredService<IEncinaPipelineInspector>().DescribeAsync<Req, string>();

        steps.ShouldContain(step => step.StepType == typeof(AsyncOnlyBehavior));
    }

    [Fact]
    public async Task Inspector_NoSteps_ReturnsAnEmptyList()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        using var provider = services.BuildServiceProvider(StrictOptions);
        var inspector = provider.GetRequiredService<IEncinaPipelineInspector>();

        (await inspector.DescribeAsync<Req, string>()).ShouldBeEmpty();
        (await inspector.DescribeStreamAsync<StreamReq, int>()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Inspector_CancelledToken_ThrowsBeforeResolvingAnyStep()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        using var provider = services.BuildServiceProvider(StrictOptions);
        var inspector = provider.GetRequiredService<IEncinaPipelineInspector>();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await inspector.DescribeAsync<Req, string>(cancelled.Token));
        await Should.ThrowAsync<OperationCanceledException>(async () => await inspector.DescribeStreamAsync<StreamReq, int>(cancelled.Token));
    }

    [Fact]
    public async Task Inspector_DescribesAStreamPipeline()
    {
        var services = new ServiceCollection();
        services.AddEncina(configuration => configuration.AddStreamPipelineBehavior<StreamBehavior>(PipelineStage.Caching, 2));
        using var provider = services.BuildServiceProvider(StrictOptions);

        var steps = await provider.GetRequiredService<IEncinaPipelineInspector>().DescribeStreamAsync<StreamReq, int>();

        steps.ShouldBe([new PipelineStepDescription(typeof(StreamBehavior), PipelineStepKind.Behavior, PipelineStage.Caching, 2, PipelineStepSource.Registration)]);
    }

    [Fact]
    public async Task Inspector_TwoAddEncinaCallsPlacingOneTypeDifferently_FailsR6()
    {
        var services = new ServiceCollection();
        services.AddEncina(configuration => configuration.AddPipelineBehavior<AuthBehavior>(PipelineStage.Authorization));
        services.AddEncina(configuration => configuration.AddPipelineBehavior<AuthBehavior>(PipelineStage.Caching));
        using var provider = services.BuildServiceProvider(StrictOptions);
        var inspector = provider.GetRequiredService<IEncinaPipelineInspector>();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await inspector.DescribeAsync<Req, string>());

        exception.Message.ShouldContain("R6");
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new EncinaPipelineInspector(null!, new PipelineOrderResolver(
            new PipelineStageRegistry(), Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineOrderResolver>.Instance)));
        Should.Throw<ArgumentNullException>(() => new EncinaPipelineInspector(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), null!));
    }

    #endregion

    private sealed record Req : IRequest<string>;

    private sealed record Cmd : ICommand<string>;

    private sealed record StreamReq : IStreamRequest<int>;

    private sealed class Handler : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, string>.Right("ok"));
    }

    private sealed class RecordingHandler(List<string> calls) : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult(Either<EncinaError, string>.Right("ok"));
        }
    }

    private abstract class Recorded(string name, List<string> calls) : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            calls.Add(name);
            return nextStep();
        }
    }

    [PipelineStage(PipelineStage.Audit)]
    private sealed class RecordedAudit(List<string> calls) : Recorded("audit", calls);

    [PipelineStage(PipelineStage.Audit)]
    private sealed class RecordedOverridden(List<string> calls) : Recorded("overridden", calls);

    private sealed class AsyncOnlyBehavior : PassThrough, IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private abstract class PassThrough : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class AuthBehavior : PassThrough;

    [PipelineStage(PipelineStage.Audit)]
    private sealed class AuditBehavior : PassThrough;

    [PipelineStage(PipelineStage.Audit)]
    private sealed class OverriddenBehavior : PassThrough;

    private sealed class OpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class CommandBehavior : ICommandPipelineBehavior<Cmd, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Cmd request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class PreProcessor : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class PostProcessor : IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class ErrorProcessor : IRequestErrorProcessor<Req, string>
    {
        public ValueTask Process(Req request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class PostCommitProcessor : IRequestPostCommitProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Req request, IRequestContext context, string response, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class OpenErrorProcessor<TRequest, TResponse> : IRequestErrorProcessor<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask Process(TRequest request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class OpenPostCommitProcessor<TRequest, TResponse> : IRequestPostCommitProcessor<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            TRequest request, IRequestContext context, TResponse response, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class StreamBehavior : IStreamPipelineBehavior<StreamReq, int>
    {
        public IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamReq request, IRequestContext context, StreamHandlerCallback<int> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }
}
