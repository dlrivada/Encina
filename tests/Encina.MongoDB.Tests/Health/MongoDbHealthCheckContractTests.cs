using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Encina.MongoDB.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.MongoDB.Tests.Health;

/// <summary>
/// Contract tests for <see cref="MongoDbHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class MongoDbHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new MongoDbHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new MongoDbHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new MongoDbHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaMongodb()
    {
        MongoDbHealthCheck.DefaultName.Should().Be("encina-mongodb");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainMongodbTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("mongodb");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        return serviceProvider;
    }
}
