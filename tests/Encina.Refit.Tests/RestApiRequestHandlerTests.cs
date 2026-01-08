using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Refit;
using Encina.Refit;

namespace Encina.Refit.Tests;

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
        result.ShouldBeSuccess();
        result.IfRight(response => response.Data.ShouldBe(expectedResponse.Data));
    }

    [Fact]
    public async Task Handle_ApiException_ShouldReturnEncinaError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowApiException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("API call failed with status");
            // Error message uses enum name (NotFound), not numeric code (404)
            error.Message.ShouldContain("NotFound");
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
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Error content");
        });
    }

    [Fact]
    public async Task Handle_HttpRequestException_ShouldReturnEncinaError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowHttpRequestException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("HTTP request failed");
            error.Message.ShouldContain("Network error");
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
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("API request was cancelled");
        });
    }

    [Fact]
    public async Task Handle_TaskCanceledException_Timeout_ShouldReturnTimeoutError()
    {
        // Arrange
        // Create a different cancellation token than the one passed to the handler
        // to simulate a timeout (not user-initiated cancellation)
        using var differentCts = new CancellationTokenSource();
        var request = new TestRequest
        {
            ShouldThrowTaskCanceledException = true,
            CancellationTokenToThrow = differentCts.Token
        };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("API request timed out");
        });
    }

    [Fact]
    public async Task Handle_UnexpectedException_ShouldReturnEncinaError()
    {
        // Arrange
        var request = new TestRequest { ShouldThrowUnexpectedException = true };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            // error.Exception is Option<Exception>, need to use IsSome and check the inner value
            error.Exception.IsSome.ShouldBeTrue();
            error.Exception.IfSome(ex => ex.ShouldBeOfType<InvalidOperationException>());
        });
    }

    [Fact]
    public void Constructor_ShouldAcceptApiClientAndLogger()
    {
        // Act
        var handler = new RestApiRequestHandler<TestRequest, ITestApiClient, TestResponse>(_mockApiClient, _mockLogger);

        // Assert
        handler.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldLogExecutingApiCall()
    {
        // Arrange
        // Source-generated loggers check IsEnabled before logging
        // We need to return true for the log level being checked
        _mockLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(request, cancellationToken);

        // Assert - Source-generated loggers use ILogger.Log<TState>
        // Verify IsEnabled was checked for Debug level (EventId 1 = Executing API call)
        _mockLogger.Received().IsEnabled(LogLevel.Debug);
    }

    [Fact]
    public async Task Handle_Success_ShouldLogApiCallSucceeded()
    {
        // Arrange
        // Source-generated loggers check IsEnabled before logging
        _mockLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.Handle(request, cancellationToken);

        // Assert - Verify Debug logging was checked (success path logs at Debug level)
        // After a successful call, both "Executing" and "Succeeded" logs are at Debug level
        _mockLogger.Received(2).IsEnabled(LogLevel.Debug);
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
