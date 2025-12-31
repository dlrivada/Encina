using Shouldly;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Testing.Tests;

public sealed class EitherAssertionsTests
{
    #region ShouldBeSuccess Tests

    [Fact]
    public void ShouldBeSuccess_WhenRight_ReturnsValue()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act
        var value = result.ShouldBeSuccess();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public void ShouldBeSuccess_WhenLeft_Throws()
    {
        // Arrange
        Either<string, int> result = Left("error");

        // Act & Assert
        var act = () => result.ShouldBeSuccess();
        Should.Throw<Xunit.Sdk.TrueException>(act);
    }

    [Fact]
    public void ShouldBeSuccess_WithExpectedValue_Succeeds()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act & Assert (should not throw)
        result.ShouldBeSuccess(42);
    }

    [Fact]
    public void ShouldBeSuccess_WithWrongExpectedValue_Throws()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act & Assert
        var act = () => result.ShouldBeSuccess(99);
        Should.Throw<Xunit.Sdk.EqualException>(act);
    }

    [Fact]
    public void ShouldBeSuccess_WithValidator_ExecutesValidator()
    {
        // Arrange
        Either<string, int> result = Right(42);
        var validated = false;

        // Act
        result.ShouldBeSuccess(value =>
        {
            value.ShouldBe(42);
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeRight_IsAliasForShouldBeSuccess()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act
        var value = result.ShouldBeRight();

        // Assert
        value.ShouldBe(42);
    }

    #endregion

    #region ShouldBeError Tests

    [Fact]
    public void ShouldBeError_WhenLeft_ReturnsError()
    {
        // Arrange
        Either<string, int> result = Left("error message");

        // Act
        var error = result.ShouldBeError();

        // Assert
        error.ShouldBe("error message");
    }

    [Fact]
    public void ShouldBeError_WhenRight_Throws()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act & Assert
        var act = () => result.ShouldBeError();
        Should.Throw<Xunit.Sdk.TrueException>(act);
    }

    [Fact]
    public void ShouldBeError_WithValidator_ExecutesValidator()
    {
        // Arrange
        Either<string, int> result = Left("error");
        var validated = false;

        // Act
        result.ShouldBeError(error =>
        {
            error.ShouldBe("error");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeLeft_IsAliasForShouldBeError()
    {
        // Arrange
        Either<string, int> result = Left("error");

        // Act
        var error = result.ShouldBeLeft();

        // Assert
        error.ShouldBe("error");
    }

    #endregion

    #region EncinaError Specific Tests

    [Fact]
    public void ShouldBeErrorWithCode_MatchingCode_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "Test error");

        // Act
        var error = result.ShouldBeErrorWithCode("test.code");

        // Assert
        error.Message.ShouldBe("Test error");
    }

    [Fact]
    public void ShouldBeErrorWithCode_DifferentCode_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "Test error");

        // Act & Assert
        var act = () => result.ShouldBeErrorWithCode("wrong.code");
        Should.Throw<Xunit.Sdk.EqualException>(act);
    }

    [Fact]
    public void ShouldBeErrorContaining_MatchingMessage_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "This is a test error message");

        // Act
        var error = result.ShouldBeErrorContaining("test error");

        // Assert
        error.ShouldNotBeNull();
    }

    [Fact]
    public void ShouldBeErrorContaining_NoMatch_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "This is a test error");

        // Act & Assert
        var act = () => result.ShouldBeErrorContaining("does not exist");
        Should.Throw<Xunit.Sdk.ContainsException>(act);
    }

    [Fact]
    public void ShouldBeValidationError_WhenValidationCode_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation.required", "Field is required");

        // Act
        var error = result.ShouldBeValidationError();

        // Assert
        error.Message.ShouldBe("Field is required");
    }

    [Fact]
    public void ShouldBeValidationError_WhenNotValidation_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("other.code", "Not a validation error");

        // Act & Assert
        var act = () => result.ShouldBeValidationError();
        Should.Throw<Xunit.Sdk.StartsWithException>(act);
    }

    [Fact]
    public void ShouldBeAuthorizationError_WhenAuthorizationCode_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.authorization.denied", "Access denied");

        // Act
        var error = result.ShouldBeAuthorizationError();

        // Assert
        error.Message.ShouldBe("Access denied");
    }

    [Fact]
    public void ShouldBeNotFoundError_WhenNotFoundCode_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.notfound.resource", "Resource not found");

        // Act
        var error = result.ShouldBeNotFoundError();

        // Assert
        error.Message.ShouldBe("Resource not found");
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task ShouldBeSuccessAsync_WhenRight_ReturnsValue()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<string, int>>(Right(42));

        // Act
        var value = await resultTask.ShouldBeSuccessAsync();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public async Task ShouldBeErrorAsync_WhenLeft_ReturnsError()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<string, int>>(Left("error"));

        // Act
        var error = await resultTask.ShouldBeErrorAsync();

        // Assert
        error.ShouldBe("error");
    }

    [Fact]
    public async Task ShouldBeErrorWithCodeAsync_MatchingCode_ReturnsError()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<EncinaError, int>>(
            EncinaErrors.Create("async.code", "Async error"));

        // Act
        var error = await resultTask.ShouldBeErrorWithCodeAsync("async.code");

        // Assert
        error.Message.ShouldBe("Async error");
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenValidation_ReturnsError()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<EncinaError, int>>(
            EncinaErrors.Create("encina.validation.async", "Async validation error"));

        // Act
        var error = await resultTask.ShouldBeValidationErrorAsync();

        // Assert
        error.Message.ShouldBe("Async validation error");
    }

    #endregion
}
