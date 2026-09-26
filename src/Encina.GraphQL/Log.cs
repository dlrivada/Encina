using Microsoft.Extensions.Logging;

namespace Encina.GraphQL;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 4550, Level = LogLevel.Debug, Message = "Executing GraphQL query of type {QueryType}")]
    public static partial void ExecutingQuery(ILogger logger, string queryType);

    [LoggerMessage(EventId = 4551, Level = LogLevel.Debug, Message = "Successfully executed GraphQL query of type {QueryType}")]
    public static partial void SuccessfullyExecutedQuery(ILogger logger, string queryType);

    [LoggerMessage(EventId = 4552, Level = LogLevel.Warning, Message = "GraphQL query of type {QueryType} failed: {ErrorCode}")]
    public static partial void QueryFailed(ILogger logger, string queryType, string errorCode);

    [LoggerMessage(EventId = 4553, Level = LogLevel.Error, Message = "Failed to execute GraphQL query of type {QueryType}")]
    public static partial void FailedToExecuteQuery(ILogger logger, Exception exception, string queryType);

    [LoggerMessage(EventId = 4554, Level = LogLevel.Debug, Message = "Executing GraphQL mutation of type {MutationType}")]
    public static partial void ExecutingMutation(ILogger logger, string mutationType);

    [LoggerMessage(EventId = 4555, Level = LogLevel.Debug, Message = "Successfully executed GraphQL mutation of type {MutationType}")]
    public static partial void SuccessfullyExecutedMutation(ILogger logger, string mutationType);

    [LoggerMessage(EventId = 4556, Level = LogLevel.Warning, Message = "GraphQL mutation of type {MutationType} failed: {ErrorCode}")]
    public static partial void MutationFailed(ILogger logger, string mutationType, string errorCode);

    [LoggerMessage(EventId = 4557, Level = LogLevel.Error, Message = "Failed to execute GraphQL mutation of type {MutationType}")]
    public static partial void FailedToExecuteMutation(ILogger logger, Exception exception, string mutationType);
}
