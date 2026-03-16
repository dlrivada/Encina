using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Compliance.DPIA.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Marten;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DefaultDPIAService"/> to verify null parameter handling
/// in the constructor.
/// </summary>
public class DefaultDPIAServiceGuardTests
{
    private readonly IAggregateRepository<DPIAAggregate> _aggregateRepository =
        Substitute.For<IAggregateRepository<DPIAAggregate>>();

    private readonly IReadModelRepository<DPIAReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DPIAReadModel>>();

    private readonly IDPIAAssessmentEngine _assessmentEngine =
        Substitute.For<IDPIAAssessmentEngine>();

    private readonly IDocumentSession _session =
        Substitute.For<IDocumentSession>();

    private readonly ICacheProvider _cache =
        Substitute.For<ICacheProvider>();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly IOptions<DPIAOptions> _options =
        Options.Create(new DPIAOptions());

    private readonly ILogger<DefaultDPIAService> _logger =
        NullLogger<DefaultDPIAService>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullAggregateRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            null!, _readModelRepository, _assessmentEngine, _session,
            _cache, _timeProvider, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("aggregateRepository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, null!, _assessmentEngine, _session,
            _cache, _timeProvider, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullAssessmentEngine_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, null!, _session,
            _cache, _timeProvider, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("assessmentEngine");
    }

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, _assessmentEngine, null!,
            _cache, _timeProvider, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, _assessmentEngine, _session,
            null!, _timeProvider, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, _assessmentEngine, _session,
            _cache, null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, _assessmentEngine, _session,
            _cache, _timeProvider, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPIAService(
            _aggregateRepository, _readModelRepository, _assessmentEngine, _session,
            _cache, _timeProvider, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
