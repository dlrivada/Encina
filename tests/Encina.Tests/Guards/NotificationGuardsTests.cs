using Shouldly;
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
        result.ShouldBeTrue();
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
        result.ShouldBeFalse();
        error.ShouldBeError();

        var encinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"));

        encinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull);
        encinaError.Message.ShouldContain("notification");
        encinaError.Message.ShouldContain("cannot be null");
    }

    [Fact]
    public void TryValidateNotification_NullNotification_ReturnsFalseAndSetsError()
    {
        // Arrange
        TestNotification? nullNotification = null;

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(nullNotification, out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldBeError();
    }

    [Fact]
    public void TryValidateNotification_TestNotification_ReturnsTrue()
    {
        // Arrange
        var validNotification = new TestNotification("valid");

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(validNotification, out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom();
    }

    [Fact]
    public void TryValidateNotification_AnotherNotification_ReturnsTrue()
    {
        // Arrange
        var anotherValidNotification = new AnotherNotification(42);

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(anotherValidNotification, out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom();
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

        encinaError.Message.ShouldBe("The notification cannot be null.");
        encinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull);
    }

    [Fact]
    public void TryValidateNotification_ShouldReturnUnitError()
    {
        // Arrange
        INotification? notification = null;

        // Act
        var result = EncinaRequestGuards.TryValidateNotification(notification, out var error);

        // Assert
        result.ShouldBeFalse();

        // Verify the error is Either<EncinaError, Unit>
        error.Match(
            Left: e =>
            {
                // EncinaError is a struct, so it's never null
                e.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull);
            },
            Right: _ => throw new InvalidOperationException("Expected Left"));
    }

    #endregion

    #region Test Types

    private sealed record TestNotification(string Message) : INotification;

    private sealed record AnotherNotification(int Value) : INotification;

    #endregion
}
