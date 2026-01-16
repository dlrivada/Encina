using Encina.Testing.Shouldly;
using LanguageExt;

namespace Encina.UnitTests.Testing.Shouldly;

/// <summary>
/// Helper class to create EncinaError instances for testing.
/// </summary>
internal static class TestEncinaErrors
{
    public static EncinaError NotFound(string code, string message) =>
        EncinaErrors.Create(code, message);

    public static EncinaError Validation(string code, string message) =>
        EncinaErrors.Create(code, message);

    public static EncinaError Authorization(string code, string message) =>
        EncinaErrors.Create(code, message);

    public static EncinaError Conflict(string code, string message) =>
        EncinaErrors.Create(code, message);

    public static EncinaError Internal(string code, string message) =>
        EncinaErrors.Create(code, message);
}

/// <summary>
/// Unit tests for <see cref="EitherShouldlyExtensions"/>.
/// </summary>
public sealed class EitherShouldlyExtensionsTests
{
    #region Success Assertions Tests

    [Fact]
    public void ShouldBeSuccess_WhenRight_ReturnsValue()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act
        var value = result.ShouldBeSuccess();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public void ShouldBeSuccess_WhenLeft_ThrowsShouldAssertException()
    {
        // Arrange
        Either<string, int> result = "Error occurred";

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldBeSuccess());
    }

    [Fact]
    public void ShouldBeSuccess_WithExpectedValue_WhenMatches_Succeeds()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert - should not throw
        result.ShouldBeSuccess(42);
    }

    [Fact]
    public void ShouldBeSuccess_WithExpectedValue_WhenNotMatches_Throws()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldBeSuccess(100));
    }

    [Fact]
    public void ShouldBeSuccess_WithValidator_InvokesValidator()
    {
        // Arrange
        Either<string, int> result = 42;
        var validatorCalled = false;

        // Act
        result.ShouldBeSuccess(value =>
        {
            validatorCalled = true;
            value.ShouldBeGreaterThan(0);
        });

        // Assert
        validatorCalled.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeRight_IsAliasForShouldBeSuccess()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act
        var value = result.ShouldBeRight();

        // Assert
        value.ShouldBe(42);
    }

    #endregion

    #region Error Assertions Tests

    [Fact]
    public void ShouldBeError_WhenLeft_ReturnsError()
    {
        // Arrange
        Either<string, int> result = "Error message";

        // Act
        var error = result.ShouldBeError();

        // Assert
        error.ShouldBe("Error message");
    }

    [Fact]
    public void ShouldBeError_WhenRight_ThrowsShouldAssertException()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() => result.ShouldBeError());
    }

    [Fact]
    public void ShouldBeError_WithValidator_InvokesValidator()
    {
        // Arrange
        Either<string, int> result = "Error message";
        var validatorCalled = false;

        // Act
        result.ShouldBeError(error =>
        {
            validatorCalled = true;
            error.ShouldContain("Error");
        });

        // Assert
        validatorCalled.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeLeft_IsAliasForShouldBeError()
    {
        // Arrange
        Either<string, int> result = "Error";

        // Act
        var error = result.ShouldBeLeft();

        // Assert
        error.ShouldBe("Error");
    }

    #endregion

    #region EncinaError Assertions Tests

    [Fact]
    public void ShouldBeErrorWithCode_WhenCodeMatches_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("order.notfound", "Order not found");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeErrorWithCode("order.notfound");

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeErrorWithCode_WhenCodeDoesNotMatch_Throws()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("order.notfound", "Order not found");
        Either<EncinaError, int> result = encinaError;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            result.ShouldBeErrorWithCode("different.code"));
    }

    [Fact]
    public void ShouldBeErrorContaining_WhenMessageContains_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("code", "Order with ID 123 not found");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeErrorContaining("123");

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeErrorContaining_WhenMessageDoesNotContain_Throws()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("code", "Order not found");
        Either<EncinaError, int> result = encinaError;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            result.ShouldBeErrorContaining("456"));
    }

    [Fact]
    public void ShouldBeValidationError_WhenValidationError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Validation("encina.validation.failed", "Validation failed");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeValidationError();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeValidationError_WhenNotValidationError_Throws()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("encina.notfound", "Not found");
        Either<EncinaError, int> result = encinaError;

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            result.ShouldBeValidationError());
    }

    [Fact]
    public void ShouldBeAuthorizationError_WhenAuthorizationError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Authorization("encina.authorization.denied", "Access denied");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeAuthorizationError();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeNotFoundError_WhenNotFoundError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("encina.notfound.order", "Order not found");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeNotFoundError();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeConflictError_WhenConflictError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Conflict("encina.conflict.duplicate", "Duplicate entry");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeConflictError();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public void ShouldBeInternalError_WhenInternalError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Internal("encina.internal.unknown", "Internal error");
        Either<EncinaError, int> result = encinaError;

        // Act
        var error = result.ShouldBeInternalError();

        // Assert
        error.ShouldBe(encinaError);
    }

    #endregion

    #region Async Assertions Tests

    [Fact]
    public async Task ShouldBeSuccessAsync_WhenRight_ReturnsValue()
    {
        // Arrange
        Task<Either<string, int>> task = Task.FromResult<Either<string, int>>(42);

        // Act
        var value = await task.ShouldBeSuccessAsync();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public async Task ShouldBeSuccessAsync_WhenLeft_Throws()
    {
        // Arrange
        Either<string, int> errorResult = "Error";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
        {
            await Task.FromResult(errorResult).ShouldBeSuccessAsync();
        });

        exception.Message.ShouldContain("Expected success (Right)");
    }

    [Fact]
    public async Task ShouldBeErrorAsync_WhenLeft_ReturnsError()
    {
        // Arrange
        Task<Either<string, int>> task = Task.FromResult<Either<string, int>>("Error");

        // Act
        var error = await task.ShouldBeErrorAsync();

        // Assert
        error.ShouldBe("Error");
    }

    [Fact]
    public async Task ShouldBeErrorWithCodeAsync_WhenCodeMatches_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("order.notfound", "Order not found");
        Task<Either<EncinaError, int>> task = Task.FromResult<Either<EncinaError, int>>(encinaError);

        // Act
        var error = await task.ShouldBeErrorWithCodeAsync("order.notfound");

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenValidationError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Validation("encina.validation.failed", "Validation failed");
        Task<Either<EncinaError, int>> task = Task.FromResult<Either<EncinaError, int>>(encinaError);

        // Act
        var error = await task.ShouldBeValidationErrorAsync();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public async Task ShouldBeAuthorizationErrorAsync_WhenAuthorizationError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.Authorization("encina.authorization.denied", "Access denied");
        Task<Either<EncinaError, int>> task = Task.FromResult<Either<EncinaError, int>>(encinaError);

        // Act
        var error = await task.ShouldBeAuthorizationErrorAsync();

        // Assert
        error.ShouldBe(encinaError);
    }

    [Fact]
    public async Task ShouldBeNotFoundErrorAsync_WhenNotFoundError_ReturnsError()
    {
        // Arrange
        var encinaError = TestEncinaErrors.NotFound("encina.notfound.order", "Order not found");
        Task<Either<EncinaError, int>> task = Task.FromResult<Either<EncinaError, int>>(encinaError);

        // Act
        var error = await task.ShouldBeNotFoundErrorAsync();

        // Assert
        error.ShouldBe(encinaError);
    }

    #endregion

    #region Custom Message Tests

    [Fact]
    public void ShouldBeSuccess_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        Either<string, int> result = "Error";
        const string customMessage = "Custom failure message";

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            result.ShouldBeSuccess(customMessage));

        exception.Message.ShouldContain(customMessage);
    }

    [Fact]
    public void ShouldBeError_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        Either<string, int> result = 42;
        const string customMessage = "Custom failure message";

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            result.ShouldBeError(customMessage));

        exception.Message.ShouldContain(customMessage);
    }

    #endregion
}
