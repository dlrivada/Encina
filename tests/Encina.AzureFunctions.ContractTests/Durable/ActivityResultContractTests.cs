using Encina.AzureFunctions.Durable;
using Shouldly;
using LanguageExt;
using Xunit;

namespace Encina.AzureFunctions.ContractTests.Durable;

/// <summary>
/// Contract tests to verify that ActivityResult properly converts between Either and serializable form.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ActivityResultContractTests
{
    [Fact]
    public void Contract_SuccessResult_RoundTripsCorrectly()
    {
        // Arrange
        var originalValue = "test-value";
        Either<EncinaError, string> original = originalValue;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        roundTripped.IsRight.ShouldBeTrue();
        var value = roundTripped.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Expected Right"));
        value.ShouldBe(originalValue);
    }

    [Fact]
    public void Contract_FailureResult_RoundTripsCorrectly()
    {
        // Arrange
        var errorCode = "test.error";
        var errorMessage = "Test error message";
        var error = EncinaErrors.Create(errorCode, errorMessage);
        Either<EncinaError, string> original = error;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        roundTripped.IsLeft.ShouldBeTrue();
        var resultError = roundTripped.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e);
        resultError.Message.ShouldBe(errorMessage);
        resultError.GetCode().IfNone(string.Empty).ShouldBe(errorCode);
    }

    [Fact]
    public void Contract_UnitResult_SuccessRoundTripsCorrectly()
    {
        // Arrange
        Either<EncinaError, Unit> original = Unit.Default;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        roundTripped.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_UnitResult_FailureRoundTripsCorrectly()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        Either<EncinaError, Unit> original = error;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        roundTripped.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Contract_Success_HasCorrectProperties()
    {
        // Arrange & Act
        var result = ActivityResult<int>.Success(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.ErrorCode.ShouldBeNull();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Contract_Failure_HasCorrectProperties()
    {
        // Arrange
        var error = EncinaErrors.Create("code", "message");

        // Act
        var result = ActivityResult<int>.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe(default);
        result.ErrorCode.ShouldBe("code");
        result.ErrorMessage.ShouldBe("message");
    }

    [Fact]
    public void Contract_FailureWithStrings_HasCorrectProperties()
    {
        // Act
        var result = ActivityResult<int>.Failure("error.code", "Error message");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("error.code");
        result.ErrorMessage.ShouldBe("Error message");
    }

    [Fact]
    public void Contract_ToEither_PreservesSuccessValue()
    {
        // Arrange
        var result = ActivityResult<string>.Success("test-data");

        // Act
        var either = result.ToEither();

        // Assert
        either.IsRight.ShouldBeTrue();
        either.IfRight(v => v.ShouldBe("test-data"));
    }

    [Fact]
    public void Contract_ToEither_PreservesErrorDetails()
    {
        // Arrange
        var result = ActivityResult<string>.Failure("my.code", "My message");

        // Act
        var either = result.ToEither();

        // Assert
        either.IsLeft.ShouldBeTrue();
        either.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).ShouldBe("my.code");
            e.Message.ShouldBe("My message");
        });
    }

    [Fact]
    public void Contract_ToEither_WithNullErrorCode_UsesFallbackCode()
    {
        // Arrange
        var result = new ActivityResult<string>
        {
            IsSuccess = false,
            ErrorCode = null,
            ErrorMessage = "Some error"
        };

        // Act
        var either = result.ToEither();

        // Assert
        either.IsLeft.ShouldBeTrue();
        either.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).ShouldBe("durable.activity_failed");
        });
    }

    [Fact]
    public void Contract_ToEither_WithNullErrorMessage_UsesFallbackMessage()
    {
        // Arrange
        var result = new ActivityResult<string>
        {
            IsSuccess = false,
            ErrorCode = "code",
            ErrorMessage = null
        };

        // Act
        var either = result.ToEither();

        // Assert
        either.IsLeft.ShouldBeTrue();
        either.IfLeft(e =>
        {
            e.Message.ShouldBe("Activity failed");
        });
    }

    [Fact]
    public void Contract_ComplexType_RoundTripsCorrectly()
    {
        // Arrange
        var complexValue = new TestComplexType
        {
            Id = 123,
            Name = "Test",
            CreatedAt = DateTime.UtcNow
        };
        Either<EncinaError, TestComplexType> original = complexValue;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        roundTripped.IsRight.ShouldBeTrue();
        roundTripped.IfRight(v =>
        {
            v.Id.ShouldBe(complexValue.Id);
            v.Name.ShouldBe(complexValue.Name);
            v.CreatedAt.ShouldBe(complexValue.CreatedAt);
        });
    }

    private sealed class TestComplexType
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
