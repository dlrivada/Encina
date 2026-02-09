using System.Text;
using System.Text.Json;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.Debezium.Kafka;

/// <summary>
/// Represents a CDC position based on a Kafka topic/partition/offset combined with
/// the Debezium source offset JSON. Extends <see cref="CdcPosition"/> to provide
/// Kafka-specific offset tracking for precise resume-from-position support.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="DebeziumCdcPosition"/> (used by the HTTP connector), this position
/// type stores Kafka-specific metadata (topic, partition, offset) in addition to the
/// Debezium source offset JSON. This enables reliable resume across Kafka consumer restarts.
/// </para>
/// </remarks>
public sealed class DebeziumKafkaPosition : CdcPosition
{
    /// <summary>
    /// Gets the Debezium source offset as a JSON string.
    /// </summary>
    public string OffsetJson { get; }

    /// <summary>
    /// Gets the Kafka topic this event was consumed from.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the Kafka partition number.
    /// </summary>
    public int Partition { get; }

    /// <summary>
    /// Gets the Kafka offset within the partition.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumKafkaPosition"/> class.
    /// </summary>
    /// <param name="offsetJson">The Debezium source offset as a JSON string.</param>
    /// <param name="topic">The Kafka topic name.</param>
    /// <param name="partition">The Kafka partition number.</param>
    /// <param name="offset">The Kafka offset within the partition.</param>
    public DebeziumKafkaPosition(string offsetJson, string topic, int partition, long offset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(offsetJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        OffsetJson = offsetJson;
        Topic = topic;
        Partition = partition;
        Offset = offset;
    }

    /// <summary>
    /// Creates a <see cref="DebeziumKafkaPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">A UTF-8 encoded JSON string containing the Kafka position data.</param>
    /// <returns>A new <see cref="DebeziumKafkaPosition"/>.</returns>
    public static DebeziumKafkaPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var json = Encoding.UTF8.GetString(bytes);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var offsetJson = root.GetProperty("offsetJson").GetString()!;
        var topic = root.GetProperty("topic").GetString()!;
        var partition = root.GetProperty("partition").GetInt32();
        var offset = root.GetProperty("offset").GetInt64();

        return new DebeziumKafkaPosition(offsetJson, topic, partition, offset);
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var jsonObj = new
        {
            offsetJson = OffsetJson,
            topic = Topic,
            partition = Partition,
            offset = Offset
        };

        return JsonSerializer.SerializeToUtf8Bytes(jsonObj);
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is DebeziumKafkaPosition kafkaPosition)
        {
            // Same topic and partition: compare by Kafka offset (most reliable)
            if (string.Equals(Topic, kafkaPosition.Topic, StringComparison.Ordinal) &&
                Partition == kafkaPosition.Partition)
            {
                return Offset.CompareTo(kafkaPosition.Offset);
            }

            // Different topic or partition: compare by topic, then partition
            var topicComparison = string.Compare(Topic, kafkaPosition.Topic, StringComparison.Ordinal);
            return topicComparison != 0
                ? topicComparison
                : Partition.CompareTo(kafkaPosition.Partition);
        }

        throw new ArgumentException(
            $"Cannot compare DebeziumKafkaPosition with {other.GetType().Name}.",
            nameof(other));
    }

    /// <inheritdoc />
    public override string ToString() => $"Kafka:{Topic}[{Partition}]@{Offset}";
}
