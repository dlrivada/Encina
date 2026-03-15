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
/// Guard tests for <see cref="DefaultDSRService"/> to verify null parameter handling.
/// </summary>
public class DefaultDSRServiceGuardTests
{
    private readonly IAggregateRepository<DSRRequestAggregate> _repository =
        Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
    private readonly IReadModelRepository<DSRRequestReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
    private readonly IPersonalDataLocator _locator = Substitute.For<IPersonalDataLocator>();
    private readonly IDataErasureExecutor _erasureExecutor = Substitute.For<IDataErasureExecutor>();
    private readonly IDataPortabilityExporter _portabilityExporter = Substitute.For<IDataPortabilityExporter>();
    private readonly IProcessingActivityRegistry _processingActivityRegistry =
        Substitute.For<IProcessingActivityRegistry>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            null!, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, null!, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullLocator_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, null!, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("locator");
    }

    [Fact]
    public void Constructor_NullErasureExecutor_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, null!,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("erasureExecutor");
    }

    [Fact]
    public void Constructor_NullPortabilityExporter_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            null!, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("portabilityExporter");
    }

    [Fact]
    public void Constructor_NullProcessingActivityRegistry_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, null!, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processingActivityRegistry");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, null!,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            null!, NullLogger<DefaultDSRService>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullEncina_ShouldNotThrow()
    {
        var act = () => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            TimeProvider.System, NullLogger<DefaultDSRService>.Instance, encina: null);

        Should.NotThrow(act);
    }

    #endregion
}
