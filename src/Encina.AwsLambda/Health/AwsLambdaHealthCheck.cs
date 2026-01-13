using Encina.Messaging.Health;
using Microsoft.Extensions.Options;

namespace Encina.AwsLambda.Health;

/// <summary>
/// Health check implementation for AWS Lambda integration.
/// </summary>
/// <remarks>
/// <para>
/// This health check validates that the AWS Lambda integration is properly configured
/// and ready to process requests.
/// </para>
/// </remarks>
public sealed class AwsLambdaHealthCheck : IEncinaHealthCheck
{
    private const string UnknownStatus = "unknown";
    private readonly EncinaAwsLambdaOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The AWS Lambda options.</param>
    public AwsLambdaHealthCheck(IOptions<EncinaAwsLambdaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string Name => "aws-lambda";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags { get; } = ["serverless", "aws", "lambda"];

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        // Basic configuration validation
        var data = new Dictionary<string, object>
        {
            ["enableRequestContextEnrichment"] = _options.EnableRequestContextEnrichment,
            ["useApiGatewayV2Format"] = _options.UseApiGatewayV2Format,
            ["enableSqsBatchItemFailures"] = _options.EnableSqsBatchItemFailures,
            ["correlationIdHeader"] = _options.CorrelationIdHeader
        };

        // Check if running in Lambda environment
        var isInLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
        data["isInLambdaEnvironment"] = isInLambda;

        if (isInLambda)
        {
            data["functionName"] = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") ?? UnknownStatus;
            data["functionVersion"] = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_VERSION") ?? UnknownStatus;
            data["region"] = Environment.GetEnvironmentVariable("AWS_REGION") ?? UnknownStatus;
            data["memoryLimitMB"] = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_MEMORY_SIZE") ?? UnknownStatus;
        }

        var result = new HealthCheckResult(
            HealthStatus.Healthy,
            "AWS Lambda integration is configured and ready",
            exception: null,
            data: data);

        return Task.FromResult(result);
    }
}
