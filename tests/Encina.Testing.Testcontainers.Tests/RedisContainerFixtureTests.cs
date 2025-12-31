namespace Encina.Testing.Testcontainers.Tests;

/// <summary>
/// Unit tests for <see cref="RedisContainerFixture"/>.
/// </summary>
public class RedisContainerFixtureTests
{
    [Fact]
    public void Container_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new RedisContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.Container)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void ConnectionString_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new RedisContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void IsRunning_BeforeInitialize_ShouldBeFalse()
    {
        // Arrange
        var fixture = new RedisContainerFixture();

        // Act & Assert
        fixture.IsRunning.ShouldBeFalse();
    }
}
