using Encina.Testing.Testcontainers;
using Testcontainers.PostgreSql;

namespace Encina.UnitTests.Testing.Testcontainers;

/// <summary>
/// Unit tests for <see cref="ConfiguredContainerFixture{TContainer}"/>.
/// </summary>
public class ConfiguredContainerFixtureTests
{
    [Fact]
    public void Constructor_WithNullContainer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConfiguredContainerFixture<PostgreSqlContainer>(null!));
    }

    [Fact]
    public async Task Container_ShouldReturnProvidedContainer()
    {
        // Arrange
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true)
            .Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try
        {
            // Act
            var result = fixture.Container;

            // Assert
            result.ShouldBe(container);
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    [Fact]
    public async Task IsRunning_BeforeStart_ShouldBeFalse()
    {
        // Arrange
        var container = new PostgreSqlBuilder("postgres:16")
            .WithCleanUp(true)
            .Build();
        var fixture = new ConfiguredContainerFixture<PostgreSqlContainer>(container);

        try
        {
            // Act & Assert
            fixture.IsRunning.ShouldBeFalse();
        }
        finally
        {
            await container.DisposeAsync();
        }
    }
}
