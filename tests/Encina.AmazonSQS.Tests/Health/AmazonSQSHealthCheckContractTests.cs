using Encina.AmazonSQS.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.AmazonSQS.Tests.Health;

/// <summary>
/// Contract tests for <see cref="AmazonSQSHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class AmazonSQSHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new AmazonSQSHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new AmazonSQSHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new AmazonSQSHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAmazonSqs()
    {
        AmazonSQSHealthCheck.DefaultName.Should().Be("encina-amazon-sqs");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainAmazonSqsTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("amazon-sqs");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
