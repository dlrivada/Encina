using System.Data;
using Encina.ADO.PostgreSQL.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.PostgreSQL.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="PostgreSqlHealthCheck"/> (ADO.NET) ensuring it follows the IEncinaHealthCheck contract.
/// </summary>
public sealed class PostgreSqlHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        var serviceProvider = CreateMockServiceProvider();
        return new PostgreSqlHealthCheck(serviceProvider, null);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        return new PostgreSqlHealthCheck(serviceProvider, options);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tags.ToArray() };
        return new PostgreSqlHealthCheck(serviceProvider, options);
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaAdoPostgresql()
    {
        PostgreSqlHealthCheck.DefaultName.ShouldBe("encina-ado-postgresql");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainPostgresqlTag()
    {
        var healthCheck = CreateHealthCheck();
        healthCheck.Tags.ShouldContain("postgresql");
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
