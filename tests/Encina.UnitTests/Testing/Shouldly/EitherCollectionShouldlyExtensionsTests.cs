using Encina.Testing.Shouldly;
using LanguageExt;

namespace Encina.UnitTests.Testing.Shouldly;

/// <summary>
/// Unit tests for <see cref="EitherCollectionShouldlyExtensions"/>.
/// </summary>
public sealed class EitherCollectionShouldlyExtensionsTests
{
    #region ShouldAllBeSuccess Tests

    [Fact]
    public void ShouldAllBeSuccess_WhenAllRight_ReturnsValues()
    {
        // Arrange
        var results = new Either<string, int>[] { 1, 2, 3 };

        // Act
        var values = results.ShouldAllBeSuccess();

        // Assert
        values.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ShouldAllBeSuccess_WhenSomeLeft_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error at index 1",
            3
        };

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            results.ShouldAllBeSuccess());

        exception.Message.ShouldContain("1 of 3 were errors");
    }

    [Fact]
    public void ShouldAllBeSuccess_WhenAllLeft_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error 1",
            "Error 2"
        };

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            results.ShouldAllBeSuccess());

        exception.Message.ShouldContain("2 of 2 were errors");
    }

    [Fact]
    public void ShouldAllBeSuccess_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var results = Array.Empty<Either<string, int>>();

        // Act
        var values = results.ShouldAllBeSuccess();

        // Assert
        values.ShouldBeEmpty();
    }

    #endregion

    #region ShouldAllBeError Tests

    [Fact]
    public void ShouldAllBeError_WhenAllLeft_ReturnsErrors()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error 1",
            "Error 2",
            "Error 3"
        };

        // Act
        var errors = results.ShouldAllBeError();

        // Assert
        errors.ShouldBe(["Error 1", "Error 2", "Error 3"]);
    }

    [Fact]
    public void ShouldAllBeError_WhenSomeRight_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error",
            42,
            "Another error"
        };

        // Act & Assert
        var exception = Should.Throw<ShouldAssertException>(() =>
            results.ShouldAllBeError());

        exception.Message.ShouldContain("1 of 3 were successes");
    }

    #endregion

    #region ShouldContainSuccess Tests

    [Fact]
    public void ShouldContainSuccess_WhenContainsRight_ReturnsFirstSuccess()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error",
            42,
            100
        };

        // Act
        var firstSuccess = results.ShouldContainSuccess();

        // Assert
        firstSuccess.ShouldBe(42);
    }

    [Fact]
    public void ShouldContainSuccess_WhenAllLeft_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error 1",
            "Error 2"
        };

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            results.ShouldContainSuccess());
    }

    [Fact]
    public void ShouldContainSuccess_WhenEmpty_Throws()
    {
        // Arrange
        var results = Array.Empty<Either<string, int>>();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            results.ShouldContainSuccess());
    }

    #endregion

    #region ShouldContainError Tests

    [Fact]
    public void ShouldContainError_WhenContainsLeft_ReturnsFirstError()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            42,
            "First error",
            "Second error"
        };

        // Act
        var firstError = results.ShouldContainError();

        // Assert
        firstError.ShouldBe("First error");
    }

    [Fact]
    public void ShouldContainError_WhenAllRight_Throws()
    {
        // Arrange
        var results = new Either<string, int>[] { 1, 2, 3 };

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            results.ShouldContainError());
    }

    #endregion

    #region ShouldHaveSuccessCount Tests

    [Fact]
    public void ShouldHaveSuccessCount_WhenCountMatches_ReturnsSuccesses()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error",
            3
        };

        // Act
        var successes = results.ShouldHaveSuccessCount(2);

        // Assert
        successes.ShouldBe([1, 3]);
    }

    [Fact]
    public void ShouldHaveSuccessCount_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error",
            3
        };

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            results.ShouldHaveSuccessCount(3));
    }

    #endregion

    #region ShouldHaveErrorCount Tests

    [Fact]
    public void ShouldHaveErrorCount_WhenCountMatches_ReturnsErrors()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error 1",
            "Error 2"
        };

        // Act
        var errors = results.ShouldHaveErrorCount(2);

        // Assert
        errors.ShouldBe(["Error 1", "Error 2"]);
    }

    [Fact]
    public void ShouldHaveErrorCount_WhenCountDoesNotMatch_Throws()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error"
        };

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            results.ShouldHaveErrorCount(2));
    }

    #endregion

    #region GetSuccesses / GetErrors Tests

    [Fact]
    public void GetSuccesses_ReturnsOnlySuccesses()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error",
            3,
            "Another error",
            5
        };

        // Act
        var successes = results.GetSuccesses();

        // Assert
        successes.ShouldBe([1, 3, 5]);
    }

    [Fact]
    public void GetErrors_ReturnsOnlyErrors()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            1,
            "Error 1",
            3,
            "Error 2",
            5
        };

        // Act
        var errors = results.GetErrors();

        // Assert
        errors.ShouldBe(["Error 1", "Error 2"]);
    }

    [Fact]
    public void GetSuccesses_WhenAllErrors_ReturnsEmpty()
    {
        // Arrange
        var results = new Either<string, int>[]
        {
            "Error 1",
            "Error 2"
        };

        // Act
        var successes = results.GetSuccesses();

        // Assert
        successes.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrors_WhenAllSuccesses_ReturnsEmpty()
    {
        // Arrange
        var results = new Either<string, int>[] { 1, 2, 3 };

        // Act
        var errors = results.GetErrors();

        // Assert
        errors.ShouldBeEmpty();
    }

    #endregion
}
