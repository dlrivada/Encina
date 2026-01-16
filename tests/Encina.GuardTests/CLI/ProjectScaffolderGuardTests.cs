using Encina.Cli.Services;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard clause tests for ProjectScaffolder methods.
/// </summary>
public class ProjectScaffolderGuardTests
{
    [Fact]
    public async Task CreateProjectAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProjectScaffolder.CreateProjectAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task CreateProjectAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = "api",
            Name = name!,
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(options.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProjectAsync_InvalidTemplate_ThrowsArgumentException(string? template)
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = template!,
            Name = "TestProject",
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(options.Template));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProjectAsync_InvalidOutputDirectory_ThrowsArgumentException(string? outputDir)
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = "api",
            Name = "TestProject",
            OutputDirectory = outputDir!
        };

        // Act
        var act = () => ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(options.OutputDirectory));
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownTemplate_ReturnsError()
    {
        // Arrange
        var options = new ProjectOptions
        {
            Template = "unknown",
            Name = "TestProject",
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var result = await ProjectScaffolder.CreateProjectAsync(options);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Unknown template");
    }
}
