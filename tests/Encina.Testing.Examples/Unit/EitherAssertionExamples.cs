namespace Encina.Testing.Examples.Unit;

/// <summary>
/// Examples demonstrating Either assertion patterns with Encina.Testing.Shouldly.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 10.1-10.2
/// </summary>
public sealed class EitherAssertionExamples
{
    /// <summary>
    /// Pattern: Basic success assertion that extracts the value.
    /// </summary>
    [Fact]
    public void ShouldBeSuccess_ExtractsValue()
    {
        // Arrange
        Either<EncinaError, string> result = "Hello, World!";

        // Act & Assert - ShouldBeSuccess returns the value
        var value = result.ShouldBeSuccess();
        value.ShouldBe("Hello, World!");
    }

    /// <summary>
    /// Pattern: Success assertion with expected value comparison.
    /// </summary>
    [Fact]
    public void ShouldBeSuccess_WithExpectedValue()
    {
        // Arrange
        Either<EncinaError, int> result = 42;

        // Assert - Direct value comparison
        result.ShouldBeSuccess(42);
    }

    /// <summary>
    /// Pattern: Success assertion with custom validator.
    /// </summary>
    [Fact]
    public void ShouldBeSuccess_WithValidator()
    {
        // Arrange
        Either<EncinaError, Guid> result = Guid.NewGuid();

        // Assert - Custom validation on the success value
        result.ShouldBeSuccess(id => id.ShouldNotBe(Guid.Empty));
    }

    /// <summary>
    /// Pattern: Error assertion that extracts the error.
    /// </summary>
    [Fact]
    public void ShouldBeError_ExtractsError()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("encina.validation.field", "Invalid value");

        // Act & Assert - ShouldBeError returns the error
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Invalid value");
    }

    /// <summary>
    /// Pattern: Domain-specific validation error assertion.
    /// </summary>
    [Fact]
    public void ShouldBeValidationError_ChecksErrorCode()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("encina.validation.email", "Invalid format");

        // Assert - Checks error code starts with "encina.validation"
        result.ShouldBeValidationError();
    }

    /// <summary>
    /// Pattern: Domain-specific not found error assertion.
    /// </summary>
    [Fact]
    public void ShouldBeNotFoundError_ChecksErrorCode()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("encina.notfound.user", "User 123 not found");

        // Assert - Checks error code starts with "encina.notfound"
        result.ShouldBeNotFoundError();
    }

    /// <summary>
    /// Pattern: Error assertion with specific code check.
    /// </summary>
    [Fact]
    public void ShouldBeErrorWithCode_ChecksSpecificCode()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("encina.notfound.order", "Order ORD-001 not found");

        // Assert - Check specific error code
        result.ShouldBeErrorWithCode("encina.notfound.order");
    }

    /// <summary>
    /// Pattern: Chained assertions on error for detailed validation.
    /// </summary>
    [Fact]
    public void ChainedErrorAssertions()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("encina.validation.customerid", "Customer ID is required");

        // Assert - Chain multiple assertions
        result
            .ShouldBeValidationError()
            .Message.ShouldContain("Customer ID");
    }

    /// <summary>
    /// Pattern: Async assertions for Task-returning operations.
    /// </summary>
    [Fact]
    public async Task AsyncAssertions()
    {
        // Arrange - Async operation returning Either
        var task = Task.FromResult<Either<EncinaError, int>>(100);

        // Assert - Use async extension
        var value = await task.ShouldBeSuccessAsync();
        value.ShouldBe(100);
    }

    /// <summary>
    /// Pattern: Error message contains check.
    /// </summary>
    [Fact]
    public void ShouldBeErrorContaining_ChecksMessage()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create(
            "custom.error",
            "The operation failed because the connection timed out");

        // Assert - Check message contains text
        result.ShouldBeErrorContaining("timed out");
    }
}
