using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Refit;
using Encina.Refit;

namespace Encina.Refit.LoadTests;

/// <summary>
/// Load tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// Tests performance and concurrency using NBomber.
/// </summary>
[Trait("Category", "Load")]
public class RestApiRequestLoadTests
{
    [Fact]
    public void HighConcurrency_ParallelApiCalls_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        // Act
        var scenario = Scenario.Create("parallel_api_calls", async context =>
        {
            var todoId = Random.Shared.Next(1, 200);
            var request = new GetTodoRequest(todoId);
            var result = await Encina.Send(request);

            return result.IsRight
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scnStats = stats.ScenarioStats[0];
        scnStats.Ok.Request.Count.Should().BeGreaterThan(0);
        scnStats.Fail.Request.Count.Should().Be(0);
    }

    [Fact]
    public void MixedLoad_SuccessAndFailure_ShouldNotDeadlock()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        // Act - Mix of valid and invalid IDs
        var scenario = Scenario.Create("mixed_load", async context =>
        {
            var todoId = Random.Shared.Next(1, 300); // Some will be 404
            var request = new GetTodoRequest(todoId);
            var result = await Encina.Send(request);

            // Both success and failure are acceptable
            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert - Should complete without deadlock
        var scnStats = stats.ScenarioStats[0];
        scnStats.Ok.Request.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HighThroughput_HttpClientPooling_ShouldNotExhaust()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Increase handler lifetime

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        // Act - High throughput scenario
        var scenario = Scenario.Create("high_throughput", async context =>
        {
            var todoId = Random.Shared.Next(1, 100);
            var request = new GetTodoRequest(todoId);
            var result = await Encina.Send(request);

            return result.IsRight
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scnStats = stats.ScenarioStats[0];
        scnStats.Ok.Request.Count.Should().BeGreaterThan(0);
        scnStats.Ok.Request.RPS.Should().BeGreaterThan(0);
    }

    [Fact]
    public void StressTest_ConcurrentHandlers_ShouldNotCorrupt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddEncina(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddEncinaRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var successCount = 0;
        var failureCount = 0;
        var lockObj = new object();

        // Act - Stress test with concurrent requests
        var scenario = Scenario.Create("stress_test", async context =>
        {
            var todoId = Random.Shared.Next(1, 50);
            var request = new GetTodoRequest(todoId);
            var result = await Encina.Send(request);

            lock (lockObj)
            {
                if (result.IsRight)
                    successCount++;
                else
                    failureCount++;
            }

            return result.IsRight
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scnStats = stats.ScenarioStats[0];
        var totalRequests = successCount + failureCount;
        totalRequests.Should().BeGreaterThan(0);
        scnStats.Ok.Request.Count.Should().Be(successCount);
    }

    // Test API
    public interface ITodoApi
    {
        [Get("/todos/{id}")]
        Task<Todo> GetTodoAsync(int id);
    }

    // Test model
    public class Todo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public int UserId { get; set; }
    }

    // Test request
    public record GetTodoRequest(int Id) : IRestApiRequest<ITodoApi, Todo>
    {
        public async Task<Todo> ExecuteAsync(ITodoApi apiClient, CancellationToken cancellationToken)
        {
            return await apiClient.GetTodoAsync(Id);
        }
    }
}
