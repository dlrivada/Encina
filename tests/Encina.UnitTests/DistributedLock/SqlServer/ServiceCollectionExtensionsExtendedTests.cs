using Encina.DistributedLock;
using Encina.DistributedLock.SqlServer;
using Encina.DistributedLock.SqlServer.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Extended DI registration tests for SQL Server distributed lock covering
/// health check registration, null guards on overloads, and option application.
/// </summary>
public sealed class ServiceCollectionExtensionsExtendedTests
{
    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithOptionsOverload_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockSqlServer("Server=.;", null!));
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithOptionsOverload_NullConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDistributedLockSqlServer(null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockSqlServer(
            "Server=.;Database=Test;Trusted_Connection=True;",
            options =>
            {
                options.ProviderHealthCheck.Enabled = true;
            });

        // Assert
        var sp = services.BuildServiceProvider();
        var healthCheck = sp.GetService<SqlServerDistributedLockHealthCheck>();
        healthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockSqlServer(
            "Server=.;Database=Test;Trusted_Connection=True;",
            options =>
            {
                options.ProviderHealthCheck.Enabled = false;
            });

        // Assert
        var sp = services.BuildServiceProvider();
        var healthCheck = sp.GetService<SqlServerDistributedLockHealthCheck>();
        healthCheck.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_DefaultHealthCheckEnabled_RegistersIEncinaHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - health check is enabled by default
        services.AddEncinaDistributedLockSqlServer("Server=.;Database=Test;Trusted_Connection=True;");

        // Assert
        var sp = services.BuildServiceProvider();
        var healthChecks = sp.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldContain(h => h is SqlServerDistributedLockHealthCheck);
    }

    [Fact]
    public void AddEncinaDistributedLockSqlServer_ConfiguresOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockSqlServer(
            "Server=.;Database=Test;Trusted_Connection=True;",
            options =>
            {
                options.KeyPrefix = "test-prefix";
                options.DefaultExpiry = TimeSpan.FromMinutes(2);
            });

        // Assert
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<SqlServerLockOptions>>();
        opts.Value.ConnectionString.ShouldBe("Server=.;Database=Test;Trusted_Connection=True;");
        opts.Value.KeyPrefix.ShouldBe("test-prefix");
        opts.Value.DefaultExpiry.ShouldBe(TimeSpan.FromMinutes(2));
    }
}
