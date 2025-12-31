namespace Encina.Testing.Testcontainers.Tests;

/// <summary>
/// Unit tests for <see cref="MySqlIntegrationTestBase"/>.
/// </summary>
public class MySqlIntegrationTestBaseTests
{
    [Fact]
    public void Constructor_WithNullFixture_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestableMySqlIntegrationTestBase(null!));
    }

    [Fact]
    public void MySqlFixture_ShouldReturnProvidedFixture()
    {
        // Arrange
        var fixture = new MySqlContainerFixture();
        var testBase = new TestableMySqlIntegrationTestBase(fixture);

        // Act
        var result = testBase.GetMySqlFixture();

        // Assert
        result.ShouldBe(fixture);
    }

    [Fact]
    public void Fixture_ShouldBeSameAsMySqlFixture()
    {
        // Arrange
        var fixture = new MySqlContainerFixture();
        var testBase = new TestableMySqlIntegrationTestBase(fixture);

        // Act
        var fixtureResult = testBase.GetFixture();
        var mySqlFixtureResult = testBase.GetMySqlFixture();

        // Assert
        fixtureResult.ShouldBe(mySqlFixtureResult);
    }

    #region Test Helpers

    private sealed class TestableMySqlIntegrationTestBase : MySqlIntegrationTestBase
    {
        public TestableMySqlIntegrationTestBase(MySqlContainerFixture fixture) : base(fixture) { }

        public MySqlContainerFixture GetFixture() => Fixture;
        public MySqlContainerFixture GetMySqlFixture() => MySqlFixture;
    }

    #endregion
}
