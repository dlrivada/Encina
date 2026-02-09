using System.Text;
using System.Text.Json;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.MySql;

/// <summary>
/// Represents a CDC position based on a MySQL binary log position.
/// Supports both GTID-based and file/position-based tracking.
/// </summary>
public sealed class MySqlCdcPosition : CdcPosition
{
    /// <summary>
    /// Gets the GTID set (Global Transaction Identifier), if using GTID mode.
    /// </summary>
    public string? GtidSet { get; }

    /// <summary>
    /// Gets the binary log file name, if using file/position mode.
    /// </summary>
    public string? BinlogFileName { get; }

    /// <summary>
    /// Gets the position within the binary log file, if using file/position mode.
    /// </summary>
    public long BinlogPosition { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCdcPosition"/> class using GTID mode.
    /// </summary>
    /// <param name="gtidSet">The GTID set string.</param>
    public MySqlCdcPosition(string gtidSet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gtidSet);
        GtidSet = gtidSet;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCdcPosition"/> class using file/position mode.
    /// </summary>
    /// <param name="binlogFileName">The binary log file name.</param>
    /// <param name="binlogPosition">The position within the binary log file.</param>
    public MySqlCdcPosition(string binlogFileName, long binlogPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(binlogFileName);
        BinlogFileName = binlogFileName;
        BinlogPosition = binlogPosition;
    }

    /// <summary>
    /// Creates a <see cref="MySqlCdcPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">A UTF-8 JSON representation of the position.</param>
    /// <returns>A new <see cref="MySqlCdcPosition"/>.</returns>
    public static MySqlCdcPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var json = Encoding.UTF8.GetString(bytes);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("gtid", out var gtidElement) &&
            gtidElement.GetString() is { Length: > 0 } gtid)
        {
            return new MySqlCdcPosition(gtid);
        }

        var file = root.GetProperty("file").GetString()!;
        var pos = root.GetProperty("pos").GetInt64();
        return new MySqlCdcPosition(file, pos);
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["gtid"] = GtidSet,
            ["file"] = BinlogFileName,
            ["pos"] = BinlogPosition
        };

        var json = JsonSerializer.Serialize(data);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not MySqlCdcPosition mysqlPosition)
        {
            throw new ArgumentException(
                $"Cannot compare MySqlCdcPosition with {other.GetType().Name}.",
                nameof(other));
        }

        // GTID comparison is string-based (alphabetical ordering)
        if (GtidSet is not null && mysqlPosition.GtidSet is not null)
        {
            return string.Compare(GtidSet, mysqlPosition.GtidSet, StringComparison.Ordinal);
        }

        // File/position comparison
        if (BinlogFileName is not null && mysqlPosition.BinlogFileName is not null)
        {
            var fileCompare = string.Compare(
                BinlogFileName, mysqlPosition.BinlogFileName, StringComparison.Ordinal);
            return fileCompare != 0 ? fileCompare : BinlogPosition.CompareTo(mysqlPosition.BinlogPosition);
        }

        return 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (GtidSet is not null)
        {
            return $"GTID:{GtidSet}";
        }

        return $"Binlog:{BinlogFileName}:{BinlogPosition}";
    }
}
