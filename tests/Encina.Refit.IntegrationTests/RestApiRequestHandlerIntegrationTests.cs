using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using Encina.Refit;

namespace Encina.Refit.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// Tests real HTTP calls using public APIs.
/// </summary>
[Trait("Category", "Integration")]
public class RestApiRequestHandlerIntegrationTests
{
    [Fact]
    public async Task EndToEnd_RealApiCall_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IJsonPlaceholderApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<RestApiRequestHandler<GetPostRequest, IJsonPlaceholderApi, Post>>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<RestApiRequestHandler<GetPostRequest, IJsonPlaceholderApi, Post>>();
        var request = new GetPostRequest(1);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(post =>
        {
            post.Id.Should().Be(1);
            post.Title.Should().NotBeNullOrEmpty();
            post.Body.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task EndToEnd_404NotFound_ShouldReturnEncinaError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IJsonPlaceholderApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<RestApiRequestHandler<GetPostRequest, IJsonPlaceholderApi, Post>>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<RestApiRequestHandler<GetPostRequest, IJsonPlaceholderApi, Post>>();
        var request = new GetPostRequest(999999); // Non-existent post

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("404");
        });
    }

    [Fact]
    public async Task EndToEnd_500InternalServerError_ShouldReturnEncinaError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IHttpBinApi>(client =>
        {
            client.BaseAddress = new Uri("https://httpbin.org");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<RestApiRequestHandler<GetStatusCodeRequest, IHttpBinApi, string>>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<RestApiRequestHandler<GetStatusCodeRequest, IHttpBinApi, string>>();
        var request = new GetStatusCodeRequest(500);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("500");
        });
    }

    [Fact]
    public async Task EndToEnd_NetworkError_ShouldReturnEncinaError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IInvalidApi>(client =>
        {
            client.BaseAddress = new Uri("https://this-domain-does-not-exist-12345.com");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<RestApiRequestHandler<GetDataRequest, IInvalidApi, string>>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<RestApiRequestHandler<GetDataRequest, IInvalidApi, string>>();
        var request = new GetDataRequest();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("request failed");
        });
    }

    [Fact]
    public async Task EndToEnd_Timeout_ShouldReturnTimeoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<IHttpBinApi>(client =>
        {
            client.BaseAddress = new Uri("https://httpbin.org");
            client.Timeout = TimeSpan.FromMilliseconds(100); // Very short timeout
        });
        services.AddSingleton<RestApiRequestHandler<GetDelayRequest, IHttpBinApi, string>>();

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<RestApiRequestHandler<GetDelayRequest, IHttpBinApi, string>>();
        var request = new GetDelayRequest(5); // 5 second delay

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("timed out");
        });
    }

    // Test APIs
    public interface IJsonPlaceholderApi
    {
        [Get("/posts/{id}")]
        Task<Post> GetPostAsync(int id);
    }

    public interface IHttpBinApi
    {
        [Get("/status/{code}")]
        Task<string> GetStatusCodeAsync(int code);

        [Get("/delay/{seconds}")]
        Task<string> GetDelayAsync(int seconds);
    }

    public interface IInvalidApi
    {
        [Get("/data")]
        Task<string> GetDataAsync();
    }

    // Test models
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    // Test requests
    public record GetPostRequest(int Id) : IRestApiRequest<IJsonPlaceholderApi, Post>
    {
        public async Task<Post> ExecuteAsync(IJsonPlaceholderApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetPostAsync(Id);
        }
    }

    public record GetStatusCodeRequest(int StatusCode) : IRestApiRequest<IHttpBinApi, string>
    {
        public async Task<string> ExecuteAsync(IHttpBinApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetStatusCodeAsync(StatusCode);
        }
    }

    public record GetDelayRequest(int Seconds) : IRestApiRequest<IHttpBinApi, string>
    {
        public async Task<string> ExecuteAsync(IHttpBinApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetDelayAsync(Seconds);
        }
    }

    public record GetDataRequest : IRestApiRequest<IInvalidApi, string>
    {
        public async Task<string> ExecuteAsync(IInvalidApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetDataAsync();
        }
    }
}
