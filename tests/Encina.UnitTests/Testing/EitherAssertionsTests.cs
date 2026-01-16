using Encina.Testing;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing;

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
        Action act = () => { _ = result.ShouldBeSuccess(); };
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
        Action act = () => { result.ShouldBeSuccess(99); };
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
        Action act = () => { _ = result.ShouldBeError(); };
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
        Action act = () => { _ = result.ShouldBeErrorWithCode("wrong.code"); };
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
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ShouldBeErrorContaining_NoMatch_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "This is a test error");

        // Act & Assert
        Action act = () => { _ = result.ShouldBeErrorContaining("does not exist"); };
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
        Action act = () => { _ = result.ShouldBeValidationError(); };
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

    #region AndConstraint Tests

    [Fact]
    public void ShouldBeSuccessAnd_WhenRight_ReturnsAndConstraint()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act
        var constraint = result.ShouldBeSuccessAnd();

        // Assert
        constraint.Value.ShouldBe(42);
        constraint.And.Value.ShouldBe(42);
    }

    [Fact]
    public void ShouldBeSuccessAnd_ShouldSatisfy_ExecutesAssertion()
    {
        // Arrange
        Either<string, int> result = Right(42);
        var executed = false;

        // Act
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(v =>
            {
                v.ShouldBe(42);
                executed = true;
            });

        // Assert
        executed.ShouldBeTrue();
    }

    [Fact]
    public void ShouldBeSuccessAnd_ChainingMultipleAssertions_Works()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act & Assert (should not throw)
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(v => v.ShouldBeGreaterThan(0))
            .And.ShouldSatisfy(v => v.ShouldBeLessThan(100));
    }

    [Fact]
    public void ShouldBeErrorAnd_WhenLeft_ReturnsAndConstraint()
    {
        // Arrange
        Either<string, int> result = Left("error message");

        // Act
        var constraint = result.ShouldBeErrorAnd();

        // Assert
        constraint.Value.ShouldBe("error message");
    }

    [Fact]
    public void ShouldBeErrorAnd_ShouldSatisfy_ExecutesAssertion()
    {
        // Arrange
        Either<string, int> result = Left("validation failed");

        // Act & Assert
        result.ShouldBeErrorAnd()
            .ShouldSatisfy(e => e.ShouldContain("validation"));
    }

    [Fact]
    public void ShouldBeRightAnd_IsAliasForShouldBeSuccessAnd()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act
        var constraint = result.ShouldBeRightAnd();

        // Assert
        constraint.Value.ShouldBe(42);
    }

    [Fact]
    public void ShouldBeLeftAnd_IsAliasForShouldBeErrorAnd()
    {
        // Arrange
        Either<string, int> result = Left("error");

        // Act
        var constraint = result.ShouldBeLeftAnd();

        // Assert
        constraint.Value.ShouldBe("error");
    }

    [Fact]
    public void AndConstraint_ImplicitConversion_ReturnsValue()
    {
        // Arrange
        Either<string, int> result = Right(42);

        // Act
        int value = result.ShouldBeSuccessAnd();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public void ShouldBeValidationErrorAnd_ReturnsAndConstraint()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation.required", "Field is required");

        // Act
        var constraint = result.ShouldBeValidationErrorAnd();

        // Assert
        constraint.Value.Message.ShouldBe("Field is required");
    }

    [Fact]
    public void ShouldBeErrorWithCodeAnd_ReturnsAndConstraint()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("test.code", "Test error");

        // Act
        var constraint = result.ShouldBeErrorWithCodeAnd("test.code");

        // Assert
        constraint.Value.Message.ShouldBe("Test error");
    }

    [Fact]
    public async Task ShouldBeSuccessAndAsync_ReturnsAndConstraint()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<string, int>>(Right(42));

        // Act
        var constraint = await resultTask.ShouldBeSuccessAndAsync();

        // Assert
        constraint.Value.ShouldBe(42);
    }

    [Fact]
    public async Task ShouldBeErrorAndAsync_ReturnsAndConstraint()
    {
        // Arrange
        var resultTask = Task.FromResult<Either<string, int>>(Left("async error"));

        // Act
        var constraint = await resultTask.ShouldBeErrorAndAsync();

        // Assert
        constraint.Value.ShouldBe("async error");
    }

    #endregion

    #region ShouldBeValidationErrorForProperty Tests

    [Fact]
    public void ShouldBeValidationErrorForProperty_WhenPropertyInMessage_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create(
            "encina.validation.required",
            "The field 'Email' is required");

        // Act
        var error = result.ShouldBeValidationErrorForProperty("Email");

        // Assert
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public void ShouldBeValidationErrorForProperty_WhenPropertyNotInMessage_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create(
            "encina.validation.required",
            "Field is required");

        // Act & Assert
        Action act = () => result.ShouldBeValidationErrorForProperty("Email");
        Should.Throw<Xunit.Sdk.TrueException>(act);
    }

    [Fact]
    public void ShouldBeValidationErrorForPropertyAnd_ReturnsAndConstraint()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create(
            "encina.validation.required",
            "The field 'Name' is required");

        // Act
        var constraint = result.ShouldBeValidationErrorForPropertyAnd("Name");

        // Assert
        constraint.Value.Message.ShouldContain("Name");
    }

    #endregion
}
