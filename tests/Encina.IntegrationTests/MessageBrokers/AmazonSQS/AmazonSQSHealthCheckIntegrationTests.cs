using Amazon.SQS;
using Encina.AmazonSQS;
using Encina.AmazonSQS.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Encina.IntegrationTests.MessageBrokers.AmazonSQS.AmazonSQSTestSupport;

namespace Encina.IntegrationTests.MessageBrokers.AmazonSQS;

/// <summary>
/// Integration tests for <see cref="AmazonSQSHealthCheck"/> and the package registration against LocalStack.
/// </summary>
[Collection(LocalStackFixture.CollectionName)]
[Trait("Category", "Integration")]
[Trait("Messaging", "AmazonSQS")]
public sealed class AmazonSQSHealthCheckIntegrationTests : IAsyncLifetime
{
    private readonly LocalStackFixture _fixture;

    public AmazonSQSHealthCheckIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CheckHealthAsync_WhenEmulatorIsRunning_ReturnsHealthy()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        var healthCheck = new AmazonSQSHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);
        var healthCheck = new AmazonSQSHealthCheck(provider, new ProviderHealthCheckOptions { Name = "my-sqs" });

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-sqs");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenEndpointIsUnreachable_ReturnsUnhealthy()
    {
        // Arrange: a client pointed at a closed port, no retries so the test stays fast.
        using var deadClient = new AmazonSQSClient(
            _fixture.Credentials,
            new AmazonSQSConfig
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "us-east-1",
                MaxErrorRetry = 0,
                UseHttp = true
            });
        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSQS>(deadClient);
        await using var provider = services.BuildServiceProvider();
        var healthCheck = new AmazonSQSHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task AddEncinaAmazonSQS_RegistersHealthCheckThatReportsHealthy()
    {
        // Arrange
        await using var provider = CreateServiceProvider(_fixture);

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>().ToList();
        var sqsCheck = healthChecks.OfType<AmazonSQSHealthCheck>().Single();
        var result = await sqsCheck.CheckHealthAsync();

        // Assert
        sqsCheck.Name.ShouldBe(AmazonSQSHealthCheck.DefaultName);
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithClientsRegisteredFirst_KeepsThoseClients()
    {
        // Arrange
        using var provider = CreateServiceProvider(_fixture);

        // Act
        var sqs = provider.GetRequiredService<IAmazonSQS>();

        // Assert
        sqs.ShouldBeSameAs(_fixture.SqsClient);
    }

    [Fact]
    public void AddEncinaAmazonSQS_WithHealthCheckDisabled_RegistersNoHealthCheck()
    {
        // Arrange
        using var provider = CreateServiceProvider(_fixture, options => options.ProviderHealthCheck.Enabled = false);

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();

        // Assert
        healthChecks.OfType<AmazonSQSHealthCheck>().ShouldBeEmpty();
    }
}
