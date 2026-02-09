using System.Buffers.Binary;
using Encina.Cdc.Abstractions;
using NpgsqlTypes;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// Represents a CDC position based on a PostgreSQL Log Sequence Number (LSN).
/// The LSN is a pointer to a location in the Write-Ahead Log (WAL).
/// </summary>
public sealed class PostgresCdcPosition : CdcPosition
{
    /// <summary>
    /// Gets the PostgreSQL Log Sequence Number.
    /// </summary>
    public NpgsqlLogSequenceNumber Lsn { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresCdcPosition"/> class.
    /// </summary>
    /// <param name="lsn">The PostgreSQL Log Sequence Number.</param>
    public PostgresCdcPosition(NpgsqlLogSequenceNumber lsn)
    {
        Lsn = lsn;
    }

    /// <summary>
    /// Creates a <see cref="PostgresCdcPosition"/> from a byte array previously produced by <see cref="ToBytes"/>.
    /// </summary>
    /// <param name="bytes">An 8-byte representation of the LSN.</param>
    /// <returns>A new <see cref="PostgresCdcPosition"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the byte array is not exactly 8 bytes.</exception>
    public static PostgresCdcPosition FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length != 8)
        {
            throw new ArgumentException("PostgreSQL CDC position requires exactly 8 bytes.", nameof(bytes));
        }

        var value = BinaryPrimitives.ReadUInt64BigEndian(bytes);
        return new PostgresCdcPosition(new NpgsqlLogSequenceNumber(value));
    }

    /// <inheritdoc />
    public override byte[] ToBytes()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)Lsn);
        return bytes;
    }

    /// <inheritdoc />
    public override int CompareTo(CdcPosition? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other is not PostgresCdcPosition pgPosition)
        {
            throw new ArgumentException(
                $"Cannot compare PostgresCdcPosition with {other.GetType().Name}.",
                nameof(other));
        }

        return Lsn.CompareTo(pgPosition.Lsn);
    }

    /// <inheritdoc />
    public override string ToString() => $"LSN:{Lsn}";
}
