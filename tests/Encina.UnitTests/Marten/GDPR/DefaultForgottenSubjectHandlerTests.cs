using Encina.Marten.GDPR;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class DefaultForgottenSubjectHandlerTests
{
    private readonly DefaultForgottenSubjectHandler _sut =
        new(NullLogger<DefaultForgottenSubjectHandler>.Instance);

    [Fact]
    public async Task HandleForgottenSubjectAsync_CompletesWithoutError()
    {
        // Act & Assert — should not throw
        await _sut.HandleForgottenSubjectAsync(
            "user-42",
            "Email",
            typeof(string));
    }

    [Fact]
    public async Task HandleForgottenSubjectAsync_ReturnsCompletedTask()
    {
        // Act
        var task = _sut.HandleForgottenSubjectAsync(
            "user-42",
            "Email",
            typeof(string));

        // Assert
        task.IsCompleted.ShouldBeTrue();
        await task; // Ensure no exception
    }

    [Fact]
    public async Task HandleForgottenSubjectAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert — should not throw
        await _sut.HandleForgottenSubjectAsync(
            "user-42",
            "Name",
            typeof(object),
            cts.Token);
    }
}
