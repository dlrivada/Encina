using Encina.Cdc.SqlServer;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.SqlServer;

/// <summary>
/// Property-based tests for <see cref="SqlServerCdcPosition"/> invariants.
/// Verifies serialization round-trips and comparison semantics for SQL Server
/// Change Tracking version positions.
/// </summary>
[Trait("Category", "Property")]
public sealed class SqlServerCdcPositionPropertyTests
{
    #region ToBytes/FromBytes Round-Trip

    /// <summary>
    /// Property: For any long value, ToBytes then FromBytes returns a position with the same Version.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_RoundTrip_PreservesVersion(long version)
    {
        var original = new SqlServerCdcPosition(version);
        var restored = SqlServerCdcPosition.FromBytes(original.ToBytes());

        return restored.Version == original.Version;
    }

    /// <summary>
    /// Property: Round-tripped positions compare equal to the original.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_RoundTrip_ComparesToZero(long version)
    {
        var original = new SqlServerCdcPosition(version);
        var restored = SqlServerCdcPosition.FromBytes(original.ToBytes());

        return original.CompareTo(restored) == 0;
    }

    /// <summary>
    /// Property: ToBytes always produces exactly 8 bytes.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ToBytes_ProducesEightBytes(long version)
    {
        var position = new SqlServerCdcPosition(version);

        return position.ToBytes().Length == 8;
    }

    /// <summary>
    /// Property: Same version produces identical byte arrays.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ToBytes_IsDeterministic(long version)
    {
        var a = new SqlServerCdcPosition(version);
        var b = new SqlServerCdcPosition(version);

        return a.ToBytes().SequenceEqual(b.ToBytes());
    }

    #endregion

    #region CompareTo Ordering

    /// <summary>
    /// Property: CompareTo ordering matches long comparison.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_MatchesLongComparison(long versionA, long versionB)
    {
        var a = new SqlServerCdcPosition(versionA);
        var b = new SqlServerCdcPosition(versionB);

        return Math.Sign(a.CompareTo(b)) == Math.Sign(versionA.CompareTo(versionB));
    }

    /// <summary>
    /// Property: CompareTo is antisymmetric (a &lt; b implies b &gt; a).
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_IsAntisymmetric(long versionA, long versionB)
    {
        var a = new SqlServerCdcPosition(versionA);
        var b = new SqlServerCdcPosition(versionB);

        var ab = a.CompareTo(b);
        var ba = b.CompareTo(a);

        if (ab > 0) return ba < 0;
        if (ab < 0) return ba > 0;
        return ba == 0;
    }

    /// <summary>
    /// Property: CompareTo is reflexive (a.CompareTo(a) == 0).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_CompareTo_IsReflexive(long version)
    {
        var position = new SqlServerCdcPosition(version);

        return position.CompareTo(position) == 0;
    }

    /// <summary>
    /// Property: CompareTo with null always returns positive.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_CompareTo_NullReturnsPositive(long version)
    {
        var position = new SqlServerCdcPosition(version);

        return position.CompareTo(null) > 0;
    }

    #endregion
}
