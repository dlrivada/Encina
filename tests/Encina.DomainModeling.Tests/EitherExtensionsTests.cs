using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Unit tests for the Either extension methods.
/// </summary>
public class EitherExtensionsTests
{
    private sealed record TestError(string Message);

    #region Combine Tests

    [Fact]
    public void Combine_TwoRights_ReturnsTuple()
    {
        // Arrange
        var first = Right<TestError, int>(1);
        var second = Right<TestError, string>("hello");

        // Act
        var result = first.Combine(second);

        // Assert
        var tuple = result.ShouldBeSuccess();
        tuple.Item1.ShouldBe(1);
        tuple.Item2.ShouldBe("hello");
    }

    [Fact]
    public void Combine_FirstIsLeft_ReturnsLeft()
    {
        // Arrange
        var error = new TestError("Error");
        var first = Left<TestError, int>(error);
        var second = Right<TestError, string>("hello");

        // Act
        var result = first.Combine(second);

        // Assert
        var returnedError = result.ShouldBeError();
        returnedError.ShouldBe(error);
    }

    [Fact]
    public void Combine_SecondIsLeft_ReturnsLeft()
    {
        // Arrange
        var error = new TestError("Error");
        var first = Right<TestError, int>(1);
        var second = Left<TestError, string>(error);

        // Act
        var result = first.Combine(second);

        // Assert
        var returnedError = result.ShouldBeError();
        returnedError.ShouldBe(error);
    }

    [Fact]
    public void Combine_ThreeRights_ReturnsTuple()
    {
        // Arrange
        var first = Right<TestError, int>(1);
        var second = Right<TestError, string>("hello");
        var third = Right<TestError, bool>(true);

        // Act
        var result = first.Combine(second, third);

        // Assert
        var tuple = result.ShouldBeSuccess();
        tuple.Item1.ShouldBe(1);
        tuple.Item2.ShouldBe("hello");
        tuple.Item3.ShouldBeTrue();
    }

    [Fact]
    public void Combine_FourRights_ReturnsTuple()
    {
        // Arrange
        var first = Right<TestError, int>(1);
        var second = Right<TestError, string>("hello");
        var third = Right<TestError, bool>(true);
        var fourth = Right<TestError, double>(3.14);

        // Act
        var result = first.Combine(second, third, fourth);

        // Assert
        var tuple = result.ShouldBeSuccess();
        tuple.Item1.ShouldBe(1);
        tuple.Item2.ShouldBe("hello");
        tuple.Item3.ShouldBeTrue();
        tuple.Item4.ShouldBe(3.14);
    }

    [Fact]
    public void Combine_Collection_AllRights_ReturnsList()
    {
        // Arrange
        var results = new List<Either<TestError, int>>
        {
            Right<TestError, int>(1),
            Right<TestError, int>(2),
            Right<TestError, int>(3)
        };

        // Act
        var combined = results.Combine();

        // Assert
        var list = combined.ShouldBeSuccess();
        list.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Combine_Collection_OneLeft_ReturnsFirstLeft()
    {
        // Arrange
        var firstError = new TestError("First Error");
        var secondError = new TestError("Second Error");
        var results = new List<Either<TestError, int>>
        {
            Right<TestError, int>(1),
            Left<TestError, int>(firstError),
            Left<TestError, int>(secondError),
            Right<TestError, int>(3)
        };

        // Act
        var combined = results.Combine();

        // Assert
        var returnedError = combined.ShouldBeError();
        returnedError.ShouldBe(firstError);
    }

    #endregion

    #region When Tests

    [Fact]
    public void When_ConditionTrue_ExecutesOperation()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var doubled = result.When(true, x => Right<TestError, int>(x * 2));

        // Assert
        var value = doubled.ShouldBeSuccess();
        value.ShouldBe(10);
    }

    [Fact]
    public void When_ConditionFalse_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var unchanged = result.When(false, x => Right<TestError, int>(x * 2));

