using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DefaultRetentionPolicyService"/>.
/// </summary>
public sealed class DefaultRetentionPolicyServiceTests
{
    private readonly IAggregateRepository<RetentionPolicyAggregate> _repository;
    private readonly IReadModelRepository<RetentionPolicyReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly IOptions<RetentionOptions> _options;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultRetentionPolicyService> _logger;
    private readonly DefaultRetentionPolicyService _sut;

    public DefaultRetentionPolicyServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<RetentionPolicyAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<RetentionPolicyReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _options = Options.Create(new RetentionOptions());
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 17, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultRetentionPolicyService>.Instance;

        _sut = new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
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
        var act = () => new DefaultRetentionPolicyService(
            null!,
            _readModelRepository,
            _cache,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            null!,
            _cache,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            null!,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
            null!,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    // ========================================================================
    // CreatePolicyAsync tests
    // ========================================================================

    #region CreatePolicyAsync

    [Fact]
    public async Task CreatePolicyAsync_ValidInput_ReturnsRightWithPolicyId()
    {
        _repository
            .CreateAsync(Arg.Any<RetentionPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.CreatePolicyAsync(
            "customer-data",
            TimeSpan.FromDays(365),
            autoDelete: true,
            RetentionPolicyType.TimeBased,
            reason: "GDPR Article 5(1)(e)");

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.ShouldNotBe(Guid.Empty), _ => { });
        await _repository.Received(1).CreateAsync(
            Arg.Any<RetentionPolicyAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePolicyAsync_StoreFailure_ReturnsLeft()
    {
        var error = EncinaError.New("store failed");
        _repository
            .CreateAsync(Arg.Any<RetentionPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        var result = await _sut.CreatePolicyAsync(
            "customer-data",
            TimeSpan.FromDays(365),
            autoDelete: true,
            RetentionPolicyType.TimeBased);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    // ========================================================================
    // UpdatePolicyAsync tests
    // ========================================================================

    #region UpdatePolicyAsync

    [Fact]
    public async Task UpdatePolicyAsync_ValidInput_ReturnsRight()
    {
        var policyId = Guid.NewGuid();
        var aggregate = CreatePolicyAggregate(policyId);

        _repository
            .LoadAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionPolicyAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.UpdatePolicyAsync(
            policyId,
            TimeSpan.FromDays(730),
            autoDelete: false,
            reason: "Updated policy",
            legalBasis: "Tax regulation §147");

        result.IsRight.ShouldBeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionPolicyAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicyAsync_PolicyNotFound_ReturnsLeft()
    {
        var policyId = Guid.NewGuid();
        _repository
            .LoadAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyAggregate>(EncinaError.New("not found")));

        var result = await _sut.UpdatePolicyAsync(
            policyId,
            TimeSpan.FromDays(730),
            autoDelete: false,
            reason: null,
            legalBasis: null);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    #endregion

    // ========================================================================
    // DeactivatePolicyAsync tests
    // ========================================================================

    #region DeactivatePolicyAsync

    [Fact]
    public async Task DeactivatePolicyAsync_ValidInput_ReturnsRight()
    {
        var policyId = Guid.NewGuid();
        var aggregate = CreatePolicyAggregate(policyId);

        _repository
            .LoadAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionPolicyAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<RetentionPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.DeactivatePolicyAsync(policyId, "Policy superseded");

        result.IsRight.ShouldBeTrue();
        await _repository.Received(1).SaveAsync(
            Arg.Any<RetentionPolicyAggregate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivatePolicyAsync_PolicyNotFound_ReturnsLeft()
    {
        var policyId = Guid.NewGuid();
        _repository
            .LoadAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyAggregate>(EncinaError.New("not found")));

        var result = await _sut.DeactivatePolicyAsync(policyId, "Superseded");

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    #endregion

    // ========================================================================
    // GetPolicyAsync tests (cache-aside pattern)
    // ========================================================================

    #region GetPolicyAsync

    [Fact]
    public async Task GetPolicyByIdAsync_CacheHit_ReturnsCachedValue()
    {
        var policyId = Guid.NewGuid();
        var cachedModel = CreatePolicyReadModel(policyId);

        _cache
            .GetAsync<RetentionPolicyReadModel>($"ret:policy:{policyId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        var result = await _sut.GetPolicyAsync(policyId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(policyId), _ => { });

        await _readModelRepository
            .DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyByIdAsync_CacheMiss_QueriesReadModel()
    {
        var policyId = Guid.NewGuid();
        var readModel = CreatePolicyReadModel(policyId);

        _cache
            .GetAsync<RetentionPolicyReadModel>($"ret:policy:{policyId}", Arg.Any<CancellationToken>())
            .Returns((RetentionPolicyReadModel?)null);
        _readModelRepository
            .GetByIdAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, RetentionPolicyReadModel>(readModel));

        var result = await _sut.GetPolicyAsync(policyId);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.Id.ShouldBe(policyId), _ => { });

        await _cache.Received(1).SetAsync(
            $"ret:policy:{policyId}",
            readModel,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyByIdAsync_NotFound_ReturnsLeft()
    {
        var policyId = Guid.NewGuid();

        _cache
            .GetAsync<RetentionPolicyReadModel>($"ret:policy:{policyId}", Arg.Any<CancellationToken>())
            .Returns((RetentionPolicyReadModel?)null);
        _readModelRepository
            .GetByIdAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, RetentionPolicyReadModel>(EncinaError.New("not found")));

        var result = await _sut.GetPolicyAsync(policyId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not found"));
    }

    #endregion

    // ========================================================================
    // GetPolicyByCategoryAsync tests
    // ========================================================================

    #region GetPolicyByCategoryAsync

    [Fact]
    public async Task GetPolicyByCategoryAsync_ReturnsPolicy()
    {
        var policyId = Guid.NewGuid();
        const string category = "financial-records";
        var readModel = CreatePolicyReadModel(policyId, category);
        var policies = new List<RetentionPolicyReadModel> { readModel };

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionPolicyReadModel>, IQueryable<RetentionPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>(policies));

        var result = await _sut.GetPolicyByCategoryAsync(category);

        result.IsRight.ShouldBeTrue();
        result.Match(model => model.DataCategory.ShouldBe(category), _ => { });
    }

    [Fact]
    public async Task GetPolicyByCategoryAsync_NoPolicyExists_ReturnsLeft()
    {
        const string category = "unknown-category";
        var emptyList = new List<RetentionPolicyReadModel>();

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionPolicyReadModel>, IQueryable<RetentionPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>(emptyList));

        var result = await _sut.GetPolicyByCategoryAsync(category);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain(category));
    }

    #endregion

    // ========================================================================
    // GetRetentionPeriodAsync tests
    // ========================================================================

    #region GetRetentionPeriodAsync

    [Fact]
    public async Task GetRetentionPeriodAsync_PolicyExists_ReturnsPeriod()
    {
        var policyId = Guid.NewGuid();
        const string category = "customer-data";
        var expectedPeriod = TimeSpan.FromDays(365);
        var readModel = CreatePolicyReadModel(policyId, category, retentionPeriod: expectedPeriod);
        var policies = new List<RetentionPolicyReadModel> { readModel };

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionPolicyReadModel>, IQueryable<RetentionPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>(policies));

        var result = await _sut.GetRetentionPeriodAsync(category);

        result.IsRight.ShouldBeTrue();
        result.Match(period => period.ShouldBe(expectedPeriod), _ => { });
    }

    [Fact]
    public async Task GetRetentionPeriodAsync_NoPolicyButDefaultConfigured_ReturnsDefault()
    {
        const string category = "unknown-category";
        var defaultPeriod = TimeSpan.FromDays(180);
        var options = Options.Create(new RetentionOptions { DefaultRetentionPeriod = defaultPeriod });
        var sut = new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            options,
            _timeProvider,
            _logger);

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionPolicyReadModel>, IQueryable<RetentionPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>(
                new List<RetentionPolicyReadModel>()));

        var result = await sut.GetRetentionPeriodAsync(category);

        result.IsRight.ShouldBeTrue();
        result.Match(period => period.ShouldBe(defaultPeriod), _ => { });
    }

    [Fact]
    public async Task GetRetentionPeriodAsync_NoPolicyNoDefault_ReturnsLeft()
    {
        const string category = "unconfigured-category";

        _readModelRepository
            .QueryAsync(
                Arg.Any<Func<IQueryable<RetentionPolicyReadModel>, IQueryable<RetentionPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>(
                new List<RetentionPolicyReadModel>()));

        var result = await _sut.GetRetentionPeriodAsync(category);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain(category));
    }

    #endregion

    // ========================================================================
    // GetPolicyHistoryAsync tests
    // ========================================================================

    #region GetPolicyHistoryAsync

    [Fact]
    public async Task GetPolicyHistoryAsync_ReturnsEventHistoryUnavailableError()
    {
        var policyId = Guid.NewGuid();

        var result = await _sut.GetPolicyHistoryAsync(policyId);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("not yet available"));
    }

    #endregion

    // ========================================================================
    // Helpers
    // ========================================================================

    private RetentionPolicyAggregate CreatePolicyAggregate(Guid id)
    {
        return RetentionPolicyAggregate.Create(
            id,
            dataCategory: "customer-data",
            retentionPeriod: TimeSpan.FromDays(365),
            autoDelete: true,
            policyType: RetentionPolicyType.TimeBased,
            reason: null,
            legalBasis: null,
            occurredAtUtc: _timeProvider.GetUtcNow(),
            tenantId: null,
            moduleId: null);
    }

    private static RetentionPolicyReadModel CreatePolicyReadModel(
        Guid id,
        string dataCategory = "customer-data",
        TimeSpan? retentionPeriod = null)
    {
        return new RetentionPolicyReadModel
        {
            Id = id,
            DataCategory = dataCategory,
            RetentionPeriod = retentionPeriod ?? TimeSpan.FromDays(365),
            AutoDelete = true,
            PolicyType = RetentionPolicyType.TimeBased,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 1
        };
    }
}
