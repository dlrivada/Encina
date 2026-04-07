using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Method-level guard tests for <see cref="DefaultDPAService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DefaultDPAService"/> is an <c>internal sealed</c> class accessible via
/// <c>InternalsVisibleTo("Encina.GuardTests")</c>.
/// </para>
/// <para>
/// The public methods of <see cref="DefaultDPAService"/> (ExecuteDPAAsync, AmendDPAAsync,
/// GetDPAAsync, etc.) do NOT use <c>ArgumentNullException.ThrowIfNull</c> on their parameters;
/// they rely on the constructor-injected dependencies being non-null and use try/catch for
/// runtime errors. This test class validates that the constructor guards are comprehensive
/// and documents the absence of method-level guards.
/// </para>
/// </remarks>
public sealed class DefaultDPAServiceMethodGuardTests
{
    private readonly IAggregateRepository<DPAAggregate> _repository =
        Substitute.For<IAggregateRepository<DPAAggregate>>();

    private readonly IReadModelRepository<DPAReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DPAReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());

    private readonly ILogger<DefaultDPAService> _logger =
        NullLogger<DefaultDPAService>.Instance;

    #region Constructor Guard Completeness

    /// <summary>
    /// Verifies that the constructor accepts all valid non-null parameters without throwing.
    /// </summary>
    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        var act = () => new DefaultDPAService(
            _repository, _readModelRepository, _cache, _timeProvider, _options, _logger);

        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that all six constructor parameters are individually guarded by
    /// trying each null permutation and expecting ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_EachParameter_IsGuarded()
    {
        // repository
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(null!, _readModelRepository, _cache, _timeProvider, _options, _logger))
            .ParamName.ShouldBe("repository");

        // readModelRepository
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(_repository, null!, _cache, _timeProvider, _options, _logger))
            .ParamName.ShouldBe("readModelRepository");

        // cache
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(_repository, _readModelRepository, null!, _timeProvider, _options, _logger))
            .ParamName.ShouldBe("cache");

        // timeProvider
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(_repository, _readModelRepository, _cache, null!, _options, _logger))
            .ParamName.ShouldBe("timeProvider");

        // options
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(_repository, _readModelRepository, _cache, _timeProvider, null!, _logger))
            .ParamName.ShouldBe("options");

        // logger
        Should.Throw<ArgumentNullException>(
            () => new DefaultDPAService(_repository, _readModelRepository, _cache, _timeProvider, _options, null!))
            .ParamName.ShouldBe("logger");
    }

    #endregion
}
