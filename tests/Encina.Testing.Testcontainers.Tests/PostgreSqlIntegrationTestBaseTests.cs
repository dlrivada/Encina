namespace Encina.Testing.Testcontainers.Tests;

/// <summary>
/// Unit tests for <see cref="PostgreSqlIntegrationTestBase"/>.
/// </summary>
public class PostgreSqlIntegrationTestBaseTests
{
    [Fact]
    public void Constructor_WithNullFixture_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestablePostgreSqlIntegrationTestBase(null!));
    }

    [Fact]
    public void PostgreSqlFixture_ShouldReturnProvidedFixture()
    {
        // Arrange - use a partial substitute to keep test fast (no real Docker container)
        var fixture = Substitute.ForPartsOf<PostgreSqlContainerFixture>();
        var testBase = new TestablePostgreSqlIntegrationTestBase(fixture);

        // Act
        var result = testBase.GetPostgreSqlFixture();

        // Assert
        result.ShouldBe(fixture);
    }

    [Fact]
    public void Fixture_ShouldBeSameAsPostgreSqlFixture()
    {
        // Arrange - use a partial substitute to keep test fast (no real Docker container)
        var fixture = Substitute.ForPartsOf<PostgreSqlContainerFixture>();
        var testBase = new TestablePostgreSqlIntegrationTestBase(fixture);

        // Act
        var fixtureResult = testBase.GetFixture();
        var postgreSqlFixtureResult = testBase.GetPostgreSqlFixture();

        // Assert
        fixtureResult.ShouldBe(postgreSqlFixtureResult);
    }

    #region Test Helpers

    private sealed class TestablePostgreSqlIntegrationTestBase : PostgreSqlIntegrationTestBase
    {
        public TestablePostgreSqlIntegrationTestBase(PostgreSqlContainerFixture fixture) : base(fixture) { }

        public PostgreSqlContainerFixture GetFixture() => Fixture;
        public PostgreSqlContainerFixture GetPostgreSqlFixture() => PostgreSqlFixture;
    }

    #endregion
}
