namespace Encina.NATS.Tests;

/// <summary>
/// Unit tests for <see cref="NATSPublishAck"/> record.
/// </summary>
public sealed class NATSPublishAckTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        var ack = new NATSPublishAck("test-stream", 42UL, false);

        // Assert
        ack.Stream.ShouldBe("test-stream");
        ack.Sequence.ShouldBe(42UL);
        ack.Duplicate.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithDuplicate_ShouldSetDuplicateFlag()
    {
        // Arrange & Act
        var ack = new NATSPublishAck("test-stream", 1UL, true);

        // Assert
        ack.Duplicate.ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoAcksWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var ack1 = new NATSPublishAck("stream", 100UL, false);
        var ack2 = new NATSPublishAck("stream", 100UL, false);

        // Act & Assert
        ack1.ShouldBe(ack2);
        (ack1 == ack2).ShouldBeTrue();
        ack1.GetHashCode().ShouldBe(ack2.GetHashCode());
    }

    [Fact]
    public void Equality_TwoAcksWithDifferentStream_ShouldNotBeEqual()
    {
        // Arrange
        var ack1 = new NATSPublishAck("stream-1", 100UL, false);
        var ack2 = new NATSPublishAck("stream-2", 100UL, false);

        // Act & Assert
        ack1.ShouldNotBe(ack2);
        (ack1 != ack2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoAcksWithDifferentSequence_ShouldNotBeEqual()
    {
        // Arrange
        var ack1 = new NATSPublishAck("stream", 100UL, false);
        var ack2 = new NATSPublishAck("stream", 200UL, false);

        // Act & Assert
        ack1.ShouldNotBe(ack2);
    }

    [Fact]
    public void Equality_TwoAcksWithDifferentDuplicate_ShouldNotBeEqual()
    {
        // Arrange
        var ack1 = new NATSPublishAck("stream", 100UL, false);
        var ack2 = new NATSPublishAck("stream", 100UL, true);

        // Act & Assert
        ack1.ShouldNotBe(ack2);
    }

    [Fact]
    public void ToString_ShouldReturnReadableFormat()
    {
        // Arrange
        var ack = new NATSPublishAck("my-stream", 42UL, false);

        // Act
        var result = ack.ToString();

        // Assert
        result.ShouldContain("my-stream");
        result.ShouldContain("42");
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = new NATSPublishAck("original-stream", 1UL, false);

        // Act
        var modified = original with { Stream = "modified-stream" };

        // Assert
        modified.Stream.ShouldBe("modified-stream");
        modified.Sequence.ShouldBe(1UL);
        modified.Duplicate.ShouldBeFalse();
        original.Stream.ShouldBe("original-stream"); // Original unchanged
    }

    [Fact]
    public void Deconstruct_ShouldProvideAllValues()
    {
        // Arrange
        var ack = new NATSPublishAck("test-stream", 99UL, true);

        // Act
        var (stream, sequence, duplicate) = ack;

        // Assert
        stream.ShouldBe("test-stream");
        sequence.ShouldBe(99UL);
        duplicate.ShouldBeTrue();
    }

    [Fact]
    public void EmptyStream_ShouldBeAllowed()
    {
        // Arrange & Act
        var ack = new NATSPublishAck(string.Empty, 0UL, false);

        // Assert
        ack.Stream.ShouldBe(string.Empty);
        ack.Sequence.ShouldBe(0UL);
    }
}
