using Encina.Testing.Testcontainers;
namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="SqlServerIntegrationTestBase"/>.
/// </summary>
public class SqlServerIntegrationTestBaseTests
{
    [Fact]
    public void Constructor_WithNullFixture_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestableSqlServerIntegrationTestBase(null!));
    }

    [Fact]
    public void SqlServerFixture_ShouldReturnProvidedFixture()
    {
        // Arrange - use a substitute with constructor args to keep test fast (no real Docker container)
        var fixture = Substitute.ForPartsOf<SqlServerContainerFixture>(null, null);
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Act
        var result = testBase.GetSqlServerFixture();

        // Assert
        result.ShouldBe(fixture);
    }

    [Fact]
    public void Fixture_ShouldBeSameAsSqlServerFixture()
    {
        // Arrange - use a substitute with constructor args to keep test fast (no real Docker container)
        var fixture = Substitute.ForPartsOf<SqlServerContainerFixture>(null, null);
        var testBase = new TestableSqlServerIntegrationTestBase(fixture);

        // Act
        var fixtureResult = testBase.GetFixture();
        var sqlServerFixtureResult = testBase.GetSqlServerFixture();

        // Assert
        fixtureResult.ShouldBe(sqlServerFixtureResult);
    }

    #region Test Helpers

    private sealed class TestableSqlServerIntegrationTestBase : SqlServerIntegrationTestBase
    {
        public TestableSqlServerIntegrationTestBase(SqlServerContainerFixture fixture) : base(fixture) { }

        public SqlServerContainerFixture GetFixture() => Fixture;
        public SqlServerContainerFixture GetSqlServerFixture() => SqlServerFixture;
    }

    #endregion
}
