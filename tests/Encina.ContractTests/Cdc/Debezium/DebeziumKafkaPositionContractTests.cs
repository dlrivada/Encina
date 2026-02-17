using Encina.Cdc.Debezium.Kafka;

namespace Encina.ContractTests.Cdc.Debezium;

/// <summary>
/// Contract tests verifying that <see cref="DebeziumKafkaPosition"/> correctly satisfies
/// the <see cref="Encina.Cdc.Abstractions.CdcPosition"/> base class contract.
/// Additionally tests Kafka-specific comparison semantics (topic/partition/offset).
/// </summary>
[Trait("Category", "Contract")]
public sealed class DebeziumKafkaPositionContractTests
{
    private const string DefaultOffsetJson = "{\"lsn\":100}";
    private const string DefaultTopic = "dbserver1.dbo.Orders";

    #region ToBytes Contract

    /// <summary>
    /// Contract: ToBytes must return a non-null byte array.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_ReturnsNonNull()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var bytes = position.ToBytes();

        // Assert
        bytes.ShouldNotBeNull();
    }

    /// <summary>
    /// Contract: ToBytes must return a non-empty byte array.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_ReturnsNonEmpty()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var bytes = position.ToBytes();

        // Assert
        bytes.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Contract: ToBytes must be deterministic (same input → same output).
    /// </summary>
    [Fact]
    public void Contract_ToBytes_IsDeterministic()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var bytes1 = position.ToBytes();
        var bytes2 = position.ToBytes();

        // Assert
        bytes1.ShouldBe(bytes2);
    }

    /// <summary>
    /// Contract: FromBytes(ToBytes()) round-trip preserves all fields.
    /// </summary>
    [Fact]
    public void Contract_FromBytes_ToBytes_RoundTrip()
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

    #endregion

    #region CompareTo Contract

    /// <summary>
    /// Contract: CompareTo(null) must return a positive value.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Contract: CompareTo(self) must return zero (reflexive).
    /// </summary>
    [Fact]
    public void Contract_CompareTo_Self_ReturnsZero()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = position.CompareTo(position);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Contract: CompareTo must be consistent — if a &lt; b then b &gt; a (antisymmetric).
    /// </summary>
    [Fact]
    public void Contract_CompareTo_Antisymmetric()
    {
        // Arrange
        var posA = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 10);
        var posB = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 20);

        // Act
        var aToB = posA.CompareTo(posB);
        var bToA = posB.CompareTo(posA);

        // Assert
        aToB.ShouldBeLessThan(0);
        bToA.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Contract: Two positions with equal data should compare as zero.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_EqualPositions_ReturnsZero()
    {
        // Arrange
        var pos1 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);
        var pos2 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = pos1.CompareTo(pos2);

        // Assert
        result.ShouldBe(0);
    }

    #endregion

    #region Kafka-Specific Comparison Contract

    /// <summary>
    /// Contract: Same topic+partition positions are compared by Kafka offset.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_SameTopicPartition_UsesOffsetOrdering()
    {
        // Arrange
        var earlier = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 100);
        var later = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 200);

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0);
        later.CompareTo(earlier).ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Contract: Cross-partition positions are compared by topic, then partition.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_CrossPartition_UsesTopicThenPartitionOrdering()
    {
        // Arrange — same topic, different partitions
        var partition0 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 999);
        var partition5 = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 5, 1);

        // Act & Assert — partition 0 < partition 5 regardless of offset
        partition0.CompareTo(partition5).ShouldBeLessThan(0);
        partition5.CompareTo(partition0).ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Contract: Different topics are compared by topic ordinal, ignoring offset.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_DifferentTopics_UsesTopicOrdering()
    {
        // Arrange — "alpha" < "beta" in ordinal
        var posAlpha = new DebeziumKafkaPosition(DefaultOffsetJson, "alpha.dbo.Orders", 0, 999);
        var posBeta = new DebeziumKafkaPosition(DefaultOffsetJson, "beta.dbo.Orders", 0, 1);

        // Act & Assert
        posAlpha.CompareTo(posBeta).ShouldBeLessThan(0);
        posBeta.CompareTo(posAlpha).ShouldBeGreaterThan(0);
    }

    #endregion

    #region ToString Contract

    /// <summary>
    /// Contract: ToString must return a non-null string.
    /// </summary>
    [Fact]
    public void Contract_ToString_ReturnsNonNull()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Contract: ToString must return a non-empty string.
    /// </summary>
    [Fact]
    public void Contract_ToString_ReturnsNonEmpty()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, DefaultTopic, 0, 42);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Contract: ToString must contain meaningful Kafka position info.
    /// </summary>
    [Fact]
    public void Contract_ToString_ContainsKafkaInfo()
    {
        // Arrange
        var position = new DebeziumKafkaPosition(DefaultOffsetJson, "my-topic", 2, 100);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldContain("my-topic");
        result.ShouldContain("2");
        result.ShouldContain("100");
    }

    #endregion
}
