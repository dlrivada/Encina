using Encina.AzureFunctions.Durable;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;

namespace Encina.AzureFunctions.PropertyTests.Durable;

/// <summary>
/// Property-based tests for ActivityResult to ensure round-trip conversion invariants.
/// </summary>
public sealed class ActivityResultProperties
{
    [Property(MaxTest = 100)]
    public bool SuccessRoundTrip_PreservesValue(NonNull<string> value)
    {
        // Arrange
        Either<EncinaError, string> original = value.Get;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        return roundTripped.IsRight &&
               roundTripped.Match(Right: v => v == value.Get, Left: _ => false);
    }

    [Property(MaxTest = 100)]
    public bool FailureRoundTrip_PreservesErrorCode(NonEmptyString errorCode, NonEmptyString errorMessage)
    {
        // Skip control characters which may be handled differently by exception creation
        if (errorCode.Get.Any(char.IsControl) || errorMessage.Get.Any(char.IsControl))
        {
            return true;
        }

        // Arrange
        var error = EncinaErrors.Create(errorCode.Get, errorMessage.Get);
        Either<EncinaError, string> original = error;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        return roundTripped.IsLeft &&
               roundTripped.Match(
                   Right: _ => false,
                   Left: e => e.GetCode().IfNone(string.Empty) == errorCode.Get);
    }

    [Property(MaxTest = 100)]
    public bool FailureRoundTrip_PreservesErrorMessage(NonEmptyString errorCode, NonEmptyString errorMessage)
    {
        // Skip control characters which may be handled differently by exception creation
        if (errorCode.Get.Any(char.IsControl) || errorMessage.Get.Any(char.IsControl))
        {
            return true;
        }

        // Arrange
        var error = EncinaErrors.Create(errorCode.Get, errorMessage.Get);
        Either<EncinaError, string> original = error;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        return roundTripped.IsLeft &&
               roundTripped.Match(
                   Right: _ => false,
                   Left: e => e.Message == errorMessage.Get);
    }

    [Property(MaxTest = 100)]
    public bool Success_IsSuccessIsAlwaysTrue(NonNull<string> value)
    {
        // Act
        var result = ActivityResult<string>.Success(value.Get);

        // Assert
        return result.IsSuccess;
    }

    [Property(MaxTest = 100)]
    public bool Failure_IsSuccessIsAlwaysFalse(NonEmptyString code, NonEmptyString message)
    {
        // Act
        var result = ActivityResult<string>.Failure(code.Get, message.Get);

        // Assert
        return !result.IsSuccess;
    }

    [Property(MaxTest = 100)]
    public bool Success_ErrorFieldsAreAlwaysNull(NonNull<string> value)
    {
        // Act
        var result = ActivityResult<string>.Success(value.Get);

        // Assert
        return result.ErrorCode == null && result.ErrorMessage == null;
    }

    [Property(MaxTest = 100)]
    public bool Failure_ValueIsAlwaysDefault(NonEmptyString code, NonEmptyString message)
    {
        // Act
        var result = ActivityResult<string>.Failure(code.Get, message.Get);

        // Assert
        return result.Value == default;
    }

    [Property(MaxTest = 100)]
    public bool ToEither_SuccessAlwaysReturnsRight(PositiveInt value)
    {
        // Arrange
        var result = ActivityResult<int>.Success(value.Get);

        // Act
        var either = result.ToEither();

        // Assert
        return either.IsRight;
    }

    [Property(MaxTest = 100)]
    public bool ToEither_FailureAlwaysReturnsLeft(NonEmptyString code, NonEmptyString message)
    {
        // Arrange
        var result = ActivityResult<int>.Failure(code.Get, message.Get);

        // Act
        var either = result.ToEither();

        // Assert
        return either.IsLeft;
    }

    [Property(MaxTest = 50)]
    public bool NumericRoundTrip_PreservesValue(int value)
    {
        // Arrange
        Either<EncinaError, int> original = value;

        // Act
        var activityResult = original.ToActivityResult();
        var roundTripped = activityResult.ToEither();

        // Assert
        return roundTripped.IsRight &&
               roundTripped.Match(Right: v => v == value, Left: _ => false);
    }
}
