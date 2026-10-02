using Encina.AwsLambda;
using Encina.AwsLambda.Health;
using Encina.Messaging.Health;

namespace Encina.UnitTests.AwsLambda;

public class AwsLambdaHealthCheckTests
{
    [Fact]
    public void Name_ReturnsAwsLambda()
    {
        // Arrange
        var options = Options.Create(new EncinaAwsLambdaOptions());
        var healthCheck = new AwsLambdaHealthCheck(options);

        // Act
        var name = healthCheck.Name;

        // Assert
        name.ShouldBe("aws-lambda");
    }

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        // Arrange
        var options = Options.Create(new EncinaAwsLambdaOptions());
        var healthCheck = new AwsLambdaHealthCheck(options);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("serverless");
        tags.ShouldContain("aws");
        tags.ShouldContain("lambda");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy()
    {
        // Arrange
        var options = Options.Create(new EncinaAwsLambdaOptions());
        var healthCheck = new AwsLambdaHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("configured and ready");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesConfigurationInData()
    {
        // Arrange
        var options = Options.Create(new EncinaAwsLambdaOptions
        {
            EnableRequestContextEnrichment = true,
            UseApiGatewayV2Format = false,
            EnableSqsBatchItemFailures = true,
            CorrelationIdHeader = "X-Correlation-ID"
        });
        var healthCheck = new AwsLambdaHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("enableRequestContextEnrichment");
        result.Data["enableRequestContextEnrichment"].ShouldBe(true);
        result.Data.ShouldContainKey("useApiGatewayV2Format");
        result.Data["useApiGatewayV2Format"].ShouldBe(false);
        result.Data.ShouldContainKey("enableSqsBatchItemFailures");
        result.Data["enableSqsBatchItemFailures"].ShouldBe(true);
        result.Data.ShouldContainKey("correlationIdHeader");
        result.Data["correlationIdHeader"].ShouldBe("X-Correlation-ID");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesLambdaEnvironmentCheck()
    {
        // Arrange
        var options = Options.Create(new EncinaAwsLambdaOptions());
        var healthCheck = new AwsLambdaHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("isInLambdaEnvironment");
    }

    [Fact]
    public async Task CheckHealthAsync_InsideLambda_IncludesFunctionDetails()
    {
        // Arrange
        var previous = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME");
        Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", "orders-fn");

        try
        {
            var healthCheck = new AwsLambdaHealthCheck(Options.Create(new EncinaAwsLambdaOptions()));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            result.Data["isInLambdaEnvironment"].ShouldBe(true);
            result.Data["functionName"].ShouldBe("orders-fn");
            result.Data.ShouldContainKey("functionVersion");
            result.Data.ShouldContainKey("region");
            result.Data.ShouldContainKey("memoryLimitMB");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", previous);
        }
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new AwsLambdaHealthCheck(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("options");
    }
}
