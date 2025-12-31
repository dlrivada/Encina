using System.Data;
using Encina.ADO.SqlServer.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.SqlServer.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="SqlServerHealthCheck"/> (ADO.NET) ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class SqlServerHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new SqlServerHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new SqlServerHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new SqlServerHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAdoSqlserver()
    {
        SqlServerHealthCheck.DefaultName.ShouldBe("encina-ado-sqlserver");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainSqlserverTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("sqlserver");
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
