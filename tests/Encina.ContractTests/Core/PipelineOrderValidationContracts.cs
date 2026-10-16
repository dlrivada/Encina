using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ContractTests.Core;

/// <summary>
/// Contract of the fail-closed pipeline order validation: the rule ids, the enforced rule sets and the startup path,
/// proven on the real <c>AddEncina</c> registrations.
/// </summary>
public sealed class PipelineOrderValidationContracts
{
    private static readonly Type Key = typeof(IPipelineBehavior<Req, string>);

    [Theory]
    [InlineData("R1")]
    [InlineData("R3")]
    [InlineData("R4")]
    [InlineData("R5")]
    [InlineData("R6")]
    [InlineData("R7")]
    public void EveryRule_FailsWithAnInvalidOperationExceptionThatNamesItsId(string ruleId)
    {
        var exception = Should.Throw<InvalidOperationException>(() => Violate(ruleId));

        exception.Message.ShouldStartWith($"Pipeline order rule {ruleId} failed");
    }

    [Fact]
    public void EnforcedRuleSets_StagedExcludesR3AndR4_AllIncludesEveryRule()
    {
        PipelineOrderRuleSet.Staged.HasFlag(PipelineOrderRuleSet.BuiltInStageDeclared).ShouldBeFalse();
        PipelineOrderRuleSet.Staged.HasFlag(PipelineOrderRuleSet.NoDeadRegistrations).ShouldBeFalse();
        Enum.GetValues<PipelineOrderRuleSet>()
            .Where(rule => rule != PipelineOrderRuleSet.None && rule != PipelineOrderRuleSet.Staged && rule != PipelineOrderRuleSet.All)
            .ShouldAllBe(rule => PipelineOrderRuleSet.All.HasFlag(rule));
    }

    [Fact]
    public void BuiltInAssemblies_ListsExactlyTheProjectsUnderSrc()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Encina.slnx")))
        {
            root = root.Parent;
        }

        root.ShouldNotBeNull("the repository root (Encina.slnx) must be found above the test output");
        var projects = new DirectoryInfo(Path.Combine(root.FullName, "src")).GetDirectories()
            .Where(directory => directory.GetFiles("*.csproj").Length > 0)
            .Select(directory => directory.Name);

        PipelineOrderValidator.BuiltInAssemblies.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(projects.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void AddEncina_RegistersOneSnapshotAndOneResolver_EvenWhenCalledTwice()
    {
        var services = new ServiceCollection();

        services.AddEncina();
        services.AddEncina();

        services.Count(descriptor => descriptor.ServiceType == typeof(PipelineRegistrationSnapshot)).ShouldBe(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(PipelineOrderResolver)).ShouldBe(1);
    }

    [Fact]
    public async Task HostedService_OverTheRealRegistrations_FailsStartAsyncOnAnInvalidOrder()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<PipelineStageRegistry>().Set(typeof(Gate), (PipelineStage)7, 0);
        var service = new PipelineOrderValidationHostedService(
            provider,
            provider.GetRequiredService<PipelineRegistrationSnapshot>(),
            provider.GetRequiredService<PipelineOrderResolver>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("R1");
        exception.Message.ShouldContain("Gate");
    }

    [Fact]
    public async Task HostedService_OverTheRealRegistrations_StartsAValidRequestAndStreamPipelineAndWarmsTheCache()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddScoped<IPipelineBehavior<Req, string>, Gate>();
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new Gate());
        services.AddScoped<IRequestPreProcessor<Req>, Pre>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OpenGate<,>));
        services.AddScoped<IStreamRequestHandler<StreamReq, int>, StreamHandler>();
        services.AddScoped<IStreamPipelineBehavior<StreamReq, int>, StreamGate>();
        await using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<PipelineOrderResolver>();
        var service = new PipelineOrderValidationHostedService(
            provider, provider.GetRequiredService<PipelineRegistrationSnapshot>(), resolver);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        resolver.Computations.ShouldBe(2);
    }

    [Fact]
    public async Task HostedService_WithR4Enforced_FailsStartAsyncOnADeadCommandBehaviorRegistration()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(OpenGate<,>));
        await using var provider = services.BuildServiceProvider();
        var snapshot = provider.GetRequiredService<PipelineRegistrationSnapshot>();
        var resolver = new PipelineOrderResolver(
            provider.GetRequiredService<PipelineStageRegistry>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineOrderResolver>.Instance,
            snapshot,
            PipelineOrderRuleSet.All);
        var service = new PipelineOrderValidationHostedService(provider, snapshot, resolver);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        exception.Message.ShouldStartWith("Pipeline order rule R4 failed");
        exception.Message.ShouldContain("ICommandPipelineBehavior");
    }

    [Fact]
    public async Task HostedService_OverTheRealRegistrations_ReportsAThrowingFactoryAsAResolutionError()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>, Handler>();
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => throw new InvalidOperationException("detail"));
        await using var provider = services.BuildServiceProvider();
        var service = new PipelineOrderValidationHostedService(
            provider,
            provider.GetRequiredService<PipelineRegistrationSnapshot>(),
            provider.GetRequiredService<PipelineOrderResolver>());

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        exception.Message.ShouldContain("resolution failed");
        exception.Message.ShouldNotContain("detail");
    }

    private static void Violate(string ruleId)
    {
        Type builtIn = typeof(EncinaMetrics);
        switch (ruleId)
        {
            case "R1":
                PipelineOrderValidator.Validate(Key, [Step(typeof(Gate), (PipelineStage)7)], PipelineOrderRuleSet.All);
                break;
            case "R3":
                PipelineOrderValidator.Validate(
                    Key, [Step(builtIn, PipelineStage.Application, PipelineStepSource.Default)], PipelineOrderRuleSet.All);
                break;
            case "R4":
                PipelineOrderValidator.ValidateRegistrations(
                    [], [ServiceDescriptor.Transient(typeof(ICommandPipelineBehavior<,>), typeof(Gate))], PipelineOrderRuleSet.All);
                break;
            case "R5":
                PipelineOrderValidator.Validate(
                    Key, [Step(builtIn, PipelineStage.Audit), Step(typeof(PipelineOrderResolver), PipelineStage.Audit)], PipelineOrderRuleSet.All);
                break;
            case "R6":
                PipelineOrderValidator.ValidateRegistrations(
                    [new PipelineStageConflict(typeof(Gate), PipelineStage.Audit, 0, PipelineStage.Caching, 0)], [], PipelineOrderRuleSet.All);
                break;
            default:
                PipelineOrderValidator.Validate(
                    Key, [new ResolvedStep(typeof(Gate), PipelineStepKind.PostCommitProcessor, PipelineStage.Application, 0, PipelineStepSource.Registration)],
                    PipelineOrderRuleSet.All);
                break;
        }
    }

    private static ResolvedStep Step(Type type, PipelineStage stage, PipelineStepSource source = PipelineStepSource.Attribute)
        => new(type, PipelineStepKind.Behavior, stage, 0, source);

    private sealed record Req : IRequest<string>;

    private sealed record StreamReq : IStreamRequest<int>;

    private sealed class Pre : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
    }

    private sealed class OpenGate<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
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

    private sealed class StreamGate : IStreamPipelineBehavior<StreamReq, int>
    {
        public IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamReq request, IRequestContext context, StreamHandlerCallback<int> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class Handler : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, string>.Right("handled"));
    }

    private sealed class Gate : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }
}
