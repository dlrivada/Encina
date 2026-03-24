using Encina.Cdc.PostgreSql;
using FsCheck;
using FsCheck.Xunit;
using NpgsqlTypes;

namespace Encina.PropertyTests.Cdc.PostgreSql;

/// <summary>
/// Property-based tests for <see cref="PostgresCdcPosition"/> invariants.
/// Verifies serialization round-trips and comparison semantics for PostgreSQL LSN positions.
/// </summary>
[Trait("Category", "Property")]
public sealed class PostgresCdcPositionPropertyTests
{
    #region ToBytes/FromBytes Round-Trip

    /// <summary>
    /// Property: For any valid LSN value, ToBytes then FromBytes returns an equivalent position.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_RoundTrip_PreservesLsn(ulong lsnValue)
    {
        var original = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));
        var restored = PostgresCdcPosition.FromBytes(original.ToBytes());

        return restored.Lsn == original.Lsn;
    }

    /// <summary>
    /// Property: Round-tripped positions compare equal to the original.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_RoundTrip_ComparesToZero(ulong lsnValue)
    {
        var original = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));
        var restored = PostgresCdcPosition.FromBytes(original.ToBytes());

        return original.CompareTo(restored) == 0;
    }

    /// <summary>
    /// Property: ToBytes always produces exactly 8 bytes.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ToBytes_ProducesEightBytes(ulong lsnValue)
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));

        return position.ToBytes().Length == 8;
    }

    #endregion

    #region CompareTo Consistency

    /// <summary>
    /// Property: CompareTo is antisymmetric (a &lt; b implies b &gt; a).
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_IsAntisymmetric(ulong valueA, ulong valueB)
    {
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(valueA));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(valueB));

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
    public bool Property_CompareTo_IsReflexive(ulong lsnValue)
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));

        return position.CompareTo(position) == 0;
    }

    /// <summary>
    /// Property: CompareTo with null always returns positive.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_CompareTo_NullReturnsPositive(ulong lsnValue)
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));

        return position.CompareTo(null) > 0;
    }

    /// <summary>
    /// Property: CompareTo ordering is consistent with underlying LSN ordering.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool Property_CompareTo_MatchesUnderlyingOrdering(ulong valueA, ulong valueB)
    {
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(valueA));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(valueB));

        return Math.Sign(a.CompareTo(b)) == Math.Sign(valueA.CompareTo(valueB));
    }

    #endregion
}
