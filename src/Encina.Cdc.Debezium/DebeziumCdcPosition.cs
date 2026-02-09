using System.Text;
using System.Text.Json;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Represents a CDC position based on a Debezium source offset.
/// The offset is a JSON document containing source-specific position information
/// (e.g., LSN, binlog position, etc.) as reported by Debezium Server.
/// </summary>
public sealed class DebeziumCdcPosition : CdcPosition
{
    /// <summary>
    /// Gets the Debezium source offset as a JSON string.
    /// </summary>
    public string OffsetJson { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebeziumCdcPosition"/> class.
    /// </summary>
    /// <param name="offsetJson">The Debezium source offset as a JSON string.</param>
    public DebeziumCdcPosition(string offsetJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(offsetJson);
        OffsetJson = offsetJson;
    }

    /// <summary>
    /// Creates a <see cref="DebeziumCdcPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">A UTF-8 encoded JSON offset string.</param>
    /// <returns>A new <see cref="DebeziumCdcPosition"/>.</returns>
    public static DebeziumCdcPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var json = Encoding.UTF8.GetString(bytes);
        return new DebeziumCdcPosition(json);
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        return Encoding.UTF8.GetBytes(OffsetJson);
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not DebeziumCdcPosition debeziumPosition)
        {
            throw new ArgumentException(
                $"Cannot compare DebeziumCdcPosition with {other.GetType().Name}.",
                nameof(other));
        }

        // Debezium offsets are opaque; compare as strings
        return string.Compare(OffsetJson, debeziumPosition.OffsetJson, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => $"Debezium-Offset:{OffsetJson[..Math.Min(50, OffsetJson.Length)]}";
}
