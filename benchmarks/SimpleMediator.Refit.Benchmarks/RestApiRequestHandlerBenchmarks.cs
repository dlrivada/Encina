using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using SimpleMediator.Refit;

namespace SimpleMediator.Refit.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// Measures performance overhead of using SimpleMediator with Refit.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RestApiRequestHandlerBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private ISimpleMediator _mediator = null!;
    private ITodoApi _directClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSimpleMediator(options =>
        {
            options.RegisterServicesFromAssemblyContaining<GetTodoRequest>();
        });
        services.AddSimpleMediatorRefitClient<ITodoApi>(client =>
        {
            client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<ISimpleMediator>();
        _directClient = _serviceProvider.GetRequiredService<ITodoApi>();
    }

    [Benchmark(Baseline = true)]
    public async Task<Todo> DirectRefitCall_Baseline()
    {
        return await _directClient.GetTodoAsync(1);
    }

    [Benchmark]
    public async Task<Todo?> SimpleMediatorRefitCall()
    {
        var request = new GetTodoRequest(1);
        var result = await _mediator.Send(request);

        return result.Match(
            Right: todo => todo,
            Left: _ => null
        );
    }

    [Benchmark]
    public async Task<List<Todo>> DirectRefitCall_Batch10()
    {
        var tasks = Enumerable.Range(1, 10)
            .Select(id => _directClient.GetTodoAsync(id))
            .ToList();

        return (await Task.WhenAll(tasks)).ToList();
    }

    [Benchmark]
    public async Task<List<Todo>> SimpleMediatorRefitCall_Batch10()
    {
        var tasks = Enumerable.Range(1, 10)
            .Select(async id =>
            {
                var request = new GetTodoRequest(id);
                var result = await _mediator.Send(request);
                return result.Match(
                    Right: todo => todo,
                    Left: _ => new Todo()
                );
            })
            .ToList();

        return await Task.WhenAll(tasks).ContinueWith(t => t.Result.ToList());
    }

    [Benchmark]
    public async Task<List<Todo>> DirectRefitCall_Sequential5()
    {
        var todos = new List<Todo>();
        for (int i = 1; i <= 5; i++)
        {
            todos.Add(await _directClient.GetTodoAsync(i));
        }
        return todos;
    }

    [Benchmark]
    public async Task<List<Todo>> SimpleMediatorRefitCall_Sequential5()
    {
        var todos = new List<Todo>();
        for (int i = 1; i <= 5; i++)
        {
            var request = new GetTodoRequest(i);
            var result = await _mediator.Send(request);
            result.IfRight(todo => todos.Add(todo));
        }
        return todos;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
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
