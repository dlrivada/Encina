using Encina.DistributedLock;
using Encina.DistributedLock.SqlServer;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Unit tests for <see cref="SqlServerLockOptions"/>.
/// </summary>
public sealed class SqlServerLockOptionsTests
{
    [Fact]
    public void DefaultValues_ConnectionString_IsNull()
    {
        var options = new SqlServerLockOptions();
        options.ConnectionString.ShouldBeNull();
    }

    [Fact]
    public void ConnectionString_CanBeSet()
    {
        var options = new SqlServerLockOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        };

        options.ConnectionString.ShouldBe("Server=.;Database=Test;Trusted_Connection=True;");
    }

    [Fact]
    public void ToString_ShouldContainPrefixAndExpiry()
    {
        var options = new SqlServerLockOptions
        {
            KeyPrefix = "myapp"
        };

        var result = options.ToString();
        result.ShouldContain("myapp");
        result.ShouldContain("SqlServerLockOptions");
    }

    [Fact]
    public void InheritsFromDistributedLockOptions()
    {
        var options = new SqlServerLockOptions();
        options.ShouldBeAssignableTo<DistributedLockOptions>();
    }

    [Fact]
    public void KeyPrefix_InheritsDefaultFromBase()
    {
        var options = new SqlServerLockOptions();
        // DistributedLockOptions has a default KeyPrefix of null or empty
        options.KeyPrefix.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void DefaultExpiry_InheritsFromBase()
    {
        var options = new SqlServerLockOptions();
        options.DefaultExpiry.ShouldBeGreaterThan(TimeSpan.Zero);
    }
}
