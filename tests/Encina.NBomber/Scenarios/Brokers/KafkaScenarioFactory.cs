using System.Collections.Concurrent;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Encina.NBomber.Scenarios.Brokers.Providers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Factory for creating Kafka load test scenarios.
/// Tests produce throughput, batch production, consume throughput, and partition distribution.
/// </summary>
public sealed class KafkaScenarioFactory
{
    private readonly BrokerScenarioContext _context;
    private KafkaProviderFactory? _kafkaFactory;
    private IProducer<string, byte[]>? _producer;
    private readonly ConcurrentDictionary<string, long> _metrics = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The broker scenario context.</param>
    public KafkaScenarioFactory(BrokerScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _kafkaFactory = context.ProviderFactory as KafkaProviderFactory;
    }

    /// <summary>
    /// Creates all Kafka scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateProduceThroughputScenario();
        yield return CreateBatchProduceThroughputScenario();
        yield return CreateConsumeThroughputScenario();
        yield return CreatePartitionDistributionScenario();
    }

    /// <summary>
    /// Creates the produce throughput scenario.
    /// Tests single-message production rate.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateProduceThroughputScenario()
    {
        var topicName = $"{_context.Options.TopicPrefix}-produce-throughput";

        return Scenario.Create(
            name: "kafka-produce-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_producer is null)
                    {
                        return Response.Fail("Producer not initialized", statusCode: "no_producer");
                    }

                    var message = _context.CreateTestMessage();
                    var key = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    var result = await _producer.ProduceAsync(
                        topicName,
                        new Message<string, byte[]> { Key = key, Value = message }).ConfigureAwait(false);

                    if (result.Status == PersistenceStatus.Persisted)
                    {
                        _metrics.AddOrUpdate("produced", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "produced");
                    }

                    _metrics.AddOrUpdate("not_persisted", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "not_persisted");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _producer = _kafkaFactory!.CreateProducer();

                // Create topic
                using var adminClient = new AdminClientBuilder(_kafkaFactory.CreateAdminClientConfig()).Build();
                try
                {
                    await adminClient.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = topicName,
                            NumPartitions = _context.Options.PartitionCount,
                            ReplicationFactor = 1
                        }
                    ]).ConfigureAwait(false);
                }
                catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    // Topic already exists, ignore
                }
            })
            .WithClean(_ =>
            {
                var produced = _metrics.GetValueOrDefault("produced", 0);
                var notPersisted = _metrics.GetValueOrDefault("not_persisted", 0);
                Console.WriteLine($"Kafka produce throughput - Produced: {produced}, Not persisted: {notPersisted}");
                _metrics.Clear();

                _producer?.Dispose();
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
    /// Creates the batch produce throughput scenario.
    /// Compares batch production vs individual message throughput.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateBatchProduceThroughputScenario()
    {
        var topicName = $"{_context.Options.TopicPrefix}-batch-produce";

        return Scenario.Create(
            name: "kafka-batch-produce-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_producer is null)
                    {
                        return Response.Fail("Producer not initialized", statusCode: "no_producer");
                    }

                    var useBatch = scenarioContext.InvocationNumber % 2 == 0;

                    if (useBatch)
                    {
                        // Batch production - fire and forget, then flush
                        var batchSize = _context.Options.BatchSize;
                        for (var i = 0; i < batchSize; i++)
                        {
                            var message = _context.CreateTestMessage();
                            var key = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture);

                            _producer.Produce(
                                topicName,
                                new Message<string, byte[]> { Key = key, Value = message });
                        }

                        _producer.Flush(TimeSpan.FromSeconds(10));
                        _metrics.AddOrUpdate("batch_messages", batchSize, (_, c) => c + batchSize);
                        _metrics.AddOrUpdate("batches", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "batch");
                    }
                    else
                    {
                        // Individual production
                        var message = _context.CreateTestMessage();
                        var key = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture);

                        await _producer.ProduceAsync(
                            topicName,
                            new Message<string, byte[]> { Key = key, Value = message }).ConfigureAwait(false);

                        _metrics.AddOrUpdate("individual_messages", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "individual");
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _producer = _kafkaFactory!.CreateProducer();

                using var adminClient = new AdminClientBuilder(_kafkaFactory.CreateAdminClientConfig()).Build();
                try
                {
                    await adminClient.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = topicName,
                            NumPartitions = _context.Options.PartitionCount,
                            ReplicationFactor = 1
                        }
                    ]).ConfigureAwait(false);
                }
                catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    // Topic already exists
                }
            })
            .WithClean(_ =>
            {
                var batchMessages = _metrics.GetValueOrDefault("batch_messages", 0);
                var batches = _metrics.GetValueOrDefault("batches", 0);
                var individual = _metrics.GetValueOrDefault("individual_messages", 0);
                var avgBatchSize = batches > 0 ? batchMessages / batches : 0;

                Console.WriteLine($"Kafka batch produce - Batches: {batches} (total msgs: {batchMessages}, avg: {avgBatchSize}), Individual: {individual}");
                _metrics.Clear();

                _producer?.Dispose();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the consume throughput scenario.
    /// Tests consumer group message processing rate.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConsumeThroughputScenario()
    {
        var topicName = $"{_context.Options.TopicPrefix}-consume-throughput";
        IConsumer<string, byte[]>? consumer = null;

        return Scenario.Create(
            name: "kafka-consume-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_producer is null || consumer is null)
                    {
                        return Response.Fail("Producer/Consumer not initialized", statusCode: "no_client");
                    }

                    // Produce a message first
                    var message = _context.CreateTestMessage();
                    var key = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    await _producer.ProduceAsync(
                        topicName,
                        new Message<string, byte[]> { Key = key, Value = message }).ConfigureAwait(false);

                    _metrics.AddOrUpdate("produced", 1, (_, c) => c + 1);

                    // Try to consume
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));

                    if (result is not null && !result.IsPartitionEOF)
                    {
                        consumer.Commit(result);
                        _metrics.AddOrUpdate("consumed", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "consumed");
                    }

                    return Response.Ok(statusCode: "no_message");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _producer = _kafkaFactory!.CreateProducer();
                consumer = _kafkaFactory.CreateConsumer();

                using var adminClient = new AdminClientBuilder(_kafkaFactory.CreateAdminClientConfig()).Build();
                try
                {
                    await adminClient.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = topicName,
                            NumPartitions = _context.Options.PartitionCount,
                            ReplicationFactor = 1
                        }
                    ]).ConfigureAwait(false);
                }
                catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    // Topic already exists
                }

                consumer.Subscribe(topicName);
            })
            .WithClean(_ =>
            {
                var produced = _metrics.GetValueOrDefault("produced", 0);
                var consumed = _metrics.GetValueOrDefault("consumed", 0);
                Console.WriteLine($"Kafka consume throughput - Produced: {produced}, Consumed: {consumed}");
                _metrics.Clear();

                consumer?.Close();
                consumer?.Dispose();
                _producer?.Dispose();
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
    /// Creates the partition distribution scenario.
    /// Verifies messages distribute evenly across partitions.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePartitionDistributionScenario()
    {
        var topicName = $"{_context.Options.TopicPrefix}-partition-dist";

        return Scenario.Create(
            name: "kafka-partition-distribution",
            run: async scenarioContext =>
            {
                try
                {
                    if (_producer is null)
                    {
                        return Response.Fail("Producer not initialized", statusCode: "no_producer");
                    }

                    var message = _context.CreateTestMessage();
                    var key = _context.NextMessageId().ToString(System.Globalization.CultureInfo.InvariantCulture);

                    var result = await _producer.ProduceAsync(
                        topicName,
                        new Message<string, byte[]> { Key = key, Value = message }).ConfigureAwait(false);

                    var partition = result.Partition.Value;
                    _metrics.AddOrUpdate($"partition_{partition}", 1, (_, c) => c + 1);
                    _metrics.AddOrUpdate("total", 1, (_, c) => c + 1);

                    return Response.Ok(statusCode: $"partition_{partition}");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _producer = _kafkaFactory!.CreateProducer();

                using var adminClient = new AdminClientBuilder(_kafkaFactory.CreateAdminClientConfig()).Build();
                try
                {
                    await adminClient.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = topicName,
                            NumPartitions = _context.Options.PartitionCount,
                            ReplicationFactor = 1
                        }
                    ]).ConfigureAwait(false);
                }
                catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    // Topic already exists
                }
            })
            .WithClean(_ =>
            {
                var total = _metrics.GetValueOrDefault("total", 0);
                Console.WriteLine($"Kafka partition distribution - Total: {total}");

                for (var i = 0; i < _context.Options.PartitionCount; i++)
                {
                    var count = _metrics.GetValueOrDefault($"partition_{i}", 0);
                    var percentage = total > 0 ? (double)count / total * 100 : 0;
                    Console.WriteLine($"  Partition {i}: {count} ({percentage:F1}%)");
                }

                _metrics.Clear();
                _producer?.Dispose();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
