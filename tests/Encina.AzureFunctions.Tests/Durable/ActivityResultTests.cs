using Encina.AzureFunctions.Durable;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class ActivityResultTests
{
    [Fact]
    public void Success_WithValue_CreatesSuccessfulResult()
    {
        // Act
        var result = ActivityResult<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.ErrorCode.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithEncinaError_CreatesFailedResult()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error message");

        // Act
        var result = ActivityResult<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
        result.ErrorCode.Should().Be("test.error");
        result.ErrorMessage.Should().Be("Test error message");
    }

    [Fact]
    public void Failure_WithCodeAndMessage_CreatesFailedResult()
    {
        // Act
        var result = ActivityResult<string>.Failure("custom.error", "Custom error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorCode.Should().Be("custom.error");
        result.ErrorMessage.Should().Be("Custom error");
    }

    [Fact]
    public void ToEither_WhenSuccess_ReturnsRight()
    {
        // Arrange
        var result = ActivityResult<int>.Success(42);

        // Act
        var either = result.ToEither();

        // Assert
        either.IsRight.Should().BeTrue();
        either.IfRight(v => v.Should().Be(42));
    }

    [Fact]
    public void ToEither_WhenFailure_ReturnsLeft()
    {
        // Arrange
        var result = ActivityResult<int>.Failure("test.error", "Test error");

        // Act
        var either = result.ToEither();

        // Assert
        either.IsLeft.Should().BeTrue();
        either.IfLeft(e =>
        {
            e.Message.Should().Be("Test error");
            e.GetCode().IfSome(c => c.Should().Be("test.error"));
        });
    }

    [Fact]
    public void ToActivityResult_WhenRight_CreatesSuccess()
    {
        // Arrange
        var either = Either<EncinaError, int>.Right(42);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
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
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("test.error");
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void ToActivityResult_WithUnit_WhenRight_CreatesSuccess()
    {
        // Arrange
        var either = Either<EncinaError, Unit>.Right(Unit.Default);

        // Act
        var result = either.ToActivityResult();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Default);
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
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("unit.error");
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
        roundtripped.IsRight.Should().BeTrue();
        roundtripped.IfRight(v => v.Should().Be("test value"));
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
        roundtripped.IsLeft.Should().BeTrue();
        roundtripped.IfLeft(e =>
        {
            e.GetCode().IfSome(c => c.Should().Be("roundtrip.error"));
            e.Message.Should().Be("Roundtrip error message");
        });
    }
}
