using FluentAssertions;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for Stream Request validation in <see cref="EncinaRequestGuards"/>.
/// Tests ensure proper null validation and error handling for streaming requests.
/// </summary>
public sealed class StreamRequestGuardsTests
{
    #region TryValidateStreamRequest Tests

    [Fact]
    public void TryValidateStreamRequest_WithValidRequest_ShouldReturnTrue()
    {
        // Arrange
        var request = new TestStreamRequest();

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(request, out var error);

        // Assert
        result.Should().BeTrue();
        error.IsLeft.Should().BeFalse();
        error.IsRight.Should().BeFalse(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateStreamRequest_WithNullRequest_ShouldReturnFalse()
    {
        // Arrange
        IStreamRequest<int>? request = null;

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(request, out var error);

        // Assert
        result.Should().BeFalse();
        error.IsLeft.Should().BeTrue();

        var EncinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"));

        EncinaError.GetEncinaCode().Should().Be(EncinaErrorCodes.RequestNull);
        EncinaError.Message.Should().Contain("stream request");
        EncinaError.Message.Should().Contain("cannot be null");
    }

    [Fact]
    public void TryValidateStreamRequest_WithValidGenericRequest_ShouldReturnTrue()
    {
        // Arrange
        var request = new TestStreamRequest();

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<string>(request, out var error);

        // Assert - request is valid even though TItem type doesn't match
        // (type checking is done elsewhere in the pipeline)
        result.Should().BeTrue();
        error.IsLeft.Should().BeFalse();
        error.IsRight.Should().BeFalse(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateStreamRequest_WithDifferentItemTypes_ShouldHandleCorrectly()
    {
        // Arrange
        IStreamRequest<int>? request1 = new TestStreamRequest();
        IStreamRequest<string>? request2 = null;

        // Act
        var result1 = EncinaRequestGuards.TryValidateStreamRequest<int>(request1, out var error1);
        var result2 = EncinaRequestGuards.TryValidateStreamRequest<string>(request2, out var error2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();

        error2.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void TryValidateStreamRequest_ErrorMessage_ShouldBeDescriptive()
    {
        // Arrange
        IStreamRequest<int>? request = null;

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(request, out var error);

        // Assert
        result.Should().BeFalse();

        var EncinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        EncinaError.Message.Should().Be("The stream request cannot be null.");
        EncinaError.GetEncinaCode().Should().Be(EncinaErrorCodes.RequestNull);
    }

    [Fact]
    public void TryValidateStreamRequest_WithComplexType_ShouldValidate()
    {
        // Arrange
        var complexRequest = new ComplexStreamRequest(ComplexRequestValues, DateTime.UtcNow);

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(complexRequest, out var error);

        // Assert
        result.Should().BeTrue();
        error.IsLeft.Should().BeFalse();
        error.IsRight.Should().BeFalse(); // Default Either is neither Left nor Right
    }

    #endregion

    #region Test Data

    private static readonly int[] ComplexRequestValues = [1, 2, 3];

    private sealed record TestStreamRequest : IStreamRequest<int>;

    private sealed record ComplexStreamRequest(int[] Values, DateTime Timestamp) : IStreamRequest<int>;

    #endregion
}
