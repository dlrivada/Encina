using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Testing.Tests;

public sealed class EitherCollectionAssertionsTests
{
    #region ShouldAllBeSuccess Tests

    [Fact]
    public void ShouldAllBeSuccess_WhenAllRight_ReturnsValues()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Right(2),
            Right(3)
        };

        // Act
        var values = results.ShouldAllBeSuccess();

        // Assert
        values.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ShouldAllBeSuccess_WhenSomeLeft_Throws()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("error"),
            Right(3)
        };

        // Act & Assert
        Action act = () => results.ShouldAllBeSuccess();
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    [Fact]
    public void ShouldAllBeSuccessAnd_ReturnsAndConstraint()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Right(2)
        };

        // Act
        var constraint = results.ShouldAllBeSuccessAnd();

        // Assert
        constraint.Value.Count.ShouldBe(2);
    }

    #endregion

    #region ShouldAllBeError Tests

    [Fact]
    public void ShouldAllBeError_WhenAllLeft_ReturnsErrors()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Left("error1"),
            Left("error2")
        };

        // Act
        var errors = results.ShouldAllBeError();

        // Assert
        errors.ShouldBe(["error1", "error2"]);
    }

    [Fact]
    public void ShouldAllBeError_WhenSomeRight_Throws()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Left("error"),
            Right(42)
        };

        // Act & Assert
        Action act = () => results.ShouldAllBeError();
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    #endregion

    #region ShouldContainSuccess Tests

    [Fact]
    public void ShouldContainSuccess_WhenAtLeastOneRight_ReturnsFirst()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Left("error"),
            Right(42),
            Right(100)
        };

        // Act
        var value = results.ShouldContainSuccess();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public void ShouldContainSuccess_WhenAllLeft_Throws()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Left("error1"),
            Left("error2")
        };

        // Act & Assert
        Action act = () => results.ShouldContainSuccess();
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    [Fact]
    public void ShouldContainSuccessAnd_ReturnsAndConstraint()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Left("error"),
            Right(42)
        };

        // Act
        var constraint = results.ShouldContainSuccessAnd();

        // Assert
        constraint.Value.ShouldBe(42);
    }

    #endregion

    #region ShouldContainError Tests

    [Fact]
    public void ShouldContainError_WhenAtLeastOneLeft_ReturnsFirst()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("first error"),
            Left("second error")
        };

        // Act
        var error = results.ShouldContainError();

        // Assert
        error.ShouldBe("first error");
    }

    [Fact]
    public void ShouldContainError_WhenAllRight_Throws()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Right(2)
        };

        // Act & Assert
        Action act = () => results.ShouldContainError();
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    #endregion

    #region ShouldHaveSuccessCount Tests

    [Fact]
    public void ShouldHaveSuccessCount_WhenCorrectCount_ReturnsValues()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("error"),
            Right(3)
        };

        // Act
        var values = results.ShouldHaveSuccessCount(2);

        // Assert
        values.ShouldBe([1, 3]);
    }

    [Fact]
    public void ShouldHaveSuccessCount_WhenWrongCount_Throws()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Right(2)
        };

        // Act & Assert
        Action act = () => results.ShouldHaveSuccessCount(3);
        Should.Throw<Xunit.Sdk.TrueException>(act);
    }

    #endregion

    #region ShouldHaveErrorCount Tests

    [Fact]
    public void ShouldHaveErrorCount_WhenCorrectCount_ReturnsErrors()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("error1"),
            Left("error2")
        };

        // Act
        var errors = results.ShouldHaveErrorCount(2);

        // Assert
        errors.ShouldBe(["error1", "error2"]);
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void GetSuccesses_ReturnsOnlySuccesses()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("error"),
            Right(3)
        };

        // Act
        var successes = results.GetSuccesses();

        // Assert
        successes.ShouldBe([1, 3]);
    }

    [Fact]
    public void GetErrors_ReturnsOnlyErrors()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            Right(1),
            Left("error1"),
            Left("error2")
        };

        // Act
        var errors = results.GetErrors();

        // Assert
        errors.ShouldBe(["error1", "error2"]);
    }

    #endregion

    #region EncinaError Specific Tests

    [Fact]
    public void ShouldContainValidationErrorFor_WhenPropertyInErrors_ReturnsError()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Right(1),
            EncinaErrors.Create("encina.validation.required", "The field 'Email' is required"),
            Right(3)
        };

        // Act
        var error = results.ShouldContainValidationErrorFor("Email");

        // Assert
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public void ShouldContainValidationErrorFor_WhenPropertyNotInErrors_Throws()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Right(1),
            EncinaErrors.Create("encina.validation.required", "Field is required")
        };

        // Act & Assert
        Action act = () => results.ShouldContainValidationErrorFor("Email");
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    [Fact]
    public void ShouldNotContainAuthorizationErrors_WhenNoAuthErrors_Succeeds()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Right(1),
            EncinaErrors.Create("encina.validation.required", "Field is required")
        };

        // Act & Assert (should not throw)
        results.ShouldNotContainAuthorizationErrors();
    }

    [Fact]
    public void ShouldNotContainAuthorizationErrors_WhenAuthErrorExists_Throws()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Right(1),
            EncinaErrors.Create("encina.authorization.denied", "Access denied")
        };

        // Act & Assert
        Action act = () => results.ShouldNotContainAuthorizationErrors();
        Should.Throw<Xunit.Sdk.FailException>(act);
    }

    [Fact]
    public void ShouldContainAuthorizationError_WhenAuthErrorExists_ReturnsError()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Right(1),
            EncinaErrors.Create("encina.authorization.denied", "Access denied")
        };

        // Act
        var error = results.ShouldContainAuthorizationError();

        // Assert
        error.Message.ShouldBe("Access denied");
    }

    [Fact]
    public void ShouldAllHaveErrorCode_WhenAllMatchingCode_ReturnsErrors()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            EncinaErrors.Create("test.code", "Error 1"),
            EncinaErrors.Create("test.code", "Error 2")
        };

        // Act
        var errors = results.ShouldAllHaveErrorCode("test.code");

        // Assert
        errors.Count.ShouldBe(2);
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task ShouldAllBeSuccessAsync_WhenAllRight_ReturnsValues()
    {
        // Arrange
        var resultsTask = Task.FromResult<IEnumerable<Either<string, int>>>(new List<Either<string, int>>
        {
            Right(1),
            Right(2)
        });

        // Act
        var values = await resultsTask.ShouldAllBeSuccessAsync();

        // Assert
        values.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task ShouldContainSuccessAsync_ReturnsFirstSuccess()
    {
        // Arrange
        var resultsTask = Task.FromResult<IEnumerable<Either<string, int>>>(new List<Either<string, int>>
        {
            Left("error"),
            Right(42)
        });

        // Act
        var value = await resultsTask.ShouldContainSuccessAsync();

        // Assert
        value.ShouldBe(42);
    }

    #endregion
}
