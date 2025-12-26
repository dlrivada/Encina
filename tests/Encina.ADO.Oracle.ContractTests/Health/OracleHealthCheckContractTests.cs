using System.Data;
using Encina.ADO.Oracle.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.Oracle.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="OracleHealthCheck"/> (ADO.NET) ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class OracleHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new OracleHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new OracleHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new OracleHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAdoOracle()
    {
        OracleHealthCheck.DefaultName.Should().Be("encina-ado-oracle");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainOracleTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.Should().Contain("oracle");
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var serviceScope = Substitute.For<IServiceScope>();
        var scopedServiceProvider = Substitute.For<IServiceProvider>();
        var connection = Substitute.For<IDbConnection>();

        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceScope.ServiceProvider.Returns(scopedServiceProvider);
        scopedServiceProvider.GetService(typeof(IDbConnection)).Returns(connection);

        return serviceProvider;
    }
}
