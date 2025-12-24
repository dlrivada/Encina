using FluentAssertions;
using LanguageExt;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for Notification validation in <see cref="EncinaRequestGuards"/>.
/// Tests ensure proper null validation and error handling for notifications (Publish).
/// </summary>
public sealed class NotificationGuardsTests
{
    #region TryValidateNotification Tests

    [Fact]
    public void TryValidateNotification_WithValidNotification_ShouldReturnTrue()
    {
        // Arrange
        var notification = new TestNotification("test message");

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(notification, out var error);

        // Assert
        result.Should().BeTrue();
        error.ShouldBeBottom(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateNotification_WithNullNotification_ShouldReturnFalse()
    {
        // Arrange
        INotification? notification = null;

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(notification, out var error);

        // Assert
        result.Should().BeFalse();
        error.ShouldBeError();

        var encinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"));

        encinaError.GetEncinaCode().Should().Be(EncinaErrorCodes.NotificationNull);
        encinaError.Message.Should().Contain("notification");
        encinaError.Message.Should().Contain("cannot be null");
    }

    [Fact]
    public void TryValidateNotification_WithDifferentNotificationTypes_ShouldHandleCorrectly()
    {
        // Arrange
        TestNotification? nullNotification = null;
        var validNotification = new TestNotification("valid");
        var anotherValidNotification = new AnotherNotification(42);

        // Act
        var result1 = EncinaRequestGuards.TryValidateNotification(nullNotification, out var error1);
        var result2 = EncinaRequestGuards.TryValidateNotification(validNotification, out _);
        var result3 = EncinaRequestGuards.TryValidateNotification(anotherValidNotification, out _);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeTrue();
        result3.Should().BeTrue();

        error1.ShouldBeError();
    }

    [Fact]
    public void TryValidateNotification_ErrorMessage_ShouldBeDescriptive()
    {
        // Arrange
        INotification? notification = null;

        // Act
        EncinaRequestGuards.TryValidateNotification(notification, out var error);

        // Assert
        var encinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        encinaError.Message.Should().Be("The notification cannot be null.");
        encinaError.GetEncinaCode().Should().Be(EncinaErrorCodes.NotificationNull);
    }

    [Fact]
    public void TryValidateNotification_ShouldReturnUnitError()
    {
        // Arrange
        INotification? notification = null;

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(notification, out var error);

        // Assert
        result.Should().BeFalse();

        // Verify the error is Either<EncinaError, Unit>
        error.Match(
            Left: e =>
            {
                e.Should().NotBeNull();
                e.GetEncinaCode().Should().Be(EncinaErrorCodes.NotificationNull);
            },
            Right: _ => throw new InvalidOperationException("Expected Left"));
    }

    #endregion

    #region Test Types

    private sealed record TestNotification(string Message) : INotification;

    private sealed record AnotherNotification(int Value) : INotification;

    #endregion
}
