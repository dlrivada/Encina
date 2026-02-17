using System.Text;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumCdcPosition"/>.
/// Verifies construction, serialization, comparison, and string representation.
/// </summary>
public sealed class DebeziumCdcPositionTests
{
    #region Constructor

    /// <summary>
    /// Verifies that the constructor stores the OffsetJson correctly.
    /// </summary>
    [Fact]
    public void Constructor_ValidOffsetJson_ShouldStoreOffsetJson()
    {
        // Arrange
        var offsetJson = "{\"lsn\":12345}";

        // Act
        var position = new DebeziumCdcPosition(offsetJson);

        // Assert
        position.OffsetJson.ShouldBe(offsetJson);
    }

    #endregion

    #region ToBytes

    /// <summary>
    /// Verifies that ToBytes returns valid UTF-8 bytes of the OffsetJson.
    /// </summary>
    [Fact]
    public void ToBytes_ShouldReturnUtf8BytesOfOffsetJson()
    {
        // Arrange
        var offsetJson = "{\"lsn\":12345}";
        var position = new DebeziumCdcPosition(offsetJson);

        // Act
        var bytes = position.ToBytes();

        // Assert
        var decoded = Encoding.UTF8.GetString(bytes);
        decoded.ShouldBe(offsetJson);
    }

    /// <summary>
    /// Verifies that ToBytes returns non-empty byte array.
    /// </summary>
    [Fact]
    public void ToBytes_ShouldReturnNonEmptyArray()
    {
        // Arrange
        var position = new DebeziumCdcPosition("{\"x\":1}");

        // Act
        var bytes = position.ToBytes();

        // Assert
        bytes.ShouldNotBeEmpty();
    }

    #endregion

    #region FromBytes

    /// <summary>
    /// Verifies that FromBytes round-trips correctly with ToBytes.
    /// </summary>
    [Fact]
    public void FromBytes_RoundTrip_ShouldPreserveOffsetJson()
    {
        // Arrange
        var offsetJson = "{\"lsn\":99999,\"file\":\"binlog.000001\"}";
        var original = new DebeziumCdcPosition(offsetJson);

        // Act
        var bytes = original.ToBytes();
        var restored = DebeziumCdcPosition.FromBytes(bytes);

        // Assert
        restored.OffsetJson.ShouldBe(original.OffsetJson);
    }

    /// <summary>
    /// Verifies that FromBytes correctly parses a valid byte array.
    /// </summary>
    [Fact]
    public void FromBytes_ValidUtf8Bytes_ShouldReturnCorrectPosition()
    {
        // Arrange
        var offsetJson = "{\"pos\":42}";
        var bytes = Encoding.UTF8.GetBytes(offsetJson);

        // Act
        var position = DebeziumCdcPosition.FromBytes(bytes);

        // Assert
        position.OffsetJson.ShouldBe(offsetJson);
    }

    #endregion

    #region CompareTo

    /// <summary>
    /// Verifies that CompareTo with null returns a positive value.
    /// </summary>
    [Fact]
    public void CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var position = new DebeziumCdcPosition("{\"x\":1}");

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with equal offsets returns zero.
    /// </summary>
    [Fact]
    public void CompareTo_EqualOffsets_ShouldReturnZero()
    {
        // Arrange
        var position1 = new DebeziumCdcPosition("{\"lsn\":100}");
        var position2 = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that CompareTo with a lesser offset returns negative (ordinal comparison).
    /// </summary>
    [Fact]
    public void CompareTo_LesserOffset_ShouldReturnNegative()
    {
        // Arrange — ordinal: "A" < "B"
        var position1 = new DebeziumCdcPosition("A");
        var position2 = new DebeziumCdcPosition("B");

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeLessThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with a greater offset returns positive (ordinal comparison).
    /// </summary>
    [Fact]
    public void CompareTo_GreaterOffset_ShouldReturnPositive()
    {
        // Arrange — ordinal: "B" > "A"
        var position1 = new DebeziumCdcPosition("B");
        var position2 = new DebeziumCdcPosition("A");

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo throws ArgumentException for incompatible position types.
    /// </summary>
    [Fact]
    public void CompareTo_IncompatibleType_ShouldThrowArgumentException()
    {
        // Arrange
        var debeziumPos = new DebeziumCdcPosition("{\"x\":1}");
        var otherPos = new TestCdcPosition(42);

        // Act & Assert
        Should.Throw<ArgumentException>(() => debeziumPos.CompareTo(otherPos));
    }

    #endregion

    #region ToString

    /// <summary>
    /// Verifies that ToString includes the "Debezium-Offset:" prefix.
    /// </summary>
    [Fact]
    public void ToString_ShouldIncludePrefix()
    {
        // Arrange
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldStartWith("Debezium-Offset:");
    }

    /// <summary>
    /// Verifies that ToString truncates at 50 characters for long offsets.
    /// </summary>
    [Fact]
    public void ToString_LongOffset_ShouldTruncateAt50Characters()
    {
        // Arrange — create an offset longer than 50 characters
        var longOffset = new string('x', 100);
        var position = new DebeziumCdcPosition(longOffset);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldBe($"Debezium-Offset:{longOffset[..50]}");
    }

    /// <summary>
    /// Verifies that ToString does not truncate short offsets.
    /// </summary>
    [Fact]
    public void ToString_ShortOffset_ShouldNotTruncate()
    {
        // Arrange
        var shortOffset = "{\"lsn\":1}";
        var position = new DebeziumCdcPosition(shortOffset);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldBe($"Debezium-Offset:{shortOffset}");
    }

    #endregion

    #region Test Helpers

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
}
