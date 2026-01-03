using Encina.gRPC;
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
        GrpcHealthCheck.DefaultName.ShouldBe("encina-grpc");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMessagingTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainGrpcTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("grpc");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        // Mock the IGrpcEncinaService
        var grpcService = Substitute.For<IGrpcEncinaService>();

        // Mock the scoped service provider that will return the gRPC service
        var serviceScope = Substitute.For<IServiceScope>();
        var scopedServiceProvider = Substitute.For<IServiceProvider>();
        scopedServiceProvider.GetService(typeof(IGrpcEncinaService)).Returns(grpcService);
        serviceScope.ServiceProvider.Returns(scopedServiceProvider);

        // Mock the IServiceScopeFactory
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactory.CreateScope().Returns(serviceScope);

        // Mock the root service provider
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);

        return serviceProvider;
    }
}
