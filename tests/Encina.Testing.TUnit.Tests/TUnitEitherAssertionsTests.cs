using Encina.Testing.TUnit;
using LanguageExt;

namespace Encina.Testing.TUnit.Tests;

/// <summary>
/// Unit tests for <see cref="TUnitEitherAssertions"/>.
/// </summary>
public class TUnitEitherAssertionsTests
{
    #region Success Assertions

    [Test]
    public async Task ShouldBeSuccessAsync_WhenRight_ShouldReturnValue()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act
        var value = await result.ShouldBeSuccessAsync();

        // Assert
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task ShouldBeSuccessAsync_WhenLeft_ShouldThrow()
    {
        // Arrange
        Either<string, int> result = "error";

        // Act & Assert
        await Assert.That(async () => await result.ShouldBeSuccessAsync())
            .ThrowsException();
    }

    [Test]
    public async Task ShouldBeSuccessAsync_WithExpectedValue_WhenMatches_ShouldPass()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert - should not throw
        await result.ShouldBeSuccessAsync(42);
    }

    [Test]
    public async Task ShouldBeSuccessAsync_WithExpectedValue_WhenDifferent_ShouldThrow()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert
        await Assert.That(async () => await result.ShouldBeSuccessAsync(99))
            .ThrowsException();
    }

    [Test]
    public async Task ShouldBeSuccessAsync_WithValidator_ShouldCallValidator()
    {
        // Arrange
        Either<string, int> result = 42;
        var validatorCalled = false;

        // Act
        await result.ShouldBeSuccessAsync(async value =>
        {
            validatorCalled = true;
            await Assert.That(value).IsEqualTo(42);
        });

        // Assert
        await Assert.That(validatorCalled).IsTrue();
    }

    [Test]
    public async Task AndReturnAsync_WhenRight_ShouldReturnValue()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act
        var value = await result.AndReturnAsync();

        // Assert
        await Assert.That(value).IsEqualTo(42);
    }

    #endregion

    #region Error Assertions

    [Test]
    public async Task ShouldBeErrorAsync_WhenLeft_ShouldReturnError()
    {
        // Arrange
        Either<string, int> result = "error message";

        // Act
        var error = await result.ShouldBeErrorAsync();

        // Assert
        await Assert.That(error).IsEqualTo("error message");
    }

    [Test]
    public async Task ShouldBeErrorAsync_WhenRight_ShouldThrow()
    {
        // Arrange
        Either<string, int> result = 42;

        // Act & Assert
        await Assert.That(async () => await result.ShouldBeErrorAsync())
            .ThrowsException();
    }

    [Test]
    public async Task ShouldBeErrorAsync_WithValidator_ShouldCallValidator()
    {
        // Arrange
        Either<string, int> result = "error";
        var validatorCalled = false;

        // Act
        await result.ShouldBeErrorAsync(async error =>
        {
            validatorCalled = true;
            await Assert.That(error).IsEqualTo("error");
        });

        // Assert
        await Assert.That(validatorCalled).IsTrue();
    }

    #endregion

    #region EncinaError Specific Assertions

    [Test]
    public async Task ShouldBeErrorWithCodeAsync_WhenCodeMatches_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation", "Value is required");

        // Act
        var error = await result.ShouldBeErrorWithCodeAsync("encina.validation");

        // Assert
        await Assert.That(error.Message).IsEqualTo("Value is required");
    }

    [Test]
    public async Task ShouldBeErrorWithCodeAsync_WhenCodeDiffers_ShouldThrow()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation", "Value is required");

        // Act & Assert
        await Assert.That(async () => await result.ShouldBeErrorWithCodeAsync("encina.notfound"))
            .ThrowsException();
    }

    [Test]
    public async Task ShouldBeErrorContainingAsync_WhenMessageContains_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation", "Value is required");

        // Act
        var error = await result.ShouldBeErrorContainingAsync("required");

        // Assert
        await Assert.That(error.Message).Contains("required");
    }

    [Test]
    public async Task ShouldBeValidationErrorAsync_WhenValidationError_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation", "Invalid value");

        // Act
        var error = await result.ShouldBeValidationErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Invalid value");
    }

    [Test]
    public async Task ShouldBeValidationErrorAsync_WhenNotValidationError_ShouldThrow()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.notfound", "Item not found");

        // Act & Assert
        await Assert.That(async () => await result.ShouldBeValidationErrorAsync())
            .ThrowsException();
    }

    [Test]
    public async Task ShouldBeNotFoundErrorAsync_WhenNotFoundError_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.notfound", "Item not found");

        // Act
        var error = await result.ShouldBeNotFoundErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Item not found");
    }

    [Test]
    public async Task ShouldBeAuthorizationErrorAsync_WhenAuthorizationError_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.authorization", "Access denied");

        // Act
        var error = await result.ShouldBeAuthorizationErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Access denied");
    }

    [Test]
    public async Task ShouldBeConflictErrorAsync_WhenConflictError_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.conflict", "Resource conflict");

        // Act
        var error = await result.ShouldBeConflictErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Resource conflict");
    }

    [Test]
    public async Task ShouldBeInternalErrorAsync_WhenInternalError_ShouldPass()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.internal", "Internal server error");

        // Act
        var error = await result.ShouldBeInternalErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Internal server error");
    }

    #endregion

    #region Task Extensions

    [Test]
    public async Task Task_ShouldBeSuccessAsync_WhenRight_ShouldReturnValue()
    {
        // Arrange
        Task<Either<string, int>> task = Task.FromResult<Either<string, int>>(42);

        // Act
        var value = await task.ShouldBeSuccessAsync();

        // Assert
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task Task_ShouldBeErrorAsync_WhenLeft_ShouldReturnError()
    {
        // Arrange
        Task<Either<string, int>> task = Task.FromResult<Either<string, int>>("error");

        // Act
        var error = await task.ShouldBeErrorAsync();

        // Assert
        await Assert.That(error).IsEqualTo("error");
    }

    [Test]
    public async Task Task_AndReturnAsync_WhenRight_ShouldReturnValue()
    {
        // Arrange
        Task<Either<string, int>> task = Task.FromResult<Either<string, int>>(42);

        // Act
        var value = await task.AndReturnAsync();

        // Assert
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task Task_ShouldBeValidationErrorAsync_WhenValidationError_ShouldPass()
    {
        // Arrange
        Task<Either<EncinaError, int>> task =
            Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("encina.validation", "Invalid"));

        // Act
        var error = await task.ShouldBeValidationErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Invalid");
    }

    [Test]
    public async Task Task_ShouldBeAuthorizationErrorAsync_WhenAuthorizationError_ShouldPass()
    {
        // Arrange
        Task<Either<EncinaError, int>> task =
            Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("encina.authorization", "Unauthorized"));

        // Act
        var error = await task.ShouldBeAuthorizationErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Unauthorized");
    }

    [Test]
    public async Task Task_ShouldBeNotFoundErrorAsync_WhenNotFoundError_ShouldPass()
    {
        // Arrange
        Task<Either<EncinaError, int>> task =
            Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("encina.notfound", "Resource not found"));

        // Act
        var error = await task.ShouldBeNotFoundErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Resource not found");
    }

    [Test]
    public async Task Task_ShouldBeConflictErrorAsync_WhenConflictError_ShouldPass()
    {
        // Arrange
        Task<Either<EncinaError, int>> task =
            Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("encina.conflict", "Resource conflict"));

        // Act
        var error = await task.ShouldBeConflictErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Resource conflict");
    }

    [Test]
    public async Task Task_ShouldBeInternalErrorAsync_WhenInternalError_ShouldPass()
    {
        // Arrange
        Task<Either<EncinaError, int>> task =
            Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Create("encina.internal", "Internal server error"));

        // Act
        var error = await task.ShouldBeInternalErrorAsync();

        // Assert
        await Assert.That(error.Message).IsEqualTo("Internal server error");
    }

    #endregion
}
