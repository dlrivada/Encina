using Encina.Compliance.BreachNotification.InMemory;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="InMemoryBreachRecordStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryBreachRecordStoreGuardTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<InMemoryBreachRecordStore> _logger = NullLogger<InMemoryBreachRecordStore>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryBreachRecordStore(null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryBreachRecordStore(_timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
