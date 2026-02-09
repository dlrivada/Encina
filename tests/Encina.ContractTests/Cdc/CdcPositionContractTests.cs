using Encina.Cdc.Abstractions;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that the abstract <see cref="CdcPosition"/> base class contract
/// is correctly satisfied by concrete implementations. Tests cover serialization via
/// <see cref="CdcPosition.ToBytes"/>, comparison ordering via <see cref="CdcPosition.CompareTo"/>,
/// string representation via <see cref="CdcPosition.ToString"/>, and value equality semantics.
/// Uses a test-only <c>TestCdcPosition(long)</c> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CdcPositionContractTests
{
    #region Test Helpers

    /// <summary>
    /// Test-only CDC position backed by a simple <see cref="long"/> value.
    /// </summary>
    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion

    #region ToBytes Contract

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToBytes"/> must return a non-null byte array.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_ReturnsNonNull()
    {
        // Arrange
        var position = new TestCdcPosition(42);

        // Act
        var bytes = position.ToBytes();

        // Assert
        bytes.ShouldNotBeNull("ToBytes must return a non-null byte array");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToBytes"/> must return a non-empty byte array
    /// so that the position can be persisted and later restored.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_ReturnsNonEmptyBytes()
    {
        // Arrange
        var position = new TestCdcPosition(1);

        // Act
        var bytes = position.ToBytes();

        // Assert
        bytes.Length.ShouldBeGreaterThan(0,
            "ToBytes must return a non-empty byte array for persistence");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToBytes"/> must produce consistent output
    /// for the same position value (idempotent serialization).
    /// </summary>
    [Fact]
    public void Contract_ToBytes_IsIdempotent()
    {
        // Arrange
        var position = new TestCdcPosition(999);

        // Act
        var bytes1 = position.ToBytes();
        var bytes2 = position.ToBytes();

        // Assert
        bytes1.ShouldBe(bytes2,
            "ToBytes must produce the same byte array for the same position (idempotent)");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToBytes"/> must produce different byte arrays
    /// for positions with different values.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_DifferentPositions_ProduceDifferentBytes()
    {
        // Arrange
        var position1 = new TestCdcPosition(10);
        var position2 = new TestCdcPosition(20);

        // Act
        var bytes1 = position1.ToBytes();
        var bytes2 = position2.ToBytes();

        // Assert
        bytes1.ShouldNotBe(bytes2,
            "Different position values must produce different byte representations");
    }

    #endregion

    #region CompareTo Contract

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> must return a negative value
    /// when this position precedes the other.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_LessThan_ReturnsNegative()
    {
        // Arrange
        var earlier = new TestCdcPosition(10);
        var later = new TestCdcPosition(20);

        // Act
        var result = earlier.CompareTo(later);

        // Assert
        result.ShouldBeLessThan(0,
            "CompareTo must return negative when this position precedes the other");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> must return a positive value
    /// when this position follows the other.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_GreaterThan_ReturnsPositive()
    {
        // Arrange
        var earlier = new TestCdcPosition(10);
        var later = new TestCdcPosition(20);

        // Act
        var result = later.CompareTo(earlier);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo must return positive when this position follows the other");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> must return zero when both
    /// positions represent the same point in the change stream.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_Equal_ReturnsZero()
    {
        // Arrange
        var position1 = new TestCdcPosition(42);
        var position2 = new TestCdcPosition(42);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBe(0,
            "CompareTo must return zero when positions have the same value");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> with <c>null</c> must return
    /// a positive value, placing non-null positions after null.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var position = new TestCdcPosition(1);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo(null) must return positive (non-null is greater than null)");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> must maintain transitivity.
    /// If a &lt; b and b &lt; c, then a &lt; c.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_IsTransitive()
    {
        // Arrange
        var a = new TestCdcPosition(1);
        var b = new TestCdcPosition(2);
        var c = new TestCdcPosition(3);

        // Act & Assert
        a.CompareTo(b).ShouldBeLessThan(0, "a < b");
        b.CompareTo(c).ShouldBeLessThan(0, "b < c");
        a.CompareTo(c).ShouldBeLessThan(0, "a < c (transitivity)");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.CompareTo"/> must be anti-symmetric.
    /// If a &lt; b, then b &gt; a.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_IsAntiSymmetric()
    {
        // Arrange
        var smaller = new TestCdcPosition(5);
        var larger = new TestCdcPosition(10);

        // Act
        var forward = smaller.CompareTo(larger);
        var reverse = larger.CompareTo(smaller);

        // Assert
        forward.ShouldBeLessThan(0, "smaller < larger");
        reverse.ShouldBeGreaterThan(0, "larger > smaller (anti-symmetry)");
    }

    #endregion

    #region ToString Contract

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToString"/> must return a non-null,
    /// non-empty string for diagnostics and logging.
    /// </summary>
    [Fact]
    public void Contract_ToString_ReturnsMeaningfulRepresentation()
    {
        // Arrange
        var position = new TestCdcPosition(42);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "ToString must return a meaningful (non-null, non-empty) string representation");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition.ToString"/> must produce different strings
    /// for different position values to aid in diagnostics.
    /// </summary>
    [Fact]
    public void Contract_ToString_DifferentPositions_ProduceDifferentStrings()
    {
        // Arrange
        var position1 = new TestCdcPosition(10);
        var position2 = new TestCdcPosition(20);

        // Act
        var str1 = position1.ToString();
        var str2 = position2.ToString();

        // Assert
        str1.ShouldNotBe(str2,
            "Different position values must produce different string representations");
    }

    #endregion

    #region Equality Semantics Contract

    /// <summary>
    /// Contract: Two positions created with the same underlying value should be
    /// comparable as equal via <see cref="CdcPosition.CompareTo"/>.
    /// </summary>
    [Fact]
    public void Contract_SameValue_CompareAsEqual()
    {
        // Arrange
        var position1 = new TestCdcPosition(100);
        var position2 = new TestCdcPosition(100);

        // Act
        var comparison = position1.CompareTo(position2);

        // Assert
        comparison.ShouldBe(0,
            "Two positions with the same underlying value must compare as equal");
    }

    /// <summary>
    /// Contract: Two positions created with the same value must produce identical
    /// byte representations via <see cref="CdcPosition.ToBytes"/>.
    /// </summary>
    [Fact]
    public void Contract_SameValue_ProduceSameBytes()
    {
        // Arrange
        var position1 = new TestCdcPosition(100);
        var position2 = new TestCdcPosition(100);

        // Act
        var bytes1 = position1.ToBytes();
        var bytes2 = position2.ToBytes();

        // Assert
        bytes1.ShouldBe(bytes2,
            "Two positions with the same value must produce identical byte representations");
    }

    #endregion

    #region Abstract Class Contract

    /// <summary>
    /// Contract: <see cref="CdcPosition"/> must be abstract, preventing direct instantiation.
    /// </summary>
    [Fact]
    public void Contract_CdcPosition_IsAbstract()
    {
        typeof(CdcPosition).IsAbstract.ShouldBeTrue(
            "CdcPosition must be abstract to enforce provider-specific implementations");
    }

    /// <summary>
    /// Contract: <see cref="CdcPosition"/> must implement <see cref="IComparable{T}"/>
    /// for monotonic ordering verification.
    /// </summary>
    [Fact]
    public void Contract_CdcPosition_ImplementsIComparable()
    {
        typeof(IComparable<CdcPosition>).IsAssignableFrom(typeof(CdcPosition))
            .ShouldBeTrue("CdcPosition must implement IComparable<CdcPosition>");
    }

    #endregion
}
