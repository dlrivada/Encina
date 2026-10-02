using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Confluent.Kafka;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
using Encina.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Debezium.Kafka;

/// <summary>
/// CDC connector that consumes Debezium change events from Kafka topics.
/// Uses <see cref="DebeziumEventMapper"/> (shared with the HTTP connector) to parse
/// Debezium envelopes into <see cref="ChangeEvent"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This connector implements <see cref="ICdcConnector"/> and creates a Kafka consumer
/// using <c>Confluent.Kafka</c>. It subscribes to the configured topics and yields
/// parsed change events via <see cref="StreamChangesAsync"/>.
/// </para>
/// <para>
/// Auto-commit is disabled. Position tracking and offset management are handled by
/// the <see cref="CdcProcessor"/> via <see cref="ICdcPositionStore"/>.
/// </para>
/// <para>
/// On startup, the connector retrieves the last saved position from the position store
/// and skips events that have already been processed, enabling resume after restarts.
/// </para>
/// </remarks>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Consumer loop must catch all exceptions to continue processing and report errors via Either")]
internal sealed class DebeziumKafkaConnector : ICdcConnector, IDisposable
{
    private readonly DebeziumKafkaOptions _options;
    private readonly ICdcPositionStore _positionStore;
    private readonly ILogger<DebeziumKafkaConnector> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IConsumer<string, string> _consumer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumKafkaConnector"/> class.
    /// </summary>
    /// <param name="options">Kafka-specific CDC options.</param>
    /// <param name="positionStore">Position store for tracking progress.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="timeProvider">The time provider for testing.</param>
    public DebeziumKafkaConnector(
        DebeziumKafkaOptions options,
        ICdcPositionStore positionStore,
        ILogger<DebeziumKafkaConnector> logger,
        TimeProvider? timeProvider = null)
        : this(options, positionStore, logger, timeProvider, consumer: null)
    {
    }

    /// <summary>
    /// Initializes a new instance with an explicit consumer (tests supply a fake; <c>null</c> builds the Kafka consumer).
    /// </summary>
    internal DebeziumKafkaConnector(
        DebeziumKafkaOptions options,
        ICdcPositionStore positionStore,
        ILogger<DebeziumKafkaConnector> logger,
        TimeProvider? timeProvider,
        IConsumer<string, string>? consumer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _consumer = consumer ?? BuildConsumer();
        SubscribeToTopics();
    }

    /// <inheritdoc />
    public string ConnectorId => "encina-cdc-debezium-kafka";

    /// <inheritdoc />
    public async Task<Either<EncinaError, CdcPosition>> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
                .ConfigureAwait(false);

            if (positionResult.IsRight)
            {
                var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
                if (optPosition.IsSome)
                {
                    return Right<EncinaError, CdcPosition>((CdcPosition)optPosition);
                }
            }

            return Right<EncinaError, CdcPosition>(
                new DebeziumCdcPosition("{\"status\":\"awaiting_events\"}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Left(CdcErrors.ConnectionFailed("Failed to get Debezium Kafka position", ex));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Retrieve saved position for resume-from-position logic
        var resumePosition = await GetResumePositionAsync(cancellationToken).ConfigureAwait(false);
        var passedResumePoint = resumePosition is null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var outcome = TryConsume(cancellationToken);
            if (outcome.Cancelled)
            {
                yield break;
            }

            if (outcome.Error is not null)
            {
                yield return outcome.Error.Value;
                continue;
            }

            var consumeResult = outcome.Result;
            if (consumeResult?.Message?.Value is null)
            {
                continue;
            }

            DebeziumKafkaLog.EventConsumed(
                _logger, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

            var result = ParseAndEnrich(consumeResult);

            // Skip events that were already processed before restart
            if (result.IsRight && !passedResumePoint)
            {
                if (IsAlreadyProcessed(resumePosition, consumeResult))
                {
                    DebeziumKafkaLog.EventSkippedAlreadyProcessed(_logger);
                    continue;
                }

                passedResumePoint = true;
            }

            yield return result;
        }

        DebeziumKafkaLog.ConsumerStopped(_logger);
    }

    private readonly record struct ConsumeOutcome(
        bool Cancelled,
        ConsumeResult<string, string>? Result,
        Either<EncinaError, ChangeEvent>? Error);

    /// <summary>
    /// Polls the consumer once; cancellation and consume errors are reported through the outcome
    /// because an iterator cannot yield from inside a catch block.
    /// </summary>
    private ConsumeOutcome TryConsume(CancellationToken cancellationToken)
    {
        try
        {
            return new ConsumeOutcome(false, _consumer.Consume(cancellationToken), null);
        }
        catch (OperationCanceledException)
        {
            return new ConsumeOutcome(true, null, null);
        }
        catch (ConsumeException ex)
        {
            DebeziumKafkaLog.ConsumerError(_logger, ex.Error.Code.ToString());
            Either<EncinaError, ChangeEvent> error = Left(CdcErrors.StreamInterrupted(ex));
            return new ConsumeOutcome(false, null, error);
        }
    }

    /// <summary>
    /// Parses the Debezium event JSON and, on success, replaces the position with the Kafka-specific
    /// one that carries topic, partition and offset.
    /// </summary>
    private Either<EncinaError, ChangeEvent> ParseAndEnrich(ConsumeResult<string, string> consumeResult)
    {
        var topic = consumeResult.Topic;
        Either<EncinaError, ChangeEvent> result;
        try
        {
            using var doc = JsonDocument.Parse(consumeResult.Message.Value);
            var eventJson = doc.RootElement.Clone();
            result = DebeziumEventMapper.MapEvent(eventJson, _options.EventFormat, _logger, _timeProvider);
        }
        catch (JsonException ex)
        {
            return Left(CdcErrors.DeserializationFailed(topic, typeof(ChangeEvent), ex));
        }

        if (result.IsLeft)
        {
            return result;
        }

        var changeEvent = (ChangeEvent)result;
        var kafkaPosition = new DebeziumKafkaPosition(
            changeEvent.Metadata.Position is DebeziumCdcPosition debPos
                ? debPos.OffsetJson
                : "{\"kafka\":true}",
            topic,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value);

        var enrichedMetadata = changeEvent.Metadata with { Position = kafkaPosition };
        return Right<EncinaError, ChangeEvent>(changeEvent with { Metadata = enrichedMetadata });
    }

    private static bool IsAlreadyProcessed(DebeziumKafkaPosition? resumePosition, ConsumeResult<string, string> consumeResult) =>
        resumePosition is not null &&
        string.Equals(consumeResult.Topic, resumePosition.Topic, StringComparison.Ordinal) &&
        consumeResult.Partition.Value == resumePosition.Partition &&
        consumeResult.Offset.Value <= resumePosition.Offset;

    /// <summary>
    /// Builds the Kafka consumer from configured options.
    /// </summary>
    private IConsumer<string, string> BuildConsumer()
    {
        var config = BuildConfig(_options);

        return new ConsumerBuilder<string, string>(config)
            .SetPartitionsAssignedHandler((_, partitions) =>
            {
                var partitionList = string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"));
                DebeziumKafkaLog.PartitionsAssigned(_logger, partitionList);
            })
            .SetPartitionsRevokedHandler((_, partitions) =>
            {
                var partitionList = string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"));
                DebeziumKafkaLog.PartitionsRevoked(_logger, partitionList);
            })
            .SetErrorHandler((_, error) =>
            {
                DebeziumKafkaLog.ConsumerError(_logger, error.Code.ToString());
            })
            .Build();
    }

    /// <summary>
    /// Maps the options to the Confluent consumer configuration.
    /// </summary>
    internal static ConsumerConfig BuildConfig(DebeziumKafkaOptions options)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = options.AutoOffsetReset == "latest"
                ? Confluent.Kafka.AutoOffsetReset.Latest
                : Confluent.Kafka.AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = options.SessionTimeoutMs,
            MaxPollIntervalMs = options.MaxPollIntervalMs
        };

