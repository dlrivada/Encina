using Microsoft.Extensions.Logging;

namespace Encina.AzureFunctions;

/// <summary>
/// High-performance logging for Encina Azure Functions integration.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 4750,
        Level = LogLevel.Debug,
        Message = "Function {FunctionName} execution starting (InvocationId: {InvocationId})")]
    public static partial void FunctionExecutionStarting(
        ILogger logger,
        string functionName,
        string invocationId);

    [LoggerMessage(
        EventId = 4751,
        Level = LogLevel.Debug,
        Message = "Function {FunctionName} execution completed (InvocationId: {InvocationId})")]
    public static partial void FunctionExecutionCompleted(
        ILogger logger,
        string functionName,
        string invocationId);

    [LoggerMessage(
        EventId = 4752,
        Level = LogLevel.Error,
        Message = "Function {FunctionName} execution failed (InvocationId: {InvocationId})")]
    public static partial void FunctionExecutionFailed(
        ILogger logger,
        string functionName,
        string invocationId,
        Exception exception);

    [LoggerMessage(
        EventId = 4753,
        Level = LogLevel.Debug,
        Message = "Context enriched for function {FunctionName}: CorrelationId={CorrelationId}, UserId={UserId}, TenantId={TenantId}")]
    public static partial void ContextEnriched(
        ILogger logger,
        string functionName,
        string correlationId,
        string userId,
        string tenantId);

    [LoggerMessage(
        EventId = 4754,
        Level = LogLevel.Information,
        Message = "Azure Functions health check completed: {Status}")]
    public static partial void HealthCheckCompleted(
        ILogger logger,
        string status);
}
