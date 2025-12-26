using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

namespace Encina.AzureFunctions;

/// <summary>
/// Extension methods for <see cref="IFunctionsWorkerApplicationBuilder"/> to configure Encina middleware.
/// </summary>
public static class FunctionsWorkerApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Encina middleware to the Azure Functions worker pipeline.
    /// </summary>
    /// <param name="builder">The functions worker application builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The middleware provides:
    /// <list type="bullet">
    /// <item><description>Automatic correlation ID extraction/generation</description></item>
    /// <item><description>User ID extraction from claims</description></item>
    /// <item><description>Tenant ID extraction from headers or claims</description></item>
    /// <item><description>Structured logging for function execution</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This middleware should be registered before other custom middleware for consistent
    /// context enrichment throughout the function execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = new HostBuilder()
    ///     .ConfigureFunctionsWorkerDefaults(builder =>
    ///     {
    ///         builder.UseEncinaMiddleware();
    ///     })
    ///     .ConfigureServices(services =>
    ///     {
    ///         services.AddEncina(typeof(Program).Assembly);
    ///         services.AddEncinaAzureFunctions();
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static IFunctionsWorkerApplicationBuilder UseEncinaMiddleware(
        this IFunctionsWorkerApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseMiddleware<EncinaFunctionMiddleware>();

        return builder;
    }
}
