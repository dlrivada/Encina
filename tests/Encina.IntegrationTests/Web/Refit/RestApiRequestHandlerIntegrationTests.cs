using Encina.Refit;
using Encina.Testing.WireMock;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Encina.IntegrationTests.Web.Refit;

/// <summary>
/// Integration tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>
/// using WireMock for HTTP mocking instead of real external APIs.
/// </summary>
/// <remarks>
/// This test class demonstrates how to use <see cref="EncinaRefitMockFixture{TApiClient}"/>
/// for reliable, deterministic integration testing without external dependencies.
/// </remarks>
[Trait("Category", "Integration")]
public class RestApiRequestHandlerIntegrationTests : IClassFixture<EncinaRefitMockFixture<ITestPostApi>>, IAsyncLifetime
{
    private readonly EncinaRefitMockFixture<ITestPostApi> _fixture;
    private IServiceProvider _serviceProvider = null!;
    private RestApiRequestHandler<GetPostRequest, ITestPostApi, Post> _handler = null!;

    public RestApiRequestHandlerIntegrationTests(EncinaRefitMockFixture<ITestPostApi> fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.Reset();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<ITestPostApi>(client =>
        {
            client.BaseAddress = new Uri(_fixture.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<RestApiRequestHandler<GetPostRequest, ITestPostApi, Post>>();

        _serviceProvider = services.BuildServiceProvider();
        _handler = _serviceProvider.GetRequiredService<RestApiRequestHandler<GetPostRequest, ITestPostApi, Post>>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task Handle_SuccessfulApiCall_ShouldReturnRight()
    {
        // Arrange
        var expectedPost = new Post
        {
            Id = 1,
            UserId = 1,
            Title = "Test Post Title",
            Body = "This is the body of the test post"
        };
        _fixture.StubGet("/posts/1", expectedPost);

        var request = new GetPostRequest(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.IfRight(post =>
        {
            post.Id.ShouldBe(1);
            post.Title.ShouldBe("Test Post Title");
            post.Body.ShouldBe("This is the body of the test post");
        });

        _fixture.VerifyCallMade("/posts/1", times: 1, method: "GET");
    }

    [Fact]
    public async Task Handle_404NotFound_ShouldReturnEncinaError()
    {
        // Arrange
        _fixture.StubError("/posts/999999", statusCode: 404, errorResponse: new { error = "Post not found" }, method: "GET");

        var request = new GetPostRequest(999999);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            // Error message contains the HTTP status name (NotFound) rather than numeric code
            error.Message.ShouldContain("NotFound");
        });

        _fixture.VerifyCallMade("/posts/999999", times: 1, method: "GET");
    }

    [Fact]
    public async Task Handle_500InternalServerError_ShouldReturnEncinaError()
    {
        // Arrange
        _fixture.StubError("/posts/1", statusCode: 500, errorResponse: new { error = "Internal server error" }, method: "GET");

        var request = new GetPostRequest(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            // Error message contains the HTTP status name (InternalServerError) rather than numeric code
            error.Message.ShouldContain("InternalServerError");
        });
    }

    [Fact]
    public async Task Handle_Timeout_ShouldReturnTimeoutError()
    {
        // Arrange - Stub a response with delay longer than client timeout
        var post = new Post { Id = 1, Title = "Delayed", Body = "Body" };
        _fixture.StubDelay("/posts/1", TimeSpan.FromSeconds(10), post, method: "GET");

        var request = new GetPostRequest(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            // Timeout can manifest as "timed out" or "cancelled" depending on the scenario
            (error.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
             error.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase))
                .ShouldBeTrue($"Expected timeout or cancellation message, but got: {error.Message}");
        });
    }

    [Fact]
    public async Task Handle_ConcurrentRequests_ShouldAllSucceed()
    {
        // Arrange
        _fixture.StubGet("/posts/1", new Post { Id = 1, Title = "Post 1", Body = "Body 1" });
        _fixture.StubGet("/posts/2", new Post { Id = 2, Title = "Post 2", Body = "Body 2" });
        _fixture.StubGet("/posts/3", new Post { Id = 3, Title = "Post 3", Body = "Body 3" });

        // Act
        var results = await Task.WhenAll(
            _handler.Handle(new GetPostRequest(1), CancellationToken.None),
            _handler.Handle(new GetPostRequest(2), CancellationToken.None),
            _handler.Handle(new GetPostRequest(3), CancellationToken.None)
        );

        // Assert
        results.Length.ShouldBe(3);
        results.AllShouldBeSuccess();

        results[0].IfRight(post => post.Id.ShouldBe(1));
        results[1].IfRight(post => post.Id.ShouldBe(2));
        results[2].IfRight(post => post.Id.ShouldBe(3));

        _fixture.VerifyCallMade("/posts/1", times: 1, method: "GET");
        _fixture.VerifyCallMade("/posts/2", times: 1, method: "GET");
        _fixture.VerifyCallMade("/posts/3", times: 1, method: "GET");
    }

    [Fact]
    public async Task Handle_EmptyResponseBody_ShouldHandleGracefully()
    {
        // Arrange - Stub returns 204 No Content (empty body is expected)
        _fixture.Stub("GET", "/posts/1", statusCode: 204);

        var request = new GetPostRequest(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - 204 No Content is a valid success response
        // The handler should handle empty body gracefully
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_MalformedJson_ShouldReturnError()
    {
        // Arrange - We need to use the underlying Server for malformed responses
        _fixture.Server.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/posts/1")
                .UsingMethod("GET"))
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ invalid json }"));

        var request = new GetPostRequest(1);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.ShouldBeError();
    }

}

// Test API interface for WireMock (must be at namespace level for IClassFixture)
public interface ITestPostApi
{
    [Get("/posts/{id}")]
    Task<Post> GetPostAsync(int id);
}

// Test models
public sealed class Post
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

// Test request
public sealed record GetPostRequest(int Id) : IRestApiRequest<ITestPostApi, Post>
{
    public async Task<Post> ExecuteAsync(ITestPostApi apiClient, CancellationToken cancellationToken)
    {
        return await apiClient.GetPostAsync(Id);
    }
}
