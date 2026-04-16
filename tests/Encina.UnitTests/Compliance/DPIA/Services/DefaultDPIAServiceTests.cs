#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Caching;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Compliance.DPIA.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Shouldly;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA.Services;

/// <summary>
/// Unit tests for <see cref="DefaultDPIAService"/>.
/// </summary>
public class DefaultDPIAServiceTests
{
    private readonly IAggregateRepository<DPIAAggregate> _aggregateRepository;
    private readonly IReadModelRepository<DPIAReadModel> _readModelRepository;
    private readonly IDPIAAssessmentEngine _assessmentEngine;
    private readonly IDocumentSession _session;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultDPIAService _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    public DefaultDPIAServiceTests()
    {
        _aggregateRepository = Substitute.For<IAggregateRepository<DPIAAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DPIAReadModel>>();
        _assessmentEngine = Substitute.For<IDPIAAssessmentEngine>();
        _session = Substitute.For<IDocumentSession>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(FixedNow);
        var options = Options.Create(new DPIAOptions { DPOName = "Test DPO", DPOEmail = "dpo@test.com" });

        _sut = new DefaultDPIAService(
            _aggregateRepository,
            _readModelRepository,
            _assessmentEngine,
            _session,
            _cache,
            _timeProvider,
            options,
            NullLogger<DefaultDPIAService>.Instance);
    }

    // ========================================================================
    // CreateAssessmentAsync
    // ========================================================================

    #region CreateAssessmentAsync

    [Fact]
    public async Task CreateAssessmentAsync_HappyPath_ReturnsGuid()
    {
        // Arrange
        _aggregateRepository.CreateAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        // Act
        var result = await _sut.CreateAssessmentAsync("My.Request.Type");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBe(Guid.Empty));

