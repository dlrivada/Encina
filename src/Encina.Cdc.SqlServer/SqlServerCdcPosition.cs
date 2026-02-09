using System.Buffers.Binary;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.SqlServer;

/// <summary>
/// Represents a CDC position based on SQL Server Change Tracking version number.
/// The version is a monotonically increasing <see cref="long"/> value returned by
/// <c>CHANGE_TRACKING_CURRENT_VERSION()</c>.
/// </summary>
public sealed class SqlServerCdcPosition : CdcPosition
{
    /// <summary>
    /// Gets the Change Tracking version number.
    /// </summary>
    public long Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCdcPosition"/> class.
    /// </summary>
    /// <param name="version">The Change Tracking version number.</param>
    public SqlServerCdcPosition(long version)
    {
        Version = version;
    }

    /// <summary>
    /// Creates a <see cref="SqlServerCdcPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">An 8-byte big-endian representation of the version.</param>
    /// <returns>A new <see cref="SqlServerCdcPosition"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the byte array is not exactly 8 bytes.</exception>
    public static SqlServerCdcPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length != 8)
        {
            throw new ArgumentException("SQL Server CDC position requires exactly 8 bytes.", nameof(bytes));
        }

        var version = BinaryPrimitives.ReadInt64BigEndian(bytes);
        return new SqlServerCdcPosition(version);
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, Version);
        return bytes;
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not SqlServerCdcPosition sqlPosition)
        {
            throw new ArgumentException(
                $"Cannot compare SqlServerCdcPosition with {other.GetType().Name}.",
                nameof(other));
        }

        return Version.CompareTo(sqlPosition.Version);
    }

    /// <inheritdoc />
    public override string ToString() => $"CT-Version:{Version}";
}
