using Encina.Cdc.MySql;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.MySql;

/// <summary>
/// Property-based tests for <see cref="MySqlCdcPosition"/> invariants.
/// Verifies serialization round-trips and comparison semantics for both
/// GTID and file/position modes.
/// </summary>
[Trait("Category", "Property")]
public sealed class MySqlCdcPositionPropertyTests
{
    #region GTID Mode Round-Trip

    /// <summary>
    /// Property: GTID mode ToBytes/FromBytes round-trip for any non-empty string.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_GtidMode_RoundTrip_PreservesGtidSet()
    {
        return Prop.ForAll(
            Arb.From(GenGtidString()),
            gtid =>
            {
                var original = new MySqlCdcPosition(gtid);
                var restored = MySqlCdcPosition.FromBytes(original.ToBytes());

                return restored.GtidSet == original.GtidSet
                       && restored.BinlogFileName is null;
            });
    }

    #endregion

    #region File/Position Mode Round-Trip

    /// <summary>
    /// Property: File/Position mode round-trip for any (non-empty string, positive long).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_FilePositionMode_RoundTrip()
    {
        return Prop.ForAll(
            Arb.From(GenBinlogFileName()),
            Arb.From(GenPositiveLong()),
            (fileName, position) =>
            {
                var original = new MySqlCdcPosition(fileName, position);
                var restored = MySqlCdcPosition.FromBytes(original.ToBytes());

                return restored.BinlogFileName == original.BinlogFileName
                       && restored.BinlogPosition == original.BinlogPosition
                       && restored.GtidSet is null;
            });
    }

    #endregion

    #region CompareTo Transitivity

    /// <summary>
    /// Property: CompareTo ordering is transitive for GTID positions.
    /// If a &lt;= b and b &lt;= c, then a &lt;= c.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_GtidCompareTo_IsTransitive()
    {
        return Prop.ForAll(
            Arb.From(GenGtidString()),
            Arb.From(GenGtidString()),
            Arb.From(GenGtidString()),
            (gtidA, gtidB, gtidC) =>
            {
                var positions = new[] { gtidA, gtidB, gtidC };
                Array.Sort(positions, StringComparer.Ordinal);

                var a = new MySqlCdcPosition(positions[0]);
                var b = new MySqlCdcPosition(positions[1]);
                var c = new MySqlCdcPosition(positions[2]);

                var ab = a.CompareTo(b);
                var bc = b.CompareTo(c);
                var ac = a.CompareTo(c);

                if (ab <= 0 && bc <= 0)
                {
                    return ac <= 0;
                }

                return true;
            });
    }

    /// <summary>
    /// Property: CompareTo ordering is transitive for file/position positions.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_FilePositionCompareTo_IsTransitive()
    {
        var genThreeLongs = GenPositiveLong().Three().Select(t => new { posA = t.Item1, posB = t.Item2, posC = t.Item3 });
        return Prop.ForAll(
            Arb.From(GenBinlogFileName()),
            Arb.From(genThreeLongs),
            (fileName, positions) =>
            {
                var values = new[] { positions.posA, positions.posB, positions.posC };
                Array.Sort(values);

                var a = new MySqlCdcPosition(fileName, values[0]);
                var b = new MySqlCdcPosition(fileName, values[1]);
                var c = new MySqlCdcPosition(fileName, values[2]);

                var ab = a.CompareTo(b);
                var bc = b.CompareTo(c);
                var ac = a.CompareTo(c);

                if (ab <= 0 && bc <= 0)
                {
                    return ac <= 0;
                }

                return true;
            });
    }

    #endregion

    #region CompareTo Antisymmetry

    /// <summary>
    /// Property: CompareTo is antisymmetric for GTID mode.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_GtidCompareTo_IsAntisymmetric()
    {
        return Prop.ForAll(
            Arb.From(GenGtidString()),
            Arb.From(GenGtidString()),
            (gtidA, gtidB) =>
            {
                var a = new MySqlCdcPosition(gtidA);
                var b = new MySqlCdcPosition(gtidB);

                var ab = a.CompareTo(b);
                var ba = b.CompareTo(a);

                if (ab > 0) return ba < 0;
                if (ab < 0) return ba > 0;
                return ba == 0;
            });
    }

    #endregion

    #region CompareTo Null

    /// <summary>
    /// Property: CompareTo(null) always returns positive for GTID positions.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_CompareTo_NullReturnsPositive()
    {
        return Prop.ForAll(
            Arb.From(GenGtidString()),
            gtid =>
            {
                var position = new MySqlCdcPosition(gtid);
                return position.CompareTo(null) > 0;
            });
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates GTID-style strings that are non-empty and non-whitespace.
    /// </summary>
    private static Gen<string> GenGtidString()
    {
        return Gen.Elements("a", "b", "c", "d", "e")
            .SelectMany(prefix =>
                Gen.Choose(1, 10000).Select(n => $"{prefix}-uuid:{n}"));
    }

    /// <summary>
    /// Generates binlog file name strings (e.g., "mysql-bin.000001").
    /// </summary>
    private static Gen<string> GenBinlogFileName()
    {
        return Gen.Choose(1, 999999)
            .Select(n => $"mysql-bin.{n:D6}");
    }

    /// <summary>
    /// Generates positive long values suitable for binlog positions.
    /// </summary>
    private static Gen<long> GenPositiveLong()
    {
        return Gen.Choose(1, int.MaxValue).Select(n => (long)n);
    }

    #endregion
}
