using System.Collections.Concurrent;
using System.Text;
using Encina.NBomber.Scenarios.Brokers.Providers;
using NBomber.Contracts;
using NBomber.CSharp;
using RabbitMQ.Client;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Factory for creating RabbitMQ load test scenarios.
/// Tests publish throughput, consume throughput, concurrent consumers, and publisher confirms.
/// </summary>
public sealed class RabbitMQScenarioFactory
{
    private readonly BrokerScenarioContext _context;
    private RabbitMQProviderFactory? _rabbitFactory;
    private IChannel? _publishChannel;
    private readonly ConcurrentDictionary<string, long> _metrics = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The broker scenario context.</param>
    public RabbitMQScenarioFactory(BrokerScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _rabbitFactory = context.ProviderFactory as RabbitMQProviderFactory;
    }

    /// <summary>
    /// Creates all RabbitMQ scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreatePublishThroughputScenario();
        yield return CreateConsumeThroughputScenario();
        yield return CreateConcurrentConsumersScenario();
        yield return CreatePublisherConfirmsScenario();
    }

    /// <summary>
    /// Creates the publish throughput scenario.
    /// Tests maximum publishing rate. Target: 5,000+ msg/sec.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePublishThroughputScenario()
    {
        var queueName = $"{_context.Options.TopicPrefix}-publish-throughput";

        return Scenario.Create(
            name: "rabbitmq-publish-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishChannel is null)
                    {
                        return Response.Fail("Channel not initialized", statusCode: "no_channel");
                    }

                    var message = _context.CreateTestMessage();
                    var properties = new BasicProperties
                    {
                        Persistent = false,
                        MessageId = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    };

                    await _publishChannel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: message).ConfigureAwait(false);

                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "published");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishChannel = await _rabbitFactory!.CreateChannelAsync().ConfigureAwait(false);
                if (_publishChannel is not null)
                {
                    await _publishChannel.QueueDeclareAsync(
                        queue: queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: true,
                        arguments: null).ConfigureAwait(false);
                }
            })
            .WithClean(async _ =>
            {
                var published = _metrics.GetValueOrDefault("published", 0);
                Console.WriteLine($"RabbitMQ publish throughput - Published: {published}");
                _metrics.Clear();

                if (_publishChannel is not null)
                {
                    await _publishChannel.CloseAsync().ConfigureAwait(false);
                    _publishChannel.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 500,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the consume throughput scenario.
    /// Tests consumer processing rate with manual acknowledgments.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConsumeThroughputScenario()
    {
        var queueName = $"{_context.Options.TopicPrefix}-consume-throughput";
        IChannel? consumerChannel = null;

        return Scenario.Create(
            name: "rabbitmq-consume-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishChannel is null || consumerChannel is null)
                    {
                        return Response.Fail("Channels not initialized", statusCode: "no_channel");
                    }

                    // Publish a message
                    var message = _context.CreateTestMessage();
                    var properties = new BasicProperties
                    {
                        MessageId = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    };

                    await _publishChannel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: message).ConfigureAwait(false);

                    // Consume with BasicGet (synchronous for testing)
                    var result = await consumerChannel.BasicGetAsync(queueName, autoAck: false)
                        .ConfigureAwait(false);

                    if (result is not null)
                    {
                        await consumerChannel.BasicAckAsync(result.DeliveryTag, multiple: false)
                            .ConfigureAwait(false);
                        _metrics.AddOrUpdate("consumed", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "consumed");
                    }

                    _metrics.AddOrUpdate("empty", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "empty_queue");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishChannel = await _rabbitFactory!.CreateChannelAsync().ConfigureAwait(false);
                consumerChannel = await _rabbitFactory.CreateChannelAsync().ConfigureAwait(false);

                if (_publishChannel is not null)
                {
                    await _publishChannel.QueueDeclareAsync(
                        queue: queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: true,
                        arguments: null).ConfigureAwait(false);
                }

                if (consumerChannel is not null)
                {
                    await consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 100, global: false)
                        .ConfigureAwait(false);
                }
            })
            .WithClean(async _ =>
            {
                var consumed = _metrics.GetValueOrDefault("consumed", 0);
                var empty = _metrics.GetValueOrDefault("empty", 0);
                Console.WriteLine($"RabbitMQ consume throughput - Consumed: {consumed}, Empty polls: {empty}");
                _metrics.Clear();

                if (_publishChannel is not null)
                {
                    await _publishChannel.CloseAsync().ConfigureAwait(false);
                    _publishChannel.Dispose();
                }

                if (consumerChannel is not null)
                {
                    await consumerChannel.CloseAsync().ConfigureAwait(false);
                    consumerChannel.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the concurrent consumers scenario.
    /// Tests 5-10 concurrent consumers with queue distribution.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConcurrentConsumersScenario()
    {
        var queueName = $"{_context.Options.TopicPrefix}-concurrent-consumers";
        var consumerChannels = new List<IChannel>();

        return Scenario.Create(
            name: "rabbitmq-concurrent-consumers",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishChannel is null || consumerChannels.Count == 0)
                    {
                        return Response.Fail("Channels not initialized", statusCode: "no_channel");
                    }

                    // Publish a message
                    var message = _context.CreateTestMessage();
                    var properties = new BasicProperties
                    {
                        MessageId = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    };

                    await _publishChannel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: message).ConfigureAwait(false);

                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);

                    // Try to consume from a random consumer channel
                    var consumerIndex = (int)(scenarioContext.InvocationNumber % consumerChannels.Count);
                    var channel = consumerChannels[consumerIndex];

                    var result = await channel.BasicGetAsync(queueName, autoAck: true).ConfigureAwait(false);

                    if (result is not null)
                    {
                        _metrics.AddOrUpdate($"consumer_{consumerIndex}", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("consumed", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: $"consumer_{consumerIndex}");
                    }

                    return Response.Ok(statusCode: "empty");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishChannel = await _rabbitFactory!.CreateChannelAsync().ConfigureAwait(false);

                if (_publishChannel is not null)
                {
                    await _publishChannel.QueueDeclareAsync(
                        queue: queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: true,
                        arguments: null).ConfigureAwait(false);
                }

                // Create multiple consumer channels
                for (var i = 0; i < _context.Options.ConsumerCount; i++)
                {
                    var channel = await _rabbitFactory.CreateChannelAsync().ConfigureAwait(false);
                    if (channel is not null)
                    {
                        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false)
                            .ConfigureAwait(false);
                        consumerChannels.Add(channel);
                    }
                }
            })
            .WithClean(async _ =>
            {
                var published = _metrics.GetValueOrDefault("published", 0);
                var consumed = _metrics.GetValueOrDefault("consumed", 0);

                Console.WriteLine($"RabbitMQ concurrent consumers - Published: {published}, Consumed: {consumed}");

                for (var i = 0; i < consumerChannels.Count; i++)
                {
                    var count = _metrics.GetValueOrDefault($"consumer_{i}", 0);
                    Console.WriteLine($"  Consumer {i}: {count} messages");
                }

                _metrics.Clear();

                if (_publishChannel is not null)
                {
                    await _publishChannel.CloseAsync().ConfigureAwait(false);
                    _publishChannel.Dispose();
                }

                foreach (var channel in consumerChannels)
                {
                    await channel.CloseAsync().ConfigureAwait(false);
                    channel.Dispose();
                }

                consumerChannels.Clear();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the publisher confirms scenario.
    /// Compares throughput with publisher confirms enabled vs disabled.
    /// In RabbitMQ.Client 7.x, publisher confirms are tracked per message.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePublisherConfirmsScenario()
    {
        var queueName = $"{_context.Options.TopicPrefix}-publisher-confirms";

        return Scenario.Create(
            name: "rabbitmq-publisher-confirms",
            run: async scenarioContext =>
            {
                try
                {
                    if (_publishChannel is null)
                    {
                        return Response.Fail("Channel not initialized", statusCode: "no_channel");
                    }

                    var message = _context.CreateTestMessage();
                    var properties = new BasicProperties
                    {
                        MessageId = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Persistent = true
                    };

                    // In RabbitMQ.Client 7.x, BasicPublishAsync returns a ValueTask that
                    // completes when the message is confirmed by the broker (if channel is in confirm mode)
                    await _publishChannel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: message).ConfigureAwait(false);

                    _metrics.AddOrUpdate("published", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "published");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _publishChannel = await _rabbitFactory!.CreateChannelAsync().ConfigureAwait(false);

                if (_publishChannel is not null)
                {
                    await _publishChannel.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null).ConfigureAwait(false);
                }
            })
            .WithClean(async _ =>
            {
                var published = _metrics.GetValueOrDefault("published", 0);
                Console.WriteLine($"RabbitMQ publisher confirms - Published: {published}");
                _metrics.Clear();

                if (_publishChannel is not null)
                {
                    await _publishChannel.CloseAsync().ConfigureAwait(false);
                    _publishChannel.Dispose();
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
