using Encina.AzureFunctions.Durable;
using FluentAssertions;
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
        roundTripped.IsRight.Should().BeTrue();
        var value = roundTripped.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Expected Right"));
        value.Should().Be(originalValue);
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
        roundTripped.IsLeft.Should().BeTrue();
        var resultError = roundTripped.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e);
        resultError.Message.Should().Be(errorMessage);
        resultError.GetCode().IfNone(string.Empty).Should().Be(errorCode);
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
        roundTripped.IsRight.Should().BeTrue();
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
        roundTripped.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void Contract_Success_HasCorrectProperties()
    {
        // Arrange & Act
        var result = ActivityResult<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Contract_Failure_HasCorrectProperties()
    {
        // Arrange
        var error = EncinaErrors.Create("code", "message");

        // Act
        var result = ActivityResult<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
        result.ErrorCode.Should().Be("code");
        result.ErrorMessage.Should().Be("message");
    }

    [Fact]
    public void Contract_FailureWithStrings_HasCorrectProperties()
    {
        // Act
        var result = ActivityResult<int>.Failure("error.code", "Error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("error.code");
        result.ErrorMessage.Should().Be("Error message");
    }

    [Fact]
    public void Contract_ToEither_PreservesSuccessValue()
    {
        // Arrange
        var result = ActivityResult<string>.Success("test-data");

        // Act
        var either = result.ToEither();

        // Assert
        either.IsRight.Should().BeTrue();
        either.IfRight(v => v.Should().Be("test-data"));
    }

    [Fact]
    public void Contract_ToEither_PreservesErrorDetails()
    {
        // Arrange
        var result = ActivityResult<string>.Failure("my.code", "My message");

        // Act
        var either = result.ToEither();

        // Assert
        either.IsLeft.Should().BeTrue();
        either.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).Should().Be("my.code");
            e.Message.Should().Be("My message");
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
        either.IsLeft.Should().BeTrue();
        either.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).Should().Be("durable.activity_failed");
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
        either.IsLeft.Should().BeTrue();
        either.IfLeft(e =>
        {
            e.Message.Should().Be("Activity failed");
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
        roundTripped.IsRight.Should().BeTrue();
        roundTripped.IfRight(v =>
        {
            v.Id.Should().Be(complexValue.Id);
            v.Name.Should().Be(complexValue.Name);
            v.CreatedAt.Should().Be(complexValue.CreatedAt);
        });
    }

    private sealed class TestComplexType
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
