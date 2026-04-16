using Encina.Caching;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Compliance.BreachNotification.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="DefaultBreachNotificationService"/>.
/// </summary>
public sealed class DefaultBreachNotificationServiceTests
{
    private readonly IAggregateRepository<BreachAggregate> _repository;
    private readonly IReadModelRepository<BreachReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultBreachNotificationService> _logger;
    private readonly DefaultBreachNotificationService _sut;

    public DefaultBreachNotificationServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<BreachAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<BreachReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultBreachNotificationService>.Instance;

        _sut = new DefaultBreachNotificationService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _logger);
    }

    // ========================================================================
    // Constructor guard tests
    // ========================================================================

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
            null!,
            _readModelRepository,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
            _repository,
            null!,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
            _repository,
            _readModelRepository,
            null!,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    // ========================================================================
    // RecordBreachAsync tests
    // ========================================================================

    #region RecordBreachAsync

    [Fact]
    public async Task RecordBreachAsync_Success_ReturnsBreachId()
    {
        _repository
            .CreateAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.RecordBreachAsync(
            "unauthorized access",
            BreachSeverity.High,
            "rule-001",
            100,
            "Data was exfiltrated via API endpoint");

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.ShouldNotBeEmpty(), _ => { });
        await _repository.Received(1).CreateAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordBreachAsync_RepositoryError_ReturnsLeft()
    {
        var error = EncinaError.New("create failed");
        _repository
            .CreateAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        var result = await _sut.RecordBreachAsync(
            "unauthorized access",
            BreachSeverity.High,
            "rule-001",
            100,
            "Data was exfiltrated");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    // ========================================================================
    // AssessBreachAsync tests (representative load-modify-save pattern)
    // ========================================================================

    #region AssessBreachAsync

    [Fact]
    public async Task AssessBreachAsync_Success_ReturnsRight()
    {
        var breachId = Guid.NewGuid();
        var aggregate = CreateDetectedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.AssessBreachAsync(
            breachId,
            BreachSeverity.Critical,
            500,
            "Assessment complete",
            "user-42");

        result.IsRight.ShouldBeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssessBreachAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();
        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachAggregate>(EncinaError.New("not found")));

        var result = await _sut.AssessBreachAsync(
            breachId,
            BreachSeverity.Critical,
            500,
            "Assessment complete",
            "user-42");

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task AssessBreachAsync_InvalidState_ReturnsLeftInvalidStateTransition()
    {
        // Create an aggregate that is NOT in Detected status (e.g., Investigating)
        var breachId = Guid.NewGuid();
        var aggregate = CreateAssessedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));

        var result = await _sut.AssessBreachAsync(
            breachId,
            BreachSeverity.Critical,
            500,
            "Second assessment attempt",
            "user-42");

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("Invalid state transition"));
    }

    #endregion

    // ========================================================================
    // GetBreachAsync tests (cache-aside pattern)
    // ========================================================================

    #region GetBreachAsync

    [Fact]
    public async Task GetBreachAsync_CacheHit_ReturnsCachedValue()
    {
        var breachId = Guid.NewGuid();
        var cachedModel = CreateBreachReadModel(breachId);

        _cache
            .GetAsync<BreachReadModel>($"breach:{breachId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        var result = await _sut.GetBreachAsync(breachId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(breachId), _ => { });

        // Should NOT call the read model repository when cache hit
        await _readModelRepository
            .DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBreachAsync_CacheMiss_FallsBackToRepository()
    {
        var breachId = Guid.NewGuid();
        var readModel = CreateBreachReadModel(breachId);

        _cache
            .GetAsync<BreachReadModel>($"breach:{breachId}", Arg.Any<CancellationToken>())
            .Returns((BreachReadModel?)null);
        _readModelRepository
            .GetByIdAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachReadModel>(readModel));

        var result = await _sut.GetBreachAsync(breachId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(breachId), _ => { });

        // Should cache the result
        await _cache.Received(1).SetAsync(
            $"breach:{breachId}",
            readModel,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBreachAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();

        _cache
            .GetAsync<BreachReadModel>($"breach:{breachId}", Arg.Any<CancellationToken>())
            .Returns((BreachReadModel?)null);
        _readModelRepository
            .GetByIdAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachReadModel>(EncinaError.New("not found")));

        var result = await _sut.GetBreachAsync(breachId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    #endregion

    // ========================================================================
    // GetBreachHistoryAsync tests
    // ========================================================================

    #region GetBreachHistoryAsync

    [Fact]
    public async Task GetBreachHistoryAsync_ReturnsEventHistoryUnavailableError()
    {
        var breachId = Guid.NewGuid();

        var result = await _sut.GetBreachHistoryAsync(breachId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not available"));
    }

    #endregion

    // ========================================================================
    // GetApproachingDeadlineBreachesAsync tests
    // ========================================================================

    #region GetApproachingDeadlineBreachesAsync

    [Fact]
    public async Task GetApproachingDeadlineBreachesAsync_DelegatesToReadModelRepository()
    {
        var expectedList = new List<BreachReadModel> { CreateBreachReadModel(Guid.NewGuid()) };

        _readModelRepository
            .QueryAsync(Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(expectedList));

        var result = await _sut.GetApproachingDeadlineBreachesAsync();

        result.IsRight.ShouldBeTrue();
        await _readModelRepository.Received(1).QueryAsync(
            Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    // ========================================================================
    // Helpers
    // ========================================================================

    private BreachAggregate CreateDetectedAggregate(Guid id)
    {
        return BreachAggregate.Detect(
            id,
            "unauthorized access",
            BreachSeverity.High,
            "rule-001",
            100,
            "Test breach description",
            "user-1",
            _timeProvider.GetUtcNow());
    }

    private BreachAggregate CreateAssessedAggregate(Guid id)
    {
        var aggregate = CreateDetectedAggregate(id);
        aggregate.Assess(
            BreachSeverity.Critical,
            200,
            "Initial assessment",
            "user-2",
            _timeProvider.GetUtcNow());
        return aggregate;
    }

    private static BreachReadModel CreateBreachReadModel(Guid id)
    {
        return new BreachReadModel
        {
            Id = id,
            Nature = "unauthorized access",
            Severity = BreachSeverity.High,
            Status = BreachStatus.Detected,
            DetectedByRule = "rule-001",
            EstimatedAffectedSubjects = 100,
            Description = "Test breach",
            DetectedAtUtc = DateTimeOffset.UtcNow,
            DeadlineUtc = DateTimeOffset.UtcNow.AddHours(72),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };
    }
}
