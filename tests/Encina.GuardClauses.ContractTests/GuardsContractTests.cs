using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.GuardClauses.ContractTests;

/// <summary>
/// Contract tests for Guards static methods.
/// Verifies that Guards adhere to expected contracts and guarantees.
/// </summary>
public sealed class GuardsContractTests
{
    [Fact]
    public void Contract_TryValidateNotNull_WithNonNullValue_MustReturnTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert - Contract: Non-null values MUST return true
        result.ShouldBeTrue();
        error.ShouldBe(default(MediatorError));
    }

    [Fact]
    public void Contract_TryValidateNotNull_WithNullValue_MustReturnFalseWithError()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert - Contract: Null values MUST return false with error
        result.ShouldBeFalse();
        error.ShouldNotBe(default(MediatorError));
        error.Message.ShouldContain("cannot be null");
    }

    [Fact]
    public void Contract_TryValidateNotEmpty_String_WithValidString_MustReturnTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert - Contract: Valid strings MUST return true
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contract_TryValidateNotEmpty_String_WithEmptyString_MustReturnFalseWithError()
    {
        // Arrange
        var value = "";

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert - Contract: Empty strings MUST return false with error
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void Contract_TryValidatePositive_WithPositiveValue_MustReturnTrue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert - Contract: Positive values MUST return true
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contract_TryValidatePositive_WithZeroOrNegative_MustReturnFalseWithError()
    {
        // Arrange
        var zero = 0;
        var negative = -10;

        // Act
        var resultZero = Guards.TryValidatePositive(zero, nameof(zero), out var errorZero);
        var resultNeg = Guards.TryValidatePositive(negative, nameof(negative), out var errorNeg);

        // Assert - Contract: Zero and negative MUST return false with error
        resultZero.ShouldBeFalse();
        errorZero.Message.ShouldContain("must be positive");
        resultNeg.ShouldBeFalse();
        errorNeg.Message.ShouldContain("must be positive");
    }

    [Fact]
    public void Contract_TryValidateInRange_WithValueInRange_MustReturnTrue()
    {
        // Arrange
        var value = 50;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert - Contract: Values in range MUST return true
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contract_TryValidateInRange_WithValueOutOfRange_MustReturnFalseWithError()
    {
        // Arrange
        var value = 150;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert - Contract: Values out of range MUST return false with error
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be between");
    }

    [Fact]
    public void Contract_TryValidateEmail_WithValidEmail_MustReturnTrue()
    {
        // Arrange
        var value = "user@example.com";

        // Act
        var result = Guards.TryValidateEmail(value, nameof(value), out var error);

        // Assert - Contract: Valid emails MUST return true
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contract_TryValidateEmail_WithInvalidEmail_MustReturnFalseWithError()
    {
        // Arrange
        var value = "invalid-email";

        // Act
        var result = Guards.TryValidateEmail(value, nameof(value), out var error);

        // Assert - Contract: Invalid emails MUST return false with error
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be a valid email");
    }

    [Fact]
    public void Contract_TryValidateUrl_WithValidUrl_MustReturnTrue()
    {
        // Arrange
        var value = "https://example.com";

        // Act
        var result = Guards.TryValidateUrl(value, nameof(value), out var error);

        // Assert - Contract: Valid URLs MUST return true
        result.ShouldBeTrue();
    }

    [Fact]
    public void Contract_TryValidateUrl_WithInvalidUrl_MustReturnFalseWithError()
    {
        // Arrange
        var value = "not-a-url";

        // Act
        var result = Guards.TryValidateUrl(value, nameof(value), out var error);

        // Assert - Contract: Invalid URLs MUST return false with error
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be a valid HTTP or HTTPS URL");
    }

    [Fact]
    public void Contract_CustomMessage_MustUseCustomMessageWhenProvided()
    {
        // Arrange
        string? value = null;
        var customMessage = "Custom validation error";

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error, customMessage);

        // Assert - Contract: Custom messages MUST be used when provided
        result.ShouldBeFalse();
        error.Message.ShouldBe(customMessage);
    }

    [Fact]
    public void Contract_ErrorMessage_MustNotBeNullOrEmpty()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert - Contract: Error message MUST not be null or empty
        result.ShouldBeFalse();
        error.Message.ShouldNotBeNullOrEmpty();
        error.Message.ShouldContain("cannot be null");
    }
}
