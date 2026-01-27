using System.Collections.Concurrent;
using Encina.NBomber.Scenarios.Brokers.Providers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Factory for creating NATS load test scenarios.
/// Tests core NATS publishing, request/reply, and JetStream persistent publishing.
/// </summary>
public sealed class NATSScenarioFactory
{
    private readonly BrokerScenarioContext _context;
    private NATSProviderFactory? _natsFactory;
    private readonly ConcurrentDictionary<string, long> _metrics = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NATSScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The broker scenario context.</param>
    public NATSScenarioFactory(BrokerScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _natsFactory = context.ProviderFactory as NATSProviderFactory;
    }

    /// <summary>
    /// Creates all NATS scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreatePublishThroughputScenario();
        yield return CreateRequestReplyScenario();
        yield return CreateJetStreamPublishScenario();
    }

    /// <summary>
    /// Creates the publish throughput scenario.
    /// Tests core NATS at-most-once delivery with maximum publishing rate.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePublishThroughputScenario()
    {
        var subject = $"{_context.Options.TopicPrefix}.publish.throughput";

        return Scenario.Create(
            name: "nats-publish-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    var connection = _natsFactory?.Connection;
                    if (connection is null)
                    {
                        return Response.Fail("Connection not initialized", statusCode: "no_connection");
                    }

                    var message = _context.CreateTestMessage();

                    await connection.PublishAsync(subject, message).ConfigureAwait(false);

                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "published");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ => Task.CompletedTask)
            .WithClean(_ =>
            {
                var published = _metrics.GetValueOrDefault("published", 0);
                Console.WriteLine($"NATS publish throughput - Published: {published}");
                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 500,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the request/reply scenario.
    /// Measures request/reply latency under load.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateRequestReplyScenario()
    {
        var subject = $"{_context.Options.TopicPrefix}.request.reply";
        NatsConnection? responderConnection = null;
        CancellationTokenSource? subscriptionCts = null;
        Task? responderTask = null;

        return Scenario.Create(
            name: "nats-request-reply",
            run: async scenarioContext =>
            {
                try
                {
                    var connection = _natsFactory?.Connection;
                    if (connection is null)
                    {
                        return Response.Fail("Connection not initialized", statusCode: "no_connection");
                    }

                    var request = _context.CreateTestMessage();

                    var response = await connection.RequestAsync<byte[], byte[]>(
                        subject,
                        request,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (response.Data is not null)
                    {
                        _metrics.AddOrUpdate("replies", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "reply_received");
                    }

                    _metrics.AddOrUpdate("empty_replies", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "empty_reply");
                }
                catch (NatsNoRespondersException)
                {
                    _metrics.AddOrUpdate("no_responders", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "no_responders");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                // Create a responder connection
                responderConnection = await _natsFactory!.CreateConnectionAsync().ConfigureAwait(false);
                subscriptionCts = new CancellationTokenSource();

                // Start responder in background
                responderTask = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var msg in responderConnection.SubscribeAsync<byte[]>(subject)
                            .WithCancellation(subscriptionCts.Token))
                        {
                            var response = _context.CreateTestMessage();
                            await msg.ReplyAsync(response).ConfigureAwait(false);
                            _metrics.AddOrUpdate("requests_handled", 1, (_, c) => c + 1);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected on shutdown
                    }
                }, subscriptionCts.Token);

                // Give responder time to subscribe
                await Task.Delay(500).ConfigureAwait(false);
            })
            .WithClean(async _ =>
            {
                var replies = _metrics.GetValueOrDefault("replies", 0);
                var emptyReplies = _metrics.GetValueOrDefault("empty_replies", 0);
                var noResponders = _metrics.GetValueOrDefault("no_responders", 0);
                var handled = _metrics.GetValueOrDefault("requests_handled", 0);

                Console.WriteLine($"NATS request/reply - Replies: {replies}, Empty: {emptyReplies}, " +
                    $"No responders: {noResponders}, Handled: {handled}");
                _metrics.Clear();

                subscriptionCts?.Cancel();
                if (responderTask is not null)
                {
                    try { await responderTask.ConfigureAwait(false); } catch { }
                }

                subscriptionCts?.Dispose();
                if (responderConnection is not null)
                {
                    await responderConnection.DisposeAsync().ConfigureAwait(false);
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the JetStream publish scenario.
    /// Tests JetStream with acknowledgments and persistence.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateJetStreamPublishScenario()
    {
        var streamName = $"{_context.Options.TopicPrefix}_jetstream";
        var subject = $"{_context.Options.TopicPrefix}.jetstream.publish";
        INatsJSContext? jsContext = null;

        return Scenario.Create(
            name: "nats-jetstream-publish",
            run: async scenarioContext =>
            {
                try
                {
                    if (jsContext is null)
                    {
                        return Response.Fail("JetStream not initialized", statusCode: "no_jetstream");
                    }

                    var message = _context.CreateTestMessage();

                    var ack = await jsContext.PublishAsync(subject, message).ConfigureAwait(false);

                    if (ack.Duplicate)
                    {
                        _metrics.AddOrUpdate("duplicates", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "duplicate");
                    }

                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "acked");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                // Create stream
                await _natsFactory!.CreateStreamAsync(streamName, subject).ConfigureAwait(false);
                jsContext = _natsFactory.CreateJetStreamContext();
            })
            .WithClean(_ =>
            {
                var published = _metrics.GetValueOrDefault("published", 0);
                var duplicates = _metrics.GetValueOrDefault("duplicates", 0);
                Console.WriteLine($"NATS JetStream publish - Published: {published}, Duplicates: {duplicates}");
                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
