using Encina.Testing.Testcontainers;

namespace Encina.GuardTests.Testing.Testcontainers;

public class EncinaContainersGuardTests
{
    [Fact]
    public void SqlServer_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.SqlServer(null!));
    }

    [Fact]
    public void PostgreSql_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.PostgreSql(null!));
    }

    [Fact]
    public void MySql_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.MySql(null!));
    }

    [Fact]
    public void MongoDb_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.MongoDb(null!));
    }

    [Fact]
    public void Redis_NullConfigure_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            EncinaContainers.Redis(null!));
    }
}
