using Encina.Security.PII.Health;

namespace Encina.GuardTests.Security.PII;

/// <summary>
/// Guard clause tests for <see cref="PIIHealthCheck"/>.
/// Verifies null argument validation on constructor.
/// </summary>
public sealed class PIIHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PIIHealthCheck(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }
}
