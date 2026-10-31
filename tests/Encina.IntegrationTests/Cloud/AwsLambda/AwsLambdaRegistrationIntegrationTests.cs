using Encina.AwsLambda;
using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Encina.IntegrationTests.Cloud.AwsLambda.AwsLambdaTestSupport;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// Checks the <c>AddEncinaAwsLambda</c> registration and its health check on a provider built with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c>, next to the real Encina pipeline.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AwsLambda")]
public sealed class AwsLambdaRegistrationIntegrationTests
{
    [Fact]
    public async Task AddEncinaAwsLambda_BuildsAValidProviderWithTheOptionsAndTheHealthCheck()
    {
        // Arrange
        await using var provider = CreateServiceProvider(options =>
        {
            options.CorrelationIdHeader = "X-Request-ID";
            options.EnableSqsBatchItemFailures = false;
        });

        // Act
        var options = provider.GetRequiredService<IOptions<EncinaAwsLambdaOptions>>().Value;
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();

        // Assert
        options.CorrelationIdHeader.ShouldBe("X-Request-ID");
        options.EnableSqsBatchItemFailures.ShouldBeFalse();
        healthChecks.ShouldHaveSingleItem().ShouldBeOfType<AwsLambdaHealthCheck>();
    }

    [Fact]
    public async Task AddEncinaAwsLambda_CalledTwice_RegistersTheHealthCheckOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaAwsLambda();
        services.AddEncinaAwsLambda(options => options.UseApiGatewayV2Format = true);

        // Act
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        provider.GetServices<IEncinaHealthCheck>().ShouldHaveSingleItem();
        provider.GetRequiredService<IOptions<EncinaAwsLambdaOptions>>().Value.UseApiGatewayV2Format.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_ReportsTheConfigurationAndTheLambdaEnvironment()
    {
        // Arrange
        await using var provider = CreateServiceProvider(options => options.UseApiGatewayV2Format = true);
        var healthCheck = provider.GetServices<IEncinaHealthCheck>().Single();
        var inLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("aws-lambda");
        healthCheck.Tags.ShouldBe(["serverless", "aws", "lambda"], ignoreOrder: true);
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["useApiGatewayV2Format"].ShouldBe(true);
        result.Data["enableSqsBatchItemFailures"].ShouldBe(true);
        result.Data["correlationIdHeader"].ShouldBe("X-Correlation-ID");
        result.Data["isInLambdaEnvironment"].ShouldBe(inLambda);
        result.Data.ContainsKey("functionName").ShouldBe(inLambda);
    }
}
