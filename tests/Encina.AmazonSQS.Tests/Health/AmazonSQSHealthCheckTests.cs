using Amazon.SQS;
using Amazon.SQS.Model;
using Encina.AmazonSQS.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System.Net;

namespace Encina.AmazonSQS.Tests.Health;

public sealed class AmazonSQSHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAmazonSQS _sqsClient;

    public AmazonSQSHealthCheckTests()
    {
        _sqsClient = Substitute.For<IAmazonSQS>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IAmazonSQS)).Returns(_sqsClient);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        AmazonSQSHealthCheck.DefaultName.ShouldBe("encina-amazon-sqs");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-sqs" };

        // Act
        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-sqs");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(AmazonSQSHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("amazon-sqs");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenListQueuesSucceeds_ReturnsHealthy()
    {
        // Arrange
        var response = new ListQueuesResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            QueueUrls = ["https://sqs.us-east-1.amazonaws.com/123456789/test-queue"]
        };
        _sqsClient.ListQueuesAsync(Arg.Any<ListQueuesRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenListQueuesReturnsNonOk_ReturnsUnhealthy()
    {
        // Arrange
        var response = new ListQueuesResponse
        {
            HttpStatusCode = HttpStatusCode.ServiceUnavailable
        };
        _sqsClient.ListQueuesAsync(Arg.Any<ListQueuesRequest>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ServiceUnavailable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSQSExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _sqsClient.ListQueuesAsync(Arg.Any<ListQueuesRequest>(), Arg.Any<CancellationToken>())
            .Returns<ListQueuesResponse>(_ => throw new AmazonSQSException("Access denied"));

        var healthCheck = new AmazonSQSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Access denied");
    }
}
