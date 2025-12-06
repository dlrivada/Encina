using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shouldly;
using SimpleMediator;

namespace SimpleMediator.Tests;

public sealed class SimpleMediatorTests
{
    [Fact]
    public async Task Send_ExecutesHandlersThroughConfiguredPipeline()
    {
        var tracker = new PipelineTracker();

        var services = new ServiceCollection();
        services.AddSimpleMediator(cfg =>
        {
            cfg.AddPipelineBehavior(typeof(TrackingBehavior<,>))
               .AddPipelineBehavior(typeof(SecondTrackingBehavior<,>));
        }, typeof(EchoRequest).Assembly);
        services.AddScoped(_ => tracker);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("hola"), CancellationToken.None);

        response.ShouldBe("hola");
        tracker.Events.ShouldBe(new[] { "tracking:before", "second:before", "handler", "second:after", "tracking:after" });
    }

    [Fact]
    public async Task Send_ThrowsInvalidOperation_WhenHandlerNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(EchoRequest).Assembly);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var action = async () => await mediator.Send(new MissingHandlerRequest(), CancellationToken.None);

        var exception = await Should.ThrowAsync<InvalidOperationException>(action);
        exception.Message.ShouldContain("No se encontró un IRequestHandler registrado");
        exception.Message.ShouldContain(nameof(MissingHandlerRequest));
    }

    [Fact]
    public async Task Send_PropagatesHandlerExceptions()
    {
        var services = BuildServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var action = async () => await mediator.Send(new FaultyRequest(), CancellationToken.None);

        var exception = await Should.ThrowAsync<InvalidOperationException>(action);
        exception.Message.ShouldBe("boom");
    }

    [Fact]
    public async Task Send_DetectsCancellation()
    {
        var services = BuildServiceCollection();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = async () => await mediator.Send(new CancellableRequest(), cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(action);
    }

    [Fact]
    public async Task Publish_InvokesAllNotificationHandlers_AndAllowsNullResult()
    {
        var tracker = new NotificationTracker();
        var services = BuildServiceCollection(notificationTracker: tracker);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new SampleNotification(42), CancellationToken.None);

        tracker.Handled.ShouldBe(new[] { "A:42", "B:42" });
    }

    [Fact]
    public async Task Publish_SwallowsWhenNoHandlersRegistered()
    {
        var services = new ServiceCollection();
        services.AddApplicationMessaging(typeof(SimpleMediatorTests).Assembly);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new UnhandledNotification(), CancellationToken.None);
    }

    [Fact]
    public async Task Publish_PropagatesHandlerFailures()
    {
        var tracker = new NotificationTracker();
        var services = BuildServiceCollection(notificationTracker: tracker);
        services.AddScoped<INotificationHandler<SampleNotification>, FaultyNotificationHandler>();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var action = async () => await mediator.Publish(new SampleNotification(7), CancellationToken.None);

        var exception = await Should.ThrowAsync<InvalidOperationException>(action);
        exception.Message.ShouldBe("notify-failure");
    }

    [Fact]
    public async Task Send_WritesDiagnosticLogs()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new EchoRequest("hola"), CancellationToken.None);

        loggerCollector.Entries.ShouldContain(entry => entry.LogLevel == LogLevel.Debug && entry.Message.Contains("Procesando EchoRequest"));
        loggerCollector.Entries.ShouldContain(entry => entry.LogLevel == LogLevel.Debug && entry.Message.Contains("Solicitud EchoRequest completada."));
    }

    [Fact]
    public async Task Send_InvokesPreAndPostProcessors()
    {
        var lifecycleTracker = new LifecycleTracker();
        var services = BuildServiceCollection();
        services.AddScoped(_ => lifecycleTracker);

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new LifecycleRequest("in"), CancellationToken.None);

        response.ShouldBe("in:ok");
        lifecycleTracker.Events.ShouldBe(new[] { "pre", "handler", "post" });
    }

    [Fact]
    public async Task Send_LogsErrorWhenHandlerThrows()
    {
        var loggerCollector = new LoggerCollector();
        var services = BuildServiceCollection(loggerCollector: loggerCollector);
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(new FaultyRequest(), CancellationToken.None);
        await Should.ThrowAsync<InvalidOperationException>(action);

        loggerCollector.Entries.ShouldContain(entry => entry.LogLevel == LogLevel.Error && entry.Message.Contains("Error inesperado procesando FaultyRequest"));
    }

    private static ServiceCollection BuildServiceCollection(
        PipelineTracker? pipelineTracker = null,
        NotificationTracker? notificationTracker = null,
        LoggerCollector? loggerCollector = null,
        Action<SimpleMediatorConfiguration>? configuration = null)
    {
        var services = new ServiceCollection();
        services.AddApplicationMessaging(configuration, typeof(EchoRequest).Assembly);
        services.RemoveAll(typeof(INotificationHandler<SampleNotification>));
        services.AddScoped<INotificationHandler<SampleNotification>, FirstSampleNotificationHandler>();
        services.AddScoped<INotificationHandler<SampleNotification>, SecondSampleNotificationHandler>();

        services.AddScoped(_ => pipelineTracker ?? new PipelineTracker());
        services.AddScoped(_ => notificationTracker ?? new NotificationTracker());

        if (loggerCollector is not null)
        {
            services.AddSingleton(loggerCollector);
            services.AddSingleton<ILogger<global::SimpleMediator.SimpleMediator>>(sp => new ListLogger<global::SimpleMediator.SimpleMediator>(sp.GetRequiredService<LoggerCollector>()));
        }

        return services;
    }

    // Supporting types ------------------------------------------------------

    private sealed record EchoRequest(string Value) : IRequest<string>;

    private sealed class EchoRequestHandler : IRequestHandler<EchoRequest, string>
    {
        private readonly PipelineTracker _tracker;

        public EchoRequestHandler(PipelineTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("handler");
            return Task.FromResult(request.Value);
        }
    }

    private sealed record FaultyRequest() : IRequest<string>;

    private sealed class FaultyRequestHandler : IRequestHandler<FaultyRequest, string>
    {
        public Task<string> Handle(FaultyRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    private sealed record CancellableRequest() : IRequest<string>;

    private sealed class CancellableRequestHandler : IRequestHandler<CancellableRequest, string>
    {
        public Task<string> Handle(CancellableRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult("ok");
        }
    }

    private sealed record LifecycleRequest(string Value) : IRequest<string>;

    private sealed class LifecycleRequestHandler : IRequestHandler<LifecycleRequest, string>
    {
        private readonly LifecycleTracker _tracker;

        public LifecycleRequestHandler(LifecycleTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<string> Handle(LifecycleRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("handler");
            return Task.FromResult(request.Value + ":ok");
        }
    }

    private sealed record SampleNotification(int Value) : INotification;

    private sealed record UnhandledNotification() : INotification;

    private sealed class FirstSampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        private readonly NotificationTracker _tracker;

        public FirstSampleNotificationHandler(NotificationTracker tracker)
        {
            _tracker = tracker;
        }

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _tracker.Handled.Add($"A:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondSampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        private readonly NotificationTracker _tracker;

        public SecondSampleNotificationHandler(NotificationTracker tracker)
        {
            _tracker = tracker;
        }

        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
        {
            _tracker.Handled.Add($"B:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    private sealed class FaultyNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException("notify-failure");
    }

    private sealed record MissingHandlerRequest : IRequest<int>;

    private sealed class TrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly PipelineTracker _tracker;

        public TrackingBehavior(PipelineTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _tracker.Events.Add("tracking:before");
            var response = await next().ConfigureAwait(false);
            _tracker.Events.Add("tracking:after");
            return response;
        }
    }

    private sealed class SecondTrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly PipelineTracker _tracker;

        public SecondTrackingBehavior(PipelineTracker tracker)
        {
            _tracker = tracker;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            _tracker.Events.Add("second:before");
            var response = await next().ConfigureAwait(false);
            _tracker.Events.Add("second:after");
            return response;
        }
    }

    private sealed class LifecyclePreProcessor : IRequestPreProcessor<LifecycleRequest>
    {
        private readonly LifecycleTracker _tracker;

        public LifecyclePreProcessor(LifecycleTracker tracker)
        {
            _tracker = tracker;
        }

        public Task Process(LifecycleRequest request, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("pre");
            return Task.CompletedTask;
        }
    }

    private sealed class LifecyclePostProcessor : IRequestPostProcessor<LifecycleRequest, string>
    {
        private readonly LifecycleTracker _tracker;

        public LifecyclePostProcessor(LifecycleTracker tracker)
        {
            _tracker = tracker;
        }

        public Task Process(LifecycleRequest request, string response, CancellationToken cancellationToken)
        {
            _tracker.Events.Add("post");
            return Task.CompletedTask;
        }
    }

    private sealed class LifecycleTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class PipelineTracker
    {
        public List<string> Events { get; } = new();
    }

    private sealed class NotificationTracker
    {
        public List<string> Handled { get; } = new();
    }

    private sealed class LoggerCollector
    {
        public ConcurrentBag<LogEntry> Entries { get; } = new();
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class ListLogger<T> : ILogger<T>
    {
        private readonly LoggerCollector _collector;

        public ListLogger(LoggerCollector collector)
        {
            _collector = collector;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                return;
            }

            _collector.Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        IDisposable ILogger.BeginScope<TState>(TState state) => NoopScope.Instance;

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
