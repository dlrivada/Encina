using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using FluentAssertions;
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
        name.Should().Be("aws-lambda");
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
        tags.Should().Contain("serverless");
        tags.Should().Contain("aws");
        tags.Should().Contain("lambda");
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
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("configured and ready");
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
        result.Data.Should().ContainKey("enableRequestContextEnrichment");
        result.Data["enableRequestContextEnrichment"].Should().Be(true);
        result.Data.Should().ContainKey("useApiGatewayV2Format");
        result.Data["useApiGatewayV2Format"].Should().Be(false);
        result.Data.Should().ContainKey("enableSqsBatchItemFailures");
        result.Data["enableSqsBatchItemFailures"].Should().Be(true);
        result.Data.Should().ContainKey("correlationIdHeader");
        result.Data["correlationIdHeader"].Should().Be("X-Correlation-ID");
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
        result.Data.Should().ContainKey("isInLambdaEnvironment");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new AwsLambdaHealthCheck(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
