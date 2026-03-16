using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Marten;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="CheckDPAExpirationHandler"/> to verify null parameter handling.
/// </summary>
public class CheckDPAExpirationHandlerGuardTests
{
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();
    private readonly IProcessorService _processorService = Substitute.For<IProcessorService>();
    private readonly IAggregateRepository<DPAAggregate> _dpaRepository = Substitute.For<IAggregateRepository<DPAAggregate>>();
    private readonly IEncina _encina = Substitute.For<IEncina>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<CheckDPAExpirationHandler> _logger =
        NullLogger<CheckDPAExpirationHandler>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDpaService_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            null!, _processorService, _dpaRepository, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dpaService");
    }

    [Fact]
    public void Constructor_NullProcessorService_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, null!, _dpaRepository, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processorService");
    }

    [Fact]
    public void Constructor_NullDpaRepository_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, null!, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dpaRepository");
    }

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, _dpaRepository, null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, _dpaRepository, _encina, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, _dpaRepository, _encina, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaService, _processorService, _dpaRepository, _encina, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
