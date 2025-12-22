using Microsoft.Extensions.Logging;

namespace Encina.GraphQL;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Executing GraphQL query of type {QueryType}")]
    public static partial void ExecutingQuery(ILogger logger, string queryType);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully executed GraphQL query of type {QueryType}")]
    public static partial void SuccessfullyExecutedQuery(ILogger logger, string queryType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "GraphQL query of type {QueryType} failed: {ErrorMessage}")]
    public static partial void QueryFailed(ILogger logger, string queryType, string errorMessage);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to execute GraphQL query of type {QueryType}")]
    public static partial void FailedToExecuteQuery(ILogger logger, Exception exception, string queryType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Executing GraphQL mutation of type {MutationType}")]
    public static partial void ExecutingMutation(ILogger logger, string mutationType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Successfully executed GraphQL mutation of type {MutationType}")]
    public static partial void SuccessfullyExecutedMutation(ILogger logger, string mutationType);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "GraphQL mutation of type {MutationType} failed: {ErrorMessage}")]
    public static partial void MutationFailed(ILogger logger, string mutationType, string errorMessage);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Failed to execute GraphQL mutation of type {MutationType}")]
    public static partial void FailedToExecuteMutation(ILogger logger, Exception exception, string mutationType);
}
