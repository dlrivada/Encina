using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.ContractTests;

/// <summary>
/// Contract of the registration-time pipeline API (#2184 Phase 4): what is registered through the stage-aware
/// overloads of <see cref="EncinaConfiguration"/> runs in the pipeline built by <c>AddEncina</c>.
/// </summary>
public sealed class PipelineStageRegistrationContracts
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public async Task Send_WithNoStepsRegistered_RunsTheHandlerDirectly()
    {
        var calls = new List<string>();
        await using var provider = Build(calls, configuration => { });

        var result = await Send(provider);

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["handler"]);
    }

    [Fact]
    public async Task Send_ErrorProcessorRegisteredThroughTheOverload_ObservesTheFailureAndKeepsIt()
    {
        var calls = new List<string>();
        await using var provider = Build(calls, configuration => configuration
            .AddRequestErrorProcessor<RecordingErrorProcessor>(PipelineStage.Observability), failing: true);

        var result = await Send(provider);

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["handler", "error"]);
    }

    [Fact]
    public async Task Send_PostCommitProcessorRegisteredThroughTheOverload_RunsAfterTheHandlerSucceeds()
    {
        var calls = new List<string>();
        await using var provider = Build(calls, configuration => configuration
            .AddRequestPostCommitProcessor<RecordingPostCommitProcessor>(PipelineStage.Transaction));

        var result = await Send(provider);

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["handler", "post-commit"]);
    }

    [Fact]
    public async Task Send_PostCommitProcessorOnAFailedRequest_DoesNotRun()
    {
        var calls = new List<string>();
        await using var provider = Build(calls, configuration => configuration
            .AddRequestPostCommitProcessor<RecordingPostCommitProcessor>(), failing: true);

        var result = await Send(provider);

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["handler"]);
    }

    private static ServiceProvider Build(List<string> calls, Action<EncinaConfiguration> configure, bool failing = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(calls);
        services.AddEncina(configure);
        services.AddScoped<IRequestHandler<Probe, string>>(provider => new ProbeHandler(provider.GetRequiredService<List<string>>(), failing));
        return services.BuildServiceProvider(StrictOptions);
    }

    private static async Task<Either<EncinaError, string>> Send(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new Probe(), CancellationToken.None);
    }

    private sealed record Probe : IRequest<string>;

    private sealed class ProbeHandler(List<string> calls, bool failing) : IRequestHandler<Probe, string>
    {
        public Task<Either<EncinaError, string>> Handle(Probe request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult(failing
                ? Either<EncinaError, string>.Left(EncinaErrors.Create("probe.failed", "failed"))
                : Either<EncinaError, string>.Right("ok"));
        }
    }

    private sealed class RecordingErrorProcessor(List<string> calls) : IRequestErrorProcessor<Probe, string>
    {
        public ValueTask Process(Probe request, IRequestContext context, EncinaError error, CancellationToken cancellationToken)
        {
            calls.Add("error");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingPostCommitProcessor(List<string> calls) : IRequestPostCommitProcessor<Probe, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(
            Probe request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            calls.Add("post-commit");
            return ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
        }
    }
}
