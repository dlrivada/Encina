using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="MartenEventMetadataQuery"/>.
/// </summary>
public sealed class MartenEventMetadataQueryTests
{
    private readonly IDocumentStore _mockStore;
    private readonly MartenEventMetadataQuery _query;

    public MartenEventMetadataQueryTests()
    {
        _mockStore = Substitute.For<IDocumentStore>();
        _query = new MartenEventMetadataQuery(
            _mockStore,
            NullLogger<MartenEventMetadataQuery>.Instance);
    }

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_NullCorrelationId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventsByCorrelationIdAsync(null!);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_EmptyCorrelationId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventsByCorrelationIdAsync("");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_WhitespaceCorrelationId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventsByCorrelationIdAsync("   ");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetEventsByCausationIdAsync_NullCausationId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventsByCausationIdAsync(null!);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetEventsByCausationIdAsync_EmptyCausationId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventsByCausationIdAsync("");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetCausalChainAsync_EmptyEventId_ReturnsError()
    {
        // Act
        var result = await _query.GetCausalChainAsync(Guid.Empty);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetCausalChainAsync_ZeroMaxDepth_ReturnsError()
    {
        // Act
        var result = await _query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 0);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetCausalChainAsync_NegativeMaxDepth_ReturnsError()
    {
        // Act
        var result = await _query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: -1);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetCausalChainAsync_MaxDepthExceeds1000_ReturnsError()
    {
        // Act
        var result = await _query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 1001);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public async Task GetEventByIdAsync_EmptyEventId_ReturnsError()
    {
        // Act
        var result = await _query.GetEventByIdAsync(Guid.Empty);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.InvalidQuery),
            () => throw new ShouldAssertException("Expected error code but got none"));
    }

    [Fact]
    public void CausalChainDirection_HasExpectedValues()
    {
        // Assert enum values exist
        CausalChainDirection.Ancestors.ShouldBe((CausalChainDirection)0);
        CausalChainDirection.Descendants.ShouldBe((CausalChainDirection)1);
    }

    [Fact]
    public void CausalChainDirection_DefaultIsAncestors()
    {
        // Assert default value
        default(CausalChainDirection).ShouldBe(CausalChainDirection.Ancestors);
    }
}
