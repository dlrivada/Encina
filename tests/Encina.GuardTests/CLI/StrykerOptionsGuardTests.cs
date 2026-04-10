using Encina.Cli.Services;

using Shouldly;

using Xunit;

namespace Encina.GuardTests.CLI;

/// <summary>
/// Guard tests for <see cref="StrykerOptions"/> and <see cref="CodeGenerator.GenerateStrykerConfigAsync"/>.
/// </summary>
public class StrykerOptionsGuardTests
{
    [Fact]
    public void StrykerOptions_Defaults_AreSensible()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/X/X.csproj",
            OutputDirectory = "."
        };

        options.ThresholdHigh.ShouldBe(80);
        options.ThresholdLow.ShouldBe(60);
        options.ThresholdBreak.ShouldBe(50);
        options.UseAdvanced.ShouldBeFalse();
        options.TestProjects.ShouldBeEmpty();
    }

    [Fact]
    public void StrykerOptions_CanSetAllProperties()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "p",
            OutputDirectory = "o",
            ThresholdHigh = 90,
            ThresholdLow = 70,
            ThresholdBreak = 55,
            UseAdvanced = true,
            TestProjects = ["a", "b"]
        };

        options.ProjectPath.ShouldBe("p");
        options.OutputDirectory.ShouldBe("o");
        options.ThresholdHigh.ShouldBe(90);
        options.ThresholdLow.ShouldBe(70);
        options.ThresholdBreak.ShouldBe(55);
        options.UseAdvanced.ShouldBeTrue();
        options.TestProjects.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateStrykerConfigAsync_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => CodeGenerator.GenerateStrykerConfigAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateStrykerConfigAsync_InvalidProjectPath_Throws(string? project)
    {
        var options = new StrykerOptions
        {
            ProjectPath = project!,
            OutputDirectory = Path.GetTempPath()
        };

        await Should.ThrowAsync<ArgumentException>(() => CodeGenerator.GenerateStrykerConfigAsync(options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateStrykerConfigAsync_InvalidOutputDirectory_Throws(string? output)
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/MyApp/MyApp.csproj",
            OutputDirectory = output!
        };

        await Should.ThrowAsync<ArgumentException>(() => CodeGenerator.GenerateStrykerConfigAsync(options));
    }

    // Invalid threshold combinations return an error result (not an exception).

    [Theory]
    [InlineData(80, 60, 70)] // break > low
    [InlineData(60, 80, 50)] // low > high
    [InlineData(80, 60, -1)] // break < 0
    [InlineData(101, 60, 50)] // high > 100
    public async Task GenerateStrykerConfigAsync_InvalidThresholds_ReturnsError(int high, int low, int @break)
    {
        var options = new StrykerOptions
        {
            ProjectPath = "src/X/X.csproj",
            OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            ThresholdHigh = high,
            ThresholdLow = low,
            ThresholdBreak = @break
        };

        Directory.CreateDirectory(options.OutputDirectory);
        try
        {
            var result = await CodeGenerator.GenerateStrykerConfigAsync(options);
            result.Success.ShouldBeFalse();
            result.ErrorMessage!.ShouldContain("Invalid thresholds");
        }
        finally
        {
            if (Directory.Exists(options.OutputDirectory))
            {
                Directory.Delete(options.OutputDirectory, recursive: true);
            }
        }
    }
}
