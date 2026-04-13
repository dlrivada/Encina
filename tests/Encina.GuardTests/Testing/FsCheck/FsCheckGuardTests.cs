using System.Globalization;

using Encina.Testing.FsCheck;

using FsCheck;
using FsCheck.Fluent;

using LanguageExt;

using Shouldly;

namespace Encina.GuardTests.Testing.FsCheck;

/// <summary>
/// Guard tests for Encina.Testing.FsCheck covering null-guard clauses and happy paths
/// for <see cref="GenExtensions"/>, <see cref="EncinaProperties"/>, and
/// <see cref="EncinaArbitraries"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class FsCheckGuardTests
{
    private static readonly Func<int, string> IntToString = x => x.ToString(CultureInfo.InvariantCulture);

    // ─── GenExtensions null guards ───

    [Fact]
    public void ToEither_NullGen_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            GenExtensions.ToEither<string>(null!));
    }

    [Fact]
    public void ToSuccess_NullGen_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            GenExtensions.ToSuccess<string>(null!));
    }

    [Fact]
    public void ToFailure_NullGen_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            GenExtensions.ToFailure<string>(null!));
    }

    [Fact]
    public void OrNull_NullGen_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            GenExtensions.OrNull<string>(null!));
    }

    [Fact]
    public void OrNullValue_NullGen_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            GenExtensions.OrNullValue<int>(null!));
    }

    // ─── GenExtensions happy paths ───

    [Fact]
    public void ToEither_ValidGen_ReturnsGen()
    {
        var gen = Gen.Constant("hello").ToEither();
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void ToSuccess_ValidGen_ReturnsGen()
    {
        var gen = Gen.Constant(42).ToSuccess();
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void ToFailure_ValidGen_ReturnsGen()
    {
        var gen = EncinaArbitraries.EncinaError().Generator.ToFailure<int>();
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void OrNull_ValidGen_ReturnsGen()
    {
        var gen = Gen.Constant("value").OrNull();
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void OrNull_ZeroProbability_ReturnsGen()
    {
        var gen = Gen.Constant("value").OrNull(0.0);
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void OrNull_OneProbability_ReturnsGen()
    {
        var gen = Gen.Constant("value").OrNull(1.0);
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void OrNull_NegativeProbability_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Gen.Constant("value").OrNull(-0.1));
    }

    [Fact]
    public void OrNull_OverOneProbability_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Gen.Constant("value").OrNull(1.1));
    }

    [Fact]
    public void OrNullValue_ValidGen_ReturnsGen()
    {
        var gen = Gen.Constant(42).OrNullValue();
        gen.ShouldNotBeNull();
    }

    [Fact]
    public void OrNullValue_NegativeProbability_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Gen.Constant(42).OrNullValue(-0.1));
    }

    [Fact]
    public void OrNullValue_OverOneProbability_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Gen.Constant(42).OrNullValue(1.1));
    }

    // ─── EncinaProperties null guards ───

    [Fact]
    public void MapPreservesRightState_NullMapper_Throws()
    {
        var either = Either<EncinaError, int>.Right(1);
        Should.Throw<ArgumentNullException>(() =>
            EncinaProperties.MapPreservesRightState<int, string>(either, null!));
    }

    [Fact]
    public void MapPreservesLeftError_NullMapper_Throws()
    {
        var either = Either<EncinaError, int>.Right(1);
        Should.Throw<ArgumentNullException>(() =>
            EncinaProperties.MapPreservesLeftError<int, string>(either, null!));
    }

    [Fact]
    public void HandlerIsDeterministic_NullHandler_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaProperties.HandlerIsDeterministic<string, int>(null!, "test"));
    }

    [Fact]
    public void AsyncHandlerIsDeterministic_NullHandler_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaProperties.AsyncHandlerIsDeterministic<string, int>(null!, "test"));
    }

    // ─── EncinaProperties happy paths ───

    [Fact]
    public void EitherIsExclusive_Right_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Right(42);
        var prop = EncinaProperties.EitherIsExclusive(either);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void EitherIsExclusive_Left_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Left(EncinaError.New("err"));
        var prop = EncinaProperties.EitherIsExclusive(either);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void MapPreservesRightState_RightValue_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Right(1);
        var prop = EncinaProperties.MapPreservesRightState(either, IntToString);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void MapPreservesRightState_LeftValue_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Left(EncinaError.New("e"));
        var prop = EncinaProperties.MapPreservesRightState(either, IntToString);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void MapPreservesLeftError_LeftValue_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Left(EncinaError.New("e"));
        var prop = EncinaProperties.MapPreservesLeftError(either, IntToString);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void MapPreservesLeftError_RightValue_ReturnsProperty()
    {
        var either = Either<EncinaError, int>.Right(1);
        var prop = EncinaProperties.MapPreservesLeftError(either, IntToString);
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void HandlerIsDeterministic_DeterministicHandler_ReturnsProperty()
    {
        var prop = EncinaProperties.HandlerIsDeterministic<string, int>(
            s => Either<EncinaError, int>.Right(s.Length), "test");
        prop.ShouldNotBeNull();
    }

    [Fact]
    public void AsyncHandlerIsDeterministic_DeterministicHandler_ReturnsProperty()
    {
        var prop = EncinaProperties.AsyncHandlerIsDeterministic<string, int>(
            (s, _) => Task.FromResult(Either<EncinaError, int>.Right(s.Length)), "test");
        prop.ShouldNotBeNull();
    }

    // ─── EncinaArbitraries factory methods ───

    [Fact]
    public void EncinaError_ReturnsArbitrary()
    {
        var arb = EncinaArbitraries.EncinaError();
        arb.ShouldNotBeNull();
    }

    [Fact]
    public void OutboxMessage_ReturnsArbitrary()
    {
        var arb = EncinaArbitraries.OutboxMessage();
        arb.ShouldNotBeNull();
    }

    [Fact]
    public void InboxMessage_ReturnsArbitrary()
    {
        var arb = EncinaArbitraries.InboxMessage();
        arb.ShouldNotBeNull();
    }

    [Fact]
    public void SagaState_ReturnsArbitrary()
    {
        var arb = EncinaArbitraries.SagaState();
        arb.ShouldNotBeNull();
    }

    [Fact]
    public void ScheduledMessage_ReturnsArbitrary()
    {
        var arb = EncinaArbitraries.ScheduledMessage();
        arb.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultSeed_IsPositive()
    {
        EncinaArbitraries.DefaultSeed.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SagaStatuses_AreNotEmpty()
    {
        EncinaArbitraries.SagaStatuses.ShouldNotBeEmpty();
    }
}
