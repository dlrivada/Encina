using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="DefaultConsentService"/> to verify null parameter handling
/// in the constructor.
/// </summary>
public class DefaultConsentServiceGuardTests
{
    private readonly IAggregateRepository<ConsentAggregate> _repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
    private readonly IReadModelRepository<ConsentReadModel> _readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly IRequestContextAccessor _requestContextAccessor = Substitute.For<IRequestContextAccessor>();
    private readonly IOptions<ConsentOptions> _options = Options.Create(new ConsentOptions());
    private readonly ILogger<DefaultConsentService> _logger = NullLogger<DefaultConsentService>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            null!, _readModelRepository, _cache, _timeProvider, _requestContextAccessor, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, null!, _cache, _timeProvider, _requestContextAccessor, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, _readModelRepository, null!, _timeProvider, _requestContextAccessor, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, _readModelRepository, _cache, null!, _requestContextAccessor, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, _readModelRepository, _cache, _timeProvider, null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, _readModelRepository, _cache, _timeProvider, _requestContextAccessor, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentService(
            _repository, _readModelRepository, _cache, _timeProvider, _requestContextAccessor, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region GrantConsentAsync Guards (#3771)

    [Theory]
    [InlineData(null, "marketing", "v1", "web", "admin")]
    [InlineData("subject-1", null, "v1", "web", "admin")]
    [InlineData("subject-1", "marketing", null, "web", "admin")]
    [InlineData("subject-1", "marketing", "   ", "web", "admin")]
    [InlineData("subject-1", "marketing", "v1", "", "admin")]
    [InlineData("subject-1", "marketing", "v1", "web", " ")]
    public async Task GrantConsentAsync_InvalidArgument_ReturnsServiceErrorWithoutTouchingTheStore(
        string? subjectId, string? purpose, string? versionId, string? source, string? grantedBy)
    {
        var sut = CreateSut();

        var result = await sut.GrantConsentAsync(subjectId!, purpose!, versionId!, source!, grantedBy!);

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(ConsentErrors.ServiceErrorCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_OccurredAtUtcInTheFuture_ReturnsActInFuture()
    {
        var sut = CreateSut();

        var result = await sut.GrantConsentAsync(
            "subject-1", "marketing", "v1", "web", "admin", occurredAtUtc: DateTimeOffset.UtcNow.AddDays(1));

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(ConsentErrors.ActInFutureCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GrantConsentAsync_NullOptionalArgumentsOnAnExistingStream_RecordsTheRenewal()
    {
        // Null ip, proof, metadata, expiry, tenant, module and occurredAtUtc are all valid.
        var id = ConsentStreamId.For(null, "subject-1", "marketing");
        var existing = ConsentAggregate.Grant(
            id, "subject-1", "marketing", "v1", "web", null, null,
            new Dictionary<string, object?>(), null, "admin", DateTimeOffset.UtcNow.AddDays(-1));
        _repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(MartenErrorCodes.StreamAlreadyExists, "exists")));
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(existing));
        _repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut();

        var result = await sut.GrantConsentAsync("subject-1", "marketing", "v1", "web", "admin");

        result.ShouldBe(Right<EncinaError, Guid>(id));
        existing.Status.ShouldBe(ConsentStatus.Active);
        await _repository.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
    }

    private DefaultConsentService CreateSut() =>
        new(_repository, _readModelRepository, _cache, _timeProvider, _requestContextAccessor, _options, _logger);

    #endregion
}
