using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.ContractTests;

/// <summary>
/// Contract tests for HangfireRequestJobAdapter.
/// Verifies that the adapter correctly implements its contract.
/// </summary>
public sealed class HangfireRequestJobAdapterContractTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldInvokeEncinaSend()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(Encina, logger);
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("result");

        Encina.Send(request, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        var result = await adapter.ExecuteAsync(request);

        // Assert
        await Encina.Received(1).Send(request, Arg.Any<CancellationToken>());

        result.Match(
            Left: _ => Assert.Fail("Expected Right but got Left"),
            Right: actual => actual.Should().Be(expectedResponse));
    }

    [Fact]
    public async Task ExecuteAsync_WhenEncinaReturnsError_ShouldReturnError()
    {
        // Arrange
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        var adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(Encina, logger);
        var request = new TestRequest("test");
        var expectedError = EncinaErrors.Create("test.error", "Test error");

        Encina.Send(request, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(expectedError));

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
