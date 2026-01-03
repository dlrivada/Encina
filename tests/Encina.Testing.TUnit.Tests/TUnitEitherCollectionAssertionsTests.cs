using Encina.Testing.TUnit;
using LanguageExt;

namespace Encina.Testing.TUnit.Tests;

/// <summary>
/// Unit tests for <see cref="TUnitEitherCollectionAssertions"/>.
/// </summary>
public class TUnitEitherCollectionAssertionsTests
{
    #region ShouldAllBeSuccessAsync

    [Test]
    public async Task ShouldAllBeSuccessAsync_WhenAllRight_ShouldReturnValues()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            2,
            3
        };

        // Act
        var values = await results.ShouldAllBeSuccessAsync();

        // Assert
        await Assert.That(values.Count).IsEqualTo(3);
        await Assert.That(values[0]).IsEqualTo(1);
        await Assert.That(values[1]).IsEqualTo(2);
        await Assert.That(values[2]).IsEqualTo(3);
    }

    [Test]
    public async Task ShouldAllBeSuccessAsync_WhenAnyLeft_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error",
            3
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldAllBeSuccessAsync())
            .ThrowsException();
    }

    [Test]
    public async Task ShouldAllBeSuccessAsync_WhenEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var results = new List<Either<string, int>>();

        // Act
        var values = await results.ShouldAllBeSuccessAsync();

        // Assert
        await Assert.That(values.Count).IsEqualTo(0);
    }

    #endregion

    #region ShouldAllBeErrorAsync

    [Test]
    public async Task ShouldAllBeErrorAsync_WhenAllLeft_ShouldReturnErrors()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            "error2",
            "error3"
        };

        // Act
        var errors = await results.ShouldAllBeErrorAsync();

        // Assert
        await Assert.That(errors.Count).IsEqualTo(3);
        await Assert.That(errors[0]).IsEqualTo("error1");
        await Assert.That(errors[1]).IsEqualTo("error2");
        await Assert.That(errors[2]).IsEqualTo("error3");
    }

    [Test]
    public async Task ShouldAllBeErrorAsync_WhenAnyRight_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            42,
            "error3"
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldAllBeErrorAsync())
            .ThrowsException();
    }

    #endregion

    #region ShouldContainSuccessAsync

    [Test]
    public async Task ShouldContainSuccessAsync_WhenContainsRight_ShouldPass()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error",
            42,
            "error2"
        };

        // Act & Assert - should not throw
        await results.ShouldContainSuccessAsync();
    }

    [Test]
    public async Task ShouldContainSuccessAsync_WhenNoRight_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            "error2",
            "error3"
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldContainSuccessAsync())
            .ThrowsException();
    }

    #endregion

    #region ShouldContainErrorAsync

    [Test]
    public async Task ShouldContainErrorAsync_WhenContainsLeft_ShouldPass()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error",
            3
        };

        // Act & Assert - should not throw
        await results.ShouldContainErrorAsync();
    }

    [Test]
    public async Task ShouldContainErrorAsync_WhenNoLeft_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            2,
            3
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldContainErrorAsync())
            .ThrowsException();
    }

    #endregion

    #region ShouldHaveSuccessCountAsync

    [Test]
    public async Task ShouldHaveSuccessCountAsync_WhenCountMatches_ShouldPass()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error",
            3
        };

        // Act & Assert - should not throw
        await results.ShouldHaveSuccessCountAsync(2);
    }

    [Test]
    public async Task ShouldHaveSuccessCountAsync_WhenCountDiffers_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error",
            3
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldHaveSuccessCountAsync(3))
            .ThrowsException();
    }

    #endregion

    #region ShouldHaveErrorCountAsync

    [Test]
    public async Task ShouldHaveErrorCountAsync_WhenCountMatches_ShouldPass()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            2,
            "error2"
        };

        // Act & Assert - should not throw
        await results.ShouldHaveErrorCountAsync(2);
    }

    [Test]
    public async Task ShouldHaveErrorCountAsync_WhenCountDiffers_ShouldThrow()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            2,
            "error2"
        };

        // Act & Assert
        await Assert.That(async () => await results.ShouldHaveErrorCountAsync(1))
            .ThrowsException();
    }

    #endregion

    #region GetSuccesses / GetErrors

    [Test]
    public async Task GetSuccesses_ShouldReturnOnlyRightValues()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error",
            3,
            "error2",
            5
        };

        // Act
        var successes = results.GetSuccesses();

        // Assert
        await Assert.That(successes.Count).IsEqualTo(3);
        await Assert.That(successes).Contains(1);
        await Assert.That(successes).Contains(3);
        await Assert.That(successes).Contains(5);
    }

    [Test]
    public async Task GetErrors_ShouldReturnOnlyLeftValues()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            "error1",
            3,
            "error2",
            5
        };

        // Act
        var errors = results.GetErrors();

        // Assert
        await Assert.That(errors.Count).IsEqualTo(2);
        await Assert.That(errors).Contains("error1");
        await Assert.That(errors).Contains("error2");
    }

    [Test]
    public async Task GetSuccesses_WhenNoSuccesses_ShouldReturnEmpty()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            "error1",
            "error2"
        };

        // Act
        var successes = results.GetSuccesses();

        // Assert
        await Assert.That(successes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetErrors_WhenNoErrors_ShouldReturnEmpty()
    {
        // Arrange
        var results = new List<Either<string, int>>
        {
            1,
            2
        };

        // Act
        var errors = results.GetErrors();

        // Assert
        await Assert.That(errors.Count).IsEqualTo(0);
    }

    #endregion

    #region Null Argument Tests

    [Test]
    public async Task ShouldAllBeSuccessAsync_NullInput_ShouldThrow()
    {
        // Arrange
        IEnumerable<Either<string, int>>? results = null;

        // Act & Assert
        await Assert.That(async () => await results!.ShouldAllBeSuccessAsync())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetSuccesses_NullInput_ShouldThrow()
    {
        // Arrange
        IEnumerable<Either<string, int>>? results = null;

        // Act & Assert
        await Assert.That(() => results!.GetSuccesses())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetErrors_NullInput_ShouldThrow()
    {
        // Arrange
        IEnumerable<Either<string, int>>? results = null;

        // Act & Assert
        await Assert.That(() => results!.GetErrors())
            .Throws<ArgumentNullException>();
    }

    #endregion
}