        await _aggregateRepository.Received(1).CreateAsync(
            Arg.Is<DPIAAggregate>(a => a.RequestTypeName == "My.Request.Type"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAssessmentAsync_RepositoryFailure_ReturnsError()
    {
        // Arrange
        var error = DPIAErrors.StoreError("CreateAssessment", "DB failure");
        _aggregateRepository.CreateAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, Unit>(error)));

        // Act
        var result = await _sut.CreateAssessmentAsync("My.Request.Type");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.StoreErrorCode));
    }

    #endregion

    // ========================================================================
    // EvaluateAssessmentAsync
    // ========================================================================

    #region EvaluateAssessmentAsync

    [Fact]
    public async Task EvaluateAssessmentAsync_HappyPath_ReturnsDPIAResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = DPIAAggregate.Create(id, "Test.Type", _timeProvider.GetUtcNow());

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAAggregate>(aggregate)));

        var dpiaResult = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = FixedNow,
        };

        _assessmentEngine.AssessAsync(Arg.Any<DPIAContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, DPIAResult>>(Right<EncinaError, DPIAResult>(dpiaResult)));

        _aggregateRepository.SaveAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        var context = new DPIAContext
        {
            RequestType = typeof(string),
            DataCategories = ["PersonalData"],
            HighRiskTriggers = [],
        };

        // Act
        var result = await _sut.EvaluateAssessmentAsync(id, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.OverallRisk.ShouldBe(RiskLevel.Medium));

        await _aggregateRepository.Received(1).SaveAsync(
            Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAssessmentAsync_AssessmentNotFound_ReturnsError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = DPIAErrors.AssessmentNotFound(id);

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DPIAAggregate>(error)));

        var context = new DPIAContext
        {
            RequestType = typeof(string),
            DataCategories = [],
            HighRiskTriggers = [],
        };

        // Act
        var result = await _sut.EvaluateAssessmentAsync(id, context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentNotFoundCode));
    }

    #endregion

    // ========================================================================
    // ApproveAssessmentAsync
    // ========================================================================

    #region ApproveAssessmentAsync

    [Fact]
    public async Task ApproveAssessmentAsync_HappyPath_ReturnsUnit()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = CreateEvaluatedAggregate(id);

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAAggregate>(aggregate)));

        _aggregateRepository.SaveAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        // Act
        var result = await _sut.ApproveAssessmentAsync(id, "admin@test.com");

        // Assert
        result.IsRight.ShouldBeTrue();

        await _aggregateRepository.Received(1).SaveAsync(
            Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAssessmentAsync_AssessmentNotFound_ReturnsError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = DPIAErrors.AssessmentNotFound(id);

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DPIAAggregate>(error)));

        // Act
        var result = await _sut.ApproveAssessmentAsync(id, "admin@test.com");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentNotFoundCode));
    }

    #endregion

    // ========================================================================
    // RejectAssessmentAsync
    // ========================================================================

    #region RejectAssessmentAsync

    [Fact]
    public async Task RejectAssessmentAsync_HappyPath_ReturnsUnit()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = CreateEvaluatedAggregate(id);

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAAggregate>(aggregate)));

        _aggregateRepository.SaveAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        // Act
        var result = await _sut.RejectAssessmentAsync(id, "admin@test.com", "Risk too high");

        // Assert
        result.IsRight.ShouldBeTrue();

        await _aggregateRepository.Received(1).SaveAsync(
            Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // ========================================================================
    // RequestDPOConsultationAsync
    // ========================================================================

    #region RequestDPOConsultationAsync

    [Fact]
    public async Task RequestDPOConsultationAsync_HappyPath_ReturnsConsultationId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = CreateEvaluatedAggregate(id);

        _aggregateRepository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAAggregate>(aggregate)));

        _aggregateRepository.SaveAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        // Act
        var result = await _sut.RequestDPOConsultationAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(consultationId => consultationId.ShouldNotBe(Guid.Empty));

        await _aggregateRepository.Received(1).SaveAsync(
            Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestDPOConsultationAsync_DPONotConfigured_ReturnsError()
    {
        // Arrange
        var noDpoOptions = Options.Create(new DPIAOptions { DPOName = null, DPOEmail = null });
        var sut = new DefaultDPIAService(
            _aggregateRepository,
            _readModelRepository,
            _assessmentEngine,
            _session,
            _cache,
            _timeProvider,
            noDpoOptions,
            NullLogger<DefaultDPIAService>.Instance);

        var id = Guid.NewGuid();

        // Act
        var result = await sut.RequestDPOConsultationAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.DPOConsultationRequiredCode));

        // Should NOT attempt to load the aggregate since DPO check happens first
        await _aggregateRepository.DidNotReceive().LoadAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    // ========================================================================
    // GetAssessmentAsync
    // ========================================================================

    #region GetAssessmentAsync

    [Fact]
    public async Task GetAssessmentAsync_CacheHit_ReturnsCachedAndSkipsRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        var readModel = new DPIAReadModel
        {
            Id = id,
            RequestTypeName = "Test.Type",
            Status = DPIAAssessmentStatus.Approved,
        };

        _cache.GetAsync<DPIAReadModel>($"dpia:{id}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DPIAReadModel?>(readModel));

        // Act
        var result = await _sut.GetAssessmentAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.Id.ShouldBe(id));

        await _readModelRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAssessmentAsync_CacheMiss_CallsRepositoryAndCaches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var readModel = new DPIAReadModel
        {
            Id = id,
            RequestTypeName = "Test.Type",
            Status = DPIAAssessmentStatus.Approved,
        };

        _cache.GetAsync<DPIAReadModel>($"dpia:{id}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DPIAReadModel?>(null));

        _readModelRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPIAReadModel>(readModel)));

        // Act
        var result = await _sut.GetAssessmentAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.Id.ShouldBe(id));

        await _readModelRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAssessmentAsync_NotFound_ReturnsAssessmentNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();

        _cache.GetAsync<DPIAReadModel>($"dpia:{id}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DPIAReadModel?>(null));

        var notFoundError = DPIAErrors.AssessmentNotFound(id);
        _readModelRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DPIAReadModel>(notFoundError)));

        // Act
        var result = await _sut.GetAssessmentAsync(id);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentNotFoundCode));
    }

    #endregion

    // ========================================================================
    // GetAssessmentByRequestTypeAsync
    // ========================================================================

    #region GetAssessmentByRequestTypeAsync

    [Fact]
    public async Task GetAssessmentByRequestTypeAsync_Found_ReturnsReadModel()
    {
        // Arrange
        var requestType = "My.Request.Type";
        var readModel = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = requestType,
            Status = DPIAAssessmentStatus.Approved,
        };

        _cache.GetAsync<DPIAReadModel>($"dpia:type:{requestType}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DPIAReadModel?>(null));

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)new List<DPIAReadModel> { readModel })));

        // Act
        var result = await _sut.GetAssessmentByRequestTypeAsync(requestType);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.RequestTypeName.ShouldBe(requestType));
    }

    [Fact]
    public async Task GetAssessmentByRequestTypeAsync_NotFound_ReturnsError()
    {
        // Arrange
        var requestType = "Unknown.Type";

        _cache.GetAsync<DPIAReadModel>($"dpia:type:{requestType}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DPIAReadModel?>(null));

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)new List<DPIAReadModel>())));

        // Act
        var result = await _sut.GetAssessmentByRequestTypeAsync(requestType);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetEncinaCode().ShouldBe(DPIAErrors.AssessmentNotFoundCode));
    }

    #endregion

    // ========================================================================
    // GetExpiredAssessmentsAsync
    // ========================================================================

    #region GetExpiredAssessmentsAsync

    [Fact]
    public async Task GetExpiredAssessmentsAsync_ReturnsFilteredList()
    {
        // Arrange
        var expired = new DPIAReadModel
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Expired.Type",
            Status = DPIAAssessmentStatus.Approved,
            NextReviewAtUtc = FixedNow.AddDays(-1),
        };

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)new List<DPIAReadModel> { expired })));

        // Act
        var result = await _sut.GetExpiredAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(1));
    }

    #endregion

    // ========================================================================
    // GetAllAssessmentsAsync
    // ========================================================================

    #region GetAllAssessmentsAsync

    [Fact]
    public async Task GetAllAssessmentsAsync_ReturnsAll()
    {
        // Arrange
        var models = new List<DPIAReadModel>
        {
            new() { Id = Guid.NewGuid(), RequestTypeName = "Type.A", Status = DPIAAssessmentStatus.Approved },
            new() { Id = Guid.NewGuid(), RequestTypeName = "Type.B", Status = DPIAAssessmentStatus.Draft },
        };

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)models)));

        // Act
        var result = await _sut.GetAllAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(2));
    }

    #endregion

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Creates a <see cref="DPIAAggregate"/> that has been evaluated (InReview status),
    /// ready for approve/reject/DPO consultation operations.
    /// </summary>
    private DPIAAggregate CreateEvaluatedAggregate(Guid id)
    {
        var aggregate = DPIAAggregate.Create(id, "Test.Type", _timeProvider.GetUtcNow());
        var dpiaResult = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = FixedNow,
        };
        aggregate.Evaluate(dpiaResult, _timeProvider.GetUtcNow());
        return aggregate;
    }
}
