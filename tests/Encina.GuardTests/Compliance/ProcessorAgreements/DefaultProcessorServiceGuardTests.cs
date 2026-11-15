using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="DefaultProcessorService"/> to verify null parameter handling.
/// </summary>
public class DefaultProcessorServiceGuardTests
{
    private readonly IAggregateRepository<ProcessorAggregate> _repository =
        Substitute.For<IAggregateRepository<ProcessorAggregate>>();

    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<ProcessorReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly IRequestContextAccessor _requestContextAccessor = Substitute.For<IRequestContextAccessor>();

    private readonly ILogger<DefaultProcessorService> _logger =
        NullLogger<DefaultProcessorService>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            null!, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, null!, _cache, _options, _timeProvider, _requestContextAccessor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, null!, _options, _timeProvider, _requestContextAccessor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, null!, _timeProvider, _requestContextAccessor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, null!, _requestContextAccessor, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, _timeProvider, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Tenant scope guards

    [Fact]
    public void TenantScope_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorAgreementTenantScope(null!, new ProcessorAgreementOptions(), false, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContextAccessor");
    }

    [Fact]
    public void TenantScope_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorAgreementTenantScope(_requestContextAccessor, null!, false, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void TenantScope_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorAgreementTenantScope(
            _requestContextAccessor, new ProcessorAgreementOptions(), false, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
