using Encina.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using static Encina.IntegrationTests.Cloud.AzureFunctions.AzureFunctionsTestSupport;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Runs <see cref="EncinaFunctionMiddleware"/> as the worker pipeline does, in front of a function that uses the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class EncinaFunctionMiddlewareIntegrationTests
{
    private static async Task<HttpResponseData> OrdersFunction(FunctionContext context, PlaceOrder command)
    {
        var request = new TestHttpRequestData(context);
        var encina = context.InstanceServices.GetRequiredService<IEncina>();
        return await (await encina.Send(command)).ToHttpResponseData(request);
    }

    [Fact]
    public async Task UseEncinaMiddleware_RegistersTheMiddlewareInTheWorkerPipeline()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var builder = new TestWorkerApplicationBuilder();
        var context = new TestFunctionContext(scope.ServiceProvider, "PlaceOrder", "inv-1");
        HttpResponseData? response = null;

        // Act
        var returned = builder.UseEncinaMiddleware();
        await builder.Build(async ctx => response = await OrdersFunction(ctx, new PlaceOrder("o-1", 10m)))(context);

        // Assert
        returned.ShouldBeSameAs(builder);
        context.GetCorrelationId().ShouldNotBeNullOrEmpty();
        response.ShouldNotBeNull().StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        ((TestHttpResponseData)response).ReadBody().ShouldContain("\"status\":\"Placed\"");
    }

    [Fact]
    public async Task Invoke_EnrichesTheContextBeforeTheFunctionRunsAndLogsTheLifecycle()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var logger = new FakeLogger<EncinaFunctionMiddleware>();
        var middleware = new EncinaFunctionMiddleware(
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EncinaAzureFunctionsOptions>>(),
            logger);
        var context = new TestFunctionContext(scope.ServiceProvider, "PlaceOrder", "inv-2");
        string? correlationInsideFunction = null;

        // Act
        await middleware.Invoke(context, ctx =>
        {
            correlationInsideFunction = ctx.GetCorrelationId();
            return Task.CompletedTask;
        });

        // Assert
        correlationInsideFunction.ShouldNotBeNullOrEmpty();
        correlationInsideFunction.ShouldBe(context.GetCorrelationId());
        context.GetInvocationId().ShouldBe("inv-2");
        logger.Collector.GetSnapshot().Select(r => r.Message).ShouldContain(m => m.Contains("PlaceOrder"));
    }

    [Fact]
    public async Task Invoke_WithEnrichmentDisabled_LeavesTheContextUntouched()
    {
        // Arrange
        await using var provider = CreateServiceProvider(o => o.EnableRequestContextEnrichment = false);
        await using var scope = provider.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<EncinaFunctionMiddleware>();
        var context = new TestFunctionContext(scope.ServiceProvider, "PlaceOrder", "inv-3");

        // Act
        await middleware.Invoke(context, _ => Task.CompletedTask);

        // Assert
        context.GetCorrelationId().ShouldBeNull();
    }

    [Fact]
    public async Task Invoke_WhenTheFunctionThrows_LogsTheFailureAndRethrows()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var logger = new FakeLogger<EncinaFunctionMiddleware>();
        var middleware = new EncinaFunctionMiddleware(
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EncinaAzureFunctionsOptions>>(),
            logger);
        var context = new TestFunctionContext(scope.ServiceProvider, "PlaceOrder", "inv-4");

        // Act
        var act = () => middleware.Invoke(context, _ => throw new InvalidOperationException("secret detail"));

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
        var records = logger.Collector.GetSnapshot();
        var failure = records[^1];
        failure.Level.ShouldBe(LogLevel.Error);
        failure.Message.ShouldNotContain("secret detail");
    }
}
