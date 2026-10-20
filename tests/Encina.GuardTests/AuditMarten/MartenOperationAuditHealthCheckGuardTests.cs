using Encina.Audit.Marten.Health;

namespace Encina.GuardTests.AuditMarten;

public class MartenOperationAuditHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MartenOperationAuditHealthCheck(null!));
    }
}
