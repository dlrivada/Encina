using Encina.Compliance.BreachNotification;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachHandler"/> to verify null parameter handling.
/// </summary>
public class DefaultBreachHandlerGuardTests
{
    private readonly IBreachRecordStore _recordStore = Substitute.For<IBreachRecordStore>();
    private readonly IBreachAuditStore _auditStore = Substitute.For<IBreachAuditStore>();
    private readonly IBreachNotifier _notifier = Substitute.For<IBreachNotifier>();
    private readonly IOptions<BreachNotificationOptions> _options;
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultBreachHandler> _logger = NullLogger<DefaultBreachHandler>.Instance;

    public DefaultBreachHandlerGuardTests()
    {
        _options = Substitute.For<IOptions<BreachNotificationOptions>>();
        _options.Value.Returns(new BreachNotificationOptions());
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            null!, _auditStore, _notifier, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("recordStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when auditStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, null!, _notifier, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when notifier is null.
    /// </summary>
    [Fact]
    public void Constructor_NullNotifier_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, _auditStore, null!, _options, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("notifier");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, _auditStore, _notifier, null!, _serviceProvider, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, _auditStore, _notifier, _options, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, _auditStore, _notifier, _options, _serviceProvider, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachHandler(
            _recordStore, _auditStore, _notifier, _options, _serviceProvider, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
