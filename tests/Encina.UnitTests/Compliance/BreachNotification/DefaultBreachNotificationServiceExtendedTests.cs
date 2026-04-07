#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Caching;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Compliance.BreachNotification.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Extended unit tests for <see cref="DefaultBreachNotificationService"/>
/// covering methods not tested by the primary test class.
/// </summary>
public sealed class DefaultBreachNotificationServiceExtendedTests
{
    private readonly IAggregateRepository<BreachAggregate> _repository;
    private readonly IReadModelRepository<BreachReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultBreachNotificationService> _logger;
    private readonly DefaultBreachNotificationService _sut;

    public DefaultBreachNotificationServiceExtendedTests()
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
    // ReportToDPAAsync
    // ========================================================================

    #region ReportToDPAAsync

    [Fact]
    public async Task ReportToDPAAsync_Success_ReturnsRight()
    {
        var breachId = Guid.NewGuid();
        var aggregate = CreateDetectedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.ReportToDPAAsync(
            breachId,
            "Spanish DPA",
            "contact@aepd.es",
            "Report summary",
            "user-10");

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportToDPAAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();
        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachAggregate>(EncinaError.New("not found")));

        var result = await _sut.ReportToDPAAsync(
            breachId,
            "Spanish DPA",
            "contact@aepd.es",
            "Report summary",
            "user-10");

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    [Fact]
    public async Task ReportToDPAAsync_InvalidState_ReturnsLeftInvalidStateTransition()
    {
        var breachId = Guid.NewGuid();
        // Create an aggregate in AuthorityNotified status (already reported)
        var aggregate = CreateAuthorityNotifiedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));

        var result = await _sut.ReportToDPAAsync(
            breachId,
            "Another DPA",
            "contact@cnil.fr",
            "Second report",
            "user-10");

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("Invalid state transition"));
    }

    #endregion

    // ========================================================================
    // NotifySubjectsAsync
    // ========================================================================

    #region NotifySubjectsAsync

    [Fact]
    public async Task NotifySubjectsAsync_Success_ReturnsRight()
    {
        var breachId = Guid.NewGuid();
        var aggregate = CreateAuthorityNotifiedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.NotifySubjectsAsync(
            breachId,
            250,
            "email",
            SubjectNotificationExemption.None,
            "user-11");

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifySubjectsAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();
        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachAggregate>(EncinaError.New("not found")));

        var result = await _sut.NotifySubjectsAsync(
            breachId,
            250,
            "email",
            SubjectNotificationExemption.None,
            "user-11");

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // ContainBreachAsync
    // ========================================================================

    #region ContainBreachAsync

    [Fact]
    public async Task ContainBreachAsync_Success_ReturnsRight()
    {
        var breachId = Guid.NewGuid();
        var aggregate = CreateDetectedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.ContainBreachAsync(
            breachId,
            "Revoked access tokens and blocked IPs",
            "user-12");

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ContainBreachAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();
        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachAggregate>(EncinaError.New("not found")));

        var result = await _sut.ContainBreachAsync(
            breachId,
            "Revoked access tokens",
            "user-12");

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // CloseBreachAsync
    // ========================================================================

    #region CloseBreachAsync

    [Fact]
    public async Task CloseBreachAsync_Success_ReturnsRight()
    {
        var breachId = Guid.NewGuid();
        var aggregate = CreateSubjectsNotifiedAggregate(breachId);

        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, BreachAggregate>(aggregate));
        _repository
            .SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var result = await _sut.CloseBreachAsync(
            breachId,
            "Root cause: misconfigured firewall. Remediation complete.",
            "user-13");

        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<BreachAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseBreachAsync_NotFound_ReturnsLeftNotFound()
    {
        var breachId = Guid.NewGuid();
        _repository
            .LoadAsync(breachId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, BreachAggregate>(EncinaError.New("not found")));

        var result = await _sut.CloseBreachAsync(
            breachId,
            "Resolution summary",
            "user-13");

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("not found"));
    }

    #endregion

    // ========================================================================
    // GetBreachesByStatusAsync
    // ========================================================================

    #region GetBreachesByStatusAsync

    [Fact]
    public async Task GetBreachesByStatusAsync_Success_ReturnsList()
    {
        var expectedList = new List<BreachReadModel>
        {
            CreateBreachReadModel(Guid.NewGuid()),
            CreateBreachReadModel(Guid.NewGuid())
        };

        _readModelRepository
            .QueryAsync(Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(expectedList));

        var result = await _sut.GetBreachesByStatusAsync(BreachStatus.Detected);

        result.IsRight.Should().BeTrue();
        result.Match(list => list.Should().HaveCount(2), _ => { });
    }

    [Fact]
    public async Task GetBreachesByStatusAsync_Exception_ReturnsLeftServiceError()
    {
        _readModelRepository
            .QueryAsync(Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, IReadOnlyList<BreachReadModel>>>(_ => throw new InvalidOperationException("DB error"));

        var result = await _sut.GetBreachesByStatusAsync(BreachStatus.Detected);

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("GetBreachesByStatus"));
    }

    #endregion

    // ========================================================================
    // GetApproachingDeadlineBreachesAsync
    // ========================================================================

    #region GetApproachingDeadlineBreachesAsync

    [Fact]
    public async Task GetApproachingDeadlineBreachesAsync_Success_ReturnsList()
    {
        var expectedList = new List<BreachReadModel> { CreateBreachReadModel(Guid.NewGuid()) };

        _readModelRepository
            .QueryAsync(Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<BreachReadModel>>(expectedList));

        var result = await _sut.GetApproachingDeadlineBreachesAsync();

        result.IsRight.Should().BeTrue();
        result.Match(list => list.Should().HaveCount(1), _ => { });
    }

    [Fact]
    public async Task GetApproachingDeadlineBreachesAsync_Exception_ReturnsLeftServiceError()
    {
        _readModelRepository
            .QueryAsync(Arg.Any<Func<IQueryable<BreachReadModel>, IQueryable<BreachReadModel>>>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, IReadOnlyList<BreachReadModel>>>(_ => throw new InvalidOperationException("DB error"));

        var result = await _sut.GetApproachingDeadlineBreachesAsync();

        result.IsLeft.Should().BeTrue();
        result.Match(_ => { }, error => error.Message.Should().Contain("GetApproachingDeadlineBreaches"));
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

    private BreachAggregate CreateAuthorityNotifiedAggregate(Guid id)
    {
        var aggregate = CreateDetectedAggregate(id);
        aggregate.ReportToDPA(
            "Spanish DPA",
            "contact@aepd.es",
            "Initial report",
            "user-3",
            _timeProvider.GetUtcNow());
        return aggregate;
    }

    private BreachAggregate CreateSubjectsNotifiedAggregate(Guid id)
    {
        var aggregate = CreateAuthorityNotifiedAggregate(id);
        aggregate.NotifySubjects(
            100,
            "email",
            SubjectNotificationExemption.None,
            "user-4",
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
