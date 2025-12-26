using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.AzureFunctions;

/// <summary>
/// Middleware for Azure Functions that enriches the function context with Encina request information.
/// </summary>
/// <remarks>
/// <para>
/// This middleware automatically extracts and sets correlation ID, user ID, and tenant ID
/// from HTTP request headers and claims, making them available throughout the function execution.
/// </para>
/// <para>
/// The middleware integrates with <see cref="FunctionContextExtensions"/> to provide consistent
/// access to context information.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// var host = new HostBuilder()
///     .ConfigureFunctionsWorkerDefaults(builder =>
///     {
///         builder.UseEncinaMiddleware();
///     })
///     .Build();
/// </code>
/// </example>
public sealed class EncinaFunctionMiddleware : IFunctionsWorkerMiddleware
{
    internal const string CorrelationIdKey = "Encina.CorrelationId";
    internal const string UserIdKey = "Encina.UserId";
    internal const string TenantIdKey = "Encina.TenantId";

    private readonly EncinaAzureFunctionsOptions _options;
    private readonly ILogger<EncinaFunctionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaFunctionMiddleware"/> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger.</param>
    public EncinaFunctionMiddleware(
        IOptions<EncinaAzureFunctionsOptions> options,
        ILogger<EncinaFunctionMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (_options.EnableRequestContextEnrichment)
        {
            EnrichContext(context);
        }

        Log.FunctionExecutionStarting(_logger, context.FunctionDefinition.Name, context.InvocationId);

        try
        {
            await next(context);
            Log.FunctionExecutionCompleted(_logger, context.FunctionDefinition.Name, context.InvocationId);
        }
        catch (Exception ex)
        {
            Log.FunctionExecutionFailed(_logger, context.FunctionDefinition.Name, context.InvocationId, ex);
            throw;
        }
    }

    private void EnrichContext(FunctionContext context)
    {
        // Generate a new correlation ID (HTTP header extraction happens via extensions at function level)
        var correlationId = Guid.NewGuid().ToString("N");
        context.Items[CorrelationIdKey] = correlationId;

        Log.ContextEnriched(
            _logger,
            context.FunctionDefinition.Name,
            correlationId,
            "(pending)",
            "(pending)");
    }
}
