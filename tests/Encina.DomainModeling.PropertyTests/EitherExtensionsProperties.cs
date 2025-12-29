using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for Either extension invariants (monad laws, etc.).
/// </summary>
public sealed class EitherExtensionsProperties
{
    private sealed record TestError(string Message);

    #region Map Properties (Functor Laws)

    [Property(MaxTest = 200)]
    public bool Map_Identity_ReturnsEquivalent(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var mapped = either.Map(x => x);

        return mapped.Match(Left: _ => false, Right: v => v == value);
    }

    [Property(MaxTest = 200)]
    public bool Map_Composition(int value, int addend, int multiplier)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        Func<int, int> f = x => x + addend;
        Func<int, int> g = x => x * multiplier;

        var mapTwice = either.Map(f).Map(g);
        var mapComposed = either.Map(x => g(f(x)));

        return mapTwice.Match(Left: _ => false, Right: v1 =>
            mapComposed.Match(Left: _ => false, Right: v2 => v1 == v2));
    }

    #endregion

    #region Bind Properties (Monad Laws)

    [Property(MaxTest = 200)]
    public bool Bind_LeftIdentity(int value, int addend)
    {
        Func<int, Either<TestError, int>> f = x => Right<TestError, int>(x + addend);

        var direct = f(value);
        Either<TestError, int> wrapped = Right<TestError, int>(value);
        var bound = wrapped.Bind(f);

        return direct.Match(
            Left: _ => bound.IsLeft,
            Right: v1 => bound.Match(Left: _ => false, Right: v2 => v1 == v2));
    }

    [Property(MaxTest = 200)]
    public bool Bind_RightIdentity(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var bound = either.Bind(x => Right<TestError, int>(x));

        return either.Match(
            Left: _ => bound.IsLeft,
            Right: v1 => bound.Match(Left: _ => false, Right: v2 => v1 == v2));
    }

    [Property(MaxTest = 200)]
    public bool Bind_Associativity(int value, int add1, int add2)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        Func<int, Either<TestError, int>> f = x => Right<TestError, int>(x + add1);
        Func<int, Either<TestError, int>> g = x => Right<TestError, int>(x + add2);

        var leftAssoc = either.Bind(f).Bind(g);
        var rightAssoc = either.Bind(x => f(x).Bind(g));

        return leftAssoc.Match(
            Left: _ => rightAssoc.IsLeft,
            Right: v1 => rightAssoc.Match(Left: _ => false, Right: v2 => v1 == v2));
    }

    #endregion

    #region Combine Properties

    [Property(MaxTest = 100)]
    public bool Combine_BothRight_ReturnsRight(int value1, int value2)
    {
        Either<TestError, int> e1 = Right<TestError, int>(value1);
        Either<TestError, int> e2 = Right<TestError, int>(value2);

        var combined = e1.Combine(e2);

        return combined.Match(Left: _ => false, Right: tuple => tuple.Item1 == value1 && tuple.Item2 == value2);
    }

    [Property(MaxTest = 100)]
    public bool Combine_FirstLeft_ReturnsLeft(int value2)
    {
        Either<TestError, int> e1 = Left<TestError, int>(new TestError("first"));
        Either<TestError, int> e2 = Right<TestError, int>(value2);

        var combined = e1.Combine(e2);

        return combined.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Combine_SecondLeft_ReturnsLeft(int value1)
    {
        Either<TestError, int> e1 = Right<TestError, int>(value1);
        Either<TestError, int> e2 = Left<TestError, int>(new TestError("second"));

        var combined = e1.Combine(e2);

        return combined.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Combine3_AllRight_ReturnsRight(int v1, int v2, int v3)
    {
        Either<TestError, int> e1 = Right<TestError, int>(v1);
        Either<TestError, int> e2 = Right<TestError, int>(v2);
        Either<TestError, int> e3 = Right<TestError, int>(v3);

        var combined = e1.Combine(e2, e3);

        return combined.Match(Left: _ => false, Right: tuple => tuple.Item1 == v1 && tuple.Item2 == v2 && tuple.Item3 == v3);
    }

    #endregion

    #region Ensure Properties

    [Property(MaxTest = 200)]
    public bool Ensure_PassingPredicate_ReturnsOriginal(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.Ensure(x => true, _ => new TestError("should not appear"));

        return result.Match(Left: _ => false, Right: v => v == value);
    }

    [Property(MaxTest = 200)]
    public bool Ensure_FailingPredicate_ReturnsError(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);
        var expectedError = new TestError("failed predicate");

        var result = either.Ensure(x => false, _ => expectedError);

        return result.Match(Left: e => e == expectedError, Right: _ => false);
    }

    [Property(MaxTest = 200)]
    public bool Ensure_OnLeft_ReturnsOriginalLeft(NonEmptyString message)
    {
        var originalError = new TestError(message.Get);
        Either<TestError, int> either = Left<TestError, int>(originalError);
        var otherError = new TestError("other");

        var result = either.Ensure(x => false, _ => otherError);

        return result.Match(Left: e => e == originalError, Right: _ => false);
    }

    #endregion

    #region When Properties

    [Property(MaxTest = 200)]
    public bool When_TrueCondition_ExecutesTransform(int value, int addend)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.When(true, v => Right<TestError, int>(v + addend));

        return result.Match(Left: _ => false, Right: v => v == value + addend);
    }

    [Property(MaxTest = 200)]
    public bool When_FalseCondition_ReturnsOriginal(int value, int addend)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.When(false, v => Right<TestError, int>(v + addend));

        return result.Match(Left: _ => false, Right: v => v == value);
    }

    #endregion

    #region GetOrDefault Properties

    [Property(MaxTest = 200)]
    public bool GetOrDefault_Right_ReturnsValue(int value, int defaultVal)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.GetOrDefault(defaultVal);

        return result == value;
    }

    [Property(MaxTest = 200)]
    public bool GetOrDefault_Left_ReturnsDefault(int defaultVal)
    {
        Either<TestError, int> either = Left<TestError, int>(new TestError("error"));

        var result = either.GetOrDefault(defaultVal);

        return result == defaultVal;
    }

    [Property(MaxTest = 200)]
    public bool GetOrElse_Right_ReturnsValue(int value, int fallback)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.GetOrElse(_ => fallback);

        return result == value;
    }

    [Property(MaxTest = 200)]
    public bool GetOrElse_Left_ReturnsFallbackResult(int fallback)
    {
        Either<TestError, int> either = Left<TestError, int>(new TestError("error"));

        var result = either.GetOrElse(_ => fallback);

        return result == fallback;
    }

    #endregion

    #region OrElse Properties

    [Property(MaxTest = 200)]
    public bool OrElse_Right_ReturnsOriginal(int value, int fallbackValue)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var result = either.OrElse(_ => Right<TestError, int>(fallbackValue));

        return result.Match(Left: _ => false, Right: v => v == value);
    }

    [Property(MaxTest = 200)]
    public bool OrElse_Left_ReturnsFallback(int fallbackValue)
    {
        Either<TestError, int> either = Left<TestError, int>(new TestError("error"));

        var result = either.OrElse(_ => Right<TestError, int>(fallbackValue));

        return result.Match(Left: _ => false, Right: v => v == fallbackValue);
    }

    #endregion

    #region Tap Properties

    [Property(MaxTest = 200)]
    public bool Tap_Right_ExecutesAction(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);
        var executed = false;
        var capturedValue = 0;

        var result = either.Tap(v =>
        {
            executed = true;
            capturedValue = v;
        });

        return executed && capturedValue == value && result.Match(Left: _ => false, Right: v => v == value);
    }

    [Property(MaxTest = 200)]
    public bool Tap_Left_DoesNotExecuteAction(NonEmptyString message)
    {
        Either<TestError, int> either = Left<TestError, int>(new TestError(message.Get));
        var executed = false;

        var result = either.Tap(_ => executed = true);

        return !executed && result.IsLeft;
    }

    [Property(MaxTest = 200)]
    public bool TapError_Left_ExecutesAction(NonEmptyString message)
    {
        var error = new TestError(message.Get);
        Either<TestError, int> either = Left<TestError, int>(error);
        var executed = false;
        TestError? capturedError = null;

        var result = either.TapError(e =>
        {
            executed = true;
            capturedError = e;
        });

        return executed && capturedError == error && result.IsLeft;
    }

    [Property(MaxTest = 200)]
    public bool TapError_Right_DoesNotExecuteAction(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);
        var executed = false;

        var result = either.TapError(_ => executed = true);

        return !executed && result.Match(Left: _ => false, Right: v => v == value);
    }

    #endregion

    #region ToOption Properties

    [Property(MaxTest = 200)]
    public bool ToOption_Right_ReturnsSome(int value)
    {
        Either<TestError, int> either = Right<TestError, int>(value);

        var option = either.ToOption();

        return option.IsSome && option.Match(Some: v => v == value, None: () => false);
    }

    [Property(MaxTest = 200)]
    public bool ToOption_Left_ReturnsNone()
    {
        Either<TestError, int> either = Left<TestError, int>(new TestError("error"));

        var option = either.ToOption();

        return option.IsNone;
    }

    #endregion
}
