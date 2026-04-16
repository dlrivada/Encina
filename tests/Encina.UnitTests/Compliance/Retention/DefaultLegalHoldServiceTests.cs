using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DefaultLegalHoldService"/>.
/// </summary>
public sealed class DefaultLegalHoldServiceTests
{
    private readonly IAggregateRepository<LegalHoldAggregate> _repository;
    private readonly IReadModelRepository<LegalHoldReadModel> _readModelRepository;
    private readonly IReadModelRepository<RetentionRecordReadModel> _recordReadModelRepository;
    private readonly IRetentionRecordService _retentionRecordService;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultLegalHoldService> _logger;
    private readonly DefaultLegalHoldService _sut;

    public DefaultLegalHoldServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<LegalHoldAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<LegalHoldReadModel>>();
        _recordReadModelRepository = Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();
        _retentionRecordService = Substitute.For<IRetentionRecordService>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultLegalHoldService>.Instance;

        _sut = new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
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
        var act = () => new DefaultLegalHoldService(
            null!,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            null!,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullRecordReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            null!,
            _retentionRecordService,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("recordReadModelRepository");
    }

    [Fact]
    public void Constructor_NullRetentionRecordService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            null!,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("retentionRecordService");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
            null!,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            null!,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    // ========================================================================
    // PlaceHoldAsync tests
    // ========================================================================

    #region PlaceHoldAsync

