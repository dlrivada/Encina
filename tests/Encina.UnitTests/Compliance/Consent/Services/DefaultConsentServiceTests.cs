using Encina.Caching;
using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Events;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
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
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly DefaultConsentService _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    public DefaultConsentServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(FixedNow);
        _requestContextAccessor = Substitute.For<IRequestContextAccessor>();

        _sut = new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
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
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            null!,
            _cache,
            _timeProvider,
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            null!,
            _timeProvider,
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _requestContextAccessor,
            null!,
            NullLogger<DefaultConsentService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _requestContextAccessor,
            Options.Create(new ConsentOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: id => id.ShouldNotBe(Guid.Empty),
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
        result.IsLeft.ShouldBeTrue();
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_FirstGrant_CreatesStreamWithDeterministicId()
    {
        // Arrange
        var expectedId = ConsentStreamId.For(null, "subject-1", "marketing");
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — the returned id and the created stream id are the key-derived id (#3769).
        result.ShouldBe(Right<EncinaError, Guid>(expectedId));
        await _repository.Received(1).CreateAsync(
            Arg.Is<ConsentAggregate>(a => a.Id == expectedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_SameKeyTwice_ReturnsSameId()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var first = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");
        var second = await _sut.GrantConsentAsync("subject-1", "marketing", "v2", "api", "other-actor");

        // Assert — only the key decides the id, not the version, source or actor.
        second.ShouldBe(first);
    }

    [Fact]
    public async Task GrantConsentAsync_NullAndEmptyTenant_DeriveSameId()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var withNull = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", tenantId: null);
        var withEmpty = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", tenantId: string.Empty);

        // Assert
        withEmpty.ShouldBe(withNull);
    }

    [Fact]
    public async Task GrantConsentAsync_DifferentTenantSubjectOrPurpose_DeriveDifferentIds()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var baseline = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", tenantId: "tenant-a");
        var otherTenant = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", tenantId: "tenant-b");
        var otherSubject = await _sut.GrantConsentAsync("subject-2", "marketing", "v1", "web-form", "admin", tenantId: "tenant-a");
        var otherPurpose = await _sut.GrantConsentAsync("subject-1", "analytics", "v1", "web-form", "admin", tenantId: "tenant-a");

        // Assert
        var ids = new[] { baseline, otherTenant, otherSubject, otherPurpose }
            .Select(r => r.Match(Right: id => id, Left: _ => Guid.Empty))
            .ToList();
        ids.ShouldNotContain(Guid.Empty);
        ids.Distinct().Count().ShouldBe(4);
    }

    [Fact]
    public async Task GrantConsentAsync_AmbientTenant_IsPartOfTheKey()
    {
        // Arrange — no explicit tenant: the effective tenant is the ambient one.
        _requestContextAccessor.RequestContext.Returns(RequestContext.CreateForTest(tenantId: "tenant-a"));
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert
        result.ShouldBe(Right<EncinaError, Guid>(ConsentStreamId.For("tenant-a", "subject-1", "marketing")));
        await _repository.Received(1).CreateAsync(
            Arg.Is<ConsentAggregate>(a => a.TenantId == "tenant-a"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_NoOccurredAtUtc_UsesInjectedClockForTheAct()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert
        await _repository.Received(1).CreateAsync(
            Arg.Is<ConsentAggregate>(a => a.GivenAtUtc == FixedNow && a.LastActAtUtc == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExplicitOccurredAtUtc_IsTheActTimestamp()
    {
        // Arrange — the subject acted earlier than Encina received the request (#3771).
        var actedAt = FixedNow.AddMinutes(-3);
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", occurredAtUtc: actedAt);

        // Assert
        await _repository.Received(1).CreateAsync(
            Arg.Is<ConsentAggregate>(a => a.GivenAtUtc == actedAt && a.LastActAtUtc == actedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingActive_RecordsRenewalWithTheNewProof()
    {
        // Arrange
        var (id, existing) = ArrangeExisting(CreateActiveAggregate);

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v2", "api", "admin",
            ipAddress: "10.0.0.1", proofOfConsent: "form-hash-2", expiresAtUtc: FixedNow.AddYears(1));

        // Assert — a repeated grant while Active is a renewal carrying the full proof (D4).
        result.ShouldBe(Right<EncinaError, Guid>(id));
        var renewal = existing.UncommittedEvents.OfType<ConsentRenewedByGrant>().ShouldHaveSingleItem();
        renewal.ConsentVersionId.ShouldBe("v2");
        renewal.ProofOfConsent.ShouldBe("form-hash-2");
        renewal.OccurredAtUtc.ShouldBe(FixedNow);
        existing.Status.ShouldBe(ConsentStatus.Active);
        existing.ConsentVersionId.ShouldBe("v2");
        existing.ExpiresAtUtc.ShouldBe(FixedNow.AddYears(1));
        await _repository.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"consent:{id}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("consent:tenant:-:subject:subject-1:purpose:marketing", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingActive_IdenticalRepeats_AppendIdenticalEvents()
    {
        // Arrange
        var (_, existing) = ArrangeExisting(CreateActiveAggregate);

        // Act
        await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", occurredAtUtc: FixedNow);
        await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin", occurredAtUtc: FixedNow);

        // Assert
        var renewals = existing.UncommittedEvents.OfType<ConsentRenewedByGrant>().ToList();
        renewals.Count.ShouldBe(2);
        renewals[1].ShouldBe(renewals[0] with { Metadata = renewals[1].Metadata });
        renewals[1].Metadata.ShouldBeEmpty();
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingActive_LateOlderAct_IsRecordedWithoutChangingState()
    {
        // Arrange — the record's latest act was granted 10 days ago; this act claims 20 days ago.
        var (id, existing) = ArrangeExisting(CreateActiveAggregate);

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v0", "paper", "admin", occurredAtUtc: FixedNow.AddDays(-20));

        // Assert
        result.ShouldBe(Right<EncinaError, Guid>(id));
        existing.UncommittedEvents.OfType<ConsentRenewedByGrant>().ShouldHaveSingleItem()
            .ConsentVersionId.ShouldBe("v0");
        existing.ConsentVersionId.ShouldBe("v1");
        existing.Source.ShouldBe("web-form");
        existing.LastActAtUtc.ShouldBe(FixedNow.AddDays(-10));
        await _repository.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingWithdrawn_RecordsRegrantAndReactivates()
    {
        // Arrange
        var (id, existing) = ArrangeExisting(CreateWithdrawnAggregate);

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v3", "api", "admin", proofOfConsent: "form-hash-3");

        // Assert — D5: an explicit regrant on the same stream.
        result.ShouldBe(Right<EncinaError, Guid>(id));
        var regrant = existing.UncommittedEvents.OfType<ConsentRegranted>().ShouldHaveSingleItem();
        regrant.PreviousStatus.ShouldBe(ConsentStatus.Withdrawn);
        regrant.ProofOfConsent.ShouldBe("form-hash-3");
        existing.Status.ShouldBe(ConsentStatus.Active);
        existing.WithdrawnAtUtc.ShouldBeNull();
        existing.GivenAtUtc.ShouldBe(FixedNow);
        await _cache.Received(1).RemoveAsync($"consent:{id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingWithdrawn_ActOlderThanTheWithdrawal_StaysWithdrawn()
    {
        // Arrange — withdrawn 5 days ago; a grant made 7 days ago arrives only now.
        var (_, existing) = ArrangeExisting(CreateWithdrawnAggregate);

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v1", "web-form", "admin", occurredAtUtc: FixedNow.AddDays(-7));

        // Assert — recorded in the history, never undoing the later withdrawal (Art. 7(3)).
        result.IsRight.ShouldBeTrue();
        existing.UncommittedEvents.OfType<ConsentRegranted>().ShouldHaveSingleItem();
        existing.Status.ShouldBe(ConsentStatus.Withdrawn);
        existing.WithdrawnAtUtc.ShouldBe(FixedNow.AddDays(-5));
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingExpired_RecordsRegrantAndReactivates()
    {
        // Arrange
        var (_, existing) = ArrangeExisting(CreateExpiredAggregate);

        // Act
        var result = await _sut.GrantConsentAsync(
            "subject-1", "marketing", "v1", "web-form", "admin", expiresAtUtc: FixedNow.AddDays(30));

        // Assert
        result.IsRight.ShouldBeTrue();
        existing.UncommittedEvents.OfType<ConsentRegranted>().ShouldHaveSingleItem()
            .PreviousStatus.ShouldBe(ConsentStatus.Expired);
        existing.Status.ShouldBe(ConsentStatus.Active);
        existing.ExpiresAtUtc.ShouldBe(FixedNow.AddDays(30));
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingRequiresReconsent_CurrentVersion_CountsAsTheReconsent()
    {
        // Arrange — the terms changed to v2 and require reconsent.
        var (id, existing) = ArrangeExisting(CreateRequiresReconsentAggregate);

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v2", "web-form", "admin");

        // Assert
        result.ShouldBe(Right<EncinaError, Guid>(id));
        existing.UncommittedEvents.OfType<ConsentReconsentProvided>().ShouldHaveSingleItem()
            .NewConsentVersionId.ShouldBe("v2");
        existing.Status.ShouldBe(ConsentStatus.Active);
        await _repository.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingRequiresReconsent_OlderVersion_IsRejected()
    {
        // Arrange
        var (_, existing) = ArrangeExisting(CreateRequiresReconsentAggregate);

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — a coded InvalidRequest error that points to ProvideReconsentAsync (D5, D8).
        var error = result.Match(Right: _ => throw new InvalidOperationException("Expected Left"), Left: e => e);
        error.GetCode().ShouldBe(Some(ConsentErrors.ReconsentRequiredCode));
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
        error.Message.ShouldContain("ProvideReconsentAsync");
        error.Message.ShouldNotContain("subject-1");
        existing.Status.ShouldBe(ConsentStatus.RequiresReconsent);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_TenantRequiredAndMissing_FailsClosedBeforeTouchingTheStream()
    {
        // Arrange
        var sut = new DefaultConsentService(
            _repository, _readModelRepository, _cache, _timeProvider, _requestContextAccessor,
            Options.Create(new ConsentOptions { RequireTenantContext = true }),
            NullLogger<DefaultConsentService>.Instance);

        // Act
        var result = await sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — the same fail-closed check guards a renewal or a regrant.
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(ConsentErrors.TenantRequiredCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingStream_ConcurrencyConflictOnce_ReloadsAndRetries()
    {
        // Arrange — another write wins the race; the retry sees the reloaded (withdrawn) stream.
        var id = ConsentStreamId.For(null, "subject-1", "marketing");
        var stale = CreateActiveAggregate(id);
        var reloaded = CreateWithdrawnAggregate(id);
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.StreamAlreadyExists, "exists")));
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(stale), Right<EncinaError, ConsentAggregate>(reloaded));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(
                Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.ConcurrencyConflict, "conflict")),
                Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — evaluated against the state that won: a regrant, not a renewal.
        result.ShouldBe(Right<EncinaError, Guid>(id));
        await _repository.Received(2).LoadAsync(id, Arg.Any<CancellationToken>());
        await _repository.Received(2).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
        reloaded.UncommittedEvents.OfType<ConsentRegranted>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingStream_ConflictOnEveryAttempt_ReturnsTheConflictAfterFiveAttempts()
    {
        // Arrange
        var id = ConsentStreamId.For(null, "subject-1", "marketing");
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.StreamAlreadyExists, "exists")));
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => Right<EncinaError, ConsentAggregate>(CreateActiveAggregate(id)));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.ConcurrencyConflict, "conflict")));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — bounded retries, then reported so the caller can retry; never a success.
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(MartenErrorCodes.ConcurrencyConflict);
        await _repository.Received(5).LoadAsync(id, Arg.Any<CancellationToken>());
        await _repository.Received(5).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_ExistingStream_OtherSaveError_IsReturnedWithoutRetry()
    {
        // Arrange
        var (_, _) = ArrangeExisting(CreateActiveAggregate);
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.SaveFailed, "save failed")));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(MartenErrorCodes.SaveFailed);
        await _repository.Received(1).SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_StreamAlreadyExists_ReloadFails_ReturnsReloadError()
    {
        // Arrange
        var existingId = ConsentStreamId.For(null, "subject-1", "marketing");
        var loadError = EncinaErrors.Create(MartenErrorCodes.LoadFailed, "Load failed");
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.StreamAlreadyExists, "Stream already exists")));
        _repository.LoadAsync(existingId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ConsentAggregate>(loadError));

        // Act
        var result = await _sut.GrantConsentAsync("subject-1", "marketing", "v1", "web-form", "admin");

        // Assert — the failure is reported, never turned into a success.
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetCode().IfSome(code => code.ShouldBe(MartenErrorCodes.LoadFailed)));
    }

    [Fact]
    public async Task GrantConsentAsync_NullSubject_ReturnsServiceError()
    {
        // Act
        var result = await _sut.GrantConsentAsync(null!, "marketing", "v1", "web-form", "admin");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetCode().IfSome(code => code.ShouldBe(ConsentErrors.ServiceErrorCode)));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
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
        result.IsRight.ShouldBeTrue();
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
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain(consentId.ToString()));
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
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("Invalid consent state transition"));
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
        result.IsLeft.ShouldBeTrue();
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
        result.IsRight.ShouldBeTrue();
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
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain(consentId.ToString()));
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
        result.IsRight.ShouldBeTrue();
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
        result.IsLeft.ShouldBeTrue();
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: model => model.Id.ShouldBe(consentId),
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
        result.IsRight.ShouldBeTrue();
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
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain(consentId.ToString()));
    }

    #endregion

    #region GetConsentBySubjectAndPurposeAsync

    [Fact]
    public async Task GetConsentBySubjectAndPurposeAsync_CacheHit_ShouldReturnSome()
    {
        // Arrange
        var model = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        _cache.GetAsync<ConsentReadModel>("consent:tenant:-:subject:subject-1:purpose:marketing", Arg.Any<CancellationToken>())
            .Returns(model);

        // Act
        var result = await _sut.GetConsentBySubjectAndPurposeAsync("subject-1", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: opt => opt.IsSome.ShouldBeTrue(),
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: opt => opt.IsSome.ShouldBeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _cache.Received(1).SetAsync(
            "consent:tenant:-:subject:subject-1:purpose:marketing",
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: opt => opt.IsNone.ShouldBeTrue(),
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: valid => valid.ShouldBeTrue(),
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: valid => valid.ShouldBeTrue(),
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: valid => valid.ShouldBeFalse(),
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
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: valid => valid.ShouldBeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Tenant scoping (#1315)

    [Fact]
    public async Task GetAllConsentsAsync_SingleTenantApp_NoTenantProvider_NoAmbientTenant_ShouldRunUnscopedQuery()
    {
        // Arrange — no ITenantProvider registered (single-tenant app), no ambient tenant.
        var sut = CreateServiceWithTenancy(tenantProvider: null, requireTenantContext: null);
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(new List<ConsentReadModel>()));

        // Act
        var result = await sut.GetAllConsentsAsync("subject-1");

        // Assert — no tenant registered, so the auto-detected default allows the unscoped query.
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllConsentsAsync_MultiTenantApp_NoAmbientTenant_ShouldFailClosed()
    {
        // Arrange — ITenantProvider registered (multi-tenant app), but no ambient tenant on this call.
        var sut = CreateServiceWithTenancy(tenantProvider: Substitute.For<ITenantProvider>(), requireTenantContext: null);
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);

        // Act
        var result = await sut.GetAllConsentsAsync("subject-1");

        // Assert — auto-detected multi-tenant application fails closed (SPEC-002 DEC-006/DEC-009).
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetCode().IfSome(code => code.ShouldBe(ConsentErrors.TenantRequiredCode)));

        await _readModelRepository.DidNotReceive().QueryAsync(
            Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllConsentsAsync_MultiTenantApp_ExplicitOptOut_ShouldRunUnscopedQuery()
    {
        // Arrange — application is multi-tenant, but explicitly opted out of enforcement.
        var sut = CreateServiceWithTenancy(tenantProvider: Substitute.For<ITenantProvider>(), requireTenantContext: false);
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(new List<ConsentReadModel>()));

        // Act
        var result = await sut.GetAllConsentsAsync("subject-1");

        // Assert — the explicit opt-out is honored (and logged, see ConsentTenantEnforcementOptedOut).
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllConsentsAsync_SingleTenantApp_ExplicitRequireTenantContext_ShouldFailClosed()
    {
        // Arrange — no ITenantProvider registered, but the application explicitly forces enforcement.
        var sut = CreateServiceWithTenancy(tenantProvider: null, requireTenantContext: true);
        _requestContextAccessor.RequestContext.Returns((IRequestContext?)null);

        // Act
        var result = await sut.GetAllConsentsAsync("subject-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetCode().IfSome(code => code.ShouldBe(ConsentErrors.TenantRequiredCode)));
    }

    [Fact]
    public async Task GetAllConsentsAsync_WithAmbientTenant_ShouldFilterByTenantId()
    {
        // Arrange
        var sut = CreateServiceWithTenancy(tenantProvider: Substitute.For<ITenantProvider>(), requireTenantContext: null);
        _requestContextAccessor.RequestContext.Returns(RequestContext.CreateForTest(tenantId: "tenant-a"));

        var ownRecord = CreateReadModel(Guid.NewGuid(), "subject-1", "marketing");
        ownRecord.TenantId = "tenant-a";

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>();
                var all = new List<ConsentReadModel>
                {
                    ownRecord,
                    new() { Id = Guid.NewGuid(), DataSubjectId = "subject-1", Purpose = "marketing", TenantId = "tenant-b" }
                };
                var filtered = predicate(all.AsQueryable()).ToList();
                return Right<EncinaError, IReadOnlyList<ConsentReadModel>>(filtered);
            });

        // Act
        var result = await sut.GetAllConsentsAsync("subject-1");

        // Assert — only the ambient tenant's record is returned; tenant-b's is never observed.
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: models =>
            {
                models.Count.ShouldBe(1);
                models[0].TenantId.ShouldBe("tenant-a");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetConsentAsync_CachedModelFromDifferentTenant_ShouldBeTreatedAsMiss()
    {
        // Arrange
        var sut = CreateServiceWithTenancy(tenantProvider: null, requireTenantContext: null);
        _requestContextAccessor.RequestContext.Returns(RequestContext.CreateForTest(tenantId: "tenant-a"));

        var consentId = Guid.NewGuid();
        var cachedFromOtherTenant = CreateReadModel(consentId, "subject-1", "marketing");
        cachedFromOtherTenant.TenantId = "tenant-b";

        var freshFromOwnTenant = CreateReadModel(consentId, "subject-1", "marketing");
        freshFromOwnTenant.TenantId = "tenant-a";

        _cache.GetAsync<ConsentReadModel>($"consent:{consentId}", Arg.Any<CancellationToken>())
            .Returns(cachedFromOtherTenant);
        _readModelRepository.GetByIdAsync(consentId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentReadModel>(freshFromOwnTenant));

        // Act
        var result = await sut.GetConsentAsync(consentId);

        // Assert — the mismatched cache entry is never returned; the tenant-scoped read model is.
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: model => model.TenantId.ShouldBe("tenant-a"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    private DefaultConsentService CreateServiceWithTenancy(ITenantProvider? tenantProvider, bool? requireTenantContext)
    {
        var options = new ConsentOptions { RequireTenantContext = requireTenantContext };
        return new DefaultConsentService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _requestContextAccessor,
            Options.Create(options),
            NullLogger<DefaultConsentService>.Instance,
            tenantProvider);
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
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain(consentId.ToString());
                error.Message.ShouldContain("not available");
            });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Arranges a grant whose stream already exists (#3771): creation reports
    /// StreamAlreadyExists, the load returns the given aggregate and saving succeeds.
    /// </summary>
    private (Guid Id, ConsentAggregate Existing) ArrangeExisting(Func<Guid, ConsentAggregate> factory)
    {
        var id = ConsentStreamId.For(null, "subject-1", "marketing");
        var existing = factory(id);
        existing.ClearUncommittedEvents();
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.StreamAlreadyExists, "Stream already exists")));
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(existing));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        return (id, existing);
    }

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

    private static ConsentAggregate CreateExpiredAggregate(Guid id)
    {
        var aggregate = ConsentAggregate.Grant(
            id, "subject-1", "marketing", "v1", "web-form", null, null,
            new Dictionary<string, object?>(), FixedNow.AddDays(-1), "admin", FixedNow.AddDays(-10));
        aggregate.Expire(FixedNow);
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
