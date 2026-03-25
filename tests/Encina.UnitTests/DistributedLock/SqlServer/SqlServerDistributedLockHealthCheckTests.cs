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
}
