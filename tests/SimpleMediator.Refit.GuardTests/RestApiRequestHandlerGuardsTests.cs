using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SimpleMediator.Refit;

namespace SimpleMediator.Refit.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// </summary>
public class RestApiRequestHandlerGuardsTests
{
    [Fact]
    public void Constructor_NullApiClient_ThrowsArgumentNullException()
    {
        // Arrange
        ITestApiClient? nullApiClient = null;
        var logger = Substitute.For<ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, string>>>();

        // Act
        Action act = () => _ = new RestApiRequestHandler<TestRequest, ITestApiClient, string>(nullApiClient!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var apiClient = Substitute.For<ITestApiClient>();
        ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, string>>? nullLogger = null;

        // Act
        Action act = () => _ = new RestApiRequestHandler<TestRequest, ITestApiClient, string>(apiClient, nullLogger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_BothParametersNull_ThrowsArgumentNullException()
    {
        // Arrange
        ITestApiClient? nullApiClient = null;
        ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, string>>? nullLogger = null;

        // Act
        Action act = () => _ = new RestApiRequestHandler<TestRequest, ITestApiClient, string>(nullApiClient!, nullLogger!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // Test helpers
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }

    public class TestRequest : IRestApiRequest<ITestApiClient, string>
    {
        public Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            return Task.FromResult("test");
        }
    }
}
