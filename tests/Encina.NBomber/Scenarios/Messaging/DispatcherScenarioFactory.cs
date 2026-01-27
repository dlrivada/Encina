using System.Collections.Concurrent;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.CSharp;
using static LanguageExt.Prelude;

namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Factory for creating Dispatcher load test scenarios.
/// Tests parallel dispatch, sequential dispatch, and pipeline behavior overhead.
/// </summary>
public sealed class DispatcherScenarioFactory
{
    private readonly MessagingScenarioContext _context;
    private readonly ConcurrentDictionary<string, long> _handlerExecutionCounts = new();
    private ServiceProvider? _parallelServiceProvider;
    private ServiceProvider? _sequentialServiceProvider;
    private ServiceProvider? _pipelineServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatcherScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The messaging scenario context.</param>
    public DispatcherScenarioFactory(MessagingScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates all Dispatcher scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateParallelThroughputScenario();
        yield return CreateSequentialThroughputScenario();
        yield return CreatePipelineOverheadScenario();
    }

    /// <summary>
    /// Creates the parallel throughput scenario.
    /// Tests ParallelDispatchStrategy with multiple concurrent handlers.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateParallelThroughputScenario()
    {
        var handlerCount = _context.Options.HandlersPerMessageType;

        return Scenario.Create(
            name: "dispatcher-parallel-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    using var scope = _parallelServiceProvider!.CreateScope();
                    var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                    var notification = new DispatcherLoadTestNotification(
                        _context.NextMessageId(),
                        DateTime.UtcNow,
                        $"Parallel-{scenarioContext.InvocationNumber}");

                    var result = await encina.Publish(notification, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.IsLeft)
                    {
                        var error = result.Match(
                            Left: err => err.Message,
                            Right: _ => "Unknown error");
                        return Response.Fail(error, statusCode: "dispatch_error");
                    }

                    return Response.Ok(statusCode: $"handlers:{handlerCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Parallel dispatch exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                var services = new ServiceCollection();
                services.AddLogging();

                // NOTE: Do NOT use RegisterServicesFromAssemblyContaining here.
                // DispatcherLoadTestHandler has constructor dependencies that can't be auto-resolved.
                // We manually register handlers with factory functions below.
                services.AddEncina(config =>
                {
                    config.NotificationDispatch.Strategy = NotificationDispatchStrategy.Parallel;
                    config.NotificationDispatch.MaxDegreeOfParallelism = _context.Options.MaxDegreeOfParallelism;
                });

                // Register multiple handlers
                for (var i = 0; i < handlerCount; i++)
                {
                    var handlerIndex = i;
                    services.AddScoped<INotificationHandler<DispatcherLoadTestNotification>>(sp =>
                        new DispatcherLoadTestHandler(handlerIndex, _handlerExecutionCounts));
                }

                _parallelServiceProvider = services.BuildServiceProvider();
                return Task.CompletedTask;
            })
            .WithClean(async _ =>
            {
                if (_parallelServiceProvider is not null)
                {
                    await _parallelServiceProvider.DisposeAsync().ConfigureAwait(false);
                }

                LogHandlerDistribution("Parallel");
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the sequential throughput scenario.
    /// Tests SequentialDispatchStrategy baseline for comparison.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateSequentialThroughputScenario()
    {
        var handlerCount = _context.Options.HandlersPerMessageType;

        return Scenario.Create(
            name: "dispatcher-sequential-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    using var scope = _sequentialServiceProvider!.CreateScope();
                    var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                    var notification = new DispatcherLoadTestNotification(
                        _context.NextMessageId(),
                        DateTime.UtcNow,
                        $"Sequential-{scenarioContext.InvocationNumber}");

                    var result = await encina.Publish(notification, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.IsLeft)
                    {
                        var error = result.Match(
                            Left: err => err.Message,
                            Right: _ => "Unknown error");
                        return Response.Fail(error, statusCode: "dispatch_error");
                    }

                    return Response.Ok(statusCode: $"handlers:{handlerCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sequential dispatch exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                var services = new ServiceCollection();
                services.AddLogging();

                // NOTE: Do NOT use RegisterServicesFromAssemblyContaining here.
                // DispatcherLoadTestHandler has constructor dependencies that can't be auto-resolved.
                // We manually register handlers with factory functions below.
                services.AddEncina(config =>
                {
                    config.NotificationDispatch.Strategy = NotificationDispatchStrategy.Sequential;
                });

                // Register multiple handlers
                for (var i = 0; i < handlerCount; i++)
                {
                    var handlerIndex = i + 100; // Offset to distinguish from parallel
                    services.AddScoped<INotificationHandler<DispatcherLoadTestNotification>>(sp =>
                        new DispatcherLoadTestHandler(handlerIndex, _handlerExecutionCounts));
                }

                _sequentialServiceProvider = services.BuildServiceProvider();
                return Task.CompletedTask;
            })
            .WithClean(async _ =>
            {
                if (_sequentialServiceProvider is not null)
                {
                    await _sequentialServiceProvider.DisposeAsync().ConfigureAwait(false);
                }

                LogHandlerDistribution("Sequential");
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the pipeline overhead scenario.
    /// Tests the latency impact of chained pipeline behaviors.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePipelineOverheadScenario()
    {
        return Scenario.Create(
            name: "dispatcher-pipeline-overhead",
            run: async scenarioContext =>
            {
                try
                {
                    using var scope = _pipelineServiceProvider!.CreateScope();
                    var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                    var request = new PipelineLoadTestRequest(
                        _context.NextMessageId(),
                        DateTime.UtcNow);

                    var result = await encina.Send(request, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.IsLeft)
                    {
                        var error = result.Match(
                            Left: err => err.Message,
                            Right: _ => "Unknown error");
                        return Response.Fail(error, statusCode: "pipeline_error");
                    }

                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Pipeline dispatch exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                var services = new ServiceCollection();
                services.AddLogging();

                // Register the handler manually to avoid auto-scan issues
                services.AddScoped<IRequestHandler<PipelineLoadTestRequest, long>, PipelineLoadTestHandler>();

                services.AddEncina(config =>
                {
                    // Add multiple pipeline behaviors to measure cumulative overhead
                    config.AddPipelineBehavior(typeof(TimingBehavior1<,>));
                    config.AddPipelineBehavior(typeof(TimingBehavior2<,>));
                    config.AddPipelineBehavior(typeof(TimingBehavior3<,>));
                });

                _pipelineServiceProvider = services.BuildServiceProvider();
                return Task.CompletedTask;
            })
            .WithClean(async _ =>
            {
                if (_pipelineServiceProvider is not null)
                {
                    await _pipelineServiceProvider.DisposeAsync().ConfigureAwait(false);
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 150,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    private void LogHandlerDistribution(string scenarioType)
    {
        Console.WriteLine($"{scenarioType} handler execution distribution:");
        var relevantKeys = scenarioType == "Parallel"
            ? _handlerExecutionCounts.Keys.Where(k => k.StartsWith("handler-", StringComparison.Ordinal) && int.TryParse(k.Replace("handler-", "", StringComparison.Ordinal), out var idx) && idx < 100)
            : _handlerExecutionCounts.Keys.Where(k => k.StartsWith("handler-", StringComparison.Ordinal) && int.TryParse(k.Replace("handler-", "", StringComparison.Ordinal), out var idx) && idx >= 100);

        foreach (var key in relevantKeys.OrderBy(k => k))
        {
            if (_handlerExecutionCounts.TryGetValue(key, out var count))
            {
                Console.WriteLine($"  {key}: {count} executions");
            }
        }
    }
}

/// <summary>
/// Test notification for dispatcher load testing.
/// </summary>
public sealed record DispatcherLoadTestNotification(long Id, DateTime Timestamp, string Payload) : INotification;

/// <summary>
/// Handler for dispatcher load test notifications.
/// </summary>
public sealed class DispatcherLoadTestHandler : INotificationHandler<DispatcherLoadTestNotification>
{
    private readonly int _handlerIndex;
    private readonly ConcurrentDictionary<string, long> _executionCounts;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public DispatcherLoadTestHandler(int handlerIndex, ConcurrentDictionary<string, long> executionCounts)
    {
        _handlerIndex = handlerIndex;
        _executionCounts = executionCounts;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> Handle(DispatcherLoadTestNotification notification, CancellationToken cancellationToken)
    {
        _executionCounts.AddOrUpdate($"handler-{_handlerIndex}", 1, (_, count) => count + 1);
        return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
    }
}

/// <summary>
/// Test request for pipeline overhead testing.
/// </summary>
public sealed record PipelineLoadTestRequest(long Id, DateTime Timestamp) : IRequest<long>;

/// <summary>
/// Handler for pipeline load test requests.
/// </summary>
public sealed class PipelineLoadTestHandler : IRequestHandler<PipelineLoadTestRequest, long>
{
    /// <inheritdoc />
    public Task<Either<EncinaError, long>> Handle(PipelineLoadTestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, long>(request.Id));
    }
}

/// <summary>
/// Timing behavior 1 for pipeline overhead testing.
/// </summary>
public sealed class TimingBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Minimal overhead - just pass through
        return await nextStep().ConfigureAwait(false);
    }
}

/// <summary>
/// Timing behavior 2 for pipeline overhead testing.
/// </summary>
public sealed class TimingBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        return await nextStep().ConfigureAwait(false);
    }
}

/// <summary>
/// Timing behavior 3 for pipeline overhead testing.
/// </summary>
public sealed class TimingBehavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        return await nextStep().ConfigureAwait(false);
    }
}
