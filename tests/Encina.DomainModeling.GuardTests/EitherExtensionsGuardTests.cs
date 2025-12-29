using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for the Either extension methods.
/// </summary>
public class EitherExtensionsGuardTests
{
    private sealed record TestError(string Message);

    #region When Guards

    [Fact]
    public void When_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.When(true, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    #endregion

    #region Ensure Guards

    [Fact]
    public void Ensure_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.Ensure(null!, _ => new TestError("Error"));

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void Ensure_NullErrorFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.Ensure(x => x > 0, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorFactory");
    }

    #endregion

    #region OrElse Guards

    [Fact]
    public void OrElse_NullFallback_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.OrElse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("fallback");
    }

    #endregion

    #region GetOrElse Guards

    [Fact]
    public void GetOrElse_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.GetOrElse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("defaultFactory");
    }

    #endregion

    #region Tap Guards

    [Fact]
    public void Tap_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.Tap(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("action");
    }

    #endregion

    #region TapError Guards

    [Fact]
    public void TapError_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.TapError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("action");
    }

    #endregion

    #region ToEither Guards

    [Fact]
    public void ToEither_NullErrorFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var option = Some(5);

        // Act
        var act = () => option.ToEither<TestError, int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorFactory");
    }

    #endregion

    #region GetOrThrow Guards

    [Fact]
    public void GetOrThrow_NullExceptionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.GetOrThrow(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("exceptionFactory");
    }

    #endregion

    #region Async Guards

    [Fact]
    public async Task BindAsync_TaskEither_NullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var act = async () => await task.BindAsync<TestError, int, int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("binder");
    }

    [Fact]
    public async Task BindAsync_TaskEither_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<Either<TestError, int>> task = null!;

        // Act
        var act = async () => await task.BindAsync(
            x => Task.FromResult(Right<TestError, int>(x * 2)));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("task");
    }

    [Fact]
    public async Task MapAsync_TaskEither_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var act = async () => await task.MapAsync<TestError, int, string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }

    [Fact]
    public async Task TapAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var act = async () => await task.TapAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public async Task BindAsync_Either_NullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var either = Right<TestError, int>(5);

        // Act
        var act = async () => await either.BindAsync<TestError, int, int>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("binder");
    }

    [Fact]
    public async Task MapAsync_Either_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var either = Right<TestError, int>(5);

        // Act
        var act = async () => await either.MapAsync<TestError, int, string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }

    #endregion
}
