using System.Text.Json;
using Confluent.Kafka;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Kafka;

/// <summary>
/// Kafka-based implementation of the message publisher.
/// </summary>
public sealed class KafkaMessagePublisher : IKafkaMessagePublisher, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaMessagePublisher> _logger;
    private readonly EncinaKafkaOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaMessagePublisher"/> class.
    /// </summary>
    /// <param name="producer">The Kafka producer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public KafkaMessagePublisher(
        IProducer<string, byte[]> producer,
        ILogger<KafkaMessagePublisher> logger,
        IOptions<EncinaKafkaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _producer = producer;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, KafkaDeliveryResult>> ProduceAsync<TMessage>(
        TMessage message,
        string? topic = null,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveTopic = topic ?? _options.DefaultEventTopic;

        try
        {
            Log.ProducingMessage(_logger, typeof(TMessage).Name, effectiveTopic);

            var kafkaMessage = new Message<string, byte[]>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = JsonSerializer.SerializeToUtf8Bytes(message),
                Headers = new Headers
                {
                    { "MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TMessage).FullName ?? typeof(TMessage).Name) }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(
                effectiveTopic,
                kafkaMessage,
                cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyProducedMessage(
                _logger,
                deliveryResult.Topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);

            return Right<EncinaError, KafkaDeliveryResult>( // NOSONAR S6966: LanguageExt Right is a pure function
                new KafkaDeliveryResult(
                    deliveryResult.Topic,
                    deliveryResult.Partition.Value,
                    deliveryResult.Offset.Value,
                    deliveryResult.Timestamp.UtcDateTime));
        }
        catch (Exception ex)
        {
            Log.FailedToProduceMessage(_logger, ex, typeof(TMessage).Name, effectiveTopic);

            return Left<EncinaError, KafkaDeliveryResult>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    "KAFKA_PRODUCE_FAILED",
                    ex,
                    $"Failed to produce message of type {typeof(TMessage).Name} to topic {effectiveTopic}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<KafkaDeliveryResult>>> ProduceBatchAsync<TMessage>(
        IEnumerable<(TMessage Message, string? Key)> messages,
        string? topic = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(messages);

        var effectiveTopic = topic ?? _options.DefaultEventTopic;
        var results = new List<KafkaDeliveryResult>();

        try
        {
            foreach (var (message, key) in messages)
            {
                var result = await ProduceAsync(message, effectiveTopic, key, cancellationToken).ConfigureAwait(false);

                if (result.IsLeft)
                {
                    return Left<EncinaError, IReadOnlyList<KafkaDeliveryResult>>( // NOSONAR S6966: LanguageExt Left is a pure function
                        result.Match(
                            Right: _ => throw new InvalidOperationException(),
                            Left: error => error));
                }

                result.IfRight(results.Add);
            }

            return Right<EncinaError, IReadOnlyList<KafkaDeliveryResult>>(results); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            Log.FailedToProduceBatch(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, IReadOnlyList<KafkaDeliveryResult>>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    "KAFKA_BATCH_PRODUCE_FAILED",
                    ex,
                    $"Failed to produce batch of messages of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, KafkaDeliveryResult>> ProduceWithHeadersAsync<TMessage>(
        TMessage message,
        IDictionary<string, byte[]> headers,
        string? topic = null,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(headers);

        var effectiveTopic = topic ?? _options.DefaultEventTopic;

        try
        {
            Log.ProducingMessageWithHeaders(_logger, typeof(TMessage).Name, headers.Count, effectiveTopic);

            var kafkaHeaders = new Headers();
            foreach (var header in headers)
            {
                kafkaHeaders.Add(header.Key, header.Value);
            }

            kafkaHeaders.Add("MessageType", System.Text.Encoding.UTF8.GetBytes(typeof(TMessage).FullName ?? typeof(TMessage).Name));

            var kafkaMessage = new Message<string, byte[]>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = JsonSerializer.SerializeToUtf8Bytes(message),
                Headers = kafkaHeaders
            };

            var deliveryResult = await _producer.ProduceAsync(
                effectiveTopic,
                kafkaMessage,
                cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, KafkaDeliveryResult>( // NOSONAR S6966: LanguageExt Right is a pure function
                new KafkaDeliveryResult(
                    deliveryResult.Topic,
                    deliveryResult.Partition.Value,
                    deliveryResult.Offset.Value,
                    deliveryResult.Timestamp.UtcDateTime));
        }
        catch (Exception ex)
        {
            Log.FailedToProduceMessageWithHeaders(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, KafkaDeliveryResult>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    "KAFKA_PRODUCE_HEADERS_FAILED",
                    ex,
                    $"Failed to produce message of type {typeof(TMessage).Name} with headers."));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
