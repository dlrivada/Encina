using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;

namespace Encina.GuardTests.Cdc.Messaging;

/// <summary>
/// Guard clause tests for <see cref="CdcChangeNotification"/>.
/// Verifies that the FromChangeEvent factory method properly guards its parameters.
/// </summary>
public sealed class CdcChangeNotificationGuardTests
{
    #region FromChangeEvent Guards

    /// <summary>
    /// Verifies that FromChangeEvent throws ArgumentNullException when changeEvent is null.
    /// </summary>
    [Fact]
    public void FromChangeEvent_NullChangeEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        ChangeEvent changeEvent = null!;

        // Act
        var act = () => CdcChangeNotification.FromChangeEvent(changeEvent);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("changeEvent");
    }

    /// <summary>
    /// Verifies that FromChangeEvent throws ArgumentNullException when topicPattern is null.
    /// </summary>
    [Fact]
    public void FromChangeEvent_NullTopicPattern_ShouldThrowArgumentNullException()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();
        string topicPattern = null!;

        // Act
        var act = () => CdcChangeNotification.FromChangeEvent(changeEvent, topicPattern);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topicPattern");
    }

    /// <summary>
    /// Verifies that FromChangeEvent throws ArgumentException when topicPattern is empty.
    /// </summary>
    [Fact]
    public void FromChangeEvent_EmptyTopicPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();

        // Act
        var act = () => CdcChangeNotification.FromChangeEvent(changeEvent, "");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("topicPattern");
    }

    /// <summary>
    /// Verifies that FromChangeEvent throws ArgumentException when topicPattern is whitespace.
    /// </summary>
    [Fact]
    public void FromChangeEvent_WhitespaceTopicPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var changeEvent = CreateChangeEvent();

        // Act
        var act = () => CdcChangeNotification.FromChangeEvent(changeEvent, "   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("topicPattern");
    }

    #endregion

    #region Test Helpers

    private static ChangeEvent CreateChangeEvent()
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        return new ChangeEvent("Orders", ChangeOperation.Insert, null, new { Id = 1 }, metadata);
    }

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
