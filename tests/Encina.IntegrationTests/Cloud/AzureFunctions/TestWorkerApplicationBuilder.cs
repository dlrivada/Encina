using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// A concrete worker application builder that composes the registered middleware the way the worker host does.
/// </summary>
internal sealed class TestWorkerApplicationBuilder : IFunctionsWorkerApplicationBuilder
{
    private readonly List<Func<FunctionExecutionDelegate, FunctionExecutionDelegate>> _middleware = [];

    public IServiceCollection Services { get; } = new ServiceCollection();

    public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>Composes the registered middleware in front of the function.</summary>
    public FunctionExecutionDelegate Build(FunctionExecutionDelegate function)
    {
        var pipeline = function;
        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            pipeline = _middleware[i](pipeline);
        }

        return pipeline;
    }
}
