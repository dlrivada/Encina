using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DefaultRetentionRecordService"/>.
/// </summary>
public sealed class DefaultRetentionRecordServiceTests
{
    private readonly IAggregateRepository<RetentionRecordAggregate> _repository;
    private readonly IReadModelRepository<RetentionRecordReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultRetentionRecordService> _logger;
    private readonly DefaultRetentionRecordService _sut;

    public DefaultRetentionRecordServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<RetentionRecordAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultRetentionRecordService>.Instance;

        _sut = new DefaultRetentionRecordService(
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
        var act = () => new DefaultRetentionRecordService(
            null!,
            _readModelRepository,
            _cache,
            _timeProvider,
            _logger);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            _repository,
            null!,
            _cache,
            _timeProvider,
            _logger);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            null!,
            _timeProvider,
            _logger);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            _logger);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    #endregion

    // ========================================================================
    // TrackEntityAsync tests
    // ========================================================================

    #region TrackEntityAsync

    [Fact]
    public async Task TrackRecordAsync_ValidInput_ReturnsRight()
    {
        _repository
            .CreateAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.TrackEntityAsync(
            entityId: "customer-42",
            dataCategory: "customer-data",
            policyId: Guid.NewGuid(),
            retentionPeriod: TimeSpan.FromDays(365));

        result.IsRight.Should().BeTrue();
        result.Match(id => id.Should().NotBeEmpty(), _ => { });
        await _repository.Received(1).CreateAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackRecordAsync_StoreFailure_ReturnsLeft()
    {
        var error = EncinaError.New("store failed");
        _repository
            .CreateAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        var result = await _sut.TrackEntityAsync(
            entityId: "customer-42",
            dataCategory: "customer-data",
            policyId: Guid.NewGuid(),
            retentionPeriod: TimeSpan.FromDays(365));

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    // ========================================================================
    // MarkExpiredAsync tests
    // ========================================================================

    #region MarkExpiredAsync

    [Fact]
    public async Task MarkExpiredAsync_ValidInput_ReturnsRight()
    {
        var recordId = Guid.NewGuid();
        var aggregate = CreateActiveRecordAggregate(recordId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.MarkExpiredAsync(recordId);

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkExpiredAsync_RecordNotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordAggregate>(EncinaError.New("not found")));

        var result = await _sut.MarkExpiredAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    [Fact]
    public async Task MarkExpiredAsync_InvalidStateTransition_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        // A record already marked as expired cannot be expired again — InvalidOperationException
        var aggregate = CreateExpiredRecordAggregate(recordId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));

        var result = await _sut.MarkExpiredAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("Invalid state transition"));
    }

    #endregion

    // ========================================================================
    // HoldRecordAsync tests
    // ========================================================================

    #region HoldRecordAsync

    [Fact]
    public async Task HoldRecordAsync_ValidInput_ReturnsRight()
    {
        var recordId = Guid.NewGuid();
        var legalHoldId = Guid.NewGuid();
        var aggregate = CreateActiveRecordAggregate(recordId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.HoldRecordAsync(recordId, legalHoldId);

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HoldRecordAsync_RecordNotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordAggregate>(EncinaError.New("not found")));

        var result = await _sut.HoldRecordAsync(recordId, Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // ReleaseRecordAsync tests
    // ========================================================================

    #region ReleaseRecordAsync

    [Fact]
    public async Task ReleaseRecordAsync_ValidInput_ReturnsRight()
    {
        var recordId = Guid.NewGuid();
        var legalHoldId = Guid.NewGuid();
        var aggregate = CreateHeldRecordAggregate(recordId, legalHoldId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.ReleaseRecordAsync(recordId, legalHoldId);

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseRecordAsync_RecordNotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordAggregate>(EncinaError.New("not found")));

        var result = await _sut.ReleaseRecordAsync(recordId, Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // MarkDeletedAsync tests
    // ========================================================================

    #region MarkDeletedAsync

    [Fact]
    public async Task MarkDeletedAsync_ValidInput_ReturnsRight()
    {
        var recordId = Guid.NewGuid();
        var aggregate = CreateExpiredRecordAggregate(recordId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.MarkDeletedAsync(recordId);

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkDeletedAsync_RecordNotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordAggregate>(EncinaError.New("not found")));

        var result = await _sut.MarkDeletedAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // MarkAnonymizedAsync tests
    // ========================================================================

    #region MarkAnonymizedAsync

    [Fact]
    public async Task MarkAnonymizedAsync_ValidInput_ReturnsRight()
    {
        var recordId = Guid.NewGuid();
        var aggregate = CreateExpiredRecordAggregate(recordId);

        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionRecordAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.MarkAnonymizedAsync(recordId);

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionRecordAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAnonymizedAsync_RecordNotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();
        _repository
            .LoadAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordAggregate>(EncinaError.New("not found")));

        var result = await _sut.MarkAnonymizedAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // GetRecordAsync tests (cache-aside pattern)
    // ========================================================================

    #region GetRecordAsync

    [Fact]
    public async Task GetRecordByIdAsync_CacheHit_ReturnsCachedValue()
    {
        var recordId = Guid.NewGuid();
        var cachedModel = CreateRecordReadModel(recordId);

        _cache
            .GetAsync<RetentionRecordReadModel>($"ret:record:{recordId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        var result = await _sut.GetRecordAsync(recordId);

        result.IsRight.Should().BeTrue();
        result.Match(model => model.Id.Should().Be(recordId), _ => { });

        await _readModelRepository
            .DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecordByIdAsync_CacheMiss_QueriesReadModel()
    {
        var recordId = Guid.NewGuid();
        var readModel = CreateRecordReadModel(recordId);

        _cache
            .GetAsync<RetentionRecordReadModel>($"ret:record:{recordId}", Arg.Any<CancellationToken>())
            .Returns((RetentionRecordReadModel?)null);
        _readModelRepository
            .GetByIdAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionRecordReadModel>(readModel));

        var result = await _sut.GetRecordAsync(recordId);

        result.IsRight.Should().BeTrue();
        result.Match(model => model.Id.Should().Be(recordId), _ => { });

        await _cache.Received(1).SetAsync(
            $"ret:record:{recordId}",
            readModel,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecordByIdAsync_NotFound_ReturnsLeft()
    {
        var recordId = Guid.NewGuid();

        _cache
            .GetAsync<RetentionRecordReadModel>($"ret:record:{recordId}", Arg.Any<CancellationToken>())
            .Returns((RetentionRecordReadModel?)null);
        _readModelRepository
            .GetByIdAsync(recordId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionRecordReadModel>(EncinaError.New("not found")));

        var result = await _sut.GetRecordAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // GetRecordHistoryAsync tests
    // ========================================================================

    #region GetRecordHistoryAsync

    [Fact]
    public async Task GetRecordHistoryAsync_ReturnsEventHistoryUnavailableError()
    {
        var recordId = Guid.NewGuid();

        var result = await _sut.GetRecordHistoryAsync(recordId);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not yet available"));
    }

    #endregion

    // ========================================================================
    // Helpers
    // ========================================================================

    private RetentionRecordAggregate CreateActiveRecordAggregate(Guid id)
    {
        var now = _timeProvider.GetUtcNow();
        return RetentionRecordAggregate.Track(
            id,
            entityId: "customer-42",
            dataCategory: "customer-data",
            policyId: Guid.NewGuid(),
            retentionPeriod: TimeSpan.FromDays(365),
            expiresAtUtc: now.AddDays(365),
            occurredAtUtc: now);
    }

    private RetentionRecordAggregate CreateExpiredRecordAggregate(Guid id)
    {
        var now = _timeProvider.GetUtcNow();
        var aggregate = RetentionRecordAggregate.Track(
            id,
            entityId: "customer-42",
            dataCategory: "customer-data",
            policyId: Guid.NewGuid(),
            retentionPeriod: TimeSpan.FromDays(365),
            expiresAtUtc: now.AddDays(-1),
            occurredAtUtc: now.AddDays(-366));
        aggregate.MarkExpired(now);
        return aggregate;
    }

    private RetentionRecordAggregate CreateHeldRecordAggregate(Guid id, Guid legalHoldId)
    {
        var aggregate = CreateActiveRecordAggregate(id);
        aggregate.Hold(legalHoldId, _timeProvider.GetUtcNow());
        return aggregate;
    }

    private static RetentionRecordReadModel CreateRecordReadModel(Guid id)
    {
        return new RetentionRecordReadModel
        {
            Id = id,
            EntityId = "customer-42",
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
