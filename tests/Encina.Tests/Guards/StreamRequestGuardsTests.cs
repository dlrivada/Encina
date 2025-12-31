using Shouldly;

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
        result.ShouldBeTrue();
        error.ShouldBeBottom(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateStreamRequest_WithNullRequest_ShouldReturnFalse()
    {
        // Arrange
        IStreamRequest<int>? request = null;

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(request, out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldBeError();

        var EncinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"));

        EncinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull);
        EncinaError.Message.ShouldContain("stream request");
        EncinaError.Message.ShouldContain("cannot be null");
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
        result.ShouldBeTrue();
        error.ShouldBeBottom(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateStreamRequest_WithDifferentItemTypes_ShouldHandleCorrectly()
    {
        // Arrange
        IStreamRequest<int>? request1 = new TestStreamRequest();
        IStreamRequest<string>? request2 = null;

        // Act
        var result1 = EncinaRequestGuards.TryValidateStreamRequest<int>(request1, out _);
        var result2 = EncinaRequestGuards.TryValidateStreamRequest<string>(request2, out var error2);

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeFalse();

        error2.ShouldBeError();
    }

    [Fact]
    public void TryValidateStreamRequest_ErrorMessage_ShouldBeDescriptive()
    {
        // Arrange
        IStreamRequest<int>? request = null;

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(request, out var error);

        // Assert
        result.ShouldBeFalse();

        var EncinaError = error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException("Expected Left"));

        EncinaError.Message.ShouldBe("The stream request cannot be null.");
        EncinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull);
    }

    [Fact]
    public void TryValidateStreamRequest_WithComplexType_ShouldValidate()
    {
        // Arrange
        var complexRequest = new ComplexStreamRequest(ComplexRequestValues, DateTime.UtcNow);

        // Act
        var result = EncinaRequestGuards.TryValidateStreamRequest<int>(complexRequest, out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom(); // Default Either is neither Left nor Right
    }

    #endregion

    #region Test Data

    private static readonly int[] ComplexRequestValues = [1, 2, 3];

    private sealed record TestStreamRequest : IStreamRequest<int>;

    private sealed record ComplexStreamRequest(int[] Values, DateTime Timestamp) : IStreamRequest<int>;

    #endregion
}
