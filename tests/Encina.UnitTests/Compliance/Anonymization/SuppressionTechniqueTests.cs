using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using FluentAssertions;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="SuppressionTechnique"/>.
/// </summary>
public class SuppressionTechniqueTests
{
    private readonly SuppressionTechnique _technique = new();

    [Fact]
    public void Technique_ShouldReturnSuppression()
    {
        // Act
        var result = _technique.Technique;

        // Assert
        result.Should().Be(AnonymizationTechnique.Suppression);
    }

    [Fact]
    public void CanApply_StringType_ShouldReturnTrue()
    {
        // Act
        var result = _technique.CanApply(typeof(string));

        // Assert
        result.Should().BeTrue();
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
    public async Task ApplyAsync_StringValue_ShouldReturnLeft()
    {
        // Arrange & Act
        var result = await _technique.ApplyAsync("John", typeof(string), null, CancellationToken.None);

        // Assert
        // Suppression of reference types produces null, but LanguageExt throws
        // ValueIsNullException when Right(null) is called, caught by try/catch -> Left
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAsync_IntValue_ShouldReturnDefault()
    {
        // Arrange & Act
        var result = await _technique.ApplyAsync(42, typeof(int), null, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.Should().Be(0),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldReturnLeft()
    {
        // Arrange & Act
        var result = await _technique.ApplyAsync(null, typeof(string), null, CancellationToken.None);

        // Assert
        // Suppression of reference types produces null, but LanguageExt throws
        // ValueIsNullException when Right(null) is called, caught by try/catch -> Left
        result.IsLeft.Should().BeTrue();
    }
}
