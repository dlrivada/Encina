using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Confluent.Kafka;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Errors;
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
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(positionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _positionStore = positionStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _consumer = BuildConsumer();
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
            ConsumeResult<string, string>? consumeResult = null;
            Either<EncinaError, ChangeEvent>? consumeError = null;

            try
            {
                consumeResult = _consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (ConsumeException ex)
            {
                DebeziumKafkaLog.ConsumerError(_logger, ex.Error.Reason);
                consumeError = Left(CdcErrors.StreamInterrupted(ex));
            }

            if (consumeError is not null)
            {
                yield return consumeError.Value;
                continue;
            }

            if (consumeResult?.Message?.Value is null)
            {
                continue;
            }

            var topic = consumeResult.Topic;
            var partition = consumeResult.Partition.Value;
            var offset = consumeResult.Offset.Value;

            DebeziumKafkaLog.EventConsumed(_logger, topic, partition, offset);

            // Parse the Debezium event JSON
            Either<EncinaError, ChangeEvent> result;
            try
            {
                using var doc = JsonDocument.Parse(consumeResult.Message.Value);
                var eventJson = doc.RootElement.Clone();
                result = DebeziumEventMapper.MapEvent(eventJson, _options.EventFormat, _logger, _timeProvider);
            }
            catch (JsonException ex)
            {
                result = Left(CdcErrors.DeserializationFailed(
                    topic,
                    typeof(ChangeEvent),
                    ex));
            }

            // Replace position with Kafka-specific position that includes topic/partition/offset
            if (result.IsRight)
            {
                var changeEvent = (ChangeEvent)result;
                var kafkaPosition = new DebeziumKafkaPosition(
                    changeEvent.Metadata.Position is DebeziumCdcPosition debPos
                        ? debPos.OffsetJson
                        : "{\"kafka\":true}",
                    topic,
                    partition,
                    offset);

                var enrichedMetadata = changeEvent.Metadata with { Position = kafkaPosition };
                var enrichedEvent = changeEvent with { Metadata = enrichedMetadata };
                result = Right<EncinaError, ChangeEvent>(enrichedEvent);

                // Skip events that were already processed before restart
                if (!passedResumePoint)
                {
                    if (resumePosition is not null &&
                        string.Equals(topic, resumePosition.Topic, StringComparison.Ordinal) &&
                        partition == resumePosition.Partition &&
                        offset <= resumePosition.Offset)
                    {
                        DebeziumKafkaLog.EventSkippedAlreadyProcessed(_logger);
                        continue;
                    }

                    passedResumePoint = true;
                }
            }

            yield return result;
        }

        DebeziumKafkaLog.ConsumerStopped(_logger);
    }

    /// <summary>
    /// Builds the Kafka consumer from configured options.
    /// </summary>
    private IConsumer<string, string> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = _options.AutoOffsetReset switch
            {
                "latest" => Confluent.Kafka.AutoOffsetReset.Latest,
                "earliest" => Confluent.Kafka.AutoOffsetReset.Earliest,
                _ => Confluent.Kafka.AutoOffsetReset.Earliest
            },
            EnableAutoCommit = false,
            SessionTimeoutMs = _options.SessionTimeoutMs,
            MaxPollIntervalMs = _options.MaxPollIntervalMs
        };

        // Apply security settings if configured
        if (!string.IsNullOrEmpty(_options.SecurityProtocol))
        {
            config.SecurityProtocol = _options.SecurityProtocol switch
            {
                "SSL" => Confluent.Kafka.SecurityProtocol.Ssl,
                "SASL_PLAINTEXT" => Confluent.Kafka.SecurityProtocol.SaslPlaintext,
                "SASL_SSL" => Confluent.Kafka.SecurityProtocol.SaslSsl,
                _ => Confluent.Kafka.SecurityProtocol.Plaintext
            };
        }

        if (!string.IsNullOrEmpty(_options.SaslMechanism))
        {
            config.SaslMechanism = _options.SaslMechanism switch
            {
                "PLAIN" => Confluent.Kafka.SaslMechanism.Plain,
                "SCRAM-SHA-256" => Confluent.Kafka.SaslMechanism.ScramSha256,
                "SCRAM-SHA-512" => Confluent.Kafka.SaslMechanism.ScramSha512,
                "GSSAPI" => Confluent.Kafka.SaslMechanism.Gssapi,
                _ => Confluent.Kafka.SaslMechanism.Plain
            };
        }

        if (!string.IsNullOrEmpty(_options.SaslUsername))
        {
            config.SaslUsername = _options.SaslUsername;
        }

        if (!string.IsNullOrEmpty(_options.SaslPassword))
        {
            config.SaslPassword = _options.SaslPassword;
        }

        if (!string.IsNullOrEmpty(_options.SslCaLocation))
        {
            config.SslCaLocation = _options.SslCaLocation;
        }

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
                DebeziumKafkaLog.ConsumerError(_logger, error.Reason);
            })
            .Build();
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
    private async Task<DebeziumKafkaPosition?> GetResumePositionAsync(CancellationToken cancellationToken)
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
            DebeziumCdcLog.PositionRetrievalFailed(_logger, ex, ConnectorId);
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
