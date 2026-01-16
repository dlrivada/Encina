using Encina.Cli.Services;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.CLI;

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
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(options.Name));
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
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GenerateQueryHandlerAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateQueryHandlerAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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
        await Should.ThrowAsync<ArgumentException>(act);
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
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GenerateSagaAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateSagaAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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
        await Should.ThrowAsync<ArgumentException>(act);
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
        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("At least one step is required");
    }

    [Fact]
    public async Task GenerateNotificationAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CodeGenerator.GenerateNotificationAsync(null!);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
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
        await Should.ThrowAsync<ArgumentException>(act);
    }
}