    [Fact]
    public async Task PlaceHoldAsync_ValidInput_ReturnsRight()
    {
        _repository
            .CreateAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Cascade query: no records for entity (cascade is best-effort)
        _recordReadModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionRecordReadModel>, IQueryable<RetentionRecordReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                new List<RetentionRecordReadModel>()));

        var result = await _sut.PlaceHoldAsync(
            entityId: "customer-42",
            reason: "Ongoing litigation - Case #12345",
            appliedByUserId: "legal-counsel-1");

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.ShouldNotBe(Guid.Empty), _ => { });
        await _repository.Received(1).CreateAsync(
            Arg.Any<LegalHoldAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceHoldAsync_StoreFailure_ReturnsLeft()
    {
        var error = EncinaError.New("store failed");
        _repository
            .CreateAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        var result = await _sut.PlaceHoldAsync(
            entityId: "customer-42",
            reason: "Litigation hold",
            appliedByUserId: "legal-counsel-1");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PlaceHoldAsync_WithAffectedRecords_CascadesHoldToRecords()
    {
        var record1 = CreateRecordReadModel(Guid.NewGuid(), "customer-42");
        var record2 = CreateRecordReadModel(Guid.NewGuid(), "customer-42");
        var records = new List<RetentionRecordReadModel> { record1, record2 };

        _repository
            .CreateAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        _recordReadModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionRecordReadModel>, IQueryable<RetentionRecordReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(records));

        _retentionRecordService
            .HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.PlaceHoldAsync(
            entityId: "customer-42",
            reason: "Litigation hold",
            appliedByUserId: "legal-counsel-1");

        result.IsRight.ShouldBeTrue();
        await _retentionRecordService.Received(2).HoldRecordAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    // ========================================================================
    // LiftHoldAsync tests
    // ========================================================================

    #region LiftHoldAsync

    [Fact]
    public async Task LiftHoldAsync_ValidInput_ReturnsRight()
    {
        var holdId = Guid.NewGuid();
        var aggregate = CreateHoldAggregate(holdId, "customer-42");

        _repository
            .LoadAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LegalHoldAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // No other active holds remain after lifting
        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<LegalHoldReadModel>, IQueryable<LegalHoldReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(
                new List<LegalHoldReadModel>()));

        // No held records to release
        _recordReadModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionRecordReadModel>, IQueryable<RetentionRecordReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                new List<RetentionRecordReadModel>()));

        var result = await _sut.LiftHoldAsync(holdId, releasedByUserId: "legal-counsel-1");

        result.IsRight.ShouldBeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<LegalHoldAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LiftHoldAsync_HoldNotFound_ReturnsLeft()
    {
        var holdId = Guid.NewGuid();
        _repository
            .LoadAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LegalHoldAggregate>(EncinaError.New("not found")));

        var result = await _sut.LiftHoldAsync(holdId, releasedByUserId: "legal-counsel-1");

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task LiftHoldAsync_AlreadyLifted_ReturnsLeft()
    {
        var holdId = Guid.NewGuid();
        // A hold that has already been lifted — Lift() throws InvalidOperationException
        var aggregate = CreateLiftedHoldAggregate(holdId, "customer-42");

        _repository
            .LoadAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LegalHoldAggregate>(aggregate));

        var result = await _sut.LiftHoldAsync(holdId, releasedByUserId: "legal-counsel-2");

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("Invalid state transition"));
    }

    [Fact]
    public async Task LiftHoldAsync_OtherActiveHoldsRemain_DoesNotReleaseRecords()
    {
        var holdId = Guid.NewGuid();
        var aggregate = CreateHoldAggregate(holdId, "customer-42");
        var otherActiveHold = CreateHoldReadModel(Guid.NewGuid(), "customer-42");

        _repository
            .LoadAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LegalHoldAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Another active hold remains for the same entity
        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<LegalHoldReadModel>, IQueryable<LegalHoldReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(
                new List<LegalHoldReadModel> { otherActiveHold }));

        var result = await _sut.LiftHoldAsync(holdId, releasedByUserId: "legal-counsel-1");

        result.IsRight.ShouldBeTrue();

        // Records must NOT be released because another hold is still active
        await _retentionRecordService
            .DidNotReceive()
            .ReleaseRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // ========================================================================
    // GetHoldAsync tests (cache-aside pattern)
    // ========================================================================

    #region GetHoldAsync

    [Fact]
    public async Task GetHoldByIdAsync_CacheHit_ReturnsCachedValue()
    {
        var holdId = Guid.NewGuid();
        var cachedModel = CreateHoldReadModel(holdId, "customer-42");

        _cache
            .GetAsync<LegalHoldReadModel>($"ret:hold:{holdId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        var result = await _sut.GetHoldAsync(holdId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(holdId), _ => { });

        await _readModelRepository
            .DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHoldByIdAsync_CacheMiss_QueriesReadModel()
    {
        var holdId = Guid.NewGuid();
        var readModel = CreateHoldReadModel(holdId, "customer-42");

        _cache
            .GetAsync<LegalHoldReadModel>($"ret:hold:{holdId}", Arg.Any<CancellationToken>())
            .Returns((LegalHoldReadModel?)null);
        _readModelRepository
            .GetByIdAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LegalHoldReadModel>(readModel));

        var result = await _sut.GetHoldAsync(holdId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(holdId), _ => { });

        await _cache.Received(1).SetAsync(
            $"ret:hold:{holdId}",
            readModel,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHoldByIdAsync_NotFound_ReturnsLeft()
    {
        var holdId = Guid.NewGuid();

        _cache
            .GetAsync<LegalHoldReadModel>($"ret:hold:{holdId}", Arg.Any<CancellationToken>())
            .Returns((LegalHoldReadModel?)null);
        _readModelRepository
            .GetByIdAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LegalHoldReadModel>(EncinaError.New("not found")));

        var result = await _sut.GetHoldAsync(holdId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    #endregion

    // ========================================================================
    // GetActiveHoldsForEntityAsync tests
    // ========================================================================

    #region GetActiveHoldsForEntityAsync

    [Fact]
    public async Task GetActiveHoldsForEntityAsync_ReturnsHolds()
    {
        const string entityId = "customer-42";
        var hold1 = CreateHoldReadModel(Guid.NewGuid(), entityId);
        var hold2 = CreateHoldReadModel(Guid.NewGuid(), entityId);
        var holds = new List<LegalHoldReadModel> { hold1, hold2 };

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<LegalHoldReadModel>, IQueryable<LegalHoldReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(holds));

        var result = await _sut.GetActiveHoldsForEntityAsync(entityId);

        result.IsRight.ShouldBeTrue();
        result.Match(list => list.Count.ShouldBe(2), _ => { });
    }

    [Fact]
    public async Task GetActiveHoldsForEntityAsync_NoHolds_ReturnsEmptyList()
    {
        const string entityId = "customer-99";

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<LegalHoldReadModel>, IQueryable<LegalHoldReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(
                new List<LegalHoldReadModel>()));

        var result = await _sut.GetActiveHoldsForEntityAsync(entityId);

        result.IsRight.ShouldBeTrue();
        result.Match(list => list.ShouldBeEmpty(), _ => { });
    }

    #endregion

    // ========================================================================
    // GetHoldHistoryAsync tests
    // ========================================================================

    #region GetHoldHistoryAsync

    [Fact]
    public async Task GetHoldHistoryAsync_ReturnsEventHistoryUnavailableError()
    {
        var holdId = Guid.NewGuid();

        var result = await _sut.GetHoldHistoryAsync(holdId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not yet available"));
    }

    #endregion

    // ========================================================================
    // Helpers
    // ========================================================================

    private LegalHoldAggregate CreateHoldAggregate(Guid id, string entityId)
    {
        return LegalHoldAggregate.Place(
            id,
            entityId: entityId,
            reason: "Ongoing litigation - Case #12345",
            appliedByUserId: "legal-counsel-1",
            appliedAtUtc: _timeProvider.GetUtcNow());
    }

    private LegalHoldAggregate CreateLiftedHoldAggregate(Guid id, string entityId)
    {
        var aggregate = CreateHoldAggregate(id, entityId);
        aggregate.Lift("legal-counsel-1", _timeProvider.GetUtcNow());
        return aggregate;
    }

    private static LegalHoldReadModel CreateHoldReadModel(Guid id, string entityId)
    {
        return new LegalHoldReadModel
        {
            Id = id,
            EntityId = entityId,
            Reason = "Ongoing litigation - Case #12345",
            AppliedByUserId = "legal-counsel-1",
            IsActive = true,
            AppliedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };
    }

    private static RetentionRecordReadModel CreateRecordReadModel(Guid id, string entityId)
    {
        return new RetentionRecordReadModel
        {
            Id = id,
            EntityId = entityId,
            DataCategory = "customer-data",
            PolicyId = Guid.NewGuid(),
            RetentionPeriod = TimeSpan.FromDays(365),
            Status = RetentionStatus.Active,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(365),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };
    }
}
