using Encina.Cdc.Debezium.Kafka;

namespace Encina.GuardTests.Cdc.Debezium;

/// <summary>
/// Guard clause tests for <see cref="DebeziumKafkaPosition"/>.
/// Verifies that null/empty/whitespace parameters are properly guarded.
/// </summary>
public sealed class DebeziumKafkaPositionGuardTests
{
    #region Constructor Guards — offsetJson

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when offsetJson is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOffsetJson_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition(null!, "topic", 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when offsetJson is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyOffsetJson_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition("", "topic", 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when offsetJson is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceOffsetJson_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition("   ", "topic", 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    #endregion

    #region Constructor Guards — topic

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when topic is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopic_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition("{\"key\":1}", null!, 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topic");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when topic is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTopic_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition("{\"key\":1}", "", 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("topic");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when topic is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTopic_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumKafkaPosition("{\"key\":1}", "   ", 0, 0);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("topic");
    }

    #endregion

    #region FromBytes Guards

    /// <summary>
    /// Verifies that FromBytes throws ArgumentNullException when bytes is null.
    /// </summary>
    [Fact]
    public void FromBytes_NullBytes_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => DebeziumKafkaPosition.FromBytes(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bytes");
    }

    #endregion
}
