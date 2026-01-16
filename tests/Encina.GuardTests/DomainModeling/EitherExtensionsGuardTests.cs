using Encina.DomainModeling;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.DomainModeling;

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
        Action act = () => { _ = result.When(true, null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("operation");
    }

    #endregion

    #region Ensure Guards

    [Fact]
    public void Ensure_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.Ensure(null!, _ => new TestError("Error")); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("predicate");
    }

    [Fact]
    public void Ensure_NullErrorFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.Ensure(x => x > 0, null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorFactory");
    }

    #endregion

    #region OrElse Guards

    [Fact]
    public void OrElse_NullFallback_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.OrElse(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("fallback");
    }

    #endregion

    #region GetOrElse Guards

    [Fact]
    public void GetOrElse_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.GetOrElse(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("defaultFactory");
    }

    #endregion

    #region Tap Guards

    [Fact]
    public void Tap_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.Tap(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("action");
    }

    #endregion

    #region TapError Guards

    [Fact]
    public void TapError_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.TapError(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("action");
    }

    #endregion

    #region ToEither Guards

    [Fact]
    public void ToEither_NullErrorFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var option = Some(5);

        // Act
        Action act = () => { _ = option.ToEither<TestError, int>(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorFactory");
    }

    #endregion

    #region GetOrThrow Guards

    [Fact]
    public void GetOrThrow_NullExceptionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => { _ = result.GetOrThrow(null!); };

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("exceptionFactory");
    }

    #endregion

    #region Async Guards

    [Fact]
    public async Task BindAsync_TaskEither_NullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));
        Func<int, Task<Either<TestError, int>>>? nullBinder = null;

        // Act
        var act = async () => await task.BindAsync(nullBinder!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("binder");
    }

    [Fact]
    public async Task BindAsync_TaskEither_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<Either<TestError, int>> task = null!;
        Func<int, Task<Either<TestError, int>>> binder = x => Task.FromResult(Right<TestError, int>(x * 2));

        // Act
        var act = async () => await task.BindAsync(binder);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("task");
    }

    [Fact]
    public async Task MapAsync_TaskEither_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));
        Func<int, Task<string>>? nullMapper = null;

        // Act
        var act = async () => await task.MapAsync(nullMapper!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("mapper");
    }

    [Fact]
    public async Task TapAsync_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));
        Func<int, Task>? nullAction = null;

        // Act
        var act = async () => await task.TapAsync(nullAction!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("action");
    }

    [Fact]
    public async Task BindAsync_Either_NullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var either = Right<TestError, int>(5);
        Func<int, Task<Either<TestError, int>>>? nullBinder = null;

        // Act
        var act = async () => await either.BindAsync(nullBinder!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("binder");
    }

    [Fact]
    public async Task MapAsync_Either_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var either = Right<TestError, int>(5);
        Func<int, Task<string>>? nullMapper = null;

        // Act
        var act = async () => await either.MapAsync(nullMapper!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await act());
        ex.ParamName.ShouldBe("mapper");
    }

    #endregion
}