        // Assert
        var value = unchanged.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void When_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.When(true, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("operation");
    }

    #endregion

    #region Ensure Tests

    [Fact]
    public void Ensure_PredicateTrue_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var ensured = result.Ensure(x => x > 0, _ => new TestError("Must be positive"));

        // Assert
        var value = ensured.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void Ensure_PredicateFalse_ReturnsError()
    {
        // Arrange
        var result = Right<TestError, int>(-5);

        // Act
        var ensured = result.Ensure(x => x > 0, _ => new TestError("Must be positive"));

        // Assert
        var error = ensured.ShouldBeError();
        error.Message.ShouldBe("Must be positive");
    }

    [Fact]
    public void Ensure_OnLeft_ReturnsOriginalLeft()
    {
        // Arrange
        var error = new TestError("Original error");
        var result = Left<TestError, int>(error);

        // Act
        var ensured = result.Ensure(x => x > 0, _ => new TestError("New error"));

        // Assert
        var errorResult = ensured.ShouldBeError();
        errorResult.Message.ShouldBe("Original error");
    }

    [Fact]
    public void Ensure_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.Ensure(null!, _ => new TestError("Error"));

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
        Action act = () => _ = result.Ensure(x => x > 0, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorFactory");
    }

    #endregion

    #region OrElse Tests

    [Fact]
    public void OrElse_OnRight_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var orElse = result.OrElse(_ => Right<TestError, int>(0));

        // Assert
        var value = orElse.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void OrElse_OnLeft_ReturnsFallback()
    {
        // Arrange
        var error = new TestError("Original error");
        var result = Left<TestError, int>(error);

        // Act
        var orElse = result.OrElse(_ => Right<TestError, int>(0));

        // Assert
        var value = orElse.ShouldBeSuccess();
        value.ShouldBe(0);
    }

    [Fact]
    public void OrElse_NullFallback_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.OrElse(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("fallback");
    }

    #endregion

    #region GetOrDefault Tests

    [Fact]
    public void GetOrDefault_OnRight_ReturnsValue()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var value = result.GetOrDefault(0);

