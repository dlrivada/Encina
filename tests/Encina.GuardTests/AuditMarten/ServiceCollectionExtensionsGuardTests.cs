using Encina.Audit.Marten;

namespace Encina.GuardTests.AuditMarten;

public class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaAuditMarten_NullServices_Throws()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaAuditMarten());
    }
}
