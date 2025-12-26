using System.Data;
using Encina.Dapper.PostgreSQL.Health;
using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.Dapper.PostgreSQL.PropertyTests.Health;

/// <summary>
/// Property-based tests for <see cref="PostgreSqlHealthCheck"/> invariants.
/// </summary>
public sealed class PostgreSqlHealthCheckPropertyTests
{
    /// <summary>
    /// DefaultName should always be "encina-postgresql".
    /// </summary>
    [Fact]
    public void DefaultName_IsAlwaysEncinaPostgresql()
    {
        Assert.Equal("encina-postgresql", PostgreSqlHealthCheck.DefaultName);
    }

    /// <summary>
    /// Constructor with null options should use DefaultName.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Constructor_WithNullOptions_UsesDefaultName(int seed)
    {
        // Arrange
        var serviceProvider = CreateMockServiceProvider();

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Assert
        return healthCheck.Name == PostgreSqlHealthCheck.DefaultName;
    }

    /// <summary>
    /// Constructor with custom name should use that name.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Constructor_WithCustomName_UsesCustomName(PositiveInt seed)
    {
        // Arrange
        var customName = $"custom-name-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = customName };

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, options);

        // Assert
        return healthCheck.Name == customName;
    }

    /// <summary>
    /// Tags should always contain "encina" when using default options.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsEncina(int seed)
    {
        // Arrange
        var serviceProvider = CreateMockServiceProvider();

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Assert
        return healthCheck.Tags.Contains("encina");
    }

    /// <summary>
    /// Tags should always contain "database" when using default options.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsDatabase(int seed)
    {
        // Arrange
        var serviceProvider = CreateMockServiceProvider();

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Assert
        return healthCheck.Tags.Contains("database");
    }

    /// <summary>
    /// Tags should always contain "postgresql" when using default options.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Tags_WithDefaultOptions_ContainsPostgresql(int seed)
    {
        // Arrange
        var serviceProvider = CreateMockServiceProvider();

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Assert
        return healthCheck.Tags.Contains("postgresql");
    }

    /// <summary>
    /// Custom tags should be used when provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Tags_WithCustomTags_UsesCustomTags(PositiveInt count)
    {
        // Arrange
        var tagCount = (count.Get % 5) + 1; // 1-5 tags
        var tagArray = Enumerable.Range(1, tagCount).Select(i => $"tag-{i}").ToArray();
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Tags = tagArray };

        // Act
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, options);

        // Assert
        return tagArray.All(tag => healthCheck.Tags.Contains(tag));
    }

    /// <summary>
    /// CheckHealthAsync should return a valid HealthStatus.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool CheckHealthAsync_ReturnsValidHealthStatus(int seed)
    {
        // Arrange
        var serviceProvider = CreateMockServiceProvider();
        var healthCheck = new PostgreSqlHealthCheck(serviceProvider, null);

        // Act
        var result = healthCheck.CheckHealthAsync().GetAwaiter().GetResult();

        // Assert - Status should be one of the valid enum values
        return result.Status == HealthStatus.Healthy
            || result.Status == HealthStatus.Unhealthy
            || result.Status == HealthStatus.Degraded;
    }

    /// <summary>
    /// Multiple health check instances with same options should have same Name.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MultipleInstances_WithSameOptions_HaveSameName(PositiveInt seed)
    {
        // Arrange
        var name = $"health-check-{seed.Get}";
        var serviceProvider = CreateMockServiceProvider();
        var options = new ProviderHealthCheckOptions { Name = name };

        // Act
        var healthCheck1 = new PostgreSqlHealthCheck(serviceProvider, options);
        var healthCheck2 = new PostgreSqlHealthCheck(serviceProvider, options);

        // Assert
        return healthCheck1.Name == healthCheck2.Name;
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
