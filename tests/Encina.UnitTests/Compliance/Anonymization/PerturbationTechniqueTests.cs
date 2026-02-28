using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using FluentAssertions;

using LanguageExt;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="PerturbationTechnique"/>.
/// </summary>
public class PerturbationTechniqueTests
{
    private readonly PerturbationTechnique _technique = new();

    [Fact]
    public void Technique_ShouldReturnPerturbation()
    {
        // Act
        var result = _technique.Technique;

        // Assert
        result.Should().Be(AnonymizationTechnique.Perturbation);
    }

    [Fact]
    public void CanApply_IntType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(int));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanApply_StringType_ShouldReturnFalse()
    {
        // Act
        var result = _technique.CanApply(typeof(string));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_IntValue_ShouldReturnPerturbedValue()
    {
        // Arrange
        // Default NoiseRange is 0.1 (10%), so 100 should produce a value in [90, 110]
        var parameters = new Dictionary<string, object> { ["NoiseRange"] = 0.1 };

        // Act
        var result = await _technique.ApplyAsync(100, typeof(int), parameters, CancellationToken.None);

        // Assert
        result.Match(
            Right: v =>
            {
                var val = (int)v!;
                val.Should().BeInRange(90, 110);
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldThrowValueIsNullException()
    {
        // Arrange & Act
        Func<Task> act = async () =>
            await _technique.ApplyAsync(null, typeof(int), null, CancellationToken.None);

        // Assert
        // LanguageExt throws ValueIsNullException when Right(null) is called
        // and the null check is before the try/catch block
        await act.Should().ThrowAsync<ValueIsNullException>();
    }

    [Fact]
    public async Task ApplyAsync_WithNoiseRange_ShouldReturnWithinRange()
    {
        // Arrange
        // NoiseRange 0.05 (5%) on value 200 -> result should be in [190, 210]
        var parameters = new Dictionary<string, object> { ["NoiseRange"] = 0.05 };

        // Act
        var result = await _technique.ApplyAsync(200, typeof(int), parameters, CancellationToken.None);

        // Assert
        result.Match(
            Right: v =>
            {
                var val = (int)v!;
                val.Should().BeInRange(190, 210);
            },
            Left: _ => Assert.Fail("Expected Right"));
    }
}
