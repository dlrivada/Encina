using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using Shouldly;

using LanguageExt;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="GeneralizationTechnique"/>.
/// </summary>
public class GeneralizationTechniqueTests
{
    private readonly GeneralizationTechnique _technique = new();

    [Fact]
    public void Technique_ShouldReturnGeneralization()
    {
        // Act
        var result = _technique.Technique;

        // Assert
        result.ShouldBe(AnonymizationTechnique.Generalization);
    }

    [Fact]
    public void CanApply_IntType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(int));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_DoubleType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(double));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_DateTimeType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(DateTime));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_StringType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(string));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanApply_BoolType_ShouldReturnFalse()
    {
        // Act
        var result = _technique.CanApply(typeof(bool));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsync_IntWithGranularity10_ShouldRoundToRange()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Granularity"] = 10 };

        // Act
        var result = await _technique.ApplyAsync(25, typeof(int), parameters, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).ShouldBe("20-29"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_IntWithGranularity5_ShouldRoundToRange()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Granularity"] = 5 };

        // Act
        var result = await _technique.ApplyAsync(17, typeof(int), parameters, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).ShouldBe("15-19"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldThrowValueIsNullException()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Granularity"] = 10 };

        // Act
        Func<Task> act = async () =>
            await _technique.ApplyAsync(null, typeof(int), parameters, CancellationToken.None);

        // Assert
        // LanguageExt throws ValueIsNullException when Right(null) is called
        // and the null check is before the try/catch block
        await Should.ThrowAsync<ValueIsNullException>(act);
    }

    [Fact]
    public async Task ApplyAsync_NoParameters_ShouldUseDefaultGranularity()
    {
        // Arrange - No parameters, default granularity is 10

        // Act
        var result = await _technique.ApplyAsync(25, typeof(int), null, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).ShouldBe("20-29"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_DoubleValue_ShouldGeneralizeToRange()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Granularity"] = 10 };

        // Act
        var result = await _technique.ApplyAsync(25.7, typeof(double), parameters, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).ShouldBe("20-29"),
            Left: _ => Assert.Fail("Expected Right"));
    }
}
