using Encina.Testing.Respawn;
using Encina.Testing.Testcontainers;
using Testcontainers.PostgreSql;

namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="DatabaseIntegrationTestBase{TFixture}"/>.
/// </summary>
public class DatabaseIntegrationTestBaseTests
{
    [Fact]
    public void Constructor_WithNullFixture_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestableIntegrationTestBase(null!));
    }

    [Fact]
    public void Fixture_ShouldReturnProvidedFixture()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();

        // Act
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Assert
        testBase.GetFixture().ShouldBe(fixture);
    }

    [Fact]
    public void RespawnAdapter_WithSqlServerFixture_ShouldReturnSqlServer()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Act
        var adapter = testBase.GetRespawnAdapter();

        // Assert
        adapter.ShouldBe(RespawnAdapter.SqlServer);
    }

    [Fact]
    public void RespawnAdapter_WithPostgreSqlFixture_ShouldReturnPostgreSql()
    {
        // Arrange
        var fixture = new PostgreSqlContainerFixture();
        var testBase = new TestablePostgreSqlIntegrationTestBase(fixture);

        // Act
        var adapter = testBase.GetRespawnAdapter();

        // Assert
        adapter.ShouldBe(RespawnAdapter.PostgreSql);
    }

    [Fact]
    public void RespawnAdapter_WithMySqlFixture_ShouldReturnMySql()
    {
        // Arrange
        var fixture = new MySqlContainerFixture();
        var testBase = new TestableMySqlIntegrationTestBase(fixture);

        // Act
        var adapter = testBase.GetRespawnAdapter();

        // Assert
        adapter.ShouldBe(RespawnAdapter.MySql);
    }

    [Fact]
    public void RespawnAdapter_WithUnsupportedFixture_ShouldThrowNotSupportedException()
    {
        // Arrange
        var fixture = new MongoDbContainerFixture();
        var testBase = new TestableMongoDbIntegrationTestBase(fixture);

        // Act & Assert
        Should.Throw<NotSupportedException>(() => testBase.GetRespawnAdapter())
            .Message.ShouldContain("does not have a corresponding Respawn adapter");
    }

    [Fact]
    public void RespawnOptions_ByDefault_ShouldNotResetEncinaMessagingTables()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Act
        var options = testBase.GetRespawnOptions();

        // Assert
        options.ResetEncinaMessagingTables.ShouldBeFalse();
    }

    [Fact]
    public void RespawnOptions_WhenOverridden_ShouldUseCustomOptions()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();
        var customOptions = new RespawnOptions
        {
            ResetEncinaMessagingTables = true,
            TablesToIgnore = ["CustomTable"]
        };
        var testBase = new TestableSqlServerIntegrationTestBaseWithCustomOptions(fixture, customOptions);

        // Act
        var options = testBase.GetRespawnOptions();

        // Assert
        options.ResetEncinaMessagingTables.ShouldBeTrue();
        options.TablesToIgnore.ShouldContain("CustomTable");
    }

    [Fact]
    public void Respawner_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => testBase.GetRespawner())
            .Message.ShouldContain("not initialized");
    }

    #region Test Helpers

    /// <summary>
    /// Testable wrapper for DatabaseIntegrationTestBase with SqlServerContainerFixture.
    /// </summary>
    private sealed class TestableSqlServerIntegrationTestBase : DatabaseIntegrationTestBase<SqlServerContainerFixture>
    {
        public TestableSqlServerIntegrationTestBase(SqlServerContainerFixture fixture) : base(fixture) { }

        public SqlServerContainerFixture GetFixture() => Fixture;
        public RespawnAdapter GetRespawnAdapter() => RespawnAdapter;
        public RespawnOptions GetRespawnOptions() => RespawnOptions;
        public DatabaseRespawner GetRespawner() => Respawner;
    }

    /// <summary>
    /// Testable wrapper with custom RespawnOptions.
    /// </summary>
    private sealed class TestableSqlServerIntegrationTestBaseWithCustomOptions : DatabaseIntegrationTestBase<SqlServerContainerFixture>
    {
        private readonly RespawnOptions _customOptions;

        public TestableSqlServerIntegrationTestBaseWithCustomOptions(
            SqlServerContainerFixture fixture,
            RespawnOptions customOptions) : base(fixture)
        {
            _customOptions = customOptions;
        }

        protected override RespawnOptions RespawnOptions => _customOptions;

        public RespawnOptions GetRespawnOptions() => RespawnOptions;
    }

    /// <summary>
    /// Testable wrapper for DatabaseIntegrationTestBase with PostgreSqlContainerFixture.
    /// </summary>
    private sealed class TestablePostgreSqlIntegrationTestBase : DatabaseIntegrationTestBase<PostgreSqlContainerFixture>
    {
        public TestablePostgreSqlIntegrationTestBase(PostgreSqlContainerFixture fixture) : base(fixture) { }

        public RespawnAdapter GetRespawnAdapter() => RespawnAdapter;
    }

    /// <summary>
    /// Testable wrapper for DatabaseIntegrationTestBase with MySqlContainerFixture.
    /// </summary>
    private sealed class TestableMySqlIntegrationTestBase : DatabaseIntegrationTestBase<MySqlContainerFixture>
    {
        public TestableMySqlIntegrationTestBase(MySqlContainerFixture fixture) : base(fixture) { }

        public RespawnAdapter GetRespawnAdapter() => RespawnAdapter;
    }

    /// <summary>
    /// Testable wrapper for unsupported fixture type (MongoDB doesn't support Respawn).
    /// </summary>
    private sealed class TestableMongoDbIntegrationTestBase : DatabaseIntegrationTestBase<MongoDbContainerFixture>
    {
        public TestableMongoDbIntegrationTestBase(MongoDbContainerFixture fixture) : base(fixture) { }

        public RespawnAdapter GetRespawnAdapter() => RespawnAdapter;
    }

    /// <summary>
    /// Testable wrapper for generic fixture.
    /// </summary>
    private sealed class TestableIntegrationTestBase : DatabaseIntegrationTestBase<SqlServerContainerFixture>
    {
        public TestableIntegrationTestBase(SqlServerContainerFixture fixture) : base(fixture) { }
    }

    #endregion
}
