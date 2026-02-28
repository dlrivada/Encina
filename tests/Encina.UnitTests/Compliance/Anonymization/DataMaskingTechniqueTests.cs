using Encina.Compliance.Anonymization.Model;
using Encina.Compliance.Anonymization.Techniques;

using FluentAssertions;

using LanguageExt;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="DataMaskingTechnique"/>.
/// </summary>
public class DataMaskingTechniqueTests
{
    private readonly DataMaskingTechnique _technique = new();

    [Fact]
    public void Technique_ShouldReturnDataMasking()
    {
        // Act
        var result = _technique.Technique;

        // Assert
        result.Should().Be(AnonymizationTechnique.DataMasking);
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
    public void CanApply_IntType_ShouldReturnFalse()
    {
        // Act
        var result = _technique.CanApply(typeof(int));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_StringValue_ShouldMask()
    {
        // Arrange - Default: PreserveStart=1, PreserveEnd=0, MaskChar='*'
        // "John Doe" (8 chars) -> "J*******"

        // Act
        var result = await _technique.ApplyAsync("John Doe", typeof(string), null, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).Should().Be("J*******"),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task ApplyAsync_NullValue_ShouldThrowValueIsNullException()
    {
        // Arrange & Act
        Func<Task> act = async () =>
            await _technique.ApplyAsync(null, typeof(string), null, CancellationToken.None);

        // Assert
        // LanguageExt throws ValueIsNullException when Right(null) is called
        // and the null check is before the try/catch block
        await act.Should().ThrowAsync<ValueIsNullException>();
    }

    [Fact]
    public async Task ApplyAsync_EmptyString_ShouldReturnEmpty()
    {
        // Arrange & Act
        var result = await _technique.ApplyAsync(string.Empty, typeof(string), null, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => ((string)v!).Should().BeEmpty(),
            Left: _ => Assert.Fail("Expected Right"));
    }
}
