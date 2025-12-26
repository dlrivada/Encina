using System.Data;
using Encina.ADO.PostgreSQL.Health;
using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.PostgreSQL.PropertyTests.Health;

/// <summary>
/// Property-based tests for <see cref="PostgreSqlHealthCheck"/> invariants (ADO.NET).
/// </summary>
public sealed class PostgreSqlHealthCheckPropertyTests
{
    [Fact]
    public void DefaultName_IsAlwaysEncinaAdoPostgresql()
    {
        Assert.Equal("encina-ado-postgresql", PostgreSqlHealthCheck.DefaultName);
    }

    [Property(MaxTest = 50)]
    public bool Constructor_WithNullOptions_UsesDefaultName(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);
        return healthCheck.Name == PostgreSqlHealthCheck.DefaultName;
    }

    [Property(MaxTest = 100)]
    public bool Constructor_WithCustomName_UsesCustomName(PositiveInt seed)
    {
        var customName = $"custom-name-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = customName };
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, options);
        return healthCheck.Name == customName;
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsEncina(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsDatabase(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("database");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsPostgresql(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("postgresql");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        var tagCount = (count.Get % 5) + 1;
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, options);
        return tagArray.All(tag => healthCheck.Tags.Contains(tag));
    }

    [Property(MaxTest = 20)]
    public bool CheckHealthAsync_ReturnsValidHealthStatus(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();
        return result.Status == HealthStatus.Healthy
            || result.Status == HealthStatus.Unhealthy
            || result.Status == HealthStatus.Degraded;
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
