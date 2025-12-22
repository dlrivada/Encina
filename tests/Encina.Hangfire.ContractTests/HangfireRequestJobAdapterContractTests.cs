using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.Hangfire;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.ContractTests;

/// <summary>
/// Contract tests for HangfireRequestJobAdapter.
/// Verifies that the adapter correctly implements its contract.
/// </summary>
public sealed class HangfireRequestJobAdapterContractTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldInvokeMediatorSend()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(mediator, logger);
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("result");

        mediator.Send(request, Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(expectedResponse));

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        await mediator.Received(1).Send(request, Arg.Any<CancellationToken>());

        result.Match(
            Left: _ => Assert.Fail("Expected Right but got Left"),
            Right: actual => actual.Should().Be(expectedResponse));
    }

    [Fact]
    public async Task ExecuteAsync_WhenMediatorReturnsError_ShouldReturnError()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(mediator, logger);
        var request = new TestRequest("test");
        var expectedError = MediatorErrors.Create("test.error", "Test error");

        mediator.Send(request, Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, TestResponse>(expectedError));

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        result.Match(
            Left: actual => actual.Should().Be(expectedError),
            Right: _ => Assert.Fail("Expected Left but got Right"));
    }
}

// Test types (must be public for NSubstitute proxying with strong-named assemblies)
public sealed record TestRequest(string Data) : IRequest<TestResponse>;
public sealed record TestResponse(string Result);
