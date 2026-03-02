using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultRetentionEnforcer"/> to verify null parameter handling.
/// </summary>
public class DefaultRetentionEnforcerGuardTests
{
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly ILegalHoldStore _legalHoldStore = Substitute.For<ILegalHoldStore>();
    private readonly IRetentionAuditStore _auditStore = Substitute.For<IRetentionAuditStore>();
    private readonly IOptions<RetentionOptions> _options;
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultRetentionEnforcer> _logger = NullLogger<DefaultRetentionEnforcer>.Instance;

    public DefaultRetentionEnforcerGuardTests()
    {
        _options = Substitute.For<IOptions<RetentionOptions>>();
        _options.Value.Returns(new RetentionOptions());
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            null!, _legalHoldStore, _auditStore, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("recordStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when legalHoldStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLegalHoldStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, null!, _auditStore, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("legalHoldStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when auditStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, _legalHoldStore, null!, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, _legalHoldStore, _auditStore, null!, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, _legalHoldStore, _auditStore, _options, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, _legalHoldStore, _auditStore, _options, _serviceProvider, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionEnforcer(
            _recordStore, _legalHoldStore, _auditStore, _options, _serviceProvider, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
