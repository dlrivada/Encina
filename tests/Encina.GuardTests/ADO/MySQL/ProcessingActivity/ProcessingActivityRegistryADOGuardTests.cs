using Encina.ADO.MySQL.ProcessingActivity;
using Shouldly;

namespace Encina.GuardTests.ADO.MySQL.ProcessingActivity;

/// <summary>
/// Guard tests for <see cref="ProcessingActivityRegistryADO"/> to verify null and invalid parameter handling.
/// </summary>
public class ProcessingActivityRegistryADOGuardTests
{
    [Fact]
    public void Constructor_NullConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ProcessingActivityRegistryADO(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_WhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new ProcessingActivityRegistryADO("  ");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ValidConnectionString_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new ProcessingActivityRegistryADO("Server=localhost;Database=test;"));
    }

    [Fact]
    public async Task RegisterActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryADO("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.RegisterActivityAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activity");
    }

    [Fact]
    public async Task GetActivityByRequestTypeAsync_NullRequestType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryADO("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.GetActivityByRequestTypeAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestType");
    }

    [Fact]
    public async Task UpdateActivityAsync_NullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProcessingActivityRegistryADO("Server=localhost;Database=test;");

        // Act & Assert
        var act = async () => await registry.UpdateActivityAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activity");
    }
}
