using Encina.Dapper.SqlServer.ProcessingActivity;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.ProcessingActivity;

/// <summary>
/// Guard tests for <see cref="ProcessingActivityRegistryDapper"/> to verify null and invalid parameter handling.
/// </summary>
public class ProcessingActivityRegistryDapperGuardTests
{
    [Fact]
    public void Constructor_NullConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ProcessingActivityRegistryDapper(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new ProcessingActivityRegistryDapper("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidConnectionString_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new ProcessingActivityRegistryDapper("Server=localhost;Database=test;"));
    }

    [Fact]
    public async Task RegisterActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryDapper("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.RegisterActivityAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NullRequestType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryDapper("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.GetActivityByRequestTypeAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestType");
    }

    [Fact]
    public async Task UpdateActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryDapper("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.UpdateActivityAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activity");
    }
}
