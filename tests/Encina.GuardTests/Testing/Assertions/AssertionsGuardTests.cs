using Encina.Testing;

namespace Encina.GuardTests.Testing.Assertions;

/// <summary>
/// Guard tests for EitherAssertions, EitherCollectionAssertions, and StreamingAssertions.
/// Uses explicit static class invocation to avoid ambiguity with Shouldly extensions.
/// </summary>
public class AssertionsGuardTests
{
    #region EitherAssertions

    public class EitherAssertionsGuards
    {
        [Fact]
        public void ShouldBeSuccess_ErrorResult_Throws()
        {
            var result = LanguageExt.Prelude.Left<string, int>("error");

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeSuccess(result));
        }

        [Fact]
        public void ShouldBeError_SuccessResult_Throws()
        {
            var result = LanguageExt.Prelude.Right<string, int>(42);

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeError(result));
        }

        [Fact]
        public void ShouldBeSuccess_WithExpectedValue_WrongValue_Throws()
        {
            var result = LanguageExt.Prelude.Right<string, int>(42);

            Should.Throw<Xunit.Sdk.EqualException>(() =>
                EitherAssertions.ShouldBeSuccess(result, 99));
        }

        [Fact]
        public void ShouldBeSuccessAnd_ErrorResult_Throws()
        {
            var result = LanguageExt.Prelude.Left<string, int>("error");

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeSuccessAnd(result));
        }

        [Fact]
        public void ShouldBeErrorAnd_SuccessResult_Throws()
        {
            var result = LanguageExt.Prelude.Right<string, int>(42);

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeErrorAnd(result));
        }

        [Fact]
        public void ShouldBeRight_ErrorResult_Throws()
        {
            var result = LanguageExt.Prelude.Left<string, int>("error");

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeRight(result));
        }

        [Fact]
        public void ShouldBeRightAnd_ErrorResult_Throws()
        {
            var result = LanguageExt.Prelude.Left<string, int>("error");

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeRightAnd(result));
        }

        [Fact]
        public void ShouldBeLeft_SuccessResult_Throws()
        {
            var result = LanguageExt.Prelude.Right<string, int>(42);

            Should.Throw<Xunit.Sdk.TrueException>(() =>
                EitherAssertions.ShouldBeLeft(result));
        }

        [Fact]
        public void ShouldBeSuccess_WithValidator_RunsValidator()
        {
            var result = LanguageExt.Prelude.Right<string, int>(42);
            var validated = false;

            EitherAssertions.ShouldBeSuccess(result, _ => validated = true);

            validated.ShouldBeTrue();
        }

        [Fact]
        public void ShouldBeError_WithValidator_RunsValidator()
        {
            var result = LanguageExt.Prelude.Left<string, int>("error");
            var validated = false;

            EitherAssertions.ShouldBeError(result, _ => validated = true);

            validated.ShouldBeTrue();
        }
    }

    #endregion

    #region EitherCollectionAssertions

    public class EitherCollectionAssertionsGuards
    {
        [Fact]
        public void ShouldAllBeSuccess_WithErrors_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Right<string, int>(1),
                LanguageExt.Prelude.Left<string, int>("error"),
                LanguageExt.Prelude.Right<string, int>(3),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldAllBeSuccess(results));
        }

        [Fact]
        public void ShouldAllBeError_WithSuccesses_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Left<string, int>("error"),
                LanguageExt.Prelude.Right<string, int>(2),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldAllBeError(results));
        }

        [Fact]
        public void ShouldAllBeSuccessAnd_WithErrors_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Left<string, int>("error"),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldAllBeSuccessAnd(results));
        }

        [Fact]
        public void ShouldContainSuccess_AllErrors_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Left<string, int>("error1"),
                LanguageExt.Prelude.Left<string, int>("error2"),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldContainSuccess(results));
        }

        [Fact]
        public void ShouldContainError_AllSuccesses_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Right<string, int>(1),
                LanguageExt.Prelude.Right<string, int>(2),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldContainError(results));
        }

        [Fact]
        public void ShouldHaveSuccessCount_WrongCount_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Right<string, int>(1),
                LanguageExt.Prelude.Right<string, int>(2),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldHaveSuccessCount(results, 5));
        }

        [Fact]
        public void ShouldHaveErrorCount_WrongCount_Throws()
        {
            var results = new[]
            {
                LanguageExt.Prelude.Left<string, int>("error"),
            };

            Should.Throw<Xunit.Sdk.XunitException>(() =>
                EitherCollectionAssertions.ShouldHaveErrorCount(results, 5));
        }
    }

    #endregion

    #region StreamingAssertions

    public class StreamingAssertionsGuards
    {
        [Fact]
        public async Task ShouldAllBeSuccessAsync_WithErrors_Throws()
        {
            var stream = CreateEitherStream(
                LanguageExt.Prelude.Right<string, int>(1),
                LanguageExt.Prelude.Left<string, int>("error"));

            await Should.ThrowAsync<Xunit.Sdk.XunitException>(async () =>
                await StreamingAssertions.ShouldAllBeSuccessAsync(stream));
        }

        [Fact]
        public async Task ShouldAllBeErrorAsync_WithSuccesses_Throws()
        {
            var stream = CreateEitherStream(
                LanguageExt.Prelude.Left<string, int>("error"),
                LanguageExt.Prelude.Right<string, int>(2));

            await Should.ThrowAsync<Xunit.Sdk.XunitException>(async () =>
                await StreamingAssertions.ShouldAllBeErrorAsync(stream));
        }

        [Fact]
        public async Task CollectAsync_EmptyStream_ReturnsEmpty()
        {
            var stream = CreateStream<int>();

            var result = await StreamingAssertions.CollectAsync(stream);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task ShouldHaveCountAsync_WrongCount_Throws()
        {
            var stream = CreateEitherStream(
                LanguageExt.Prelude.Right<string, int>(1),
                LanguageExt.Prelude.Right<string, int>(2),
                LanguageExt.Prelude.Right<string, int>(3));

            await Should.ThrowAsync<Xunit.Sdk.EqualException>(async () =>
                await StreamingAssertions.ShouldHaveCountAsync(stream, 5));
        }

        private static async IAsyncEnumerable<T> CreateStream<T>(params T[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        private static async IAsyncEnumerable<LanguageExt.Either<TLeft, TRight>> CreateEitherStream<TLeft, TRight>(
            params LanguageExt.Either<TLeft, TRight>[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }
    }

    #endregion
}
