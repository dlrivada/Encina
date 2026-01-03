using System.Data;
using Encina.Dapper.Sqlite.Health;
using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Dapper.Sqlite.PropertyTests.Health;

/// <summary>
/// Property-based tests for <see cref="SqliteHealthCheck"/> invariants.
/// </summary>
public sealed class SqliteHealthCheckPropertyTests
{
    [Fact]
    public void DefaultName_IsAlwaysEncinaSqlite()
    {
        Assert.Equal("encina-sqlite", SqliteHealthCheck.DefaultName);
    }

    [Property(MaxTest = 50)]
    public bool Tags_AlwaysContainsEncina(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsDatabase(int seed)
    {
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new SqliteHealthCheck(serviceProvider, null);
        return healthCheck.Tags.Contains("database");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        var tagCount = (count.Get % 5) + 1;
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };
        var healthCheck = new SqliteHealthCheck(serviceProvider, options);

        // Should contain all custom tags plus "encina"
        return tagArray.All(tag => healthCheck.Tags.Contains(tag)) &&
               healthCheck.Tags.Contains("encina");
    }

    [Property(MaxTest = 50)]
    public bool Tags_WithCustomName_UsesCustomName(NonEmptyString customName)
    {
        var name = customName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return true; // Skip invalid names
        }

        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };
        var healthCheck = new SqliteHealthCheck(serviceProvider, options);
        return healthCheck.Name == name;
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
