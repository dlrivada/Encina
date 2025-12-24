using Microsoft.Extensions.DependencyInjection;
using Refit;
using Encina.Refit;

namespace Encina.Refit.IntegrationTests;

/// <summary>
/// Integration tests for Refit client integration with Encina.
/// Tests end-to-end scenarios with real HTTP calls.
/// </summary>
[Trait("Category", "Integration")]
public class RefitClientIntegrationTests
{
    [Fact]
    public async Task EndToEnd_WithEncina_ShouldResolveClientAndExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new GetTodoRequest(1);

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess();
        result.IfRight(todo =>
        {
            todo.Id.Should().Be(1);
            todo.Title.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task EndToEnd_MultipleRequests_ShouldReuseHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        // Act
        var results = await Task.WhenAll(
            Encina.Send(new GetTodoRequest(1)),
            Encina.Send(new GetTodoRequest(2)),
            Encina.Send(new GetTodoRequest(3))
        );

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsRight);

        results[0].IfRight(todo => todo.Id.Should().Be(1));
        results[1].IfRight(todo => todo.Id.Should().Be(2));
        results[2].IfRight(todo => todo.Id.Should().Be(3));
    }

    [Fact]
    public async Task EndToEnd_WithCustomHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetHeadersRequest>();
        });
        services.AddEncinaRefitClient<IHttpBinApi>(client =>
        {
            client.BaseAddress = new Uri("https://httpbin.org");
            client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new GetHeadersRequest();

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess();
        result.IfRight(response =>
        {
            response.Headers.Should().ContainKey("X-Custom-Header");
            response.Headers["X-Custom-Header"].Should().Be("TestValue");
        });
    }

    [Fact]
    public async Task EndToEnd_PostRequest_ShouldSendData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<CreateTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new CreateTodoRequest("New Todo", false);

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess();
        result.IfRight(todo =>
        {
            todo.Title.Should().Be("New Todo");
            todo.Completed.Should().BeFalse();
        });
    }

    // Test APIs
    public interface ITodoApi
    {
        [Get("/todos/{id}")]
        Task<Todo> GetTodoAsync(int id);

        [Post("/todos")]
        Task<Todo> CreateTodoAsync([Body] Todo todo);
    }

    public interface IHttpBinApi
    {
        [Get("/headers")]
        Task<HeadersResponse> GetHeadersAsync();
    }

    // Test models
    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public int UserId { get; set; }
    }

    public class HeadersResponse
    {
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    // Test requests
    public record GetTodoRequest(int Id) : IRestApiRequest<ITodoApi, Todo>
    {
        public async Task<Todo> ExecuteAsync(ITodoApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetTodoAsync(Id);
        }
    }

    public record CreateTodoRequest(string Title, bool Completed) : IRestApiRequest<ITodoApi, Todo>
    {
        public async Task<Todo> ExecuteAsync(ITodoApi apiClient, CancellationToken cancellationToken)
        {
            var todo = new Todo
            {
                Title = Title,
                Completed = Completed,
                UserId = 1
            };
            return await apiClient.CreateTodoAsync(todo);
        }
    }

    public record GetHeadersRequest : IRestApiRequest<IHttpBinApi, HeadersResponse>
    {
        public async Task<HeadersResponse> ExecuteAsync(IHttpBinApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetHeadersAsync();
        }
    }
}
