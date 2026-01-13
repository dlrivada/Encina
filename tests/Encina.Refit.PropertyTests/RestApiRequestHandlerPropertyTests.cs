using System.Globalization;
using Encina.Refit;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;

namespace Encina.Refit.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// Uses FsCheck to verify invariants hold for all inputs.
/// </summary>
public class RestApiRequestHandlerPropertyTests
{
    [Property]
    public FsCheck.Property Property_HandleSuccess_AlwaysReturnsRight()
    {
        return Prop.ForAll<NonNull<string>>(data =>
        {
            // Arrange
            var apiClient = Substitute.For<ITestApiClient>();
            var logger = Substitute.For<ILogger<RestApiRequestHandler<SuccessRequest, ITestApiClient, string>>>();
            var handler = new RestApiRequestHandler<SuccessRequest, ITestApiClient, string>(apiClient, logger);
            var request = new SuccessRequest(data.Get);

            // Act
            var result = handler.Handle(request, CancellationToken.None).GetAwaiter().GetResult();

            // Assert
            return result.IsRight.ToProperty();
        });
    }

    [Property]
    public FsCheck.Property Property_HandleFailure_AlwaysReturnsLeft()
    {
        return Prop.ForAll<NonNull<string>>(errorMessage =>
        {
            // Arrange
            var apiClient = Substitute.For<ITestApiClient>();
            var logger = Substitute.For<ILogger<RestApiRequestHandler<FailureRequest, ITestApiClient, string>>>();
            var handler = new RestApiRequestHandler<FailureRequest, ITestApiClient, string>(apiClient, logger);
            var request = new FailureRequest(errorMessage.Get);

            // Act
            var result = handler.Handle(request, CancellationToken.None).GetAwaiter().GetResult();

            // Assert
            return result.IsLeft.ToProperty();
        });
    }

    [Property]
    public FsCheck.Property Property_ApiException_AlwaysContainsStatusCode()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(400, 599)), // HTTP error status codes
            (statusCode) =>
            {
                // Arrange
                var apiClient = Substitute.For<ITestApiClient>();
                var logger = Substitute.For<ILogger<RestApiRequestHandler<ApiExceptionRequest, ITestApiClient, string>>>();
                var handler = new RestApiRequestHandler<ApiExceptionRequest, ITestApiClient, string>(apiClient, logger);
                var request = new ApiExceptionRequest(statusCode);

                // Act
                var result = handler.Handle(request, CancellationToken.None).GetAwaiter().GetResult();

                // Assert
                return result.Match(
                    Left: error => error.Message.Contains(statusCode.ToString(CultureInfo.InvariantCulture)),
                    Right: _ => false
                ).ToProperty();
            });
    }

    [Property]
    public FsCheck.Property Property_SuccessResponse_MatchesInput()
    {
        return Prop.ForAll<NonNull<string>>(data =>
        {
            // Arrange
            var apiClient = Substitute.For<ITestApiClient>();
            var logger = Substitute.For<ILogger<RestApiRequestHandler<SuccessRequest, ITestApiClient, string>>>();
            var handler = new RestApiRequestHandler<SuccessRequest, ITestApiClient, string>(apiClient, logger);
            var request = new SuccessRequest(data.Get);

            // Act
            var result = handler.Handle(request, CancellationToken.None).GetAwaiter().GetResult();

            // Assert
            return result.Match(
                Left: _ => false,
                Right: response => response == data.Get
            ).ToProperty();
        });
    }

    [Property]
    public FsCheck.Property Property_ErrorMessage_NeverNull()
    {
        return Prop.ForAll<NonNull<string>>(errorMessage =>
        {
            // Arrange
            var apiClient = Substitute.For<ITestApiClient>();
            var logger = Substitute.For<ILogger<RestApiRequestHandler<FailureRequest, ITestApiClient, string>>>();
            var handler = new RestApiRequestHandler<FailureRequest, ITestApiClient, string>(apiClient, logger);
            var request = new FailureRequest(errorMessage.Get);

            // Act
            var result = handler.Handle(request, CancellationToken.None).GetAwaiter().GetResult();

            // Assert
            return result.Match(
                Left: error => !string.IsNullOrEmpty(error.Message),
                Right: _ => false
            ).ToProperty();
        });
    }

    [Property]
    public FsCheck.Property Property_Cancellation_AlwaysReturnsLeft()
    {
        return Prop.ForAll<NonNull<string>>(data =>
        {
            // Arrange
            var apiClient = Substitute.For<ITestApiClient>();
            var logger = Substitute.For<ILogger<RestApiRequestHandler<CancellationRequest, ITestApiClient, string>>>();
            var handler = new RestApiRequestHandler<CancellationRequest, ITestApiClient, string>(apiClient, logger);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var request = new CancellationRequest(cts.Token);

            // Act
            var result = handler.Handle(request, cts.Token).GetAwaiter().GetResult();

            // Assert
            return result.IsLeft.ToProperty();
        });
    }

    // Test helpers
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }

    public record SuccessRequest(string Data) : IRestApiRequest<ITestApiClient, string>
    {
        public Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            return Task.FromResult(Data);
        }
    }

    public record FailureRequest(string ErrorMessage) : IRestApiRequest<ITestApiClient, string>
    {
        public Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(ErrorMessage);
        }
    }

    public record ApiExceptionRequest(int StatusCode) : IRestApiRequest<ITestApiClient, string>
    {
        public async Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            var statusCode = (System.Net.HttpStatusCode)StatusCode;
            throw await ApiException.Create(
                new HttpRequestMessage(HttpMethod.Get, "http://test.com"),
                HttpMethod.Get,
                new HttpResponseMessage(statusCode),
                new RefitSettings());
        }
    }

    public record CancellationRequest(CancellationToken TokenToThrow) : IRestApiRequest<ITestApiClient, string>
    {
        public Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            throw new TaskCanceledException("Cancelled", null, TokenToThrow);
        }
    }
}
