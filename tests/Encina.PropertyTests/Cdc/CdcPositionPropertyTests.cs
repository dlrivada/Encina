using FsCheck;
using FsCheck.Xunit;
using Encina.Cdc.Abstractions;

namespace Encina.PropertyTests.Cdc;

/// <summary>
/// Property-based tests for <see cref="CdcPosition"/> invariants.
/// Verifies comparison semantics, serialization round-trips, and null handling.
/// </summary>
[Trait("Category", "Property")]
public sealed class CdcPositionPropertyTests
{
    #region CompareTo Antisymmetry

    [Property(MaxTest = 200)]
    public bool Property_CompareTo_IsAntisymmetric(long valueA, long valueB)
    {
        // Property: If a.CompareTo(b) > 0, then b.CompareTo(a) < 0
        // Also: if a.CompareTo(b) == 0, then b.CompareTo(a) == 0
        var a = new TestCdcPosition(valueA);
        var b = new TestCdcPosition(valueB);

        var abCompare = a.CompareTo(b);
        var baCompare = b.CompareTo(a);

        if (abCompare > 0) return baCompare < 0;
        if (abCompare < 0) return baCompare > 0;
        return baCompare == 0;
    }

    #endregion

    #region CompareTo Transitivity

    [Property(MaxTest = 200)]
    public bool Property_CompareTo_IsTransitive(long valueA, long valueB, long valueC)
    {
        // Property: If a < b and b < c, then a < c
        var values = new[] { valueA, valueB, valueC };
        Array.Sort(values);

        var a = new TestCdcPosition(values[0]);
        var b = new TestCdcPosition(values[1]);
        var c = new TestCdcPosition(values[2]);

        var ab = a.CompareTo(b);
        var bc = b.CompareTo(c);
        var ac = a.CompareTo(c);

        // If a <= b and b <= c then a <= c
        if (ab <= 0 && bc <= 0)
        {
            return ac <= 0;
        }

        return true;
    }

    #endregion

    #region CompareTo Reflexivity

    [Property(MaxTest = 100)]
    public bool Property_CompareTo_IsReflexive(long value)
    {
        // Property: a.CompareTo(a) == 0
        var a = new TestCdcPosition(value);

        return a.CompareTo(a) == 0;
    }

    #endregion

    #region CompareTo with Null

    [Property(MaxTest = 100)]
    public bool Property_CompareTo_NullAlwaysReturnsPositive(long value)
    {
        // Property: CompareTo(null) always returns positive
        var position = new TestCdcPosition(value);

        return position.CompareTo(null) > 0;
    }

    #endregion

    #region ToBytes Round Trip

    [Property(MaxTest = 200)]
    public bool Property_ToBytes_SameValueProducesSameBytes(long value)
    {
        // Property: Positions with the same value produce the same bytes
        var a = new TestCdcPosition(value);
        var b = new TestCdcPosition(value);

        return a.ToBytes().SequenceEqual(b.ToBytes());
    }

    [Property(MaxTest = 200)]
    public bool Property_ToBytes_DifferentValuesProduceDifferentBytes(long valueA, long valueB)
    {
        // Property: Positions with different values produce different bytes
        if (valueA == valueB) return true; // vacuously true

        var a = new TestCdcPosition(valueA);
        var b = new TestCdcPosition(valueB);

        return !a.ToBytes().SequenceEqual(b.ToBytes());
    }

    [Property(MaxTest = 100)]
    public bool Property_ToBytes_ProducesConsistentLength(long value)
    {
        // Property: ToBytes always produces 8 bytes for a long-based position
        var position = new TestCdcPosition(value);

        return position.ToBytes().Length == sizeof(long);
    }

    [Property(MaxTest = 100)]
    public bool Property_ToBytes_CanReconstructValue(long value)
    {
        // Property: The byte representation can reconstruct the original value
        var position = new TestCdcPosition(value);
        var bytes = position.ToBytes();
        var reconstructed = BitConverter.ToInt64(bytes, 0);

        return reconstructed == value;
    }

    #endregion

    #region ToString Invariants

    [Property(MaxTest = 100)]
    public bool Property_ToString_IsNonNullAndNonEmpty(long value)
    {
        // Property: ToString always returns a non-null, non-empty string
        var position = new TestCdcPosition(value);

        return !string.IsNullOrEmpty(position.ToString());
    }

    [Property(MaxTest = 100)]
    public bool Property_ToString_IsDeterministic(long value)
    {
        // Property: Same value always produces the same string representation
        var a = new TestCdcPosition(value);
        var b = new TestCdcPosition(value);

        return a.ToString() == b.ToString();
    }

    #endregion

    #region CompareTo Consistency with Equality

    [Property(MaxTest = 100)]
    public bool Property_CompareTo_ZeroImpliesSameValue(long value)
    {
        // Property: If CompareTo returns 0, the positions have the same logical value
        var a = new TestCdcPosition(value);
        var b = new TestCdcPosition(value);

        return a.CompareTo(b) == 0;
    }

    [Property(MaxTest = 200)]
    public bool Property_CompareTo_OrderMatchesLongOrdering(long valueA, long valueB)
    {
        // Property: The ordering of TestCdcPosition matches the ordering of the underlying long values
        var a = new TestCdcPosition(valueA);
        var b = new TestCdcPosition(valueB);

        return Math.Sign(a.CompareTo(b)) == Math.Sign(valueA.CompareTo(valueB));
    }

    #endregion

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
