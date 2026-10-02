using Encina.DistributedLock.SqlServer;

namespace Encina.UnitTests.DistributedLock.SqlServer;

public class SqlServerLockOptionsAndHealthCheckTests
{
    [Fact]
    public void SqlServerLockOptions_ConnectionString_DefaultIsNull()
    {
        var options = new SqlServerLockOptions();
        options.ConnectionString.ShouldBeNull();
    }

    [Fact]
    public void SqlServerLockOptions_ConnectionString_IsSettable()
    {
        var options = new SqlServerLockOptions { ConnectionString = "Server=.;Database=locks" };
        options.ConnectionString.ShouldBe("Server=.;Database=locks");
    }

    [Fact]
    public void SqlServerLockOptions_ToString_DoesNotLeakConnectionString()
    {
        var options = new SqlServerLockOptions { ConnectionString = "SuperSecret=123" };
        var str = options.ToString();
        str.ShouldNotContain("SuperSecret");
        str.ShouldContain("SqlServerLockOptions");
    }

    [Fact]
    public void SqlServerLockOptions_InheritsFromDistributedLockOptions()
    {
        typeof(SqlServerLockOptions).BaseType.ShouldBe(typeof(global::Encina.DistributedLock.DistributedLockOptions));
    }

    [Fact]
    public void SqlServerLockOptions_ToString_ContainsPrefix()
    {
        var options = new SqlServerLockOptions();
        options.KeyPrefix = "test";
        options.ToString().ShouldContain("test");
    }

    [Fact]
    public async Task HealthCheck_WhenConnectionFails_ReportsOnlyTheExceptionType()
    {
        // An unsupported keyword makes SqlConnection throw an ArgumentException whose message
        // names the keyword; none of that text may reach the health result.
        var healthCheck = new global::Encina.DistributedLock.SqlServer.Health.SqlServerDistributedLockHealthCheck(
            "Server=db-secret-host.internal;Bogus Keyword=1",
            new global::Encina.Messaging.Health.ProviderHealthCheckOptions());

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(global::Encina.Messaging.Health.HealthStatus.Unhealthy);
        result.Description!.ShouldContain("SQL Server connection failed");
        result.Description!.ShouldContain(nameof(ArgumentException));
        result.Description!.ShouldNotContain("Keyword");
        result.Description!.ShouldNotContain("db-secret-host");
    }
}
