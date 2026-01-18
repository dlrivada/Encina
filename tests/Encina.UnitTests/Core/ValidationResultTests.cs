using Encina.Validation;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.UnitTests.Core;

public sealed class ValidationResultTests
{
    [Fact]
    public void Success_ReturnsValidResult()
    {
        var result = ValidationResult.Success;

        result.IsValid.ShouldBeTrue();
        result.IsInvalid.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_WithSingleError_ReturnsInvalidResult()
    {
        var result = ValidationResult.Failure("Name", "Name is required");

        result.IsValid.ShouldBeFalse();
        result.IsInvalid.ShouldBeTrue();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Name");
        result.Errors[0].ErrorMessage.ShouldBe("Name is required");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ReturnsAllErrors()
    {
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Invalid email format")
        };

        var result = ValidationResult.Failure(errors);

        result.IsInvalid.ShouldBeTrue();
        result.Errors.Length.ShouldBe(2);
    }

    [Fact]
    public void Failure_WithEmptyErrorCollection_ReturnsSuccess()
    {
        var result = ValidationResult.Failure(Array.Empty<ValidationError>());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Failure_WithNullErrors_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ValidationResult.Failure(null!));
    }

    [Fact]
    public void ToErrorMessage_WhenValid_ReturnsEmptyString()
    {
        var result = ValidationResult.Success;

        result.ToErrorMessage("TestRequest").ShouldBeEmpty();
    }

    [Fact]
    public void ToErrorMessage_WhenInvalid_ContainsRequestTypeAndErrors()
    {
        var result = ValidationResult.Failure("Name", "Name is required");

        var message = result.ToErrorMessage("CreateUserCommand");

        message.ShouldContain("Validation failed");
        message.ShouldContain("CreateUserCommand");
        message.ShouldContain("1 error(s)");
        message.ShouldContain("Name: Name is required");
    }

    [Fact]
    public void ToErrorMessage_WithNullPropertyName_FormatsCorrectly()
    {
        var errors = new[] { new ValidationError(null, "Object-level error") };
        var result = ValidationResult.Failure(errors);

        var message = result.ToErrorMessage("TestRequest");

        message.ShouldContain("Object-level error");
        message.ShouldNotContain("null:");
    }

    [Fact]
    public void ValidationFailedPrefix_HasCorrectValue()
    {
        ValidationResult.ValidationFailedPrefix.ShouldBe("Validation failed");
    }
}

public sealed class ValidationErrorTests
{
    [Fact]
    public void ValidationError_StoresPropertyNameAndMessage()
    {
        var error = new ValidationError("Email", "Invalid format");

        error.PropertyName.ShouldBe("Email");
        error.ErrorMessage.ShouldBe("Invalid format");
    }

    [Fact]
    public void ValidationError_AllowsNullPropertyName()
    {
        var error = new ValidationError(null, "Object-level validation failed");

        error.PropertyName.ShouldBeNull();
        error.ErrorMessage.ShouldBe("Object-level validation failed");
    }

    [Fact]
    public void ValidationError_Equality_WorksCorrectly()
    {
        var error1 = new ValidationError("Name", "Required");
        var error2 = new ValidationError("Name", "Required");
        var error3 = new ValidationError("Email", "Required");

        error1.ShouldBe(error2);
        error1.ShouldNotBe(error3);
    }
}
