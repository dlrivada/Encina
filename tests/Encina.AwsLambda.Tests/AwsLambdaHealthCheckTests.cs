using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using Shouldly;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AwsLambda.Tests;

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
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new AwsLambdaHealthCheck(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("options");
    }
}
