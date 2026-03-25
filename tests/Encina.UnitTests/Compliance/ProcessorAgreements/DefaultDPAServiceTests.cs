using Encina.Caching;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DefaultDPAService"/>.
/// </summary>
public sealed class DefaultDPAServiceTests
{
    private readonly IAggregateRepository<DPAAggregate> _repository;
    private readonly IReadModelRepository<DPAReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IOptions<ProcessorAgreementOptions> _options;
    private readonly ILogger<DefaultDPAService> _logger;
    private readonly DefaultDPAService _sut;

    public DefaultDPAServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<DPAAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DPAReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero));
        _options = Options.Create(new ProcessorAgreementOptions { ExpirationWarningDays = 30 });
        _logger = NullLogger<DefaultDPAService>.Instance;

        _sut = new DefaultDPAService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _options,
            _logger);
    }

    // ========================================================================
    // Constructor guard tests
    // ========================================================================

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            null!, _readModelRepository, _cache, _timeProvider, _options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            _repository, null!, _cache, _timeProvider, _options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            _repository, _readModelRepository, null!, _timeProvider, _options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            _repository, _readModelRepository, _cache, null!, _options, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            _repository, _readModelRepository, _cache, _timeProvider, null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAService(
            _repository, _readModelRepository, _cache, _timeProvider, _options, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ========================================================================
    // ExecuteDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task ExecuteDPAAsync_WhenRepositorySucceeds_ShouldReturnGuid()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        _repository.CreateAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.ExecuteDPAAsync(
            processorId, terms, true, ["analytics"], _timeProvider.GetUtcNow(), null);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteDPAAsync_WhenRepositoryFails_ShouldReturnError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        var error = EncinaErrors.Create("test.error", "Repository failed");
        _repository.CreateAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act
        var result = await _sut.ExecuteDPAAsync(
            processorId, terms, true, ["analytics"], _timeProvider.GetUtcNow(), null);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

#pragma warning disable CA2201
    [Fact]
    public async Task ExecuteDPAAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        _repository.CreateAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection failed"));

        // Act
        var result = await _sut.ExecuteDPAAsync(
            processorId, terms, true, ["analytics"], _timeProvider.GetUtcNow(), null);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201

    // ========================================================================
    // GetDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task GetDPAAsync_WhenCached_ShouldReturnCachedValue()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var readModel = CreateDPAReadModel(dpaId);
        _cache.GetAsync<DPAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(readModel);

        // Act
        var result = await _sut.GetDPAAsync(dpaId);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _readModelRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDPAAsync_WhenNotCached_ShouldQueryRepository()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var readModel = CreateDPAReadModel(dpaId);
        _cache.GetAsync<DPAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DPAReadModel?)null);
        _readModelRepository.GetByIdAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAReadModel>(readModel));

        // Act
        var result = await _sut.GetDPAAsync(dpaId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDPAAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        _cache.GetAsync<DPAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DPAReadModel?)null);
        var error = EncinaErrors.Create("not_found", "Not found");
        _readModelRepository.GetByIdAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPAReadModel>(error));

        // Act
        var result = await _sut.GetDPAAsync(dpaId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.dpa_not_found");
    }

    // ========================================================================
    // GetDPAsByProcessorIdAsync tests
    // ========================================================================

#pragma warning disable CA2201
    [Fact]
    public async Task GetDPAsByProcessorIdAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPAReadModel>, IQueryable<DPAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Query failed"));

        // Act
        var result = await _sut.GetDPAsByProcessorIdAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
#pragma warning restore CA2201

    // ========================================================================
    // GetDPAHistoryAsync tests
    // ========================================================================

    [Fact]
    public async Task GetDPAHistoryAsync_ShouldReturnNotAvailableError()
    {
        // Act
        var result = await _sut.GetDPAHistoryAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }

    // ========================================================================
    // HasValidDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task HasValidDPAAsync_WhenNoActiveDPA_ShouldReturnFalse()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _cache.GetAsync<DPAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DPAReadModel?)null);
        var emptyList = new List<DPAReadModel>() as IReadOnlyList<DPAReadModel>;
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPAReadModel>, IQueryable<DPAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DPAReadModel>>(emptyList));

        // Act
        var result = await _sut.HasValidDPAAsync(processorId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v, Left: _ => true).ShouldBeFalse();
    }

    // ========================================================================
    // GetExpiringDPAsAsync tests
    // ========================================================================

    [Fact]
    public async Task GetExpiringDPAsAsync_ShouldQueryWithWarningThreshold()
    {
        // Arrange
        var dpas = new List<DPAReadModel>() as IReadOnlyList<DPAReadModel>;
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPAReadModel>, IQueryable<DPAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DPAReadModel>>(dpas));

        // Act
        var result = await _sut.GetExpiringDPAsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static DPAMandatoryTerms CreateFullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    private static DPAReadModel CreateDPAReadModel(Guid id) => new()
    {
        Id = id,
        ProcessorId = Guid.NewGuid(),
        Status = DPAStatus.Active,
        MandatoryTerms = CreateFullyCompliantTerms(),
        HasSCCs = true,
        ProcessingPurposes = ["analytics"],
        SignedAtUtc = DateTimeOffset.UtcNow,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastModifiedAtUtc = DateTimeOffset.UtcNow
    };
}
