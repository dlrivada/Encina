using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Refit;
using SimpleMediator.Refit;

namespace SimpleMediator.Refit.Tests;

/// <summary>
/// Unit tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// </summary>
public class RestApiRequestHandlerTests
{
    private readonly ITestApiClient _mockApiClient;
    private readonly ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse>> _mockLogger;
    private readonly RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse> _handler;

    public RestApiRequestHandlerTests()
    {
        _mockApiClient = Substitute.For<ITestApiClient>();
        _mockLogger = Substitute.For<ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse>>>();
        _handler = new RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse>(_mockApiClient, _mockLogger);
    }

    [Fact]
    public async Task Handle_SuccessfulApiCall_ShouldReturnRight()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse { Data = "Success" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(response => response.Should().Be(expectedResponse));
    }

    [Fact]
    public async Task Handle_ApiException_ShouldReturnMediatorError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowApiException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("API call failed with status");
            error.Message.Should().Contain("404");
        });
    }

    [Fact]
    public async Task Handle_ApiException_WithContent_ShouldIncludeContent()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowApiException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("Error content");
        });
    }

    [Fact]
    public async Task Handle_HttpRequestException_ShouldReturnMediatorError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowHttpRequestException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("HTTP request failed");
            error.Message.Should().Contain("Network error");
        });
    }

    [Fact]
    public async Task Handle_TaskCanceledException_ByCaller_ShouldReturnCancellationError()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var request = new TestRequest { ShouldThrowTaskCanceledException = true, CancellationTokenToThrow = cts.Token };
        cts.Cancel();

        // Act
        var result = await _handler.Handle(request, cts.Token);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("API request was cancelled");
        });
    }

    [Fact]
    public async Task Handle_TaskCanceledException_Timeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowTaskCanceledException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("API request timed out");
        });
    }

    [Fact]
    public async Task Handle_UnexpectedException_ShouldReturnMediatorError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowUnexpectedException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            ((object?)error.Exception).Should().NotBeNull();
            ((object?)error.Exception).Should().BeOfType<InvalidOperationException>();
        });
    }

    [Fact]
    public void Constructor_ShouldAcceptApiClientAndLogger()
    {
        // Act
        var handler = new RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse>(_mockApiClient, _mockLogger);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldLogExecutingApiCall()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(request, cancellationToken);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Executing API call")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_Success_ShouldLogApiCallSucceeded()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(request, cancellationToken);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("succeeded")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // Test helpers
    public interface ITestApiClient
    {
        Task<TestResponse> GetDataAsync(CancellationToken cancellationToken);
    }

    public class TestResponse
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestRequest : IRestApiRequest<ITestApiClient, TestResponse>
    {
        public bool ShouldThrowApiException { get; set; }
        public bool ShouldThrowHttpRequestException { get; set; }
        public bool ShouldThrowTaskCanceledException { get; set; }
        public bool ShouldThrowUnexpectedException { get; set; }
        public CancellationToken CancellationTokenToThrow { get; set; }

        public async Task<TestResponse> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            if (ShouldThrowApiException)
            {
                throw await ApiException.Create(
                    new HttpRequestMessage(HttpMethod.Get, "http://test.com"),
                    HttpMethod.Get,
                    new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("Error content")
                    },
                    new RefitSettings());
            }

            if (ShouldThrowHttpRequestException)
            {
                throw new HttpRequestException("Network error");
            }

            if (ShouldThrowTaskCanceledException)
            {
                throw new TaskCanceledException("Cancelled", null, CancellationTokenToThrow);
            }

            if (ShouldThrowUnexpectedException)
            {
                throw new InvalidOperationException("Unexpected error");
            }

            return await Task.FromResult(new TestResponse { Data = "Success" });
        }
    }
}
