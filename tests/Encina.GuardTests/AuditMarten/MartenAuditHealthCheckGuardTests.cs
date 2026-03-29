using Encina.Audit.Marten.Health;

namespace Encina.GuardTests.AuditMarten;

public class MartenAuditHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MartenAuditHealthCheck(null!));
    }
}
