using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Scheduling;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="CheckDPAExpirationHandler"/> to verify null parameter handling.
/// </summary>
public class CheckDPAExpirationHandlerGuardTests
{
    private readonly IDPAStore _dpaStore = Substitute.For<IDPAStore>();
    private readonly IProcessorRegistry _processorRegistry = Substitute.For<IProcessorRegistry>();
    private readonly IProcessorAuditStore _auditStore = Substitute.For<IProcessorAuditStore>();
    private readonly IEncina _encina = Substitute.For<IEncina>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<CheckDPAExpirationHandler> _logger =
        NullLogger<CheckDPAExpirationHandler>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDpaStore_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            null!, _processorRegistry, _auditStore, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dpaStore");
    }

    [Fact]
    public void Constructor_NullProcessorRegistry_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, null!, _auditStore, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processorRegistry");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, _processorRegistry, null!, _encina, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, _processorRegistry, _auditStore, null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, _processorRegistry, _auditStore, _encina, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, _processorRegistry, _auditStore, _encina, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CheckDPAExpirationHandler(
            _dpaStore, _processorRegistry, _auditStore, _encina, _options, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
