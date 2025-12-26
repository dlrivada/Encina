using System.Data;
using Encina.ADO.MySQL.Health;
using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.MySQL.PropertyTests.Health;

/// <summary>
/// Property-based tests for <see cref="MySqlHealthCheck"/> invariants (ADO.NET).
/// </summary>
public sealed class MySqlHealthCheckPropertyTests
{
    [Fact]
    public void DefaultName_IsAlwaysEncinaAdoMysql()
    {
        Assert.Equal("encina-ado-mysql", MySqlHealthCheck.DefaultName);
    }

    [Property(MaxTest = 50)]
    public bool Constructor_WithNullOptions_UsesDefaultName(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);
        return healthCheck.Name == MySqlHealthCheck.DefaultName;
    }

    [Property(MaxTest = 100)]
    public bool Constructor_WithCustomName_UsesCustomName(PositiveInt seed)
    {
        var customName = $"custom-name-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = customName };
        var healthCheck = new MySqlHealthCheck(serviceProvider, options);
        return healthCheck.Name == customName;
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsEncina(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsDatabase(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("database");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsMysql(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("mysql");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        var tagCount = (count.Get % 5) + 1;
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };
        var healthCheck = new MySqlHealthCheck(serviceProvider, options);
        return tagArray.All(tag => healthCheck.Tags.Contains(tag));
    }

    [Property(MaxTest = 20)]
    public bool CheckHealthAsync_ReturnsValidHealthStatus(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new MySqlHealthCheck(serviceProvider, null);
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
