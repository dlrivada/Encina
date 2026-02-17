using System.Text;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium.Kafka;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumKafkaPosition"/>.
/// Verifies construction, serialization, comparison, and string representation.
/// </summary>
public sealed class DebeziumKafkaPositionTests
{
    private const string DefaultOffsetJson = "{\"lsn\":12345}";
    private const string DefaultTopic = "dbserver1.dbo.Orders";
    private const int DefaultPartition = 0;
    private const long DefaultOffset = 42;

    #region Constructor

    /// <summary>
    /// Verifies that the constructor stores all properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_ShouldStoreAllProperties()
    {
        // Act
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, DefaultPartition, DefaultOffset);

        // Assert
        position.OffsetJson.ShouldBe(DefaultOffsetJson);
        position.Topic.ShouldBe(DefaultTopic);
        position.Partition.ShouldBe(DefaultPartition);
        position.Offset.ShouldBe(DefaultOffset);
    }

    #endregion

    #region ToBytes

    /// <summary>
    /// Verifies that ToBytes returns valid JSON containing all 4 fields.
    /// </summary>
    [Fact]
    public void ToBytes_ShouldReturnValidJsonWithAllFields()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, DefaultPartition, DefaultOffset);

        // Act
        var bytes = position.ToBytes();
        var json = Encoding.UTF8.GetString(bytes);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        root.GetProperty("offsetJson").GetString().ShouldBe(DefaultOffsetJson);
        root.GetProperty("topic").GetString().ShouldBe(DefaultTopic);
        root.GetProperty("partition").GetInt32().ShouldBe(DefaultPartition);
        root.GetProperty("offset").GetInt64().ShouldBe(DefaultOffset);
    }

    /// <summary>
    /// Verifies that ToBytes returns non-empty byte array.
    /// </summary>
    [Fact]
    public void ToBytes_ShouldReturnNonEmptyArray()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, DefaultPartition, DefaultOffset);

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
    public void FromBytes_RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var original = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 3, 999);

        // Act
        var bytes = original.ToBytes();
        var restored = DebeziumKafkaPosition.FromBytes(bytes);

        // Assert
        restored.OffsetJson.ShouldBe(original.OffsetJson);
        restored.Topic.ShouldBe(original.Topic);
        restored.Partition.ShouldBe(original.Partition);
        restored.Offset.ShouldBe(original.Offset);
    }

    /// <summary>
    /// Verifies that FromBytes throws JsonException for invalid JSON.
    /// </summary>
    [Fact]
    public void FromBytes_InvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var invalidBytes = Encoding.UTF8.GetBytes("not valid json");

        // Act & Assert
        Should.Throw<JsonException>(() => DebeziumKafkaPosition.FromBytes(invalidBytes));
    }

    /// <summary>
    /// Verifies that FromBytes throws when a required property is missing.
    /// </summary>
    [Fact]
    public void FromBytes_MissingProperty_ShouldThrow()
    {
        // Arrange — JSON missing "topic" property
        var incompleteJson = "{\"offsetJson\":\"{}\",\"partition\":0,\"offset\":0}";
        var bytes = Encoding.UTF8.GetBytes(incompleteJson);

        // Act & Assert
        Should.Throw<KeyNotFoundException>(() => DebeziumKafkaPosition.FromBytes(bytes));
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
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, DefaultPartition, DefaultOffset);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with same topic+partition and lower offset returns negative.
    /// </summary>
    [Fact]
    public void CompareTo_SameTopicPartition_LowerOffset_ShouldReturnNegative()
    {
        // Arrange
        var position1 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 10);
        var position2 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 20);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeLessThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with same topic+partition and equal offset returns zero.
    /// </summary>
    [Fact]
    public void CompareTo_SameTopicPartition_EqualOffset_ShouldReturnZero()
    {
        // Arrange
        var position1 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);
        var position2 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that CompareTo with same topic+partition and higher offset returns positive.
    /// </summary>
    [Fact]
    public void CompareTo_SameTopicPartition_HigherOffset_ShouldReturnPositive()
    {
        // Arrange
        var position1 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 20);
        var position2 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 10);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with different topics compares by topic ordinal.
    /// </summary>
    [Fact]
    public void CompareTo_DifferentTopic_ShouldCompareByTopicOrdinal()
    {
        // Arrange — "A" < "B" in ordinal
        var position1 = new DebeziumKafkaPosition(DefaultOffsetJson, "A.dbo.Orders", 0, 100);
        var position2 = new DebeziumKafkaPosition(DefaultOffsetJson, "B.dbo.Orders", 0, 1);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeLessThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo with same topic but different partition compares by partition.
    /// </summary>
    [Fact]
    public void CompareTo_SameTopicDifferentPartition_ShouldCompareByPartition()
    {
        // Arrange
        var position1 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 1, 100);
        var position2 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 3, 1);

        // Act
        var result = position1.CompareTo(position2);

        // Assert
        result.ShouldBeLessThan(0);
    }

    /// <summary>
    /// Verifies that CompareTo throws ArgumentException for incompatible position types.
    /// </summary>
    [Fact]
    public void CompareTo_IncompatibleType_ShouldThrowArgumentException()
    {
        // Arrange
        var kafkaPos = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, DefaultPartition, DefaultOffset);
        var otherPos = new TestCdcPosition(42);

        // Act & Assert
        Should.Throw<ArgumentException>(() => kafkaPos.CompareTo(otherPos));
    }

    #endregion

    #region ToString

    /// <summary>
    /// Verifies that ToString returns the expected Kafka position format.
    /// </summary>
    [Fact]
    public void ToString_ShouldReturnKafkaFormat()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, "my-topic", 2, 100);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldBe("Kafka:my-topic[2]@100");
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
