using Encina.AzureFunctions.Durable;
using Encina.TestInfrastructure.Extensions;

namespace Encina.UnitTests.AzureFunctions.Durable;

public class ActivityResultTests
{
    [Fact]
    public void Success_WithValue_CreatesSuccessfulResult()
    {
        // Act
        var result = ActivityResult<int>.Success(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.ErrorCode.ShouldBeNull();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Failure_WithEncinaError_CreatesFailedResult()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error message");

        // Act
        var result = ActivityResult<int>.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe(default);
        result.ErrorCode.ShouldBe("test.error");
        result.ErrorMessage.ShouldBe("Test error message");
    }

    [Fact]
    public void Failure_WithCodeAndMessage_CreatesFailedResult()
    {
        // Act
        var result = ActivityResult<string>.Failure("custom.error", "Custom error");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.ErrorCode.ShouldBe("custom.error");
        result.ErrorMessage.ShouldBe("Custom error");
    }

    [Fact]
    public void ToEither_WhenSuccess_ReturnsRight()
    {
        // Arrange
        var result = ActivityResult<int>.Success(42);

        // Act
        var either = result.ToEither();

        // Assert
        var value = either.ShouldBeSuccess();
        value.ShouldBe(42);
    }

    [Fact]
    public void ToEither_WhenFailure_ReturnsLeft()
    {
        // Arrange
        var result = ActivityResult<int>.Failure("test.error", "Test error");

        // Act
        var either = result.ToEither();

        // Assert
        var error = either.ShouldBeErrorWithCode("test.error");
        error.Message.ShouldBe("Test error");
    }

    [Fact]
    public void ToActivityResult_WhenRight_CreatesSuccess()
    {
        // Arrange
        var either = Either<EncinaError, int>.Right(42);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ToActivityResult_WhenLeft_CreatesFailure()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var either = Either<EncinaError, int>.Left(error);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("test.error");
        result.ErrorMessage.ShouldBe("Test error");
    }

    [Fact]
    public void ToActivityResult_WithUnit_WhenRight_CreatesSuccess()
    {
        // Arrange
        var either = Either<EncinaError, Unit>.Right(Unit.Default);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Unit.Default);
    }

    [Fact]
    public void ToActivityResult_WithUnit_WhenLeft_CreatesFailure()
    {
        // Arrange
        var error = EncinaErrors.Create("unit.error", "Unit error");
        var either = Either<EncinaError, Unit>.Left(error);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("unit.error");
    }

    [Fact]
    public void Roundtrip_SuccessValue_PreservesData()
    {
        // Arrange
        var original = Either<EncinaError, string>.Right("test value");

        // Act
        var activityResult = original.ToActivityResult();
        var roundtripped = activityResult.ToEither();

        // Assert
        var value = roundtripped.ShouldBeSuccess();
        value.ShouldBe("test value");
    }

    [Fact]
    public void Roundtrip_ErrorValue_PreservesData()
    {
        // Arrange
        var error = EncinaErrors.Create("roundtrip.error", "Roundtrip error message");
        var original = Either<EncinaError, string>.Left(error);

        // Act
        var activityResult = original.ToActivityResult();
        var roundtripped = activityResult.ToEither();

        // Assert
        var err = roundtripped.ShouldBeErrorWithCode("roundtrip.error");
        err.Message.ShouldBe("Roundtrip error message");
    }
}