        // Assert
        value.ShouldBe(5);
    }

    [Fact]
    public void GetOrDefault_OnLeft_ReturnsDefault()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var value = result.GetOrDefault(0);

        // Assert
        value.ShouldBe(0);
    }

    #endregion

    #region GetOrElse Tests

    [Fact]
    public void GetOrElse_OnRight_ReturnsValue()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var value = result.GetOrElse(_ => 0);

        // Assert
        value.ShouldBe(5);
    }

    [Fact]
    public void GetOrElse_OnLeft_ReturnsFactoryResult()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var value = result.GetOrElse(e => e.Message.Length);

        // Assert
        value.ShouldBe(5); // "Error".Length
    }

    [Fact]
    public void GetOrElse_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.GetOrElse(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("defaultFactory");
    }

    #endregion

    #region Tap Tests

    [Fact]
    public void Tap_OnRight_ExecutesActionAndReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);
        var executed = false;

        // Act
        var tapped = result.Tap(x => executed = true);

        // Assert
        executed.ShouldBeTrue();
        var value = tapped.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void Tap_OnLeft_DoesNotExecuteAction()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));
        var executed = false;

        // Act
        var tapped = result.Tap(x => executed = true);

        // Assert
        executed.ShouldBeFalse();
        tapped.ShouldBeError();
    }

    [Fact]
    public void Tap_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.Tap(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("action");
    }

    #endregion

    #region TapError Tests

    [Fact]
    public void TapError_OnLeft_ExecutesActionAndReturnsOriginal()
    {
        // Arrange
        var originalError = new TestError("Error");
        var result = Left<TestError, int>(originalError);
        var executed = false;

        // Act
        var tapped = result.TapError(e => executed = true);

        // Assert
        executed.ShouldBeTrue();
        var returnedError = tapped.ShouldBeError();
        returnedError.ShouldBe(originalError);
    }

    [Fact]
    public void TapError_OnRight_DoesNotExecuteAction()
    {
        // Arrange
        var result = Right<TestError, int>(5);
        var executed = false;

        // Act
        var tapped = result.TapError(e => executed = true);

        // Assert
        executed.ShouldBeFalse();
        var value = tapped.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void TapError_NullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.TapError(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("action");
    }

    #endregion

    #region ToOption Tests

    [Fact]
    public void ToOption_OnRight_ReturnsSome()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var option = result.ToOption();

        // Assert
        option.IsSome.ShouldBeTrue();
        option.IfSome(x => x.ShouldBe(5));
    }

    [Fact]
    public void ToOption_OnLeft_ReturnsNone()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var option = result.ToOption();

        // Assert
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region ToEither Tests

    [Fact]
    public void ToEither_OnSome_ReturnsRight()
    {
        // Arrange
        var option = Some(5);

        // Act
        var result = option.ToEither(() => new TestError("Not found"));

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public void ToEither_OnNone_ReturnsLeft()
    {
        // Arrange
        var option = Option<int>.None;

        // Act
        var result = option.ToEither(() => new TestError("Not found"));

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldBe("Not found");
    }

    [Fact]
    public void ToEither_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var option = Some(5);

        // Act
        Action act = () => _ = option.ToEither<TestError, int>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorFactory");
    }

    #endregion

    #region GetOrThrow Tests

    [Fact]
    public void GetOrThrow_OnRight_ReturnsValue()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var value = result.GetOrThrow(e => new InvalidOperationException(e.Message));

        // Assert
        value.ShouldBe(5);
    }

    [Fact]
    public void GetOrThrow_OnLeft_ThrowsException()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        Action act = () => _ = result.GetOrThrow(e => new InvalidOperationException(e.Message));

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("Error");
    }

    [Fact]
    public void GetOrThrow_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        Action act = () => _ = result.GetOrThrow(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("exceptionFactory");
    }

    #endregion

    #region Async Extensions Tests

    [Fact]
    public async Task BindAsync_TaskEither_OnRight_BindsCorrectly()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var result = await task.BindAsync(
            x => Task.FromResult(Right<TestError, int>(x * 2)));

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(10);
    }

    [Fact]
    public async Task BindAsync_TaskEither_OnLeft_ReturnsLeft()
    {
        // Arrange
        var expectedError = new TestError("Error");
        var task = Task.FromResult(Left<TestError, int>(expectedError));

        // Act
        var result = await task.BindAsync(
            x => Task.FromResult(Right<TestError, int>(x * 2)));

        // Assert
        var returnedError = result.ShouldBeError();
        returnedError.ShouldBe(expectedError);
    }

    [Fact]
    public async Task MapAsync_TaskEither_OnRight_MapsCorrectly()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var result = await task.MapAsync(x => Task.FromResult(x.ToString(CultureInfo.InvariantCulture)));

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe("5");
    }

    [Fact]
    public async Task TapAsync_TaskEither_OnRight_ExecutesAction()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));
        var executed = false;

        // Act
        var result = await task.TapAsync(x =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        executed.ShouldBeTrue();
        var value = result.ShouldBeSuccess();
        value.ShouldBe(5);
    }

    [Fact]
    public async Task BindAsync_Either_OnRight_BindsCorrectly()
    {
        // Arrange
        var either = Right<TestError, int>(5);

        // Act
        var result = await either.BindAsync(
            x => Task.FromResult(Right<TestError, int>(x * 2)));

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(10);
    }

    [Fact]
    public async Task MapAsync_Either_OnRight_MapsCorrectly()
    {
        // Arrange
        var either = Right<TestError, int>(5);

        // Act
        var result = await either.MapAsync(x => Task.FromResult(x.ToString(CultureInfo.InvariantCulture)));

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe("5");
    }

    #endregion
}
