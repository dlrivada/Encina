using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using FluentAssertions;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="SwappingTechnique"/>.
/// </summary>
public class SwappingTechniqueTests
{
    private readonly SwappingTechnique _technique = new();

    [Fact]
    public void Technique_ShouldReturnSwapping()
    {
        // Act
        var result = _technique.Technique;

        // Assert
        result.Should().Be(AnonymizationTechnique.Swapping);
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
        // Arrange - Single-record swapping replaces reference types with null

        // Act
        var result = await _technique.ApplyAsync("John Doe", typeof(string), null, CancellationToken.None);

        // Assert
        // Swapping produces null for reference types, but LanguageExt throws
        // ValueIsNullException when Right(null) is called, caught by try/catch -> Left
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldReturnLeft()
    {
        // Arrange & Act
        var result = await _technique.ApplyAsync(null, typeof(string), null, CancellationToken.None);

        // Assert
        // Swapping produces null for reference types, but LanguageExt throws
        // ValueIsNullException when Right(null) is called, caught by try/catch -> Left
        result.IsLeft.Should().BeTrue();
    }
}
