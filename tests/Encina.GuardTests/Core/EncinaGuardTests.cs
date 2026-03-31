using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="Encina"/> to verify constructor null guards,
/// Send with null request, and Publish with null notification.
/// </summary>
public class EncinaGuardTests
{
    #region Constructor

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when scopeFactory is null.
    /// </summary>
    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceScopeFactory scopeFactory = null!;

        // Act & Assert
        var act = () => new Encina(scopeFactory);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with a valid scopeFactory and null optional parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidScopeFactory_NullOptionals_DoesNotThrow()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act & Assert
        var act = () => new Encina(scopeFactory, logger: null, notificationOptions: null);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that the constructor stores the scopeFactory.
    /// </summary>
    [Fact]
    public void Constructor_ValidScopeFactory_StoresScopeFactory()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory);

        // Assert
        sut._scopeFactory.ShouldBeSameAs(scopeFactory);
    }

    /// <summary>
    /// Verifies that the constructor assigns a NullLogger when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_AssignsNullLogger()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory, logger: null);

        // Assert
        sut._logger.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor uses default notification options when null.
    /// </summary>
    [Fact]
    public void Constructor_NullNotificationOptions_UsesDefaults()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory, notificationOptions: null);

        // Assert
        sut._notificationOptions.ShouldNotBeNull();
        sut._notificationOptions.Strategy.ShouldBe(NotificationDispatchStrategy.Sequential);
    }

    #endregion

    #region Send

    /// <summary>
    /// Verifies that Send returns a Left error when request is null.
    /// </summary>
    [Fact]
    public async Task Send_NullRequest_ReturnsLeftError()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        IRequest<string> request = null!;

        // Act
        var result = await sut.Send<string>(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull));
    }

    /// <summary>
    /// Verifies that Send returns a Left error with the correct message when request is null.
    /// </summary>
    [Fact]
    public async Task Send_NullRequest_ErrorMessageContainsNull()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        IRequest<string> request = null!;

        // Act
        var result = await sut.Send<string>(request);

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("null"));
    }

    #endregion

    #region Publish

    /// <summary>
    /// Verifies that Publish returns a Left error when notification is null.
    /// </summary>
    [Fact]
    public async Task Publish_NullNotification_ReturnsLeftError()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        TestNotification notification = null!;

        // Act
        var result = await sut.Publish(notification);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull));
    }

    /// <summary>
    /// Verifies that Publish returns a Left error with the correct message when notification is null.
    /// </summary>
    [Fact]
    public async Task Publish_NullNotification_ErrorMessageContainsNull()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        TestNotification notification = null!;

        // Act
        var result = await sut.Publish(notification);

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("null"));
    }

    #endregion

    #region IsCancellationCode (internal)

    /// <summary>
    /// Verifies that IsCancellationCode returns false for null or whitespace input.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCancellationCode_NullOrWhitespace_ReturnsFalse(string? code)
    {
        // Act
        var result = Encina.IsCancellationCode(code!);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that IsCancellationCode returns true for codes containing "cancelled".
    /// </summary>
    [Theory]
    [InlineData("handler.cancelled")]
    [InlineData("CANCELLED")]
    [InlineData("request_Cancelled")]
    public void IsCancellationCode_ContainsCancelled_ReturnsTrue(string code)
    {
        // Act
        var result = Encina.IsCancellationCode(code);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that IsCancellationCode returns false for non-cancellation codes.
    /// </summary>
    [Theory]
    [InlineData("handler.failed")]
    [InlineData("validation.error")]
    [InlineData("unknown")]
    public void IsCancellationCode_NonCancellationCode_ReturnsFalse(string code)
    {
        // Act
        var result = Encina.IsCancellationCode(code);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Test Stubs

    private sealed class TestNotification : INotification { }

    #endregion
}
