using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Core;

/// <summary>
/// Guard tests for <see cref="MartenEventMetadataQuery"/> covering input validation
/// on all public query methods.
/// </summary>
public class MartenEventMetadataQueryGuardTests
{
    private static readonly IDocumentStore Store = Substitute.For<IDocumentStore>();
    private static readonly ILogger<MartenEventMetadataQuery> Logger = NullLogger<MartenEventMetadataQuery>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenEventMetadataQuery(null!, Logger));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenEventMetadataQuery(Store, null!));

    #endregion

    #region GetEventsByCorrelationIdAsync Guards

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_NullCorrelationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCorrelationIdAsync(null!);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_EmptyCorrelationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCorrelationIdAsync(string.Empty);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetEventsByCorrelationIdAsync_WhitespaceCorrelationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCorrelationIdAsync("   ");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetEventsByCausationIdAsync Guards

    [Fact]
    public async Task GetEventsByCausationIdAsync_NullCausationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCausationIdAsync(null!);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetEventsByCausationIdAsync_EmptyCausationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCausationIdAsync(string.Empty);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetEventsByCausationIdAsync_WhitespaceCausationId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventsByCausationIdAsync("   ");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetCausalChainAsync Guards

    [Fact]
    public async Task GetCausalChainAsync_EmptyGuidEventId_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.Empty);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCausalChainAsync_ZeroMaxDepth_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 0);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCausalChainAsync_NegativeMaxDepth_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: -1);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCausalChainAsync_ExceedingMaxDepth_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 1001);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCausalChainAsync_BoundaryMaxDepth1_DoesNotReturnValidationError()
    {
        // maxDepth=1 is valid, but may fail for other reasons (no store data)
        // This verifies no validation error message about depth is returned
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 1);

        result.Match(
            Right: _ => { }, // Success is fine
            Left: error => error.Message.ShouldNotContain("Max depth must be"));
    }

    [Fact]
    public async Task GetCausalChainAsync_BoundaryMaxDepth1000_DoesNotReturnValidationError()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetCausalChainAsync(Guid.NewGuid(), maxDepth: 1000);

        result.Match(
            Right: _ => { },
            Left: error => error.Message.ShouldNotContain("Max depth must be"));
    }

    #endregion

    #region GetEventByIdAsync Guards

    [Fact]
    public async Task GetEventByIdAsync_EmptyGuid_ReturnsLeft()
    {
        var query = new MartenEventMetadataQuery(Store, Logger);
        var result = await query.GetEventByIdAsync(Guid.Empty);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
