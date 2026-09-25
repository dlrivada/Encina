using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.Options;

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
}
