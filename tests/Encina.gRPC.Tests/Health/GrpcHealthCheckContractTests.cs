using Encina.gRPC.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.gRPC.Tests.Health;

/// <summary>
/// Contract tests for <see cref="GrpcHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class GrpcHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new GrpcHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new GrpcHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new GrpcHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaGrpc()
    {
        GrpcHealthCheck.DefaultName.Should().Be("encina-grpc");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainGrpcTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("grpc");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
