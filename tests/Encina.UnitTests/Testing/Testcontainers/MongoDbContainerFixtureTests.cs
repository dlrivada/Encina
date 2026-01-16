using Encina.Testing.Testcontainers;
namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="MongoDbContainerFixture"/>.
/// </summary>
public class MongoDbContainerFixtureTests
{
    [Fact]
    public void Container_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new MongoDbContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.Container)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void ConnectionString_BeforeInitialize_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var fixture = new MongoDbContainerFixture();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = fixture.ConnectionString)
            .Message.ShouldContain("not initialized");
    }

    [Fact]
    public void IsRunning_BeforeInitialize_ShouldBeFalse()
    {
        // Arrange
        var fixture = new MongoDbContainerFixture();

        // Act & Assert
        fixture.IsRunning.ShouldBeFalse();
    }
}
