using LanguageExt;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for <see cref="EncinaRequestGuards"/> to verify validation methods
/// for requests, notifications, handlers, and stream requests.
/// </summary>
public class EncinaRequestGuardsTests
{
    #region TryValidateRequest

    /// <summary>
    /// Verifies that TryValidateRequest returns false and a Left error when request is null.
    /// </summary>
    [Fact]
    public void TryValidateRequest_NullRequest_ReturnsFalseWithLeftError()
    {
        // Arrange
        object? request = null;

        // Act
        var isValid = EncinaRequestGuards.TryValidateRequest<string>(request, out var error);

        // Assert
        isValid.ShouldBeFalse();
        error.IsLeft.ShouldBeTrue();
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull));
    }

    /// <summary>
    /// Verifies that TryValidateRequest returns false and error message contains "null".
    /// </summary>
    [Fact]
    public void TryValidateRequest_NullRequest_ErrorMessageContainsNull()
    {
        // Act
        EncinaRequestGuards.TryValidateRequest<string>(null, out var error);

        // Assert
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("null"));
    }

    /// <summary>
    /// Verifies that TryValidateRequest returns true when request is not null.
    /// </summary>
    [Fact]
    public void TryValidateRequest_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new TestCommand();

        // Act
        var isValid = EncinaRequestGuards.TryValidateRequest<string>(request, out _);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region TryValidateNotification

    /// <summary>
    /// Verifies that TryValidateNotification returns false and a Left error when notification is null.
    /// </summary>
    [Fact]
    public void TryValidateNotification_NullNotification_ReturnsFalseWithLeftError()
    {
        // Act
        var isValid = EncinaRequestGuards.TryValidateNotification(null, out var error);

        // Assert
        isValid.ShouldBeFalse();
        error.IsLeft.ShouldBeTrue();
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull));
    }

    /// <summary>
    /// Verifies that TryValidateNotification returns false and error message contains "null".
    /// </summary>
    [Fact]
    public void TryValidateNotification_NullNotification_ErrorMessageContainsNull()
    {
        // Act
        EncinaRequestGuards.TryValidateNotification(null, out var error);

        // Assert
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("null"));
    }

    /// <summary>
    /// Verifies that TryValidateNotification returns true when notification is not null.
    /// </summary>
    [Fact]
    public void TryValidateNotification_ValidNotification_ReturnsTrue()
    {
        // Arrange
        var notification = new TestNotification();

        // Act
        var isValid = EncinaRequestGuards.TryValidateNotification(notification, out _);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region TryValidateHandler

    /// <summary>
    /// Verifies that TryValidateHandler returns false and a Left error when handler is null.
    /// </summary>
    [Fact]
    public void TryValidateHandler_NullHandler_ReturnsFalseWithLeftError()
    {
        // Act
        var isValid = EncinaRequestGuards.TryValidateHandler<string>(
            null,
            typeof(TestCommand),
            typeof(string),
            out var error);

        // Assert
        isValid.ShouldBeFalse();
        error.IsLeft.ShouldBeTrue();
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestHandlerMissing));
    }

    /// <summary>
    /// Verifies that TryValidateHandler error message includes request and response type names.
    /// </summary>
    [Fact]
    public void TryValidateHandler_NullHandler_ErrorMessageIncludesTypeNames()
    {
        // Act
        EncinaRequestGuards.TryValidateHandler<string>(
            null,
            typeof(TestCommand),
            typeof(string),
            out var error);

        // Assert
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e =>
            {
                e.Message.ShouldContain("TestCommand");
                e.Message.ShouldContain("String");
            });
    }

    /// <summary>
    /// Verifies that TryValidateHandler returns true when handler is not null.
    /// </summary>
    [Fact]
    public void TryValidateHandler_ValidHandler_ReturnsTrue()
    {
        // Arrange
        var handler = new object();

        // Act
        var isValid = EncinaRequestGuards.TryValidateHandler<string>(
            handler,
            typeof(TestCommand),
            typeof(string),
            out _);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region TryValidateHandlerType

    /// <summary>
    /// Verifies that TryValidateHandlerType returns false when handler is not of expected type.
    /// </summary>
    [Fact]
    public void TryValidateHandlerType_WrongType_ReturnsFalseWithLeftError()
    {
        // Arrange
        var handler = new object();
        var expectedType = typeof(IRequestHandler<TestCommand, string>);

        // Act
        var isValid = EncinaRequestGuards.TryValidateHandlerType<string>(
            handler,
            expectedType,
            typeof(TestCommand),
            out var error);

        // Assert
        isValid.ShouldBeFalse();
        error.IsLeft.ShouldBeTrue();
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestHandlerTypeMismatch));
    }

    /// <summary>
    /// Verifies that TryValidateHandlerType error message includes handler and expected type names.
    /// </summary>
    [Fact]
    public void TryValidateHandlerType_WrongType_ErrorMessageIncludesTypeNames()
    {
        // Arrange
        var handler = new object();
        var expectedType = typeof(IRequestHandler<TestCommand, string>);

        // Act
        EncinaRequestGuards.TryValidateHandlerType<string>(
            handler,
            expectedType,
            typeof(TestCommand),
            out var error);

        // Assert
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e =>
            {
                e.Message.ShouldContain("Object");
                e.Message.ShouldContain("TestCommand");
            });
    }

    /// <summary>
    /// Verifies that TryValidateHandlerType returns true when handler is of expected type.
    /// </summary>
    [Fact]
    public void TryValidateHandlerType_CorrectType_ReturnsTrue()
    {
        // Arrange
        var handler = new ConcreteHandler();
        var expectedType = typeof(IRequestHandler<TestCommand, string>);

        // Act
        var isValid = EncinaRequestGuards.TryValidateHandlerType<string>(
            handler,
            expectedType,
            typeof(TestCommand),
            out _);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region TryValidateStreamRequest

    /// <summary>
    /// Verifies that TryValidateStreamRequest returns false and a Left error when request is null.
    /// </summary>
    [Fact]
    public void TryValidateStreamRequest_NullRequest_ReturnsFalseWithLeftError()
    {
        // Act
        var isValid = EncinaRequestGuards.TryValidateStreamRequest<string>(null, out var error);

        // Assert
        isValid.ShouldBeFalse();
        error.IsLeft.ShouldBeTrue();
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull));
    }

    /// <summary>
    /// Verifies that TryValidateStreamRequest error message references "stream request".
    /// </summary>
    [Fact]
    public void TryValidateStreamRequest_NullRequest_ErrorMessageContainsStreamRequest()
    {
        // Act
        EncinaRequestGuards.TryValidateStreamRequest<string>(null, out var error);

        // Assert
        error.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("stream request"));
    }

    /// <summary>
    /// Verifies that TryValidateStreamRequest returns true when request is not null.
    /// </summary>
    [Fact]
    public void TryValidateStreamRequest_ValidRequest_ReturnsTrue()
    {
        // Act
        var isValid = EncinaRequestGuards.TryValidateStreamRequest<string>(new object(), out _);

        // Assert
        isValid.ShouldBeTrue();
    }

    #endregion

    #region Test Stubs

    private sealed class TestCommand : ICommand<string> { }

    private sealed class TestNotification : INotification { }

    private sealed class ConcreteHandler : IRequestHandler<TestCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCommand request, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, string>>("ok");
    }

    #endregion
}
