using Encina.Cdc.Debezium;

namespace Encina.ContractTests.Cdc.Debezium;

/// <summary>
/// Contract tests verifying that <see cref="DebeziumCdcPosition"/> correctly satisfies
/// the <see cref="Encina.Cdc.Abstractions.CdcPosition"/> base class contract.
/// Tests cover serialization via ToBytes, comparison ordering via CompareTo,
/// and string representation via ToString.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DebeziumCdcPositionContractTests
{
    #region ToBytes Contract

    /// <summary>
    /// Contract: ToBytes must return a non-null byte array.
    /// </summary>
    [Fact]
    public void Contract_ToBytes_ReturnsNonNull()
    {
        // Arrange
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var bytes1 = position.ToBytes();
        var bytes2 = position.ToBytes();

        // Assert
        bytes1.ShouldBe(bytes2);
    }

    /// <summary>
    /// Contract: FromBytes(ToBytes()) round-trip preserves data.
    /// </summary>
    [Fact]
    public void Contract_FromBytes_ToBytes_RoundTrip()
    {
        // Arrange
        var original = new DebeziumCdcPosition("{\"lsn\":12345,\"file\":\"binlog.000001\"}");

        // Act
        var bytes = original.ToBytes();
        var restored = DebeziumCdcPosition.FromBytes(bytes);

        // Assert
        restored.OffsetJson.ShouldBe(original.OffsetJson);
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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var result = position.CompareTo(position);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Contract: CompareTo must be consistent — if a &lt; b then b &gt; a.
    /// </summary>
    [Fact]
    public void Contract_CompareTo_ConsistentOrdering()
    {
        // Arrange — ordinal: "A" < "B"
        var posA = new DebeziumCdcPosition("A-offset");
        var posB = new DebeziumCdcPosition("B-offset");

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
        var pos1 = new DebeziumCdcPosition("{\"lsn\":100}");
        var pos2 = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var result = pos1.CompareTo(pos2);

        // Assert
        result.ShouldBe(0);
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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

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
        var position = new DebeziumCdcPosition("{\"lsn\":100}");

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace();
    }

    #endregion
}
