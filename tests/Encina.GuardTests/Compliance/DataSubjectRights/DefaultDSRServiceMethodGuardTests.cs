using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Deep method-level guard tests for <see cref="DefaultDSRService"/>.
/// Exercises method-level null/empty checks on handler operations.
/// </summary>
public class DefaultDSRServiceMethodGuardTests
{
    private readonly DefaultDSRService _sut;

    public DefaultDSRServiceMethodGuardTests()
    {
        _sut = new DefaultDSRService(
            Substitute.For<IAggregateRepository<DSRRequestAggregate>>(),
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            TimeProvider.System,
            NullLogger<DefaultDSRService>.Instance);
    }

    // --- HandleAccessAsync ---

    [Fact]
    public async Task HandleAccessAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandleAccessAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    // --- HandleRectificationAsync ---

    [Fact]
    public async Task HandleRectificationAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandleRectificationAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    // --- HandleErasureAsync ---

    [Fact]
    public async Task HandleErasureAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandleErasureAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    // --- HandleRestrictionAsync ---

    [Fact]
    public async Task HandleRestrictionAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandleRestrictionAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    // --- HandlePortabilityAsync ---

    [Fact]
    public async Task HandlePortabilityAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandlePortabilityAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    // --- HandleObjectionAsync ---

    [Fact]
    public async Task HandleObjectionAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.HandleObjectionAsync(null!).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }
}
