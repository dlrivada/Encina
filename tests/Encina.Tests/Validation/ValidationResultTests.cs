using Encina.Validation;
using Shouldly;

namespace Encina.Tests.Validation;

/// <summary>
/// Tests for ValidationResult and ValidationError classes.
/// </summary>
public sealed class ValidationResultTests
{
    #region ValidationResult.Success Tests

    [Fact]
    public void Success_IsValid_ReturnsTrue()
    {
        // Act
        var result = ValidationResult.Success;

        // Assert
        result.IsValid.ShouldBeTrue();
        result.IsInvalid.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Success_IsSingleton()
    {
        // Act
        var result1 = ValidationResult.Success;
        var result2 = ValidationResult.Success;

        // Assert
        result1.ShouldBeSameAs(result2);
    }

    #endregion

    #region ValidationResult.Failure Tests

    [Fact]
    public void Failure_WithErrors_IsInvalid()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Age", "Age must be positive")
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.IsInvalid.ShouldBeTrue();
        result.Errors.Length.ShouldBe(2);
    }

    [Fact]
    public void Failure_WithEmptyErrors_ReturnsSuccess()
    {
        // Arrange
        var errors = Array.Empty<ValidationError>();

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ShouldBeSameAs(ValidationResult.Success);
    }

    [Fact]
    public void Failure_NullErrors_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ValidationResult.Failure(null!));
    }

    [Fact]
    public void Failure_WithPropertyAndMessage_CreatesCorrectError()
    {
        // Act
        var result = ValidationResult.Failure("Email", "Invalid email format");

        // Assert
        result.IsInvalid.ShouldBeTrue();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Email");
        result.Errors[0].ErrorMessage.ShouldBe("Invalid email format");
    }

    #endregion

    #region ToErrorMessage Tests

    [Fact]
    public void ToErrorMessage_WhenValid_ReturnsEmptyString()
    {
        // Arrange
        var result = ValidationResult.Success;

        // Act
        var message = result.ToErrorMessage("TestRequest");

        // Assert
        message.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToErrorMessage_WithPropertyErrors_IncludesPropertyNames()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Invalid format")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var message = result.ToErrorMessage("CreateUserCommand");

        // Assert
        message.ShouldContain("CreateUserCommand");
        message.ShouldContain("2 error(s)");
        message.ShouldContain("Name: Name is required");
        message.ShouldContain("Email: Invalid format");
    }

    [Fact]
    public void ToErrorMessage_WithObjectLevelError_OmitsPropertyName()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError(null, "Object-level validation failed")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var message = result.ToErrorMessage("TestCommand");

        // Assert
        message.ShouldContain("Object-level validation failed");
        message.ShouldNotContain("null:");
    }

    [Fact]
    public void ToErrorMessage_MixedErrors_FormatsCorrectly()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Name", "Required"),
            new ValidationError(null, "Cross-field validation failed")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var message = result.ToErrorMessage("MyCommand");

        // Assert
        message.ShouldContain("MyCommand");
        message.ShouldContain("2 error(s)");
        message.ShouldContain("Name: Required");
        message.ShouldContain("Cross-field validation failed");
    }

    #endregion

    #region ValidationError Tests

    [Fact]
    public void ValidationError_RecordEquality()
    {
        // Arrange
        // Act
        var error1 = new ValidationError("Name", "Required");
        var error2 = new ValidationError("Name", "Required");
        var error3 = new ValidationError("Email", "Required");

        // Assert
        error1.ShouldBe(error2);
        error1.ShouldNotBe(error3);
        error1.GetHashCode().ShouldBe(error2.GetHashCode());
    }

    [Fact]
    public void ValidationError_NullPropertyName_IsAllowed()
    {
        // Act
        var error = new ValidationError(null, "Object error");

        // Assert
        error.PropertyName.ShouldBeNull();
        error.ErrorMessage.ShouldBe("Object error");
    }

    #endregion
}
