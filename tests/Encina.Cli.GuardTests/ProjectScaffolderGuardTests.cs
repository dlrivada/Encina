using Encina.Cli.Services;
using FluentAssertions;
using Xunit;

namespace Encina.Cli.GuardTests;

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
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
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
        await act.Should().ThrowAsync<ArgumentException>();
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
        await act.Should().ThrowAsync<ArgumentException>();
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
        await act.Should().ThrowAsync<ArgumentException>();
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
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown template");
    }
}
