using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Marten;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Method-level guard tests for <see cref="CheckDPAExpirationHandler"/>.
/// </summary>
/// <remarks>
/// <para>
/// The Handle method of <see cref="CheckDPAExpirationHandler"/> does NOT use
/// <c>ArgumentNullException.ThrowIfNull</c> on its <c>request</c> parameter.
/// This test class validates that the constructor guards are comprehensive
/// and exercises constructor parameter guard combinations.
/// </para>
/// </remarks>
public sealed class CheckDPAExpirationHandlerMethodGuardTests
{
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();
    private readonly IProcessorService _processorService = Substitute.For<IProcessorService>();
    private readonly IAggregateRepository<DPAAggregate> _dpaRepository = Substitute.For<IAggregateRepository<DPAAggregate>>();
    private readonly IEncina _encina = Substitute.For<IEncina>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<CheckDPAExpirationHandler> _logger =
        NullLogger<CheckDPAExpirationHandler>.Instance;

    #region Constructor Guard Completeness

    /// <summary>
    /// Verifies that the constructor accepts all valid non-null parameters without throwing.
    /// </summary>
    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, _dpaRepository, _encina, _options, _timeProvider, _logger);

        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that all seven constructor parameters are individually guarded by
    /// trying each null permutation and expecting ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_EachParameter_IsGuarded()
    {
        // dpaService
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(null!, _processorService, _dpaRepository, _encina, _options, _timeProvider, _logger))
            .ParamName.ShouldBe("dpaService");

        // processorService
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, null!, _dpaRepository, _encina, _options, _timeProvider, _logger))
            .ParamName.ShouldBe("processorService");

        // dpaRepository
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, _processorService, null!, _encina, _options, _timeProvider, _logger))
            .ParamName.ShouldBe("dpaRepository");

        // encina
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, _processorService, _dpaRepository, null!, _options, _timeProvider, _logger))
            .ParamName.ShouldBe("encina");

        // options
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, _processorService, _dpaRepository, _encina, null!, _timeProvider, _logger))
            .ParamName.ShouldBe("options");

        // timeProvider
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, _processorService, _dpaRepository, _encina, _options, null!, _logger))
            .ParamName.ShouldBe("timeProvider");

        // logger
        Should.Throw<ArgumentNullException>(
            () => new CheckDPAExpirationHandler(_dpaService, _processorService, _dpaRepository, _encina, _options, _timeProvider, null!))
            .ParamName.ShouldBe("logger");
    }

    #endregion
}
