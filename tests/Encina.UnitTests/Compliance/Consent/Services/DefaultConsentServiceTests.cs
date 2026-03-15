using Encina.Caching;
using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="DefaultConsentService"/>.
/// </summary>
public class DefaultConsentServiceTests
{
    private readonly IAggregateRepository<ConsentAggregate> _repository;
    private readonly IReadModelRepository<ConsentReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultConsentService _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    public DefaultConsentServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(FixedNow);

        _sut = new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            NullLogger<DefaultConsentService>.Instance);
    }

    #region Constructor Guard Clauses

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            null!,
            _readModelRepository,
            _cache,
            _timeProvider,
            NullLogger<DefaultConsentService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            null!,
            _cache,
            _timeProvider,
            NullLogger<DefaultConsentService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            null!,
            _timeProvider,
            NullLogger<DefaultConsentService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            NullLogger<DefaultConsentService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region GrantConsentAsync

    [Fact]
    public async Task GrantConsentAsync_Success_ShouldReturnGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v1", "web-form", "admin",
            ipAddress: "127.0.0.1", proofOfConsent: "hash-abc");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: id => id.Should().NotBeEmpty(),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _repository.Received(1).CreateAsync(
            Arg.Is<ConsentAggregate>(a =>
                a.DataSubjectId == "subject-1" &&
                a.Purpose == "marketing" &&
                a.Status == ConsentStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_RepositoryFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaError.New("Repository failure");
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v1", "web-form", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region WithdrawConsentAsync

    [Fact]
    public async Task WithdrawConsentAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var aggregate = CreateActiveAggregate(consentId);

        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.WithdrawConsentAsync(consentId, "admin", "User requested");

        // Assert
        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawConsentAsync_NotFound_ShouldReturnConsentNotFoundError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ConsentAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.WithdrawConsentAsync(consentId, "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(consentId.ToString()));
    }

    [Fact]
    public async Task WithdrawConsentAsync_InvalidState_ShouldReturnInvalidStateTransitionError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var aggregate = CreateWithdrawnAggregate(consentId);

        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));

        // Act — Withdraw on an already-withdrawn aggregate triggers InvalidOperationException
        var result = await _sut.WithdrawConsentAsync(consentId, "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain("Invalid consent state transition"));
    }

    [Fact]
    public async Task WithdrawConsentAsync_SaveFails_ShouldReturnError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var aggregate = CreateActiveAggregate(consentId);

        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaError.New("Save failed")));

        // Act
        var result = await _sut.WithdrawConsentAsync(consentId, "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RenewConsentAsync

    [Fact]
    public async Task RenewConsentAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var aggregate = CreateActiveAggregate(consentId);

        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RenewConsentAsync(
            consentId, "v2", "admin",
            newExpiresAtUtc: FixedNow.AddYears(1),
            source: "web-form");

        // Assert
        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenewConsentAsync_NotFound_ShouldReturnConsentNotFoundError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ConsentAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.RenewConsentAsync(consentId, "v2", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(consentId.ToString()));
    }

    #endregion

    #region ProvideReconsentAsync

    [Fact]
    public async Task ProvideReconsentAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var aggregate = CreateRequiresReconsentAggregate(consentId);

        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.ProvideReconsentAsync(
            consentId, "v3", "web-form", "admin",
            ipAddress: "10.0.0.1");

        // Assert
        result.IsRight.Should().BeTrue();
        await _repository.Received(1).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvideReconsentAsync_NotFound_ShouldReturnConsentNotFoundError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        _repository.LoadAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ConsentAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.ProvideReconsentAsync(
            consentId, "v3", "web-form", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region GetConsentAsync

    [Fact]
    public async Task GetConsentAsync_CacheHit_ShouldReturnCachedModel()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var cachedModel = CreateReadModel(consentId, "subject-1", "marketing");

        _cache.GetAsync<ConsentReadModel>($"consent:{consentId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        // Act
        var result = await _sut.GetConsentAsync(consentId);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: model => model.Id.Should().Be(consentId),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        // Should NOT hit the read model repository
        await _readModelRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConsentAsync_CacheMiss_ShouldQueryRepositoryAndCacheResult()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var readModel = CreateReadModel(consentId, "subject-1", "marketing");

        _cache.GetAsync<ConsentReadModel>($"consent:{consentId}", Arg.Any<CancellationToken>())
            .Returns((ConsentReadModel?)null);
        _readModelRepository.GetByIdAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentReadModel>(readModel));

        // Act
        var result = await _sut.GetConsentAsync(consentId);

        // Assert
        result.IsRight.Should().BeTrue();
        await _cache.Received(1).SetAsync(
            $"consent:{consentId}",
            Arg.Any<ConsentReadModel>(),
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConsentAsync_NotFound_ShouldReturnConsentNotFoundError()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        _cache.GetAsync<ConsentReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ConsentReadModel?)null);
        _readModelRepository.GetByIdAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ConsentReadModel>(EncinaError.New("not found")));

        // Act
        var result = await _sut.GetConsentAsync(consentId);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(consentId.ToString()));
    }

    #endregion

    #region GetConsentBySubjectAndPurposeAsync

    [Fact]
    public async Task GetConsentBySubjectAndPurposeAsync_CacheHit_ShouldReturnSome()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        _cache.GetAsync<ConsentReadModel>("consent:subject:subject-1:purpose:marketing", Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _sut.GetConsentBySubjectAndPurposeAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsSome.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConsentBySubjectAndPurposeAsync_CacheMiss_Found_ShouldReturnSomeAndCache()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        _cache.GetAsync<ConsentReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ConsentReadModel?)null);
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel> { model }));

        // Act
        var result = await _sut.GetConsentBySubjectAndPurposeAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsSome.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _cache.Received(1).SetAsync(
            "consent:subject:subject-1:purpose:marketing",
            Arg.Any<ConsentReadModel>(),
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConsentBySubjectAndPurposeAsync_CacheMiss_NotFound_ShouldReturnNone()
    {
        // Arrange
        _cache.GetAsync<ConsentReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ConsentReadModel?)null);
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel>()));

        // Act
        var result = await _sut.GetConsentBySubjectAndPurposeAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: opt => opt.IsNone.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region HasValidConsentAsync

    [Fact]
    public async Task HasValidConsentAsync_ActiveNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        model.Status = ConsentStatus.Active;
        model.ExpiresAtUtc = FixedNow.AddDays(30);

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel> { model }));

        // Act
        var result = await _sut.HasValidConsentAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: valid => valid.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HasValidConsentAsync_ActiveNoExpiration_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        model.Status = ConsentStatus.Active;
        model.ExpiresAtUtc = null;

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel> { model }));

        // Act
        var result = await _sut.HasValidConsentAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: valid => valid.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HasValidConsentAsync_Expired_ShouldReturnFalse()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        model.Status = ConsentStatus.Active;
        model.ExpiresAtUtc = FixedNow.AddDays(-1); // expired yesterday

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel> { model }));

        // Act
        var result = await _sut.HasValidConsentAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: valid => valid.Should().BeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HasValidConsentAsync_NoMatchingConsent_ShouldReturnFalse()
    {
        // Arrange
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(
                new List<ConsentReadModel>()));

        // Act
        var result = await _sut.HasValidConsentAsync("subject-1", "marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: valid => valid.Should().BeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region GetConsentHistoryAsync

    [Fact]
    public async Task GetConsentHistoryAsync_ShouldReturnEventHistoryUnavailableError()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetConsentHistoryAsync(consentId);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.Should().Contain(consentId.ToString());
                error.Message.Should().Contain("not available");
            });
    }

    #endregion

    #region Helpers

    private static ConsentAggregate CreateActiveAggregate(Guid id)
    {
        return ConsentAggregate.Grant(
            id,
            dataSubjectId: "subject-1",
            purpose: "marketing",
            consentVersionId: "v1",
            source: "web-form",
            ipAddress: null,
            proofOfConsent: null,
            metadata: new Dictionary<string, object?>(),
            expiresAtUtc: null,
            grantedBy: "admin",
            occurredAtUtc: FixedNow.AddDays(-10));
    }

    private static ConsentAggregate CreateWithdrawnAggregate(Guid id)
    {
        var aggregate = CreateActiveAggregate(id);
        aggregate.Withdraw("admin", "User requested", FixedNow.AddDays(-5));
        return aggregate;
    }

    private static ConsentAggregate CreateRequiresReconsentAggregate(Guid id)
    {
        var aggregate = CreateActiveAggregate(id);
        aggregate.ChangeVersion("v2", "Terms updated", requiresReconsent: true, "legal-team", FixedNow.AddDays(-2));
        return aggregate;
    }

    private static ConsentReadModel CreateReadModel(Guid id, string subjectId, string purpose)
    {
        return new ConsentReadModel
        {
            Id = id,
            DataSubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = FixedNow.AddDays(-10),
            Source = "web-form",
            Version = 1,
            LastModifiedAtUtc = FixedNow.AddDays(-10)
        };
    }

    #endregion
}

#pragma warning restore CA2012
