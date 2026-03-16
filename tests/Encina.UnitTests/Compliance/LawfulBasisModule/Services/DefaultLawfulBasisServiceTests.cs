using Encina.Caching;
using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Errors;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Compliance.LawfulBasis.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using static LanguageExt.Prelude;
using GDPR = global::Encina.Compliance.GDPR;

#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Services;

/// <summary>
/// Unit tests for <see cref="DefaultLawfulBasisService"/>.
/// </summary>
public class DefaultLawfulBasisServiceTests
{
    private readonly IAggregateRepository<LawfulBasisAggregate> _registrationRepository;
    private readonly IAggregateRepository<LIAAggregate> _liaRepository;
    private readonly IReadModelRepository<LawfulBasisReadModel> _registrationReadModels;
    private readonly IReadModelRepository<LIAReadModel> _liaReadModels;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DefaultLawfulBasisService _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    public DefaultLawfulBasisServiceTests()
    {
        _registrationRepository = Substitute.For<IAggregateRepository<LawfulBasisAggregate>>();
        _liaRepository = Substitute.For<IAggregateRepository<LIAAggregate>>();
        _registrationReadModels = Substitute.For<IReadModelRepository<LawfulBasisReadModel>>();
        _liaReadModels = Substitute.For<IReadModelRepository<LIAReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(FixedNow);

        _sut = new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            _registrationReadModels,
            _liaReadModels,
            _cache,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);
    }

    #region Constructor Guard Clauses

    [Fact]
    public void Constructor_NullRegistrationRepository_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            null!,
            _liaRepository,
            _registrationReadModels,
            _liaReadModels,
            _cache,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("registrationRepository");
    }

    [Fact]
    public void Constructor_NullLIARepository_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            null!,
            _registrationReadModels,
            _liaReadModels,
            _cache,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("liaRepository");
    }

    [Fact]
    public void Constructor_NullRegistrationReadModels_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            null!,
            _liaReadModels,
            _cache,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("registrationReadModels");
    }

    [Fact]
    public void Constructor_NullLIAReadModels_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            _registrationReadModels,
            null!,
            _cache,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("liaReadModels");
    }

    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            _registrationReadModels,
            _liaReadModels,
            null!,
            _timeProvider,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            _registrationReadModels,
            _liaReadModels,
            _cache,
            null!,
            NullLogger<DefaultLawfulBasisService>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultLawfulBasisService(
            _registrationRepository,
            _liaRepository,
            _registrationReadModels,
            _liaReadModels,
            _cache,
            _timeProvider,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_WithValidInput_ReturnsGuid()
    {
        // Arrange
        var id = Guid.NewGuid();
        _registrationRepository.CreateAsync(Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RegisterAsync(
            id, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            purpose: "Order processing", liaReference: null,
            legalReference: null, contractReference: "contract-001");

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: returnedId => returnedId.Should().Be(id),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _registrationRepository.Received(1).CreateAsync(
            Arg.Is<LawfulBasisAggregate>(a =>
                a.RequestTypeName == "MyApp.Commands.CreateOrder" &&
                a.Basis == global::Encina.Compliance.GDPR.LawfulBasis.Contract),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WhenRepositoryFails_ReturnsError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = EncinaError.New("Repository failure");
        _registrationRepository.CreateAsync(Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act
        var result = await _sut.RegisterAsync(
            id, "MyApp.Commands.CreateOrder", global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            purpose: null, liaReference: null, legalReference: null, contractReference: null);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region ChangeBasisAsync

    [Fact]
    public async Task ChangeBasisAsync_WhenRegistrationExists_ReturnsUnit()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var aggregate = CreateActiveRegistration(registrationId);

        _registrationRepository.LoadAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LawfulBasisAggregate>(aggregate));
        _registrationRepository.SaveAsync(Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.ChangeBasisAsync(
            registrationId, global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests,
            purpose: "Fraud prevention", liaReference: "LIA-001",
            legalReference: null, contractReference: null);

        // Assert
        result.IsRight.Should().BeTrue();
        await _registrationRepository.Received(1).SaveAsync(
            Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeBasisAsync_WhenNotFound_ReturnsRegistrationNotFoundError()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _registrationRepository.LoadAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LawfulBasisAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.ChangeBasisAsync(
            registrationId, global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests,
            purpose: null, liaReference: null, legalReference: null, contractReference: null);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(registrationId.ToString()));
    }

    #endregion

    #region RevokeAsync

    [Fact]
    public async Task RevokeAsync_WhenExists_ReturnsUnit()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var aggregate = CreateActiveRegistration(registrationId);

        _registrationRepository.LoadAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LawfulBasisAggregate>(aggregate));
        _registrationRepository.SaveAsync(Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RevokeAsync(registrationId, "No longer needed");

        // Assert
        result.IsRight.Should().BeTrue();
        await _registrationRepository.Received(1).SaveAsync(
            Arg.Any<LawfulBasisAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAsync_WhenNotFound_ReturnsError()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _registrationRepository.LoadAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LawfulBasisAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.RevokeAsync(registrationId, "No longer needed");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(registrationId.ToString()));
    }

    #endregion

    #region CreateLIAAsync

    [Fact]
    public async Task CreateLIAAsync_WithValidInput_ReturnsGuid()
    {
        // Arrange
        var id = Guid.NewGuid();
        _liaRepository.CreateAsync(Arg.Any<LIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.CreateLIAAsync(
            id,
            reference: "LIA-2026-FRAUD-001",
            name: "Fraud Prevention LIA",
            purpose: "Detect and prevent fraudulent transactions",
            legitimateInterest: "Protecting business from fraud",
            benefits: "Reduced financial losses",
            consequencesIfNotProcessed: "Increased fraud exposure",
            necessityJustification: "No less intrusive alternative available",
            alternativesConsidered: ["Manual review", "Rule-based filtering"],
            dataMinimisationNotes: "Only transaction metadata is processed",
            natureOfData: "Transaction data, IP addresses",
            reasonableExpectations: "Users expect fraud protection",
            impactAssessment: "Minimal impact on individual rights",
            safeguards: ["Encryption", "Access controls"],
            assessedBy: "DPO",
            dpoInvolvement: true);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: returnedId => returnedId.Should().Be(id),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        await _liaRepository.Received(1).CreateAsync(
            Arg.Is<LIAAggregate>(a =>
                a.Reference == "LIA-2026-FRAUD-001" &&
                a.Name == "Fraud Prevention LIA"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ApproveLIAAsync

    [Fact]
    public async Task ApproveLIAAsync_WhenExists_ReturnsUnit()
    {
        // Arrange
        var liaId = Guid.NewGuid();
        var aggregate = CreatePendingLIA(liaId);

        _liaRepository.LoadAsync(liaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LIAAggregate>(aggregate));
        _liaRepository.SaveAsync(Arg.Any<LIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.ApproveLIAAsync(
            liaId, "Balancing test favors controller", "dpo@example.com");

        // Assert
        result.IsRight.Should().BeTrue();
        await _liaRepository.Received(1).SaveAsync(
            Arg.Any<LIAAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLIAAsync_WhenNotFound_ReturnsLIANotFoundError()
    {
        // Arrange
        var liaId = Guid.NewGuid();
        _liaRepository.LoadAsync(liaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LIAAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.ApproveLIAAsync(
            liaId, "Balancing test favors controller", "dpo@example.com");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(liaId.ToString()));
    }

    #endregion

    #region RejectLIAAsync

    [Fact]
    public async Task RejectLIAAsync_WhenExists_ReturnsUnit()
    {
        // Arrange
        var liaId = Guid.NewGuid();
        var aggregate = CreatePendingLIA(liaId);

        _liaRepository.LoadAsync(liaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LIAAggregate>(aggregate));
        _liaRepository.SaveAsync(Arg.Any<LIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RejectLIAAsync(
            liaId, "Data subject rights override", "dpo@example.com");

        // Assert
        result.IsRight.Should().BeTrue();
        await _liaRepository.Received(1).SaveAsync(
            Arg.Any<LIAAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectLIAAsync_WhenNotFound_ReturnsLIANotFoundError()
    {
        // Arrange
        var liaId = Guid.NewGuid();
        _liaRepository.LoadAsync(liaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LIAAggregate>(EncinaError.New("not found")));

        // Act
        var result = await _sut.RejectLIAAsync(
            liaId, "Data subject rights override", "dpo@example.com");

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(liaId.ToString()));
    }

    #endregion

    #region GetRegistrationAsync

    [Fact]
    public async Task GetRegistrationAsync_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var cachedModel = CreateRegistrationReadModel(registrationId);

        _cache.GetAsync<LawfulBasisReadModel>($"lb:reg:{registrationId}", Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        // Act
        var result = await _sut.GetRegistrationAsync(registrationId);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: model => model.Id.Should().Be(registrationId),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        // Should NOT hit the read model repository
        await _registrationReadModels.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrationAsync_WhenNotCached_QueriesRepository()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var readModel = CreateRegistrationReadModel(registrationId);

        _cache.GetAsync<LawfulBasisReadModel>($"lb:reg:{registrationId}", Arg.Any<CancellationToken>())
            .Returns((LawfulBasisReadModel?)null);
        _registrationReadModels.GetByIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LawfulBasisReadModel>(readModel));

        // Act
        var result = await _sut.GetRegistrationAsync(registrationId);

        // Assert
        result.IsRight.Should().BeTrue();
        await _cache.Received(1).SetAsync(
            $"lb:reg:{registrationId}",
            Arg.Any<LawfulBasisReadModel>(),
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrationAsync_WhenNotFound_ReturnsRegistrationNotFoundError()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        _cache.GetAsync<LawfulBasisReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((LawfulBasisReadModel?)null);
        _registrationReadModels.GetByIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, LawfulBasisReadModel>(EncinaError.New("not found")));

        // Act
        var result = await _sut.GetRegistrationAsync(registrationId);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.Should().Contain(registrationId.ToString()));
    }

    #endregion

    #region HasApprovedLIAAsync

    [Fact]
    public async Task HasApprovedLIAAsync_WhenApproved_ReturnsTrue()
    {
        // Arrange
        var liaReference = "LIA-2026-FRAUD-001";
        var liaModel = CreateLIAReadModel(Guid.NewGuid(), liaReference, global::Encina.Compliance.GDPR.LIAOutcome.Approved);

        _cache.GetAsync<LIAReadModel>($"lb:lia:ref:{liaReference}", Arg.Any<CancellationToken>())
            .Returns((LIAReadModel?)null);
        _liaReadModels.QueryAsync(
                Arg.Any<Func<IQueryable<LIAReadModel>, IQueryable<LIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LIAReadModel>>(
                new List<LIAReadModel> { liaModel }));

        // Act
        var result = await _sut.HasApprovedLIAAsync(liaReference);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: approved => approved.Should().BeTrue(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HasApprovedLIAAsync_WhenNotApproved_ReturnsFalse()
    {
        // Arrange
        var liaReference = "LIA-2026-FRAUD-002";
        var liaModel = CreateLIAReadModel(Guid.NewGuid(), liaReference, global::Encina.Compliance.GDPR.LIAOutcome.Rejected);

        _cache.GetAsync<LIAReadModel>($"lb:lia:ref:{liaReference}", Arg.Any<CancellationToken>())
            .Returns((LIAReadModel?)null);
        _liaReadModels.QueryAsync(
                Arg.Any<Func<IQueryable<LIAReadModel>, IQueryable<LIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LIAReadModel>>(
                new List<LIAReadModel> { liaModel }));

        // Act
        var result = await _sut.HasApprovedLIAAsync(liaReference);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: approved => approved.Should().BeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task HasApprovedLIAAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        var liaReference = "LIA-NONEXISTENT";

        _cache.GetAsync<LIAReadModel>($"lb:lia:ref:{liaReference}", Arg.Any<CancellationToken>())
            .Returns((LIAReadModel?)null);
        _liaReadModels.QueryAsync(
                Arg.Any<Func<IQueryable<LIAReadModel>, IQueryable<LIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<LIAReadModel>>(
                new List<LIAReadModel>()));

        // Act
        var result = await _sut.HasApprovedLIAAsync(liaReference);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: approved => approved.Should().BeFalse(),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Helpers

    private static LawfulBasisAggregate CreateActiveRegistration(Guid id)
    {
        return LawfulBasisAggregate.Register(
            id,
            requestTypeName: "MyApp.Commands.CreateOrder",
            basis: global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            purpose: "Order processing",
            liaReference: null,
            legalReference: null,
            contractReference: "contract-001",
            registeredAtUtc: FixedNow.AddDays(-10));
    }

    private static LIAAggregate CreatePendingLIA(Guid id)
    {
        return LIAAggregate.Create(
            id,
            reference: "LIA-2026-FRAUD-001",
            name: "Fraud Prevention LIA",
            purpose: "Detect fraudulent transactions",
            legitimateInterest: "Protecting business from fraud",
            benefits: "Reduced financial losses",
            consequencesIfNotProcessed: "Increased fraud exposure",
            necessityJustification: "No less intrusive alternative",
            alternativesConsidered: ["Manual review"],
            dataMinimisationNotes: "Only transaction metadata",
            natureOfData: "Transaction data",
            reasonableExpectations: "Users expect fraud protection",
            impactAssessment: "Minimal impact",
            safeguards: ["Encryption"],
            assessedBy: "DPO",
            dpoInvolvement: true,
            assessedAtUtc: FixedNow.AddDays(-5));
    }

    private static LawfulBasisReadModel CreateRegistrationReadModel(Guid id)
    {
        return new LawfulBasisReadModel
        {
            Id = id,
            RequestTypeName = "MyApp.Commands.CreateOrder",
            Basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract,
            Purpose = "Order processing",
            ContractReference = "contract-001",
            RegisteredAtUtc = FixedNow.AddDays(-10),
            LastModifiedAtUtc = FixedNow.AddDays(-10),
            Version = 1
        };
    }

    private static LIAReadModel CreateLIAReadModel(Guid id, string reference, global::Encina.Compliance.GDPR.LIAOutcome outcome)
    {
        return new LIAReadModel
        {
            Id = id,
            Reference = reference,
            Name = "Test LIA",
            Purpose = "Test purpose",
            LegitimateInterest = "Test interest",
            Benefits = "Test benefits",
            ConsequencesIfNotProcessed = "Test consequences",
            NecessityJustification = "Test justification",
            AlternativesConsidered = ["Alt 1"],
            DataMinimisationNotes = "Test notes",
            NatureOfData = "Test data",
            ReasonableExpectations = "Test expectations",
            ImpactAssessment = "Test impact",
            Safeguards = ["Safeguard 1"],
            AssessedBy = "DPO",
            DPOInvolvement = true,
            AssessedAtUtc = FixedNow.AddDays(-5),
            Outcome = outcome,
            LastModifiedAtUtc = FixedNow.AddDays(-5),
            Version = 1
        };
    }

    #endregion
}
