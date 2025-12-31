namespace Encina.Testing.Testcontainers.Tests;

/// <summary>
/// Unit tests for <see cref="SqlServerContainerFixture"/>.
/// </summary>
/// <remarks>
/// These tests verify the fixture behavior without starting actual containers.
/// Integration tests that start real containers are in a separate project.
/// </remarks>
public class SqlServerContainerFixtureTests
{
    [Fact]
    public void Container_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.Container)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void ConnectionString_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void IsRunning_BeforeInitialize_ShouldBeFalse()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();

        // Act & Assert
        fixture.IsRunning.ShouldBeFalse();
    }
}
