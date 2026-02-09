using System.Diagnostics.CodeAnalysis;

namespace Encina.Cdc.Abstractions;

/// <summary>
/// Abstract base class representing a CDC position in the change stream.
/// Provider-specific implementations encode their position format (e.g., LSN, GTID, resume token).
/// Supports serialization for persistence and comparison for verifying monotonic progress.
/// </summary>
/// <remarks>
/// <para>
/// Each CDC provider has a different position format:
/// <list type="bullet">
///   <item><description>SQL Server: Change tracking version (long)</description></item>
///   <item><description>PostgreSQL: Log Sequence Number (LSN)</description></item>
///   <item><description>MySQL: Binlog file/position or GTID</description></item>
///   <item><description>MongoDB: Resume token (BSON bytes)</description></item>
/// </list>
/// </para>
/// <para>
/// Provider-specific position classes (e.g., <c>SqlServerCdcPosition</c>, <c>PostgresCdcPosition</c>)
/// are defined in their respective packages.
/// </para>
/// </remarks>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Abstract base class - operators are defined by concrete provider-specific position types")]
public abstract class CdcPosition : IComparable<CdcPosition>
{
    /// <summary>
    /// Serializes this position to a byte array for persistent storage.
    /// </summary>
    /// <returns>A byte array representing this position.</returns>
    public abstract byte[] ToBytes();

    /// <summary>
    /// Compares this position with another to determine ordering.
    /// Positions must be monotonically increasing within a single connector.
    /// </summary>
    /// <param name="other">The position to compare with.</param>
    /// <returns>
    /// A negative value if this position precedes <paramref name="other"/>,
    /// zero if they are equal, or a positive value if this position follows <paramref name="other"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="other"/> is a different position type than this instance.
    /// </exception>
    public abstract int CompareTo(CdcPosition? other);

    /// <summary>
    /// Returns a human-readable representation of this position for diagnostics.
    /// </summary>
    /// <returns>A string representation of this position.</returns>
    public abstract override string ToString();
}