        ApplySecurity(config, options);
        return config;
    }

    private static void ApplySecurity(ConsumerConfig config, DebeziumKafkaOptions options)
    {
        if (!string.IsNullOrEmpty(options.SecurityProtocol))
        {
            config.SecurityProtocol = options.SecurityProtocol switch
            {
                "SSL" => Confluent.Kafka.SecurityProtocol.Ssl,
                "SASL_PLAINTEXT" => Confluent.Kafka.SecurityProtocol.SaslPlaintext,
                "SASL_SSL" => Confluent.Kafka.SecurityProtocol.SaslSsl,
                _ => Confluent.Kafka.SecurityProtocol.Plaintext
            };
        }

        if (!string.IsNullOrEmpty(options.SaslMechanism))
        {
            config.SaslMechanism = options.SaslMechanism switch
            {
                "SCRAM-SHA-256" => Confluent.Kafka.SaslMechanism.ScramSha256,
                "SCRAM-SHA-512" => Confluent.Kafka.SaslMechanism.ScramSha512,
                "GSSAPI" => Confluent.Kafka.SaslMechanism.Gssapi,
                _ => Confluent.Kafka.SaslMechanism.Plain
            };
        }

        ApplyCredentials(config, options);
    }

    private static void ApplyCredentials(ConsumerConfig config, DebeziumKafkaOptions options)
    {
        if (!string.IsNullOrEmpty(options.SaslUsername))
        {
            config.SaslUsername = options.SaslUsername;
        }

        if (!string.IsNullOrEmpty(options.SaslPassword))
        {
            config.SaslPassword = options.SaslPassword;
        }

        if (!string.IsNullOrEmpty(options.SslCaLocation))
        {
            config.SslCaLocation = options.SslCaLocation;
        }
    }

    /// <summary>
    /// Subscribes the consumer to the configured topics.
    /// </summary>
    private void SubscribeToTopics()
    {
        if (_options.Topics.Length == 0)
        {
            throw new InvalidOperationException(
                "At least one topic must be configured in DebeziumKafkaOptions.Topics.");
        }

        _consumer.Subscribe(_options.Topics);

        var topicList = string.Join(", ", _options.Topics);
        DebeziumKafkaLog.ConsumerStarted(_logger, topicList);
    }

    /// <summary>
    /// Retrieves the last saved position from the position store for resume logic.
    /// Returns <c>null</c> if no saved position exists or if retrieval fails.
    /// </summary>
    internal async Task<DebeziumKafkaPosition?> GetResumePositionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var positionResult = await _positionStore.GetPositionAsync(ConnectorId, cancellationToken)
                .ConfigureAwait(false);

            if (positionResult.IsRight)
            {
                var optPosition = (LanguageExt.Option<CdcPosition>)positionResult;
                if (optPosition.IsSome)
                {
                    var position = (CdcPosition)optPosition;
                    if (position is DebeziumKafkaPosition kafkaPosition)
                    {
                        DebeziumKafkaLog.ResumingFromOffset(
                            _logger, kafkaPosition.Offset, kafkaPosition.Topic, kafkaPosition.Partition);
                        return kafkaPosition;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DebeziumCdcLog.PositionRetrievalFailed(_logger, ex.ForLogging(), ConnectorId);
        }

        CdcLog.NoSavedPosition(_logger, ConnectorId);
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _consumer.Close();
        _consumer.Dispose();
        _disposed = true;
    }
}
