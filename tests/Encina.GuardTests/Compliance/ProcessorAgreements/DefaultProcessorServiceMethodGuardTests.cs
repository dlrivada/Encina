using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;
using Microsoft.Extensions.Options;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Method-level guard tests for <see cref="DefaultProcessorService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DefaultProcessorService"/> is an <c>internal sealed</c> class accessible via
/// <c>InternalsVisibleTo("Encina.GuardTests")</c>.
/// </para>
/// <para>
/// The public methods of <see cref="DefaultProcessorService"/> (RegisterProcessorAsync,
/// UpdateProcessorAsync, GetProcessorAsync, etc.) do NOT use
/// <c>ArgumentNullException.ThrowIfNull</c> on their parameters; they rely on
/// constructor-injected dependencies and use try/catch for runtime errors.
/// This test class validates constructor guard completeness and documents
/// the absence of method-level guards.
/// </para>
/// </remarks>
public sealed class DefaultProcessorServiceMethodGuardTests
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

    #region Constructor Guard Completeness

    /// <summary>
    /// Verifies that the constructor accepts all valid non-null parameters without throwing,
    /// with and without the optional tenant provider.
    /// </summary>
    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        Should.NotThrow(() => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, _logger));
        Should.NotThrow(() => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, _logger,
            Substitute.For<ITenantProvider>()));
    }

    /// <summary>
    /// Verifies that every required constructor parameter is individually guarded by
    /// trying each null permutation and expecting ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_EachParameter_IsGuarded()
    {
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(null!, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, _logger))
            .ParamName.ShouldBe("repository");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, null!, _cache, _options, _timeProvider, _requestContextAccessor, _logger))
            .ParamName.ShouldBe("readModelRepository");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, null!, _options, _timeProvider, _requestContextAccessor, _logger))
            .ParamName.ShouldBe("cache");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, null!, _timeProvider, _requestContextAccessor, _logger))
            .ParamName.ShouldBe("options");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, _options, null!, _requestContextAccessor, _logger))
            .ParamName.ShouldBe("timeProvider");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, _options, _timeProvider, null!, _logger))
            .ParamName.ShouldBe("requestContextAccessor");

        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, null!))
            .ParamName.ShouldBe("logger");
    }

    #endregion
}
