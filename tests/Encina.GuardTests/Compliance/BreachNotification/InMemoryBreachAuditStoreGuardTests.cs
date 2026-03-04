using Encina.Compliance.BreachNotification.InMemory;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="InMemoryBreachAuditStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryBreachAuditStoreGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryBreachAuditStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
