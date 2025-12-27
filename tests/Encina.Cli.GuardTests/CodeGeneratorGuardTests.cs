using Encina.Cli.Services;
using FluentAssertions;
using Xunit;

namespace Encina.Cli.GuardTests;

/// <summary>
/// Guard clause tests for CodeGenerator methods.
/// </summary>
public class CodeGeneratorGuardTests
{
    [Fact]
    public async Task GenerateCommandHandlerAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateCommandHandlerAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task GenerateCommandHandlerAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new HandlerOptions
        {
            Name = name!,
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => CodeGenerator.GenerateCommandHandlerAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateCommandHandlerAsync_InvalidOutputDirectory_ThrowsArgumentException(string? outputDir)
    {
        // Arrange
        var options = new HandlerOptions
        {
            Name = "TestCommand",
            OutputDirectory = outputDir!
        };

        // Act
        var act = () => CodeGenerator.GenerateCommandHandlerAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateQueryHandlerAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateQueryHandlerAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateQueryHandlerAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new QueryOptions
        {
            Name = name!,
            ResponseType = "OrderDto",
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => CodeGenerator.GenerateQueryHandlerAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateQueryHandlerAsync_InvalidResponseType_ThrowsArgumentException(string? responseType)
    {
        // Arrange
        var options = new QueryOptions
        {
            Name = "TestQuery",
            ResponseType = responseType!,
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => CodeGenerator.GenerateQueryHandlerAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateSagaAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateSagaAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateSagaAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new SagaOptions
        {
            Name = name!,
            Steps = ["Step1"],
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => CodeGenerator.GenerateSagaAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateSagaAsync_EmptySteps_ReturnsError()
    {
        // Arrange
        var options = new SagaOptions
        {
            Name = "TestSaga",
            Steps = [],
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var result = await CodeGenerator.GenerateSagaAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task GenerateNotificationAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateNotificationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateNotificationAsync_InvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var options = new NotificationOptions
        {
            Name = name!,
            OutputDirectory = Path.GetTempPath()
        };

        // Act
        var act = () => CodeGenerator.GenerateNotificationAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
