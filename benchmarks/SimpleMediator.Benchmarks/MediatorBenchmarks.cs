using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using LanguageExt;

namespace SimpleMediator.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MediatorBenchmarks>();
    }
}

[MemoryDiagnoser]
public class MediatorBenchmarks
{
    private IServiceProvider _provider = default!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddScoped<CallRecorder>();
        services.AddSimpleMediator(options =>
        {
            options.AddPipelineBehavior(typeof(TracingPipelineBehavior<,>));
            options.AddRequestPreProcessor(typeof(TracingPreProcessor<>));
            options.AddRequestPostProcessor(typeof(TracingPostProcessor<,>));
        }, typeof(SimpleMediator).Assembly, typeof(MediatorBenchmarks).Assembly);

        services.AddScoped<IRequestHandler<SampleCommand, int>, SampleCommandHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, NotificationHandlerOne>();
        services.AddScoped<INotificationHandler<SampleNotification>, NotificationHandlerTwo>();

        _provider = services.BuildServiceProvider();
    }

    [Benchmark]
    public async Task<int> Send_Command_WithInstrumentation()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new SampleCommand(Guid.NewGuid());
        var outcome = await mediator.Send(command).ConfigureAwait(false);
        return outcome.Match(
            Left: error => throw new InvalidOperationException($"Sample command failed: {error.Message}"),
            Right: value => value);
    }

    [Benchmark]
    public async Task<int> Publish_Notification_WithMultipleHandlers()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(new SampleNotification(Guid.NewGuid())).ConfigureAwait(false);
        return scope.ServiceProvider.GetRequiredService<CallRecorder>().InvocationCount;
    }

    private sealed record SampleCommand(Guid RequestId) : ICommand<int>;

    private sealed class SampleCommandHandler : ICommandHandler<SampleCommand, int>
    {
        public Task<int> Handle(SampleCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.RequestId.GetHashCode());
        }
    }

    private sealed record SampleNotification(Guid NotificationId) : INotification;

    private sealed class NotificationHandlerOne : INotificationHandler<SampleNotification>
    {
        private readonly CallRecorder _recorder;

        public NotificationHandlerOne(CallRecorder recorder) => _recorder = recorder;

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Register("handler-one");
            return Task.CompletedTask;
        }
    }

    private sealed class NotificationHandlerTwo : INotificationHandler<SampleNotification>
    {
        private readonly CallRecorder _recorder;

        public NotificationHandlerTwo(CallRecorder recorder) => _recorder = recorder;

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _recorder.Register("handler-two");
            return Task.CompletedTask;
        }
    }

    private sealed class TracingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly CallRecorder _recorder;

        public TracingPipelineBehavior(CallRecorder recorder) => _recorder = recorder;

        public async Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _recorder.Register("pipeline:enter");
            try
            {
                return await next().ConfigureAwait(false);
            }
            finally
            {
                _recorder.Register("pipeline:exit");
            }
        }
    }

    private sealed class TracingPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        private readonly CallRecorder _recorder;

        public TracingPreProcessor(CallRecorder recorder) => _recorder = recorder;

        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            _recorder.Register("pre");
            return Task.CompletedTask;
        }
    }

    private sealed class TracingPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        private readonly CallRecorder _recorder;

        public TracingPostProcessor(CallRecorder recorder) => _recorder = recorder;

        public Task Process(TRequest request, Either<Error, TResponse> response, CancellationToken cancellationToken)
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
