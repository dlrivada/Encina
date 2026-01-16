using System.Net.Http.Json;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Encina.Refit;
using Encina.Testing.WireMock;

namespace Encina.IntegrationTests.Web.Refit;

/// <summary>
/// Integration tests for Refit client integration with WireMock for HTTP mocking.
/// Tests end-to-end scenarios with mocked HTTP responses for reliable, deterministic testing.
/// </summary>
/// <remarks>
/// This test class demonstrates how to use <see cref="EncinaWireMockFixture"/>
/// for testing Refit clients directly with WireMock.
/// </remarks>
[Trait("Category", "Integration")]
public class RefitClientIntegrationTests : IClassFixture<EncinaWireMockFixture>, IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture;
    private IServiceProvider _serviceProvider = null!;
    private ITodoApi _todoApi = null!;
    private IHttpHeadersApi _headersApi = null!;

    public RefitClientIntegrationTests(EncinaWireMockFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.Reset();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri(_fixture.BaseUrl);
        });
        services.AddEncinaRefitClient<IHttpHeadersApi>(client =>
        {
            client.BaseAddress = new Uri(_fixture.BaseUrl);
            client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
        });

        _serviceProvider = services.BuildServiceProvider();
        _todoApi = _serviceProvider.GetRequiredService<ITodoApi>();
        _headersApi = _serviceProvider.GetRequiredService<IHttpHeadersApi>();

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
    public async Task RefitClient_GetRequest_ShouldResolveAndExecute()
    {
        // Arrange
        var expectedTodo = new Todo { Id = 1, Title = "Test Todo", Completed = false, UserId = 1 };
        _fixture.StubGet("/todos/1", expectedTodo);

        // Act
        var result = await _todoApi.GetTodoAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Title.ShouldBe("Test Todo");
        result.Completed.ShouldBeFalse();

        _fixture.VerifyCallMade("/todos/1", times: 1, method: "GET");
    }

    [Fact]
    public async Task RefitClient_MultipleRequests_ShouldAllSucceed()
    {
        // Arrange
        _fixture.StubGet("/todos/1", new Todo { Id = 1, Title = "Todo 1", Completed = false, UserId = 1 });
        _fixture.StubGet("/todos/2", new Todo { Id = 2, Title = "Todo 2", Completed = true, UserId = 1 });
        _fixture.StubGet("/todos/3", new Todo { Id = 3, Title = "Todo 3", Completed = false, UserId = 2 });

        // Act
        var results = await Task.WhenAll(
            _todoApi.GetTodoAsync(1),
            _todoApi.GetTodoAsync(2),
            _todoApi.GetTodoAsync(3)
        );

        // Assert
        results.Length.ShouldBe(3);
        results[0].Id.ShouldBe(1);
        results[1].Id.ShouldBe(2);
        results[2].Id.ShouldBe(3);

        _fixture.VerifyCallMade("/todos/1", times: 1, method: "GET");
        _fixture.VerifyCallMade("/todos/2", times: 1, method: "GET");
        _fixture.VerifyCallMade("/todos/3", times: 1, method: "GET");
    }

    [Fact]
    public async Task RefitClient_WithCustomHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var expectedResponse = new HeadersResponse
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "TestValue"
            }
        };
        _fixture.StubGet("/headers", expectedResponse);

        // Act
        var result = await _headersApi.GetHeadersAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Headers.ShouldContainKey("X-Custom-Header");
        result.Headers["X-Custom-Header"].ShouldBe("TestValue");

        _fixture.VerifyCallMade("/headers", times: 1, method: "GET");
    }

    [Fact]
    public async Task RefitClient_PostRequest_ShouldSendData()
    {
        // Arrange
        var createdTodo = new Todo { Id = 201, Title = "New Todo", Completed = false, UserId = 1 };
        _fixture.StubPost("/todos", createdTodo, statusCode: 201);

        var newTodo = new Todo { Title = "New Todo", Completed = false, UserId = 1 };

        // Act
        var result = await _todoApi.CreateTodoAsync(newTodo);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Todo");
        result.Completed.ShouldBeFalse();

        _fixture.VerifyCallMade("/todos", times: 1, method: "POST");
    }

    [Fact]
    public async Task RefitClient_DeleteRequest_ShouldSucceed()
    {
        // Arrange
        _fixture.StubDelete("/todos/1");

        // Act & Assert
        await Should.NotThrowAsync(async () => await _todoApi.DeleteTodoAsync(1));

        _fixture.VerifyCallMade("/todos/1", times: 1, method: "DELETE");
    }

    [Fact]
    public async Task RefitClient_PutRequest_ShouldUpdateData()
    {
        // Arrange
        var updatedTodo = new Todo { Id = 1, Title = "Updated Todo", Completed = true, UserId = 1 };
        _fixture.StubPut("/todos/1", updatedTodo);

        var todoUpdate = new Todo { Id = 1, Title = "Updated Todo", Completed = true, UserId = 1 };

        // Act
        var result = await _todoApi.UpdateTodoAsync(1, todoUpdate);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Updated Todo");
        result.Completed.ShouldBeTrue();

        _fixture.VerifyCallMade("/todos/1", times: 1, method: "PUT");
    }

    [Fact]
    public async Task RefitClient_ServerError_ShouldThrowApiException()
    {
        // Arrange
        _fixture.Server.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/todos/1")
                .UsingMethod("GET"))
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(503)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"error\": \"Service unavailable\"}"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(async () =>
        {
            await _todoApi.GetTodoAsync(1);
        });

        exception.StatusCode.ShouldBe(System.Net.HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task RefitClient_NotFound_ShouldThrowApiException()
    {
        // Arrange
        _fixture.Server.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/todos/999")
                .UsingMethod("GET"))
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(404)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"error\": \"Todo not found\"}"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(async () =>
        {
            await _todoApi.GetTodoAsync(999);
        });

        exception.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodoSequence_WhenFirstResponseSucceeds_ReturnsTodo()
    {
        // Arrange
        const string scenario = "first-success";
        ConfigureTodoSequenceStub(scenario);

        // Act
        var result = await _todoApi.GetTodoSequenceAsync(scenario);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Title.ShouldBe("First Call");
    }

    [Fact]
    public async Task GetTodoSequence_WhenSecondResponseFails_ThrowsRateLimitException()
    {
        // Arrange
        const string scenario = "second-ratelimit";
        ConfigureTodoSequenceStub(scenario);
        _ = await _todoApi.GetTodoSequenceAsync(scenario); // Consume first response to advance stub sequence

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(async () =>
        {
            await _todoApi.GetTodoSequenceAsync(scenario);
        });

        exception.StatusCode.ShouldBe(System.Net.HttpStatusCode.TooManyRequests);
    }

    private void ConfigureTodoSequenceStub(string scenario)
    {
        _fixture.StubSequence("GET", $"/todos/sequence/{scenario}",
            (new Todo { Id = 1, Title = "First Call", Completed = false, UserId = 1 }, 200),
            (new { error = "Rate limited" }, 429));
    }

    // Test APIs
    public interface ITodoApi
    {
        [Get("/todos/{id}")]
        Task<Todo> GetTodoAsync(int id);

        [Get("/todos/sequence/{scenario}")]
        Task<Todo> GetTodoSequenceAsync(string scenario);

        [Post("/todos")]
        Task<Todo> CreateTodoAsync([Body] Todo todo);

        [Put("/todos/{id}")]
        Task<Todo> UpdateTodoAsync(int id, [Body] Todo todo);

        [Delete("/todos/{id}")]
        Task DeleteTodoAsync(int id);
    }

    public interface IHttpHeadersApi
    {
        [Get("/headers")]
        Task<HeadersResponse> GetHeadersAsync();
    }

    // Test models
    public sealed class Todo
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool Completed { get; init; }
        public int UserId { get; init; }
    }

    public sealed class HeadersResponse
    {
        public Dictionary<string, string> Headers { get; init; } = new();
    }
}
