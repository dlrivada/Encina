using Encina.Testing;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Base;

public sealed class StreamingAssertionsTests
{
    #region Helper Methods

    private static async IAsyncEnumerable<Either<string, int>> CreateSuccessStreamAsync(params int[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return Right(value);
        }
    }

    private static async IAsyncEnumerable<Either<string, int>> CreateErrorStreamAsync(params string[] errors)
    {
        foreach (var error in errors)
        {
            await Task.Yield();
            yield return Left(error);
        }
    }

    private static async IAsyncEnumerable<Either<string, int>> CreateMixedStreamAsync()
    {
        await Task.Yield();
        yield return Right(1);
        await Task.Yield();
        yield return Left("error");
        await Task.Yield();
        yield return Right(3);
    }

    private static async IAsyncEnumerable<Either<EncinaError, int>> CreateEncinaErrorStreamAsync()
    {
        await Task.Yield();
        yield return Right(1);
        await Task.Yield();
        yield return EncinaErrors.Create("encina.validation.required", "The field 'Email' is required");
        await Task.Yield();
        yield return Right(3);
    }

    #endregion

    #region CollectAsync Tests

    [Fact]
    public async Task CollectAsync_ReturnsAllItems()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2, 3);

        // Act
        var results = await stream.CollectAsync();

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task CollectAsync_EmptyStream_ReturnsNoItems()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync();

        // Act
        var results = await stream.CollectAsync();

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region ShouldAllBeSuccessAsync Tests

    [Fact]
    public async Task ShouldAllBeSuccessAsync_WhenAllRight_ReturnsValues()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2, 3);

        // Act
        var values = await stream.ShouldAllBeSuccessAsync();

        // Assert
        values.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ShouldAllBeSuccessAsync_WhenSomeLeft_Throws()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await stream.ShouldAllBeSuccessAsync());
    }

    [Fact]
    public async Task ShouldAllBeSuccessAndAsync_ReturnsAndConstraint()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2);

        // Act
        var constraint = await stream.ShouldAllBeSuccessAndAsync();

        // Assert
        constraint.Value.Count.ShouldBe(2);
    }

    #endregion

    #region ShouldAllBeErrorAsync Tests

    [Fact]
    public async Task ShouldAllBeErrorAsync_WhenAllLeft_ReturnsErrors()
    {
        // Arrange
        var stream = CreateErrorStreamAsync("error1", "error2");

        // Act
        var errors = await stream.ShouldAllBeErrorAsync();

        // Assert
        errors.ShouldBe(["error1", "error2"]);
    }

    [Fact]
    public async Task ShouldAllBeErrorAsync_WhenSomeRight_Throws()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await stream.ShouldAllBeErrorAsync());
    }

    #endregion

    #region ShouldContainSuccessAsync Tests

    [Fact]
    public async Task ShouldContainSuccessAsync_WhenAtLeastOneRight_ReturnsFirst()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act
        var value = await stream.ShouldContainSuccessAsync();

        // Assert
        value.ShouldBe(1);
    }

    [Fact]
    public async Task ShouldContainSuccessAsync_WhenAllLeft_Throws()
    {
        // Arrange
        var stream = CreateErrorStreamAsync("error1", "error2");

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await stream.ShouldContainSuccessAsync());
    }

    [Fact]
    public async Task ShouldContainSuccessAndAsync_ReturnsAndConstraint()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act
        var constraint = await stream.ShouldContainSuccessAndAsync();

        // Assert
        constraint.Value.ShouldBe(1);
    }

    #endregion

    #region ShouldContainErrorAsync Tests

    [Fact]
    public async Task ShouldContainErrorAsync_WhenAtLeastOneLeft_ReturnsFirst()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act
        var error = await stream.ShouldContainErrorAsync();

        // Assert
        error.ShouldBe("error");
    }

    [Fact]
    public async Task ShouldContainErrorAsync_WhenAllRight_Throws()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2);

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await stream.ShouldContainErrorAsync());
    }

    #endregion

    #region ShouldHaveCountAsync Tests

    [Fact]
    public async Task ShouldHaveCountAsync_WhenCorrectCount_ReturnsResults()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2, 3);

        // Act
        var results = await stream.ShouldHaveCountAsync(3);

        // Assert
        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ShouldHaveCountAsync_WhenWrongCount_Throws()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2);

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.EqualException>(async () =>
            await stream.ShouldHaveCountAsync(5));
    }

    #endregion

    #region ShouldHaveSuccessCountAsync Tests

    [Fact]
    public async Task ShouldHaveSuccessCountAsync_WhenCorrectCount_ReturnsValues()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act
        var values = await stream.ShouldHaveSuccessCountAsync(2);

        // Assert
        values.ShouldBe([1, 3]);
    }

    #endregion

    #region ShouldHaveErrorCountAsync Tests

    [Fact]
    public async Task ShouldHaveErrorCountAsync_WhenCorrectCount_ReturnsErrors()
    {
        // Arrange
        var stream = CreateMixedStreamAsync();

        // Act
        var errors = await stream.ShouldHaveErrorCountAsync(1);

        // Assert
        errors.ShouldBe(["error"]);
    }

    #endregion

    #region FirstShouldBeSuccessAsync Tests

    [Fact]
    public async Task FirstShouldBeSuccessAsync_WhenFirstIsRight_ReturnsValue()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(42, 100);

        // Act
        var value = await stream.FirstShouldBeSuccessAsync();

        // Assert
        value.ShouldBe(42);
    }

    [Fact]
    public async Task FirstShouldBeSuccessAsync_WhenFirstIsLeft_Throws()
    {
        // Arrange
        var stream = CreateErrorStreamAsync("first error");

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.TrueException>(async () =>
            await stream.FirstShouldBeSuccessAsync());
    }

    #endregion

    #region FirstShouldBeErrorAsync Tests

    [Fact]
    public async Task FirstShouldBeErrorAsync_WhenFirstIsLeft_ReturnsError()
    {
        // Arrange
        var stream = CreateErrorStreamAsync("first error", "second error");

        // Act
        var error = await stream.FirstShouldBeErrorAsync();

        // Assert
        error.ShouldBe("first error");
    }

    [Fact]
    public async Task FirstShouldBeErrorAsync_WhenFirstIsRight_Throws()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(42);

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.TrueException>(async () =>
            await stream.FirstShouldBeErrorAsync());
    }

    #endregion

    #region ShouldBeEmptyAsync Tests

    [Fact]
    public async Task ShouldBeEmptyAsync_WhenEmpty_Succeeds()
    {
        // Arrange
        static async IAsyncEnumerable<Either<string, int>> EmptyStream()
        {
            await Task.Yield();
            yield break;
        }

        // Act & Assert (should not throw)
        await EmptyStream().ShouldBeEmptyAsync();
    }

    [Fact]
    public async Task ShouldBeEmptyAsync_WhenNotEmpty_Throws()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1);

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await stream.ShouldBeEmptyAsync());
    }

    #endregion

    #region ShouldNotBeEmptyAsync Tests

    [Fact]
    public async Task ShouldNotBeEmptyAsync_WhenNotEmpty_ReturnsResults()
    {
        // Arrange
        var stream = CreateSuccessStreamAsync(1, 2);

        // Act
        var results = await stream.ShouldNotBeEmptyAsync();

        // Assert
        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ShouldNotBeEmptyAsync_WhenEmpty_Throws()
    {
        // Arrange
        static async IAsyncEnumerable<Either<string, int>> EmptyStream()
        {
            await Task.Yield();
            yield break;
        }

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await EmptyStream().ShouldNotBeEmptyAsync());
    }

    #endregion

    #region EncinaError Specific Streaming Tests

    [Fact]
    public async Task ShouldContainValidationErrorForAsync_WhenPropertyInErrors_ReturnsError()
    {
        // Arrange
        var stream = CreateEncinaErrorStreamAsync();

        // Act
        var error = await stream.ShouldContainValidationErrorForAsync("Email");

        // Assert
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task ShouldNotContainAuthorizationErrorsAsync_WhenNoAuthErrors_Succeeds()
    {
        // Arrange
        var stream = CreateEncinaErrorStreamAsync();

        // Act & Assert (should not throw)
        await stream.ShouldNotContainAuthorizationErrorsAsync();
    }

    [Fact]
    public async Task ShouldNotContainAuthorizationErrorsAsync_WhenAuthErrorExists_Throws()
    {
        // Arrange
        static async IAsyncEnumerable<Either<EncinaError, int>> StreamWithAuthError()
        {
            await Task.Yield();
            yield return EncinaErrors.Create("encina.authorization.denied", "Access denied");
        }

        // Act & Assert
        await Should.ThrowAsync<Xunit.Sdk.FailException>(async () =>
            await StreamWithAuthError().ShouldNotContainAuthorizationErrorsAsync());
    }

    [Fact]
    public async Task ShouldContainAuthorizationErrorAsync_WhenAuthErrorExists_ReturnsError()
    {
        // Arrange
        static async IAsyncEnumerable<Either<EncinaError, int>> StreamWithAuthError()
        {
            await Task.Yield();
            yield return Right(1);
            await Task.Yield();
            yield return EncinaErrors.Create("encina.authorization.denied", "Access denied");
        }

        // Act
        var error = await StreamWithAuthError().ShouldContainAuthorizationErrorAsync();

        // Assert
        error.Message.ShouldBe("Access denied");
    }

    [Fact]
    public async Task ShouldAllHaveErrorCodeAsync_WhenAllMatchingCode_ReturnsErrors()
    {
        // Arrange
        static async IAsyncEnumerable<Either<EncinaError, int>> StreamWithSameCode()
        {
            await Task.Yield();
            yield return EncinaErrors.Create("test.code", "Error 1");
            await Task.Yield();
            yield return EncinaErrors.Create("test.code", "Error 2");
        }

        // Act
        var errors = await StreamWithSameCode().ShouldAllHaveErrorCodeAsync("test.code");

        // Assert
        errors.Count.ShouldBe(2);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ShouldAllBeSuccessAsync_WithCancellation_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        static async IAsyncEnumerable<Either<string, int>> InfiniteStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return Right(1);
            }
        }

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await InfiniteStream().ShouldAllBeSuccessAsync(cancellationToken: cts.Token));
    }

    #endregion
}
