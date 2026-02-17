using System.Runtime.CompilerServices;
using Encina.Testing.Shouldly;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Testing.Shouldly;

/// <summary>
/// Unit tests for <see cref="StreamingShouldlyExtensions"/>.
/// </summary>
public sealed class StreamingShouldlyExtensionsTests
{
    #region ShouldContainSuccessStreamingAsync Tests

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WhenFirstItemIsSuccess_ReturnsImmediately()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Right(42));

        // Act
        var result = await stream.ShouldContainSuccessStreamingAsync();

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WhenSuccessAfterErrors_ReturnsFirstSuccess()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Left("err1"),
            Either<string, int>.Left("err2"),
            Either<string, int>.Right(99),
            Either<string, int>.Right(100));

        // Act
        var result = await stream.ShouldContainSuccessStreamingAsync();

        // Assert
        result.ShouldBe(99);
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WhenNoSuccess_Throws()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Left("err1"),
            Either<string, int>.Left("err2"));

        // Act & Assert
        await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainSuccessStreamingAsync());
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WhenEmptyStream_Throws()
    {
        // Arrange
        var stream = CreateStream<string, int>();

        // Act & Assert
        await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainSuccessStreamingAsync());
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WithCustomMessage_UsesMessage()
    {
        // Arrange
        var stream = CreateStream(Either<string, int>.Left("err"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainSuccessStreamingAsync("custom message"));
        ex.Message.ShouldContain("custom message");
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WithInfiniteStream_StopsAtFirstSuccess()
    {
        // Arrange - infinite stream that yields errors then one success
        var itemsConsumed = 0;
        var stream = InfiniteStreamWithSuccessAtPosition<string, int>(
            successPosition: 5,
            successValue: 42,
            errorFactory: i => $"error-{i}",
            onConsume: () => itemsConsumed++);

        // Act
        var result = await stream.ShouldContainSuccessStreamingAsync();

        // Assert
        result.ShouldBe(42);
        itemsConsumed.ShouldBe(6); // 5 errors + 1 success
    }

    [Fact]
    public async Task ShouldContainSuccessStreamingAsync_WithCancellation_Cancels()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var stream = InfiniteErrorStream<string, int>(i => $"error-{i}");

        // Cancel after short delay
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stream.ShouldContainSuccessStreamingAsync(cancellationToken: cts.Token));
    }

    #endregion

    #region ShouldContainErrorStreamingAsync Tests

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WhenFirstItemIsError_ReturnsImmediately()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Left("fail"));

        // Act
        var result = await stream.ShouldContainErrorStreamingAsync();

        // Assert
        result.ShouldBe("fail");
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WhenErrorAfterSuccesses_ReturnsFirstError()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Right(1),
            Either<string, int>.Right(2),
            Either<string, int>.Left("found-it"),
            Either<string, int>.Left("not-this"));

        // Act
        var result = await stream.ShouldContainErrorStreamingAsync();

        // Assert
        result.ShouldBe("found-it");
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WhenNoError_Throws()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Right(1),
            Either<string, int>.Right(2));

        // Act & Assert
        await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainErrorStreamingAsync());
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WhenEmptyStream_Throws()
    {
        // Arrange
        var stream = CreateStream<string, int>();

        // Act & Assert
        await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainErrorStreamingAsync());
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WithCustomMessage_UsesMessage()
    {
        // Arrange
        var stream = CreateStream(Either<string, int>.Right(1));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ShouldAssertException>(
            async () => await stream.ShouldContainErrorStreamingAsync("custom error msg"));
        ex.Message.ShouldContain("custom error msg");
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WithInfiniteStream_StopsAtFirstError()
    {
        // Arrange
        var itemsConsumed = 0;
        var stream = InfiniteStreamWithErrorAtPosition<string, int>(
            errorPosition: 3,
            errorValue: "found",
            successFactory: i => i * 10,
            onConsume: () => itemsConsumed++);

        // Act
        var result = await stream.ShouldContainErrorStreamingAsync();

        // Assert
        result.ShouldBe("found");
        itemsConsumed.ShouldBe(4); // 3 successes + 1 error
    }

    [Fact]
    public async Task ShouldContainErrorStreamingAsync_WithCancellation_Cancels()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var stream = InfiniteSuccessStream<string, int>(i => i);

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stream.ShouldContainErrorStreamingAsync(cancellationToken: cts.Token));
    }

    #endregion

    #region FirstOrDefaultSuccessAsync Tests

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WhenSuccessExists_ReturnsIt()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Left("err"),
            Either<string, int>.Right(77));

        // Act
        var result = await stream.FirstOrDefaultSuccessAsync();

        // Assert
        result.ShouldBe(77);
    }

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WhenNoSuccess_ReturnsDefault()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, int>.Left("err1"),
            Either<string, int>.Left("err2"));

        // Act
        var result = await stream.FirstOrDefaultSuccessAsync();

        // Assert
        result.ShouldBe(default);
    }

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WhenEmpty_ReturnsDefault()
    {
        // Arrange
        var stream = CreateStream<string, int>();

        // Act
        var result = await stream.FirstOrDefaultSuccessAsync();

        // Assert
        result.ShouldBe(default);
    }

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WithReferenceType_ReturnsNull()
    {
        // Arrange
        var stream = CreateStream(
            Either<string, string>.Left("err"));

        // Act
        var result = await stream.FirstOrDefaultSuccessAsync();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WithInfiniteStream_StopsAtFirstSuccess()
    {
        // Arrange
        var itemsConsumed = 0;
        var stream = InfiniteStreamWithSuccessAtPosition<string, int>(
            successPosition: 10,
            successValue: 999,
            errorFactory: i => $"error-{i}",
            onConsume: () => itemsConsumed++);

        // Act
        var result = await stream.FirstOrDefaultSuccessAsync();

        // Assert
        result.ShouldBe(999);
        itemsConsumed.ShouldBe(11); // 10 errors + 1 success
    }

    [Fact]
    public async Task FirstOrDefaultSuccessAsync_WithCancellation_Cancels()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var stream = InfiniteErrorStream<string, int>(i => $"error-{i}");

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stream.FirstOrDefaultSuccessAsync(cts.Token));
    }

    #endregion

    #region Helpers

    private static async IAsyncEnumerable<Either<TLeft, TRight>> CreateStream<TLeft, TRight>(
        params Either<TLeft, TRight>[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Either<TLeft, TRight>> InfiniteStreamWithSuccessAtPosition<TLeft, TRight>(
        int successPosition,
        TRight successValue,
        Func<int, TLeft> errorFactory,
        Action? onConsume = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; ; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onConsume?.Invoke();

            if (i == successPosition)
            {
                yield return Either<TLeft, TRight>.Right(successValue);
                yield break;
            }

            yield return Either<TLeft, TRight>.Left(errorFactory(i));
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Either<TLeft, TRight>> InfiniteStreamWithErrorAtPosition<TLeft, TRight>(
        int errorPosition,
        TLeft errorValue,
        Func<int, TRight> successFactory,
        Action? onConsume = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; ; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onConsume?.Invoke();

            if (i == errorPosition)
            {
                yield return Either<TLeft, TRight>.Left(errorValue);
                yield break;
            }

            yield return Either<TLeft, TRight>.Right(successFactory(i));
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Either<TLeft, TRight>> InfiniteErrorStream<TLeft, TRight>(
        Func<int, TLeft> errorFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; ; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Either<TLeft, TRight>.Left(errorFactory(i));
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Either<TLeft, TRight>> InfiniteSuccessStream<TLeft, TRight>(
        Func<int, TRight> successFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; ; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Either<TLeft, TRight>.Right(successFactory(i));
            await Task.Yield();
        }
    }

    #endregion
}
