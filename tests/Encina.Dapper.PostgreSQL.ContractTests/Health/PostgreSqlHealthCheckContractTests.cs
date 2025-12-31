using System.Data;
using Encina.Dapper.PostgreSQL.Health;
using Encina.Messaging.ContractTests.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Dapper.PostgreSQL.ContractTests.Health;

/// <summary>
/// Contract tests for <see cref="PostgreSqlHealthCheck"/> ensuring it follows the IEncinaHealthCheck contract.
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
    public void DefaultName_ShouldBeEncinaHyphenPostgresql()
    {
        // Arrange
        var expected = "encina-postgresql";

        // Act
        var actual = PostgreSqlHealthCheck.DefaultName;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainDatabaseTag()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("database");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ShouldContainPostgresqlTag()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("postgresql");
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
