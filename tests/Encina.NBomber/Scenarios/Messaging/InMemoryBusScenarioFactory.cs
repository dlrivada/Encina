using System.Collections.Concurrent;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Factory for creating InMemoryMessageBus load test scenarios.
/// Tests concurrent publishing, handler registration, and multi-handler execution.
/// </summary>
public sealed class InMemoryBusScenarioFactory
{
    private readonly MessagingScenarioContext _context;
    private readonly ConcurrentDictionary<int, List<IDisposable>> _subscriptions = new();
    private readonly ConcurrentDictionary<int, long> _handlerExecutionCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBusScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The messaging scenario context.</param>
    public InMemoryBusScenarioFactory(MessagingScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates all InMemoryMessageBus scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateConcurrentPublishScenario();
        yield return CreateHandlerRegistrationScenario();
        yield return CreateHandlerExecutionScenario();
    }

    /// <summary>
    /// Creates the concurrent publish scenario.
    /// Tests high-rate message publishing throughput (target: 1000+ msg/sec).
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConcurrentPublishScenario()
    {
        return Scenario.Create(
            name: "inmemory-concurrent-publish",
            run: async scenarioContext =>
            {
                try
                {
                    var messageBus = _context.ProviderFactory.CreateMessageBus();
                    if (messageBus is null)
                    {
                        return Response.Fail("Message bus not available", statusCode: "no_bus");
                    }

                    var message = new LoadTestMessage(
                        _context.NextMessageId(),
                        DateTime.UtcNow,
                        $"Payload-{scenarioContext.InvocationNumber}");

                    var result = await messageBus.PublishAsync(message, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.IsLeft)
                    {
                        var error = result.Match(
                            Left: err => err.Message,
                            Right: _ => "Unknown error");
                        return Response.Fail(error, statusCode: "publish_error");
                    }

                    return Response.Ok();
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                // Pre-register a subscriber to receive messages
                var messageBus = _context.ProviderFactory.CreateMessageBus();
                if (messageBus is not null)
                {
                    var subscription = messageBus.Subscribe<LoadTestMessage>(_ => ValueTask.CompletedTask);
                    _subscriptions.TryAdd(0, [subscription]);
                }

                return Task.CompletedTask;
            })
            .WithClean(_ =>
            {
                // Clean up subscriptions
                if (_subscriptions.TryRemove(0, out var subs))
                {
                    foreach (var sub in subs)
                    {
                        sub.Dispose();
                    }
                }

                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the handler registration scenario.
    /// Tests concurrent subscribe/unsubscribe operations under load.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateHandlerRegistrationScenario()
    {
        return Scenario.Create(
            name: "inmemory-handler-registration",
            run: async scenarioContext =>
            {
                try
                {
                    var messageBus = _context.ProviderFactory.CreateMessageBus();
                    if (messageBus is null)
                    {
                        return Response.Fail("Message bus not available", statusCode: "no_bus");
                    }

                    var handlerId = _context.NextHandlerId();
                    var bucketId = (int)(handlerId % 100);

                    // Subscribe a new handler
                    var subscription = messageBus.Subscribe<LoadTestMessage>(msg =>
                    {
                        _handlerExecutionCounts.AddOrUpdate(bucketId, 1, (_, count) => count + 1);
                        return ValueTask.CompletedTask;
                    });

                    // Track subscription for cleanup
                    var bucket = _subscriptions.GetOrAdd(bucketId, _ => []);
                    lock (bucket)
                    {
                        bucket.Add(subscription);
                    }

                    // Randomly unsubscribe some handlers to stress the ConcurrentDictionary
                    if (scenarioContext.InvocationNumber % 5 == 0 && bucket.Count > 1)
                    {
                        IDisposable? subToRemove = null;
                        lock (bucket)
                        {
                            if (bucket.Count > 1)
                            {
                                subToRemove = bucket[0];
                                bucket.RemoveAt(0);
                            }
                        }

                        subToRemove?.Dispose();
                    }

                    // Also publish a message to test concurrent access
                    var message = new LoadTestMessage(
                        _context.NextMessageId(),
                        DateTime.UtcNow,
                        "RegistrationTest");

                    await messageBus.PublishAsync(message, CancellationToken.None)
                        .ConfigureAwait(false);

                    return Response.Ok(statusCode: $"handlers:{messageBus.SubscriberCount}");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                // Clean up all subscriptions
                foreach (var kvp in _subscriptions)
                {
                    foreach (var sub in kvp.Value)
                    {
                        sub.Dispose();
                    }
                }

                _subscriptions.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the handler execution scenario.
    /// Tests multiple handlers per message type with worker pool distribution.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateHandlerExecutionScenario()
    {
        var handlerCount = _context.Options.HandlersPerMessageType;

        return Scenario.Create(
            name: "inmemory-handler-execution",
            run: async scenarioContext =>
            {
                try
                {
                    var messageBus = _context.ProviderFactory.CreateMessageBus();
                    if (messageBus is null)
                    {
                        return Response.Fail("Message bus not available", statusCode: "no_bus");
                    }

                    var message = new LoadTestMessage(
                        _context.NextMessageId(),
                        DateTime.UtcNow,
                        $"MultiHandler-{scenarioContext.InvocationNumber}");

                    var result = await messageBus.PublishAsync(message, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.IsLeft)
                    {
                        var error = result.Match(
                            Left: err => err.Message,
                            Right: _ => "Unknown error");
                        return Response.Fail(error, statusCode: "publish_error");
                    }

                    return Response.Ok(statusCode: $"handlers:{handlerCount}");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                // Register multiple handlers for the same message type
                var messageBus = _context.ProviderFactory.CreateMessageBus();
                if (messageBus is null)
                {
                    return Task.CompletedTask;
                }

                var subscriptions = new List<IDisposable>();
                for (var i = 0; i < handlerCount; i++)
                {
                    var handlerIndex = i;
                    var subscription = messageBus.Subscribe<LoadTestMessage>(msg =>
                    {
                        // Simulate some work
                        _handlerExecutionCounts.AddOrUpdate(handlerIndex, 1, (_, count) => count + 1);
                        return ValueTask.CompletedTask;
                    });
                    subscriptions.Add(subscription);
                }

                _subscriptions.TryAdd(999, subscriptions);
                return Task.CompletedTask;
            })
            .WithClean(_ =>
            {
                if (_subscriptions.TryRemove(999, out var subs))
                {
                    foreach (var sub in subs)
                    {
                        sub.Dispose();
                    }
                }

                // Log handler execution distribution
                Console.WriteLine("Handler execution distribution:");
                foreach (var kvp in _handlerExecutionCounts.OrderBy(k => k.Key))
                {
                    Console.WriteLine($"  Handler {kvp.Key}: {kvp.Value} executions");
                }

                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 150,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}

/// <summary>
/// Test message for load testing scenarios.
/// </summary>
/// <param name="Id">Unique message identifier.</param>
/// <param name="Timestamp">Message creation timestamp.</param>
/// <param name="Payload">Message payload data.</param>
public sealed record LoadTestMessage(long Id, DateTime Timestamp, string Payload);
