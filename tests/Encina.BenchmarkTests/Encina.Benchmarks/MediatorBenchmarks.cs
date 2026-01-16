using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Encina.Benchmarks.Inbox;
using Encina.Benchmarks.Outbox;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks;

public static class Program
{
    public static void Main(string[] _)
    {
        var config = DefaultConfig.Instance
            .WithArtifactsPath(Path.Combine(
                FindRepositoryRoot(),
                "artifacts",
                "performance"));

        // Run all benchmark suites
        BenchmarkRunner.Run<EncinaBenchmarks>(config);
        BenchmarkRunner.Run<DelegateInvocationBenchmarks>(config);
        BenchmarkRunner.Run<CacheOptimizationBenchmarks>(config);
        BenchmarkRunner.Run<StreamRequestBenchmarks>(config);

        // Validation benchmarks (FluentValidation vs DataAnnotations vs MiniValidator vs GuardClauses)
        BenchmarkRunner.Run<ValidationBenchmarks>(config);

        // Job Scheduling benchmarks (Hangfire vs Quartz)
        BenchmarkRunner.Run<JobSchedulingBenchmarks>(config);

        // OpenTelemetry benchmarks
        BenchmarkRunner.Run<OpenTelemetryBenchmarks>(config);

        // Outbox benchmarks (Dapper vs EF Core)
        BenchmarkRunner.Run<OutboxDapperBenchmarks>(config);
        BenchmarkRunner.Run<OutboxEfCoreBenchmarks>(config);

        // Inbox benchmarks (Dapper vs EF Core)
        BenchmarkRunner.Run<InboxDapperBenchmarks>(config);
        BenchmarkRunner.Run<InboxEfCoreBenchmarks>(config);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Encina.slnx");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Encina.slnx.");
    }
}

[MemoryDiagnoser]
public class EncinaBenchmarks
{
    private IServiceProvider _provider = default!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddScoped<CallRecorder>();
        services.AddEncina(options =>
        {
            options.AddPipelineBehavior(typeof(TracingPipelineBehavior<,>));
            options.AddRequestPreProcessor(typeof(TracingPreProcessor<>));
            options.AddRequestPostProcessor(typeof(TracingPostProcessor<,>));
        }, typeof(Encina).Assembly, typeof(EncinaBenchmarks).Assembly);

        services.AddScoped<IRequestHandler<SampleCommand, int>, SampleCommandHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, NotificationHandlerOne>();
        services.AddScoped<INotificationHandler<SampleNotification>, NotificationHandlerTwo>();

        _provider = services.BuildServiceProvider();
    }

    [Benchmark]
    public async Task<int> Send_Command_WithInstrumentation()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var command = new SampleCommand(Guid.NewGuid());
        var outcome = await Encina.Send(command).ConfigureAwait(false);
        return outcome.Match(
            Left: error => throw new InvalidOperationException($"Sample command failed: {error.Message}"),
            Right: value => value);
    }

    [Benchmark]
    public async Task<int> Publish_Notification_WithMultipleHandlers()
    {
        using var scope = _provider.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        await Encina.Publish(new SampleNotification(Guid.NewGuid())).ConfigureAwait(false);
        return scope.ServiceProvider.GetRequiredService<CallRecorder>().InvocationCount;
    }

    private sealed record SampleCommand(Guid RequestId) : ICommand<int>;

    private sealed class SampleCommandHandler : ICommandHandler<SampleCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(SampleCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, int>(request.RequestId.GetHashCode()));
        }
    }

    private sealed record SampleNotification(Guid NotificationId) : INotification;

    private sealed class NotificationHandlerOne(CallRecorder recorder) : INotificationHandler<SampleNotification>
    {
        private readonly CallRecorder _recorder = recorder;

        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Register("handler-one");
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class NotificationHandlerTwo(CallRecorder recorder) : INotificationHandler<SampleNotification>
    {
        private readonly CallRecorder _recorder = recorder;

        public Task<Either<EncinaError, Unit>> Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Register("handler-two");
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class TracingPipelineBehavior<TRequest, TResponse>(CallRecorder recorder) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly CallRecorder _recorder = recorder;

        public async ValueTask<Either<EncinaError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        {
            _recorder.Register("pipeline:enter");
            try
            {
                return await nextStep().ConfigureAwait(false);
            }
            finally
            {
                _recorder.Register("pipeline:exit");
            }
        }
    }

    private sealed class TracingPreProcessor<TRequest>(CallRecorder recorder) : IRequestPreProcessor<TRequest>
    {
        private readonly CallRecorder _recorder = recorder;

        public Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken)
        {
            _recorder.Register("pre");
            return Task.CompletedTask;
        }
    }

    private sealed class TracingPostProcessor<TRequest, TResponse>(CallRecorder recorder) : IRequestPostProcessor<TRequest, TResponse>
    {
        private readonly CallRecorder _recorder = recorder;

        public Task Process(TRequest request, IRequestContext context, Either<EncinaError, TResponse> response, CancellationToken cancellationToken)
        {
            _recorder.Register("post");
            return Task.CompletedTask;
        }
    }

    private sealed class CallRecorder
    {
        private int _count;

        public void Register(string marker)
        {
            _ = marker;
            Interlocked.Increment(ref _count);
        }

        public int InvocationCount => _count;
    }
}
