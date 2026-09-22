using Microsoft.Extensions.Logging;

namespace Encina.AwsLambda;

/// <summary>
/// High-performance logging for Encina AWS Lambda integration.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 4700,
        Level = LogLevel.Debug,
        Message = "Lambda function {FunctionName} execution starting (RequestId: {RequestId})")]
    public static partial void LambdaExecutionStarting(
        ILogger logger,
        string functionName,
        string requestId);

    [LoggerMessage(
        EventId = 4701,
        Level = LogLevel.Debug,
        Message = "Lambda function {FunctionName} execution completed (RequestId: {RequestId})")]
    public static partial void LambdaExecutionCompleted(
        ILogger logger,
        string functionName,
        string requestId);

    [LoggerMessage(
        EventId = 4702,
        Level = LogLevel.Error,
        Message = "Lambda function {FunctionName} execution failed (RequestId: {RequestId})")]
    public static partial void LambdaExecutionFailed(
        ILogger logger,
        string functionName,
        string requestId,
        Exception exception);

    [LoggerMessage(
        EventId = 4703,
        Level = LogLevel.Debug,
        Message = "Processing SQS batch with {MessageCount} messages")]
    public static partial void ProcessingSqsBatch(
        ILogger logger,
        int messageCount);

    [LoggerMessage(
        EventId = 4704,
        Level = LogLevel.Error,
        Message = "Failed to process SQS message {MessageId}")]
    public static partial void SqsMessageProcessingFailed(
        ILogger logger,
        string messageId,
        Exception exception);

    [LoggerMessage(
        EventId = 4705,
        Level = LogLevel.Debug,
        Message = "Processing EventBridge event {EventId} from source {Source}")]
    public static partial void ProcessingEventBridgeEvent(
        ILogger logger,
        string eventId,
        string source);

    [LoggerMessage(
        EventId = 4706,
        Level = LogLevel.Information,
        Message = "AWS Lambda health check completed: {Status}")]
    public static partial void HealthCheckCompleted(
        ILogger logger,
        string status);
}
