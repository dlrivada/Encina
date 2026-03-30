using Encina.Testing.Testcontainers;

namespace Encina.GuardTests.Testing.Testcontainers;

public class DatabaseIntegrationTestBaseGuardTests
{
    [Fact]
    public void Constructor_NullFixture_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TestDatabaseIntegrationTestBase(null!));
    }

    /// <summary>
    /// Concrete test subclass to verify guard clause on the abstract base class.
    /// </summary>
    private sealed class TestDatabaseIntegrationTestBase
        : DatabaseIntegrationTestBase<SqlServerContainerFixture>
    {
        public TestDatabaseIntegrationTestBase(SqlServerContainerFixture fixture)
            : base(fixture)
        {
        }
    }
}
