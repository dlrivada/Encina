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
        result.IsRight.Should().BeTrue();
        result.IfRight(tuple =>
        {
            tuple.Item1.Should().Be(1);
            tuple.Item2.Should().Be("hello");
        });
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
        result.IsLeft.Should().BeTrue();
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
        result.IsLeft.Should().BeTrue();
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
        result.IsRight.Should().BeTrue();
        result.IfRight(tuple =>
        {
            tuple.Item1.Should().Be(1);
            tuple.Item2.Should().Be("hello");
            tuple.Item3.Should().BeTrue();
        });
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
        result.IsRight.Should().BeTrue();
        result.IfRight(tuple =>
        {
            tuple.Item1.Should().Be(1);
            tuple.Item2.Should().Be("hello");
            tuple.Item3.Should().BeTrue();
            tuple.Item4.Should().Be(3.14);
        });
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
        combined.IsRight.Should().BeTrue();
        combined.IfRight(list =>
        {
            list.Should().BeEquivalentTo([1, 2, 3]);
        });
    }

    [Fact]
    public void Combine_Collection_OneLeft_ReturnsFirstLeft()
    {
        // Arrange
        var error = new TestError("Error");
        var results = new List<Either<TestError, int>>
        {
            Right<TestError, int>(1),
            Left<TestError, int>(error),
            Right<TestError, int>(3)
        };

        // Act
        var combined = results.Combine();

        // Assert
        combined.IsLeft.Should().BeTrue();
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
        doubled.IsRight.Should().BeTrue();
        doubled.IfRight(x => x.Should().Be(10));
    }

    [Fact]
    public void When_ConditionFalse_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var unchanged = result.When(false, x => Right<TestError, int>(x * 2));

        // Assert
        unchanged.IsRight.Should().BeTrue();
        unchanged.IfRight(x => x.Should().Be(5));
    }

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

    #region Ensure Tests

    [Fact]
    public void Ensure_PredicateTrue_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var ensured = result.Ensure(x => x > 0, _ => new TestError("Must be positive"));

        // Assert
        ensured.IsRight.Should().BeTrue();
        ensured.IfRight(x => x.Should().Be(5));
    }

    [Fact]
    public void Ensure_PredicateFalse_ReturnsError()
    {
        // Arrange
        var result = Right<TestError, int>(-5);

        // Act
        var ensured = result.Ensure(x => x > 0, _ => new TestError("Must be positive"));

        // Assert
        ensured.IsLeft.Should().BeTrue();
        ensured.IfLeft(e => e.Message.Should().Be("Must be positive"));
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
        ensured.IsLeft.Should().BeTrue();
        ensured.IfLeft(e => e.Message.Should().Be("Original error"));
    }

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

    #region OrElse Tests

    [Fact]
    public void OrElse_OnRight_ReturnsOriginal()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var orElse = result.OrElse(_ => Right<TestError, int>(0));

        // Assert
        orElse.IsRight.Should().BeTrue();
        orElse.IfRight(x => x.Should().Be(5));
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
        orElse.IsRight.Should().BeTrue();
        orElse.IfRight(x => x.Should().Be(0));
    }

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

    #region GetOrDefault Tests

    [Fact]
    public void GetOrDefault_OnRight_ReturnsValue()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var value = result.GetOrDefault(0);

        // Assert
        value.Should().Be(5);
    }

    [Fact]
    public void GetOrDefault_OnLeft_ReturnsDefault()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var value = result.GetOrDefault(0);

        // Assert
        value.Should().Be(0);
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
        value.Should().Be(5);
    }

    [Fact]
    public void GetOrElse_OnLeft_ReturnsFactoryResult()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var value = result.GetOrElse(e => e.Message.Length);

        // Assert
        value.Should().Be(5); // "Error".Length
    }

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
        executed.Should().BeTrue();
        tapped.IsRight.Should().BeTrue();
        tapped.IfRight(x => x.Should().Be(5));
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
        executed.Should().BeFalse();
        tapped.IsLeft.Should().BeTrue();
    }

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

    #region TapError Tests

    [Fact]
    public void TapError_OnLeft_ExecutesActionAndReturnsOriginal()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));
        var executed = false;

        // Act
        var tapped = result.TapError(e => executed = true);

        // Assert
        executed.Should().BeTrue();
        tapped.IsLeft.Should().BeTrue();
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
        executed.Should().BeFalse();
        tapped.IsRight.Should().BeTrue();
    }

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

    #region ToOption Tests

    [Fact]
    public void ToOption_OnRight_ReturnsSome()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var option = result.ToOption();

        // Assert
        option.IsSome.Should().BeTrue();
        option.IfSome(x => x.Should().Be(5));
    }

    [Fact]
    public void ToOption_OnLeft_ReturnsNone()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var option = result.ToOption();

        // Assert
        option.IsNone.Should().BeTrue();
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
        result.IsRight.Should().BeTrue();
        result.IfRight(x => x.Should().Be(5));
    }

    [Fact]
    public void ToEither_OnNone_ReturnsLeft()
    {
        // Arrange
        var option = Option<int>.None;

        // Act
        var result = option.ToEither(() => new TestError("Not found"));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(e => e.Message.Should().Be("Not found"));
    }

    [Fact]
    public void ToEither_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var option = Some(5);

        // Act
        var act = () => option.ToEither<TestError, int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("errorFactory");
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
        value.Should().Be(5);
    }

    [Fact]
    public void GetOrThrow_OnLeft_ThrowsException()
    {
        // Arrange
        var result = Left<TestError, int>(new TestError("Error"));

        // Act
        var act = () => result.GetOrThrow(e => new InvalidOperationException(e.Message));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Error");
    }

    [Fact]
    public void GetOrThrow_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Right<TestError, int>(5);

        // Act
        var act = () => result.GetOrThrow(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("exceptionFactory");
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
        result.IsRight.Should().BeTrue();
        result.IfRight(x => x.Should().Be(10));
    }

    [Fact]
    public async Task BindAsync_TaskEither_OnLeft_ReturnsLeft()
    {
        // Arrange
        var task = Task.FromResult(Left<TestError, int>(new TestError("Error")));

        // Act
        var result = await task.BindAsync(
            x => Task.FromResult(Right<TestError, int>(x * 2)));

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task MapAsync_TaskEither_OnRight_MapsCorrectly()
    {
        // Arrange
        var task = Task.FromResult(Right<TestError, int>(5));

        // Act
        var result = await task.MapAsync(x => Task.FromResult(x.ToString(CultureInfo.InvariantCulture)));

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(x => x.Should().Be("5"));
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
        executed.Should().BeTrue();
        result.IsRight.Should().BeTrue();
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
        result.IsRight.Should().BeTrue();
        result.IfRight(x => x.Should().Be(10));
    }

    [Fact]
    public async Task MapAsync_Either_OnRight_MapsCorrectly()
    {
        // Arrange
        var either = Right<TestError, int>(5);

        // Act
        var result = await either.MapAsync(x => Task.FromResult(x.ToString(CultureInfo.InvariantCulture)));

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(x => x.Should().Be("5"));
    }

    #endregion
}
